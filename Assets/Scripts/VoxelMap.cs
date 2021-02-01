using UnityEngine;

public partial class VoxelMap : MonoBehaviour {
    [Range(8, 56)]
    public int voxelResolution = 8;
    public int chunkResolution = 2;
    public VoxelChunk voxelChunkPrefab;
    public bool useVoxelReferences = false;
    public ComputeShader shader;

    private TerrainNoise terrainNoise;
    private VoxelMesh voxelMesh;
    private VoxelEditor voxelEditor;

    private VoxelChunk[] chunks;
    private float voxelSize, halfSize;

    private void Awake() {
        terrainNoise = FindObjectOfType<TerrainNoise>();
        voxelMesh = FindObjectOfType<VoxelMesh>();
        voxelEditor = FindObjectOfType<VoxelEditor>();

        GenerateTerrain();
    }

    private void GenerateTerrain() {
        halfSize = 0.5f * chunkResolution;
        voxelSize = 1f / voxelResolution;
        chunks = new VoxelChunk[chunkResolution * chunkResolution];

        terrainNoise.Startup(voxelResolution, chunkResolution);
        voxelMesh.Startup(voxelResolution, chunkResolution);
        voxelEditor.Startup(voxelResolution, chunkResolution, chunks);

        Cleanup();

        CreateChunks();

        terrainNoise.GenerateNoise(chunks);

        voxelMesh.TriangulateChunks(chunks);
    }

    private void Cleanup() {
        foreach (Transform child in this.transform) {
            GameObject.DestroyImmediate(child.gameObject);
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
        foreach (Transform child in transform) {
            GameObject.DestroyImmediate(child.gameObject);
        }
    }

    private void OnGUI() {
        GUILayout.BeginArea(new Rect(4f, 4f, 150f, 1000f));
        GUILayout.Label("Regenerate");
        if (GUI.Button(new Rect(0, 225, 150f, 20f), "Generate")) {
            foreach (Transform child in this.transform) {
                Destroy(child.gameObject);
            }
            GenerateTerrain();
        }
        GUILayout.EndArea();
    }
}