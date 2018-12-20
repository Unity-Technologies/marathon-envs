using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAgents;

public class DeepMindHumanoidAgent : MarathonAgent
{
    public override void AgentReset()
    {
        base.AgentReset();

        // set to true this to show monitor while training
        Monitor.SetActive(true);

        StepRewardFunction = StepRewardDeepMindHumanoid101;
        TerminateFunction = TerminateOnNonFootHitTerrain;
        ObservationsFunction = ObservationsHumanoid;

        BodyParts["head"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x => x.name == "head");
        BodyParts["shoulders"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x => x.name == "torso");
        BodyParts["waist"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x => x.name == "lower_waist");
        BodyParts["pelvis"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x => x.name == "butt");
        BodyParts["left_thigh"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x => x.name == "left_thigh");
        BodyParts["right_thigh"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x => x.name == "right_thigh");
        BodyParts["left_uarm"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x => x.name == "left_upper_arm");
        BodyParts["right_uarm"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x => x.name == "right_upper_arm");

        base.SetupBodyParts();

        // set up phase 
        PhaseBonusInitalize();
    }


    public override void AgentOnDone()
    {
    }

    void ObservationsHumanoid()
    {
        if (ShowMonitor)
        {
        }

        var pelvis = BodyParts["pelvis"];
        var shoulders = BodyParts["shoulders"];

        AddVectorObs(pelvis.velocity);
        AddVectorObs(pelvis.transform.forward); // gyroscope 
        AddVectorObs(pelvis.transform.up);

        AddVectorObs(shoulders.transform.forward); // gyroscope 
        AddVectorObs(shoulders.transform.up);

        AddVectorObs(SensorIsInTouch);
        JointRotations.ForEach(x => AddVectorObs(x));
        AddVectorObs(JointVelocity);
    }


    float GetHumanoidArmEffort()
    {
        var mJoints = MarathonJoints
            .Where(x => x.JointName.ToLowerInvariant().Contains("shoulder") ||
                        x.JointName.ToLowerInvariant().Contains("elbow"))
            .ToList();
        var effort = mJoints
            .Select(x => Actions[MarathonJoints.IndexOf(x)])
            .Select(x => Mathf.Pow(Mathf.Abs(x), 2))
            .Sum();
        return effort;
    }

    float StepRewardDeepMindHumanoid101()
    {
        _velocity = GetVelocity();
        _heightPenality = GetHeightPenality(1.2f);
        _uprightBonus =
            (GetUprightBonus("shoulders") / 6)
            + (GetUprightBonus("waist") / 6)
            + (GetUprightBonus("pelvis") / 6);
        _forwardBonus =
            (GetForwardBonus("shoulders") / 4)
            + (GetForwardBonus("waist") / 6)
            + (GetForwardBonus("pelvis") / 6);

        float leftThighPenality = Mathf.Abs(GetLeftBonus("left_thigh"));
        float rightThighPenality = Mathf.Abs(GetRightBonus("right_thigh"));
        _limbPenalty = leftThighPenality + rightThighPenality;
        _limbPenalty = Mathf.Min(0.5f, _limbPenalty);
        _finalPhaseBonus = GetPhaseBonus();
        _jointsAtLimitPenality = GetJointsAtLimitPenality() * 4;
        float effort = GetEffort(new string[] {"right_hip_y", "right_knee", "left_hip_y", "left_knee"});
        _effortPenality = 0.05f * (float) effort;
        _velocity /= 10f;
        _finalPhaseBonus *= 10f;
        _reward = _velocity
                     + _uprightBonus
                     + _forwardBonus
                     + _finalPhaseBonus
                     - _heightPenality
                     - _limbPenalty
                     - _jointsAtLimitPenality
                     - _effortPenality;
        if (ShowMonitor)
        {
            var hist = new[]
            {
                _reward, _velocity,
                _uprightBonus,
                _forwardBonus,
                _phaseBonus,
                -_heightPenality,
                -_limbPenalty,
                -_jointsAtLimitPenality,
                -_effortPenality
            }.ToList();
            Monitor.Log("rewardHist", hist.ToArray());
        }

        return _reward;
    }

    float StepRewardOaiHumanoidRunOnSpot161()
    {
        float heightPenality = GetHeightPenality(1.2f);
        _uprightBonus =
            (GetUprightBonus("shoulders") / 6)
            + (GetUprightBonus("waist") / 6)
            + (GetUprightBonus("pelvis") / 6);
        _forwardBonus =
            (GetForwardBonus("shoulders") / 2)
            + (GetForwardBonus("waist") / 6)
            + (GetForwardBonus("pelvis") / 6);

        float leftThighPenality = Mathf.Abs(GetLeftBonus("left_thigh"));
        float rightThighPenality = Mathf.Abs(GetRightBonus("right_thigh"));
        _limbPenalty = leftThighPenality + rightThighPenality;
        _limbPenalty = Mathf.Min(0.5f, _limbPenalty);
        _finalPhaseBonus = GetPhaseBonus() * 5;
        _jointsAtLimitPenality = GetJointsAtLimitPenality() * 4;
        float effort = GetEffort(new string[] {"right_hip_y", "right_knee", "left_hip_y", "left_knee"});
        _effortPenality = 0.5f * (float) effort;
        var reward = 0
                     + _uprightBonus
                     + _forwardBonus
                     + _finalPhaseBonus
                     - _heightPenality
                     - _limbPenalty
                     - _jointsAtLimitPenality
                     - _effortPenality;
        if (ShowMonitor)
        {
            var hist = new[]
            {
                reward,
                // velocity, 
                _uprightBonus,
                _forwardBonus,
                _finalPhaseBonus,
                -_heightPenality,
                -_limbPenalty,
                -_jointsAtLimitPenality,
                -_effortPenality
            }.ToList();
            Monitor.Log("rewardHist", hist.ToArray());
        }

        return reward;
    }

