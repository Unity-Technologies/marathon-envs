using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;

public class MarathonManAgent : Agent, IOnSensorCollision, IOnTerrainCollision {

	public float FixedDeltaTime = 0.005f;
	public Transform CameraTarget;

	public float FrameReward;
	public float AverageReward;
	public List<float> SensorIsInTouch;

	public List<Muscle002> Muscles;
	public List<BodyPart002> BodyParts;

	List<GameObject> _sensors;

	public bool ShowMonitor = false;
	public bool DebugDisableMotor;

	public List<float> Observations;
	public int ObservationNormalizedErrors;
	public int MaxObservationNormalizedErrors;
	public bool DebugShowWithOffset;

	static int _startCount;
	float[] lastVectorAction;
	// static ScoreHistogramData _scoreHistogramData;

	// Use this for initialization
	void FixedUpdate()
	{
		foreach (var muscle in Muscles)
		{
			var i = Muscles.IndexOf(muscle);
			muscle.UpdateObservations();
			if (!DebugShowWithOffset && !DebugDisableMotor)
				muscle.UpdateMotor();
			if (!muscle.Rigidbody.useGravity)
				continue; // skip sub joints
		}
	}
	void Start () {
		Time.fixedDeltaTime = FixedDeltaTime;

		BodyParts = new List<BodyPart002> ();
		BodyPart002 root = null;
		foreach (var t in GetComponentsInChildren<Transform>())
		{
			if (BodyHelper002.GetBodyPartGroup(t.name) == BodyHelper002.BodyPartGroup.None)
				continue;
			
			var bodyPart = new BodyPart002{
				Rigidbody = t.GetComponent<Rigidbody>(),
				Transform = t,
				Name = t.name,
				Group = BodyHelper002.GetBodyPartGroup(t.name), 
			};
			if (bodyPart.Group == BodyHelper002.BodyPartGroup.Hips)
				root = bodyPart;
			bodyPart.Root = root;
			bodyPart.Init();
			BodyParts.Add(bodyPart);
		}
		var partCount = BodyParts.Count;

		Muscles = new List<Muscle002> ();
		var muscles = GetComponentsInChildren<ConfigurableJoint>();
		ConfigurableJoint rootConfigurableJoint = null;
		var ragDoll = GetComponent<RagDoll002>();
		foreach (var m in muscles)
		{
			var maximumForce = ragDoll.MusclePowers.First(x=>x.Muscle == m.name).PowerVector;
			// maximumForce *= 2f;
			var muscle = new Muscle002{
				Rigidbody = m.GetComponent<Rigidbody>(),
				Transform = m.GetComponent<Transform>(),
				ConfigurableJoint = m,
				Name = m.name,
				Group = BodyHelper002.GetMuscleGroup(m.name),
				MaximumForce = maximumForce
			};
			if (muscle.Group == BodyHelper002.MuscleGroup.Hips)
				rootConfigurableJoint = muscle.ConfigurableJoint;
			muscle.RootConfigurableJoint = rootConfigurableJoint;
			muscle.Init();

			Muscles.Add(muscle);			
		}
		_startCount++;
	}
	
	// Update is called once per frame
	void Update () {
	}

	override public void InitializeAgent()
	{

	}

	// override public void CollectObservations()
	// {
	// 	// AddVectorObs(ObsPhase);
	// 	foreach (var bodyPart in BodyParts)
	// 	{
	// 		bodyPart.UpdateObservations();
	// 		AddVectorObs(bodyPart.ObsLocalPosition);
	// 		AddVectorObs(bodyPart.ObsRotation);
	// 		AddVectorObs(bodyPart.ObsRotationVelocity);
	// 		AddVectorObs(bodyPart.ObsVelocity);
	// 	}
	// 	foreach (var muscle in Muscles)
	// 	{
	// 		muscle.UpdateObservations();
	// 		if (muscle.ConfigurableJoint.angularXMotion != ConfigurableJointMotion.Locked)
	// 			AddVectorObs(muscle.TargetNormalizedRotationX);
	// 		if (muscle.ConfigurableJoint.angularYMotion != ConfigurableJointMotion.Locked)
	// 			AddVectorObs(muscle.TargetNormalizedRotationY);
	// 		if (muscle.ConfigurableJoint.angularZMotion != ConfigurableJointMotion.Locked)
	// 			AddVectorObs(muscle.TargetNormalizedRotationZ);
	// 	}

