using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;
using static BodyHelper002;

public class BodyManager002 : MonoBehaviour, IOnSensorCollision
{
    // Options / Configurables global properties
	public Transform CameraTarget;
	public float FixedDeltaTime = 0.005f;
	public bool ShowMonitor = false;
	public bool DebugDisableMotor;
	public bool DebugShowWithOffset;


    // Observations / Read only global properties
	public List<Muscle002> Muscles;
	public List<BodyPart002> BodyParts;
	public List<float> SensorIsInTouch;
	public List<float> Observations;
	public int ObservationNormalizedErrors;
	public int MaxObservationNormalizedErrors;
	public List<GameObject> Sensors;
	public float FrameReward;
	public float AverageReward;


    // private properties
	Vector3 startPosition;

	Dictionary<GameObject, Vector3> transformsPosition;
	Dictionary<GameObject, Quaternion> transformsRotation;

	Agent _agent;

	static int _startCount;

    float[] lastVectorAction;
	float[] vectorDifference;

	
	// static ScoreHistogramData _scoreHistogramData;


	// void FixedUpdate()
	// {
	// 	foreach (var muscle in Muscles)
	// 	{
	// 	// 	var i = Muscles.IndexOf(muscle);
	// 	// 	muscle.UpdateObservations();
	// 	// 	if (!DebugShowWithOffset && !DebugDisableMotor)
	// 	// 		muscle.UpdateMotor();
	// 	// 	if (!muscle.Rigidbody.useGravity)
	// 	// 		continue; // skip sub joints
	// 	// }
	// }



