using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;
using static BodyHelper002;

public class MarathonManAgent : Agent, IOnTerrainCollision
{
	BodyManager002 _bodyManager;
	bool _isDone;
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
		sensor.AddVectorObs(_bodyManager.GetBodyPartsObservations());
		sensor.AddVectorObs(_bodyManager.GetMusclesObservations());
		sensor.AddVectorObs(_bodyManager.GetSensorYPositions());
		sensor.AddVectorObs(_bodyManager.GetSensorZPositions());

		// _bodyManager.OnCollectObservationsHandleDebug(GetInfo());
	}

	public override void AgentAction(float[] vectorAction)
	{
		_isDone = false;
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
        var pelvis = _bodyManager.GetFirstBodyPart(BodyPartGroup.Hips);
		if (pelvis.Transform.position.y<0){
			Done();
		}

        var reward = velocity;

		AddReward(reward);
		_bodyManager.SetDebugFrameReward(reward);
	}

	public override void AgentReset()
	{
		if (!_hasLazyInitialized)
		{
			_bodyManager = GetComponent<BodyManager002>();
			_bodyManager.OnInitializeAgent();
			_hasLazyInitialized = true;
		}
		_isDone = true;
		_bodyManager.OnAgentReset();
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
			// case BodyHelper002.BodyPartGroup.LegUpper:
			case BodyHelper002.BodyPartGroup.LegLower:
			case BodyHelper002.BodyPartGroup.Hand:
			// case BodyHelper002.BodyPartGroup.ArmLower:
			// case BodyHelper002.BodyPartGroup.ArmUpper:
				break;
			default:
				// AddReward(-100f);
				if (!_isDone){
					Done();
				}
				break;
		}
	}
}
