# Training

This page covers how to train using the ML-Agents toolkit

### Requirements

* Download the correct version of MarathonEnvs for your platform and put it in the `envs\` folder

### MacOS & Linux

* Replace the path `\` with `/`
* Replace `"envs\MarathonEnvs\Unity Environment.exe"` with `"envs\MarathonEnvs"`

### Windows

#### Hopper-v0

``` shell
mlagents-learn config\marathon_envs_config.yaml --train --env="envs\MarathonEnvs\Unity Environment.exe" --spawn-env=Hopper-v0 --num-spawn-envs=50 --num-envs=10 --run-id=Hopper538
```

#### Walker2d-v0

``` shell
mlagents-learn config\marathon_envs_config.yaml --train --env="envs\MarathonEnvs\Unity Environment.exe" --spawn-env=Walker2d-v0 --num-spawn-envs=100 --num-envs=1 --run-id=Walker2d401
```

#### Ant-v0

``` shell
mlagents-learn config\marathon_envs_config.yaml --train --env="envs\MarathonEnvs\Unity Environment.exe" --spawn-env=Ant-v0 --num-spawn-envs=100 --num-envs=1 --run-id=Ant401
```

#### MarathonMan-v0

``` shell
mlagents-learn config\marathon_envs_config.yaml --train --env="envs\MarathonEnvs\Unity Environment.exe" --spawn-env=MarathonMan-v0 --num-spawn-envs=100 --num-envs=1 --run-id=MarathonMan401
```

#### MarathonManSparse-v0

``` shell
mlagents-learn config\marathon_envs_config.yaml --train --env="envs\MarathonEnvs\Unity Environment.exe" --spawn-env=MarathonManSparse-v0 --num-spawn-envs=100 --num-envs=1 --run-id=MarathonManSparse401
```

#### TerrainHopper-v0

``` shell
mlagents-learn config\marathon_envs_config.yaml --train --env="envs\MarathonEnvs\Unity Environment.exe" --spawn-env=TerrainHopper-v0 --num-spawn-envs=100 --num-envs=1 --run-id=TerrainHopper401
```

#### TerrainWalker2d-v0

``` shell
mlagents-learn config\marathon_envs_config.yaml --train --env="envs\MarathonEnvs\Unity Environment.exe" --spawn-env=TerrainWalker2d-v0 --num-spawn-envs=100 --num-envs=1 --run-id=TerrainWalker2d401
```

#### TerrainAnt-v0

``` shell
mlagents-learn config\marathon_envs_config.yaml --train --env="envs\MarathonEnvs\Unity Environment.exe" --spawn-env=TerrainAnt-v0 --num-spawn-envs=100 --num-envs=1 --run-id=TerrainAnt401
```

#### TerrainMarathonMan-v0

``` shell
mlagents-learn config\marathon_envs_config.yaml --train --env="envs\MarathonEnvs\Unity Environment.exe" --spawn-env=TerrainMarathonMan-v0 --num-spawn-envs=100 --num-envs=1 --run-id=TerrainMarathonMan401
```

----

## Legacy Turotial: [Getting Started With MarathonEnvs](https://towardsdatascience.com/gettingstartedwithmarathonenvs-v0-5-0a-c1054a0b540c)

This is a legacy tutorial from an older version of MarathonEnvs. The tutorial covers:

* How to setup your Development Environment (Unity, MarthonEnvs + ML-Agents + TensorflowSharp)
* How to run each agent with their pre-trained models.
* How to retrain the hopper agent and follow training in Tensorboard.
* How to modify the hopper reward function and train it to jump.
* See tutorial [here](https://towardsdatascience.com/gettingstartedwithmarathonenvs-v0-5-0a-c1054a0b540c)
