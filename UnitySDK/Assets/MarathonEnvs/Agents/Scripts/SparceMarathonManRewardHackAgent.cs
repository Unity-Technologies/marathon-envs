using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;
using static BodyHelper002;

public class SparceMarathonManRewardHackAgent : Agent, IOnTerrainCollision
{
	BodyManager002 _bodyManager;
	public float _heightReward;
	public float _torsoUprightReward;
	public float _torsoForwardReward;
	public float _hipsUprightReward;
	public float _hipsForwardReward;
    RewardHackAgent _rewardHackAgent;

    void Start()
    {
        _rewardHackAgent = GetComponent<RewardHackAgent>();
    }


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
        _heightReward = _bodyManager.GetHeightNormalizedReward(1.2f);
		_torsoUprightReward = _bodyManager.GetUprightNormalizedReward(BodyPartGroup.Torso);
		_torsoForwardReward = _bodyManager.GetDirectionNormalizedReward(BodyPartGroup.Torso, Vector3.forward);
		_hipsUprightReward = _bodyManager.GetUprightNormalizedReward(BodyPartGroup.Hips);
		_hipsForwardReward = _bodyManager.GetDirectionNormalizedReward(BodyPartGroup.Hips, Vector3.forward);
		_torsoUprightReward = Mathf.Clamp(_torsoUprightReward, 0f, 1f);
		_torsoForwardReward = Mathf.Clamp(_torsoForwardReward, 0f, 1f);
		_hipsUprightReward = Mathf.Clamp(_hipsUprightReward, 0f, 1f);
		_hipsForwardReward = Mathf.Clamp(_hipsForwardReward, 0f, 1f);

		var stepCount = GetStepCount() > 0 ? GetStepCount() : 1;
		if ((stepCount >= agentParameters.maxStep)
                && (agentParameters.maxStep > 0))
        {
            AddEpisodeEndReward();
        }
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
					AddEpisodeEndReward();
					Done();
				}
				break;
		}
	}

	void AddEpisodeEndReward()
	{
		var normalizedPosition = _bodyManager.GetNormalizedPosition();
        var endPos = normalizedPosition.x;
        
        var obs = new List<float>();
        obs.Add(endPos);
        obs.Add(_torsoUprightReward);
        obs.Add(_torsoForwardReward);
        obs.Add(_hipsUprightReward);
        obs.Add(_hipsForwardReward);

        var reward = _rewardHackAgent.ScoreObservations(obs, endPos);
		AddReward(reward);
		_bodyManager.SetDebugFrameReward(reward);
	}
}
