using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;

public class RagDollAgent : Agent 
{
    [Header("Settings")]
	public float FixedDeltaTime = 1f/60f;
    public float SmoothBeta = 0.2f;

    [Header("Camera")]

    public bool RequestCamera;
	public bool CameraFollowMe;
	public Transform CameraTarget;    

    [Header("... debug")]
    public bool SkipRewardSmoothing;
    public bool debugCopyMocap;
    public bool ignorActions;
    public bool dontResetOnZeroReward;
    public bool DebugPauseOnReset;

    MocapController _mocapController;
    List<ArticulationBody> _mocapBodyParts;
    List<ArticulationBody> _bodyParts;
    SpawnableEnv _spawnableEnv;
    DReConObservations _dReConObservations;
    DReConRewards _dReConRewards;
    RagDoll003 _ragDollSettings;
    TrackBodyStatesInWorldSpace _trackBodyStatesInWorldSpace;
    List<ArticulationBody> _motors;
    MarathonTestBedController _debugController;  
    InputController _inputController;
    SensorObservations _sensorObservations;
    DecisionRequester _decisionRequester;

    bool _hasLazyInitialized;
    float[] _smoothedActions;

    void Awake()
    {
		if (RequestCamera && CameraTarget != null)
		{
            // Will follow the last object to be spawned
            var camera = FindObjectOfType<Camera>();
            var follow = camera.GetComponent<SmoothFollow>();
            follow.target = CameraTarget;
        }        
    }
    void Update()
    {
        if (debugCopyMocap)
        {
            Done();
        }
        if (!_hasLazyInitialized)
        {
            return;
        }

        // hadle mocap going out of bounds
        if (!_spawnableEnv.IsPointWithinBoundsInWorldSpace(_mocapController.transform.position)) {
            _mocapController.transform.position = _spawnableEnv.transform.position;
            _trackBodyStatesInWorldSpace.Reset();
            Done();
        }

    }
	override public void CollectObservations()
    {
		var sensor = this;
		if (!_hasLazyInitialized)
		{
			AgentReset();
		}

        float timeDelta = Time.fixedDeltaTime * _decisionRequester.DecisionPeriod;
        _dReConObservations.OnStep(timeDelta);
        _dReConRewards.OnStep(timeDelta);        

        sensor.AddVectorObs(_dReConObservations.MocapCOMVelocity);
        sensor.AddVectorObs(_dReConObservations.RagDollCOMVelocity);
        sensor.AddVectorObs(_dReConObservations.RagDollCOMVelocity-_dReConObservations.MocapCOMVelocity);
        sensor.AddVectorObs(_dReConObservations.InputDesiredHorizontalVelocity);
        sensor.AddVectorObs(_dReConObservations.InputJump);
        sensor.AddVectorObs(_dReConObservations.InputBackflip);
        sensor.AddVectorObs(_dReConObservations.HorizontalVelocityDifference);
        // foreach (var stat in _dReConObservations.MocapBodyStats)
        // {
        //     sensor.AddVectorObs(stat.Position);
        //     sensor.AddVectorObs(stat.Velocity);
        // }
        foreach (var stat in _dReConObservations.RagDollBodyStats)
        {
            sensor.AddVectorObs(stat.Position);
            sensor.AddVectorObs(stat.Velocity);
        }                
        foreach (var stat in _dReConObservations.BodyPartDifferenceStats)
        {
            sensor.AddVectorObs(stat.Position);
            sensor.AddVectorObs(stat.Velocity);
        }
        sensor.AddVectorObs(_dReConObservations.PreviousActions);
        
        // add sensors (feet etc)
        sensor.AddVectorObs(_sensorObservations.SensorIsInTouch);
    }
	public override void AgentAction(float[] vectorAction)
    {
        if (!_hasLazyInitialized)
		{
			return;
		}

        bool shouldDebug = _debugController != null;
        if (_debugController != null)
        {
            shouldDebug &= _debugController.isActiveAndEnabled;
            shouldDebug &= _debugController.gameObject.activeInHierarchy;
        }
        if (shouldDebug)
        {
            if (_debugController.Actions == null || _debugController.Actions.Length == 0)
            {
                _debugController.Actions = vectorAction.Select(x=>0f).ToArray();
            }
            vectorAction = _debugController.Actions.Select(x=>Mathf.Clamp(x,-1f,1f)).ToArray();
        }
        if (!SkipRewardSmoothing)
            vectorAction = SmoothActions(vectorAction);
        if (ignorActions)
            vectorAction = vectorAction.Select(x=>0f).ToArray();
		int i = 0;
		foreach (var m in _motors)
		{
            if (m.isRoot)
                continue;
            Vector3 targetNormalizedRotation = Vector3.zero;
            if (m.swingYLock == ArticulationDofLock.LimitedMotion)
				targetNormalizedRotation.x = vectorAction[i++];
            if (m.swingZLock == ArticulationDofLock.LimitedMotion)
				targetNormalizedRotation.y = vectorAction[i++];
			if (m.twistLock == ArticulationDofLock.LimitedMotion)
				targetNormalizedRotation.z = vectorAction[i++];
            UpdateMotor(m, targetNormalizedRotation);
            
        }
        _dReConObservations.PreviousActions = vectorAction;

        AddReward(_dReConRewards.Reward);
        if (_dReConRewards.Reward <= 0f)
        {
            if (!dontResetOnZeroReward)
                Done();
        }
        else if (_dReConRewards.DistanceFactor <= .5f)
        {
            Transform ragDollCom = _dReConObservations.GetRagDollCOM();
            Vector3 snapPosition = ragDollCom.position;
            snapPosition.y = 0f;
            _mocapController.SnapTo(snapPosition);
        }
    }

