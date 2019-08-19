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
	// StyleTransfer002TrainerAgent _trainerAgent;

	List<GameObject> _sensors;

	public bool ShowMonitor = false;

	static int _startCount;
	static ScoreHistogramData _scoreHistogramData;
	int _totalAnimFrames;
	bool _ignorScoreForThisFrame;

	// Use this for initialization
	void Start () {
		_master = GetComponent<StyleTransfer002Master>();
		var spawnableEnv = GetComponentInParent<SpawnableEnv>();
		_localStyleAnimator = spawnableEnv.gameObject.GetComponentInChildren<StyleTransfer002Animator>();
		_styleAnimator = _localStyleAnimator.GetFirstOfThisAnim();
		// _styleAnimator = _localStyleAnimator;
		_startCount++;
	}
	
	// Update is called once per frame
	void Update () {
	}

	override public void InitializeAgent()
	{

	}

	override public void CollectObservations()
	{
		// for (int i = 0; i < 255; i++)
		// 	AddVectorObs(0f);
		// return;
		AddVectorObs(_master.ObsPhase);

		// if (false){
		// 	// temp hack to support old models
		// 	if (SensorIsInTouch?.Count>0){
		// 		AddVectorObs(SensorIsInTouch[0]);
		// 		AddVectorObs(0f);
		// 		AddVectorObs(SensorIsInTouch[1]);
		// 		AddVectorObs(0f);
		// 	}
		// } else {
		// 	AddVectorObs(_master.ObsCenterOfMass);
		// 	AddVectorObs(_master.ObsVelocity);
		// 	AddVectorObs(SensorIsInTouch);	
		// }

		foreach (var bodyPart in _master.BodyParts)
		{
			AddVectorObs(bodyPart.ObsLocalPosition);
			AddVectorObs(bodyPart.ObsRotation);
			AddVectorObs(bodyPart.ObsRotationVelocity);
			AddVectorObs(bodyPart.ObsVelocity);
		}
		foreach (var muscle in _master.Muscles)
		{
			if (muscle.ConfigurableJoint.angularXMotion != ConfigurableJointMotion.Locked)
				AddVectorObs(muscle.TargetNormalizedRotationX);
			if (muscle.ConfigurableJoint.angularYMotion != ConfigurableJointMotion.Locked)
				AddVectorObs(muscle.TargetNormalizedRotationY);
			if (muscle.ConfigurableJoint.angularZMotion != ConfigurableJointMotion.Locked)
				AddVectorObs(muscle.TargetNormalizedRotationZ);
		}

		AddVectorObs(_master.ObsCenterOfMass);
		AddVectorObs(_master.ObsVelocity);
		AddVectorObs(SensorIsInTouch);	
	}

	public override void AgentAction(float[] vectorAction, string textAction)
	{
		if (_styleAnimator == _localStyleAnimator)
			_styleAnimator.OnAgentAction();
		_master.OnAgentAction();
		int i = 0;
		foreach (var muscle in _master.Muscles)
		{
			// if(muscle.Parent == null)
			// 	continue;
			if (muscle.ConfigurableJoint.angularXMotion != ConfigurableJointMotion.Locked)
				muscle.TargetNormalizedRotationX = vectorAction[i++];
			if (muscle.ConfigurableJoint.angularYMotion != ConfigurableJointMotion.Locked)
				muscle.TargetNormalizedRotationY = vectorAction[i++];
			if (muscle.ConfigurableJoint.angularZMotion != ConfigurableJointMotion.Locked)
				muscle.TargetNormalizedRotationZ = vectorAction[i++];
		}
        float effort = GetEffort();
        var effortPenality = 0.05f * (float)effort;
		
		// var poseReward = 1f - _master.RotationDistance;
		// var velocityReward = 1f - Mathf.Abs(_master.VelocityDistance);
		// var endEffectorReward = 1f - _master.EndEffectorDistance;
		// // var feetPoseReward = 1f - _master.FeetRotationDistance;
		// var centerMassReward = 1f - _master.CenterOfMassDistance;
		// var sensorReward = 1f - _master.SensorDistance;

		var rotationDistanceScale = (float)_master.BodyParts.Count;
		var velocityDistanceScale = 3f;
		var endEffectorDistanceScale = 8f;
		var centerOfMassDistancScalee = 5f;
		var sensorDistanceScale = 1f;
		var rotationDistance = _master.RotationDistance;
		var velocityDistance = Mathf.Abs(_master.VelocityDistance);
		var endEffectorDistance = _master.EndEffectorDistance;
		var centerOfMassDistance = _master.CenterOfMassDistance;
		var sensorDistance = _master.SensorDistance;
		rotationDistance = Mathf.Clamp(rotationDistance, 0f, rotationDistanceScale);
		velocityDistance = Mathf.Clamp(velocityDistance, 0f, velocityDistanceScale);
		endEffectorDistance = Mathf.Clamp(endEffectorDistance, 0f, endEffectorDistanceScale);
		centerOfMassDistance = Mathf.Clamp(centerOfMassDistance, 0f, centerOfMassDistancScalee);
		sensorDistance = Mathf.Clamp(sensorDistance, 0f, sensorDistanceScale);

		var rotationReward = (rotationDistanceScale - rotationDistance) / rotationDistanceScale;
		var velocityReward = (velocityDistanceScale - velocityDistance) / velocityDistanceScale;
		var endEffectorReward = (endEffectorDistanceScale - endEffectorDistance) / endEffectorDistanceScale;
		var centerMassReward = (centerOfMassDistancScalee - centerOfMassDistance) / centerOfMassDistancScalee;
		var sensorReward = (sensorDistanceScale - sensorDistance) / sensorDistanceScale;
		rotationReward = Mathf.Pow(rotationReward, rotationDistanceScale);
		velocityReward = Mathf.Pow(velocityReward, velocityDistanceScale);
		endEffectorReward = Mathf.Pow(endEffectorReward, endEffectorDistanceScale);
		centerMassReward = Mathf.Pow(centerMassReward, centerOfMassDistancScalee);
		sensorReward = Mathf.Pow(sensorReward, sensorDistanceScale);

		float rotationRewardScale = .65f*.9f;
		float velocityRewardScale = .1f*.9f;
		float endEffectorRewardScale = .15f*.9f;
		float centerMassRewardScale = .1f*.9f;
		float sensorRewardScale = .1f*.9f;

		// float poseRewardScale = .65f;
		// float velocityRewardScale = .1f;
		// float endEffectorRewardScale = .15f;
		// // float feetRewardScale = .15f;
		// float centerMassRewardScale = .1f;
		// float sensorRewardScale = .1f;

		// poseReward = Mathf.Clamp(poseReward, -1f, 1f);
		// velocityReward = Mathf.Clamp(velocityReward, -1f, 1f);
		// endEffectorReward = Mathf.Clamp(endEffectorReward, -1f, 1f);
		// centerMassReward = Mathf.Clamp(centerMassReward, -1f, 1f);
		// feetPoseReward = Mathf.Clamp(feetPoseReward, -1f, 1f);
		// sensorReward = Mathf.Clamp(sensorReward, -1f, 1f);
        var jointsNotAtLimitReward = 1f - JointsAtLimit();
		var jointsNotAtLimitRewardScale = .09f;


		float distanceReward = 
			(rotationReward * rotationRewardScale) +
			(velocityReward * velocityRewardScale) +
			(endEffectorReward * endEffectorRewardScale) +
			// (feetPoseReward * feetRewardScale) +
			(centerMassReward * centerMassRewardScale) + 
			(sensorReward * sensorRewardScale);
		float reward = 
			distanceReward
			// - effortPenality +
			+ (jointsNotAtLimitReward * jointsNotAtLimitRewardScale);

		// HACK _startCount used as Monitor does not like reset
        if (ShowMonitor && _startCount < 2) {
            // Monitor.Log("start frame hist", Rewards.ToArray());
            var hist = new []{
                reward,
				distanceReward,
                (jointsNotAtLimitReward * jointsNotAtLimitRewardScale), 
                // - effortPenality, 
				(rotationReward * rotationRewardScale),
				(velocityReward * velocityRewardScale),
				(endEffectorReward * endEffectorRewardScale),
				// (feetPoseReward * feetRewardScale),
				(centerMassReward * centerMassRewardScale),
				(sensorReward * sensorRewardScale),
				}.ToList();
            Monitor.Log("rewardHist", hist.ToArray());
        }

		if (!_master.IgnorRewardUntilObservation)
			AddReward(reward);
		// if (distanceReward < 0.18f && _master.IsInferenceMode == false)
		// if (distanceReward < 0.334f && _master.IsInferenceMode == false)
		// if (distanceReward < 0.25f && _master.IsInferenceMode == false)
		// if (_trainerAgent.ShouldAgentTerminate(distanceReward) && _master.IsInferenceMode == false)
			// Done();
		// if (GetStepCount() >= 50 && _master.IsInferenceMode == false)
		if (distanceReward < 0.334f && _master.IsInferenceMode == false)
			Done();
		if (!IsDone()){
			// // if (distanceReward < _master.ErrorCutoff && !_master.DebugShowWithOffset) {
			// if (shouldTerminate && !_master.DebugShowWithOffset) {
			// 	AddReward(-10f);
			// 	Done();
			// 	// _master.StartAnimationIndex = _muscleAnimator.AnimationSteps.Count-1;
			// 	if (_master.StartAnimationIndex < _styleAnimator.AnimationSteps.Count-1)
			// 		_master.StartAnimationIndex++;
			// }
			if (_master.IsDone()){
				// AddReward(1f*(float)this.GetStepCount());
				// AddReward(10f);
				Done();
				// if (_master.StartAnimationIndex > 0 && distanceReward >= _master.ErrorCutoff)
				// if (_master.StartAnimationIndex > 0 && !shouldTerminate)
				if (_master.StartAnimationIndex > 0)
				 	_master.StartAnimationIndex--;
			}
		}
		FrameReward = reward;
		var stepCount = GetStepCount() > 0 ? GetStepCount() : 1;
		AverageReward = GetCumulativeReward() / (float) stepCount;
	}
	float GetEffort(string[] ignorJoints = null)
	{
		double effort = 0;
		foreach (var muscle in _master.Muscles)
		{
			if(muscle.Parent == null)
				continue;
			var name = muscle.Name;
			if (ignorJoints != null && ignorJoints.Contains(name))
				continue;
			var jointEffort = Mathf.Pow(Mathf.Abs(muscle.TargetNormalizedRotationX),2);
			effort += jointEffort;
			jointEffort = Mathf.Pow(Mathf.Abs(muscle.TargetNormalizedRotationY),2);
			effort += jointEffort;
			jointEffort = Mathf.Pow(Mathf.Abs(muscle.TargetNormalizedRotationZ),2);
			effort += jointEffort;
		}
		return (float)effort;
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
			var columns = _totalAnimFrames / agentParameters.numberOfActionsBetweenDecisions;
			_scoreHistogramData = new ScoreHistogramData(columns, 30);
		}
			Rewards = _scoreHistogramData.GetAverages().Select(x=>(float)x).ToList();
	}

	public override void AgentReset()
	{
		_ignorScoreForThisFrame = true;
		_master.ResetPhase();
		_sensors = GetComponentsInChildren<SensorBehavior>()
			.Select(x=>x.gameObject)
			.ToList();
		SensorIsInTouch = Enumerable.Range(0,_sensors.Count).Select(x=>0f).ToList();
		if (_scoreHistogramData != null) {
			var column = _master.StartAnimationIndex / agentParameters.numberOfActionsBetweenDecisions;
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
				// AddReward(-100f);
				Done();
				// if (_master.IsInferenceMode == false)
				// 	Done();
				break;
			// case BodyHelper002.BodyPartGroup.Hand:
			// 	// AddReward(-.5f);
			// 	Done();
			// 	break;
			// case BodyHelper002.BodyPartGroup.Head:
			// 	// AddReward(-2f);
			// 	Done();
			// 	break;
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
