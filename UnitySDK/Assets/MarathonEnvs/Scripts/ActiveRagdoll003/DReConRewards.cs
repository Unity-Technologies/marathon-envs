using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;
using UnityEngine.Assertions;
public class DReConRewards : MonoBehaviour
{
    [Header("Reward")]
    public float SumOfSubRewards;
    public float Reward;

    [Header("Position Reward")]
    public float SumOfDistances;
    public float SumOfSqrDistances;
    public float PositionReward;

    [Header("Velocity Reward")]
    public float MocapPointsVelocity;
    public float RagDollPointsVelocity;    
    public float PointsVelocityDifferenceSquared;
    public float PointsVelocityReward;

    [Header("Local Pose Reward")]
    public List<float> RotationDifferences;
    public float SumOfRotationDifferences;
    public float SumOfRotationSqrDifferences;
    public float LocalPoseReward;

   
    
    [Header("Center of Mass Velocity Reward")]
    public Vector3 MocapCOMVelocity;
    public Vector3 RagDollCOMVelocity;
    public float MocapCOMVelocityMagnitude;
    public float RagDollCOMVelocityMagnitude;

    public float COMVelocityDifference;
    public float ComReward;


//  fall factor
    [Header("Fall Factor")]
    public float HeadHeightDistance;
    public float FallFactor;

    [Header("Misc")]
    public float HeadDistance;

    [Header("Gizmos")]
    public int ObjectForPointDistancesGizmo;

    SpawnableEnv _spawnableEnv;
    GameObject _mocap;
    GameObject _ragDoll;

    internal DReConRewardStats _mocapBodyStats;
    internal DReConRewardStats _ragDollBodyStats;

    // List<ArticulationBody> _mocapBodyParts;
    // List<ArticulationBody> _ragDollBodyParts;
    Transform _mocapHead;
    Transform _ragDollHead;

    bool _hasLazyInit;

    void Awake()
    {
        _spawnableEnv = GetComponentInParent<SpawnableEnv>();
        Assert.IsNotNull(_spawnableEnv);
        _mocap = _spawnableEnv.GetComponentInChildren<MocapController>().gameObject;
        _ragDoll = _spawnableEnv.GetComponentInChildren<RagDollAgent>().gameObject;
        Assert.IsNotNull(_mocap);
        Assert.IsNotNull(_ragDoll);
        // _mocapBodyParts = _mocap.GetComponentsInChildren<ArticulationBody>().ToList();
        // _ragDollBodyParts = _ragDoll.GetComponentsInChildren<ArticulationBody>().ToList();
        // Assert.AreEqual(_mocapBodyParts.Count, _ragDollBodyParts.Count);
        _mocapHead = _mocap
            .GetComponentsInChildren<Transform>()
            .First(x=>x.name == "head");
        _ragDollHead = _ragDoll
            .GetComponentsInChildren<Transform>()
            .First(x=>x.name == "head");
        _mocapBodyStats= new GameObject("MocapDReConRewardStats").AddComponent<DReConRewardStats>();
        var mocapController = _spawnableEnv.GetComponentInChildren<MocapController>();
        _mocapBodyStats.ObjectToTrack = mocapController;
        _mocapBodyStats.transform.SetParent(_spawnableEnv.transform);
        _mocapBodyStats.OnAwake(_mocapBodyStats.ObjectToTrack.transform);

        _ragDollBodyStats= new GameObject("RagDollDReConRewardStats").AddComponent<DReConRewardStats>();
        _ragDollBodyStats.ObjectToTrack = this;
        _ragDollBodyStats.transform.SetParent(_spawnableEnv.transform);
        _ragDollBodyStats.OnAwake(transform, _mocapBodyStats);      

        _mocapBodyStats.AssertIsCompatible(_ragDollBodyStats);      
    }

