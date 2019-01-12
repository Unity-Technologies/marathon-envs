using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;
using static BodyHelper002;

public class MarathonManAgent : Agent, IOnTerrainCollision
{
	BodyManager002 _bodyManager;

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
		AddVectorObs(_bodyManager.GetSensorYPositions());

		_bodyManager.OnCollectObservationsHandleDebug(GetInfo());
	}

	public override void AgentAction(float[] vectorAction, string textAction)
	{
		// apply actions to body
		_bodyManager.OnAgentAction(vectorAction, textAction);

		// manage reward
        // float heightPenality = 1f-GetHeightPenality(1.2f);
        // heightPenality = Mathf.Clamp(heightPenality, 0f, 1f);
        // float uprightBonus = GetDirectionBonus("pelvis", Vector3.forward, 1f);
        // uprightBonus = Mathf.Clamp(uprightBonus, 0f, 1f);
        float velocity = Mathf.Clamp(_bodyManager.GetNormalizedVelocity().x, 0f, 1f);
        // float effort = 1f - _bodyManager.GetEffortNormalized();
        // heightPenality *= 0.05f;
        // uprightBonus *= 0.05f;
		var actionDifference = _bodyManager.GetActionDifference();
        // velocity *= 0.5f;
        // if (velocity >= .5f)
        //     actionDifference *= 0.5f;
        // else
        //     actionDifference *= velocity;

        // var reward = velocity
        //             //  + uprightBonus
        //             //  + heightPenality
        //              + actionDifference;		
        var reward = velocity;
		AddReward(reward);
		_bodyManager.SetDebugFrameReward(reward);
	}


	public override void AgentReset()
	{
		if (_bodyManager == null)
			_bodyManager = GetComponent<BodyManager002>();
		_bodyManager.OnAgentReset();
	}
	public virtual void OnTerrainCollision(GameObject other, GameObject terrain)
	{
		if (string.Compare(terrain.name, "Terrain", true) != 0)
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
				if (!IsDone()){
					Done();
				}
				break;
		}
	}
}
