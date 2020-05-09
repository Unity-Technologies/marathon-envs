using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;

public class StyleTransfer002Agent : Agent, IOnSensorCollision, IOnTerrainCollision {

	public float FrameReward;
	public float AverageReward;
	public List<float> Rewards;
	public List<float> SensorIsInTouch;
	StyleTransfer002Master _master;
	StyleTransfer002Animator _localStyleAnimator;
	StyleTransfer002Animator _styleAnimator;
	DecisionRequester _decisionRequester;

	List<GameObject> _sensors;

	public bool ShowMonitor = false;

	static int _startCount;
	static ScoreHistogramData _scoreHistogramData;
	int _totalAnimFrames;
	bool _ignorScoreForThisFrame;
	bool _isDone;
	bool _hasLazyInitialized;

	// Use this for initialization
	void Start () {
		_master = GetComponent<StyleTransfer002Master>();
		_decisionRequester = GetComponent<DecisionRequester>();
		var spawnableEnv = GetComponentInParent<SpawnableEnv>();
		_localStyleAnimator = spawnableEnv.gameObject.GetComponentInChildren<StyleTransfer002Animator>();
		_styleAnimator = _localStyleAnimator.GetFirstOfThisAnim();
		_startCount++;
	}

	// Update is called once per frame
	void Update () {
	}

	override public void CollectObservations()
	{
		var sensor = this;
		if (!_hasLazyInitialized)
		{
			AgentReset();
		}

		sensor.AddVectorObs(_master.ObsPhase);

		foreach (var bodyPart in _master.BodyParts)
		{
			sensor.AddVectorObs(bodyPart.ObsLocalPosition);
			sensor.AddVectorObs(bodyPart.ObsRotation);
			sensor.AddVectorObs(bodyPart.ObsRotationVelocity);
			sensor.AddVectorObs(bodyPart.ObsVelocity);
		}
		foreach (var muscle in _master.Muscles)
		{
			if (muscle.ConfigurableJoint.angularXMotion != ConfigurableJointMotion.Locked)
				sensor.AddVectorObs(muscle.TargetNormalizedRotationX);
			if (muscle.ConfigurableJoint.angularYMotion != ConfigurableJointMotion.Locked)
				sensor.AddVectorObs(muscle.TargetNormalizedRotationY);
			if (muscle.ConfigurableJoint.angularZMotion != ConfigurableJointMotion.Locked)
				sensor.AddVectorObs(muscle.TargetNormalizedRotationZ);
		}

		sensor.AddVectorObs(_master.ObsCenterOfMass);
		sensor.AddVectorObs(_master.ObsVelocity);
		sensor.AddVectorObs(SensorIsInTouch);
	}

	public override void AgentAction(float[] vectorAction)
	{
		_isDone = false;
		if (_styleAnimator == _localStyleAnimator)
			_styleAnimator.OnAgentAction();
		_master.OnAgentAction();
		int i = 0;
		foreach (var muscle in _master.Muscles)
		{
			if (muscle.ConfigurableJoint.angularXMotion != ConfigurableJointMotion.Locked)
				muscle.TargetNormalizedRotationX = vectorAction[i++];
			if (muscle.ConfigurableJoint.angularYMotion != ConfigurableJointMotion.Locked)
				muscle.TargetNormalizedRotationY = vectorAction[i++];
			if (muscle.ConfigurableJoint.angularZMotion != ConfigurableJointMotion.Locked)
				muscle.TargetNormalizedRotationZ = vectorAction[i++];
		}

        // the scaler factors are picked empirically by calculating the MaxRotationDistance, MaxVelocityDistance achieved for an untrained agent. 
		var rotationDistance = _master.RotationDistance / 16f ;
		var velocityDistance = _master.VelocityDistance / 6f ;
		var endEffectorDistance = _master.EndEffectorDistance / 1f ;
		var endEffectorVelocityDistance = _master.EndEffectorVelocityDistance / 60f;
		var jointAngularVelocityDistance = _master.JointAngularVelocityDistance / 7000f;
		var centerOfMassDistance = _master.CenterOfMassDistance / 0.3f;
		var sensorDistance = _master.SensorDistance / 1f;

		var rotationReward = 0.65f * Mathf.Exp(-rotationDistance);
		var velocityReward = 0.00f * Mathf.Exp(-velocityDistance);
		var endEffectorReward = 0.15f * Mathf.Exp(-endEffectorDistance);
        var endEffectorVelocityReward = 0.0f * Mathf.Exp(-endEffectorVelocityDistance);
		var jointAngularVelocityReward = 0.1f * Mathf.Exp(-jointAngularVelocityDistance);
		var centerMassReward = 0.1f * Mathf.Exp(-centerOfMassDistance);
		var sensorReward = 0f * Mathf.Exp(-sensorDistance);
        var jointsNotAtLimitReward = 0.00f * Mathf.Exp(-JointsAtLimit());

        //Debug.Log("---------------");
        //Debug.Log("rotation reward: " + rotationReward);
        //Debug.Log("velocityReward: " + velocityReward);
        //Debug.Log("endEffectorReward: " + endEffectorReward);
        //Debug.Log("endEffectorVelocityReward: " + endEffectorVelocityReward);
        //Debug.Log("jointAngularVelocityReward: " + jointAngularVelocityReward);
        //Debug.Log("centerMassReward: " + centerMassReward);
        //Debug.Log("sensorReward: " + sensorReward);
        //Debug.Log("joints not at limit rewards:" + jointsNotAtLimitReward);

        float reward = rotationReward + velocityReward + endEffectorReward + endEffectorVelocityReward + jointAngularVelocityReward + centerMassReward + sensorReward + jointsNotAtLimitReward;

		if (!_master.IgnorRewardUntilObservation)
			AddReward(reward);

		if (reward < 0.1)
			Done();

		if (!_isDone){
			if (_master.IsDone()){
				Done();
				if (_master.StartAnimationIndex > 0)
				 	_master.StartAnimationIndex--;
			}
		}
		FrameReward = reward;
		var stepCount = GetStepCount() > 0 ? GetStepCount() : 1;
		AverageReward = GetCumulativeReward() / (float) stepCount;
	}

