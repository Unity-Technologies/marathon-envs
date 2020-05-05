using UnityEngine;

public static class JointHelper002
{
    public static Quaternion FromToRotation(Quaternion from, Quaternion to) {
        if (to == from) return Quaternion.identity;

        return to * Quaternion.Inverse(from);
    }

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

    public static float NormalizedAngle(float angle) {
        if (angle < 180) {
            return angle * Mathf.Deg2Rad;
        }        
        return (angle - 360) * Mathf.Deg2Rad;
    }
}


