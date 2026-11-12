using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private InputAction moveAction;

    public float inputThreshold = 0.5f;
    public float rotationSpeed; // degrees per second

    private Vector3 startPos; // local space
    private int2 gridPos;
    private Quaternion lastRotation;

    private GameObject cameraObject;
    private Vector3 cameraStartPos; // world space
    private Vector3 cameraMovementLeftover; // world space

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

    const float GRID_SIZE = 1.4f;

    int2 GridPosFromLocalPos(Vector3 localPos)
    {
        int2 res;
        res.x = (int)((localPos.x - startPos.x) / GRID_SIZE);
        res.y = (int)((localPos.y - startPos.y) / GRID_SIZE);
        return res;
    }

    Vector3 LocalPosFromGridPos(int2 gridPos)
    {
        Vector3 res;
        res.x = (float)gridPos.x * GRID_SIZE + startPos.x;
        res.y = startPos.y;
        res.z = (float)gridPos.y * GRID_SIZE + startPos.y;
        return res;
    }

    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        state = State.Free;
        startPos = transform.localPosition;
        gridPos = GridPosFromLocalPos(startPos);
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
            }
        }

        switch (state)
        {
            case State.Free:
                break;
            case State.Left:
                Move(transform.position - GRID_SIZE/2.0f * Vector3.right - GRID_SIZE/2.0f * Vector3.up, Vector3.forward, rotationSpeed * Time.deltaTime, new int2(-1, 0));
                break;
            case State.Right:
                Move(transform.position + GRID_SIZE/2.0f * Vector3.right - GRID_SIZE/2.0f * Vector3.up, Vector3.forward, -rotationSpeed * Time.deltaTime, new int2(1, 0));
                break;
            case State.Up:
                Move(transform.position + GRID_SIZE/2.0f * Vector3.forward - GRID_SIZE/2.0f * Vector3.up, Vector3.right, rotationSpeed * Time.deltaTime, new int2(0, 1));
                break;
            case State.Down:
                Move(transform.position - GRID_SIZE/2.0f * Vector3.forward - GRID_SIZE/2.0f * Vector3.up, Vector3.right, -rotationSpeed * Time.deltaTime, new int2(0, -1));
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

    void Move(Vector3 point, Vector3 axis, float angle, int2 translation)
    {
        transform.RotateAround(point, axis, angle);
        amountRotated += Mathf.Abs(angle);
        if (amountRotated >= 90.0f)
        {
            transform.rotation = lastRotation;
            transform.RotateAround(point, axis, 90.0f*Mathf.Sign(angle));

            gridPos += translation;
            transform.localPosition = LocalPosFromGridPos(gridPos);

            amountRotated = 0.0f;
            state = State.Free;
        }
    }
}
