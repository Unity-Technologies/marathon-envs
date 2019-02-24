using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MLAgents
{
    /// <summary>
    /// EnvSpawner holds references to envIds, prefabs, brains for spawning environments.
    /// </summary>
    [System.Serializable]
    public class EnvSpawner
    {
        [System.Serializable]
        public class SpawnableEnvDefinition
        {
            public string envId;
            public GameObject envPrefab;
        }
        [System.Serializable]
        public class Volume
        {
            public Vector3 Negative;
            public Vector3 Positive;
        }

        List<PhysicsScene> physicsScenes;

        [SerializeField]
        public List<SpawnableEnvDefinition> spawnableEnvDefinitions = new List<SpawnableEnvDefinition>();

        /// <summary>
        /// The number of SpawnableEnvs inside the EnvSpawner.
        /// </summary>
        public int Count
        {
            get { return spawnableEnvDefinitions.Count; }
        }

        [Tooltip("The envId to spawn if not overriden from python")]
        public string envIdDefault;
        [Tooltip("The number of environments to spawn in Training Mode if not overriden from python")]
        public int trainingNumEnvsDefault;
        [Tooltip("The number of environments to spawn in Inference Mode if not overriden from python")]
        public int inferenceNumEnvsDefault;
        [Tooltip("If true, enter Training Mode, else Inference Mode")]
        public bool trainingMode;


        /// <summary>
        /// Return prefab for this EnvId else null
        /// </summary>
        public GameObject GetPrefabFor(string thisEnvId)
        {
            var entry = spawnableEnvDefinitions
                .FirstOrDefault(x=>x.envId==thisEnvId);
            return entry?.envPrefab; 
        }

        /// <summary>
        /// Spawn a number of environments. The enviromentment must include SpawnableEnv
        /// </summary>
        public void SpawnSpawnableEnv(GameObject parent, int numInstances, GameObject envPrefab)
        {
            CreateSceneParameters csp = new CreateSceneParameters(LocalPhysicsMode.Physics3D);
            physicsScenes = new List<PhysicsScene>();

            Vector3 spawnStartPos = parent.transform.position;
            SpawnableEnv spawnableEnv = envPrefab.GetComponent<SpawnableEnv>();
            spawnableEnv.UpdateBounds();
            Vector3 step = new Vector3(0f, 0f, spawnableEnv.bounds.size.z + (spawnableEnv.bounds.size.z*spawnableEnv.paddingBetweenEnvs));

            for (int i = 0; i < numInstances; i++)
            {
                var agent = Agent.Instantiate(envPrefab, spawnStartPos, envPrefab.gameObject.transform.rotation);
                bool multiverse = true;
                if (!multiverse)
                {
                    spawnStartPos += step;
                }
                else
                {
                    Scene scene = SceneManager.CreateScene($"SpawnedEnv-{i}", csp);
                    PhysicsScene physicsScene = scene.GetPhysicsScene();
                    SceneManager.MoveGameObjectToScene(agent, scene);
                    SpawnableEnv spawnedEnv = agent.GetComponent<SpawnableEnv>();
                    spawnableEnv.SetSceneAndPhysicsScene(scene, physicsScene);
                    physicsScenes.Add(physicsScene);
                }
            }
        }
        /// <summary>
        /// Reqests a physics step in each scene
        /// </summary>
        public void TriggerPhysicsStep()
        {
            var stepSize = Time.fixedDeltaTime;
            foreach (var physicsScene in physicsScenes)
            {
                physicsScene.Simulate(stepSize);
            }
        }

        /// <summary>
        /// Removes all the Brains of the BroadcastHub
        /// </summary>
        public void Clear()
        {
            spawnableEnvDefinitions.Clear();
        }
    }
}
