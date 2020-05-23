// Helper Utilities to work with agent's rigid bodies charateristics. Allows to
// calculate Angles between rotations in radians, find center of mass of an agent,
// and find Angular Momentum of an agent. 

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public static class JointHelper002 {

    // Return rotation from one rotation to another
    public static Quaternion FromToRotation(Quaternion from, Quaternion to) {
        if (to == from) return Quaternion.identity;

        return to * Quaternion.Inverse(from);
    }

    // Calculate rotation between two rotations in radians. Adjusts the value to lie within [-pi, +pi].
    public static Vector3 NormalizedEulerAngles(Vector3 eulerAngles) {
        var x = eulerAngles.x < 180f ?
            eulerAngles.x :
            -360 + eulerAngles.x;
        var y = eulerAngles.y < 180f ?
            eulerAngles.y :
            -360 + eulerAngles.y;
        var z = eulerAngles.z < 180f ?
            eulerAngles.z :
            -360 + eulerAngles.z;
        x = x * Mathf.Deg2Rad;
        y = y * Mathf.Deg2Rad;
        z = z * Mathf.Deg2Rad;
        return new Vector3(x, y, z);
    }

    // Adjust the value of an angle to lie within [-pi, +pi].
    public static float NormalizedAngle(float angle) {
        if (angle < 180) {
            return angle * Mathf.Deg2Rad;
        }
        return (angle - 360) * Mathf.Deg2Rad;
    }

    // Find rotation and convert to radians within [-pi, +pi].
    public static Vector3 CalcDeltaRotationNormalizedEuler(Quaternion from, Quaternion to) {
        var rotationVelocity = FromToRotation(from, to);
        var angularVelocity = NormalizedEulerAngles(rotationVelocity.eulerAngles);
        return angularVelocity;
    }

    // Find the center of mass of a list of Body Parts beloning to an agent. Relative to the root bone, i. e. "butt" for humanoid. 
    public static Vector3 GetCenterOfMassRelativeToRoot(List<BodyPart002> BodyParts) {
        var centerOfMass = Vector3.zero;
        float totalMass = 0f;
        var bodies = BodyParts
            .Select(x => x.Rigidbody)
            .Where(x => x != null)
            .ToList();
        var rootBone = BodyParts[0];
        foreach (Rigidbody rb in bodies) {
            centerOfMass += rb.worldCenterOfMass * rb.mass;
            totalMass += rb.mass;
        }
        centerOfMass /= totalMass;
        centerOfMass -= rootBone.InitialRootPosition;
        return centerOfMass;
    }

    // Find the center of mass of a List of Body Parts relative to the world coordinate system. 
    public static Vector3 GetCenterOfMassWorld(List<BodyPart002> BodyParts) {
        var centerOfMass = GetCenterOfMassRelativeToRoot(BodyParts) + BodyParts[0].InitialRootPosition;
        return centerOfMass;
    }

    // Calculate Angular Momentum of a List of Body Parts. In the world coordinate system about the center
    // of mass of the Body Parts. Formulas at https://ocw.mit.edu/courses/aeronautics-and-astronautics/16-07-dynamics-fall-2009/lecture-notes/MIT16_07F09_Lec11.pdf
    public static Vector3 GetAngularMoment(List<BodyPart002> BodyParts) {
        var centerOfMass = GetCenterOfMassWorld(BodyParts);
        var bodies = BodyParts
            .Select(x => x.Rigidbody)
            .Where(x => x != null)
            .ToList();
        Vector3 totalAngularMoment = Vector3.zero;
        foreach (Rigidbody rb in bodies) {

            var w_local = rb.transform.rotation * rb.angularVelocity;
            var w_inertiaFrame = rb.inertiaTensorRotation * w_local;

            Vector3 L_inertiaFrame = Vector3.zero;
            L_inertiaFrame[0] = w_inertiaFrame[0] * rb.inertiaTensor[0];
            L_inertiaFrame[1] = w_inertiaFrame[1] * rb.inertiaTensor[1];
            L_inertiaFrame[2] = w_inertiaFrame[2] * rb.inertiaTensor[2];

            Vector3 L_world = Quaternion.Inverse(rb.transform.rotation) * Quaternion.Inverse(rb.inertiaTensorRotation) * L_inertiaFrame;

            Vector3 bodyPartCenterOfMassRelativeTobodyPartsCenterOfMass = rb.worldCenterOfMass - centerOfMass;
            Vector3 LofBodyPartCenterOfMass = rb.mass * Vector3.Cross(bodyPartCenterOfMassRelativeTobodyPartsCenterOfMass, rb.velocity);

            totalAngularMoment += L_world + LofBodyPartCenterOfMass;

        }
        return totalAngularMoment;
    }
}


