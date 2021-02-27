using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelEditor : MonoBehaviour {
    private static string[] fillTypeNames = { "Empty", "Stone", "Dirt", "Rock", "Grass" };
    private static string[] radiusNames = { "0", "1", "2", "3", "4", "5" };
    private static string[] stencilNames = { "Square", "Circle" };

    private int voxelResolution, chunkResolution;
    private float viewDistance;
    private float halfSize, voxelSize;

    private int fillTypeIndex, radiusIndex, stencilIndex;

    private VoxelMesh voxelMesh;
    private List<VoxelChunk> chunks;
    private Dictionary<Vector2Int, VoxelChunk> existingChunks;
    private VoxelMap voxelMap;
    private Transform player;

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

        BoxCollider box = gameObject.GetComponent<BoxCollider>();
        if (box != null) {
            DestroyImmediate(box);

        }
        box = gameObject.AddComponent<BoxCollider>();
        box.center = Vector3.one * (voxelResolution / 2);
        box.size = new Vector3((chunkResolution - viewDistance) * voxelResolution, (chunkResolution - viewDistance) * voxelResolution);
    }

    private void Update() {
        if (Input.GetMouseButton(0)) {
            RaycastHit hitInfo;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo)) {
                if (hitInfo.collider.gameObject == gameObject) {
                    EditVoxels(hitInfo.point);
                }
            }
        }
    }

    private void EditVoxels(Vector3 point) {
        Vector3 p = player.position / voxelResolution;
        Vector3 clickPos = point;
        Vector3 chunkPos = new Vector3(Mathf.Floor(clickPos.x / voxelResolution), Mathf.Floor(clickPos.y / voxelResolution));

        Vector2Int checkPos = new Vector2Int((int)chunkPos.x, (int)chunkPos.y);
        Vector3 chunkVectorPos = chunkPos * voxelResolution;
        Vector3 diff = new Vector3(Mathf.Abs(clickPos.x - chunkVectorPos.x), Mathf.Abs(clickPos.y - chunkVectorPos.y));

        int centerX = (int)(diff.x);
        int centerY = (int)(diff.y);

        int xStart = (centerX - radiusIndex - 1) / voxelResolution;
        if (xStart <= -chunkResolution) {
            xStart = -chunkResolution + 1;
        }
        int xEnd = (centerX + radiusIndex) / voxelResolution;
        if (xEnd >= chunkResolution) {
            xEnd = chunkResolution - 1;
        }
        int yStart = (centerY - radiusIndex - 1) / voxelResolution;
        if (yStart <= -chunkResolution) {
            yStart = -chunkResolution + 1;
        }
        int yEnd = (centerY + radiusIndex) / voxelResolution;
        if (yEnd >= chunkResolution) {
            yEnd = chunkResolution - 1;
        }

        VoxelStencil activeStencil = stencils[stencilIndex];
        activeStencil.Initialize(fillTypeIndex, radiusIndex);

        int voxelYOffset = yEnd * voxelResolution;
        for (int y = yEnd; y >= yStart; y--) {
            int voxelXOffset = xEnd * voxelResolution;
            for (int x = xEnd; x >= xStart; x--) {
                activeStencil.SetCenter(centerX - voxelXOffset, centerY - voxelYOffset);

                // if (existingChunks.ContainsKey(checkPos)) {
                //     VoxelChunk chunk = existingChunks[checkPos];
                //     chunk.Apply(activeStencil);

                //     voxelMesh.TriangulateChunkMesh(chunk);
                // }

                EditChunkAndNeighbors(activeStencil, checkPos);

                voxelXOffset -= voxelResolution;
            }
            voxelYOffset -= voxelResolution;
        }
    }

    private void EditChunkAndNeighbors(VoxelStencil activeStencil, Vector2Int checkPos) {
        for (int x = -1; x < 1; x++) {
            for (int y = -1; y < 1; y++) {
                Vector2Int current = new Vector2Int(checkPos.x + x, checkPos.y + y);

                if (existingChunks.ContainsKey(current)) {
                    VoxelChunk chunk = existingChunks[current];

                    if(current == checkPos) {
                        chunk.Apply(activeStencil);
                    }

                    voxelMesh.TriangulateChunkMesh(chunk);
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
            voxelMap.GenerateTerrain();
        }
        GUILayout.EndArea();
    }
}
