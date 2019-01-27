using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAgents;

public class OpenAIAntAgent : MarathonAgent
{
    public override void AgentReset()
    {
        base.AgentReset();

        // set to true this to show monitor while training
        Monitor.SetActive(true);

        StepRewardFunction = StepRewardAnt101;
        TerminateFunction = TerminateAnt;
        ObservationsFunction = ObservationsDefault;

        BodyParts["pelvis"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x => x.name == "torso_geom");
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
        Vector3 normalizedFootPosition = this.GetNormalizedPosition(pelvis.transform.position);
        AddVectorObs(normalizedFootPosition.y);

    }

    bool TerminateAnt()
    {
        var pelvis = BodyParts["pelvis"];
		if (pelvis.transform.position.y<0){
			return true;
		}

        var angle = GetForwardBonus("pelvis");
        bool endOnAngle = (angle < .2f);
        return endOnAngle;
    }

    float StepRewardAnt101()
    {
        float velocity = Mathf.Clamp(GetNormalizedVelocity("pelvis").x, 0f, 1f);
        float effort = 1f - GetEffortNormalized();

        velocity *= 0.7f;
        if (velocity >= .3f)
            effort *= 0.3f;
        else
            effort *= velocity;


        var reward = velocity
                     + effort;
        if (ShowMonitor)
        {
            var hist = new[] {reward, velocity}.ToList();
            Monitor.Log("rewardHist", hist.ToArray(), displayType: Monitor.DisplayType.INDEPENDENT);
        }

        return reward;
    }
}