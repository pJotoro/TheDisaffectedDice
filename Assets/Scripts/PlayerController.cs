using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * Issues I'm noticing so far:
 * - The camera doesn't move with the player.
 * - There's no way to tell which side of the player you are looking at. This is obviously because we haven't applied any textures to the player yet.
 */

public class PlayerController : MonoBehaviour
{
    private InputAction moveAction;

    public float inputThreshold = 0.5f;
    public float rotationSpeed = 90.0f; // measured in degrees per second

    private Vector3 lastPos;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        state = State.Free;
        lastPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        bool alreadyRotating = amountRotated > 0.0f;
        bool horizontalInput = Mathf.Abs(moveValue.x) >= inputThreshold;
        bool verticalInput = Mathf.Abs(moveValue.y) >= inputThreshold;

        // If the player is attempting to move in two directions, then we don't move.
        if (!alreadyRotating && (!horizontalInput || !verticalInput))
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
                lastPos = transform.position;
            }
        }

        switch (state)
        {
            case State.Free:
                break;
            case State.Left:
                Move(transform.position - 0.5f * transform.right - 0.5f * transform.up, transform.forward, rotationSpeed * Time.deltaTime, -Vector3.right);
                break;
            case State.Right:
                Move(transform.position + 0.5f * transform.right - 0.5f * transform.up, transform.forward, -rotationSpeed * Time.deltaTime, Vector3.right);
                break;
            case State.Up:
                Move(transform.position + 0.5f * transform.forward - 0.5f * transform.up, transform.right, rotationSpeed * Time.deltaTime, Vector3.forward);
                break;
            case State.Down:
                Move(transform.position - 0.5f * transform.forward - 0.5f * transform.up, transform.right, -rotationSpeed * Time.deltaTime, -Vector3.forward);
                break;
        }
    }
    private const float FINAL_ROTATION = 90.0f;

    void Move(Vector3 point, Vector3 axis, float angle, Vector3 translation)
    {
        transform.RotateAround(point, axis, angle);
        amountRotated += rotationSpeed * Time.deltaTime;
        if (amountRotated >= FINAL_ROTATION)
        {
            transform.localPosition = lastPos + translation;
            transform.localRotation = Quaternion.identity;

            amountRotated = 0.0f;
            state = State.Free;
        }
    }
}
