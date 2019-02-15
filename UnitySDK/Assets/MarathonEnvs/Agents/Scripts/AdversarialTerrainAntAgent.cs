using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAgents;
public class AdversarialTerrainAntAgent : MarathonAgent {

    AdversarialTerrainAgent _adversarialTerrainAgent;
    int _lastXPosInMeters;
    float _pain;
    bool _modeRecover;
    Vector3 _centerOfMass;

    public override void AgentReset()
    {
        base.AgentReset();

        BodyParts["pelvis"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x => x.name == "torso_geom");

        SetCenterOfMass();

        if (_adversarialTerrainAgent == null)
            _adversarialTerrainAgent = GetComponent<AdversarialTerrainAgent>();
        _lastXPosInMeters = (int) BodyParts["pelvis"].transform.position.x;
        _adversarialTerrainAgent.Terminate(GetCumulativeReward());

        // set to true this to show monitor while training
        Monitor.SetActive(true);

        StepRewardFunction = StepRewardAnt101;
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
        int newXPosInMeters = (int) BodyParts["pelvis"].transform.position.x;
        if (newXPosInMeters > _lastXPosInMeters) {
            _adversarialTerrainAgent.OnNextMeter();
            _lastXPosInMeters = newXPosInMeters;
        }

        SetCenterOfMass();
        var xpos = _centerOfMass.x;
        var terminate = false;
		if (_adversarialTerrainAgent.IsPointOffEdge(BodyParts["pelvis"].transform.position))
            terminate = true;
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
            case "pelvis": // dm_hopper
                _pain += 5f;
                NonFootHitTerrain = true;
                _modeRecover = true;
                break;
            case "left_ankle_geom": // oai_ant
            case "right_ankle_geom": // oai_ant
            case "third_ankle_geom": // oai_ant
            case "fourth_ankle_geom": // oai_ant
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
        var pelvis = BodyParts["pelvis"];
        Vector3 normalizedVelocity = GetNormalizedVelocity(pelvis.velocity);
        AddVectorObs(normalizedVelocity);
        AddVectorObs(pelvis.transform.forward); // gyroscope 
        AddVectorObs(pelvis.transform.up);

        AddVectorObs(SensorIsInTouch);
        JointRotations.ForEach(x => AddVectorObs(x));
        AddVectorObs(JointVelocity);
        // Vector3 normalizedFootPosition = this.GetNormalizedPosition(pelvis.transform.position);
        // AddVectorObs(normalizedFootPosition.y);

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

    float StepRewardAnt101()
    {
        float velocity = Mathf.Clamp(GetNormalizedVelocity("pelvis").x, 0f, 1f);
        float effort = 1f - GetEffortNormalized();

        velocity *= 0.7f;
        if (velocity >= .25f)
            effort *= 0.25f;
        else
            effort *= velocity;

        var reward = velocity
                     + effort;
        if (ShowMonitor)
        {
            var hist = new[] {reward, velocity, effort};
            Monitor.Log("rewardHist", hist, displayType: Monitor.DisplayType.INDEPENDENT);
        }

        _pain = 0f;
        return reward;
    }
}