    // Start is called before the first frame update
    void Start()
    {
        SetupBody();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void OnAgentReset()
	{
		HandleModelReset();
		Sensors = _agent.GetComponentsInChildren<SensorBehavior>()
			.Select(x=>x.gameObject)
			.ToList();
		SensorIsInTouch = Enumerable.Range(0,Sensors.Count).Select(x=>0f).ToList();
		// HACK first spawned agent should grab the camera
		var smoothFollow = GameObject.FindObjectOfType<SmoothFollow>();
		if (smoothFollow != null && smoothFollow.target == null)
			smoothFollow.target = CameraTarget;
		lastVectorAction = null;
		vectorDifference = null;		
	}

	public void OnAgentAction(float[] vectorAction, string textAction)
	{
		if (lastVectorAction == null){
			lastVectorAction = vectorAction.Select(x=>0f).ToArray();
			vectorDifference = vectorAction.Select(x=>0f).ToArray();
		}
		int i = 0;
		foreach (var muscle in Muscles)
		{
			// if(muscle.Parent == null)
			// 	continue;
			if (muscle.ConfigurableJoint.angularXMotion != ConfigurableJointMotion.Locked){
				vectorDifference[i] = Mathf.Abs(vectorAction[i]-lastVectorAction[i]);
				muscle.TargetNormalizedRotationX = vectorAction[i++];
			}
			if (muscle.ConfigurableJoint.angularYMotion != ConfigurableJointMotion.Locked){
				vectorDifference[i] = Mathf.Abs(vectorAction[i]-lastVectorAction[i]);
				muscle.TargetNormalizedRotationY = vectorAction[i++];
			}
			if (muscle.ConfigurableJoint.angularZMotion != ConfigurableJointMotion.Locked){
				vectorDifference[i] = Mathf.Abs(vectorAction[i]-lastVectorAction[i]);
				muscle.TargetNormalizedRotationZ = vectorAction[i++];
			}
			if (!DebugDisableMotor)
				muscle.UpdateMotor();
		}

        if (ShowMonitor)
        {
            // var hist = new[] {velocity, uprightBonus, heightPenality, effort}.ToList();
            // Monitor.Log("rewardHist", hist.ToArray(), displayType: Monitor.DisplayType.INDEPENDENT);
        }
	}

    public BodyPart002 GetFirstBodyPart(BodyPartGroup bodyPartGroup)
    {
        var bodyPart = BodyParts.FirstOrDefault(x=>x.Group == bodyPartGroup);
        return bodyPart;
    }

    public float GetActionDifference()
    {
		float actionDifference = 1f - vectorDifference.Average();
		actionDifference = Mathf.Clamp(actionDifference, 0, 1);
		actionDifference = Mathf.Pow(actionDifference,2);
        return actionDifference;
    }

    void SetupBody()
    {
        _agent = GetComponent<Agent>();
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



	void HandleModelReset()
	{
		Transform[] allChildren = _agent.GetComponentsInChildren<Transform>();
		if (transformsPosition != null)
		{
			foreach (var child in allChildren)
			{
				child.position = transformsPosition[child.gameObject];
				child.rotation = transformsRotation[child.gameObject];
				var childRb = child.GetComponent<Rigidbody>();
				if (childRb != null)
				{
					childRb.angularVelocity = Vector3.zero;
					childRb.velocity = Vector3.zero;
				}
			}

		}
		else
		{
			startPosition = _agent.transform.position;
			transformsPosition = new Dictionary<GameObject, Vector3>();
			transformsRotation = new Dictionary<GameObject, Quaternion>();
			foreach (Transform child in allChildren)
			{
				transformsPosition[child.gameObject] = child.position;
				transformsRotation[child.gameObject] = child.rotation;
			}
		}
	}

	public float GetEffortNormalized(string[] ignorJoints = null)
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
	public void OnSensorCollisionEnter(Collider sensorCollider, GameObject other) {
		if (string.Compare(other.name, "Terrain", true) !=0)
			return;
		var sensor = Sensors
			.FirstOrDefault(x=>x == sensorCollider.gameObject);
		if (sensor != null) {
			var idx = Sensors.IndexOf(sensor);
			SensorIsInTouch[idx] = 1f;
		}
	}
	public void OnSensorCollisionExit(Collider sensorCollider, GameObject other)
	{
		if (string.Compare(other.gameObject.name, "Terrain", true) !=0)
			return;
		var sensor = Sensors
			.FirstOrDefault(x=>x == sensorCollider.gameObject);
		if (sensor != null) {
			var idx = Sensors.IndexOf(sensor);
			SensorIsInTouch[idx] = 0f;
		}
	}  
	public Vector3 GetLocalCenterOfMass()
    {
        var centerOfMass = GetCenterOfMass();
		centerOfMass -= transform.position;
        return centerOfMass;
    }
	public Vector3 GetCenterOfMass()
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
		return centerOfMass;
	}

    public Vector3 GetNormalizedPosition(Vector3 pos)
    {
        var n = _agent.GetNormalizedPosition(pos);
        return n;
    }

    public Vector3 GetNormalizedVelocity(Vector3 metersPerSecond)
    {
        var normalizedPosition = _agent.GetNormalizedVelocity(metersPerSecond);
        return normalizedPosition;
    }

    public Vector3 GetNormalizedVelocity()
    {
        var pelvis = GetFirstBodyPart(BodyPartGroup.Hips); 
        Vector3 metersPerSecond = pelvis.Rigidbody.velocity;
        var n = _agent.GetNormalizedVelocity(metersPerSecond);
        return n;
    }

    public Vector3 GetNormalizedPosition()
    {
		// var position = GetCenterOfMass();
        var pelvis = GetFirstBodyPart(BodyPartGroup.Hips); 
		var position = pelvis.Transform.position;
		var normalizedPosition = GetNormalizedPosition(position - startPosition);
        return normalizedPosition;
    }
    public void SetDebugFrameReward(float reward)
	{
		FrameReward = reward;
		var stepCount = _agent.GetStepCount() > 0 ? _agent.GetStepCount() : 1;
		if (_agent.agentParameters.skipActionsWithDecisions && _agent.agentParameters.numberOfActionsBetweenDecisions > 1)
			stepCount /= _agent.agentParameters.numberOfActionsBetweenDecisions;
		AverageReward = _agent.GetCumulativeReward() / (float) stepCount;		
	}


    public List<float> GetSensorIsInTouch()
    {
        return SensorIsInTouch;
    }
    public List<float> GetBodyPartsObservations()
    {
        List<float> vectorObservation = new List<float>();
		foreach (var bodyPart in BodyParts)
		{
			bodyPart.UpdateObservations();
			// _agent.AddVectorObs(bodyPart.ObsRotation);
            vectorObservation.Add(bodyPart.ObsRotation.x);
            vectorObservation.Add(bodyPart.ObsRotation.y);
            vectorObservation.Add(bodyPart.ObsRotation.z);
            vectorObservation.Add(bodyPart.ObsRotation.w);

			// _agent.AddVectorObs(bodyPart.ObsRotationVelocity);
            vectorObservation.Add(bodyPart.ObsRotationVelocity.x);
            vectorObservation.Add(bodyPart.ObsRotationVelocity.y);
            vectorObservation.Add(bodyPart.ObsRotationVelocity.z);

			// _agent.AddVectorObs(GetNormalizedVelocity(bodyPart.ObsVelocity));
            var normalizedVelocity = GetNormalizedVelocity(bodyPart.ObsVelocity);
            vectorObservation.Add(normalizedVelocity.x);
            vectorObservation.Add(normalizedVelocity.y);
            vectorObservation.Add(normalizedVelocity.z);
		}
        return vectorObservation;
    }
    public List<float> GetMusclesObservations()
    {
        List<float> vectorObservation = new List<float>();
		foreach (var muscle in Muscles)
		{
			muscle.UpdateObservations();
			if (muscle.ConfigurableJoint.angularXMotion != ConfigurableJointMotion.Locked)
				vectorObservation.Add(muscle.TargetNormalizedRotationX);
			if (muscle.ConfigurableJoint.angularYMotion != ConfigurableJointMotion.Locked)
				vectorObservation.Add(muscle.TargetNormalizedRotationY);
			if (muscle.ConfigurableJoint.angularZMotion != ConfigurableJointMotion.Locked)
				vectorObservation.Add(muscle.TargetNormalizedRotationZ);
        }        
        return vectorObservation;
    }
    public List<float> GetSensorYPositions()
    {
		var sensorYpositions = Sensors
			.Select(x=> this.GetNormalizedPosition(x.transform.position - startPosition))
			.Select(x=>x.y)
			.ToList();
        return sensorYpositions;
    }
    public void OnCollectObservationsHandleDebug(AgentInfo info)
    {
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
