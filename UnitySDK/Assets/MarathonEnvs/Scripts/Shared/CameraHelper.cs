using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraHelper : MonoBehaviour {

	Transform _camera;
	Vector3 _defaultCameraRotation;
	SmoothFollow _smoothFollow;
	float _cameraRotY;
	float _defaultTimeScale;
	float _defaultHeight;
	float _defaultDistance;
	float _height = 0f;
	float _zoom = 0;
	// Use this for initialization
	void Start () {
		_defaultTimeScale = 1f;
		_camera = transform;
		_defaultCameraRotation = _camera.eulerAngles;
		_cameraRotY = _defaultCameraRotation.y;
		_smoothFollow = GetComponent<SmoothFollow>();
		_defaultHeight = _smoothFollow.height;
		_defaultDistance = _smoothFollow.distance;
	}
	
	// Update is called once per frame
	void Update () {
		// if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown("`") || Input.GetKeyDown(KeyCode.Delete))
		// {
		// 	ToggleTimeScale(_defaultTimeScale);
		// 	SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
		// }

		if (Input.GetKeyDown("0"))
		{
			ToggleTimeScale(0f);
		}
		if (Input.GetKeyDown("1"))
		{
			ToggleTimeScale(.05f);
		}
		if (Input.GetKeyDown("2"))
		{
			ToggleTimeScale(0.1f);
		}
		if (Input.GetKeyDown("3"))
		{
			ToggleTimeScale(0.2f);
		}
		if (Input.GetKeyDown("a") || Input.GetKeyDown(KeyCode.LeftArrow))
		{
			AddAngle(30f);
		}
		if (Input.GetKeyDown("d") || Input.GetKeyDown(KeyCode.RightArrow))
		{
			AddAngle(-30f);
		}
		if (Input.GetKeyDown("w") || Input.GetKeyDown(KeyCode.UpArrow))
		{
			Height(+1f);
		}
		if (Input.GetKeyDown("s") || Input.GetKeyDown(KeyCode.DownArrow))
		{
			Height(-1f);
		}
		if (Input.GetKeyDown("e"))
		{
			Zoom(-1f);
		}
		if (Input.GetKeyDown("q"))
		{
			Zoom(1f);
		}
		if (Input.GetKeyDown(KeyCode.Return))
		{
			ResetCamera();
		}

	}
	void Zoom(float diff)
	{
		_zoom += diff;
		_zoom = Mathf.Clamp(_zoom, -10f, 10f);
		_smoothFollow.distance = _defaultDistance + (_zoom /5);
	}
	void Height(float diff)
	{
		_height += diff;
		_height = Mathf.Clamp(_height, -10f, 10f);
		_smoothFollow.height = _defaultHeight + (_height /3);
	}
	void AddAngle(float diff)
	{
		_cameraRotY += diff;
		if (_cameraRotY <= -360)
			_cameraRotY += 720;
		if (_cameraRotY >= 360)
			_cameraRotY -= 720;
		SetAngle(_cameraRotY);
	}
	void ResetCamera()
	{
		_cameraRotY = _defaultCameraRotation.y;
		_zoom = 0f;
		_height = 0f;
		SetAngle(_cameraRotY);
	}
	void SetAngle(float newAngle)
	{
		_camera.eulerAngles = new Vector3(0f, newAngle, 0f);
	}
	void ToggleTimeScale(float newTimeScale)
	{
		float setTimeScale = newTimeScale;
		if (Time.timeScale == newTimeScale)
			setTimeScale = _defaultTimeScale;
		Time.timeScale = setTimeScale;
	}
}
