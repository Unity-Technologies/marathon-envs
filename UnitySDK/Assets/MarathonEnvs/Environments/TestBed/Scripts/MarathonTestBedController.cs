using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarathonTestBedController : MonoBehaviour
{
    [Tooltip("Action applied to each motor")]
    /**< \brief Edit to manually test each motor (+1/-1)*/
    public float[] Actions;

    [Tooltip("Apply a random number to each action each framestep")]
    /**< \brief Apply a random number to each action each framestep*/
    public bool ApplyRandomActions = true;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
