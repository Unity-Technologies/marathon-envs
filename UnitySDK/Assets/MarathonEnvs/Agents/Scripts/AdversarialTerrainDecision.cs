using System.Collections;
using System.Collections.Generic;
using MLAgents;
using UnityEngine;

public class AdversarialTerrainDecision : Decision
{
    float[] actions;
    public override float[] Decide(List<float> vectorObs, List<Texture2D> visualObs, float reward, bool done, List<float> memory)
    {
        int action = Random.Range(0,21);
        if (actions == null){
            actions = new float[]{0f};
        }
        actions[0] = (float) action;
        return actions;
    }

    public override List<float> MakeMemory(List<float> vectorObs, List<Texture2D> visualObs, float reward, bool done, List<float> memory)
    {
        return new List<float>();
    }
}
