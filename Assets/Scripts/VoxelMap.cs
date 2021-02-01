using UnityEngine;

public partial class VoxelMap : MonoBehaviour {
    private static string[] fillTypeNames = { "Empty", "White", "Red", "Blue", "Green" };
    private static string[] radiusNames = { "0", "1", "2", "3", "4", "5" };
    private static string[] stencilNames = { "Square", "Circle" };

    [Range(8, 56)]
    public int voxelResolution = 8;
    public int chunkResolution = 2;
    public VoxelChunk voxelChunkPrefab;
    public bool useVoxelReferences = false;
    public ComputeShader shader;

    private TerrainNoise terrainNoise;
    private VoxelMesh voxelMesh;

    private VoxelChunk[] chunks;
    private float voxelSize, halfSize;
    private int fillTypeIndex, radiusIndex, stencilIndex;

    private VoxelStencil[] stencils = {
        new VoxelStencil(),
        new VoxelStencilCircle()
    };

    private void Awake() {
        terrainNoise = FindObjectOfType<TerrainNoise>();
        voxelMesh = FindObjectOfType<VoxelMesh>();

        GenerateTerrain();
    }

    private void Update() {
        if (Input.GetMouseButton(0)) {
            RaycastHit hitInfo;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo)) {
                if (hitInfo.collider.gameObject == gameObject) {
                    EditVoxels(transform.InverseTransformPoint(hitInfo.point));
                }
            }
        }
    }

    private void GenerateTerrain() {
        halfSize = 0.5f * chunkResolution;
        voxelSize = 1f / voxelResolution;
        chunks = new VoxelChunk[chunkResolution * chunkResolution];

        terrainNoise.Startup(voxelResolution, chunkResolution);
        voxelMesh.Startup(voxelResolution, chunkResolution);
        
        Cleanup();

        // Setup new chunks
        for (int i = 0, y = 0; y < chunkResolution; y++) {
            for (int x = 0; x < chunkResolution; x++, i++) {
                CreateChunk(i, x, y);
            }
        }

        terrainNoise.GenerateNoise(chunks);

        voxelMesh.TriangulateChunks(chunks);

        BoxCollider box = gameObject.GetComponent<BoxCollider>();
        if (box != null) {
            DestroyImmediate(box);

        }
        box = gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(chunkResolution, chunkResolution);
    }

    private void Cleanup() {
        foreach (Transform child in this.transform) {
            GameObject.DestroyImmediate(child.gameObject);
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

    private void EditVoxels(Vector3 point) {
        int centerX = (int)((point.x + halfSize) / voxelSize);
        int centerY = (int)((point.y + halfSize) / voxelSize);

        int xStart = (centerX - radiusIndex - 1) / voxelResolution;
        if (xStart < 0) {
            xStart = 0;
        }
        int xEnd = (centerX + radiusIndex) / voxelResolution;
        if (xEnd >= chunkResolution) {
            xEnd = chunkResolution - 1;
        }
        int yStart = (centerY - radiusIndex - 1) / voxelResolution;
        if (yStart < 0) {
            yStart = 0;
        }
        int yEnd = (centerY + radiusIndex) / voxelResolution;
        if (yEnd >= chunkResolution) {
            yEnd = chunkResolution - 1;
        }

        VoxelStencil activeStencil = stencils[stencilIndex];
        activeStencil.Initialize(fillTypeIndex, radiusIndex);

        int voxelYOffset = yEnd * voxelResolution;
        for (int y = yEnd; y >= yStart; y--) {
            int i = y * chunkResolution + xEnd;
            int voxelXOffset = xEnd * voxelResolution;
            for (int x = xEnd; x >= xStart; x--, i--) {
                activeStencil.SetCenter(centerX - voxelXOffset, centerY - voxelYOffset);
                chunks[i].Apply(activeStencil);

                voxelMesh.TriangulateChunk(chunks[i]);

                voxelXOffset -= voxelResolution;
            }
            voxelYOffset -= voxelResolution;
        }

        voxelMesh.TriangulateChunks(chunks);
    }

    private void OnDestroy() {
        foreach (Transform child in transform) {
            GameObject.DestroyImmediate(child.gameObject);
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
            foreach (Transform child in this.transform) {
                Destroy(child.gameObject);
            }
            GenerateTerrain();
        }
        GUILayout.EndArea();
    }
}