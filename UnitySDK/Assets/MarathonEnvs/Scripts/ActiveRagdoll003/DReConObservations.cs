using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;

public class DReConObservations : MonoBehaviour
{
    [Header("Observations")]

    [Tooltip("Kinematic character center of mass velocity, Vector3")]
    public Vector3 MocapCOMVelocity;

    [Tooltip("RagDoll character center of mass velocity, Vector3")]
    public Vector3 RagDollCOMVelocity;

    [Tooltip("User-input desired horizontal CM velocity. Vector2")]
    public Vector2 InputDesiredHorizontalVelocity;

    [Tooltip("User-input requests jump, bool")]
    public bool InputJump;

    [Tooltip("User-input requests backflip, bool")]
    public bool InputBackflip;

    [Tooltip("Difference between RagDoll character horizontal CM velocity and user-input desired horizontal CM velocity. Vector2")]
    public Vector2 HorizontalVelocityDifference;

    [Tooltip("Positions and velocities for subset of bodies")]
    public List<BodyPartDifferenceStats> BodyPartDifferenceStats;
    public List<DReConObservationStats.Stat> MocapBodyStats;
    public List<DReConObservationStats.Stat> RagDollBodyStats;

    [Tooltip("Smoothed actions produced in the previous step of the policy are collected in t −1")]
    public float[] PreviousActions;

    [Header("Settings")]
    public List<string> BodyPartsToTrack;

    [Header("Gizmos")]
    public bool VelocityInWorldSpace = true;
    public bool PositionInWorldSpace = true;


    InputController _inputController;
    SpawnableEnv _spawnableEnv;
    DReConObservationStats _mocapBodyStats;
    DReConObservationStats _ragDollBodyStats;


    // Start is called before the first frame update
    void Awake()
    {
        _spawnableEnv = GetComponentInParent<SpawnableEnv>();
        _inputController = _spawnableEnv.GetComponentInChildren<InputController>();
        BodyPartDifferenceStats = BodyPartsToTrack
            .Select(x=> new BodyPartDifferenceStats{Name = x})
            .ToList();

        _mocapBodyStats= new GameObject("MocapDReConObservationStats").AddComponent<DReConObservationStats>();
        var mocapController = _spawnableEnv.GetComponentInChildren<MocapController>();
        _mocapBodyStats.ObjectToTrack = mocapController;
        _mocapBodyStats.transform.SetParent(_spawnableEnv.transform);
        _mocapBodyStats.OnAwake(BodyPartsToTrack, _mocapBodyStats.ObjectToTrack.transform);

        _ragDollBodyStats= new GameObject("RagDollDReConObservationStats").AddComponent<DReConObservationStats>();
        _ragDollBodyStats.ObjectToTrack = this;
        _ragDollBodyStats.transform.SetParent(_spawnableEnv.transform);
        _ragDollBodyStats.OnAwake(BodyPartsToTrack, transform);
    }

    public void OnStep(float timeDelta)
    {
        _mocapBodyStats.SetStatusForStep(timeDelta);
        _ragDollBodyStats.SetStatusForStep(timeDelta);
        UpdateObservations(timeDelta);
    }
    public void OnReset()
    {
        _mocapBodyStats.OnReset();
        _ragDollBodyStats.OnReset();
        _ragDollBodyStats.transform.position = _mocapBodyStats.transform.position;
        _ragDollBodyStats.transform.rotation = _mocapBodyStats.transform.rotation;
        var timeDelta = float.MinValue;
        UpdateObservations(timeDelta);
    }

