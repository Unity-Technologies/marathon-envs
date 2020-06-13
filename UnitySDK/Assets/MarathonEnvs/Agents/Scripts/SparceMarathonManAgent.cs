using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;
using static BodyHelper002;
using System;

public class RollingAverage
{
	List<double> _window;
	int _size;
	int _count;
	double _sum;
	double _sumOfSquares;
	public double Mean;
	public double StandardDeviation;

	public RollingAverage(int size)
	{
		_window = new List<double>(size);
		_size = size;
		_count = 0;
		_sum = 0;
		_sumOfSquares = 0;
	}
	public double Normalize(double val)
	{
		Add(val);
		double normalized = val;
		if (StandardDeviation != 0) 
			normalized = (val - Mean) / StandardDeviation;
		return normalized;
	}
	void Add (double val)
	{
		if (_count >= _size)
		{
			var removedVal = _window[0];
			_window.RemoveAt(0);
			_count--;
			_sum -= removedVal;
			_sumOfSquares -= removedVal * removedVal;
		}
		_window.Add(val);
		_count++;
		_sum += val;
		_sumOfSquares += val * val;
		// set Mean to Sum / Count, 
		Mean = _sum / _count;
		// set StandardDeviation to Math.Sqrt(SumOfSquares / Count - Mean * Mean).
		StandardDeviation = Math.Sqrt(_sumOfSquares / _count - Mean * Mean);
	}
}

public class SparceMarathonManAgent : Agent, IOnTerrainCollision
{
	BodyManager002 _bodyManager;
	public float _heightReward;
	public float _torsoUprightReward;
	public float _torsoForwardReward;
	public float _hipsUprightReward;
	public float _hipsForwardReward;
	public float _notAtLimitBonus;
	public float _reducedPowerBonus;
	public float _episodeMaxDistance;

	static RollingAverage rollingAverage;

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
		foreach (var bodyPart in _bodyManager.BodyParts)
		{
			bodyPart.UpdateObservations();
			sensor.AddVectorObs(bodyPart.ObsLocalPosition);
			sensor.AddVectorObs(bodyPart.ObsRotation);
			sensor.AddVectorObs(bodyPart.ObsRotationVelocity);
			sensor.AddVectorObs(bodyPart.ObsVelocity);
		}
		sensor.AddVectorObs(_bodyManager.GetSensorObservations());

		sensor.AddVectorObs(_notAtLimitBonus);
		sensor.AddVectorObs(_reducedPowerBonus);
		// _bodyManager.OnCollectObservationsHandleDebug(GetInfo());
	}

	public override void AgentAction(float[] vectorAction)
	{
		if (!_hasLazyInitialized)
		{
			return;
		}
		_isDone = false;
		// apply actions to body
		_bodyManager.OnAgentAction(vectorAction);

		// manage reward
		var actionDifference = _bodyManager.GetActionDifference();
		var actionsAbsolute = vectorAction.Select(x=>Mathf.Abs(x)).ToList();
		var actionsAtLimit = actionsAbsolute.Select(x=> x>=1f ? 1f : 0f).ToList();
		float actionaAtLimitCount = actionsAtLimit.Sum();
        _notAtLimitBonus = 1f - (actionaAtLimitCount / (float) actionsAbsolute.Count);
        _reducedPowerBonus = 1f - actionsAbsolute.Average();
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
		if ((stepCount >= maxStep)
                && (maxStep > 0))
        {
            AddEpisodeEndReward();
        }
		else{
			var pelvis = _bodyManager.GetFirstBodyPart(BodyPartGroup.Hips);
			if (pelvis.Transform.position.y<0){
	            AddEpisodeEndReward();
				Done();
			}
		}
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
		_isDone = true;
		_bodyManager.OnAgentReset();
		_episodeMaxDistance = 0f;
		if (rollingAverage == null)
			rollingAverage = new RollingAverage(100);
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
			case BodyHelper002.BodyPartGroup.Foot:
				_episodeMaxDistance = _bodyManager.GetNormalizedPosition().x;
				break;
			case BodyHelper002.BodyPartGroup.None:
			// case BodyHelper002.BodyPartGroup.LegUpper:
			case BodyHelper002.BodyPartGroup.LegLower:
			case BodyHelper002.BodyPartGroup.Hand:
			// case BodyHelper002.BodyPartGroup.ArmLower:
			// case BodyHelper002.BodyPartGroup.ArmUpper:
				break;
			default:
				// AddReward(-100f);
				if (!_isDone){
					AddEpisodeEndReward();
					Done();
				}
				break;
		}
	}

	void AddEpisodeEndReward()
	{
		var reward = _episodeMaxDistance;

		AddReward(reward);
		_bodyManager.SetDebugFrameReward(reward);

		// # normalized reward
		// float normalizedReward = (float)rollingAverage.Normalize(reward);
		// normalizedReward += (float)rollingAverage.Mean;
		// AddReward(normalizedReward);
		// _bodyManager.SetDebugFrameReward(normalizedReward);
	}
}