	// 	// AddVectorObs(ObsCenterOfMass);
	// 	// AddVectorObs(ObsVelocity);
	// 	AddVectorObs(SensorIsInTouch);
	// }
	override public void CollectObservations()
	{
        var pelvis = BodyParts.FirstOrDefault(x=>x.Group == BodyHelper002.BodyPartGroup.Hips);
        var shoulders = BodyParts.FirstOrDefault(x=>x.Group == BodyHelper002.BodyPartGroup.Torso);

        Vector3 normalizedVelocity = this.GetNormalizedVelocity(pelvis.Rigidbody.velocity);
        AddVectorObs(normalizedVelocity);
        AddVectorObs(pelvis.Rigidbody.transform.forward); // gyroscope 
        AddVectorObs(pelvis.Rigidbody.transform.up);

        AddVectorObs(shoulders.Rigidbody.transform.forward); // gyroscope 
        AddVectorObs(shoulders.Rigidbody.transform.up);

        AddVectorObs(SensorIsInTouch);
		foreach (var bodyPart in BodyParts)
		{
			bodyPart.UpdateObservations();
			AddVectorObs(bodyPart.ObsRotation);
			AddVectorObs(bodyPart.ObsRotationVelocity);
			AddVectorObs(this.GetNormalizedVelocity(bodyPart.ObsVelocity));
		}
		foreach (var muscle in Muscles)
		{
			muscle.UpdateObservations();
			if (muscle.ConfigurableJoint.angularXMotion != ConfigurableJointMotion.Locked)
				AddVectorObs(muscle.TargetNormalizedRotationX);
			if (muscle.ConfigurableJoint.angularYMotion != ConfigurableJointMotion.Locked)
				AddVectorObs(muscle.TargetNormalizedRotationY);
			if (muscle.ConfigurableJoint.angularZMotion != ConfigurableJointMotion.Locked)
				AddVectorObs(muscle.TargetNormalizedRotationZ);
		}
		var sensorYpositions = _sensors
			.Select(x=> this.GetNormalizedPosition(x.transform.position))
			.Select(x=>x.y)
			.ToList();
		AddVectorObs(sensorYpositions);

		var info = GetInfo();
		if (Observations?.Count != info.vectorObservation.Count)
			Observations = Enumerable.Range(0, info.vectorObservation.Count).Select(x => 0f).ToList();
		ObservationNormalizedErrors = 0;
		for (int i = 0; i < Observations.Count; i++)
		{
			Observations[i] = info.vectorObservation[i];
			var x = Mathf.Abs(Observations[i]);
			var e = Mathf.Epsilon;
			bool is1 = Mathf.Approximately(x, 1f);
			if ((x > 1f + e) && !is1)
				ObservationNormalizedErrors++;
		}
		if (ObservationNormalizedErrors > MaxObservationNormalizedErrors)
			MaxObservationNormalizedErrors = ObservationNormalizedErrors;
	}

	public override void AgentAction(float[] vectorAction, string textAction)
	{
		if (lastVectorAction == null)
			lastVectorAction = vectorAction.Select(x=>0f).ToArray();
		var vectorDifferent = new List<float>();
		int i = 0;
		foreach (var muscle in Muscles)
		{
			// if(muscle.Parent == null)
			// 	continue;
			if (muscle.ConfigurableJoint.angularXMotion != ConfigurableJointMotion.Locked){
				vectorDifferent.Add(Mathf.Abs(vectorAction[i]-lastVectorAction[i]));
				muscle.TargetNormalizedRotationX = vectorAction[i++];
			}
			if (muscle.ConfigurableJoint.angularYMotion != ConfigurableJointMotion.Locked){
				vectorDifferent.Add(Mathf.Abs(vectorAction[i]-lastVectorAction[i]));
				muscle.TargetNormalizedRotationY = vectorAction[i++];
			}
			if (muscle.ConfigurableJoint.angularZMotion != ConfigurableJointMotion.Locked){
				vectorDifferent.Add(Mathf.Abs(vectorAction[i]-lastVectorAction[i]));
				muscle.TargetNormalizedRotationZ = vectorAction[i++];
			}
		}
        // float heightPenality = 1f-GetHeightPenality(1.2f);
        // heightPenality = Mathf.Clamp(heightPenality, 0f, 1f);
        // float uprightBonus = GetDirectionBonus("pelvis", Vector3.forward, 1f);
        // uprightBonus = Mathf.Clamp(uprightBonus, 0f, 1f);
        var pelvis = BodyParts.FirstOrDefault(x=>x.Group == BodyHelper002.BodyPartGroup.Hips);
        float velocity = Mathf.Clamp(this.GetNormalizedVelocity(pelvis.Rigidbody.velocity).x, 0f, 1f);
        // float effort = 1f - GetEffortNormalized();
		float effort = 1f - vectorDifferent.Average();
		effort = Mathf.Clamp(effort, 0, 1);
		effort = Mathf.Pow(effort,2);

        if (ShowMonitor)
        {
            // var hist = new[] {velocity, uprightBonus, heightPenality, effort}.ToList();
            // Monitor.Log("rewardHist", hist.ToArray(), displayType: Monitor.DisplayType.INDEPENDENT);
        }

        // heightPenality *= 0.05f;
        // uprightBonus *= 0.05f;
        velocity *= 0.5f;
        if (velocity >= .5f)
            effort *= 0.5f;
        else
            effort *= velocity;

        var reward = velocity
                    //  + uprightBonus
                    //  + heightPenality
                     + effort;		

		FrameReward = reward;
		var stepCount = GetStepCount() > 0 ? GetStepCount() : 1;
		AverageReward = GetCumulativeReward() / (float) stepCount;
	}

