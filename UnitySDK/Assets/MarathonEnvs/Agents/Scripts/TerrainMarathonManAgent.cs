using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;
using static BodyHelper002;

public class TerrainMarathonManAgent : Agent, IOnTerrainCollision
{
	BodyManager002 _bodyManager;

    TerrainGenerator _terrainGenerator;
	SpawnableEnv _spawnableEnv;
    int _stepCountAtLastMeter;
    public int lastXPosInMeters;
    public int maxXPosInMeters;
	float _pain;

	List<float> distances;
	float fraction;
	bool _hasLazyInitialized;

	override public void CollectObservations()
	{
		var sensor = this;
		if (!_hasLazyInitialized)
		{
			AgentReset();
		}

		Vector3 normalizedVelocity = _bodyManager.GetNormalizedVelocity();
        var pelvis = _bodyManager.GetFirstBodyPart(BodyPartGroup.Hips);
        var shoulders = _bodyManager.GetFirstBodyPart(BodyPartGroup.Torso);

        sensor.AddVectorObs(normalizedVelocity); 
        sensor.AddVectorObs(pelvis.Rigidbody.transform.forward); // gyroscope 
        sensor.AddVectorObs(pelvis.Rigidbody.transform.up);

        sensor.AddVectorObs(shoulders.Rigidbody.transform.forward); // gyroscope 
        sensor.AddVectorObs(shoulders.Rigidbody.transform.up);

		sensor.AddVectorObs(_bodyManager.GetSensorIsInTouch());
		foreach (var bodyPart in _bodyManager.BodyParts)
		{
			bodyPart.UpdateObservations();
			sensor.AddVectorObs(bodyPart.ObsLocalPosition);
			sensor.AddVectorObs(bodyPart.ObsRotation);
			sensor.AddVectorObs(bodyPart.ObsRotationVelocity);
			sensor.AddVectorObs(bodyPart.ObsVelocity);
		}
		sensor.AddVectorObs(_bodyManager.GetSensorObservations());
        
        (distances, fraction) = 
            _terrainGenerator.GetDistances2d(
                pelvis.Rigidbody.transform.position, _bodyManager.ShowMonitor);
    
        sensor.AddVectorObs(distances);
        sensor.AddVectorObs(fraction);
		// _bodyManager.OnCollectObservationsHandleDebug(GetInfo());
	}

	public override void AgentAction(float[] vectorAction)
	{
		if (!_hasLazyInitialized)
		{
			return;
		}
		// apply actions to body
		_bodyManager.OnAgentAction(vectorAction);

		// manage reward
        float velocity = Mathf.Clamp(_bodyManager.GetNormalizedVelocity().x, 0f, 1f);
		var actionDifference = _bodyManager.GetActionDifference();
		var actionsAbsolute = vectorAction.Select(x=>Mathf.Abs(x)).ToList();
		var actionsAtLimit = actionsAbsolute.Select(x=> x>=1f ? 1f : 0f).ToList();
		float actionaAtLimitCount = actionsAtLimit.Sum();
        float notAtLimitBonus = 1f - (actionaAtLimitCount / (float) actionsAbsolute.Count);
        float reducedPowerBonus = 1f - actionsAbsolute.Average();

		// velocity *= 0.85f;
		// reducedPowerBonus *=0f;
		// notAtLimitBonus *=.1f;
		// actionDifference *=.05f;
        // var reward = velocity
		// 				+ notAtLimitBonus
		// 				+ reducedPowerBonus
		// 				+ actionDifference;		
        var reward = velocity;
		AddReward(reward);
		_bodyManager.SetDebugFrameReward(reward);

        var pelvis = _bodyManager.GetFirstBodyPart(BodyPartGroup.Hips);
		float xpos = 
            _bodyManager.GetBodyParts(BodyPartGroup.Foot)
            .Average(x=>x.Transform.position.x);
		int newXPosInMeters = (int) xpos;
        if (newXPosInMeters > lastXPosInMeters) {
            lastXPosInMeters = newXPosInMeters;
            _stepCountAtLastMeter = this.GetStepCount();
        }
		if (newXPosInMeters > maxXPosInMeters)
			maxXPosInMeters = newXPosInMeters;
		var terminate = false;
		// bool isInBounds = _spawnableEnv.IsPointWithinBoundsInWorldSpace(pelvis.Transform.position);
		// if (!isInBounds)
        // if (pelvis.Rigidbody.transform.position.y < 0f)
		if (_terrainGenerator.IsPointOffEdge(pelvis.Transform.position)){
            terminate = true;
            AddReward(-1f);
		}
        if (this.GetStepCount()-_stepCountAtLastMeter >= (200*5))
            terminate = true;
		else if (xpos < 4f && _pain > 1f)
            terminate = true;
        else if (xpos < 2f && _pain > 0f)
            terminate = true;
		else if (_pain > 2f)
            terminate = true;
        if (terminate){
			Done();
		}
        _pain = 0f;
	}

	public override void AgentReset()
	{
		if (!_hasLazyInitialized)
		{
			_bodyManager = GetComponent<BodyManager002>();
			_bodyManager.BodyConfig = MarathonManAgent.BodyConfig;
			_bodyManager.OnInitializeAgent();
			_hasLazyInitialized = true;
		}

		if (_bodyManager == null)
			_bodyManager = GetComponent<BodyManager002>();
		_bodyManager.OnAgentReset();
        if (_terrainGenerator == null)
            _terrainGenerator = GetComponent<TerrainGenerator>();
		if (_spawnableEnv == null)
			_spawnableEnv = GetComponentInParent<SpawnableEnv>();
        _terrainGenerator.Reset();
		lastXPosInMeters = (int)
            _bodyManager.GetBodyParts(BodyPartGroup.Foot)
            .Average(x=>x.Transform.position.x);
        _pain = 0f;
	}
	public virtual void OnTerrainCollision(GameObject other, GameObject terrain)
	{
		// if (string.Compare(terrain.name, "Terrain", true) != 0)
		if (terrain.GetComponent<Terrain>() == null)
			return;
		// if (!_styleAnimator.AnimationStepsReady)
		// 	return;
        // HACK - for when agent has not been initialized
		if (_bodyManager == null)
			return;
		var bodyPart = _bodyManager.BodyParts.FirstOrDefault(x=>x.Transform.gameObject == other);
		if (bodyPart == null)
			return;
		switch (bodyPart.Group)
		{
			case BodyHelper002.BodyPartGroup.None:
			case BodyHelper002.BodyPartGroup.Foot:
			case BodyHelper002.BodyPartGroup.LegLower:
				break;
			case BodyHelper002.BodyPartGroup.LegUpper:
			case BodyHelper002.BodyPartGroup.Hand:
			case BodyHelper002.BodyPartGroup.ArmLower:
			case BodyHelper002.BodyPartGroup.ArmUpper:
				_pain += .1f;
				break;
			default:
				// AddReward(-100f);
				_pain += 5f;
				break;
		}
	}
}
