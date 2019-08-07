

-----
### ‎⁨ml-agents/⁨mlagents⁩/trainers⁩/learn.py
#### line 217
``` python
      --num-agents=<n>           The number of agests to request the Unity enviroment to generate.
      --agent-id=<name>          The name of the agent to request the Unity enviroment to instantiate.
```
-----
## Editor - Add Files
### ‎UnitySDK/Assets/ML-Agents/Editor/EnvSpawnerDrawer.cs
-----
## Scripts - Add Files
### UnitySDK/Assets/ML-Agents/Scripts/EnvSpawner.cs
### UnitySDK/Assets/ML-Agents/Scripts/SpawnableEnv.cs
### UnitySDK/Assets/ML-Agents/Scripts/SelectEnvToSpawn.cs
-----
## Scripts - Edits
### UnitySDK/Assets/ML-Agents/Scripts/Academy.cs
line 91, add
```csharp
        public EnvSpawner agentSpawner = new EnvSpawner();
```
(only if syncing with python) line 199, add 
```csharp
        /// The parameters passed from python
        CommunicatorObjects.UnityRLInitializationInput pythonParameters;
```
line 240, replace
```csharp
        void Awake()
        {
            InitializeEnvironment();
        }
```
with
```csharp
        void Awake()
        {
            if (ShouldInitalizeOnAwake())
                InitializeEnvironment();
            else
                enabled = false;
```
then add
```csharp
        void OnEnable()
        {
            if (!ShouldInitalizeOnAwake())
                InitializeEnvironment();
        }
        public bool ShouldInitalizeOnAwake()
        {
            if (agentSpawner != null && agentSpawner.trainingMode)
                return true;
            if (GetComponent<SelectEnvToSpawn>() == null)
                return true;
            return false;
        }
```
line 291, replace
```csharp
            var exposedBrains = broadcastHub.broadcastingBrains.Where(x => x != null).ToList();;
            var controlledBrains = broadcastHub.broadcastingBrains.Where(
                x => x != null && x is LearningBrain && broadcastHub.IsControlled(x));
            foreach (LearningBrain brain in controlledBrains)
            {
                brain.SetToControlledExternally();
            }
```
with
```csharp
            var spawnPrefab = agentSpawner.GetPrefabFor(GetAgentId());
            var spawnAgentPrefabs = spawnPrefab.GetComponentsInChildren<Agent>();
            var spawnAgentPrefabBrains = spawnAgentPrefabs
                .Where(x=>x.brain as LearningBrain != null)
                .Select(x=>x.brain)
                .ToList();
            var spawnerEnabled = spawnAgentPrefabBrains.Count > 0;
            var hubBrains = broadcastHub.broadcastingBrains.Where(x => x != null).ToList();;
            var hubControlledBrains = broadcastHub.broadcastingBrains.Where(
                x => x != null && x is LearningBrain && broadcastHub.IsControlled(x));

            IEnumerable<Brain> exposedBrains = 
                spawnerEnabled ? spawnAgentPrefabBrains : hubBrains;
            IEnumerable<Brain> controlledBrains = hubControlledBrains;
            if (spawnerEnabled)
                controlledBrains = agentSpawner.trainingMode 
                    ? spawnAgentPrefabBrains 
                    : new List<Brain>();
```
line 336, add
```csharp
            foreach (LearningBrain brain in controlledBrains)
            {
                brain.SetToControlledExternally();
            }
```
line 371, replace
```csharp
                var pythonParameters = brainBatcher.SendAcademyParameters(academyParameters);
```
with
```csharp
                pythonParameters = brainBatcher.SendAcademyParameters(academyParameters);
```
line 398, add
```python
            if (spawnerEnabled)
                agentSpawner.SpawnSpawnableEnv(this.gameObject, GetNumAgents() ,spawnPrefab);
```
line 441, add
```python
        /// <summary>
        /// Return the number of agents to spawn.
        /// </summary>
        public int GetNumAgents()
        {
            // // TODO - re-enable python coms
            // if (pythonParameters != null && pythonParameters.NumAgents != 0)
            //     return pythonParameters.NumAgents;
            return isInference ? agentSpawner.inferenceNumEnvsDefault : agentSpawner.trainingNumEnvsDefault;
        }

        /// <summary>
        /// Return the agentId to spawn.
        /// </summary>
        public string GetAgentId()
        {
            // // TODO - re-enable python coms
            // if (pythonParameters != null && !string.IsNullOrWhiteSpace(pythonParameters?.AgentId))
            //     return pythonParameters.AgentId;
            return agentSpawner.envIdDefault;
        }
```
line 688, add to `FixedUpdate()`
```python
            SpawnableEnv.TriggerPhysicsStep();
```
line 699, add tp `OnDestroy()`
```python
            broadcastHub.Clear();
            broadcastHub = null;
            agentSpawner = null;
```
-----
### UnitySDK/Assets/ML-Agents/Scripts/Agent.cs
line 181, add
```csharp

        /// <summary>
        /// If numberOfActionsBetweenDecisions > 1, setting this to true will 
        /// only send Actions on decisions steps. This is useful when running 
        /// physics at high frequencies on continious control agents. (used 
        /// when On Demand Decisions is turned off).
        /// </summary>
        public bool skipActionsWithDecisions;
```
line 902, add
```csharp
        protected AgentInfo GetInfo()
        {
            return info;
        }
```
line 1026, replace
```csharp
        void MakeRequests(int academyStepCounter)
        {
            agentParameters.numberOfActionsBetweenDecisions =
                Mathf.Max(agentParameters.numberOfActionsBetweenDecisions, 1);
            if (!agentParameters.onDemandDecision)
            {
                RequestAction();
                if (academyStepCounter %
                    agentParameters.numberOfActionsBetweenDecisions == 0)
                {
                    RequestDecision();
                }
            }
        }
```
with
```csharp
        void MakeRequests(int academyStepCounter)
        {
            agentParameters.numberOfActionsBetweenDecisions =
                Mathf.Max(agentParameters.numberOfActionsBetweenDecisions, 1);
            if (!agentParameters.onDemandDecision)
            {
                bool skipDecision = false;
                bool skipAction = false;
                if (academyStepCounter %
                    agentParameters.numberOfActionsBetweenDecisions != 0)
                {
                    skipDecision = true;
                    skipAction = agentParameters.skipActionsWithDecisions;
                }
                if (!skipAction)
                    RequestAction();
                if (!skipDecision)
                    RequestDecision();
            }
        }
```
line 1101, add
```csharp
        /// <summary>
        /// Cleanup function
        /// </summary>
        protected virtual void OnDestroy()
        {
            brain?.Clear();
            Monitor.RemoveAllValues(transform);
        }
     }
```
-----
### UnitySDK/Assets/ML-Agents/Scripts/Brain.cs
line 92, add
```csharp
        public void Clear()
        {
            var academy = FindObjectOfType<Academy>();
            if (academy != null)
                academy.BrainDecideAction -= BrainDecideAction;
            agentInfos.Clear();
            _isInitialized = false;
        }
```
-----
### UnitySDK/Assets/ML-Agents/Scripts/Monitor.cs
line 33 repace
``` csharp
        static bool isInstantiated;
```
with
``` csharp
        static bool isInstantiated{ get {return canvas!=null;} }
``` 
line 89, within `if (!isInstantiated)` remove 
``` csharp
        isInstantiated = true; 
```
line 159, within `if (!isInstantiated)` remove 
``` csharp
        isInstantiated = true; 
```
line 221, within `if (!isInstantiated)` remove 
``` csharp
        isInstantiated = true; 
```
line 335, within `if (!isInstantiated)` remove 
``` csharp
        isInstantiated = true; 
```
