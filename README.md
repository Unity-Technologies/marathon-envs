# Marathon Environments

A set of high-dimensional continuous control environments for use with Unity ML-Agents Toolkit.

![MarathonEnvs](images/MarathonEnvsBanner.gif)

## Preview MarathonEnvs using the **[Web Demo](http://marathonenvs.joebooth.com)**

**MarathonEnvs** is a set of high-dimensional continuous control benchmarks using Unity’s native physics simulator, PhysX. MarathonEnvs can be trained using Unity ML-Agents or any OpenAI Gym compatible algorithm. MarathonEnvs may be useful for:

* Video Game researchers interested in apply bleeding-edge robotics research into the domain of locomotion and AI for video games.
* Academic researchers looking to leverage the strengths of Unity and ML-Agents along with the body of existing research and benchmarks provided by projects such as the [DeepMind Control Suite](https://github.com/deepmind/dm_control), or [OpenAI Mujoco](http://gym.openai.com/envs/#mujoco) environments.

*Note: This project is the result of contributions from members of the Unity community (see below) who actively maintain the repository. As such, the contents of this repository are not officially supported by Unity Technologies.*

* [Joe Booth (@Sohojoe)](https://github.com/Sohojoe), Twitter - [@iAmVidyaGamer](https://twitter.com/iAmVidyaGamer)
* [Vladimir Ivanov (@vivanov879)](https://github.com/vivanov879)

**Need Help?**

* Open an [issue](https://github.com/Unity-Technologies/marathon-envs/issues)
* Join our [Discord server](https://discord.gg/MPEbHPP) 
* Say hi on Twitter - [@iAmVidyaGamer](https://twitter.com/iAmVidyaGamer)

---
## Environments

| **Controller (DReCon)** - Preview |  |
|:----------|:----------|
| ![Controller](images/Controller.gif) | <p>A controller based agent, inspired by the DReCon paper (link below). The agent learns to follow a simple traditional controller agent and exhibits emergent behavior. **In Preview** </p> <p><ul><li>ControllerMarathonMan-v0</li></ul></p> |
| **Style Transfer** (DeepMimic) |  |
| <p>Learning from motion capture examples, inspired by the DeepMimic paper (link below). The agent learns the motion capture sequence using a phase value. <p><ul><li>MarathonManWalking-v0</li><li>MarathonManRunning-v0</li><li>arathonManJazzDancing-v0</li><li>MarathonManMMAKick-v0</li><li>MarathonManPunchingBag-v0</li><li>MarathonManBackflip-v0</li></ul></p> | ![StyleTransfer](images/StyleTransfer.gif) |
| **Procedural Environments** |  |
| ![Terrain](images/Terrain.gif) | <p>Procedurally-generated terrains aimed at addressing overfitting in Reinforcement Learning and generalizable skills.</p><p><ul><li>TerrainHopper-v0</li><li>TerrainWalker2d-v0</li><li>TerrainAnt-v0</li><li>TerrainMarathonMan-v0</li></p> |
| **Classical Environments** |  |
| <p>Classical implementations of Ant, Hopper, Walker-2d, Humanoid</p><p><ul><li>Hopper-v0</li><li>Walker2d-v0</li><li>Ant-v0</li><li>MarathonMan-v0</li></ul></p> | ![Classical](images/Classical.gif) |
| **Sparse - Experimental** |  |
| <p>Sparse reward version of a humanoid learning to walk. The agent recives a single reward at the end of the episode.</p><p><ul><li>MarathonManSparse-v0</li></ul></p> | |



---
## Releases

**The latest version is v3.0.0** 

The following table lists releases, the required unity version, and links to release note, source code, and binaries:

 **Version** | **Unity** | **Updated Environments** | **Source** | **MacOS** | **Windows** | **Linux** | **Web** | **Paper** |
|:-------:|:------------:|:-------------------:|:-------:|:--------:|:---------:|:---------:|:---------:|:---------:|
| **master (unstable)** | 2020.1 beta.12 | ControllerMarathonMan-v0 | -- | -- | -- | -- | -- | -- | -- |
| [**v3.0.0**](https://github.com/Unity-Technologies/marathon-envs/releases/tag/v3.0.0) | 2020.1 beta.12 | ControllerMarathonMan-v0 | [Source](https://github.com/Unity-Technologies/marathon-envs/tree/v3.0.0) | [MacOS](https://github.com/Unity-Technologies/marathon-envs/releases/download/v3.0.0/MarathonEnvsMacOS.zip) | -- | [Linux](https://github.com/Unity-Technologies/marathon-envs/releases/download/v3.0.0/MarathonEnvsLinux.zip) | [Web](http://marathonenvs.joebooth.com) | Coming Soon | 
| [**v2.0.0**](https://github.com/Unity-Technologies/marathon-envs/releases/tag/v2.0.0) | 2018.4 LTS | MarathonManWalking-v0 MarathonManRunning-v0 MarathonManJazzDancing-v0 MarathonManMMAKick-v0 MarathonManPunchingBag-v0  | [Source](https://github.com/Unity-Technologies/marathon-envs/tree/v2.0.0) | [MacOS](https://github.com/Unity-Technologies/marathon-envs/releases/download/v2.0.0/MarathonEnvsMacOS.zip) | -- | [Linux](https://github.com/Unity-Technologies/marathon-envs/releases/download/v2.0.0/MarathonEnvsLinux.zip) | -- | -- |
| [**v2.0.0-alpha.2**](https://github.com/Unity-Technologies/marathon-envs/releases/tag/v2.0.0-alpha.2) | 2018.4 LTS | -- | [Source](https://github.com/Unity-Technologies/marathon-envs/tree/v2.0.0-alpha.2) | [MacOS](https://github.com/Unity-Technologies/marathon-envs/releases/download/v2.0.0-alpha.2/MarathonEnvsMacOS.zip) | [Windows](https://github.com/Unity-Technologies/marathon-envs/releases/download/v2.0.0-alpha.2/MarathonEnvsWindows.zip) | [Linux](https://github.com/Unity-Technologies/marathon-envs/releases/download/v2.0.0-alpha.2/MarathonEnvsLinux.zip) | -- | [AAAI 2019](https://arxiv.org/abs/1902.09097) |
| [**v2.0.0-alpha.1**](https://github.com/Unity-Technologies/marathon-envs/releases/tag/v2.0.0-alpha.1) | 2018.4 LTS | MarathonManBackflip-v0 MarathonMan-v0 ManathonManSparse-v0 TerrainHopperEnv-v0, TerrainWalker2dEnv-v0, TerrainAntEnv-v0, TerrainMarathonManEnv-v0 | [Source](https://github.com/Unity-Technologies/marathon-envs/tree/v2.0.0-alpha.1) | -- | -- | -- | -- | -- |
| [**v0.5.0a**](https://github.com/Unity-Technologies/marathon-envs/releases/tag/0.5.0a) | 2018.2 | Hopper-v0, Walker2d-v0, Ant-v0, Humanoid-v0 | [Source](https://github.com/Unity-Technologies/marathon-envs/tree/0.5.0a) | -- | -- | -- | -- | [Blog](https://towardsdatascience.com/gettingstartedwithmarathonenvs-v0-5-0a-c1054a0b540c) |



---

## Getting Started

### Requirements

* Unity 2018.4 (Download [here](https://unity3d.com/get-unity/download)).
* Clone / Download this repro
* Install ml-agents version 0.14.1 - install via:

``` sh
pip3 install mlagents==0.14.1
```

* Build or install the correct runtime for your version into the `envs\` folder

### Training

* See [Training.md](Training.md) for training us ML-Agents

### Guides

* Video walkthrough:-

    [![Video walkthrough](https://img.youtube.com/vi/itUtkgCTma4/mqdefault.jpg)](https://www.youtube.com/watch?v=itUtkgCTma4) 
* Getting started with Marathon Environments v0.5.0a [**BLOG**](https://github.com/Unity-Technologies/marathon-envs/releases/tag/0.5.0a)

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

## References
* [DReCon: data-driven responsive control of physics-based characters](https://dl.acm.org/doi/10.1145/3355089.3356536) Insperation for ControllerMarathonMan environment.
* [DeepMimic: Example-Guided Deep Reinforcement Learning of Physics-Based Character Skills](https://arxiv.org/abs/1804.02717) Insperation for Style Transfer environments.
* [OpenAI.Gym Mujoco](https://github.com/openai/gym/tree/master/gym/envs/mujoco) implementation. Good reference for enviroment setup, reward functions and termination functions.
* [PyBullet pybullet_envs](https://pybullet.org) - a bit harder than MuJoCo gym environments but with an open source simulator. Pre-trained environments in [stable-baselines zoo](https://github.com/araffin/rl-baselines-zoo).
* [DeepMind Control Suite](https://github.com/deepmind/dm_control) - Set of continuous control tasks.
* DeepMind paper [Emergence of Locomotion Behaviours in Rich Environments](https://arxiv.org/pdf/1707.02286) and [video](https://youtu.be/hx_bgoTF7bs)- see page 13 b.2 for detail of reward functions
* [MuJoCo](http://www.mujoco.org) homepage.
* A good primer on the differences between physics engines is ['Physics simulation engines have traditional made tradeoffs between performance’](https://homes.cs.washington.edu/~todorov/papers/ErezICRA15.pdf) and it’s accompanying [video](https://homes.cs.washington.edu/~todorov/media/ErezICRA15.mp4).
* [MuJoCo Unity Plugin](http://www.mujoco.org/book/unity.html) MuJoCo's Unity plugin which uses socket to comunicate between MuJoCo (for running the physics simulation and control) and Unity (for rendering).

### Citing MarathonEnvs

If you use MarathonEnvs in your research, we ask that you please cite our [paper](https://arxiv.org/abs/1902.09097).


