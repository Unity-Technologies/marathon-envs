// A class that describes a body part of an Agent or an Animator. The object
// stores the rigid body associated with an Agent's body part. Along with some
// important data like root bone, i. e. 'butt' for a marathonMan. Implments methods
// to calculate distance to the characteristics of an animation being mimiced.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAgents;


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
    public float ObsAngleDeltaFromAnimationRotation;
    public Vector3 ObsDeltaFromAnimationPosition;

    public Vector3 ObsDeltaFromAnimationVelocity;
    public Vector3 ObsDeltaFromAnimationAngularVelocity;
    public Vector3 ObsDeltaFromAnimationAngularVelocityWorld;
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
    Quaternion _lastWorldRotation;
    Vector3 _lastLocalPosition;
    Vector3 _lastWorldPosition;
    Vector3 _animationAngularVelocity;
    Vector3 _animationAngularVelocityWorld;
    Vector3 _animationVelocityWorld;

    DecisionRequester _decisionRequester;

    float _lastUpdateObsTime;
    bool _firstRunComplete;
    bool _hasRanVeryFirstInit;
    private Vector3 _animationPositionWorld;
    private Quaternion _animationRotation;

    static Vector3 Vector3Max (Vector3 a, Vector3 b)
    {
        var answer = new Vector3(
            Mathf.Max(Mathf.Abs(a.x), Mathf.Abs(b.x)),
            Mathf.Max(Mathf.Abs(a.y), Mathf.Abs(b.y)),
            Mathf.Max(Mathf.Abs(a.z), Mathf.Abs(b.z)));
        return answer;
    }

    // Initialize
    public void Init()
    {
        _decisionRequester = GameObject.Find("MarathonMan").GetComponent<DecisionRequester>();

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

    // Update values like ObsLocalPosition, ObsRotation, ... that will be accessed
    // by the agent as observations for the neural network. Also calculates distances
    // to an animation being mimicked
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
            _lastLocalPosition = position;
            _lastWorldPosition = Transform.position;
            _lastObsRotation = rotation;
            _lastWorldRotation = Transform.rotation;
        }

        var dt = Time.fixedDeltaTime * _decisionRequester.DecisionPeriod;

        var velocity = (position - _lastLocalPosition)/dt;
        var velocityWorld = (Transform.position - _lastWorldPosition)/dt;
        var angularVelocity = JointHelper002.CalcDeltaRotationNormalizedEuler(_lastObsRotation, rotation)/dt;
        var angularVelocityWorld = JointHelper002.CalcDeltaRotationNormalizedEuler(_lastWorldRotation, Transform.rotation)/dt;

        // old calulation for observation vector
        //angularVelocity = NormalizedEulerAngles(rotationVelocity.eulerAngles);
        //angularVelocity /= 128f;
        // old calculation end

        _lastUpdateObsTime = Time.time;
        _lastLocalPosition = position;
        _lastWorldPosition = Transform.position;
        _lastObsRotation = rotation;
        _lastWorldRotation = Transform.rotation;

        //if (Name == "right_right_foot") {
        //    Debug.Log("^^^^^^^^^^^^");
        //    Debug.Log("body part name: " + Name);
        //    Debug.Log("animation angular velocity:" + _animationAngularVelocity);
        //    Debug.Log("angular velocity:" + angularVelocity);
        //    Debug.Log("animation angular velocity world:" + _animationAngularVelocityWorld);
        //    Debug.Log("angular velocity world:" + angularVelocityWorld);
        //    Debug.Log("rotation local:" + rotation.eulerAngles);
        //    Debug.Log("animation rotation local: " + _animationRotation.eulerAngles);
        //    Debug.Log("velocity world: " + velocityWorld);
        //    Debug.Log("animation velocity world:" + _animationVelocityWorld);
        //    Debug.Log("transform position:" + Transform.position);
        //    Debug.Log("animation position world: " + _animationPositionWorld);
        //    Debug.Log("dt:" + dt);
        //}

        ObsLocalPosition = position;
        ObsRotation = rotation;
        ObsRotationVelocity = angularVelocity;
        ObsVelocity = velocity;

        ObsDeltaFromAnimationPosition = _animationPositionWorld - Transform.position;

        ObsAngleDeltaFromAnimationRotation = Quaternion.Angle(_animationRotation, rotation);
        ObsAngleDeltaFromAnimationRotation = JointHelper002.NormalizedAngle(ObsAngleDeltaFromAnimationRotation);  

        ObsDeltaFromAnimationVelocity = _animationVelocityWorld - velocityWorld;
        ObsDeltaFromAnimationAngularVelocity = (_animationAngularVelocity - angularVelocity);
        ObsDeltaFromAnimationAngularVelocityWorld = (_animationAngularVelocityWorld - angularVelocityWorld);

        DebugMaxRotationVelocity = Vector3Max(DebugMaxRotationVelocity, angularVelocity);
        DebugMaxVelocity = Vector3Max(DebugMaxVelocity, velocity);

        _firstRunComplete = true;
    }

    // returns local rotation
    public Quaternion LocalRotation {
        get {
            return Quaternion.Inverse(RootRotation) * Transform.rotation;
        }
    }

    // retuns root rotation
    public Quaternion RootRotation{
        get {
            return InitialRootRotation;
        }
    }

    // Set the position, rotation of a body part to match animation's position and
    // rotation. Also sets the rigid body's position and velocity, which is used
    // in angular momentum calculation
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

    // Set the position of the animation being mimicked
    public void SetAnimationPosition(Vector3 animPositionWorld, Quaternion animRotationLocal, Vector3 animVelocityWorld, Vector3 animAngularVelocityLocal, Vector3 animAngularVelocityWorld)
    {
        _animationPositionWorld = animPositionWorld;
        _animationRotation = animRotationLocal;

        _animationVelocityWorld = animVelocityWorld;
        _animationAngularVelocity = animAngularVelocityLocal;
        _animationAngularVelocityWorld = animAngularVelocityWorld;
    }
}