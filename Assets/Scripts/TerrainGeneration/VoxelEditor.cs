using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VoxelEditor : MonoBehaviour {
    private const int UPDATE_INTERVAL = 2;

    private List<string> FillTypeNames = new List<string>();
    private static readonly string[] RadiusNames = { "0", "1", "2", "3", "4", "5" };
    private static readonly string[] StencilNames = { "Square", "Circle" };

    private int voxelResolution, chunkResolution;
    private float viewDistance;

    private int fillTypeIndex, radiusIndex, stencilIndex;

    private VoxelMesh voxelMesh;
    private ChunkCollider chunkCollider;
    private Dictionary<Vector2Int, VoxelChunk> existingChunks;
    private VoxelMap voxelMap;
    private BoxCollider box;
    private Camera mainCamera;
    private TerrainMap terrainMap;

    private Vector3 oldPoint, chunkPos, absChunkPos;
    private Vector2Int diff, absCheckPos, currentPos;
    private int oldTypeIndex, xStart, xEnd, yStart, yEnd;
    private RaycastHit hitInfo;
    private VoxelStencil activeStencil;
    private readonly List<Vector2Int> updateChunkPositions = new List<Vector2Int>();

    private readonly VoxelStencil[] stencils = {
        new VoxelStencil(),
        new VoxelStencilCircle()
    };

    public void Startup(VoxelMap map) {
        var blockTypeNames = System.Enum.GetNames(typeof(BlockType));
        foreach (var blockType in blockTypeNames) {
            FillTypeNames.Add(blockType);
        }

        voxelResolution = map.voxelResolution;
        chunkResolution = map.chunkResolution;
        viewDistance = map.viewDistance;
        existingChunks = map.existingChunks;
        voxelMap = map;
        mainCamera = Camera.main;
        terrainMap = FindObjectOfType<TerrainMap>();

        voxelMesh = FindObjectOfType<VoxelMesh>();
        chunkCollider = FindObjectOfType<ChunkCollider>();

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
            if (Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out hitInfo)) {
                if (hitInfo.collider.gameObject == gameObject &&
                    (oldPoint != hitInfo.point || oldTypeIndex != fillTypeIndex)) {
                    EditVoxels(hitInfo.point);
                    oldPoint = hitInfo.point;
                    oldTypeIndex = fillTypeIndex;
                    terrainMap.RecalculateMap();
                }
            }
        }
    }

    private void EditVoxels(Vector3 point) {
        chunkPos = new Vector3(Mathf.Floor(point.x / voxelResolution), Mathf.Floor(point.y / voxelResolution));
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

        var voxelYOffset = yEnd * voxelResolution;
        Vector2Int checkChunk;
        updateChunkPositions.Clear();

        var result = false;

        for (var y = yStart - 1; y < yEnd + 1; y++) {
            var voxelXOffset = xEnd * voxelResolution;
            for (var x = xStart - 1; x < xEnd + 1; x++) {
                activeStencil.SetCenter(diff.x - voxelXOffset, diff.y - voxelYOffset);

                checkChunk = new Vector2Int((int)Mathf.Floor((point.x + voxelXOffset) / voxelResolution), (int)Mathf.Floor((point.y + voxelYOffset) / voxelResolution));

                if (existingChunks.ContainsKey(checkChunk)) {
                    var currentChunk = existingChunks[checkChunk];
                    var tempRes = currentChunk.Apply(activeStencil);
                    if (!result && tempRes) {
                        result = true;
                    }
                }
                voxelXOffset -= voxelResolution;
            }
            voxelYOffset -= voxelResolution;
        }

        if (result) {
            checkChunk = new Vector2Int((int)Mathf.Floor((point.x) / voxelResolution), (int)Mathf.Floor((point.y) / voxelResolution));

            EditChunkAndNeighbors(checkChunk, new Vector3(Mathf.Floor(point.x), Mathf.Floor(point.y)));

            updateChunkPositions.Sort(SortByPosition);
            foreach (var chunk in from pos in updateChunkPositions where existingChunks.ContainsKey(pos) select existingChunks[pos]) {
                voxelMesh.TriangulateChunkMesh(chunk);
                chunkCollider.Generate2DCollider(chunk, chunkResolution);
            }
        }
    }

    private static int SortByPosition(Vector2Int c1, Vector2Int c2) {
        return (c1.x < c2.x && c1.y < c2.y) ? 1 : 0;
    }

    private void EditChunkAndNeighbors(Vector2Int checkChunk, Vector2 pos) {
        for (var x = -1; x <= 1; x++) {
            for (var y = -1; y <= 1; y++) {
                var result = false;
                var currentChunkPos = new Vector2Int(checkChunk.x + x, checkChunk.y + y);

                for (var index = 0; index <= radiusIndex; index++) {
                    for (var index2 = 0; index2 <= radiusIndex; index2++) {
                        switch (x) {
                            case -1 when y == -1 && (Mathf.Abs(pos.x - index) % 8 == 0) && (Mathf.Abs(pos.y - index2) % 8 == 0): //1
                            case 0 when y == -1 && (Mathf.Abs(pos.y - index) % 8 == 0): //2
                            case 1 when y == -1 && (Mathf.Abs(pos.x + 1 + index) % 8 == 0) && (Mathf.Abs(pos.y - index2) % 8 == 0): //3
                            case -1 when y == 0 && (Mathf.Abs(pos.x - index) % 8 == 0): //4
                            case 0 when y == 0: //5
                            case 1 when y == 0 && (Mathf.Abs(pos.x + 1 + index) % 8 == 0): //6
                            case -1 when y == 1 && (Mathf.Abs(pos.x - index) % 8 == 0) && (Mathf.Abs(pos.y + 1 + index2) % 8 == 0): //7
                            case 0 when y == 1 && (Mathf.Abs(pos.y + 1 + index) % 8 == 0): //8
                            case 1 when y == 1 && (Mathf.Abs(pos.x + 1 - index) % 8 == 0) && (Mathf.Abs(pos.y + 1 + index2) % 8 == 0): //9
                                result = true;
                                break;
                        }
                        if (result) break;
                    }
                    if (result) break;
                }

                if (result) {
                    if (!updateChunkPositions.Contains(currentChunkPos)) {
                        updateChunkPositions.Add(currentChunkPos);
                    }
                }
            }
        }
    }

    private void OnGUI() {
        GUILayout.BeginArea(new Rect(4f, 4f, 150f, 1000f));
        GUILayout.Label("Fill Type");
        fillTypeIndex = GUILayout.SelectionGrid(fillTypeIndex, FillTypeNames.ToArray(), 2);
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