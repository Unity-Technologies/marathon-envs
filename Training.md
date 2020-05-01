# Training

This page covers how to train using the ML-Agents toolkit

### Requirements

* Download the correct version of MarathonEnvs for your platform and put it in the `envs\` folder

### Note for MacOS & Linux

* You will need to replace the path `\` with `/`
* On MacOS
  * Replace `"envs\MarathonEnvs\Unity Environment.exe"` with `"envs/MarathonEnvs"`
* On Linux
  * Replace `"envs\MarathonEnvs\Unity Environment.exe"` with `"envs/MarathonEnvs/Unity Environment.x86_64"`

### Windows

#### Hopper-v0

``` shell
mlagents-learn config\marathon_envs_config.yaml --train --env="envs\MarathonEnvs\Unity Environment.exe" --num-envs=10 --run-id=Hopper538 --env-args --spawn-env=Hopper-v0 --num-spawn-envs=50 
```

#### Walker2d-v0

``` shell
mlagents-learn config\marathon_envs_config.yaml --train --env="envs\MarathonEnvs\Unity Environment.exe" --num-envs=10 --run-id=Walker2d-502 --env-args --spawn-env=Walker2d-v0 --num-spawn-envs=50 
```

#### Ant-v0

``` shell
mlagents-learn config\marathon_envs_config.yaml --train --env="envs\MarathonEnvs\Unity Environment.exe" --num-envs=1 --run-id=Ant401 --env-args --spawn-env=Ant-v0 --num-spawn-envs=100 
```

#### MarathonMan-v0

``` shell
mlagents-learn config\marathon_envs_config.yaml --train --env="envs\MarathonEnvs\Unity Environment.exe" --num-envs=1 --run-id=MarathonMan401 --env-args --spawn-env=MarathonMan-v0 --num-spawn-envs=100 
```

#### MarathonManSparse-v0

``` shell
mlagents-learn config\marathon_envs_config.yaml --train --env="envs\MarathonEnvs\Unity Environment.exe" --num-envs=1 --run-id=MarathonManSparse401 --env-args --spawn-env=MarathonManSparse-v0 --num-spawn-envs=100 
```

#### TerrainHopper-v0

``` shell
mlagents-learn config\marathon_envs_config.yaml --train --env="envs\MarathonEnvs\Unity Environment.exe" --num-envs=1 --run-id=TerrainHopper401 --env-args --spawn-env=TerrainHopper-v0 --num-spawn-envs=100 
```

#### TerrainWalker2d-v0

``` shell
mlagents-learn config\marathon_envs_config.yaml --train --env="envs\MarathonEnvs\Unity Environment.exe" --num-envs=1 --run-id=TerrainWalker2d401 --env-args --spawn-env=TerrainWalker2d-v0 --num-spawn-envs=100
```

#### TerrainAnt-v0

``` shell
mlagents-learn config\marathon_envs_config.yaml --train --env="envs\MarathonEnvs\Unity Environment.exe" --num-envs=1 --run-id=TerrainAnt401 --env-args --spawn-env=TerrainAnt-v0 --num-spawn-envs=100 
```

#### TerrainMarathonMan-v0

``` shell
mlagents-learn config\marathon_envs_config.yaml --train --env="envs\MarathonEnvs\Unity Environment.exe" --num-envs=1 --run-id=TerrainMarathonMan401 --env-args --spawn-env=TerrainMarathonMan-v0 --num-spawn-envs=100 
```


#### MarathonManBackflip-v0

``` shell
mlagents-learn config\marathon_envs_config.yaml --train --env="envs\MarathonEnvs\Unity Environment.exe" --num-envs=4 --no-graphics --run-id=MarathonManBackflip --env-args --spawn-env=MarathonManBackflip-v0 --num-spawn-envs=64
```
----

## Legacy Turotial: [Getting Started With MarathonEnvs](https://towardsdatascience.com/gettingstartedwithmarathonenvs-v0-5-0a-c1054a0b540c)

This is a legacy tutorial from an older version of MarathonEnvs. The tutorial covers:

* How to setup your Development Environment (Unity, MarthonEnvs + ML-Agents + TensorflowSharp)
* How to run each agent with their pre-trained models.
* How to retrain the hopper agent and follow training in Tensorboard.
* How to modify the hopper reward function and train it to jump.
* See tutorial [here](https://towardsdatascience.com/gettingstartedwithmarathonenvs-v0-5-0a-c1054a0b540c)
