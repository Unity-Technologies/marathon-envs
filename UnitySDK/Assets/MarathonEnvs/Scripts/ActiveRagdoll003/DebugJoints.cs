using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DebugJoints : MonoBehaviour
{
    public float SphereSize = 0.03f;

    static Color[] _axisColor = { 
        new Color(219f / 255, 62f / 255, 29f / 255, .93f), 
        new Color(154f / 255, 243f / 255, 72f / 255, .93f), 
        new Color(58f / 255, 122f / 255, 248f / 255, .93f)};
    static Vector3[] _axisVector = { Vector3.right, Vector3.up, Vector3.forward };
    ArticulationBody _body;
    ArticulationBody _parentBody;
    MarathonTestBedController _debugController;
    // Start is called before the first frame update
    MLAgents.SpawnableEnv _spawnableEnv;
    MocapController _mocapController;
    Rigidbody _target;
    public Vector3 TargetRotationInJointSpace;
    public Vector3 RotationInJointSpace;
    public Vector3 RotationInJointSpaceError;
    public Vector3 RotationInJointSpaceErrorRad;

    public Vector3 JointPositionDeg;
    public Vector3 JointTargetDeg;
    public Vector3 JointPositionRad;
    public Vector3 JointTargetRad;

    void Start()
    {
        _body = GetComponent<ArticulationBody>();
        _parentBody = _body.transform.parent.GetComponentInParent<ArticulationBody>();
        _debugController = FindObjectOfType<MarathonTestBedController>();
        _spawnableEnv = GetComponentInParent<MLAgents.SpawnableEnv>();
        _mocapController = _spawnableEnv.GetComponentInChildren<MocapController>();
        var mocapBodyParts = _mocapController.GetComponentsInChildren<Rigidbody>().ToList();
        _target = mocapBodyParts.First(x=>x.name == _body.name);
    }




    // Update is called once per frame
    void FixedUpdate()
    {
        if (_body == null)
            return;
        if (_body.jointType != ArticulationJointType.SphericalJoint)
            return;

        RotationInJointSpace = -(Quaternion.Inverse(_body.anchorRotation) * Quaternion.Inverse(_body.transform.localRotation) * _body.parentAnchorRotation).eulerAngles;
        TargetRotationInJointSpace = -(Quaternion.Inverse(_body.anchorRotation) * Quaternion.Inverse(_target.transform.localRotation) * _body.parentAnchorRotation).eulerAngles;
        RotationInJointSpaceError = TargetRotationInJointSpace-RotationInJointSpace;
        RotationInJointSpace = new Vector3(
            Mathf.DeltaAngle(0, RotationInJointSpace.x),
            Mathf.DeltaAngle(0, RotationInJointSpace.y),
            Mathf.DeltaAngle(0, RotationInJointSpace.z));
        TargetRotationInJointSpace = new Vector3(
            Mathf.DeltaAngle(0, TargetRotationInJointSpace.x),
            Mathf.DeltaAngle(0, TargetRotationInJointSpace.y),
            Mathf.DeltaAngle(0, TargetRotationInJointSpace.z));
        RotationInJointSpaceError = new Vector3(
            Mathf.DeltaAngle(0, RotationInJointSpaceError.x),
            Mathf.DeltaAngle(0, RotationInJointSpaceError.y),
            Mathf.DeltaAngle(0, RotationInJointSpaceError.z));
        RotationInJointSpaceErrorRad = RotationInJointSpaceError * Mathf.Deg2Rad;
        JointTargetDeg.x = TargetRotationInJointSpace.y;
        JointTargetDeg.y = TargetRotationInJointSpace.z;
        JointTargetDeg.z = TargetRotationInJointSpace.x;

        var jointPosition = _body.jointPosition;
        JointPositionDeg = Vector3.zero;
        int i = 0;
        if (_body.twistLock == ArticulationDofLock.LimitedMotion)
            JointPositionDeg.x = jointPosition[i++];
        if (_body.swingYLock == ArticulationDofLock.LimitedMotion)
            JointPositionDeg.y = jointPosition[i++];
        if (_body.swingZLock == ArticulationDofLock.LimitedMotion)
            JointPositionDeg.z = jointPosition[i++];
        float stiffness = 1000f;
        float damping = 100f;
        JointPositionDeg *= Mathf.Rad2Deg;

        bool dontUpdateMotor = _debugController.DontUpdateMotor;
        dontUpdateMotor &= _debugController.isActiveAndEnabled;
        dontUpdateMotor &= _debugController.gameObject.activeInHierarchy;
        if(dontUpdateMotor)
        {
    		// var drive = _body.yDrive;
            // drive.stiffness = stiffness;
            // drive.damping = damping;
            // drive.target = JointTargetDeg.x;
            // _body.yDrive = drive;
            
            // drive = _body.zDrive;
            // drive.stiffness = stiffness;
            // drive.damping = damping;
            // drive.target = JointTargetDeg.y;
            // _body.zDrive = drive;

            // drive = _body.xDrive;
            // drive.stiffness = stiffness;
            // drive.damping = damping;
            // drive.target = JointTargetDeg.z;
            // _body.xDrive = drive;
    		var drive = _body.xDrive;
            drive.stiffness = stiffness;
            drive.damping = damping;
            drive.target = JointTargetDeg.x;
            _body.xDrive = drive;
            
            drive = _body.yDrive;
            drive.stiffness = stiffness;
            drive.damping = damping;
            drive.target = JointTargetDeg.y;
            _body.yDrive = drive;

            drive = _body.zDrive;
            drive.stiffness = stiffness;
            drive.damping = damping;
            drive.target = JointTargetDeg.z;
            _body.zDrive = drive;            
        }
        else
        {
    		// var drive = _body.yDrive;
            // JointTargetDeg = Vector3.zero;
            // JointTargetDeg.x = drive.target;
            // drive = _body.zDrive;
            // JointTargetDeg.y = drive.target;
            // drive = _body.xDrive;
            // JointTargetDeg.z = drive.target;
    		var drive = _body.xDrive;
            JointTargetDeg = Vector3.zero;
            JointTargetDeg.x = drive.target;
            drive = _body.yDrive;
            JointTargetDeg.y = drive.target;
            drive = _body.zDrive;
            JointTargetDeg.z = drive.target;            
        }
        
        JointPositionRad = JointPositionDeg * Mathf.Deg2Rad;
        JointTargetRad = JointTargetDeg * Mathf.Deg2Rad;
    }
    public static Quaternion FromToRotation(Quaternion from, Quaternion to) {
        if (to == from) return Quaternion.identity;

        return to * Quaternion.Inverse(from);
    }

    // void OnDrawGizmos()
    void OnDrawGizmosSelected()
    {
        if (_body == null)
            return;
        Gizmos.color = Color.white;
        Vector3 position = _body.transform.TransformPoint(_body.anchorPosition);
        Quaternion rotation = _body.transform.rotation * _body.anchorRotation;


        for (int i = 0; i < _axisColor.Length; i++)
        {
            var axisColor = _axisColor[i];
            var axis = _axisVector[i];
            Gizmos.color = axisColor;
            // Vector3 rotationEul = _body.transform.TransformDirection(_body.anchorRotation * axis);
            Vector3 rotationEul = rotation * axis;
            Gizmos.DrawSphere(position, SphereSize);
            Vector3 direction = rotationEul;
            Gizmos.DrawRay(position, direction);
            
        }
    }
}
