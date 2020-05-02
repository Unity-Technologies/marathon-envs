using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;

public class StyleTransfer002Animator : MonoBehaviour, IOnSensorCollision {

	internal Animator anim;

	public List<float> SensorIsInTouch;
	List<GameObject> _sensors;

	public List<AnimationStep> AnimationSteps;
	public bool AnimationStepsReady;
	public bool IsLoopingAnimation;

	[Range(0f,1f)]
	public float NormalizedTime;
	public float Lenght;

	private List<Vector3> _lastPosition;
	private List<Quaternion> _lastRotation;
	private bool _isRagDoll;

	Quaternion _baseRotation;
	List<Quaternion> _initialRotations;

	public List<BodyPart002> BodyParts;

    private Vector3 _lastVelocityPosition;

	private List<Rigidbody> _rigidbodies;
	private List<Transform> _transforms;

	private bool isFirstOfThisAnim; 

    [System.Serializable]
	public class AnimationStep
	{
		public float TimeStep;
		public float NormalizedTime;
		public List<Vector3> Velocities;
		public Vector3 Velocity;
		public List<Quaternion> RotaionVelocities;
		public List<Vector3> AngularVelocities;
		public List<Vector3> RootAngles;

		public List<Vector3> Positions;
		public List<Quaternion> Rotaions;
		public List<string> Names;
		public Vector3 CenterOfMass;
		public Vector3 TransformPosition;
		public Quaternion TransformRotation;
		public List<float> SensorIsInTouch;

	}

	public BodyConfig BodyConfig;

	// Use this for initialization
	public void OnInitializeAgent()
    {
		anim = GetComponent<Animator>();
		anim.Play("Record",0, NormalizedTime);
		anim.Update(0f);
		AnimationSteps = new List<AnimationStep>();

		if (_rigidbodies == null || _transforms == null)
		{
			_rigidbodies = GetComponentsInChildren<Rigidbody>().ToList();
			_transforms = GetComponentsInChildren<Transform>().ToList();
		}

		_baseRotation = 
			_transforms
			.First(x=> BodyConfig.GetBodyPartGroup(x.name) == BodyConfig.GetRootBodyPart())
			.rotation;
		SetupSensors();
	}
	void Awake()
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
	void Reset()
	{
		BodyParts = new List<BodyPart002> ();
		BodyPart002 root = null;

		if (_rigidbodies == null || _transforms == null)
		{
			_rigidbodies = GetComponentsInChildren<Rigidbody>().ToList();
			_transforms = GetComponentsInChildren<Transform>().ToList();
		}

		foreach (var t in _transforms)
		{
			if (BodyConfig.GetBodyPartGroup(t.name) == BodyHelper002.BodyPartGroup.None)
				continue;
			
			var bodyPart = new BodyPart002{
				Rigidbody = t.GetComponent<Rigidbody>(),
				Transform = t,
				Name = t.name,
				Group = BodyConfig.GetBodyPartGroup(t.name), 
			};
			if (bodyPart.Group == BodyConfig.GetRootBodyPart())
				root = bodyPart;
			bodyPart.Root = root;
			bodyPart.Init();
			BodyParts.Add(bodyPart);
		}
		var partCount = BodyParts.Count;
		SetupSensors();

		_lastPosition = Enumerable.Repeat(Vector3.zero, partCount).ToList();
		_lastRotation = Enumerable.Repeat(Quaternion.identity, partCount).ToList();
		_lastVelocityPosition = transform.position;
		_initialRotations = BodyParts
			.Select(x=> x.Transform.rotation)
			.ToList();
		BecomeAnimated();
	}

	public StyleTransfer002Animator GetFirstOfThisAnim()
	{
		if (isFirstOfThisAnim)
			return this;
		var anim = GetComponent<Animator>();
		var styleAnimators = FindObjectsOfType<StyleTransfer002Animator>().ToList();
		var firstOfThisAnim = styleAnimators
			.Where(x=> x.GetComponent<Animator>().avatar == anim.avatar)
			.FirstOrDefault(x=> x.isFirstOfThisAnim);
		if (firstOfThisAnim != null)
			return firstOfThisAnim;
		isFirstOfThisAnim = true;
		return this;
	}
	
