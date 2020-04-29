import gym
import numpy as np

from stable_baselines.sac.policies import MlpPolicy
from stable_baselines import SAC

from marathon_envs.envs import MarathonEnvs

from stable_baselines.sac.policies import FeedForwardPolicy
from stable_baselines.common.vec_env import DummyVecEnv

from timeit import default_timer as timer
from datetime import timedelta
import time
import os

env_names = [
    'Hopper-v0', 
    # 'Walker2d-v0', 
    # 'Ant-v0', 
    # 'MarathonMan-v0', 
    # 'MarathonManSparse-v0'
    ]
for env_name in env_names:
    print ('-------', env_name, '-------')
    env = MarathonEnvs(env_name, 1)
    model = SAC.load(os.path.join('models', "sac_"+env_name))

    obs = env.reset()
    episode_score = 0.
    episode_steps = 0
    episodes = 0
    while episodes < 5:
        action, _states = model.predict(obs)
        obs, rewards, dones, info = env.step(action)
        episode_score += rewards
        episode_steps += 1
        env.render()
        time.sleep(1./60)
        if dones:
            print ('episode_score', episode_score, 'episode_steps', episode_steps)
            episode_score = 0.
            episode_steps = 0
            episodes += 1
    env.close()