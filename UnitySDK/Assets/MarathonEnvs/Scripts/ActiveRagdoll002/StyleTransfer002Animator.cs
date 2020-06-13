// A class defining the Animator. The Animator is used as a reference for an
// Agent that mimicks the animator behavior. Before training, the animator's
// animation is run once and all the charateristics of it are stored as animSteps.
// During training, the agent can simply acess the precomputed values and mimick
// Animator's body Part' velocities, positions, rotations, angular velocities, etc. 

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
	private List<Vector3> _lastPositionLocal;
	private List<Quaternion> _lastRotationLocal;

	List<Quaternion> _initialRotations;

	public List<BodyPart002> BodyParts;

	private Vector3 _lastCenterOfMass;

	private List<Rigidbody> _rigidbodies;
	private List<Transform> _transforms;

	private bool isFirstOfThisAnim; 

    [System.Serializable]
	public class AnimationStep
	{
		public float TimeStep;
		public float NormalizedTime;
		public List<Vector3> Velocities;
		public List<Vector3> VelocitiesLocal;
		public Vector3 CenterOfMassVelocity;
		public List<Vector3> AngularVelocities;
		public List<Vector3> AngularVelocitiesLocal;

		public List<Vector3> Positions;
		public List<Quaternion> Rotations;
		public List<string> Names;
		public Vector3 CenterOfMass;
		public Vector3 AngularMoment;
		public Vector3 TransformPosition;
		public Quaternion TransformRotation;
		public List<float> SensorIsInTouch;

	}

	public BodyConfig BodyConfig;
	DecisionRequester _decisionRequester;

	// Use this for initialization
	public void OnInitializeAgent()
    {

		_decisionRequester = GameObject.Find("MarathonMan").GetComponent<DecisionRequester>();

		anim = GetComponent<Animator>();
		anim.Play("Record",0, NormalizedTime);
		anim.Update(0f);
		AnimationSteps = new List<AnimationStep>();

		if (_rigidbodies == null || _transforms == null)
		{
			_rigidbodies = GetComponentsInChildren<Rigidbody>().ToList();
			_transforms = GetComponentsInChildren<Transform>().ToList();
		}

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

    // Reset the animator. 
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

			var bodyPart = new BodyPart002 {
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
		_lastPositionLocal = Enumerable.Repeat(Vector3.zero, partCount).ToList();
		_lastRotationLocal = Enumerable.Repeat(Quaternion.identity, partCount).ToList();
		_lastCenterOfMass = transform.position;
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

    // Mimics the positions of body parts of an animation. Computes AnimStep structure
    // for the step if it is not computed already. 
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

    // Prepares an animation step. Records the positions, rotations, velocities
    // of the rigid bodies forming an animation into the animation step structure. 
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
		animStep.VelocitiesLocal = Enumerable.Repeat(Vector3.zero, c).ToList();
		animStep.AngularVelocities = Enumerable.Repeat(Vector3.zero, c).ToList();
		animStep.AngularVelocitiesLocal = Enumerable.Repeat(Vector3.zero, c).ToList();
		animStep.Positions = Enumerable.Repeat(Vector3.zero, c).ToList();
		animStep.Rotations = Enumerable.Repeat(Quaternion.identity, c).ToList();
		animStep.CenterOfMass = JointHelper002.GetCenterOfMassRelativeToRoot(BodyParts);
		//animStep.CenterOfMass = GetCenterOfMass();

		animStep.CenterOfMassVelocity = animStep.CenterOfMass - _lastCenterOfMass;
		animStep.Names = BodyParts.Select(x=>x.Name).ToList();
		animStep.SensorIsInTouch = new List<float>(SensorIsInTouch);
		_lastCenterOfMass = animStep.CenterOfMass;

		var rootBone = BodyParts[0];
		
		foreach (var bodyPart in BodyParts)
		{
			var i = BodyParts.IndexOf(bodyPart);
			if (i ==0) {
				animStep.Rotations[i] = Quaternion.Inverse(bodyPart.InitialRootRotation) * bodyPart.Transform.rotation;
				animStep.Positions[i] =  bodyPart.Transform.position - bodyPart.InitialRootPosition;
			}
			else {
				animStep.Rotations[i] = Quaternion.Inverse(rootBone.Transform.rotation) * bodyPart.Transform.rotation;
				animStep.Positions[i] =  bodyPart.Transform.position - rootBone.Transform.position;
			}
			
			if (NormalizedTime != 0f) {
				animStep.Velocities[i] = (bodyPart.Transform.position - _lastPosition[i]) / (_decisionRequester.DecisionPeriod * Time.fixedDeltaTime); ;
				animStep.AngularVelocities[i] = JointHelper002.CalcDeltaRotationNormalizedEuler(_lastRotation[i], bodyPart.Transform.rotation) / (_decisionRequester.DecisionPeriod * Time.fixedDeltaTime); ;
				animStep.VelocitiesLocal[i] = (animStep.Positions[i] - _lastPositionLocal[i]) / (_decisionRequester.DecisionPeriod * Time.fixedDeltaTime); ;
				animStep.AngularVelocitiesLocal[i] = JointHelper002.CalcDeltaRotationNormalizedEuler(_lastRotationLocal[i], animStep.Rotations[i]) / (_decisionRequester.DecisionPeriod * Time.fixedDeltaTime);
			}

			if (bodyPart.Rigidbody != null) {
				bodyPart.Rigidbody.angularVelocity = JointHelper002.CalcDeltaRotationNormalizedEuler(bodyPart.Transform.rotation, _lastRotation[i]) / (_decisionRequester.DecisionPeriod * Time.fixedDeltaTime);
				bodyPart.Rigidbody.velocity = (bodyPart.Transform.position - _lastPosition[i]) / (_decisionRequester.DecisionPeriod * Time.fixedDeltaTime);
				bodyPart.Rigidbody.transform.position = bodyPart.Transform.position;
				bodyPart.Rigidbody.transform.rotation = bodyPart.Transform.rotation;
			}

			_lastPosition[i] = bodyPart.Transform.position;
			_lastRotation[i] = bodyPart.Transform.rotation;

			_lastPositionLocal[i] = animStep.Positions[i];
			_lastRotationLocal[i] = animStep.Rotations[i];

		}
		animStep.TransformPosition = transform.position;
		animStep.TransformRotation = transform.rotation;
		animStep.AngularMoment = JointHelper002.GetAngularMoment(BodyParts);
		AnimationSteps.Add(animStep);
    }

    // Sets kinematic flag for Animator's rigid bodies to true
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
	}

    // Sets the kinematic flags for Animator's rigid bodies to false
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
	}

    // Stop the animation
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

    // Sets positions and rotations of the Animator's body Rigid Bodies to match
    // the positions and rotations of the animation avatar. The rotatin and position
    // values passed as arguments to the MimicBone() calls are adjusted so that
    // the avatar's body parts' positions and rotations look identical to the
    // Animator's body parts
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

    // Set position and rotation of a rigid body to match avatar's rigid body
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

	// Set position and rotation of a rigid body to match avatar's rigid body
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
		target.transform.position = animStartBone.transform.position + pos/2 + offset;
		target.transform.rotation = animStartBone.transform.rotation * rotationOffset;
	}

	[Range(0f,1f)]
	public float toePositionOffset = .3f;
	[Range(0f,1f)]
	public float toeRotationOffset = .7f;

	// Set position and rotation of the left foot to match avatar's left foor
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

	// Set position and rotation of the right foot to match avatar's right foot
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

    // Update the array of Sensors In Touch if an Animator's collider collides
    // with an object named "Terrain"
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

    // Update the array of Sensors In Touch if a sensor no more collides with terrain 
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
