using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAgents;
public class TerrainAntAgent : MarathonAgent {

    TerrainGenerator _terrainGenerator;
    int _lastXPosInMeters;
    int _stepCountAtLastMeter;
    float _pain;
    Vector3 _centerOfMass;

    public override void AgentReset()
    {
        base.AgentReset();

        BodyParts["pelvis"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x => x.name == "torso_geom");

        SetCenterOfMass();

        if (_terrainGenerator == null)
            _terrainGenerator = GetComponent<TerrainGenerator>();
        _lastXPosInMeters = (int) BodyParts["pelvis"].transform.position.x;
        _terrainGenerator.Reset();

        // set to true this to show monitor while training
        //Monitor.SetActive(true);

        StepRewardFunction = StepRewardAnt101;
        TerminateFunction = LocalTerminate;
        ObservationsFunction = ObservationsDefault;
        OnTerminateRewardValue = 0f;
        // OnTerminateRewardValue = -100f;
        _pain = 0f;

        base.SetupBodyParts();
        SetCenterOfMass();
    }

    bool LocalTerminate()
    {
        int newXPosInMeters = (int) BodyParts["pelvis"].transform.position.x;
        if (newXPosInMeters > _lastXPosInMeters) {
            _lastXPosInMeters = newXPosInMeters;
            _stepCountAtLastMeter = this.GetStepCount();
        }

        SetCenterOfMass();
        var xpos = _centerOfMass.x;
        var terminate = false;
		if (_terrainGenerator.IsPointOffEdge(BodyParts["pelvis"].transform.position)){
            terminate = true;
            AddReward(-1f);
        }
        if (this.GetStepCount()-_stepCountAtLastMeter >= (200*5))
            terminate = true;
        else if (xpos < 4f && _pain > 1f)
            terminate = true;
        else if (xpos < 2f && _pain > 0f)
            terminate = true;

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
                break;
        }
    }
  
    void ObservationsDefault()
    {
        var sensor = this;
        var pelvis = BodyParts["pelvis"];
        Vector3 normalizedVelocity = GetNormalizedVelocity(pelvis.velocity);
        sensor.AddVectorObs(normalizedVelocity);
        sensor.AddVectorObs(pelvis.transform.forward); // gyroscope 
        sensor.AddVectorObs(pelvis.transform.up);

        sensor.AddVectorObs(SensorIsInTouch);
        JointRotations.ForEach(x => sensor.AddVectorObs(x));
        sensor.AddVectorObs(JointVelocity);
        // Vector3 normalizedFootPosition = this.GetNormalizedPosition(pelvis.transform.position);
        // sensor.AddVectorObs(normalizedFootPosition.y);

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

    float StepRewardAnt101()
    {
        float velocity = Mathf.Clamp(GetNormalizedVelocity("pelvis").x, 0f, 1f);
        float effort = 1f - GetEffortNormalized();

        // velocity *= 0.7f;
        // if (velocity >= .25f)
        //     effort *= 0.25f;
        // else
        //     effort *= velocity;

        // var reward = velocity
        //              + effort;
        // if (ShowMonitor)
        // {
        //     var hist = new[] {reward, velocity, effort};
        //     Monitor.Log("rewardHist", hist, displayType: Monitor.DisplayType.Independent);
        // }
        _pain = 0f;
        var reward = velocity;
        return reward;
        // return 0f;
    }
}
