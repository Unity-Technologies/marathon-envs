using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAgents;

public class DeepMindHopperAgent : MarathonAgent
{

    public List<float> RewardHackingVector;

    public override void AgentReset()
    {
        base.AgentReset();

        // set to true this to show monitor while training
        Monitor.SetActive(true);

        StepRewardFunction = StepRewardHopper101;
        TerminateFunction = TerminateOnNonFootHitTerrain;
        ObservationsFunction = ObservationsDefault;

        BodyParts["pelvis"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x => x.name == "torso");
        BodyParts["foot"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x => x.name == "foot");
        SetupBodyParts();
    }


    public override void AgentOnDone()
    {
    }

    void ObservationsDefault()
    {
        if (ShowMonitor)
        {
        }

        var pelvis = BodyParts["pelvis"];
        Vector3 normalizedVelocity = GetNormalizedVelocity(pelvis.velocity);
        AddVectorObs(normalizedVelocity);
        AddVectorObs(pelvis.transform.forward); // gyroscope 
        AddVectorObs(pelvis.transform.up);

        AddVectorObs(SensorIsInTouch);
        JointRotations.ForEach(x => AddVectorObs(x));
        AddVectorObs(JointVelocity);
        var foot = BodyParts["foot"];
        Vector3 normalizedFootPosition = GetNormalizedPosition(foot.transform.position);
        AddVectorObs(normalizedFootPosition.y);
    }

    float GetRewardOnEpisodeComplete()
    {
        return FocalPoint.transform.position.x;
    }

    void UpdateRewardHackingVector()
    {
        // float uprightBonus = GetForwardBonus("pelvis");
        float uprightBonus = GetDirectionBonus("pelvis", Vector3.forward, 1f);
        uprightBonus = Mathf.Clamp(uprightBonus, 0f, 1f);
        float velocity = Mathf.Clamp(GetNormalizedVelocity("pelvis").x, 0f, 1f);
        float position = Mathf.Clamp(GetNormalizedPosition("pelvis").x, 0f, 1f);
        float effort = 1f - GetEffortNormalized();

        if (RewardHackingVector?.Count == 0)
            RewardHackingVector = Enumerable.Range(0, 6).Select(x => 0f).ToList();
        RewardHackingVector[0] = velocity;
        RewardHackingVector[1] = position;
        RewardHackingVector[2] = effort;
        RewardHackingVector[3] = uprightBonus;
    }

    float StepRewardHopper101()
    {
        UpdateRewardHackingVector();
        float uprightBonus = GetDirectionBonus("pelvis", Vector3.forward, 1f);
        uprightBonus = Mathf.Clamp(uprightBonus, 0f, 1f);
        float velocity = Mathf.Clamp(GetNormalizedVelocity("pelvis").x, 0f, 1f);
        // float position = Mathf.Clamp(GetNormalizedPosition("pelvis").x, 0f, 1f);
        float effort = 1f - GetEffortNormalized();

        uprightBonus *= 0.05f;
        velocity *= 0.7f;
        effort *= 0.25f;
        // uprightBonus *= 0f;
        // velocity *= .7f;
        // effort *= 0.3f;

        var reward = velocity
                     + uprightBonus
                     + effort;
        if (ShowMonitor)
        {
            var hist = new[] {reward, velocity, uprightBonus, effort}.ToList();
            Monitor.Log("rewardHist", hist.ToArray());
        }

        return reward;
    }
    float OldStepRewardHopper101()
    {
        UpdateRewardHackingVector();
        float uprightBonus = GetForwardBonus("pelvis");
        float velocity = GetVelocity("pelvis");
        float effort = GetEffort();
        var effortPenality = 3e-1f * (float) effort;
        var jointsAtLimitPenality = GetJointsAtLimitPenality() * 4;

        var reward = velocity
                     + uprightBonus
                     - effortPenality
                     - jointsAtLimitPenality;
        if (ShowMonitor)
        {
            var hist = new[] {reward, velocity, uprightBonus, -effortPenality, -jointsAtLimitPenality}.ToList();
            Monitor.Log("rewardHist", hist.ToArray());
        }

        return reward;
    }
}