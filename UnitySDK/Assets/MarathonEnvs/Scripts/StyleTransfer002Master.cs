using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAgents;
using System;

public class StyleTransfer002Master : MonoBehaviour {
	public float FixedDeltaTime = 0.005f;
	public bool visualizeAnimator = true;

	// general observations
	public List<Muscle002> Muscles;
	public List<BodyPart002> BodyParts;
	public float ObsPhase;
	public Vector3 ObsCenterOfMass;
	public Vector3 ObsVelocity;

	// model observations
	// i.e. model = difference between mocap and actual)
	// ideally we dont want to generate model at inference
	// public float PositionDistance;
	public float EndEffectorDistance; // feet, hands, head
	public float FeetRotationDistance; 
	public float EndEffectorVelocityDistance; // feet, hands, head
	public float RotationDistance;
	public float VelocityDistance;
	public float CenterOfMassDistance;
	public float SensorDistance;

	public float MaxEndEffectorDistance; // feet, hands, head
	public float MaxFeetRotationDistance; 
	public float MaxEndEffectorVelocityDistance; // feet, hands, head
	public float MaxRotationDistance;
	public float MaxVelocityDistance;
	public float MaxCenterOfMassDistance;
	public float MaxSensorDistance;

	


	// debug variables
	public bool IgnorRewardUntilObservation;
	public float ErrorCutoff;
	public bool DebugShowWithOffset;
	public bool DebugMode;
	public bool DebugDisableMotor;
    [Range(-100,100)]
	public int DebugAnimOffset;


	public float TimeStep;
	public int AnimationIndex;
	public int EpisodeAnimationIndex;
	public int StartAnimationIndex;
	public bool UseRandomIndexForTraining;
	public bool UseRandomIndexForInference;
	public bool CameraFollowMe;
	public Transform CameraTarget;

	private bool _isDone;
	bool _resetCenterOfMassOnLastUpdate;
	bool _fakeVelocity;
	bool _waitingForAnimation;


	// public List<float> vector;

	private StyleTransfer002Animator _muscleAnimator;
	private StyleTransfer002Agent _agent;
	StyleTransfer002Animator _styleAnimator;
	StyleTransfer002Animator _localStyleAnimator;
	DecisionRequester _decisionRequester;
	// private StyleTransfer002TrainerAgent _trainerAgent;
	// private Brain _brain;
	public bool IsInferenceMode;
	bool _phaseIsRunning;
    UnityEngine.Random _random = new UnityEngine.Random();
	Vector3 _lastCenterOfMass;

	public BodyConfig BodyConfig;

	// Use this for initialization
	void Awake () {
		foreach (var rb in GetComponentsInChildren<Rigidbody>())
		{
			if (rb.useGravity == false)
				rb.solverVelocityIterations = 255;
		}
		var masters = FindObjectsOfType<StyleTransfer002Master>().ToList();
		if (masters.Count(x=>x.CameraFollowMe) < 1)
			CameraFollowMe = true;
	}