    float[] SmoothActions(float[] vectorAction)
    {
        // yt =β at +(1−β)yt−1
        if (_smoothedActions == null)
            _smoothedActions = vectorAction.Select(x=>0f).ToArray();
        _smoothedActions = vectorAction
            .Zip(_smoothedActions, (a, y)=> SmoothBeta * a + (1f-SmoothBeta) * y)
            .ToArray();
        return _smoothedActions;
    }
	public override void AgentReset()
	{
		if (!_hasLazyInitialized)
		{
            _decisionRequester = GetComponent<DecisionRequester>();
            _debugController = FindObjectOfType<MarathonTestBedController>();
    		Time.fixedDeltaTime = FixedDeltaTime;
            _spawnableEnv = GetComponentInParent<SpawnableEnv>();
            _mocapController = _spawnableEnv.GetComponentInChildren<MocapController>();
            _mocapBodyParts = _mocapController.GetComponentsInChildren<ArticulationBody>().ToList();
            _bodyParts = GetComponentsInChildren<ArticulationBody>().ToList();
            _dReConObservations = GetComponent<DReConObservations>();
            _dReConRewards = GetComponent<DReConRewards>();
            var mocapController = _spawnableEnv.GetComponentInChildren<MocapController>();
            _trackBodyStatesInWorldSpace = mocapController.GetComponent<TrackBodyStatesInWorldSpace>();
            _ragDollSettings = GetComponent<RagDoll003>();
            _inputController = _spawnableEnv.GetComponentInChildren<InputController>();
            _sensorObservations = GetComponent<SensorObservations>();

            foreach (var body in GetComponentsInChildren<ArticulationBody>())
            {
                body.solverIterations = 255;
                body.solverVelocityIterations = 255;
            }

            _motors = GetComponentsInChildren<ArticulationBody>()
                .Where(x=>x.jointType == ArticulationJointType.SphericalJoint)
                .Where(x=>!x.isRoot)
                .Distinct()
                .ToList();
            var individualMotors = new List<float>();
            foreach (var m in _motors)
            {
                if (m.swingYLock == ArticulationDofLock.LimitedMotion)
                    individualMotors.Add(0f);
                if (m.swingZLock == ArticulationDofLock.LimitedMotion)
                    individualMotors.Add(0f);
                if (m.twistLock == ArticulationDofLock.LimitedMotion)
                    individualMotors.Add(0f);
            }
            _dReConObservations.PreviousActions = individualMotors.ToArray();
			_hasLazyInitialized = true;
		}
        _smoothedActions = null;
        debugCopyMocap = false;
        _inputController.OnReset();
        _mocapController.GetComponentInChildren<MocapAnimatorController>().OnReset();
        var angle = Vector3.SignedAngle(Vector3.forward, _inputController.HorizontalDirection, Vector3.up);
        var rotation = Quaternion.Euler(0f, angle, 0f);
        _mocapController.OnReset(rotation);
        _mocapController.CopyStatesTo(this.gameObject);
        // _trackBodyStatesInWorldSpace.CopyStatesTo(this.gameObject);
        float timeDelta = float.MinValue;
        _dReConObservations.OnReset();
        _dReConRewards.OnReset();
        _dReConObservations.OnStep(timeDelta);
        _dReConRewards.OnStep(timeDelta);
#if UNITY_EDITOR		
		if (DebugPauseOnReset)
		{
	        UnityEditor.EditorApplication.isPaused = true;
		}
#endif	        
    }    

