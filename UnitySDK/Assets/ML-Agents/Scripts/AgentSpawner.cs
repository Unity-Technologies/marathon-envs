using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MLAgents
{
    /// <summary>
    /// AgentSpawner holds references to agentIds, prefabs, brains for spawning agents.
    /// </summary>
    [System.Serializable]
    public class AgentSpawner
    {
        [System.Serializable]
        public class SpawnableAgent
        {
            public string agentId;
            public GameObject envPrefab;
        }
        [System.Serializable]
        public class Volume
        {
            public Vector3 Negative;
            public Vector3 Positive;
        }

        [SerializeField]
        public List<SpawnableAgent> spawnableAgents = new List<SpawnableAgent>();

        /// <summary>
        /// The number of SpawnableAgents inside the AgentSpawner.
        /// </summary>
        public int Count
        {
            get { return spawnableAgents.Count; }
        }

        [Tooltip("The agentId to spawn if not overriden from python")]
        public string agentIdDefault;
        [Tooltip("The number of agents to spawn in Training Mode if not overriden from python")]
        public int trainingNumAgentsDefault;
        [Tooltip("The number of agents to spawn in Inference Mode if not overriden from python")]
        public int inferenceNumAgentsDefault;
        [Tooltip("If true, enter Training Mode, else Inference Mode")]
        public bool trainingMode;


        /// <summary>
        /// Return prefab for this AgentId else null
        /// </summary>
        public GameObject GetPrefabFor(string thisAgentId)
        {
            var entry = spawnableAgents
                .FirstOrDefault(x=>x.agentId==thisAgentId);
            return entry?.envPrefab; 
        }

        /// <summary>
        /// Spawn a number of enviroments. The enviromentment must include SpawnableEnv
        /// </summary>
        public void SpawnSpawnableEnv(GameObject parent, int numInstances, GameObject envPrefab)
        {
            Vector3 spawnStartPos = parent.transform.position;
            SpawnableEnv spawnableEnv = envPrefab.GetComponent<SpawnableEnv>();
            spawnableEnv.UpdateBounds();
            Vector3 step = new Vector3(0f, 0f, spawnableEnv.bounds.size.z + (spawnableEnv.bounds.size.z*spawnableEnv.paddingBetweenEnvs));

            for (int i = 0; i < numInstances; i++)
            {
                var agent = Agent.Instantiate(envPrefab, spawnStartPos, envPrefab.gameObject.transform.rotation);
                spawnStartPos += step;
            }

        }
        
        /// <summary>
        /// Removes all the Brains of the BroadcastHub
        /// </summary>
        public void Clear()
        {
            spawnableAgents.Clear();
        }
    }
}
