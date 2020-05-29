using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;

public class SensorObservations : MonoBehaviour, IOnSensorCollision 
{
	public List<float> SensorIsInTouch;

	List<GameObject> _sensors;

    // Start is called before the first frame update
    void Start()
    {
        SetupSensors();
    }

    void SetupSensors()
	{
		_sensors = GetComponentsInChildren<SensorBehavior>()
			.Select(x=>x.gameObject)
			.ToList();
		SensorIsInTouch = Enumerable.Range(0,_sensors.Count).Select(x=>0f).ToList();
	}

    public void OnSensorCollisionEnter(Collider sensorCollider, GameObject other)
	{
		//if (string.Compare(other.name, "Terrain", true) !=0)
		if (other.GetComponent<Terrain>() == null)
			return;
		var sensor = _sensors
			.FirstOrDefault(x=>x == sensorCollider.gameObject);
		if (sensor != null) {
			var idx = _sensors.IndexOf(sensor);
			SensorIsInTouch[idx] = 1f;
		}
	}
	public void OnSensorCollisionExit(Collider sensorCollider, GameObject other)
	{
		//if (string.Compare(other.gameObject.name, "Terrain", true) !=0)
		if (other.GetComponent<Terrain>() == null)
			return;
		var sensor = _sensors
			.FirstOrDefault(x=>x == sensorCollider.gameObject);
		if (sensor != null) {
			var idx = _sensors.IndexOf(sensor);
			SensorIsInTouch[idx] = 0f;
		}
	}   
}
