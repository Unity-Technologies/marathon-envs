
using MLAgents;
using UnityEngine;



public class MocapAnimatorController : MonoBehaviour
{
    public float MaxForwardVelocity = 1f;        // Max run speed.
    public float MinTurnVelocity = 400f;         // Turn velocity when moving at maximum speed.
    public float MaxTurnVelocity = 1200f;        // Turn velocity when stationary.
    public float JumpSpeed = 10f;                // 
    public bool debugForceJump;
    Animator _anim;
    CharacterController _characterController;
    SpawnableEnv _spawnableEnv;
    InputController _inputController;

    bool _isGrounded;
    bool _previouslyGrounded;
    const float kAirborneTurnSpeedProportion = 5.4f;
    const float kGroundTurnSpeedProportion = 200f;
    const float kGroundedRayDistance = 1f;
    const float kJumpAbortSpeed = 10f;
    const float kMinEnemyDotCoeff = 0.2f;
    const float kInverseOneEighty = 1f / 180f;
    const float kStickingGravityProportion = 0.3f;

    Material materialUnderFoot;
    float _forwardVelocity;
    Vector3 _lastGroundForwardVelocity;
    float _desiredForwardSpeed;
    float _verticalVelocity = -1f;
    
    Quaternion _targetDirection;    // direction we want to move towards
    float _angleDiff;               // delta between targetRotation and current roataion
    Quaternion _targetRotation;
    bool _readyToJump;
    bool _inCombo;
    int _layerMask;


    protected bool IsMoveInput
    {
        get { return !Mathf.Approximately(_inputController.MovementVector.sqrMagnitude, 0f); }
    }


	void Awake()
    {
        _anim = GetComponent<Animator>();
        _characterController = GetComponent<CharacterController>();
        _spawnableEnv = GetComponentInParent<SpawnableEnv>();
        _inputController = _spawnableEnv.GetComponentInChildren<InputController>();
        _targetDirection = Quaternion.Euler(0, 90, 0);
        var ragDoll = _spawnableEnv.GetComponentInChildren<RagDollAgent>();
        _layerMask = 1<<ragDoll.gameObject.layer;
        _layerMask |= 1<<this.gameObject.layer;
        _layerMask = ~(_layerMask);
    }

    void Update()
    {
    }

    void FixedUpdate()
    {
        OnFixedUpdate();
    }
    void OnFixedUpdate()
    {
        // RotateTarget(Time.fixedDeltaTime);
        SetTargetFromMoveInput();
        CalculateForwardMovement(Time.fixedDeltaTime);
        CalculateVerticalMovement(Time.fixedDeltaTime);

        if (this.IsMoveInput)
            SetTargetRotation();

        UpdateOrientation(Time.fixedDeltaTime);

        // PlayAudio();

        // TimeoutToIdle();

        _previouslyGrounded = _isGrounded;
    }

    public void OnReset()
    {
        _isGrounded = true;
        _previouslyGrounded = true;
        _inCombo = false;
        _readyToJump = false;
        _forwardVelocity = 0f;
        _lastGroundForwardVelocity = Vector3.zero;
        _desiredForwardSpeed = 0f;
        _verticalVelocity = 0f;
        _angleDiff = 0f;

        _anim.SetBool("onGround", _isGrounded);
        _anim.SetFloat("verticalVelocity", _verticalVelocity);
        _anim.SetFloat("angleDeltaRad", _angleDiff * Mathf.Deg2Rad);
        _anim.SetFloat("forwardVelocity", _forwardVelocity);
        _anim.SetBool("backflip", false);
        _anim.Rebind();
        _anim.SetBool("onGround", _isGrounded);
        _anim.SetFloat("verticalVelocity", _verticalVelocity);
        _anim.SetFloat("angleDeltaRad", _angleDiff * Mathf.Deg2Rad);
        _anim.SetFloat("forwardVelocity", _forwardVelocity);
        _anim.SetBool("backflip", false);
        OnFixedUpdate();
        _anim.Update(0f);
    }


