using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelEditor : MonoBehaviour {
    private const int UPDATE_INTERVAL = 1;

    private static readonly string[] FillTypeNames = { "Empty", "Stone", "Dirt", "Rock", "Grass" };
    private static readonly string[] RadiusNames = { "0", "1", "2", "3", "4", "5" };
    private static readonly string[] StencilNames = { "Square", "Circle" };

    private int voxelResolution, chunkResolution;
    private float viewDistance, halfSize, voxelSize;

    private int fillTypeIndex, radiusIndex, stencilIndex;

    private VoxelMesh voxelMesh;
    private List<VoxelChunk> chunks;
    private Dictionary<Vector2Int, VoxelChunk> existingChunks;
    private VoxelMap voxelMap;
    private Transform player;
    private BoxCollider box;

    private Vector3 oldPoint, chunkPos, absChunkPos;
    private Vector2Int diff, absCheckPos, currentPos;
    private int oldTypeIndex, xStart, xEnd, yStart, yEnd;
    private RaycastHit hitInfo;
    private VoxelStencil activeStencil;
    private VoxelChunk currentChunk;

    private readonly VoxelStencil[] stencils = {
        new VoxelStencil(),
        new VoxelStencilCircle()
    };

    public void Startup(VoxelMap map) {
        voxelResolution = map.voxelResolution;
        chunkResolution = map.chunkResolution;
        viewDistance = map.viewDistance;
        existingChunks = map.existingChunks;
        chunks = map.chunks;
        voxelMap = map;
        halfSize = 0.5f * chunkResolution;
        voxelSize = 1f / voxelResolution;

        voxelMesh = FindObjectOfType<VoxelMesh>();
        player = FindObjectOfType<PlayerController>().transform;

        box = gameObject.GetComponent<BoxCollider>();
        if (box != null) {
            DestroyImmediate(box);
        }

        box = gameObject.AddComponent<BoxCollider>();
        box.center = Vector3.one * (voxelResolution / 2f);
        box.size = new Vector3((chunkResolution - viewDistance) * voxelResolution,
            (chunkResolution - viewDistance) * voxelResolution);
    }

    private void Update() {
        if (Time.frameCount % UPDATE_INTERVAL != 0) return;
        if (Input.GetMouseButton(0)) {
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo)) {
                if (hitInfo.collider.gameObject == gameObject &&
                    (oldPoint != hitInfo.point || oldTypeIndex != fillTypeIndex)) {
                    EditVoxels(hitInfo.point);
                    oldPoint = hitInfo.point;
                    oldTypeIndex = fillTypeIndex;
                }
            }
        }
    }

    private void EditVoxels(Vector3 point) {
        chunkPos = new Vector3(Mathf.Floor(point.x / voxelResolution), Mathf.Floor(point.y / voxelResolution));
        var checkPos = new Vector2Int((int)chunkPos.x, (int)chunkPos.y);
        diff = new Vector2Int((int)Mathf.Abs(point.x - (chunkPos * voxelResolution).x),
            (int)Mathf.Abs(point.y - (chunkPos * voxelResolution).y));

        xStart = (diff.x - radiusIndex - 1) / voxelResolution;
        if (xStart <= -chunkResolution) {
            xStart = -chunkResolution + 1;
        }

        xEnd = (diff.x + radiusIndex) / voxelResolution;
        if (xEnd >= chunkResolution) {
            xEnd = chunkResolution - 1;
        }

        yStart = (diff.y - radiusIndex - 1) / voxelResolution;
        if (yStart <= -chunkResolution) {
            yStart = -chunkResolution + 1;
        }

        yEnd = (diff.y + radiusIndex) / voxelResolution;
        if (yEnd >= chunkResolution) {
            yEnd = chunkResolution - 1;
        }

        activeStencil = stencils[stencilIndex];
        activeStencil.Initialize(fillTypeIndex, radiusIndex);

        int voxelYOffset = yEnd * voxelResolution;
        for (int y = yStart - 1; y < yEnd; y++) {
            int voxelXOffset = xEnd * voxelResolution;
            for (int x = xStart - 1; x < xEnd; x++) {
                absChunkPos = new Vector3(Mathf.Floor((point.x + voxelXOffset) / voxelResolution),
                    Mathf.Floor((point.y + voxelYOffset) / voxelResolution));
                absCheckPos = new Vector2Int((int)absChunkPos.x, (int)absChunkPos.y);
                activeStencil.SetCenter(diff.x - voxelXOffset, diff.y - voxelYOffset);

                EditChunkAndNeighbors(absCheckPos, new Vector3(Mathf.Floor(point.x + voxelXOffset), Mathf.Floor(point.y + voxelYOffset)));

                voxelXOffset -= voxelResolution;
            }

            voxelYOffset -= voxelResolution;
        }
    }

    // TODO: Keep refactoring here
    // try to fix bigger radius brush size

    // IDEAS: 
    // refactor out applied chunk so its applied in every updated chunk
    private void EditChunkAndNeighbors(Vector2Int checkPos, Vector2 pos) {
        bool mainRes = false;

        if (existingChunks.ContainsKey(checkPos)) {
            currentChunk = existingChunks[checkPos];
            mainRes = currentChunk.Apply(activeStencil);
        }

        for (int x = -1; x < 1; x++) {
            for (int y = -1; y < 1; y++) {
                bool result = false;
                currentPos = new Vector2Int(checkPos.x + x, checkPos.y + y);

                if (existingChunks.ContainsKey(currentPos)) {
                    currentChunk = existingChunks[currentPos];

                    if (x == -1 && y == -1 && (Mathf.Abs(pos.x) == 8 || pos.x == 0) && (Mathf.Abs(pos.y) == 8 || pos.y == 0)) {
                        result = true;
                    }
                    else if (x == -1 && y == 0 && (Mathf.Abs(pos.x) == 8 || pos.x == 0)) {
                        result = true;
                    }
                    else if (x == 0 && y == -1 && (Mathf.Abs(pos.y) == 8 || pos.y == 0)) {
                        result = true;
                    }

                    if (result && (x != 0 || y != 0)) {
                        voxelMesh.TriangulateChunkMesh(currentChunk);
                    }
                }
            }
        }

        if (mainRes) {
            voxelMesh.TriangulateChunkMesh(currentChunk);
        }
    }

    private void OnGUI() {
        GUILayout.BeginArea(new Rect(4f, 4f, 150f, 1000f));
        GUILayout.Label("Fill Type");
        fillTypeIndex = GUILayout.SelectionGrid(fillTypeIndex, FillTypeNames, 2);
        GUILayout.Label("Radius");
        radiusIndex = GUILayout.SelectionGrid(radiusIndex, RadiusNames, 6);
        GUILayout.Label("Stencil");
        stencilIndex = GUILayout.SelectionGrid(stencilIndex, StencilNames, 2);
        GUILayout.Label("Regenerate");
        if (GUI.Button(new Rect(0, 225, 150f, 20f), "Generate")) {
            voxelMap.FreshGeneration();
        }
        GUILayout.EndArea();
    }
}
