using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAgents;
public class TerrainHopperAgent : MarathonAgent {

    TerrainGenerator _terrainGenerator;
    int _lastXPosInMeters;
    int _stepCountAtLastMeter;
    float _pain;
    bool _modeRecover;
    Vector3 _centerOfMass;

    public override void AgentReset()
    {
        base.AgentReset();

        BodyParts["pelvis"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x=>x.name=="torso");
        BodyParts["foot"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x=>x.name=="foot");

        SetCenterOfMass();

        if (_terrainGenerator == null)
            _terrainGenerator = GetComponent<TerrainGenerator>();
        _lastXPosInMeters = (int) BodyParts["foot"].transform.position.x;
        _terrainGenerator.Reset();
        _stepCountAtLastMeter = 0;

        // set to true this to show monitor while training
        Monitor.SetActive(true);

        StepRewardFunction = StepRewardHopper101;
        TerminateFunction = LocalTerminate;
        ObservationsFunction = ObservationsDefault;
        OnTerminateRewardValue = 0f;
        _pain = 0f;
        _modeRecover = false;

        base.SetupBodyParts();
        SetCenterOfMass();
    }

    bool LocalTerminate()
    {
        int newXPosInMeters = (int) BodyParts["foot"].transform.position.x;
        if (newXPosInMeters > _lastXPosInMeters) {
            _lastXPosInMeters = newXPosInMeters;
            _stepCountAtLastMeter = this.GetStepCount();
        }

        SetCenterOfMass();
        var xpos = _centerOfMass.x;
        var terminate = false;
        if (this.GetStepCount()-_stepCountAtLastMeter >= (100*5))
            terminate = true;
        else if (xpos < 2f && _pain > 0f)
            terminate = true;
        else if (_pain > 1f)
            terminate = true;

        return terminate;
    }
    public override void OnTerrainCollision(GameObject other, GameObject terrain) {
        if (terrain.GetComponent<Terrain>() == null)
            return;

        switch (other.name.ToLowerInvariant().Trim())
        {
            // case "torso": // dm_hopper
            //     _pain += 5f;
            //     NonFootHitTerrain = true;
            //     _modeRecover = true;
            //     break;
            case "foot": // dm_hopper
            case "calf": // dm_hopper
                FootHitTerrain = true;
                break;
            default:
            case "thigh": // dm_hopper
            case "pelvis": // dm_hopper
            case "torso": // dm_hopper
                _pain += .5f;
                NonFootHitTerrain = true;
                _modeRecover = true;
                break;
        }
    }
  
    void ObservationsDefault()
    {
        var sensor = this;
        // var pelvis = BodyParts["pelvis"];
        // sensor.AddVectorObs(pelvis.velocity);
        // sensor.AddVectorObs(pelvis.transform.forward); // gyroscope 
        // sensor.AddVectorObs(pelvis.transform.up);

        // sensor.AddVectorObs(SensorIsInTouch);
        // JointRotations.ForEach(x=>sensor.AddVectorObs(x));
        // sensor.AddVectorObs(JointVelocity);
        // var foot = BodyParts["foot"];
        // sensor.AddVectorObs(foot.transform.position.y);

        var pelvis = BodyParts["pelvis"];
        Vector3 normalizedVelocity = this.GetNormalizedVelocity(pelvis.velocity);
        sensor.AddVectorObs(normalizedVelocity);
        sensor.AddVectorObs(pelvis.transform.forward); // gyroscope 
        sensor.AddVectorObs(pelvis.transform.up);

        sensor.AddVectorObs(SensorIsInTouch);
        JointRotations.ForEach(x => sensor.AddVectorObs(x));
        sensor.AddVectorObs(JointVelocity);
        var foot = BodyParts["foot"];
        Vector3 normalizedFootPosition = this.GetNormalizedPosition(foot.transform.position);
        sensor.AddVectorObs(normalizedFootPosition.y);        

        (List<float> distances, float fraction) = 
            _terrainGenerator.GetDistances2d(
                pelvis.transform.position, ShowMonitor);
   
        sensor.AddVectorObs(distances);
        sensor.AddVectorObs(fraction);
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

        // uprightBonus *= 0.05f;
        // velocity *= 0.7f;
        // if (velocity >= .25f)
        //     effort *= 0.25f;
        // else
        //     effort *= velocity;

        // var reward = velocity
        //              + uprightBonus
        //              + effort;
        // if (ShowMonitor)
        // {
        //     var hist = new[] {reward, velocity, uprightBonus, effort};
        //     Monitor.Log("rewardHist", hist, displayType: Monitor.DisplayType.Independent);
        // }
        var reward = velocity;

        _pain = 0f;
        return reward;
    }
}