    // Update is called once per frame
    public void OnStep(float timeDelta)
    {
        _mocapBodyStats.SetStatusForStep(timeDelta);
        _ragDollBodyStats.SetStatusForStep(timeDelta);

        // position reward
        List<float> distances = _mocapBodyStats.GetPointDistancesFrom(_ragDollBodyStats);
        PositionReward = -7.37f/(distances.Count/6f);
        List<float> sqrDistances = distances.Select(x=> x*x).ToList();
        SumOfDistances = distances.Sum();
        SumOfSqrDistances = sqrDistances.Sum();
        PositionReward *= SumOfSqrDistances;
        PositionReward = Mathf.Exp(PositionReward);

        // center of mass velocity reward
        MocapCOMVelocity = _mocapBodyStats.CenterOfMassVelocity;
        RagDollCOMVelocity = _ragDollBodyStats.CenterOfMassVelocity;
        MocapCOMVelocityMagnitude = MocapCOMVelocity.magnitude;
        RagDollCOMVelocityMagnitude = RagDollCOMVelocity.magnitude;
        COMVelocityDifference = (MocapCOMVelocity-RagDollCOMVelocity).magnitude;
        ComReward = -Mathf.Pow(COMVelocityDifference,2);
        ComReward = Mathf.Exp(ComReward);

        // points velocity
        MocapPointsVelocity = _mocapBodyStats.PointVelocity.Sum();
        RagDollPointsVelocity = _ragDollBodyStats.PointVelocity.Sum();
        var pointsDifference = _mocapBodyStats.PointVelocity
            .Zip(_ragDollBodyStats.PointVelocity, (a,b )=> (a-b) * (a-b));
        PointsVelocityDifferenceSquared = pointsDifference.Sum();
        PointsVelocityReward = PointsVelocityDifferenceSquared;
        PointsVelocityReward = (-1f/_mocapBodyStats.PointVelocity.Length) * PointsVelocityReward;
        PointsVelocityReward = Mathf.Exp(PointsVelocityReward);

        // local pose reward
        if (RotationDifferences == null || RotationDifferences.Count < _mocapBodyStats.Rotations.Count)
            RotationDifferences = Enumerable.Range(0,_mocapBodyStats.Rotations.Count)
            .Select(x=>0f)
            .ToList();
        SumOfRotationDifferences = 0f;
        SumOfRotationSqrDifferences = 0f;
        for (int i = 0; i < _mocapBodyStats.Rotations.Count; i++)
        { 
            var angle = Quaternion.Angle(_mocapBodyStats.Rotations[i], _ragDollBodyStats.Rotations[i]);
            Assert.IsTrue(angle <= 180f);
            angle = DReConObservationStats.NormalizedAngle(angle);
            var sqrAngle = angle * angle;
            RotationDifferences[i] = angle;
            SumOfRotationDifferences += angle;
            SumOfRotationSqrDifferences += sqrAngle;
        }
        LocalPoseReward = -6.5f/RotationDifferences.Count;
        LocalPoseReward *= SumOfRotationSqrDifferences;
        LocalPoseReward = Mathf.Exp(LocalPoseReward);

        // fall factor
        HeadDistance = (_mocapHead.position - _ragDollHead.position).magnitude;
        FallFactor = Mathf.Pow(HeadDistance,2);
        FallFactor = 1.4f*FallFactor;
        FallFactor = 1.3f-FallFactor;
        FallFactor = Mathf.Clamp(FallFactor, 0f, 1f);

        HeadHeightDistance = (_mocapHead.position.y - _ragDollHead.position.y);
        HeadHeightDistance = Mathf.Abs(HeadHeightDistance);

        // reward
        SumOfSubRewards = PositionReward+ComReward+PointsVelocityReward+LocalPoseReward;
        Reward = FallFactor*SumOfSubRewards;
    }
    public void OnReset()
    {
        _mocapBodyStats.OnReset();
        _ragDollBodyStats.OnReset();
        _ragDollBodyStats.transform.position = _mocapBodyStats.transform.position;
        _ragDollBodyStats.transform.rotation = _mocapBodyStats.transform.rotation;
    }
    void OnDrawGizmos()
    {
        if (_ragDollBodyStats == null)
            return;
        var max = (_ragDollBodyStats.Points.Length/6)-1;
        ObjectForPointDistancesGizmo = Mathf.Clamp(ObjectForPointDistancesGizmo, -1, max);
        _mocapBodyStats.DrawPointDistancesFrom(_ragDollBodyStats, ObjectForPointDistancesGizmo);
    }
}