	public void OnInitializeAgent()
    {
		Time.fixedDeltaTime = FixedDeltaTime;
		_waitingForAnimation = true;
		_decisionRequester = GetComponent<DecisionRequester>();

		BodyParts = new List<BodyPart002> ();
		BodyPart002 root = null;
		foreach (var t in GetComponentsInChildren<Transform>())
		{
			if (BodyConfig.GetBodyPartGroup(t.name) == BodyHelper002.BodyPartGroup.None)
				continue;
			
			var bodyPart = new BodyPart002{
				Rigidbody = t.GetComponent<Rigidbody>(),
				Transform = t,
				Name = t.name,
				Group = BodyConfig.GetBodyPartGroup(t.name), 
			};
			if (bodyPart.Group == BodyHelper002.BodyPartGroup.Hips)
				root = bodyPart;
			bodyPart.Root = root;
			bodyPart.Init();
			BodyParts.Add(bodyPart);
		}
		var partCount = BodyParts.Count;

		Muscles = new List<Muscle002> ();
		var muscles = GetComponentsInChildren<ConfigurableJoint>();
		ConfigurableJoint rootConfigurableJoint = null;
		var ragDoll = GetComponent<RagDoll002>();
		foreach (var m in muscles)
		{
			var maximumForce = ragDoll.MusclePowers.First(x=>x.Muscle == m.name).PowerVector;
			// maximumForce *= 2f;
			var muscle = new Muscle002{
				Rigidbody = m.GetComponent<Rigidbody>(),
				Transform = m.GetComponent<Transform>(),
				ConfigurableJoint = m,
				Name = m.name,
				Group = BodyConfig.GetMuscleGroup(m.name),
				MaximumForce = maximumForce
			};
			if (muscle.Group == BodyHelper002.MuscleGroup.Hips)
				rootConfigurableJoint = muscle.ConfigurableJoint;
			muscle.RootConfigurableJoint = rootConfigurableJoint;
			muscle.Init();

			Muscles.Add(muscle);			
		}
		var spawnableEnv = GetComponentInParent<SpawnableEnv>();
		_localStyleAnimator = spawnableEnv.gameObject.GetComponentInChildren<StyleTransfer002Animator>();
		_styleAnimator = _localStyleAnimator.GetFirstOfThisAnim();
		// _styleAnimator = _localStyleAnimator;
		_muscleAnimator = _styleAnimator;
		_agent = GetComponent<StyleTransfer002Agent>();
		// _trainerAgent = GetComponent<StyleTransfer002TrainerAgent>();
		// _brain = FindObjectsOfType<Brain>().First(x=>x.name=="LearnFromMocapBrain");
		IsInferenceMode = !Academy.Instance.IsCommunicatorOn;
	}
	
	// Update is called once per frame
	void Update () {
	}
	static float SumAbs(Vector3 vector)
	{
		var sum = Mathf.Abs(vector.x);
		sum += Mathf.Abs(vector.y);
		sum += Mathf.Abs(vector.z);
		return sum;
	}
	static float SumAbs(Quaternion q)
	{
		var sum = Mathf.Abs(q.w);
		sum += Mathf.Abs(q.x);
		sum += Mathf.Abs(q.y);
		sum += Mathf.Abs(q.z);
		return sum;
	}

