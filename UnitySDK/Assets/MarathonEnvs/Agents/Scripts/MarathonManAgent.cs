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

	// override public void CollectObservations(VectorSensor sensor)
	// {
	// 	// sensor.AddObservation(ObsPhase);
	// 	foreach (var bodyPart in BodyParts)
	// 	{
	// 		bodyPart.UpdateObservations();
	// 		sensor.AddObservation(bodyPart.ObsLocalPosition);
	// 		sensor.AddObservation(bodyPart.ObsRotation);
	// 		sensor.AddObservation(bodyPart.ObsRotationVelocity);
	// 		sensor.AddObservation(bodyPart.ObsVelocity);
	// 	}
	// 	foreach (var muscle in Muscles)
	// 	{
	// 		muscle.UpdateObservations();
	// 		if (muscle.ConfigurableJoint.angularXMotion != ConfigurableJointMotion.Locked)
	// 			sensor.AddObservation(muscle.TargetNormalizedRotationX);
	// 		if (muscle.ConfigurableJoint.angularYMotion != ConfigurableJointMotion.Locked)
	// 			sensor.AddObservation(muscle.TargetNormalizedRotationY);
	// 		if (muscle.ConfigurableJoint.angularZMotion != ConfigurableJointMotion.Locked)
	// 			sensor.AddObservation(muscle.TargetNormalizedRotationZ);
	// 	}

	// 	// sensor.AddObservation(ObsCenterOfMass);
	// 	// sensor.AddObservation(ObsVelocity);
	// 	sensor.AddObservation(SensorIsInTouch);
	// }
	override public void CollectObservations(VectorSensor sensor)
	{
		Vector3 normalizedVelocity = _bodyManager.GetNormalizedVelocity();
        var pelvis = _bodyManager.GetFirstBodyPart(BodyPartGroup.Hips);
        var shoulders = _bodyManager.GetFirstBodyPart(BodyPartGroup.Torso);

        sensor.AddObservation(normalizedVelocity); 
        sensor.AddObservation(pelvis.Rigidbody.transform.forward); // gyroscope 
        sensor.AddObservation(pelvis.Rigidbody.transform.up);

        sensor.AddObservation(shoulders.Rigidbody.transform.forward); // gyroscope 
        sensor.AddObservation(shoulders.Rigidbody.transform.up);

		sensor.AddObservation(_bodyManager.GetSensorIsInTouch());
		sensor.AddObservation(_bodyManager.GetBodyPartsObservations());
		sensor.AddObservation(_bodyManager.GetMusclesObservations());
		sensor.AddObservation(_bodyManager.GetSensorYPositions());
		sensor.AddObservation(_bodyManager.GetSensorZPositions());

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

	public override void InitializeAgent()
	{
		if (_bodyManager == null)
			_bodyManager = GetComponent<BodyManager002>();
		_bodyManager.OnInitializeAgent();
		AgentReset();
    }

	public override void AgentReset()
	{
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
