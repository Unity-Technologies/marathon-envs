
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[System.Serializable]
public class BodyPart002
{
    
    public string Name;
    public BodyHelper002.BodyPartGroup Group;

    public Vector3 ObsLocalPosition;
    public Quaternion ObsRotation;
    public Quaternion ObsRotationFromBase;
    public Vector3 ObsRotationVelocity;
    public Vector3 ObsVelocity;
    public Quaternion ObsNormalizedDeltaFromAnimationRotation;
    public float ObsAngleDeltaFromAnimationRotation;
    public Vector3 ObsDeltaFromAnimationPosition;

    public Vector3 ObsDeltaFromAnimationVelocity;
    public Vector3 ObsDeltaFromAnimationAngularVelocity;

    public Vector3 DebugMaxRotationVelocity;
    public Vector3 DebugMaxVelocity;


    public Quaternion DefaultLocalRotation;
    public Quaternion ToJointSpaceInverse;
    public Quaternion ToJointSpaceDefault;

    public Rigidbody Rigidbody;
    public Transform Transform;
    public BodyPart002 Root;
    public Quaternion InitialRootRotation;
    public Vector3 InitialRootPosition;

    // base = from where to measure rotation and position from
    public Quaternion BaseRotation;
    public Vector3 BasePosition;
    //
    public Quaternion ToFocalRoation;


    Quaternion _lastObsRotation;
    Vector3 _lastLocalPosition;
    Vector3 _animationAngularVelocity;
    Vector3 _animationVelocity;

    float _lastUpdateObsTime;
    bool _firstRunComplete;
    bool _hasRanVeryFirstInit;
    private Vector3 _animationPosition;
    private Quaternion _animationRotation;

    static Vector3 NormalizedEulerAngles(Vector3 eulerAngles)
    {
        var x = eulerAngles.x < 180f ?
            eulerAngles.x :
            - 360 + eulerAngles.x;
        var y = eulerAngles.y < 180f ?
            eulerAngles.y :
            - 360 + eulerAngles.y;
        var z = eulerAngles.z < 180f ?
            eulerAngles.z :
            - 360 + eulerAngles.z;
        x = x / 180f;
        y = y / 180f;
        z = z / 180f;
        return new Vector3(x,y,z);
    }

    static Vector3 Vector3Max (Vector3 a, Vector3 b)
    {
        var answer = new Vector3(
            Mathf.Max(Mathf.Abs(a.x), Mathf.Abs(b.x)),
            Mathf.Max(Mathf.Abs(a.y), Mathf.Abs(b.y)),
            Mathf.Max(Mathf.Abs(a.z), Mathf.Abs(b.z)));
        return answer;
    }

    public void Init()
    {
        _firstRunComplete = false;
        if (Rigidbody != null){
            Rigidbody.angularVelocity = Vector3.zero;
            Rigidbody.velocity = Vector3.zero;
        }

        if (!_hasRanVeryFirstInit) {
			
            InitialRootRotation = Root.Transform.transform.rotation;
            InitialRootPosition = Root.Transform.transform.position;
            BaseRotation = Root.Transform.transform.rotation;
            BasePosition = Root.Transform.transform.position;

			DefaultLocalRotation = LocalRotation;
            Vector3 forward = this.Transform.forward;
            Vector3 up = this.Transform.up;
			Quaternion toJointSpace = Quaternion.LookRotation(forward, up);
			
			ToJointSpaceInverse = Quaternion.Inverse(toJointSpace);
			ToJointSpaceDefault = DefaultLocalRotation * toJointSpace;

    		// set body part direction
			Vector3 focalOffset = new Vector3(10,0,0);
            if (Rigidbody != null){
                var focalPoint = Rigidbody.position + focalOffset;
                ToFocalRoation = Rigidbody.rotation;
                ToFocalRoation.SetLookRotation(focalPoint - Rigidbody.position);
            }

            _hasRanVeryFirstInit = true;

        }
    }