	public void OnAgentAction()
	{
		if (_waitingForAnimation && _styleAnimator.AnimationStepsReady){
			_waitingForAnimation = false;
			ResetPhase();
		}
		var animStep = UpdateObservations();
		Step(animStep);
	}
	StyleTransfer002Animator.AnimationStep UpdateObservations()
	{
		if (DebugMode)
			AnimationIndex = 0;
		var debugStepIdx = AnimationIndex;
		StyleTransfer002Animator.AnimationStep animStep = null;
		StyleTransfer002Animator.AnimationStep debugAnimStep = null;
		if (_phaseIsRunning) {
				debugStepIdx += DebugAnimOffset;
			if (DebugShowWithOffset){
				debugStepIdx = Mathf.Clamp(debugStepIdx, 0, _muscleAnimator.AnimationSteps.Count);
				debugAnimStep = _muscleAnimator.AnimationSteps[debugStepIdx];
			}
			animStep = _muscleAnimator.AnimationSteps[AnimationIndex];
		}
		EndEffectorDistance = 0f;
		FeetRotationDistance = 0f;
		EndEffectorVelocityDistance = 0;
		RotationDistance = 0f;
		VelocityDistance = 0f;
		CenterOfMassDistance = 0f;
		SensorDistance = 0f;
		if (_phaseIsRunning && DebugShowWithOffset)
			MimicAnimationFrame(debugAnimStep);
		else if (_phaseIsRunning)
			CompareAnimationFrame(animStep);
		foreach (var muscle in Muscles)
		{
			var i = Muscles.IndexOf(muscle);
			muscle.UpdateObservations();
			if (!DebugShowWithOffset && !DebugDisableMotor)
				muscle.UpdateMotor();
			if (!muscle.Rigidbody.useGravity)
				continue; // skip sub joints
		}
		foreach (var bodyPart in BodyParts)
		{
			if (_phaseIsRunning){
				bodyPart.UpdateObservations();
				
				var rotDistance = bodyPart.ObsAngleDeltaFromAnimationRotation;
				var squareRotDistance = Mathf.Pow(rotDistance,2);
				RotationDistance += squareRotDistance;
				// RotationDistance += rotDistance;
				if (bodyPart.Group == BodyHelper002.BodyPartGroup.Hand
					|| bodyPart.Group == BodyHelper002.BodyPartGroup.Torso
					|| bodyPart.Group == BodyHelper002.BodyPartGroup.Foot)
				{
					EndEffectorDistance += bodyPart.ObsDeltaFromAnimationPosition.sqrMagnitude;
					// EndEffectorDistance += bodyPart.ObsDeltaFromAnimationPosition.magnitude;
				}
				if (bodyPart.Group == BodyHelper002.BodyPartGroup.Foot)
				{
					FeetRotationDistance += squareRotDistance;
					// EndEffectorRotDistance += rotDistance;
				}
			}
		}

		// RotationDistance *= RotationDistance; // take the square;
		ObsCenterOfMass = GetCenterOfMass();
		if (_phaseIsRunning)
			CenterOfMassDistance = (animStep.CenterOfMass - ObsCenterOfMass).sqrMagnitude;
		ObsVelocity = ObsCenterOfMass-_lastCenterOfMass;
		if (_fakeVelocity)
			ObsVelocity = animStep.Velocity;
		_lastCenterOfMass = ObsCenterOfMass;
		if (!_resetCenterOfMassOnLastUpdate)
			_fakeVelocity = false;

		if (_phaseIsRunning){
			var animVelocity = animStep.Velocity / Time.fixedDeltaTime;
			ObsVelocity /= Time.fixedDeltaTime;
			var velocityDistance = ObsVelocity-animVelocity;
			VelocityDistance = velocityDistance.sqrMagnitude;
			var sensorDistance = 0.0;
			var sensorDistanceStep = 1.0 / _agent.SensorIsInTouch.Count;
			for (int i = 0; i < _agent.SensorIsInTouch.Count; i++)
			{
				if (animStep.SensorIsInTouch[i] != _agent.SensorIsInTouch[i])
					sensorDistance += sensorDistanceStep;
			}
			SensorDistance = (float) sensorDistance;
		}
		// normalize distances
		// VelocityDistance /= 10f;
		// VelocityDistance = Mathf.Clamp(VelocityDistance, -1f, 1f);
		// CenterOfMassDistance

		if (!IgnorRewardUntilObservation){
			MaxEndEffectorDistance = Mathf.Max(MaxEndEffectorDistance, EndEffectorDistance);
			MaxFeetRotationDistance = Mathf.Max(MaxFeetRotationDistance, FeetRotationDistance);
			MaxEndEffectorVelocityDistance = Mathf.Max(MaxEndEffectorVelocityDistance, EndEffectorVelocityDistance);
			MaxRotationDistance = Mathf.Max(MaxRotationDistance, RotationDistance);
			MaxVelocityDistance = Mathf.Max(MaxVelocityDistance, VelocityDistance);
			MaxCenterOfMassDistance = Mathf.Max(MaxCenterOfMassDistance, CenterOfMassDistance);
			MaxSensorDistance = Mathf.Max(MaxSensorDistance, SensorDistance);
		}

		if (IgnorRewardUntilObservation)
			IgnorRewardUntilObservation = false;
		ObsPhase = _muscleAnimator.AnimationSteps[AnimationIndex].NormalizedTime % 1f;
		return animStep;
	}
	void Step(StyleTransfer002Animator.AnimationStep animStep)
	{
		if (_phaseIsRunning){
			if (!DebugShowWithOffset)
				AnimationIndex++;
			if (AnimationIndex>=_muscleAnimator.AnimationSteps.Count) {
				//ResetPhase();
				Done();
				AnimationIndex--;
			}
		}
		if (_phaseIsRunning && IsInferenceMode && CameraFollowMe)
		{
			_muscleAnimator.anim.enabled = true;
			_muscleAnimator.anim.Play("Record",0, animStep.NormalizedTime);
			_muscleAnimator.anim.transform.position = animStep.TransformPosition;
			_muscleAnimator.anim.transform.rotation = animStep.TransformRotation;
		}
	}
	void CompareAnimationFrame(StyleTransfer002Animator.AnimationStep animStep)
	{
		MimicAnimationFrame(animStep, true);
	}