	public void OnAgentAction() {
			
		if (AnimationStepsReady){
			MimicAnimation();
			return;
		}
		if (_lastPosition == null)
			Reset();
		AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
		AnimatorClipInfo[] clipInfo = anim.GetCurrentAnimatorClipInfo(0);
		Lenght = stateInfo.length;
		NormalizedTime = stateInfo.normalizedTime;
		IsLoopingAnimation = stateInfo.loop;
		var timeStep = stateInfo.length * stateInfo.normalizedTime;
		var endTime = 1f;
		if (IsLoopingAnimation)
			endTime = 3f;
		if (NormalizedTime <= endTime) {
			MimicAnimation();
			if (!AnimationStepsReady)
				UpdateAnimationStep(timeStep);
		}
		else {
			StopAnimation();
			// BecomeRagDoll();
		}
	}
	void UpdateAnimationStep(float timeStep)
    {
		// HACK deal with two of first frame
		if (NormalizedTime == 0f && AnimationSteps.FirstOrDefault(x=>x.NormalizedTime == 0f) != null)
			return;

		// var c = _master.Muscles.Count;
		var c = BodyParts.Count;
		var animStep = new AnimationStep();
		animStep.TimeStep = timeStep;
		animStep.NormalizedTime = NormalizedTime;
		animStep.Velocities = Enumerable.Repeat(Vector3.zero, c).ToList();
		animStep.RotaionVelocities = Enumerable.Repeat(Quaternion.identity, c).ToList();
		animStep.AngularVelocities = Enumerable.Repeat(Vector3.zero, c).ToList();
		animStep.RootAngles = Enumerable.Repeat(Vector3.zero, c).ToList();
		animStep.Positions = Enumerable.Repeat(Vector3.zero, c).ToList();
		animStep.Rotaions = Enumerable.Repeat(Quaternion.identity, c).ToList();
		animStep.Velocity = transform.position - _lastVelocityPosition;
		animStep.Names = BodyParts.Select(x=>x.Name).ToList();
		animStep.SensorIsInTouch = new List<float>(SensorIsInTouch);
		_lastVelocityPosition = transform.position;

		var rootBone = BodyParts[0];

		foreach (var bodyPart in BodyParts)
		{
			var i = BodyParts.IndexOf(bodyPart);
			if (i ==0) {
				animStep.Rotaions[i] = Quaternion.Inverse(_baseRotation) * bodyPart.Transform.rotation;
				animStep.Positions[i] =  bodyPart.Transform.position - bodyPart.InitialRootPosition;
				animStep.RootAngles[i] = animStep.Rotaions[i].eulerAngles;
			}
			else {
				animStep.Rotaions[i] = Quaternion.Inverse(_baseRotation) * bodyPart.Transform.rotation;
				animStep.RootAngles[i] = animStep.Rotaions[i].eulerAngles;
				animStep.Positions[i] =  bodyPart.Transform.position - rootBone.Transform.position;
			}
			
			if (NormalizedTime != 0f) {
				animStep.Velocities[i] = bodyPart.Transform.position - _lastPosition[i];
				animStep.RotaionVelocities[i] = JointHelper002.FromToRotation(_lastRotation[i], bodyPart.Transform.rotation);
				animStep.AngularVelocities[i] = animStep.RotaionVelocities[i].eulerAngles;
			}
			_lastPosition[i] = bodyPart.Transform.position;
			_lastRotation[i] = bodyPart.Transform.rotation;

		}
		animStep.CenterOfMass = GetCenterOfMass();
		animStep.TransformPosition = transform.position;
		animStep.TransformRotation = transform.rotation;
		AnimationSteps.Add(animStep);
    }
	public void BecomeAnimated()
	{
		if (_rigidbodies == null || _transforms == null)
		{
			_rigidbodies = GetComponentsInChildren<Rigidbody>().ToList();
			_transforms = GetComponentsInChildren<Transform>().ToList();
		}
		foreach (var rb in _rigidbodies)
		{
			rb.isKinematic = true;
		}
		_isRagDoll = false;
	}
	public void BecomeRagDoll()
	{
		if (_rigidbodies == null || _transforms == null)
		{
			_rigidbodies = GetComponentsInChildren<Rigidbody>().ToList();
			_transforms = GetComponentsInChildren<Transform>().ToList();
		}
		foreach (var rb in _rigidbodies)
		{
			rb.isKinematic = false;
		}
		_isRagDoll = true;
	}
	public void StopAnimation()
	{
		AnimationStepsReady = true;
		anim.enabled=false;
	}

