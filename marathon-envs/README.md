# Marathon-Envs python wrapper

## Installation

The gym wrapper can be installed using:

```sh
pip install marathon_envs
```

or by running the following from the `/marathon-envs` directory of the repository:

```sh
pip install .
```

## Using Marathon-Envs

The gym interface is available from `marathon-envs.envs`. To launch an environment
from the root of the project repository use:

```python
from marathon-envs.envs import MarathonEnvs

env = MarathonEnvs(environment_name, worker_id, use_visual, uint8_visual, multiagent)
```

*  `environment_name` refers to the path to the Unity environment.

*  `worker_id` refers to the port to use for communication with the environment.
   Defaults to `0`.

The returned environment `env` will function as a gym environment.


## Limitations

* Environment registration for use with `gym.make()` is currently not supported.

## Running OpenAI Baselines Algorithms

* TBD