    // Called each physics step (so long as the Animator component is set to Animate Physics) after FixedUpdate to override root motion.
    void OnAnimatorMove()
    {
        if (_anim == null)
            return;
        Vector3 movement;
        float verticalVelocity = _verticalVelocity;
        if (_isGrounded)
        {
            // find ground
            RaycastHit hit;
            Ray ray = new Ray(transform.position + Vector3.up * kGroundedRayDistance * 0.5f, -Vector3.up);
            if (Physics.Raycast(ray, out hit, kGroundedRayDistance, _layerMask, QueryTriggerInteraction.Ignore))
            {
                // project velocity on plane
                // movement = _anim.deltaPosition;
                // movement.y = 0f;
                movement = Vector3.ProjectOnPlane(_anim.deltaPosition, hit.normal);
                
                // store material under foot
                Renderer groundRenderer = hit.collider.GetComponentInChildren<Renderer>();
                materialUnderFoot = groundRenderer ? groundRenderer.sharedMaterial : null;
            }
            else
            {
                // fail safe incase ray does not collide
                movement = _anim.deltaPosition;
                materialUnderFoot = null;
            }
            _lastGroundForwardVelocity = movement / Time.deltaTime;
        }
        else
        {
            movement = _lastGroundForwardVelocity * Time.deltaTime;
        }
        // Rotate the transform of the character controller by the animation's root rotation.
        _characterController.transform.rotation *= _anim.deltaRotation;
        print ($"delta:{_anim.deltaPosition.magnitude} movement:{movement.magnitude} delta:{_anim.deltaPosition} movement:{movement}");

        // Add to the movement with the calculated vertical speed.
        movement += verticalVelocity * Vector3.up * Time.deltaTime;

        // Move the character controller.
        _characterController.Move(movement);

        // After the movement store whether or not the character controller is grounded.
        _isGrounded = _characterController.isGrounded;

        // If Ellen is not on the ground then send the vertical speed to the animator.
        // This is so the vertical speed is kept when landing so the correct landing animation is played.
        if (!_isGrounded)
            _anim.SetFloat("verticalVelocity", verticalVelocity);

        // Send whether or not Ellen is on the ground to the animator.
        _anim.SetBool("onGround", _isGrounded);
    }

    void RotateTarget(float deltaTime)
    {
        if (!Mathf.Approximately(_inputController.CameraRotation.x*_inputController.CameraRotation.x, 0f))
        {
            float roation = _targetDirection.eulerAngles.y;
            float delta = _inputController.CameraRotation.x * kGroundTurnSpeedProportion * deltaTime;
            roation += delta;
            // print($"{_targetDirection.eulerAngles.y} delta:{delta}, {roation}");
            _targetDirection = Quaternion.Euler(0f, roation, 0f);
        }
    }
    void SetTargetFromMoveInput()
    {
        Vector2 moveInput = _inputController.MovementVector;
        Vector3 localMovementDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        _targetDirection = Quaternion.Euler(localMovementDirection);
    }

