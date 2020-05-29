using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;

public class MarathonTestBedController : MonoBehaviour
{
    [Tooltip("Action applied to each motor")]
    /**< \brief Edit to manually test each motor (+1/-1)*/
    public float[] Actions;

    [Tooltip("Apply a random number to each action each framestep")]
    /**< \brief Apply a random number to each action each framestep*/
    public bool ApplyRandomActions = true;

    public bool FreezeHead = false;
    public bool FreezeHips = false;
    bool _hasFrozen;


    // Start is called before the first frame update
    void Start()
    {

    }
    void FreezeBodyParts()
    {

        var marathonAgents = FindObjectsOfType<Agent>();
        
        foreach (var agent in marathonAgents)
        {
            ArticulationBody head = null;
            ArticulationBody butt = null;
            ArticulationBody[] children = null;
            switch (agent.name)
            {
                case "MarathonMan":
                    _hasFrozen = true;
                    children = agent.GetComponentsInChildren<ArticulationBody>();
                    head = children.FirstOrDefault(x=>x.name=="torso");
                    butt = children.FirstOrDefault(x=>x.name=="butt");
                    // var rb = children.FirstOrDefault(x=>x.name == "MarathonMan");
                    // if (FreezeHead || FreezeHips)
                    //     rb.constraints = RigidbodyConstraints.FreezeAll;
                    // if (FreezeHead && !FreezeHips)
                    //     rb.GetComponentInChildren<FixedJoint>().connectedBody = head;
                    break;
                case "RagDoll":
                    _hasFrozen = true;
                    children = agent.GetComponentsInChildren<ArticulationBody>();
                    head = children.FirstOrDefault(x=>x.name=="torso");
                    butt = children.FirstOrDefault(x=>x.name=="butt");
                    break;
                case "humanoid":
                    _hasFrozen = true;
                    children = agent.GetComponentsInChildren<ArticulationBody>();
                    head = children.FirstOrDefault(x=>x.name=="head");
                    butt = children.FirstOrDefault(x=>x.name=="butt");
                    break;
                default:
                    break;
            }
            if (FreezeHead && head != null)
                head.immovable = true;
            if (FreezeHips && butt != null)
                butt.immovable = true;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!_hasFrozen)
            FreezeBodyParts();
        if (ApplyRandomActions)
        {
            Actions = Actions.Select(x=>Random.Range(-1f,1f)).ToArray();
        }
    }
}
