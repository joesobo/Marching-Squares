using UnityEngine;

public partial class VoxelMap : MonoBehaviour {
    [Range(8, 56)]
    public int voxelResolution = 8;
    [Range(1, 16)]
    public int chunkResolution = 2;
    public VoxelChunk voxelChunkPrefab;
    public bool useVoxelReferences = false;
    public ComputeShader shader;
    public bool useColliders = false;

    private TerrainNoise terrainNoise;
    private VoxelMesh voxelMesh;
    private VoxelEditor voxelEditor;

    private VoxelChunk[] chunks;
    private float voxelSize, halfSize;
    private Camera mainCam;

    private void Awake() {
        terrainNoise = FindObjectOfType<TerrainNoise>();
        voxelMesh = FindObjectOfType<VoxelMesh>();
        voxelEditor = FindObjectOfType<VoxelEditor>();

        mainCam = Camera.main;

        GenerateTerrain();
    }

    public void GenerateTerrain() {
        halfSize = 0.5f * chunkResolution;
        voxelSize = 1f / voxelResolution;
        chunks = new VoxelChunk[chunkResolution * chunkResolution];

        mainCam.orthographicSize = halfSize;

        terrainNoise.Startup(voxelResolution, chunkResolution);
        voxelMesh.Startup(voxelResolution, chunkResolution, useColliders);
        voxelEditor.Startup(voxelResolution, chunkResolution, chunks, this);

        Cleanup();

        CreateChunks();

        terrainNoise.GenerateNoise(chunks);

        voxelMesh.TriangulateChunks(chunks);
    }

    public void Cleanup() {
        GameObject[] objsToDestroy = new GameObject[transform.childCount];
        int idx = 0;
        foreach (Transform child in transform) {
            objsToDestroy[idx++] = child.gameObject;
        }
        for (int i = 0; i < objsToDestroy.Length; i++) {
            DestroyImmediate(objsToDestroy[i]);
        }
    }

    private void CreateChunks() {
        for (int i = 0, y = 0; y < chunkResolution; y++) {
            for (int x = 0; x < chunkResolution; x++, i++) {
                CreateChunk(i, x, y);
            }
        }
    }

    private void CreateChunk(int i, int x, int y) {
        VoxelChunk chunk = Instantiate(voxelChunkPrefab) as VoxelChunk;
        chunk.Initialize(useVoxelReferences, voxelResolution);
        chunk.transform.parent = transform;
        chunk.transform.localPosition = new Vector3(x - halfSize, y - halfSize);
        chunk.gameObject.layer = 3;
        chunks[i] = chunk;
        if (x > 0) {
            chunks[i - 1].xNeighbor = chunk;
        }
        if (y > 0) {
            chunks[i - chunkResolution].yNeighbor = chunk;
            if (x > 0) {
                chunks[i - chunkResolution - 1].xyNeighbor = chunk;
            }
        }
    }

    private void OnDestroy() {
        Cleanup();
    }
}