using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelEditor : MonoBehaviour {
    private static string[] fillTypeNames = { "Empty", "Stone", "Dirt", "Rock", "Grass" };
    private static string[] radiusNames = { "0", "1", "2", "3", "4", "5" };
    private static string[] stencilNames = { "Square", "Circle" };

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

    private VoxelStencil[] stencils = {
        new VoxelStencil(),
        new VoxelStencilCircle()
    };

    public void Startup(int voxelResolution, int chunkResolution, float viewDistance, Dictionary<Vector2Int, VoxelChunk> existingChunks, List<VoxelChunk> chunks, VoxelMap voxelMap) {
        this.voxelResolution = voxelResolution;
        this.chunkResolution = chunkResolution;
        this.viewDistance = viewDistance;
        this.existingChunks = existingChunks;
        this.chunks = chunks;
        this.voxelMap = voxelMap;
        halfSize = 0.5f * chunkResolution;
        voxelSize = 1f / voxelResolution;

        voxelMesh = FindObjectOfType<VoxelMesh>();
        player = FindObjectOfType<PlayerController>().transform;

        box = gameObject.GetComponent<BoxCollider>();
        if (box != null) {
            DestroyImmediate(box);

        }
        box = gameObject.AddComponent<BoxCollider>();
        box.center = Vector3.one * (voxelResolution / 2);
        box.size = new Vector3((chunkResolution - viewDistance) * voxelResolution, (chunkResolution - viewDistance) * voxelResolution);
    }

    private void Update() {
        if (Input.GetMouseButton(0)) {
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo)) {
                if (hitInfo.collider.gameObject == gameObject && (oldPoint != hitInfo.point || oldTypeIndex != fillTypeIndex)) {
                    EditVoxels(hitInfo.point);
                    oldPoint = hitInfo.point;
                    oldTypeIndex = fillTypeIndex;
                }
            }
        }
    }

    private void EditVoxels(Vector3 point) {
        chunkPos = new Vector3(Mathf.Floor(point.x / voxelResolution), Mathf.Floor(point.y / voxelResolution));
        Vector2Int checkPos = new Vector2Int((int)chunkPos.x, (int)chunkPos.y);
        diff = new Vector2Int((int)Mathf.Abs(point.x - (chunkPos * voxelResolution).x), (int)Mathf.Abs(point.y - (chunkPos * voxelResolution).y));

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

        if (existingChunks.ContainsKey(checkPos)) {
            currentChunk = existingChunks[checkPos];
            currentChunk.shouldUpdateCollider = true;
        }

        int voxelYOffset = yEnd * voxelResolution;
        for (int y = yEnd; y >= yStart - 1; y--) {
            int voxelXOffset = xEnd * voxelResolution;
            for (int x = xEnd; x >= xStart - 1; x--) {
                absChunkPos = new Vector3(Mathf.Floor((point.x + voxelXOffset) / voxelResolution), Mathf.Floor((point.y + voxelYOffset) / voxelResolution));
                absCheckPos = new Vector2Int((int)absChunkPos.x, (int)absChunkPos.y);
                activeStencil.SetCenter(diff.x - voxelXOffset, diff.y - voxelYOffset);

                EditChunkAndNeighbors(activeStencil, absCheckPos);

                voxelXOffset -= voxelResolution;
            }
            voxelYOffset -= voxelResolution;
        }
    }

    private void EditChunkAndNeighbors(VoxelStencil activeStencil, Vector2Int checkPos) {
        bool result = false;

        if (existingChunks.ContainsKey(checkPos)) {
            currentChunk = existingChunks[checkPos];
            result = currentChunk.Apply(activeStencil);
        }

        for (int x = -1; x < 1; x++) {
            for (int y = -1; y < 1; y++) {
                currentPos = new Vector2Int(checkPos.x + x, checkPos.y + y);

                if (existingChunks.ContainsKey(currentPos)) {
                    currentChunk = existingChunks[currentPos];

                    if (result) {
                        voxelMesh.TriangulateChunkMesh(currentChunk);
                        Debug.Log(1);
                    }
                }
            }
        }
    }

    private void OnGUI() {
        GUILayout.BeginArea(new Rect(4f, 4f, 150f, 1000f));
        GUILayout.Label("Fill Type");
        fillTypeIndex = GUILayout.SelectionGrid(fillTypeIndex, fillTypeNames, 2);
        GUILayout.Label("Radius");
        radiusIndex = GUILayout.SelectionGrid(radiusIndex, radiusNames, 6);
        GUILayout.Label("Stencil");
        stencilIndex = GUILayout.SelectionGrid(stencilIndex, stencilNames, 2);
        GUILayout.Label("Regenerate");
        if (GUI.Button(new Rect(0, 225, 150f, 20f), "Generate")) {
            voxelMap.FreshGeneration();
        }
        GUILayout.EndArea();
    }
}
