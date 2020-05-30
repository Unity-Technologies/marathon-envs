using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrackBodyStatesInWorldSpace : MonoBehaviour
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
        public Vector3 LastPosition;
        [HideInInspector]
        public Quaternion LastRotation;
        [HideInInspector]
        public bool LastIsSet;
    }
    public List<TrackBodyStatesInWorldSpace.Stat> Stats;

    internal List<Rigidbody> _rigidbodies;

    // Start is called before the first frame update
    void Awake()
    {
        _rigidbodies = GetComponentsInChildren<Rigidbody>().ToList();
        Stats = _rigidbodies
            .Select(x=> new TrackBodyStatesInWorldSpace.Stat{Name = x.name})
            .ToList();        
    }

    void FixedUpdate()
    {
        float timeDelta = Time.fixedDeltaTime;

        foreach (var rb in _rigidbodies)
        {
            Stat stat = Stats.First(x=>x.Name == rb.name);
            if (!stat.LastIsSet)
            {
                stat.LastPosition = rb.transform.position;
                stat.LastRotation = rb.transform.rotation;
            }
            stat.Position = rb.transform.position;
            stat.Rotation = rb.transform.rotation;
            stat.Velocity = rb.transform.position - stat.LastPosition;
            stat.Velocity /= timeDelta;
            stat.AngualrVelocity = DReConObservationStats.GetAngularVelocity(stat.LastRotation, rb.transform.rotation, timeDelta);
            stat.LastPosition = rb.transform.position;
            stat.LastRotation = rb.transform.rotation;
            stat.LastIsSet = true;
        }        
    }

    public void Reset()
    {
        foreach (var rb in _rigidbodies)
        {
            Stat stat = Stats.First(x=>x.Name == rb.name);
            stat.LastPosition = rb.transform.position;
            stat.LastRotation = rb.transform.rotation;
            stat.Position = rb.transform.position;
            stat.Rotation = rb.transform.rotation;
            stat.Velocity = Vector3.zero;
            stat.AngualrVelocity = Vector3.zero;
            stat.LastPosition = rb.transform.position;
            stat.LastRotation = rb.transform.rotation;
            stat.LastIsSet = true;
        }        
        
    }

    public void CopyStatesTo(GameObject target)
    {
        var targets = target.GetComponentsInChildren<ArticulationBody>().ToList();
        var root = targets.First(x=>x.isRoot);
        root.gameObject.SetActive(false);
        foreach (var stat in Stats)
        {
            var targetRb = targets.First(x=>x.name == stat.Name);
            targetRb.transform.position = stat.Position;
            targetRb.transform.rotation = stat.Rotation;
            // targetRb.velocity = stat.Velocity;
            // targetRb.angularVelocity = stat.AngualrVelocity;

            // var drive = targetRb.yDrive;
            // drive.targetVelocity = stat.AngualrVelocity.x;
            // targetRb.yDrive = drive;

            // drive = targetRb.zDrive;
            // drive.targetVelocity = stat.AngualrVelocity.y;
            // targetRb.zDrive = drive;

            // drive = targetRb.xDrive;
            // drive.targetVelocity = stat.AngualrVelocity.z;
            // targetRb.xDrive = drive;

            targetRb.inertiaTensor = stat.Velocity;
            targetRb.inertiaTensorRotation = Quaternion.Euler(stat.AngualrVelocity);
            if (targetRb.isRoot)
            {
                targetRb.TeleportRoot(stat.Position, stat.Rotation);
            }
        }
        root.gameObject.SetActive(true);
    }
}
