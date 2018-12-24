using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;

public class MarathonTestBedDecision : Decision
{
    // Brain _brain;

    MarathonTestBedController _controller;


    // [Tooltip("Lock the top most element")]
    // /**< \brief Lock the top most element*/
    // public bool FreezeTop = false;
    // bool _lastFreezeTop;

    public override float[] Decide(
        List<float> vectorObs,
        List<Texture2D> visualObs,
        float reward,
        bool done,
        List<float> memory)
    {
        // lazy init
        if (_controller == null)
        {
            _controller = FindObjectOfType<MarathonTestBedController>();
            // _brain = GetComponent<Brain>();
            // Actions = Enumerable.Repeat(0f, _brain.brainParameters.vectorActionSize[0]).ToArray();
            // Actions = Enumerable.Repeat(0f, 100).ToArray();
        }
        if (_controller.ApplyRandomActions)
        {
            for (int i = 0; i < _controller.Actions.Length; i++)
                _controller.Actions[i] = Random.value * 2 - 1;
        }

        return _controller.Actions;
    }

    public override List<float> MakeMemory(
            List<float> vectorObs,
            List<Texture2D> visualObs,
            float reward,
            bool done,
            List<float> memory)
    {
        return new List<float>();
    }
}