	public void DestoryIfNotFirstAnim()
    {
		if (!isFirstOfThisAnim){
			Destroy(this.gameObject);
        }
    }
    Vector3 GetCenterOfMass()
	{
		var centerOfMass = Vector3.zero;
		float totalMass = 0f;
		var bodies = BodyParts
			.Select(x=>x.Rigidbody)
			.Where(x=>x!=null)
			.ToList();
		foreach (Rigidbody rb in bodies)
		{
			centerOfMass += rb.worldCenterOfMass * rb.mass;
			totalMass += rb.mass;
		}
		centerOfMass /= totalMass;
		centerOfMass -= transform.parent.position;
		return centerOfMass;
	}
	public void MimicAnimation()
	{
		if (!anim.enabled)
			return;

        MimicBone("butt", 			"mixamorig:Hips", 			new Vector3(.0f, -.055f, .0f), 			Quaternion.Euler(90, 0f, 0f));
        MimicBone("lower_waist",    "mixamorig:Spine",          new Vector3(.0f, .0153f, .0f), 			Quaternion.Euler(90, 0f, 0f));
        MimicBone("torso",          "mixamorig:Spine2",         new Vector3(.0f, .04f, .0f), 			Quaternion.Euler(90, 0f, 0f));

        MimicBone("left_upper_arm",   "mixamorig:LeftArm", "mixamorig:LeftForeArm", new Vector3(.0f, .0f, .0f), Quaternion.Euler(0, 45, 180));
        MimicBone("left_larm",        "mixamorig:LeftForeArm",  "mixamorig:LeftHand", new Vector3(.0f, .0f, .0f), Quaternion.Euler(0, -180-45, 180));
        
        MimicBone("right_upper_arm",  "mixamorig:RightArm", "mixamorig:RightForeArm",      new Vector3(.0f, .0f, .0f), Quaternion.Euler(0, 180-45, 180));
        MimicBone("right_larm",       "mixamorig:RightForeArm", "mixamorig:RightHand",  new Vector3(.0f, .0f, .0f), Quaternion.Euler(0, 90-45, 180));

        MimicBone("left_thigh",       "mixamorig:LeftUpLeg",  "mixamorig:LeftLeg",    new Vector3(.0f, .0f, .0f), 			Quaternion.Euler(0, 0, 180));
        MimicBone("left_shin",        "mixamorig:LeftLeg",    "mixamorig:LeftFoot",   new Vector3(.0f, .02f, .0f), 			Quaternion.Euler(0, 0, 180));

        MimicBone("right_thigh",      "mixamorig:RightUpLeg", "mixamorig:RightLeg", new Vector3(.0f, .0f, .0f), 			Quaternion.Euler(0, 0, 180));
        MimicBone("right_shin",       "mixamorig:RightLeg",   "mixamorig:RightFoot", new Vector3(.0f, .02f, .0f), 			Quaternion.Euler(0, 0, 180));

        MimicRightFoot("right_right_foot", new Vector3(.0f, -.0f, -.0f),  			Quaternion.Euler(3, -90, 180));//3));
        MimicLeftFoot("left_left_foot",   new Vector3(-.0f, -.0f, -.0f), 			Quaternion.Euler(-8, -90, 180));//3));
		

	}
	void MimicBone(string name, string bodyPartName, Vector3 offset, Quaternion rotationOffset)
	{
		if (_rigidbodies == null || _transforms == null)
		{
			_rigidbodies = GetComponentsInChildren<Rigidbody>().ToList();
			_transforms = GetComponentsInChildren<Transform>().ToList();
		}

		var bodyPart = _transforms.First(x=>x.name == bodyPartName);
		var target = _rigidbodies.First(x=>x.name == name);

		target.transform.position = bodyPart.transform.position + offset;
		target.transform.rotation = bodyPart.transform.rotation * rotationOffset;
	}

