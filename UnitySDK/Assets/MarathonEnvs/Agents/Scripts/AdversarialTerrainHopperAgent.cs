using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAgents;
public class AdversarialTerrainHopperAgent : MarathonAgent {

    AdversarialTerrainAgent _adversarialTerrainAgent;
    int _lastXPosInMeters;
    float _pain;
    bool _modeRecover;
    Vector3 _centerOfMass;

    public override void AgentReset()
    {
        base.AgentReset();

        BodyParts["pelvis"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x=>x.name=="torso");
        BodyParts["foot"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x=>x.name=="foot");

        SetCenterOfMass();

        if (_adversarialTerrainAgent == null)
            _adversarialTerrainAgent = GetComponent<AdversarialTerrainAgent>();
        _lastXPosInMeters = (int) BodyParts["foot"].transform.position.x;
        _adversarialTerrainAgent.Terminate(GetCumulativeReward());

        // set to true this to show monitor while training
        Monitor.SetActive(true);

        StepRewardFunction = StepRewardHopper101;
        TerminateFunction = LocalTerminate;
        ObservationsFunction = ObservationsDefault;
        // OnTerminateRewardValue = -100f;
        _pain = 0f;
        _modeRecover = false;

        base.SetupBodyParts();
        SetCenterOfMass();
    }

    bool LocalTerminate()
    {
        int newXPosInMeters = (int) BodyParts["foot"].transform.position.x;
        if (newXPosInMeters > _lastXPosInMeters) {
            _adversarialTerrainAgent.OnNextMeter();
            _lastXPosInMeters = newXPosInMeters;
        }

        SetCenterOfMass();
        var xpos = _centerOfMass.x;
        var terminate = false;
        if (xpos < 4f && _pain > 1f)
            terminate = true;
        else if (xpos < 2f && _pain > 0f)
            terminate = true;
        if (terminate)
            _adversarialTerrainAgent.Terminate(GetCumulativeReward());

        return terminate;
    }
    public override void OnTerrainCollision(GameObject other, GameObject terrain) {
        if (terrain.GetComponent<Terrain>() == null)
            return;

        switch (other.name.ToLowerInvariant().Trim())
        {
            case "thigh": // dm_hopper
            case "pelvis": // dm_hopper
                _pain += 5f;
                NonFootHitTerrain = true;
                _modeRecover = true;
                break;
            case "foot": // dm_hopper
            case "calf": // dm_hopper
                FootHitTerrain = true;
                break;
            default:
                _pain += 5f;
                NonFootHitTerrain = true;
                _modeRecover = true;
                break;
        }
    }


    public override void AgentOnDone()
    {
    }    
    void ObservationsDefault()
    {
        // var pelvis = BodyParts["pelvis"];
        // AddVectorObs(pelvis.velocity);
        // AddVectorObs(pelvis.transform.forward); // gyroscope 
        // AddVectorObs(pelvis.transform.up);
        
        // AddVectorObs(SensorIsInTouch);
        // JointRotations.ForEach(x=>AddVectorObs(x));
        // AddVectorObs(JointVelocity);
        // var foot = BodyParts["foot"];
        // AddVectorObs(foot.transform.position.y);

        var pelvis = BodyParts["pelvis"];
        Vector3 normalizedVelocity = this.GetNormalizedVelocity(pelvis.velocity);
        AddVectorObs(normalizedVelocity);
        AddVectorObs(pelvis.transform.forward); // gyroscope 
        AddVectorObs(pelvis.transform.up);

        AddVectorObs(SensorIsInTouch);
        JointRotations.ForEach(x => AddVectorObs(x));
        AddVectorObs(JointVelocity);
        var foot = BodyParts["foot"];
        Vector3 normalizedFootPosition = this.GetNormalizedPosition(foot.transform.position);
        AddVectorObs(normalizedFootPosition.y);        

        (List<float> distances, float fraction) = 
            _adversarialTerrainAgent.GetDistances2d(
                pelvis.transform.position, ShowMonitor);
   
        AddVectorObs(distances);
        AddVectorObs(fraction);
    }


    void SetCenterOfMass()
    {
        _centerOfMass = Vector3.zero;
        float c = 0f;
        var bodyParts = this.gameObject.GetComponentsInChildren<Rigidbody>();
 
        foreach (var part in bodyParts)
        {
            _centerOfMass += part.worldCenterOfMass * part.mass;
            c += part.mass;
        }
        _centerOfMass /= c;
    }

    float StepRewardHopper101()
    {
        float uprightBonus = GetDirectionBonus("pelvis", Vector3.forward, 1f);
        uprightBonus = Mathf.Clamp(uprightBonus, 0f, 1f);
        float velocity = Mathf.Clamp(GetNormalizedVelocity("pelvis").x, 0f, 1f);
        // float position = Mathf.Clamp(GetNormalizedPosition("pelvis").x, 0f, 1f);
        float effort = 1f - GetEffortNormalized();

        uprightBonus *= 0.05f;
        velocity *= 0.7f;
        if (velocity >= .25f)
            effort *= 0.25f;
        else
            effort *= velocity;

        var reward = velocity
                     + uprightBonus
                     + effort;
        if (ShowMonitor)
        {
            var hist = new[] {reward, velocity, uprightBonus, effort};
            Monitor.Log("rewardHist", hist, displayType: Monitor.DisplayType.INDEPENDENT);
        }

        _pain = 0f;
        return reward;
    }
}
