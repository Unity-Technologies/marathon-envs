#!/usr/bin/env python

import os
import sys
from setuptools import setup, find_packages
from setuptools.command.install import install
import marathon_envs

VERSION = marathon_envs.__version__


# class VerifyVersionCommand(install):
#     """
#     Custom command to verify that the git tag matches our version
#     See https://circleci.com/blog/continuously-deploying-python-packages-to-pypi-with-circleci/
#     """

#     description = "verify that the git tag matches our version"

#     def run(self):
#         tag = os.getenv("CIRCLE_TAG")

#         if tag != VERSION:
#             info = "Git tag: {0} does not match the version of this app: {1}".format(
#                 tag, VERSION
#             )
#             sys.exit(info)


setup(
    name="marathon_envs",
    version=VERSION,
    description="Marathon Envs Gym Interface",
    license="Apache License 2.0",
    author="Joe Booth",
    author_email="joe@joebooth.com",
    url="https://github.com/Unity-Technologies/marathon-envs",
    packages=find_packages(),
    install_requires=["gym", "mlagents_envs=={}".format(VERSION)],
    # cmdclass={"verify": VerifyVersionCommand},
)