    public void UpdateObservations(float timeDelta)
    {

        MocapCOMVelocity = _mocapBodyStats.CenterOfMassVelocity;
        RagDollCOMVelocity = _ragDollBodyStats.CenterOfMassVelocity;
        InputDesiredHorizontalVelocity = new Vector2(
            _ragDollBodyStats.DesiredCenterOfMassVelocity.x, 
            _ragDollBodyStats.DesiredCenterOfMassVelocity.z);
        InputJump = _inputController.Jump;
        InputBackflip = _inputController.Backflip;
        HorizontalVelocityDifference = new Vector2(
            _ragDollBodyStats.CenterOfMassVelocityDifference.x,
            _ragDollBodyStats.CenterOfMassVelocityDifference.z);

        MocapBodyStats = BodyPartsToTrack
            .Select(x=>_mocapBodyStats.Stats.First(y=>y.Name == x))
            .ToList();
        RagDollBodyStats = BodyPartsToTrack
            .Select(x=>_ragDollBodyStats.Stats.First(y=>y.Name == x))
            .ToList();
        // BodyPartStats = 
        foreach (var differenceStats in BodyPartDifferenceStats)
        {
            var mocapStats = _mocapBodyStats.Stats.First(x=>x.Name == differenceStats.Name);
            var ragDollStats = _ragDollBodyStats.Stats.First(x=>x.Name == differenceStats.Name);

            differenceStats.Position = mocapStats.Position - ragDollStats.Position;
            differenceStats.Velocity = mocapStats.Velocity - ragDollStats.Velocity;
            differenceStats.AngualrVelocity = mocapStats.AngualrVelocity - ragDollStats.AngualrVelocity;
            differenceStats.Rotation = DReConObservationStats.GetAngularVelocity(mocapStats.Rotation, ragDollStats.Rotation, timeDelta);
        }
    }
    public Transform GetRagDollCOM()
    {
        return _ragDollBodyStats.transform;
    }
    void OnDrawGizmos()
    {
        if (_mocapBodyStats == null)
            return;        
        // MocapCOMVelocity
        Vector3 pos = new Vector3(transform.position.x, .3f, transform.position.z);
        Vector3 vector = MocapCOMVelocity;
        if (VelocityInWorldSpace)
            vector = _mocapBodyStats.transform.TransformVector(vector);
        DrawArrow(pos, vector, Color.grey);

        // RagDollCOMVelocity;
        vector = RagDollCOMVelocity;
        if (VelocityInWorldSpace)
            vector = _ragDollBodyStats.transform.TransformVector(vector);
        DrawArrow(pos, vector, Color.blue);
        Vector3 actualPos = pos+vector;

        // InputDesiredHorizontalVelocity;
        vector = new Vector3(InputDesiredHorizontalVelocity.x, 0f, InputDesiredHorizontalVelocity.y);
        if (VelocityInWorldSpace)
            vector = _ragDollBodyStats.transform.TransformVector(vector);
        DrawArrow(pos, vector, Color.green);

        // HorizontalVelocityDifference;
        vector = new Vector3(HorizontalVelocityDifference.x, 0f, HorizontalVelocityDifference.y);
        if (VelocityInWorldSpace)
            vector = _ragDollBodyStats.transform.TransformVector(vector);
        DrawArrow(actualPos, vector, Color.red);

        for (int i = 0; i < RagDollBodyStats.Count; i++)
        {
            var stat = RagDollBodyStats[i];
            var differenceStat = BodyPartDifferenceStats[i];
            pos = stat.Position;
            vector = stat.Velocity;
            if (PositionInWorldSpace)
                pos = _ragDollBodyStats.transform.TransformPoint(pos);
            if (VelocityInWorldSpace)
                vector = _ragDollBodyStats.transform.TransformVector(vector);
            DrawArrow(pos, vector, Color.cyan);
            Vector3 velocityPos = pos+vector;

            pos = stat.Position;
            vector = differenceStat.Position;
            if (PositionInWorldSpace)
                pos = _ragDollBodyStats.transform.TransformPoint(pos);
            if (VelocityInWorldSpace)
                vector = _ragDollBodyStats.transform.TransformVector(vector);
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(pos, vector);
            Vector3 differencePos = pos+vector;

            vector = differenceStat.Velocity;
            if (VelocityInWorldSpace)
                vector = _ragDollBodyStats.transform.TransformVector(vector);
            DrawArrow(velocityPos, vector, Color.red);
        }
        
    }
    void DrawArrow(Vector3 start, Vector3 vector, Color color)
    {
        float headSize = 0.25f;
        float headAngle = 20.0f;
        Gizmos.color = color;
		Gizmos.DrawRay(start, vector);
 
        if (vector.magnitude > 0f)
        { 
            Vector3 right = Quaternion.LookRotation(vector) * Quaternion.Euler(0,180+headAngle,0) * new Vector3(0,0,1);
            Vector3 left = Quaternion.LookRotation(vector) * Quaternion.Euler(0,180-headAngle,0) * new Vector3(0,0,1);
            Gizmos.DrawRay(start + vector, right * headSize);
            Gizmos.DrawRay(start + vector, left * headSize);
        }
    }
}
