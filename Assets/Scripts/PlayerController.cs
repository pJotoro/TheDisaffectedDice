using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private InputAction moveAction;

    public float inputThreshold = 0.5f;
    public float rotationSpeed; // measured in degrees per second

    private Vector3 lastPos;
    private Quaternion lastRotation;

    private GameObject cameraObject;
    private Vector3 cameraStartPos;
    private Vector3 cameraMovementLeftover;

    enum State
    {
        Free,
        Left,
        Right,
        Up,
        Down,
    }
    private State state;
    private float amountRotated = 0.0f;

    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        state = State.Free;
        lastPos = transform.position;
        cameraObject = GameObject.Find("Camera");
        Debug.Assert(cameraObject != null);
        cameraStartPos = cameraObject.transform.position;
    }

    void Update()
    {
        Vector3 posBeforeMovement = transform.position;

        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        bool alreadyRotating = amountRotated > 0.0f;
        bool horizontalInput = Mathf.Abs(moveValue.x) >= inputThreshold;
        bool verticalInput = Mathf.Abs(moveValue.y) >= inputThreshold;
        bool attemptingToMoveInTwoDirections = horizontalInput && verticalInput;
        if (!alreadyRotating && !attemptingToMoveInTwoDirections)
        {
            Debug.Assert(state == State.Free);
            
            if (moveValue.x >= inputThreshold)
            {
                state = State.Right;
            }
            else if (moveValue.x <= -inputThreshold)
            {
                state = State.Left;
            }
            else if (moveValue.y >= inputThreshold)
            {
                state = State.Up;
            }
            else if (moveValue.y <= -inputThreshold)
            {
                state = State.Down;
            }

            if (state != State.Free)
            {
                lastRotation = transform.rotation;
                lastPos = transform.position;
            }
        }

        switch (state)
        {
            case State.Free:
                break;
            case State.Left:
                Move(transform.position - 0.7f * Vector3.right - 0.7f * Vector3.up, Vector3.forward, rotationSpeed * Time.deltaTime, -Vector3.right*1.4f);
                break;
            case State.Right:
                Move(transform.position + 0.7f * Vector3.right - 0.7f * Vector3.up, Vector3.forward, -rotationSpeed * Time.deltaTime, Vector3.right * 1.4f);
                break;
            case State.Up:
                Move(transform.position + 0.7f * Vector3.forward - 0.7f * Vector3.up, Vector3.right, rotationSpeed * Time.deltaTime, Vector3.forward * 1.4f);
                break;
            case State.Down:
                Move(transform.position - 0.7f * Vector3.forward - 0.7f * Vector3.up, Vector3.right, -rotationSpeed * Time.deltaTime, -Vector3.forward * 1.4f);
                break;
        }

        // We only move the camera by how much the player moved.
        Vector3 posDiff = transform.position - posBeforeMovement;
        posDiff.y = 0.0f; // We don't want the camera to move vertically.
        cameraMovementLeftover += posDiff;
        Vector3 cameraMovement = cameraMovementLeftover * Time.deltaTime;
        cameraObject.transform.position = cameraObject.transform.position + cameraMovement; // For some reason, Translate screws up the y-position anyway.
        cameraMovementLeftover -= cameraMovement;

    }

    void Move(Vector3 point, Vector3 axis, float angle, Vector3 translation)
    {
        transform.RotateAround(point, axis, angle);
        amountRotated += Mathf.Abs(angle);
        if (amountRotated >= 90.0f)
        {
            transform.rotation = lastRotation;
            transform.RotateAround(point, axis, 90.0f*Mathf.Sign(angle));
            transform.position = lastPos + translation;

            amountRotated = 0.0f;
            state = State.Free;
        }
    }
}
