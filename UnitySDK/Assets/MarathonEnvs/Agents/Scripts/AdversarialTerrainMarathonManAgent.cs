using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;
using static BodyHelper002;

public class AdversarialTerrainMarathonManAgent : Agent, IOnTerrainCollision
{
	BodyManager002 _bodyManager;

    AdversarialTerrainAgent _adversarialTerrainAgent;
	SpawnableEnv _spawnableEnv;
    public int lastXPosInMeters;
    float _pain;
    bool _modeRecover;

	List<float> distances;
	float fraction;

	override public void CollectObservations()
	{
		Vector3 normalizedVelocity = _bodyManager.GetNormalizedVelocity();
        var pelvis = _bodyManager.GetFirstBodyPart(BodyPartGroup.Hips);
        var shoulders = _bodyManager.GetFirstBodyPart(BodyPartGroup.Torso);

        AddVectorObs(normalizedVelocity); 
        AddVectorObs(pelvis.Rigidbody.transform.forward); // gyroscope 
        AddVectorObs(pelvis.Rigidbody.transform.up);

        AddVectorObs(shoulders.Rigidbody.transform.forward); // gyroscope 
        AddVectorObs(shoulders.Rigidbody.transform.up);

		AddVectorObs(_bodyManager.GetSensorIsInTouch());
		AddVectorObs(_bodyManager.GetBodyPartsObservations());
		AddVectorObs(_bodyManager.GetMusclesObservations());
		// AddVectorObs(_bodyManager.GetSensorYPositions());
		var sensors = _bodyManager.Sensors;
		var sensorsPos = sensors.Select(x=>x.transform.position);
		var senorHeights = _adversarialTerrainAgent.GetDistances2d(sensorsPos);
		AddVectorObs(senorHeights);
		AddVectorObs(_bodyManager.GetSensorZPositions());
        
        (distances, fraction) = 
            _adversarialTerrainAgent.GetDistances2d(
                pelvis.Rigidbody.transform.position, _bodyManager.ShowMonitor);
    
        AddVectorObs(distances);
        AddVectorObs(fraction);
		_bodyManager.OnCollectObservationsHandleDebug(GetInfo());
	}

	public override void AgentAction(float[] vectorAction, string textAction)
	{
		// apply actions to body
		_bodyManager.OnAgentAction(vectorAction, textAction);

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
            _adversarialTerrainAgent.OnNextMeter();
            lastXPosInMeters = newXPosInMeters;
        }
        var terminate = false;
		// bool isInBounds = _spawnableEnv.IsPointWithinBoundsInWorldSpace(pelvis.Transform.position);
		// if (!isInBounds)
        // if (pelvis.Rigidbody.transform.position.y < 0f)
		if (_adversarialTerrainAgent.IsPointOffEdge(pelvis.Transform.position))
            terminate = true;
        if (xpos < 4f && _pain > 1f)
            terminate = true;
        else if (xpos < 2f && _pain > 0f)
            terminate = true;
		else if (_pain > 2f)
            terminate = true;
        if (terminate){
			Done();
		}
        _pain = 0f;
        _modeRecover = false;
	}


	public override void AgentReset()
	{
		if (_bodyManager == null)
			_bodyManager = GetComponent<BodyManager002>();
		_bodyManager.OnAgentReset();
        if (_adversarialTerrainAgent == null)
            _adversarialTerrainAgent = GetComponent<AdversarialTerrainAgent>();
		if (_spawnableEnv == null)
			_spawnableEnv = GetComponentInParent<SpawnableEnv>();
        _adversarialTerrainAgent.Terminate(GetCumulativeReward());
		lastXPosInMeters = (int)
            _bodyManager.GetBodyParts(BodyPartGroup.Foot)
            .Average(x=>x.Transform.position.x);
        _pain = 0f;
        _modeRecover = false;
	}
	public virtual void OnTerrainCollision(GameObject other, GameObject terrain)
	{
		// if (string.Compare(terrain.name, "Terrain", true) != 0)
		if (terrain.GetComponent<Terrain>() == null)
			return;
		// if (!_styleAnimator.AnimationStepsReady)
		// 	return;
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
                _modeRecover = true;
				break;
			default:
				// AddReward(-100f);
				_pain += 5f;
                _modeRecover = true;
				break;
		}
	}
}