	void MimicBone(string name, string animStartName, string animEndtName, Vector3 offset, Quaternion rotationOffset)
	{
		if (_rigidbodies == null || _transforms == null)
		{
			_rigidbodies = GetComponentsInChildren<Rigidbody>().ToList();
			_transforms = GetComponentsInChildren<Transform>().ToList();
		}


		var animStartBone = _transforms.First(x=>x.name == animStartName);
		var animEndBone = _transforms.First(x=>x.name == animEndtName);
		var target = _rigidbodies.First(x=>x.name == name);

		var pos = (animEndBone.transform.position - animStartBone.transform.position);
		target.transform.position = animStartBone.transform.position + (pos/2) + offset;
		target.transform.rotation = animStartBone.transform.rotation * rotationOffset;
	}
	[Range(0f,1f)]
	public float toePositionOffset = .3f;
	[Range(0f,1f)]
	public float toeRotationOffset = .7f;
	void MimicLeftFoot(string name, Vector3 offset, Quaternion rotationOffset)
	{
		string animStartName = "mixamorig:LeftFoot";
		string animEndtName = "mixamorig:LeftToe_End";
		if (_rigidbodies == null || _transforms == null)
		{
			_rigidbodies = GetComponentsInChildren<Rigidbody>().ToList();
			_transforms = GetComponentsInChildren<Transform>().ToList();
		}

		var animStartBone = _transforms.First(x=>x.name == animStartName);
		var animEndBone = _transforms.First(x=>x.name == animEndtName);
		var target = _rigidbodies.First(x=>x.name == name);

		var rotation = Quaternion.Lerp(animStartBone.rotation, animEndBone.rotation, toeRotationOffset);
		var skinOffset = (animEndBone.transform.position - animStartBone.transform.position);
		target.transform.position = animStartBone.transform.position + (skinOffset * toePositionOffset) + offset;
		target.transform.rotation = rotation * rotationOffset;
	}
	void MimicRightFoot(string name, Vector3 offset, Quaternion rotationOffset)
	{
		string animStartName = "mixamorig:RightFoot";
		string animEndtName = "mixamorig:RightToe_End";
		if (_rigidbodies == null || _transforms == null)
		{
			_rigidbodies = GetComponentsInChildren<Rigidbody>().ToList();
			_transforms = GetComponentsInChildren<Transform>().ToList();
		}


		var animStartBone = _transforms.First(x=>x.name == animStartName);
		var animEndBone = _transforms.First(x=>x.name == animEndtName);
		var target = _rigidbodies.First(x=>x.name == name);

		var rotation = Quaternion.Lerp(animStartBone.rotation, animEndBone.rotation, toeRotationOffset);
		var skinOffset = (animEndBone.transform.position - animStartBone.transform.position);
		target.transform.position = animStartBone.transform.position + (skinOffset * toePositionOffset) + offset;
		target.transform.rotation = rotation * rotationOffset;

	}

	public void OnSensorCollisionEnter(Collider sensorCollider, GameObject other)
	{
		if (string.Compare(other.name, "Terrain", true) !=0)
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
		if (string.Compare(other.gameObject.name, "Terrain", true) !=0)
			return;
		var sensor = _sensors
			.FirstOrDefault(x=>x == sensorCollider.gameObject);
		if (sensor != null) {
			var idx = _sensors.IndexOf(sensor);
			SensorIsInTouch[idx] = 0f;
		}
	}  
}
