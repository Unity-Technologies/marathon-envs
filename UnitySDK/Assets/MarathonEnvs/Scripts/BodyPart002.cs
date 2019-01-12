
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
    // public Vector3 ObsNormalizedRotation;
    // public Vector3 ObsNormalizedDeltaFromTargetRotation;
    public Vector3 ObsRotationVelocity;
    public Vector3 ObsVelocity;
    public Quaternion ObsNormalizedDeltaFromAnimationRotation;
    public float ObsAngleDeltaFromAnimationRotation;
    public Vector3 ObsDeltaFromAnimationPosition;

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
			//Parent = ConfigurableJoint.connectedBody;
			
            InitialRootRotation = Root.Transform.transform.rotation;
            InitialRootPosition = Root.Transform.transform.position;
            BaseRotation = Root.Transform.transform.rotation;
            BasePosition = Root.Transform.transform.position;

			DefaultLocalRotation = LocalRotation;
			// Vector3 forward = Vector3.Cross (ConfigurableJoint.axis, ConfigurableJoint.secondaryAxis).normalized;
			//Vector3 up = Vector3.Cross (forward, ConfigurableJoint.axis).normalized;
            Vector3 forward = this.Transform.forward;
            // Vector3 up = this.Transform.forward;
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
        Quaternion rotationFromBase;
        Vector3 position;
        Vector3 angle;
        if (this == Root) {
            rotation = Quaternion.Inverse(InitialRootRotation) * Transform.rotation;
            position =  Transform.position - InitialRootPosition;
            angle = rotation.eulerAngles;
        }
        else {
            rotation = Quaternion.Inverse(Root.Transform.rotation) * Transform.rotation;
            angle = rotation.eulerAngles;
            position =  Transform.position - Root.Transform.position;
        }
        rotationFromBase = Quaternion.Inverse(BaseRotation) * Transform.rotation;
			// Vector3 animPosition = bodyPart.InitialRootPosition + animStep.Positions[0];
            // Quaternion animRotation = bodyPart.InitialRootRotation * animStep.Rotaions[0];
			// if (i != 0) {
			// 	animPosition += animStep.Positions[i];
			// 	animRotation *= animStep.Rotaions[i];
			// }
        
        if (_firstRunComplete == false){
            _lastUpdateObsTime = Time.time;
            _lastObsRotation = Transform.rotation;
            _lastLocalPosition = Transform.position;
        }

        var dt = Time.time - _lastUpdateObsTime;
        _lastUpdateObsTime = Time.time;
        var velocity = Transform.position - _lastLocalPosition;
        var rotationVelocity = JointHelper002.FromToRotation(_lastObsRotation, Transform.rotation);
        var angularVelocity = rotationVelocity.eulerAngles;
        _lastLocalPosition = Transform.position;
        _lastObsRotation = Transform.rotation;
        angularVelocity = NormalizedEulerAngles(angularVelocity);
        angularVelocity /= 128f;
        if (dt > 0f)
            angularVelocity /= dt;
        if (dt > 0f)
            velocity /= dt;

        ObsDeltaFromAnimationPosition = _animationPosition - position;
        ObsNormalizedDeltaFromAnimationRotation = _animationRotation * Quaternion.Inverse(rotationFromBase);
        ObsAngleDeltaFromAnimationRotation = Mathf.Abs(Quaternion.Angle(_animationRotation, rotationFromBase)/180f);
        ObsLocalPosition = position;
        ObsRotation = rotation;
        ObsRotationFromBase = rotationFromBase;
        ObsRotationVelocity = angularVelocity;
        ObsVelocity = velocity;

        // ObsRotation = this.LocalRotation;
        // ObsRotation = (ToJointSpaceInverse * UnityEngine.Quaternion.Inverse(this.LocalRotation) * this.ToJointSpaceDefault);
        
        // var normalizedRotation = NormalizedEulerAngles(ObsRotation.eulerAngles);

        // Debug code 
        // if (Group == BodyHelper002.BodyPartGroup.Head){
        //     var debug = 1;
        // }

        // var dt = Time.time - _lastUpdateObsTime;
        // _lastUpdateObsTime = Time.time;
        // var rotationVelocity = ObsRotation.eulerAngles - _lastObsRotation.eulerAngles;
        // rotationVelocity = NormalizedEulerAngles(rotationVelocity);
        // rotationVelocity /= 128f;
        // if (dt > 0f)
        //     rotationVelocity /= dt;
        // ObsRotationVelocity = rotationVelocity;
        // _lastObsRotation = ObsRotation;
        // ObsLocalPosition = Transform.position - Root.Transform.position;
        // var velocity = ObsLocalPosition - _lastLocalPosition;
        // ObsVelocity = velocity;
        // if (dt > 0f)
        //     velocity /= dt;
        // _lastLocalPosition = ObsLocalPosition;

        // // ObsDeltaFromAnimationPosition = _animationPosition - Transform.position;
        // // ObsNormalizedDeltaFromAnimationRotation = _animationRotation * Quaternion.Inverse(Transform.rotation);
        // // ObsAngleDeltaFromAnimationRotation = Quaternion.Angle(_animationRotation, Transform.rotation);

        // // ObsNormalizedDeltaFromAnimationRotation = NormalizedEulerAngles(obsDeltaFromAnimationRotation.eulerAngles);
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
            // around root Rotation 
            return Quaternion.Inverse(RootRotation) * Transform.rotation;

            // around parent space
            // return Quaternion.Inverse(ParentRotation) * transform.rotation;
        }
    }

    public Quaternion RootRotation{
        get {
            return InitialRootRotation;
        }
    }

    
    // public Quaternion ParentRotation {
    //     get {
    //         if (ConfigurableJoint.connectedBody != null) return ConfigurableJoint.connectedBody.rotation;
    //         if (transform..parent == null) return Quaternion.identity;
    //         return transform..parent.rotation;
    //     }
    // }

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
    public void SetAnimationPosition(Vector3 animPosition, Quaternion animRotation)
    {
        // _animationPosition = animPosition + InitialRootPosition;
        // _animationRotation = animRotation * InitialRootRotation;
        _animationPosition = animPosition;
        _animationRotation = animRotation;
    }
}