    void UpdateMotor(ArticulationBody joint, Vector3 targetNormalizedRotation)
    {
        Vector3 power = _ragDollSettings.MusclePowers.First(x=>x.Muscle == joint.name).PowerVector;
        power *= _ragDollSettings.Stiffness;
        float damping = _ragDollSettings.Damping;

		var drive = joint.yDrive;
        var scale = (drive.upperLimit-drive.lowerLimit) / 2f;
        var midpoint = drive.lowerLimit + scale;
        var target = midpoint + (targetNormalizedRotation.x *scale);
        drive.target = target;
        drive.stiffness = power.x;
        drive.damping = damping;
		joint.yDrive = drive;

		drive = joint.zDrive;
        scale = (drive.upperLimit-drive.lowerLimit) / 2f;
        midpoint = drive.lowerLimit + scale;
        target = midpoint + (targetNormalizedRotation.y *scale);
        drive.target = target;
        drive.stiffness = power.y;
        drive.damping = damping;
		joint.zDrive = drive;

		drive = joint.xDrive;
        scale = (drive.upperLimit-drive.lowerLimit) / 2f;
        midpoint = drive.lowerLimit + scale;
        target = midpoint + (targetNormalizedRotation.z *scale);
        drive.target = target;
        drive.stiffness = power.z;
        drive.damping = damping;
		joint.xDrive = drive;
	}

    void FixedUpdate()
    {
        if (debugCopyMocap)
        {
            Done();
        }
    }
    void OnDrawGizmos()
    {
        if (_dReConRewards == null)
            return;
        var comTransform = _dReConRewards._ragDollBodyStats.transform;
        var vector = new Vector3( _inputController.MovementVector.x, 0f, _inputController.MovementVector.y);
        var pos = new Vector3(comTransform.position.x, 0.001f, comTransform.position.z);
        DrawArrow(pos, vector, Color.black);
    }
    void DrawArrow(Vector3 start, Vector3 vector, Color color)
    {
        float headSize = 0.25f;
        float headAngle = 20.0f;
        Gizmos.color = color;
		Gizmos.DrawRay(start, vector);
        if (vector != Vector3.zero)
        { 
            Vector3 right = Quaternion.LookRotation(vector) * Quaternion.Euler(0,180+headAngle,0) * new Vector3(0,0,1);
            Vector3 left = Quaternion.LookRotation(vector) * Quaternion.Euler(0,180-headAngle,0) * new Vector3(0,0,1);
            Gizmos.DrawRay(start + vector, right * headSize);
            Gizmos.DrawRay(start + vector, left * headSize);
        }
    }
}