	float GetEffort(string[] ignorJoints = null)
	{
		double effort = 0;
		foreach (var muscle in Muscles)
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

	float GetEffortNormalized(string[] ignorJoints = null)
	{
		double effort = 0;
		double jointEffort = 0;
		double joints = 0;
		foreach (var muscle in Muscles)
		{
			if(muscle.Parent == null)
				continue;
			var name = muscle.Name;
			if (ignorJoints != null && ignorJoints.Contains(name))
				continue;
			if (muscle.ConfigurableJoint.angularXMotion != ConfigurableJointMotion.Locked) {
				jointEffort = Mathf.Pow(Mathf.Abs(muscle.TargetNormalizedRotationX),2);
				effort += jointEffort;
				joints++;
			}
			if (muscle.ConfigurableJoint.angularYMotion != ConfigurableJointMotion.Locked) {
				jointEffort = Mathf.Pow(Mathf.Abs(muscle.TargetNormalizedRotationY),2);
				effort += jointEffort;
				joints++;
			}
			if (muscle.ConfigurableJoint.angularZMotion != ConfigurableJointMotion.Locked) {
				jointEffort = Mathf.Pow(Mathf.Abs(muscle.TargetNormalizedRotationZ),2);
				effort += jointEffort;
				joints++;
			}
		}

		return (float) (effort / joints);
	}

	float JointsAtLimit(string[] ignorJoints = null)
	{
		int atLimitCount = 0;
		int totalJoints = 0;
		foreach (var muscle in Muscles)
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
	// public void SetTotalAnimFrames(int totalAnimFrames)
	// {
	// 	_totalAnimFrames = totalAnimFrames;
	// 	if (_scoreHistogramData == null) {
	// 		var columns = _totalAnimFrames / agentParameters.numberOfActionsBetweenDecisions;
	// 		_scoreHistogramData = new ScoreHistogramData(columns, 30);
	// 	}
	// 		Rewards = _scoreHistogramData.GetAverages().Select(x=>(float)x).ToList();
	// }

	public override void AgentReset()
	{
		_sensors = GetComponentsInChildren<SensorBehavior>()
			.Select(x=>x.gameObject)
			.ToList();
		SensorIsInTouch = Enumerable.Range(0,_sensors.Count).Select(x=>0f).ToList();
		// HACK first spawned agent should grab the camera
		var smoothFollow = FindObjectOfType<SmoothFollow>();
		if (smoothFollow != null && smoothFollow.target == null)
			smoothFollow.target = CameraTarget;
		lastVectorAction = null;
	}
	public virtual void OnTerrainCollision(GameObject other, GameObject terrain)
	{
		if (string.Compare(terrain.name, "Terrain", true) != 0)
			return;
		// if (!_styleAnimator.AnimationStepsReady)
		// 	return;
		var bodyPart = BodyParts.FirstOrDefault(x=>x.Transform.gameObject == other);
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
				// if (IsInferenceMode == false)
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
	Vector3 GetCenterOfMass()
	{
		var centerOfMass = Vector3.zero;
		float totalMass = 0f;
		var bodies = BodyParts
			.Select(x=>x.Rigidbody)
			.Where(x=>x!=null)
			.ToList();
		foreach (Rigidbody rb in bodies)
		{
			centerOfMass += rb.worldCenterOfMass * rb.mass;
			totalMass += rb.mass;
		}
		centerOfMass /= totalMass;
		centerOfMass -= transform.parent.position;
		return centerOfMass;
	}

	float NextGaussian(float mu = 0, float sigma = 1)
	{
		var u1 = UnityEngine.Random.value;
		var u2 = UnityEngine.Random.value;

		var rand_std_normal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) *
							Mathf.Sin(2.0f * Mathf.PI * u2);

		var rand_normal = mu + sigma * rand_std_normal;

		return rand_normal;
	}
}
