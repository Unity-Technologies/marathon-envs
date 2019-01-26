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
            public GameObject agentPrefab;
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
        [Tooltip("How much world space should each agent occupy. Offset to min")]
        public Vector3 worldBoundsMinOffset;
        [Tooltip("How much world space do we have to spawn within. Offset to max")]
        public Vector3 worldBoundsMaxOffset;
        [Tooltip("If true, enter Training Mode, else Inference Mode")]
        public bool trainingMode;


        // public bool Count
        // {
        //     get { return spawnableAgents.Count; }
        // }
        /// <summary>
        /// Return prefab for this AgentId else null
        /// </summary>
        public GameObject GetPrefabFor(string thisAgentId)
        {
            var entry = spawnableAgents
                .FirstOrDefault(x=>x.agentId==thisAgentId);
            return entry?.agentPrefab; 
        }

        public void SpawnAgents(GameObject parent, int numAgents, GameObject envPrefab)
        {
            Vector3 spawnStartPos = parent.transform.position;
            numAgents = FloodVolume(numAgents, envPrefab, spawnStartPos);
        }
        int FloodVolume(int numAgents, GameObject envPrefab, Vector3 spawnStartPos)
        {
            var agentPrefab = envPrefab.GetComponentInChildren<Agent>();
            var yStep = new Vector3(0f, agentPrefab.agentBoundsMaxOffset.y - agentPrefab.agentBoundsMinOffset.y, 0f);
            numAgents = FloodLevel(numAgents, envPrefab, spawnStartPos);
            if (numAgents > 0)
                numAgents = FloodVolume(numAgents, envPrefab, spawnStartPos + yStep);
            if (numAgents > 0)
                numAgents = FloodVolume(numAgents, envPrefab, spawnStartPos - yStep);
            return numAgents;
        }
        int FloodLevel(int numAgents, GameObject envPrefab, Vector3 spawnStartPos)
        {
            var agentPrefab = envPrefab.GetComponentInChildren<Agent>();
            var zStep = new Vector3(0f, 0f, agentPrefab.agentBoundsMaxOffset.z - agentPrefab.agentBoundsMinOffset.z);
            var xStep = new Vector3(agentPrefab.agentBoundsMaxOffset.x - agentPrefab.agentBoundsMinOffset.x, 0f, 0f);
            numAgents = FloodZ(numAgents, envPrefab, spawnStartPos, zStep);
            if (numAgents > 0)
                numAgents = FloodLevel(numAgents, envPrefab, spawnStartPos + xStep);
            if (numAgents > 0)
                numAgents = FloodLevel(numAgents, envPrefab, spawnStartPos - xStep);
            return numAgents;
        }

        int FloodZ(int numAgents, GameObject envPrefab, Vector3 spawnStartPos, Vector3 zStep)
        {
            // var zStep = new Vector3(0f, 0f, agentPrefab.agentBoundsMaxOffset.z - agentPrefab.agentBoundsMinOffset.z);
            // var xStep = new Vector3(agentPrefab.agentBoundsMaxOffset.x - agentPrefab.agentBoundsMinOffset.x, 0f, 0f);
            numAgents = Flood(numAgents, envPrefab, spawnStartPos, zStep);
            if (numAgents > 0)
                numAgents = Flood(numAgents, envPrefab, spawnStartPos-zStep, -zStep);
            return numAgents;
        }

        int Flood(int numAgents, GameObject envPrefab, Vector3 spawnStartPos, Vector3 step)
        {
            var agentPrefab = envPrefab.GetComponentInChildren<Agent>();
            // Check volume
            var agentBounds = new Bounds();
            agentBounds.SetMinMax(agentPrefab.agentBoundsMinOffset + spawnStartPos, agentPrefab.agentBoundsMaxOffset + spawnStartPos);
            // var agentEdge = agentBounds.size / 2;
            var worldBounds = new Bounds();
            worldBounds.SetMinMax(worldBoundsMinOffset, worldBoundsMaxOffset);
            if (!worldBounds.Intersects(agentBounds))
                return numAgents;
            var agent = Agent.Instantiate(envPrefab, spawnStartPos, envPrefab.gameObject.transform.rotation);
            numAgents--;
            if (numAgents > 0)
                numAgents = Flood(numAgents, envPrefab, spawnStartPos + step, step);
            return numAgents;
        }



        int FloodFill3D(int numAgents, Agent agentPrefab, Vector3 spawnStartPos)
        {
            // Check volume
            var agentBounds = new Bounds();
            agentBounds.SetMinMax(agentPrefab.agentBoundsMinOffset + spawnStartPos, agentPrefab.agentBoundsMaxOffset + spawnStartPos);
            // var agentEdge = agentBounds.size / 2;
            var worldBounds = new Bounds();
            worldBounds.SetMinMax(worldBoundsMinOffset, worldBoundsMaxOffset);
            if (!worldBounds.Intersects(agentBounds))
                return numAgents;
            var agent = Agent.Instantiate(agentPrefab, spawnStartPos, agentPrefab.gameObject.transform.rotation);
            numAgents--;
            if (numAgents > 0)
                numAgents = FloodFill3D(numAgents, agentPrefab, spawnStartPos+new Vector3(0f, 0f, agentBounds.size.z));
            if (numAgents > 0)
                numAgents = FloodFill3D(numAgents, agentPrefab, spawnStartPos+new Vector3(0f, 0f, -agentBounds.size.z));
            if (numAgents > 0)
                numAgents = FloodFill3D(numAgents, agentPrefab, spawnStartPos+new Vector3(0f, agentBounds.size.y, 0f));
            if (numAgents > 0)
                numAgents = FloodFill3D(numAgents, agentPrefab, spawnStartPos+new Vector3(0f, -agentBounds.size.y, 0f));
            if (numAgents > 0)
                numAgents = FloodFill3D(numAgents, agentPrefab, spawnStartPos+new Vector3(agentBounds.size.x, 0f, 0f));
            if (numAgents > 0)
                numAgents = FloodFill3D(numAgents, agentPrefab, spawnStartPos+new Vector3(-agentBounds.size.x, 0f, 0f));
            return numAgents;
        }


        //     // bool hasSetCamera = false;
        //     float zStart = spawnStartPos.z;
        //     while (numAgents > 0)
        //     {
        //         var agent = Agent.Instantiate(agentPrefab, spawnStartPos, agentPrefab.gameObject.transform.rotation);
        //         spawnStartPos += new Vector3(0f, 0f, 1f);
        //         numAgents--;
        //     }
        // }

        
        /// <summary>
        /// Removes all the Brains of the BroadcastHub
        /// </summary>
        public void Clear()
        {
            spawnableAgents.Clear();
        }
    }
}
