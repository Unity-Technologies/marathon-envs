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
        BodyParts["left_foot_left"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x => x.name == "left_left_foot");
        BodyParts["left_foot_right"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x => x.name == "right_left_foot");
        BodyParts["right_foot_left"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x => x.name == "right_right_foot");
        BodyParts["right_foot_right"] = GetComponentsInChildren<Rigidbody>().FirstOrDefault(x => x.name == "left_right_foot");


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

        Vector3 normalizedVelocity = this.GetNormalizedVelocity(pelvis.velocity);
        AddVectorObs(normalizedVelocity);
        AddVectorObs(pelvis.transform.forward); // gyroscope 
        AddVectorObs(pelvis.transform.up);

        AddVectorObs(shoulders.transform.forward); // gyroscope 
        AddVectorObs(shoulders.transform.up);

        AddVectorObs(SensorIsInTouch);
        JointRotations.ForEach(x => AddVectorObs(x));
        AddVectorObs(JointVelocity);
        AddVectorObs(new []{
            this.GetNormalizedPosition(BodyParts["left_foot_left"].transform.position).y,
            this.GetNormalizedPosition(BodyParts["left_foot_right"].transform.position).y,
            this.GetNormalizedPosition(BodyParts["right_foot_left"].transform.position).y,
            this.GetNormalizedPosition(BodyParts["right_foot_right"].transform.position).y,
            });

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
        _velocity = Mathf.Clamp(GetNormalizedVelocity("pelvis").x, 0f, 1f);
        _heightBonus = 1f-GetHeightPenality(1.2f);
        _uprightBonus =
            (GetUprightBonus("shoulders", 1f / 3))
            + (GetUprightBonus("waist", 1f / 3))
            + (GetUprightBonus("pelvis", 1f / 3));
        _forwardBonus =
            (GetForwardBonus("shoulders", 1f / 3))
            + (GetForwardBonus("waist", 1f / 3))
            + (GetForwardBonus("pelvis", 1f / 3));

        float leftThighPenality = Mathf.Abs(GetLeftBonus("left_thigh", .5f));
        float rightThighPenality = Mathf.Abs(GetRightBonus("right_thigh", .5f));
        _limbBonus = 1f-(leftThighPenality + rightThighPenality);
        _finalPhaseBonus = GetPhaseBonus();
        // _jointsAtLimitPenality = GetJointsAtLimitPenality() * 4;
        _effort = 1f - GetEffortNormalized(new string[] {"right_hip_y", "right_knee", "left_hip_y", "left_knee"});
        
        _velocity = Mathf.Clamp(_velocity, 0f, 1f);
        _uprightBonus = Mathf.Clamp(_uprightBonus, 0f, 1f);
        _forwardBonus = Mathf.Clamp(_forwardBonus, 0f, 1f);
        _finalPhaseBonus = Mathf.Clamp(_finalPhaseBonus, 0f, 1f);
        _heightBonus = Mathf.Clamp(_heightBonus, 0f, 1f);
        _limbBonus = Mathf.Clamp(_limbBonus, 0f, 1f);
        _effort = Mathf.Clamp(_effort, 0f, 1f);

        var velocity = _velocity * 0.4f;
        var effort = _effort;
        if (velocity >= .4f)
            effort *= 0.4f;
        else
            effort *= velocity;
        var uprightBonus = _uprightBonus * 0.02f;
        var forwardBonus = _forwardBonus * 0.02f;
        var finalPhaseBonus = _finalPhaseBonus * 0.02f;
        var heightBonus = _heightBonus * 0.02f;
        var limbBonus = _limbBonus * 0.02f;

        _reward =   velocity 
                    + uprightBonus
                    + forwardBonus
                    + finalPhaseBonus
                    + heightBonus
                    + limbBonus
                    + effort;

        if (ShowMonitor)
        {
            var hist = new[]
            {
                _reward, _velocity,
                _uprightBonus,
                _forwardBonus,
                _phaseBonus,
                _heightBonus,
                _limbBonus,
                _effort
            }.ToList();
            Monitor.Log("rewardHist", hist.ToArray(), displayType: Monitor.DisplayType.INDEPENDENT);
        }

        return _reward;
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
    public float _heightBonus;
    public float _limbBonus;
    public float _effort;


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