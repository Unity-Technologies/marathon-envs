

-----
### ‎⁨ml-agents/⁨mlagents⁩/trainers⁩/learn.py
line 90 add (last param for `create_environment_factory()`)
``` python
        list([str(x) for t in run_options.items() for x in t]), # NOTE passes all arguments to Unity
```
line 210 add (last param for `create_environment_factory()`)
``` python
    unity_args={},
```
line 246 add (last param for `UnityEnvironment()`)
``` python
            args=unity_args,
```
line 298 add (two new options for `_USAGE`)
``` 
      --spawn-env=<name>          Inform environment which SpawnableEnv to use (if supported)  [default: None].
      --num-spawn-envs=<n>        Inform environment how many SpawnableEnv to create (if supported)  [default: None].
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
            if (agentSpawner != null && IsTrainingMode())
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
                controlledBrains = IsTrainingMode()
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
            // try get from command line
            List<string> commandLineArgs = new List<string>(System.Environment.GetCommandLineArgs());
            int index = commandLineArgs.IndexOf("--num-spawn-envs");
            if(index != -1) {
                int numEnvs;
                if (int.TryParse(commandLineArgs[index + 1], out numEnvs))
                    return numEnvs;
            }
            return isInference ? agentSpawner.inferenceNumEnvsDefault : agentSpawner.trainingNumEnvsDefault;
        }

        /// <summary>
        /// Return the agentId to spawn.
        /// </summary>
        public string GetAgentId()
        {
            // try get from command line
            List<string> commandLineArgs = new List<string>(System.Environment.GetCommandLineArgs());
            int index = commandLineArgs.IndexOf("--spawn-env");
            if(index != -1) {
                return commandLineArgs[index + 1];
            }
            return agentSpawner.envIdDefault;
        }

        /// <summary>
        /// Return if training mode.
        /// </summary>
        bool IsTrainingMode()
        {
            if (agentSpawner != null && agentSpawner.trainingMode)
                return true;
            List<string> commandLineArgs = new List<string>(System.Environment.GetCommandLineArgs());
            int index = commandLineArgs.IndexOf("--train");
            bool trainingMode = index != -1;
            return trainingMode;
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
