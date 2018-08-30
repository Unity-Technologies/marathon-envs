using UnityEngine;

namespace MLAgents
{
    public class SensorBehavior : MonoBehaviour
    {
        MarathonAgent _marathonAgent;
        Collider _collider;
        void Start ()
        {
            _marathonAgent = GetComponentInParent<MarathonAgent>();
            _collider = GetComponent<Collider>();
        }
        void OnCollisionEnter(Collision other) 
        {
            if (_marathonAgent!=null)
                _marathonAgent.SensorCollisionEnter(_collider, other);
        }
        void OnCollisionExit(Collision other) 
        {
            if (_marathonAgent!=null)
                _marathonAgent.SensorCollisionExit(_collider, other);
        }

    }
}