	void MimicAnimationFrame(StyleTransfer002Animator.AnimationStep animStep, bool onlySetAnimation = false)
	{
		if (!onlySetAnimation)
		{
			foreach (var rb in GetComponentsInChildren<Rigidbody>())
			{
				rb.angularVelocity = Vector3.zero;
				rb.velocity = Vector3.zero;
			}
		}
		foreach (var bodyPart in BodyParts)
		{
			var i = animStep.Names.IndexOf(bodyPart.Name);
			Vector3 animPosition = bodyPart.InitialRootPosition + animStep.Positions[0];
            Quaternion animRotation = bodyPart.InitialRootRotation * animStep.Rotaions[0];
			if (i != 0) {
				animPosition += animStep.Positions[i];
				animRotation = bodyPart.InitialRootRotation * animStep.Rotaions[i];
			}
			Vector3 angularVelocity = animStep.AngularVelocities[i] / Time.fixedDeltaTime;
			Vector3 velocity = animStep.Velocities[i] / Time.fixedDeltaTime;
			// angularVelocity = Vector3.zero;
			// velocity = Vector3.zero;
			bool setAnim = !onlySetAnimation;
			if (bodyPart.Name.Contains("head") || bodyPart.Name.Contains("upper_waist"))
				setAnim = false;
			if (setAnim)
				bodyPart.MoveToAnim(animPosition, animRotation, angularVelocity, velocity);
			bodyPart.SetAnimationPosition(animStep.Positions[i], animStep.Rotaions[i]);
		}
	}

	protected virtual void LateUpdate() {
		if (_resetCenterOfMassOnLastUpdate){
			ObsCenterOfMass = GetCenterOfMass();
			_lastCenterOfMass = ObsCenterOfMass;
			_resetCenterOfMassOnLastUpdate = false;
		}
		#if UNITY_EDITOR
			VisualizeTargetPose();
		#endif
	}

	public bool IsDone()
	{
		return _isDone;
	}
	void Done()
	{
		_isDone = true;
	}

	public void ResetPhase()
	{
		if (_waitingForAnimation)
			return;
		_decisionRequester.enabled = true;
		// _trainerAgent.SetBrainParams(_muscleAnimator.AnimationSteps.Count);
		_agent.SetTotalAnimFrames(_muscleAnimator.AnimationSteps.Count);
		// _trainerAgent.RequestDecision(_agent.AverageReward);
		SetStartIndex(0); // HACK for gym
		UpdateObservations();
	}

