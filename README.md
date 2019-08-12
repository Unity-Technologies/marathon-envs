# Marathon Environments

A set of high-dimensional continuous control environments for use with Unity ML-Agents Toolkit.

[Web Demo](https://marathonenvsstandard.z5.web.core.windows.net)

![MarathonEnvs](images/MarathonEnvsBanner.gif)

**MarathonEnvs** enables the reproduction of these benchmarks within Unity ml-agents using Unity’s native physics simulator, PhysX. MarathonEnvs maybe useful for:

* Video Game researchers interested in apply bleeding edge robotics research into the domain of locomotion and AI for video games.
* Traditional academic researchers looking to leverage the strengths of Unity and ML-Agents along with the body of existing research and benchmarks provided by projects such as the [DeepMind Control Suite](https://github.com/deepmind/dm_control), or [OpenAI Mujoco](http://gym.openai.com/envs/#mujoco) environments.

*Note: This project is the result of a contribution from [Joe Booth (@Sohojo)](https://github.com/Sohojoe), a member of the Unity community who currently maintains the repository. As such, the contents of this repository are not officially supported by Unity Technologies.*

---

## What's new in MarathonEnvs-v2.0

### ml-agents 0.9 support

* Updated to work with ml-agents 0.9 / new inference engine

### Unity 2018.4 LTS

* Updated to use Unity 2018.4 LTS. Should work with later versions. However, sometimes Unity makes breaking physics changes.

### MarathonMan-v0

* Optimized for Unity3d + fixes some bugs with the DeepMind.xml version
* Merged from StyleTransfer experimental repro
* Replaces DeepMindHumanoid

### ManathonManSparse-v0

* Sparse version of MarathonMan.
* Single reward is given at end of episode.

### TerrainHopperEnv-v0, TerrainWalker2dEnv-v0, TerrainAntEnv-v0, TerrainMarathonManEnv-v0

* Random Terrain envionments
* Merged from AssaultCourse experimental repro

### SpawnableEnvs (Preview)

* Set the number of instances of an envrionmwnt you want for training and inference
* Envrionments are spawned from prefabs, so no need to manually duplicate
* Supports ability to select from multiple agents in one build
* Unique Physics Scene per Environment (makes it easier to port envionments however runs slower)
* SelectEnvToSpawn.cs - Optional menu to enable user to select from all agents in build

### Skip setting actions

* Option to not set actions when skipping steps.
* Optimization for when running physics at high frequencey

### Scorer.cs

* Score agent against 'goal' (for example, max distance) to distinguish rewards from goals
* Gives mean and std-div over 100 agents

### Normalized Observations (-1 to 1) and reward (0 to 1)

* No need to use normalize flag in training. Helps with OpenAI.Baselines training

### Merge CameraHelper.cs from StyleTransfer. Controls are

* 1, 2, 3 - Slow-mo modes
* arrow keys or w-a-s-d rotate around agent
* q-e zoom in / out

### Default hyperparams are now closer to OpenAI.Baselines

* (1m steps for hopper, walker, ant, 10m for humanoid)

### Training speed improvements - All feet detect distance from floor

## Getting Started

### Web Demo

* Preview MarathonEnvs using the [Web Demo](https://marathonenvsstandard.z5.web.core.windows.net)

### Requirements

* Unity 2018.4 (Download [here](https://unity3d.com/get-unity/download)).
* Cloan / Download this repro
* Install **CUSTOM** ml-agents version 0.9 - install via:

``` sh
cd ml-agents
pip3 install -e ./
```

* Build or install the correct runtime for your version into the `envs\` folder

### Training

* See [Training.md](Training.md) for training us ML-Agents

---

## Publications & Usage

### Publications

* AAAI 2019 Workshop on Games and Simulations for Artificial Intelligence: [Marathon Environments: Multi-Agent Continuous Control Benchmarks in a Modern Video Game Engine](https://arxiv.org/abs/1902.09097)
* An early version of this work was presented March 19th, 2018 at the AI Summit - [Game Developer Conference 2018](http://schedule.gdconf.com/session/beyond-bots-making-machine-learning-accessible-and-useful/856147)

### Research using ML-Agents + MarathonEnvs

* [ActiveRagdollAssaultCourse](https://github.com/Sohojoe/ActiveRagdollAssaultCourse) - Mastering Dynamic Environments
* [ActiveRagdollControllers](https://github.com/Sohojoe/ActiveRagdollControllers) - Implementing a Player Controller
* [ActiveRagdollStyleTransfer](https://github.com/Sohojoe/ActiveRagdollStyleTransfer) - Learning From Motioncapture Data
* [MarathonEnvsBaselines](https://github.com/Sohojoe/MarathonEnvsBaselines) - Experimental implementation with OpenAI.Baselines and Stable.Baselines

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

## References

* [OpenAI.Gym Mujoco](https://github.com/openai/gym/tree/master/gym/envs/mujoco) implementation. Good reference for enviroment setup, reward functions and termination functions.
* [OpenAI.Roboschool](https://github.com/openai/roboschool) - Alternative OpenAI implementation based on [Bullet Physics](http://pybullet.org) with more advanced enviroments. Alternative reference for reward functions and termination functions.
* [DeepMind Control Suite](https://github.com/deepmind/dm_control) - Set of continuous control tasks.
* DeepMind paper [Emergence of Locomotion Behaviours in Rich Environments](https://arxiv.org/pdf/1707.02286) and [video](https://youtu.be/hx_bgoTF7bs)- see page 13 b.2 for detail of reward functions
* [MuJoCo](http://www.mujoco.org) homepage.
* A good primer on the differences between physics engines is ['Physics simulation engines have traditional made tradeoffs between performance’](https://homes.cs.washington.edu/~todorov/papers/ErezICRA15.pdf) and it’s accompanying [video](https://homes.cs.washington.edu/~todorov/media/ErezICRA15.mp4).
* [MuJoCo Unity Plugin](http://www.mujoco.org/book/unity.html) MuJoCo's Unity plugin which uses socket to comunicate between MuJoCo (for running the physics simulation and control) and Unity (for rendering).

### Citing MarathonEnvs

If you use MarathonEnvs in your research, we ask that you please cite our [paper](https://arxiv.org/abs/1902.09097).