    bool TerminateHumanoid()
    {
        if (TerminateOnNonFootHitTerrain())
            return true;
        var height = GetHeightPenality(.9f);
        var angle = GetForwardBonus("pelvis");
        bool endOnHeight = height > 0f;
        bool endOnAngle = (angle < .25f);
        return endOnHeight || endOnAngle;
    }

    // implement phase bonus (reward for left then right)
    List<bool> _lastSenorState;

    public float _phaseBonus;
    public int _phase;
    public float _reward;
    public float _velocity;
    public float _uprightBonus;
    public float _forwardBonus;
    public float _finalPhaseBonus;
    public float _heightPenality;
    public float _limbPenalty;
    public float _jointsAtLimitPenality;
    public float _effortPenality;


    public float LeftMin;
    public float LeftMax;

    public float RightMin;
    public float RightMax;

    void PhaseBonusInitalize()
    {
        _lastSenorState = Enumerable.Repeat<bool>(false, NumSensors).ToList();
        _phase = 0;
        _phaseBonus = 0f;
        PhaseResetLeft();
        PhaseResetRight();
    }

    void PhaseResetLeft()
    {
        LeftMin = float.MaxValue;
        LeftMax = float.MinValue;
        PhaseSetLeft();
    }

    void PhaseResetRight()
    {
        RightMin = float.MaxValue;
        RightMax = float.MinValue;
        PhaseSetRight();
    }

    void PhaseSetLeft()
    {
        var inPhaseToFocalAngle = BodyPartsToFocalRoation["left_thigh"] * BodyParts["left_thigh"].transform.right;
        var inPhaseAngleFromUp = Vector3.Angle(inPhaseToFocalAngle, Vector3.up);

        var angle = 180 - inPhaseAngleFromUp;
        var qpos2 = (angle % 180) / 180;
        var bonus = 2 - (Mathf.Abs(qpos2) * 2) - 1;
        LeftMin = Mathf.Min(LeftMin, bonus);
        LeftMax = Mathf.Max(LeftMax, bonus);
    }

    void PhaseSetRight()
    {
        var inPhaseToFocalAngle = BodyPartsToFocalRoation["right_thigh"] * BodyParts["right_thigh"].transform.right;
        var inPhaseAngleFromUp = Vector3.Angle(inPhaseToFocalAngle, Vector3.up);

        var angle = 180 - inPhaseAngleFromUp;
        var qpos2 = (angle % 180) / 180;
        var bonus = 2 - (Mathf.Abs(qpos2) * 2) - 1;
        RightMin = Mathf.Min(RightMin, bonus);
        RightMax = Mathf.Max(RightMax, bonus);
    }

    float CalcPhaseBonus(float min, float max)
    {
        float bonus = 0f;
        if (min < 0f && max < 0f)
        {
            min = Mathf.Abs(min);
            max = Mathf.Abs(max);
        }
        else if (min < 0f)
        {
            bonus = Mathf.Abs(min);
            min = 0f;
        }

        bonus += max - min;
        return bonus;
    }

    float GetPhaseBonus()
    {
        bool noPhaseChange = true;
        bool isLeftFootDown = SensorIsInTouch[0] > 0f || SensorIsInTouch[1] > 0f;
        bool isRightFootDown = SensorIsInTouch[2] > 0f || SensorIsInTouch[3] > 0f;
        bool wasLeftFootDown = _lastSenorState[0];
        bool wasRightFootDown = _lastSenorState[1];
        noPhaseChange = noPhaseChange && isLeftFootDown == wasLeftFootDown;
        noPhaseChange = noPhaseChange && isRightFootDown == wasRightFootDown;
        _lastSenorState[0] = isLeftFootDown;
        _lastSenorState[1] = isRightFootDown;
        if (isLeftFootDown && isRightFootDown)
        {
            _phase = 0;
            _phaseBonus = 0f;
            PhaseResetLeft();
            PhaseResetRight();
            return _phaseBonus;
        }

        PhaseSetLeft();
        PhaseSetRight();

        if (noPhaseChange)
        {
            var bonus = _phaseBonus;
            _phaseBonus *= 0.9f;
            return bonus;
        }

        // new phase
        _phaseBonus = 0;
        if (isLeftFootDown)
        {
            if (_phase == 1) {
                _phaseBonus = 0f;
            }
            else {
                _phaseBonus = CalcPhaseBonus(LeftMin, LeftMax);
                _phaseBonus += 0.1f;
            }
            _phase = 1;
            PhaseResetLeft();
        }
        else if (isRightFootDown)
        {
            if (_phase == 2) {
                _phaseBonus = 0f;
            }
            else {
                _phaseBonus = CalcPhaseBonus(RightMin, RightMax);
                _phaseBonus += 0.1f;
            }
            _phase = 2;
            PhaseResetRight();
        }

        return _phaseBonus;
    }
}