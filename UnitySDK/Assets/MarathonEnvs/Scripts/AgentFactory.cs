using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MLAgents
{
    public class AgentFactory : MonoBehaviour
    {
        public Agent AgentPrefab;
        public int NumAgents = 16;
        Academy _academy;

        public Vector3 SpawnStartPos;
        public Vector3 SpawnSize;

        // Start is called before the first frame update
        void Start()
        {
            _academy = FindObjectOfType<Academy>();
            if (_academy.resetParameters.ContainsKey("num_agents"))
            {
                NumAgents = (int)_academy.resetParameters["num_agents"];
            }
            SpawnAgents(
                AgentPrefab,
                NumAgents,
                SpawnStartPos,
                SpawnSize
            );
        }
        void SpawnAgents(Agent agentPrefab, int numAgents, Vector3 spawnStartPos, Vector3 spawnSize)
        {
            bool hasSetCamera = false;
            float zStart = spawnStartPos.z;
            while (numAgents > 0)
            {
                Instantiate(agentPrefab, spawnStartPos, agentPrefab.gameObject.transform.rotation);

                spawnStartPos += new Vector3(0f, 0f, 1f);

                if (!hasSetCamera) {
                    MarathonAgent marathonAgent = agentPrefab.GetComponent<MarathonAgent>();
                    if (marathonAgent != null) {
                        marathonAgent.CameraTarget = FindObjectOfType<Camera>()?.gameObject;
                        hasSetCamera = true;
                    }
                }
                numAgents--;
            }
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    }
}