	float JointsAtLimit(string[] ignorJoints = null)
	{
		int atLimitCount = 0;
		int totalJoints = 0;
		foreach (var muscle in _master.Muscles)
		{
			if(muscle.Parent == null)
				continue;

			var name = muscle.Name;
			if (ignorJoints != null && ignorJoints.Contains(name))
				continue;
			if (Mathf.Abs(muscle.TargetNormalizedRotationX) >= 1f)
				atLimitCount++;
			if (Mathf.Abs(muscle.TargetNormalizedRotationY) >= 1f)
				atLimitCount++;
			if (Mathf.Abs(muscle.TargetNormalizedRotationZ) >= 1f)
				atLimitCount++;
			totalJoints++;
		}
		float fractionOfJointsAtLimit = (float)atLimitCount / (float)totalJoints;
		return fractionOfJointsAtLimit;
	}

	public void SetTotalAnimFrames(int totalAnimFrames)
	{
		_totalAnimFrames = totalAnimFrames;
		if (_scoreHistogramData == null) {
			var columns = _totalAnimFrames;
			if (_decisionRequester?.DecisionPeriod > 1)
				columns /= _decisionRequester.DecisionPeriod;
			_scoreHistogramData = new ScoreHistogramData(columns, 30);
		}
			Rewards = _scoreHistogramData.GetAverages().Select(x=>(float)x).ToList();
	}

	public override void AgentReset()
	{
		if (!_hasLazyInitialized)
		{
			_master = GetComponent<StyleTransfer002Master>();
			_master.BodyConfig = MarathonManAgent.BodyConfig;
			_master.OnInitializeAgent();
			_decisionRequester = GetComponent<DecisionRequester>();
			var spawnableEnv = GetComponentInParent<SpawnableEnv>();
			_localStyleAnimator = spawnableEnv.gameObject.GetComponentInChildren<StyleTransfer002Animator>();
			_styleAnimator = _localStyleAnimator.GetFirstOfThisAnim();
			_styleAnimator.BodyConfig = MarathonManAgent.BodyConfig;
			_styleAnimator.OnInitializeAgent();
			_hasLazyInitialized = true;
			_localStyleAnimator.DestoryIfNotFirstAnim();
		}
		_isDone = true;
		_ignorScoreForThisFrame = true;
		_master.ResetPhase();
		_sensors = GetComponentsInChildren<SensorBehavior>()
			.Select(x=>x.gameObject)
			.ToList();
		SensorIsInTouch = Enumerable.Range(0,_sensors.Count).Select(x=>0f).ToList();
		if (_scoreHistogramData != null) {
			var column = _master.StartAnimationIndex;
			if (_decisionRequester?.DecisionPeriod > 1)
				column /= _decisionRequester.DecisionPeriod;
			if (_ignorScoreForThisFrame)
				_ignorScoreForThisFrame = false;
			else
	             _scoreHistogramData.SetItem(column, AverageReward);
        }
	}

	public virtual void OnTerrainCollision(GameObject other, GameObject terrain)
	{
		if (string.Compare(terrain.name, "Terrain", true) != 0)
			return;
		if (!_styleAnimator.AnimationStepsReady)
			return;
		var bodyPart = _master.BodyParts.FirstOrDefault(x=>x.Transform.gameObject == other);
		if (bodyPart == null)
			return;
		switch (bodyPart.Group)
		{
			case BodyHelper002.BodyPartGroup.None:
			case BodyHelper002.BodyPartGroup.Foot:
			case BodyHelper002.BodyPartGroup.LegUpper:
			case BodyHelper002.BodyPartGroup.LegLower:
			case BodyHelper002.BodyPartGroup.Hand:
			case BodyHelper002.BodyPartGroup.ArmLower:
			case BodyHelper002.BodyPartGroup.ArmUpper:
				break;
			default:
				Done();
				break;
		}
	}


	public void OnSensorCollisionEnter(Collider sensorCollider, GameObject other) {
			if (string.Compare(other.name, "Terrain", true) !=0)
                return;
            var sensor = _sensors
                .FirstOrDefault(x=>x == sensorCollider.gameObject);
            if (sensor != null) {
                var idx = _sensors.IndexOf(sensor);
                SensorIsInTouch[idx] = 1f;
            }
		}
        public void OnSensorCollisionExit(Collider sensorCollider, GameObject other)
        {
            if (string.Compare(other.gameObject.name, "Terrain", true) !=0)
                return;
            var sensor = _sensors
                .FirstOrDefault(x=>x == sensorCollider.gameObject);
            if (sensor != null) {
                var idx = _sensors.IndexOf(sensor);
                SensorIsInTouch[idx] = 0f;
            }
        }  

}
