using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

using TextMeshPro = TMPro.TextMeshPro;
using TileBase = UnityEngine.Tilemaps.TileBase;
using UnityEngine.Rendering.Universal;
using System;
using TMPro.EditorUtilities;
using UnityEditor.Tilemaps;

// Assume unless stated otherwise that:
// - Positions are in local space.
// - Rotations are in degrees per second.

public class PlayerController : MonoBehaviour
{
    private InputAction moveAction;

    public float inputThreshold = 0.5f;
    public float rotationSpeed;

    private Vector3 startPos;
    private Vector3Int gridPos; // in grid space obviously

    private Vector3 lastPos;
    private Quaternion lastRotation;


    private Camera theCamera;
    private Vector3 cameraStartPos;
    private Vector3 cameraMovementLeftover;

    private Tilemap tilemap;

    const float GRID_SIZE = 1.4f; // TODO: Should we just get this from the grid?

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

    struct Tile
    {
        public int val; // -1 means no tile, 0 means blank tile
    }
    class Tilemap
    {
        private UnityEngine.Tilemaps.Tilemap unityTilemap;
        private Tile[][] tiles;
        public readonly Vector3Int minPos;
        public readonly Vector3Int maxPos;
        public Tilemap()
        {
            unityTilemap = GameObject.FindAnyObjectByType<UnityEngine.Tilemaps.Tilemap>();
            Debug.Assert(unityTilemap != null);

            foreach (Transform t in unityTilemap.transform)
            {
                Vector3Int cellPos = unityTilemap.LocalToCell(t.localPosition);
                this.minPos = Vector3Int.Min(this.minPos, cellPos);
                this.maxPos = Vector3Int.Max(this.maxPos, cellPos);
            }
            this.tiles = new Tile[this.maxPos.y - this.minPos.y + 1][];
            for (int row = 0; row < this.maxPos.y - this.minPos.y + 1; row++)
            {
                tiles[row] = new Tile[this.maxPos.x - this.minPos.x + 1];
                for (int col = 0; col < this.maxPos.x - this.minPos.x + 1; col++)
                {
                    tiles[row][col] = new Tile();
                    tiles[row][col].val = -1;
                }
            }

            foreach (Transform t in unityTilemap.transform)
            {
                Vector3Int cellPos = unityTilemap.LocalToCell(t.localPosition);

                string text = t.transform.GetChild(0).GetComponent<TextMeshPro>().text;
                int c = text[0];
                int n = c - '0';
                this[cellPos.y, cellPos.x].val = n;
            }
        }
        public ref Tile this[int row, int col]
        {
            get { return ref tiles[row - minPos.y][col - minPos.x]; }
        }

        public Vector3Int LocalToCell(Vector3 localPos)
        {
            return unityTilemap.LocalToCell(localPos);
        }

        public Vector3 CellToLocal(Vector3Int cellPos)
        {
            return unityTilemap.CellToLocal(cellPos);
        }
        public bool TileExists(int row, int col)
        {
            if (row >= minPos.y && row <= maxPos.y && col >= minPos.x && col <= maxPos.x)
            {
                Tile tile = this[row, col];
                return tile.val != -1;
            }
            return false;
        }
    }

    void Start()
    {
        theCamera = GameObject.FindAnyObjectByType<Camera>();
        Debug.Assert(theCamera != null);
        cameraStartPos = theCamera.transform.localPosition;
        
        tilemap = new Tilemap();

        moveAction = InputSystem.actions.FindAction("Move");
        state = State.Free;
        startPos = transform.localPosition;
        gridPos = tilemap.LocalToCell(transform.localPosition);

    }

    void Update()
    {
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
                lastRotation = transform.localRotation;
                lastPos = transform.localPosition;
            }
        }

        Vector3 posBeforeMovement = transform.localPosition;

        switch (state)
        {
            case State.Free:
                break;
            case State.Left:
                Move(lastPos - GRID_SIZE/2.0f * Vector3.right - GRID_SIZE/2.0f * Vector3.up, Vector3.forward, rotationSpeed * Time.deltaTime, -1, 0);
                break;
            case State.Right:
                Move(lastPos + GRID_SIZE/2.0f * Vector3.right - GRID_SIZE/2.0f * Vector3.up, Vector3.forward, -rotationSpeed * Time.deltaTime, 1, 0);
                break;
            case State.Up:
                Move(lastPos + GRID_SIZE/2.0f * Vector3.forward - GRID_SIZE/2.0f * Vector3.up, Vector3.right, rotationSpeed * Time.deltaTime, 0, 1);
                break;
            case State.Down:
                Move(lastPos - GRID_SIZE/2.0f * Vector3.forward - GRID_SIZE/2.0f * Vector3.up, Vector3.right, -rotationSpeed * Time.deltaTime, 0, -1);
                break;
        }

        // We only move the camera by how much the player moved.
        Vector3 posDiff = transform.localPosition - posBeforeMovement;
        posDiff.y = 0.0f; // We don't want the camera to move vertically.
        cameraMovementLeftover += posDiff;
        Vector3 cameraMovement = cameraMovementLeftover * Time.deltaTime;
        theCamera.transform.localPosition = theCamera.transform.localPosition + cameraMovement; // For some reason, Translate screws up the y-position anyway.
        cameraMovementLeftover -= cameraMovement;
    }

    void Move(Vector3 point, Vector3 axis, float angle, int tx, int ty)
    {
        transform.RotateAround(point, axis, angle);
        amountRotated += Mathf.Abs(angle);

        // land on new tile
        if (amountRotated >= 90.0f)
        {
            state = State.Free;

            // fix rotation to a multiple of 90 degrees
            transform.localRotation = lastRotation;
            transform.RotateAround(point, axis, 90.0f*Mathf.Sign(angle));
            amountRotated = 0.0f;

            // fix position to grid
            gridPos.x += tx;
            gridPos.y += ty;
            transform.localPosition = tilemap.CellToLocal(gridPos) + startPos;

            if (!tilemap.TileExists(gridPos.y, gridPos.x))
            {
                transform.localPosition = startPos;
                gridPos = tilemap.LocalToCell(transform.localPosition);
            }

        }
    }
}