    public void UpdateObservations()
    {
        Quaternion rotation;
        Vector3 position;
        if (this == Root) {
            rotation = Quaternion.Inverse(InitialRootRotation) * Transform.rotation;
            position =  Transform.position - InitialRootPosition;
        }
        else {
            rotation = Quaternion.Inverse(Root.Transform.rotation) * Transform.rotation;
            position =  Transform.position - Root.Transform.position;
        }
        
        if (_firstRunComplete == false){
            _lastUpdateObsTime = Time.time;

            _lastObsRotation = Transform.rotation;
            _lastLocalPosition = Transform.position;
        }

        var dt = Time.time - _lastUpdateObsTime;
        _lastUpdateObsTime = Time.time;

        var velocity = Transform.position - _lastLocalPosition;

        var rotationVelocity = JointHelper002.FromToRotation(_lastObsRotation, Transform.rotation);
        var angularVelocity = JointHelper002.NormalizedEulerAngles(rotationVelocity.eulerAngles);

        // old calulation for observation vector
        angularVelocity = NormalizedEulerAngles(rotationVelocity.eulerAngles);
        angularVelocity /= 128f;
        // old calculation end

        if (dt > 0f) {
            angularVelocity /= dt;
            velocity /= dt;
        }

        _lastLocalPosition = Transform.position;
        _lastObsRotation = Transform.rotation;

        Debug.Log("animation angular velocity:" + _animationAngularVelocity);
        Debug.Log("angular velocity:" + angularVelocity);
        Debug.Log("proper angular velocity:" + JointHelper002.NormalizedEulerAngles(rotationVelocity.eulerAngles) / dt);
        Debug.Log("rotation:" + rotation);
        Debug.Log("animation rotation: " + _animationRotation);
        Debug.Log("velocity: " + velocity);
        Debug.Log("animation velocity:" + _animationVelocity);
        Debug.Log("dt:" + dt);

        ObsLocalPosition = position;
        ObsRotation = rotation;
        ObsRotationVelocity = angularVelocity;
        ObsVelocity = velocity;

        ObsDeltaFromAnimationPosition = _animationPosition - position;
        ObsNormalizedDeltaFromAnimationRotation = _animationRotation * Quaternion.Inverse(Transform.rotation);
        ObsAngleDeltaFromAnimationRotation = Quaternion.Angle(_animationRotation, Transform.rotation);
        ObsAngleDeltaFromAnimationRotation = JointHelper002.NormalizedAngle(ObsAngleDeltaFromAnimationRotation);

        ObsDeltaFromAnimationVelocity = _animationVelocity - velocity;
        ObsDeltaFromAnimationAngularVelocity = (_animationAngularVelocity - angularVelocity);

        if (_firstRunComplete == false){
            ObsDeltaFromAnimationPosition = Vector3.zero;
            ObsNormalizedDeltaFromAnimationRotation = new Quaternion(0,0,0,0);
            ObsAngleDeltaFromAnimationRotation = 0f;
        }

        DebugMaxRotationVelocity = Vector3Max(DebugMaxRotationVelocity, angularVelocity);
        DebugMaxVelocity = Vector3Max(DebugMaxVelocity, velocity);

        _firstRunComplete = true;
    }
    public Quaternion LocalRotation {
        get {
            return Quaternion.Inverse(RootRotation) * Transform.rotation;
        }
    }

    public Quaternion RootRotation{
        get {
            return InitialRootRotation;
        }
    }

    public void MoveToAnim(Vector3 animPosition, Quaternion animRotation, Vector3 angularVelocity, Vector3 velocity)
    {
        Transform.position = animPosition;
        Transform.rotation = animRotation;
        if (Rigidbody != null){
            foreach (var childRb in Rigidbody.GetComponentsInChildren<Rigidbody>())
            {
                if (childRb == Rigidbody)
                    continue;
                childRb.transform.localPosition = Vector3.zero;
                childRb.transform.localEulerAngles = Vector3.zero;
                childRb.angularVelocity = Vector3.zero;
                childRb.velocity = Vector3.zero;
            }
            Rigidbody.angularVelocity = angularVelocity;
            Rigidbody.velocity = velocity;
        }
    }
    public void SetAnimationPosition(Vector3 animPosition, Quaternion animRotation, Vector3 animVelocity, Vector3 animAngularVelocity)
    {
        _animationPosition = animPosition;
        _animationRotation = animRotation;

        _animationVelocity = animVelocity;
        _animationAngularVelocity = animAngularVelocity;
    }
}