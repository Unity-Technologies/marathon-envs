using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAgents;

public class DeepMindWalkerAgent : MarathonAgent
{
    public override void AgentReset()
    {
        base.AgentReset();

        // set to true this to show monitor while training
        Monitor.SetActive(true);

        StepRewardFunction = StepRewardWalker106;
        TerminateFunction = TerminateOnNonFootHitTerrain;
        ObservationsFunction = ObservationsDefault;

        BodyParts["pelvis"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x => x.name == "torso");
        BodyParts["left_thigh"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x => x.name == "left_thigh");
        BodyParts["right_thigh"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x => x.name == "right_thigh");
        BodyParts["right_foot"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x => x.name == "right_foot");
        BodyParts["left_foot"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x => x.name == "left_foot");
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
        Vector3 normalizedVelocity = this.GetNormalizedVelocity(pelvis.velocity);
        AddVectorObs(normalizedVelocity);
        AddVectorObs(pelvis.transform.forward); // gyroscope 
        AddVectorObs(pelvis.transform.up);

        AddVectorObs(SensorIsInTouch);
        JointRotations.ForEach(x => AddVectorObs(x));
        AddVectorObs(JointVelocity);
        AddVectorObs(new []{
            this.GetNormalizedPosition(BodyParts["left_foot"].transform.position).y,
            this.GetNormalizedPosition(BodyParts["right_foot"].transform.position).y
        });
    }

    float StepRewardWalker106()
    {
        float heightPenality = 1f-GetHeightPenality(1.1f);
        heightPenality = Mathf.Clamp(heightPenality, 0f, 1f);
        float uprightBonus = GetDirectionBonus("pelvis", Vector3.forward, 1f);
        uprightBonus = Mathf.Clamp(uprightBonus, 0f, 1f);
        float velocity = Mathf.Clamp(GetNormalizedVelocity("pelvis").x, 0f, 1f);
        float effort = 1f - GetEffortNormalized();

        if (ShowMonitor)
        {
            var hist = new[] {velocity, uprightBonus, heightPenality, effort}.ToList();
            Monitor.Log("rewardHist", hist.ToArray(), displayType: Monitor.DisplayType.INDEPENDENT);
        }

        heightPenality *= 0.05f;
        uprightBonus *= 0.05f;
        velocity *= 0.4f;
        if (velocity >= .4f)
            effort *= 0.4f;
        else
            effort *= velocity;

        var reward = velocity
                     + uprightBonus
                     + heightPenality
                     + effort;

        return reward;
    }
}