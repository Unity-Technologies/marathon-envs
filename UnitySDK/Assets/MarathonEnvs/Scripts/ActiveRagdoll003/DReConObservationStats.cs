using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;

public class DReConObservationStats : MonoBehaviour
{
    [System.Serializable]
    public class Stat
    {
        public string Name;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public Vector3 AngualrVelocity;
        [HideInInspector]
        public Vector3 LastWorldPosition;
        [HideInInspector]
        public Quaternion LastWorldRotation;
        [HideInInspector]
        public bool LastIsSet;
    }

    public MonoBehaviour ObjectToTrack;
    List<string> _bodyPartsToTrack;

    [Header("Anchor stats")]
    public Vector3 HorizontalDirection; // Normalized vector in direction of travel (assume right angle to floor)
    // public Vector3 CenterOfMassInWorldSpace; 
    public Vector3 AngualrVelocity;

    [Header("Stats, relative to HorizontalDirection & Center Of Mass")]
    public Vector3 CenterOfMassVelocity;
    public Vector3 CenterOfMassHorizontalVelocity;
    public float CenterOfMassVelocityMagnitude;
    public float CenterOfMassHorizontalVelocityMagnitude;
    public Vector3 DesiredCenterOfMassVelocity;
    public Vector3 CenterOfMassVelocityDifference;
    public List<Stat> Stats;

    // [Header("... for debugging")]
    [Header("Gizmos")]
    public bool VelocityInWorldSpace = true;
    public bool HorizontalVelocity = true;

    [HideInInspector]
    public Vector3 LastCenterOfMassInWorldSpace;
    [HideInInspector]
    public Quaternion LastRotation;
    [HideInInspector]
    public bool LastIsSet;


    SpawnableEnv _spawnableEnv;
    List<Transform> _bodyParts;
    internal List<Rigidbody> _rigidbodyParts;
    internal List<ArticulationBody> _articulationBodyParts;
    GameObject _root;
    InputController _inputController;

    public void OnAwake(List<string> bodyPartsToTrack, Transform defaultTransform)
    {
        _bodyPartsToTrack = bodyPartsToTrack;
        _spawnableEnv = GetComponentInParent<SpawnableEnv>();
        _inputController = _spawnableEnv.GetComponentInChildren<InputController>();
        _rigidbodyParts = ObjectToTrack.GetComponentsInChildren<Rigidbody>().ToList();
        _articulationBodyParts = ObjectToTrack.GetComponentsInChildren<ArticulationBody>().ToList();
        if (_rigidbodyParts?.Count > 0)
            _bodyParts = _rigidbodyParts
                .SelectMany(x=>x.GetComponentsInChildren<Transform>())
                .Distinct()
                .ToList();
        else
            _bodyParts = _articulationBodyParts
                .SelectMany(x=>x.GetComponentsInChildren<Transform>())
                .Distinct()
                .ToList();
        if (_bodyPartsToTrack?.Count > 0)
            _bodyParts = _bodyPartsToTrack
                .Where(x=>_bodyPartsToTrack.Contains(x))
                .Select(x=>_bodyParts.First(y=>y.name == x))
                .ToList();
        Stats = _bodyParts
            .Select(x=> new Stat{Name = x.name})
            .ToList();
        if (_root == null)
        {
            _root = _bodyParts.First(x=>x.name=="butt").gameObject;
        }             
        transform.position = defaultTransform.position;
        transform.rotation = defaultTransform.rotation;
    }

    public void OnReset()
    {
        ResetStatus();
        foreach (var bodyPart in Stats)
        {
            bodyPart.LastIsSet = false;
        }
        LastIsSet = false;
    }
    void ResetStatus()
    {
        foreach (var bodyPart in Stats)
        {
            bodyPart.LastIsSet = false;
        }
        LastIsSet = false;
        var timeDelta = float.MinValue;
        SetStatusForStep(timeDelta);
    }

    public void SetStatusForStep(float timeDelta)
    {
        // find Center Of Mass
        Vector3 newCOM;
        if (_rigidbodyParts?.Count > 0)
            newCOM = GetCenterOfMass(_rigidbodyParts);
        else
            newCOM = GetCenterOfMass(_articulationBodyParts);
        if (!LastIsSet)
        {
            LastCenterOfMassInWorldSpace = newCOM;
        }

        // generate Horizontal Direction
        var newHorizontalDirection = new Vector3(0f, _root.transform.eulerAngles.y, 0f);
        HorizontalDirection = newHorizontalDirection / 180f;

        // set this object to be f space
        transform.position = newCOM;
        transform.rotation = Quaternion.Euler(newHorizontalDirection);

        // get Center Of Mass velocity in f space
        var velocity = transform.position - LastCenterOfMassInWorldSpace;
        velocity /= timeDelta;
        CenterOfMassVelocity = transform.InverseTransformVector(velocity);
        CenterOfMassVelocityMagnitude = CenterOfMassVelocity.magnitude;

        // get Center Of Mass horizontal velocity in f space
        var comHorizontalDirection = new Vector3(velocity.x, 0f, velocity.z);
        CenterOfMassHorizontalVelocity = transform.InverseTransformVector(comHorizontalDirection);
        CenterOfMassHorizontalVelocityMagnitude = CenterOfMassHorizontalVelocity.magnitude;

        // get Desired Center Of Mass horizontal velocity in f space
        Vector3 desiredCom = new Vector3(
            _inputController.DesiredHorizontalVelocity.x,
            0f,
            _inputController.DesiredHorizontalVelocity.y);
        DesiredCenterOfMassVelocity = transform.InverseTransformVector(desiredCom);
            
        // get Desired Center Of Mass horizontal velocity in f space
        CenterOfMassVelocityDifference = DesiredCenterOfMassVelocity-CenterOfMassHorizontalVelocity;
        
        if (!LastIsSet)
        {
            LastRotation = transform.rotation;
        }
        AngualrVelocity = GetAngularVelocity(transform.rotation, LastRotation, timeDelta);
        LastRotation = transform.rotation;
        LastCenterOfMassInWorldSpace = newCOM;
        LastIsSet = true;

        // get bodyParts stats in local space
        foreach (var bodyPart in _bodyParts)
        {
            Stat bodyPartStat = Stats.First(x=>x.Name == bodyPart.name);
            Vector3 worldPosition = bodyPart.position;
            Quaternion worldRotation = bodyPart.rotation;
            if (!bodyPartStat.LastIsSet)
            {
                bodyPartStat.LastWorldPosition = worldPosition;
                bodyPartStat.LastWorldRotation = worldRotation;
            }
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
            Quaternion localRotation = Quaternion.Inverse(transform.rotation) * worldRotation;
            bodyPartStat.Position = localPosition;
            bodyPartStat.Rotation = localRotation;
            bodyPartStat.Velocity = worldPosition - bodyPartStat.LastWorldPosition;
            bodyPartStat.Velocity /= timeDelta;
            bodyPartStat.Velocity = transform.InverseTransformVector(bodyPartStat.Velocity);
            bodyPartStat.AngualrVelocity = GetAngularVelocity(
                    Quaternion.Inverse(transform.rotation) * worldRotation, 
                    Quaternion.Inverse(transform.rotation) * bodyPartStat.LastWorldRotation, 
                    timeDelta);
            bodyPartStat.LastWorldPosition = worldPosition;
            bodyPartStat.LastWorldRotation = worldRotation;
            bodyPartStat.LastIsSet = true;
        }
    }

