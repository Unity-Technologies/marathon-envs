using UnityEngine;

namespace MLAgents 
{
    public class SendOnCollisionTrigger : MonoBehaviour {
		// Use this for initialization
		void Start () {
			
		}
		
		// Update is called once per frame
		void Update () {
			
		}

		void OnCollisionEnter(Collision other) {
			// Messenger.
			var otherGameobject = other.gameObject;
            var marathonAgent = otherGameobject.GetComponentInParent<MarathonAgent>();
			// if (marathonAgent?.Length > 0)
			if (marathonAgent != null)
				marathonAgent.OnTerrainCollision(otherGameobject, this.gameObject);
		}
    }
}