    void SetTargetRotation()
    {
        // Create three variables, move input local to the player, flattened forward direction of the camera and a local target rotation.
        Vector2 moveInput = _inputController.MovementVector;
        Vector3 localMovementDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        
        Vector3 forward = _targetDirection * Vector3.forward;
        forward.y = 0f;
        forward.Normalize();

        Quaternion targetRotation;
        
        // // If the local movement direction is the opposite of forward then the target rotation should be towards the camera.
        // if (Mathf.Approximately(Vector3.Dot(localMovementDirection, Vector3.forward), -1.0f))
        // {
        //     targetRotation = Quaternion.LookRotation(-forward);
        // }
        // else
        // {
        //     // Otherwise the rotation should be the offset of the input from the camera's forward.
        //     Quaternion cameraToInputOffset = Quaternion.FromToRotation(Vector3.forward, localMovementDirection);
        //     targetRotation = Quaternion.LookRotation(cameraToInputOffset * forward);
        // }
        // targetRotation = Quaternion.LookRotation(-forward);
        Quaternion cameraToInputOffset = Quaternion.FromToRotation(Vector3.forward, localMovementDirection);
        targetRotation = Quaternion.LookRotation(cameraToInputOffset * forward);


        // The desired forward direction.
        Vector3 resultingForward = targetRotation * Vector3.forward;

        // Find the difference between the current rotation of the player and the desired rotation of the player in radians.
        float angleCurrent = Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg;
        float targetAngle = Mathf.Atan2(resultingForward.x, resultingForward.z) * Mathf.Rad2Deg;

        _angleDiff = Mathf.DeltaAngle(angleCurrent, targetAngle);
        _targetRotation = targetRotation;
    }    
    void UpdateOrientation(float deltaTime)
    {
        _anim.SetFloat("angleDeltaRad", _angleDiff * Mathf.Deg2Rad);

        Vector3 localInput = new Vector3(_inputController.MovementVector.x, 0f, _inputController.MovementVector.y);
        float groundedTurnSpeed = Mathf.Lerp(MaxTurnVelocity, MinTurnVelocity, _forwardVelocity / _desiredForwardSpeed);
        float actualTurnSpeed = _isGrounded ? groundedTurnSpeed : Vector3.Angle(transform.forward, localInput) * kInverseOneEighty * kAirborneTurnSpeedProportion * groundedTurnSpeed;
        _targetRotation = Quaternion.RotateTowards(transform.rotation, _targetRotation, actualTurnSpeed * deltaTime);
        bool hasNan = float.IsNaN(_targetRotation.x) || float.IsNaN(_targetRotation.y) ||float.IsNaN(_targetRotation.z);
        if (!hasNan)
            transform.rotation = _targetRotation;
    }

    void CalculateForwardMovement(float deltaTime)
    {
        // Cache the move input and cap it's magnitude at 1.
        Vector2 moveInput = _inputController.MovementVector;
        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        // Calculate the speed intended by input.
        _desiredForwardSpeed = moveInput.magnitude * MaxForwardVelocity;

        // Note: acceleration is handle in InputController
        _forwardVelocity = _desiredForwardSpeed;

        // Set the animator parameter to control what animation is being played.
        _anim.SetFloat("forwardVelocity", _forwardVelocity);
    }
    void CalculateVerticalMovement(float deltaTime)
    {
        // If jump is not currently held and is on the ground then ready to jump.
        if (!_inputController.Jump && _isGrounded)
            _readyToJump = true;

        _anim.SetBool("backflip", _inputController.Backflip);

        if (_isGrounded)
        {
            // When grounded we apply a slight negative vertical speed to make Ellen "stick" to the ground.
            _verticalVelocity = Physics.gravity.y * kStickingGravityProportion;

            // If jump is held, Ellen is ready to jump and not currently in the middle of a melee combo...
            if (_inputController.Jump && _readyToJump && !_inCombo)
            {
                // ... then override the previously set vertical speed and make sure she cannot jump again.
                _verticalVelocity = JumpSpeed;
                _isGrounded = false;
                _readyToJump = false;
                _anim.SetBool("onGround", false);
            }
        }
        else 
        {
            // If Ellen is airborne, the jump button is not held and Ellen is currently moving upwards...
            if (!_inputController.Jump && _verticalVelocity > 0.0f)
            {
                // ... decrease Ellen's vertical speed.
                // This is what causes holding jump to jump higher that tapping jump.
                _verticalVelocity -= kJumpAbortSpeed * deltaTime;
            }

            // If a jump is approximately peaking, make it absolute.
            if (Mathf.Approximately(_verticalVelocity, 0f))
            {
                _verticalVelocity = 0f;
            }
            
            // If Ellen is airborne, apply gravity.
            _verticalVelocity += Physics.gravity.y * deltaTime;
        }
    }
}