	Vector3 GetCenterOfMass(IEnumerable<Rigidbody> bodies)
	{
		var centerOfMass = Vector3.zero;
		float totalMass = 0f;
		foreach (Rigidbody ab in bodies)
		{
			centerOfMass += ab.worldCenterOfMass * ab.mass;
			totalMass += ab.mass;
		}
		centerOfMass /= totalMass;
		centerOfMass -= _spawnableEnv.transform.position;
		return centerOfMass;
	}
	Vector3 GetCenterOfMass(IEnumerable<ArticulationBody> bodies)
	{
		var centerOfMass = Vector3.zero;
		float totalMass = 0f;
		foreach (ArticulationBody ab in bodies)
		{
			centerOfMass += ab.worldCenterOfMass * ab.mass;
			totalMass += ab.mass;
		}
		centerOfMass /= totalMass;
		centerOfMass -= _spawnableEnv.transform.position;
		return centerOfMass;    
    }
    public static Vector3 GetAngularVelocity(Quaternion rotation, Quaternion lastRotation, float timeDelta)
    {
        var q = lastRotation * Quaternion.Inverse(rotation);
        float gain;
        if(q.w < 0.0f)
        {
            var angle = Mathf.Acos(-q.w);
            gain = -2.0f * angle / (Mathf.Sin(angle)*timeDelta);
            // gain = -2.0f * angle / (Mathf.Sin(angle)/timeDelta);
        }
        else
        {
            var angle = Mathf.Acos(q.w);
            gain = 2.0f * angle / (Mathf.Sin(angle)*timeDelta);
            // gain = 2.0f * angle / (Mathf.Sin(angle)/timeDelta);
        }
        var result = new Vector3(q.x * gain,q.y * gain,q.z * gain);
        var safeResult = new Vector3(
            float.IsNaN(result.x) ? 0f : result.x,
            float.IsNaN(result.y) ? 0f : result.y,
            float.IsNaN(result.z) ? 0f : result.z
        );
        return safeResult;
    }    
    // public void DrawPointDistancesFrom(DReConRewardStats target, int objIdex)
    // {
    //     for (int i = objIdex*6; i < (objIdex*6)+6; i++)
    //     {
    //         Gizmos.color = Color.white;
    //         var from = Points[i];
    //         var to = target.Points[i];
    //         // transform to this object's world space
    //         from = this.transform.TransformPoint(from);
    //         to = this.transform.TransformPoint(to);
    //         Gizmos.DrawLine(from, to);
    //     }
    // }

    void OnDrawGizmosSelected()
    {
        // draw arrow for desired input velocity
        // Vector3 pos = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        Vector3 pos = new Vector3(transform.position.x, .3f, transform.position.z);
        Vector3 vector = DesiredCenterOfMassVelocity;
        if (VelocityInWorldSpace)
            vector = transform.TransformVector(vector);
        DrawArrow(pos, vector, Color.green);
        Vector3 desiredInputPos = pos+vector;

        if (HorizontalVelocity)
        {
            // arrow for actual velocity
            vector = CenterOfMassHorizontalVelocity;
            if (VelocityInWorldSpace)
                vector = transform.TransformVector(vector);
            DrawArrow(pos, vector, Color.blue);
            Vector3 actualPos = pos+vector;

            // arrow for actual velocity difference
            vector = CenterOfMassVelocityDifference;
            if (VelocityInWorldSpace)
                vector = transform.TransformVector(vector);
            DrawArrow(actualPos, vector, Color.red);
        }
        else
        {
            vector = CenterOfMassVelocity;
            if (VelocityInWorldSpace)
                vector = transform.TransformVector(vector);
            DrawArrow(pos, vector, Color.blue);
            Vector3 actualPos = pos+vector;

            // arrow for actual velocity difference
            vector = DesiredCenterOfMassVelocity-CenterOfMassVelocity;
            if (VelocityInWorldSpace)
                vector = transform.TransformVector(vector);
            DrawArrow(actualPos, vector, Color.red);

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