	public void SetStartIndex(int startIdx)
	{
		_decisionRequester.enabled = false;

		// _animationIndex =  UnityEngine.Random.Range(0, _muscleAnimator.AnimationSteps.Count);
		if (!_phaseIsRunning){
			StartAnimationIndex = _muscleAnimator.AnimationSteps.Count-1;
			EpisodeAnimationIndex = _muscleAnimator.AnimationSteps.Count-1;
			AnimationIndex = EpisodeAnimationIndex;
			if (CameraFollowMe){
				var camera = FindObjectOfType<Camera>();
				var follow = camera.GetComponent<SmoothFollow>();
				follow.target = CameraTarget;
			}
		}
		// // ErrorCutoff = UnityEngine.Random.Range(-15f, 2f);
		// // ErrorCutoff = UnityEngine.Random.Range(-10f, 1f);
		// // ErrorCutoff = UnityEngine.Random.Range(-5f, 1f);
		// ErrorCutoff = UnityEngine.Random.Range(-3f, .5f);
		// if (IsInferenceMode)
		// 	ErrorCutoff = UnityEngine.Random.Range(-3f, -3f);
		// var lastLenght = AnimationIndex - EpisodeAnimationIndex;
		// if (lastLenght >=  _muscleAnimator.AnimationSteps.Count-2){
		// 	StartAnimationIndex = _muscleAnimator.AnimationSteps.Count-1;
		// 	// StartAnimationIndex = 0;
		// 	// ErrorCutoff += 0.25f;
		// 	EpisodeAnimationIndex = _muscleAnimator.AnimationSteps.Count-1;
		// 	AnimationIndex = EpisodeAnimationIndex;
		// }

		// // start with random
		// AnimationIndex = UnityEngine.Random.Range(0, _muscleAnimator.AnimationSteps.Count);
		// // AnimationIndex = StartAnimationIndex;
		// if (IsInferenceMode && !UseRandomIndexForInference){
		// 	AnimationIndex = 1;
		// } else if (!IsInferenceMode && !UseRandomIndexForTraining) {
		// 	var minIdx = StartAnimationIndex;
		// 	if (_muscleAnimator.IsLoopingAnimation)
		// 		minIdx = minIdx == 0 ? 1 : minIdx;
		// 	var maxIdx = _muscleAnimator.AnimationSteps.Count-1;
		// 	var range = 30f;//maxIdx-minIdx;
		// 	var rnd = (NextGaussian() /3f) * (float) range;
		// 	var idx = Mathf.Clamp((float)minIdx + rnd, minIdx, (float)maxIdx);
		// 	AnimationIndex = (int)idx;
		// }
		// // AnimationIndex = StartAnimationIndex;

		AnimationIndex = startIdx;
		if (_decisionRequester?.DecisionPeriod > 1)
			AnimationIndex *= this._decisionRequester.DecisionPeriod;
		StartAnimationIndex = AnimationIndex;
		EpisodeAnimationIndex = AnimationIndex;
		_phaseIsRunning = true;
		_isDone = false;
		var animStep = _muscleAnimator.AnimationSteps[AnimationIndex];
		TimeStep = animStep.TimeStep;
		EndEffectorDistance = 0f;
		FeetRotationDistance = 0f;
		EndEffectorVelocityDistance = 0f;
		RotationDistance = 0f;
		VelocityDistance = 0f;
		IgnorRewardUntilObservation = true;
		_resetCenterOfMassOnLastUpdate = true;
		_fakeVelocity = true;
		foreach (var muscle in Muscles)
			muscle.Init();
		foreach (var bodyPart in BodyParts)
			bodyPart.Init();
		MimicAnimationFrame(animStep);
		EpisodeAnimationIndex = AnimationIndex;
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

	float NextGaussian(float mu = 0, float sigma = 1)
	{
		var u1 = UnityEngine.Random.value;
		var u2 = UnityEngine.Random.value;

		var rand_std_normal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) *
							Mathf.Sin(2.0f * Mathf.PI * u2);

		var rand_normal = mu + sigma * rand_std_normal;

		return rand_normal;
	}

	private void VisualizeTargetPose() {
		if (!visualizeAnimator) return;
		if (!Application.isEditor) return;

		// foreach (Muscle002 m in Muscles) {
		// 	if (m.ConfigurableJoint.connectedBody != null && m.connectedBodyTarget != null) {
		// 		Debug.DrawLine(m.target.position, m.connectedBodyTarget.position, Color.cyan);
				
		// 		bool isEndMuscle = true;
		// 		foreach (Muscle002 m2 in Muscles) {
		// 			if (m != m2 && m2.ConfigurableJoint.connectedBody == m.rigidbody) {
		// 				isEndMuscle = false;
		// 				break;
		// 			}
		// 		}
				
		// 		if (isEndMuscle) VisualizeHierarchy(m.target, Color.cyan);
		// 	}
		// }
	}
	
	// Recursively visualizes a bone hierarchy
	private void VisualizeHierarchy(Transform t, Color color) {
		for (int i = 0; i < t.childCount; i++) {
			Debug.DrawLine(t.position, t.GetChild(i).position, color);
			VisualizeHierarchy(t.GetChild(i), color);
		}
	}


}
