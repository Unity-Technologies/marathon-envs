using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;

public class RewardHackAgent : Agent
{
    public int NumActions;
    public int NumSteps;
    public List<float> Actions;
    public List<float> SoftMaxActions;

    int _curStep;

    void Start()
    {
    }
    void Init()
    {
        var info = GetInfo();
        var numActions = info.vectorObservation.Count;
        Actions = Enumerable.Range(0, numActions).Select(x => 0f).ToList();
        SoftMaxActions = Enumerable.Range(0, numActions).Select(x => 0f).ToList();
        _curStep = 0;
    }

	override public void CollectObservations()
    {
        if (Actions == null) {
            Init();
        }

        AddVectorObs(SoftMaxActions); 
    }

	public override void AgentAction(float[] vectorAction, string textAction)
    {
        if (Actions == null) {
            Init();
        }
        var softmaxActions = SoftMax(vectorAction).ToArray();;
        for (int i = 0; i < vectorAction.Length; i++)
        {
            Actions[i] = vectorAction[i];
            SoftMaxActions[i] = softmaxActions[i];
        }
    }

    public float ScoreObservations(List<float> observations, float targetReward)
    {
        if (Actions == null) {
            return 0f;
        }

        var scoredObs = observations.Zip(SoftMaxActions, (x,y)=>(x*y));
        var reward = scoredObs.Sum();
        RecordReward(targetReward);
        return reward;
    }

    void RecordReward(float reward)
    {
        AddReward(reward);
        _curStep++;
        if (_curStep >= NumSteps)
        {
            RequestDecision();
            _curStep = 0;
        }
    }

    IEnumerable<float> SoftMax(IEnumerable<float> vector)
    {
        var z_exp = vector.Select(Mathf.Exp);
    	var sum_z_exp = z_exp.Sum();
    	var softmax = z_exp.Select(i => i / sum_z_exp);
        return softmax;
    }
}
