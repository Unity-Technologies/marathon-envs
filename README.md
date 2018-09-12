# Marathon Environments
A set of high-dimensional continuous control environments for use with Unity ML-Agents Toolkit.


![MarathonEnvs](images/MarathonEnvsBanner.gif)

**MarathonEnvs** enables the reproduction of these benchmarks within Unity ml-agents using Unity’s native physics simulator, PhysX. MarathonEnvs maybe useful for:

* Video Game researchers interested in apply bleeding edge robotics research into the domain of locomotion and AI for video games.
* Traditional academic researchers looking to leverage the strengths of Unity and ML-Agents along with the body of existing research and benchmarks provided by projects such as the [DeepMind Control Suite](https://github.com/deepmind/dm_control), or [OpenAI Mujoco](http://gym.openai.com/envs/#mujoco) environments.

*Note: This project is the result of a contribution from [Joe Booth (@Sohojo)](https://github.com/Sohojoe), a member of the Unity community who currently maintains the repository. As such, the contents of this repository are not officially supported by Unity Technologies.* 

---
## Getting Started

### Requirements
 * Unity 2018.2 (Download [here](https://unity3d.com/get-unity/download)).
 * ML-Agents Toolkit v0.5 (Learn more [here](https://github.com/Unity-Technologies/ml-agents)).

### Installation
 * Clone [ml-agents repository](https://github.com/Unity-Technologies/ml-agents).
 * Install [ML-Agents Toolkit](https://github.com/Unity-Technologies/ml-agents/blob/master/docs/Installation.md).
 * Add `MarathonEnvs` sub-folder from this repository to `MLAgentsSDK\Assets\` in cloned ml-agents repository.
 * Add `config\marathon_envs_config.yaml` from this reprository to `config\` in cloned ml-agents repository.

---
## Publications & Usage 

An early version of this work was presented March 19th, 2018 at the AI Summit - Game Developer Conference 2018 - http://schedule.gdconf.com/session/beyond-bots-making-machine-learning-accessible-and-useful/856147

### Active Research using ML-Agents + MarathonEnvs
 * [Mastering Dynamic Environments](https://github.com/Sohojoe/ActiveRagdollAssaultCourse)
 * [Controllers](https://github.com/Sohojoe/ActiveRagdollControllers)
 * [Style Transfer](https://github.com/Sohojoe/ActiveRagdollStyleTransfer)


---
## Support and Contributing
 
**Support:** Post an issue if you are having problems or need help getting a xml working.

**Contributing:** Ml-Agents 0.5 now supports the Gym interface. It would be of value to the community to reproduce more benchmarcks and create a set of sample code for various algorthems. This would be a great way for someone looking to gain some experiance with Re-enforcement Learing. I would gladdly support and / or partner. Please post an issue if you are interesgted. Here are some ideas:
 * [Hindsight Experience Replay (HER)](https://github.com/openai/baselines/tree/master/baselines/her)
 * [Model-Agnostic Meta-Learning (MAML)](https://github.com/cbfinn/maml_rl) 
 * Any of A2C, ACER, ACKTR, DDPG, DQN, GAIL, PPO2, TRPO from [OpenAI.Baselines](https://github.com/openai/baselines)


---

## Included Environments

### Humanoid

| **DeepMindHumanoid** | 
| --- | 
| ![DeepMindHumanoid](images/DeepMindHumanoid102-2m.gif) | 


* Set-up: Complex (DeepMind) Humanoid agent. 
* Goal: The agent must move its body toward the goal as quickly as possible without falling.
* Agents: The environment contains 16 independent agents linked to a single brain.
* Agent Reward Function: 
  * Reference OpenAI.Roboschool and / or DeepMind
    * -joints at limit penality
    * -effort penality (ignors hip_y and knee)
    * +velocity
    * -height penality if below 1.2m
  * Inspired by Deliberate Practice (currently, only does legs)
    * +facing upright bonus for shoulders, waist, pelvis
    * +facing target bonus for shoulders, waist, pelvis
    * -non straight thigh penality
    * +leg phase bonus (for height of knees)
    * +0.01 times body direction alignment with goal direction.
    * -0.01 times head velocity difference from body velocity.
* Agent Terminate Function: 
  * TerminateOnNonFootHitTerrain - Agent terminates when a body part other than foot collides with the terrain.
* Brains: One brain with the following observation/action space.
    * Vector Observation space: (Continuous) 88 variables
    * Vector Action space: (Continuous) Size of 21 corresponding to target rotations applicable to the joints. 
    * Visual Observations: None.
* Reset Parameters: None.

### Hopper

| **DeepMindHopper** | 
| --- | 
| ![DeepMindHopper](images/DeepMindHopper101-1m.gif) | 

* Set-up: DeepMind Hopper agents. 
* Goal: The agent must move its body toward the goal as quickly as possible without falling.
* Agents: The environment contains 16 independent agents linked to a single brain.
* Agent Reward Function: 
  * Reference OpenAI.Roboschool and / or DeepMind
    * -effort penality
    * +velocity
    * +uprightBonus
    * -height penality if below .65m OpenAI, 1.1m DeepMind
* Agent Terminate Function: 
  * DeepMindHopper: TerminateOnNonFootHitTerrain - Agent terminates when a body part other than foot collides with the terrain.
  * OpenAIHopper
    * TerminateOnNonFootHitTerrain
    * Terminate if height < .3m
    * Terminate if head tilt > 0.4
* Brains: One brain with the following observation/action space.
    * Vector Observation space: (Continuous) 31 variables
    * Vector Action space: (Continuous) 4 corresponding to target rotations applicable to the joints. 
    * Visual Observations: None.
* Reset Parameters: None.


### Walker

| **DeepMindWalker** | 
| --- | 
| ![DeepMindWalker](images/DeepMindWalker108-1m.gif) | 

* Set-up: DeepMind Walker agent. 
* Goal: The agent must move its body toward the goal as quickly as possible without falling.
* Agents: The environment contains 16 independent agents linked to a single brain.
* Agent Reward Function: 
  * Reference OpenAI.Roboschool and / or DeepMind
    * -effort penality
    * +velocity
    * +uprightBonus
    * -height penality if below .65m OpenAI, 1.1m DeepMind
* Agent Terminate Function: 
  * TerminateOnNonFootHitTerrain - Agent terminates when a body part other than foot collides with the terrain.
* Brains: One brain with the following observation/action space.
    * Vector Observation space: (Continuous) 41 variables
    * Vector Action space: (Continuous) Size of 6 corresponding to target rotations applicable to the joints. 
    * Visual Observations: None.
* Reset Parameters: None.

### Ant

| **OpenAIAnt** |
| --- | 
| ![OpenAIAnt](images/OpenAIAnt102-1m.gif) | 

* Set-up: OpenAI and Ant agent. 
* Goal: The agent must move its body toward the goal as quickly as possible without falling.
* Agents: The environment contains 16 independent agents linked to a single brain.
* Agent Reward Function: 
  * Reference OpenAI.Roboschool and / or DeepMind
    * -joints at limit penality
    * -effort penality 
    * +velocity
* Agent Terminate Function: 
  * Terminate if head body > 0.2
* Brains: One brain with the following observation/action space.
    * Vector Observation space: (Continuous) 53 variables
    * Vector Action space: (Continuous) Size of 8 corresponding to target rotations applicable to the joints. 
    * Visual Observations: None.
* Reset Parameters: None.

---

## Details
### Key Files / Folders
* MarathonEnvs - parent folder
  * Scripts/MarathonAgent.cs - Base Agent class for Marathon implementations
  * Scripts/MarathonSpawner.cs - Class for creating a Unity game object from a xml file
  * Scripts/MarathonJoint.cs - Model for mapping MuJoCo joints to Unity
  * Scripts/MarathonSensor.cs - Model for mapping MuJoCo sensors to Unity
  * Scripts/MarathonHelper.cs - Helper functions for MarathonSpawner.cs
  * Scripts/HandleOverlap.cs - helper script to for detecting overlapping Marathon elements.
  * Scripts/ProceduralCapsule.cs - Creates a Unity capsule which matches MuJoCo capsule
  * Scripts/SendOnCollisionTrigger.cs - class for sending collisions to MarathonAgent.cs
  * Scripts/SensorBehavior.cs - behavior class for sensors
  * Scripts/SmoothFollow.cs - camera script
  * Enviroments - sample enviroments
    * DeepMindReferenceXml - xml model files used in DeepMind research [source](https://github.com/deepmind/dm_control/blob/master/dm_control/suite/walker.py)
    * DeepMindHopper - Folder for reproducing DeepMindHopper 
    * OpenAIAnt - Folder for reproducing OpenAIAnt 
    * etc
* config
  * marathon_envs_config.yaml - trainer-config file. The hyperparameters used when training from python.

### Tuning params / Magic numbers
* xxNamexx\Prefab\xxNamexx -> MarathonSpawner.Force2D = set to True when implementing a 2d model (hopper, walker)
* xxNamexx\Prefab\xxNamexx -> MarathonSpawner.DefaultDesity:
  * 1000 = default (= same as MuJoCo)
  * Note: maybe overriden within a .xml script
* xxNamexx\Prefab\xxNamexx -> MarathonSpawner.MotorScale = Magic number for tuning (scaler applied to all motors)
  * 1 = default () 
  * 1.5 used by DeepMindHopper, DeepMindWalker
  
* xxNamexx\Prefab\xxNamexx -> xxAgentScript.MaxStep / DecisionFrequency: 
  * 5000,5: OpenAIAnt, DeepMindHumanoid
  * 4000,4: DeepMindHopper, DeepMindWalker
  * Note: all params taken from OpenAI.Gym

### Important: 
* This is not a complete implementation of MuJoCo; it is focused on doing just enough to get the locomotion enviroments working in Unity. See Scripts/MarathonSpawner.cs for which MuJoCo commands and ignored or partially implemented.
* PhysX makes many tradeoffs in terms of accuracy when compared with Mujoco. It may not be the best choice for your research project.
* Marathon environments are running at 300-500 physics simulations per second. This is significantly higher that Unity’s defaults setting of 50 physics simulations per second.
* Currently, Marathon does not properly simulate how MuJoCo handles joint observations - as such, it maybe difficult to do transfer learning (from simulation to real world robots)


### References:
* [OpenAI.Gym Mujoco](https://github.com/openai/gym/tree/master/gym/envs/mujoco) implementation. Good reference for enviroment setup, reward functions and termination functions.
* [OpenAI.Roboschool](https://github.com/openai/roboschool) - Alternative OpenAI implementation based on [Bullet Physics](http://pybullet.org) with more advanced enviroments. Alternative reference for reward functions and termination functions.
* [DeepMind Control Suite](https://github.com/deepmind/dm_control) - Set of continuous control tasks.
* DeepMind paper [Emergence of Locomotion Behaviours in Rich Environments](https://arxiv.org/pdf/1707.02286) and [video](https://youtu.be/hx_bgoTF7bs)- see page 13 b.2 for detail of reward functions
* [MuJoCo](http://www.mujoco.org) homepage.
* A good primer on the differences between physics engines is ['Physics simulation engines have traditional made tradeoffs between performance’](https://homes.cs.washington.edu/~todorov/papers/ErezICRA15.pdf) and it’s accompanying [video](https://homes.cs.washington.edu/~todorov/media/ErezICRA15.mp4).
* [MuJoCo Unity Plugin](http://www.mujoco.org/book/unity.html) MuJoCo's Unity plugin which uses socket to comunicate between MuJoCo (for running the physics simulation and control) and Unity (for rendering).




