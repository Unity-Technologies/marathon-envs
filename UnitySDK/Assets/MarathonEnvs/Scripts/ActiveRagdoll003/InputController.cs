using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class InputController : MonoBehaviour
{
    [Header("Options")]
    public float MaxVelocity;
    [Range(0f,1f)]
    public float ClipInput;
    public bool NoJumpsInMockMode;

    [Header("User or Mock input states")]
    public Vector2 TargetMovementVector; // User-input desired horizontal center of mass velocity.
    public Vector2 MovementVector; // smoothed version of TargetMovementVector.
    public Vector2 CameraRotation; // User-input desired rotation for camera.
    public bool Jump; // User wants to jump
    public bool Backflip; // User wants to backflip

    [Header("Read only (or debug)")]
    public Vector2 DesiredHorizontalVelocity; // MovementVector * Max Velovity
    public Vector3 HorizontalDirection; // Normalized vector in direction of travel (assume right angle to floor)
    public bool UseHumanInput;
    public bool DemoMockIfNoInput = true; // Demo mock mode if no human input

    float _delayUntilNextAction;
    float _timeUnillDemo;

    const float kGroundAcceleration = .6f;
    const float kGroundDeceleration = .75f;


    // Start is called before the first frame update
    void Awake()
    {
        UseHumanInput = !Academy.Instance.IsCommunicatorOn;
        _timeUnillDemo = 1f;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void FixedUpdate()
    {
        DoUpdate(Time.fixedDeltaTime);
    }
    void DoUpdate(float deltaTime)
    {
        if (UseHumanInput)
            GetHumanInput();
        else
            GetMockInput();
        SmoothAcceleration(deltaTime);
    }
    public void OnReset()
    {
        SetRandomHorizontalDirection();
        _delayUntilNextAction = -1f;
        DoUpdate(Time.fixedDeltaTime);
    }
    void SmoothAcceleration(float deltaTime)
    {
        // Determine change to speed based on whether there is currently any move input.
        float acceleration = TargetMovementVector.magnitude > 0 ? kGroundAcceleration : kGroundDeceleration;

        var difference = (MovementVector - TargetMovementVector);
        if (difference.magnitude > MovementVector.magnitude)
        {
            acceleration *= 5f;
        }

        // Adjust the forward speed towards the desired speed.
        MovementVector = Vector2.MoveTowards(MovementVector, TargetMovementVector, acceleration * deltaTime);

        // Handle deadzone
        if (MovementVector.magnitude < .1f)
        {
            if (TargetMovementVector.magnitude < .1f)
            {
                TargetMovementVector = Vector2.zero;
                MovementVector = Vector2.zero;
            }
            else
            {
                MovementVector = TargetMovementVector.normalized * .1f;
            }
        }

        // handle direction
        if (!Mathf.Approximately(MovementVector.sqrMagnitude, 0f))
            HorizontalDirection = new Vector3(MovementVector.normalized.x, 0f, MovementVector.normalized.y);
        DesiredHorizontalVelocity = MovementVector.normalized * MaxVelocity * MovementVector.magnitude;
    }
    void GetHumanInput()
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            return;
        bool resetTimeUntilDemo = false;
        _timeUnillDemo -= Time.deltaTime;
        var newMovementVector = new Vector2(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical")
        );
        if (ClipInput > 0f)
        {
            newMovementVector = new Vector2(
                Mathf.Clamp(newMovementVector.x, -ClipInput, ClipInput),
                Mathf.Clamp(newMovementVector.y, -ClipInput, ClipInput));
        }
        if (!Mathf.Approximately(newMovementVector.sqrMagnitude, 0f))
        {
            TargetMovementVector = newMovementVector;
            resetTimeUntilDemo = true;
        }
        else if (DemoMockIfNoInput && _timeUnillDemo <= 0)
        {
            GetMockInput();
            _timeUnillDemo = 0f;
            return;
        }
        CameraRotation = Vector2.zero;
        Jump = Input.GetKey(KeyCode.Space); //Input.GetButtonDown("Fire1");
        Backflip = Input.GetKey(KeyCode.B);
        if (Jump || Backflip)
        {
            resetTimeUntilDemo = true;
        }
        if (resetTimeUntilDemo)
        {
            _timeUnillDemo = 3f;
        }
    }
    void GetMockInput()
    {
        _delayUntilNextAction -= Time.deltaTime;
        if (_delayUntilNextAction > 0)
            return;
        if (ChooseBackflip())
        {
            Backflip = true;
            _delayUntilNextAction = 2f;
            return;
        }
        Backflip = false;
        Jump = false;
        float direction = UnityEngine.Random.Range(0f, 360f);
        float power = UnityEngine.Random.value;
        // float direction = UnityEngine.Random.Range(-Mathf.PI/8, Mathf.PI/8);
        // float power = UnityEngine.Random.Range(1f, 1f);
        if (ClipInput > 0f)
        {
            power *= ClipInput;
        }
        TargetMovementVector = new Vector2(Mathf.Cos(direction), Mathf.Sin(direction));
        TargetMovementVector *= power;
        Jump = ChooseJump();
        _delayUntilNextAction = 1f + (UnityEngine.Random.value * 5f);
    }
    bool ChooseBackflip()
    {
        if (NoJumpsInMockMode)
            return false;
        var rnd = UnityEngine.Random.Range(0, 10);
        return rnd == 0;
    }
    bool ChooseJump()
    {
        if (NoJumpsInMockMode)
            return false;
        var rnd = UnityEngine.Random.Range(0, 5);
        return rnd == 0;
    }
    void SetRandomHorizontalDirection()
    {
        float direction = UnityEngine.Random.Range(0f, 360f);
        var movementVector = new Vector2(Mathf.Cos(direction), Mathf.Sin(direction));
        HorizontalDirection = new Vector3(movementVector.normalized.x, 0f, movementVector.normalized.y);
        movementVector /= float.MinValue;
        TargetMovementVector = new Vector2(movementVector.x, movementVector.y);
    }

}
