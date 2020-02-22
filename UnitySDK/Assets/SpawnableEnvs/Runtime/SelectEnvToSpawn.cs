using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MLAgents
{
    public class SelectEnvToSpawn : MonoBehaviour
    {
        public EnvSpawner agentSpawner;
        bool isInference = true;
        bool showPopUp = false;
        static int envIdIdex = -1;
        string[] envIds;
        int heightRequirments;
        int fontSize;

        Academy academy;
        void Awake()
        {
            // academy = GetComponent<Academy>();
            // agentSpawner = GetComponent<EnvSpawner>();
            envIds = agentSpawner.spawnableEnvDefinitions.Select(x => x.envId).ToArray();
            fontSize = 26;
            if (Screen.height < 720)
                fontSize /= 2;
            heightRequirments = (fontSize + 4) * (envIds.Length + 1);
            if (envIdIdex == -1)
            {
                var envId = GetEnvId();
                var envDef = agentSpawner.spawnableEnvDefinitions.FirstOrDefault(x => x.envId == envId);
                envIdIdex = agentSpawner.spawnableEnvDefinitions.IndexOf(envDef);
            }
            // exit if we should not dispplay the menu
            if (ShouldInitalizeOnAwake())
            {
                var spawnPrefab = agentSpawner.GetPrefabFor(GetEnvId());
                agentSpawner.SpawnSpawnableEnv(this.gameObject, GetNumAgents() ,spawnPrefab);
                return;
            }
            showPopUp = true;
        }

        /// <summary>
        /// Return the envId to spawn.
        /// </summary>
        string GetEnvId()
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
        bool ShouldInitalizeOnAwake()
        {
            if (IsTrainingMode())
                return true;
            if (GetComponent<SelectEnvToSpawn>() == null)
                return true;
            return false;
        }
                /// <summary>
        /// Return if training mode.
        /// </summary>
        bool IsTrainingMode()
        {
            if (Academy.Instance.IsCommunicatorOn)
                return false;
            if (agentSpawner != null && agentSpawner.trainingMode)
                return true;
            List<string> commandLineArgs = new List<string>(System.Environment.GetCommandLineArgs());
            int index = commandLineArgs.IndexOf("--train");
            bool trainingMode = index != -1;
            return trainingMode;
        }

        // Update is called once per frame
        async void Update()
        {
            if (showPopUp)
            {
                var oldEnvIdIdex = envIdIdex;
                if (Input.GetKeyDown(KeyCode.UpArrow))
                    envIdIdex--;
                if (Input.GetKeyDown(KeyCode.DownArrow))
                    envIdIdex++;
                envIdIdex = Mathf.Clamp(envIdIdex, 0, agentSpawner.spawnableEnvDefinitions.Count - 1);
                if (Input.GetKeyDown(KeyCode.Return))
                    Go();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown("`") || Input.GetKeyDown(KeyCode.Delete))
            {
                StartCoroutine(ReloadScene());
            }
        }
        IEnumerator ReloadScene()
        {
            var activeScene = SceneManager.GetActiveScene().buildIndex;
            yield return SceneManager.LoadSceneAsync(activeScene, LoadSceneMode.Single);
        }

        void OnGUI()
        {
            if (showPopUp)
            {
                int height = heightRequirments + 38;
                int yPos = (Screen.height / 2) - (height / 2);
                GUI.skin.window.fontSize = fontSize;
                // GUI.skin.window.margin.top = fontSize;
                GUI.Window(0, new Rect((Screen.width / 2) - 200, yPos
                    , 400, height), ShowGUI, "Select Environment");
            }
        }

        void ShowGUI(int windowID)
        {
            GUI.skin.toggle.fontSize = fontSize;
            Rect rect = new Rect(5, fontSize + 4, 400, fontSize + 4);
            for (int i = 0; i < envIds.Length; i++)
            {
                bool active = envIdIdex == i;
                GUI.SetNextControlName(envIds[i]);
                if (GUI.Toggle(rect, active, envIds[i]))
                    envIdIdex = i;
                rect.y += rect.height;
            }

            rect.x = 400 - 90;
            rect.width = 75;
            rect.height = 30;
            GUI.SetNextControlName("GO");
            if (GUI.Button(new Rect(rect), "GO"))
                Go();
            GUI.FocusControl(agentSpawner.spawnableEnvDefinitions[envIdIdex].envId);
        }
        void Go()
        {
            showPopUp = false;
            agentSpawner.envIdDefault = envIds[envIdIdex];
            //agentSpawner.enabled = true;
            var spawnPrefab = agentSpawner.GetPrefabFor(GetEnvId());
            agentSpawner.SpawnSpawnableEnv(this.gameObject, GetNumAgents() ,spawnPrefab);
        }
    }
}