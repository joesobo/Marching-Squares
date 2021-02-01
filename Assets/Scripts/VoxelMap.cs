using UnityEngine;

public class VoxelMap : MonoBehaviour {
    const int threadSize = 8;

    private static string[] fillTypeNames = { "Empty", "White", "Red", "Blue", "Green" };
    private static string[] radiusNames = { "0", "1", "2", "3", "4", "5" };
    private static string[] stencilNames = { "Square", "Circle" };

    [Range(8, 56)]
    public int voxelResolution = 8;
    public int chunkResolution = 2;
    public VoxelChunk voxelChunkPrefab;
    public bool useVoxelReferences = false;
    public ComputeShader shader;

    private VoxelChunk[] chunks;
    private float voxelSize, halfSize;
    private int fillTypeIndex, radiusIndex, stencilIndex;
    private int[] statePositions;

    ComputeBuffer verticeBuffer;
    ComputeBuffer triangleBuffer;
    //ComputeBuffer colorBuffer;
    ComputeBuffer triCountBuffer;
    ComputeBuffer stateBuffer;

    private float[,] noiseMap;
    private MapDisplay mapDisplay;
    [Range(0.3f, 100)]
    public float scaleNoise;
    public bool useRandomSeed;
    public float seed = 0;

    private VoxelStencil[] stencils = {
        new VoxelStencil(),
        new VoxelStencilCircle()
    };

    private void Awake() {
        mapDisplay = FindObjectOfType<MapDisplay>();

        Generate();
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

    private void Generate() {
        noiseMap = new float[voxelResolution * chunkResolution, voxelResolution * chunkResolution];

        halfSize = 0.5f * chunkResolution;
        voxelSize = 1f / voxelResolution;
        statePositions = new int[(voxelResolution + 1) * (voxelResolution + 1)];
        chunks = new VoxelChunk[chunkResolution * chunkResolution];

        // Clear old chunks
        foreach (Transform child in this.transform) {
            GameObject.DestroyImmediate(child.gameObject);
        }

        // Setup new chunks
        for (int i = 0, y = 0; y < chunkResolution; y++) {
            for (int x = 0; x < chunkResolution; x++, i++) {
                CreateChunk(i, x, y);
            }
        }

        //generate terrain values
        if (useRandomSeed) {
            seed = Random.Range(0f, 10000f);
        }
        foreach (VoxelChunk chunk in chunks) {
            GenerateTerrainValues(chunk);
        }
        if (mapDisplay) { mapDisplay.DrawNoiseMap(noiseMap); }

        CreateBuffers();
        GenerateTerrain();

        BoxCollider box = gameObject.GetComponent<BoxCollider>();
        if (box != null) {
            DestroyImmediate(box);

        }
        box = gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(chunkResolution, chunkResolution);
    }

    private void GenerateTerrain() {
        //create mesh
        foreach (VoxelChunk chunk in chunks) {
            TriangulateChunk(chunk);
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
                // textures[i] = GenerateTexture(chunks[i]);

                TriangulateChunk(chunks[i]);


                voxelXOffset -= voxelResolution;
            }
            voxelYOffset -= voxelResolution;
        }

        GenerateTerrain();
    }

    private void CreateBuffers() {
        int numPoints = (voxelResolution + 1) * (voxelResolution + 1);
        int numVoxelsPerResolution = voxelResolution - 1;
        int numVoxels = numVoxelsPerResolution * numVoxelsPerResolution;
        int maxTriangleCount = numVoxels * 12;

        ReleaseBuffers();
        verticeBuffer = new ComputeBuffer(numPoints, sizeof(float) * 3);
        triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
        triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        stateBuffer = new ComputeBuffer(numPoints, sizeof(int));
    }

    private void ReleaseBuffers() {
        if (triangleBuffer != null) {
            verticeBuffer.Release();
            triangleBuffer.Release();
            triCountBuffer.Release();
            stateBuffer.Release();
        }
    }

    public void TriangulateChunk(VoxelChunk chunk) {
        Mesh mesh = chunk.mesh;

        mesh.Clear();

        // Compute Shader Here
        int numThreadsPerResolution = Mathf.CeilToInt(voxelResolution / threadSize);

        triangleBuffer.SetCounterValue(0);
        shader.SetBuffer(0, "_Vertices", verticeBuffer);
        shader.SetBuffer(0, "_Triangles", triangleBuffer);
        shader.SetBuffer(0, "_States", stateBuffer);
        shader.SetInt("_VoxelResolution", voxelResolution);
        shader.SetInt("_ChunkResolution", chunkResolution);

        SetupStates(chunk);
        stateBuffer.SetData(statePositions);

        shader.Dispatch(0, numThreadsPerResolution, numThreadsPerResolution, 1);

        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        Triangle[] tris = new Triangle[numTris];
        triangleBuffer.GetData(tris, 0, 0, numTris);

        Vector3[] vertices = new Vector3[numTris * 3];
        int[] triangles = new int[numTris * 3];
        Color32[] colors = new Color32[numTris * 3];

        for (int i = 0; i < numTris; i++) {
            for (int j = 0; j < 3; j++) {
                colors[i * 3 + j] = new Color32((byte)(tris[i].red * 255), (byte)(tris[i].green * 255), (byte)(tris[i].blue * 255), 255);

                triangles[i * 3 + j] = i * 3 + j;

                var vertex = tris[i][j];
                vertex.x = vertex.x * chunkResolution;
                vertex.y = vertex.y * chunkResolution;

                vertices[i * 3 + j] = vertex;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors32 = colors;
        mesh.RecalculateNormals();
    }

    private void ColorTriangle(Color[] colors, Color color, int index) {
        for (int j = 0; j < 3; j++) {
            colors[index + j] = color;
        }
    }

    private void SetupStates(VoxelChunk chunk) {
        for (int i = 0, y = 0; y < voxelResolution; y++) {
            for (int x = 0; x < voxelResolution; x++, i++) {
                statePositions[y * voxelResolution + x + y] = chunk.voxels[i].state;
            }
        }

        for (int y = 0; y < voxelResolution; y++) {
            if (chunk.xNeighbor) {
                statePositions[y * voxelResolution + voxelResolution + y] = chunk.xNeighbor.voxels[y * voxelResolution].state;
            } else {
                statePositions[y * voxelResolution + voxelResolution + y] = -1;
            }
        }

        for (int x = 0; x < voxelResolution; x++) {
            if (chunk.yNeighbor) {
                statePositions[(voxelResolution + 1) * voxelResolution + x] = chunk.yNeighbor.voxels[x].state;
            } else {
                statePositions[(voxelResolution + 1) * voxelResolution + x] = -1;
            }
        }

        if (chunk.xyNeighbor) {
            statePositions[(voxelResolution + 1) * (voxelResolution + 1) - 1] = chunk.xyNeighbor.voxels[0].state;
        } else {
            statePositions[(voxelResolution + 1) * (voxelResolution + 1) - 1] = -1;
        }
    }

    private void GenerateTerrainValues(VoxelChunk chunk) {
        float centeredChunkX = chunk.transform.position.x + halfSize;
        float centeredChunkY = chunk.transform.position.y + halfSize;

        foreach (Voxel voxel in chunk.voxels) {
            //OFF
            voxel.state = 0;

            //ON
            // voxel.state = 1;

            //RANDOM
            // voxel.state = UnityEngine.Random.Range(0, 2);

            //PERLIN
            int x = Mathf.RoundToInt(voxel.position.x * (voxelResolution - 1) + centeredChunkX * voxelResolution);
            int y = Mathf.RoundToInt(voxel.position.y * (voxelResolution - 1) + centeredChunkY * voxelResolution);

            float scaledX = x / scaleNoise / voxelResolution;
            float scaledY = y / scaleNoise / voxelResolution;

            noiseMap[(int)x, (int)y] = Mathf.PerlinNoise(scaledX + seed, scaledY + seed);
            voxel.state = Mathf.PerlinNoise(scaledX + seed, scaledY + seed) > 0.5f ? 0 : Mathf.RoundToInt(Random.Range(1, 5));
        }
    }

    private void OnDestroy() {
        if (Application.isPlaying) {
            ReleaseBuffers();
        }

        foreach (Transform child in transform) {
            GameObject.DestroyImmediate(child.gameObject);
        }
    }

    struct Triangle {
#pragma warning disable 649 // disable unassigned variable warning
        public Vector2 a;
        public Vector2 b;
        public Vector2 c;
        public float red;
        public float green;
        public float blue;

        public Vector2 this[int i] {
            get {
                switch (i) {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }
    }

//     struct TriangleColor {
// #pragma warning disable 649 // disable unassigned variable warning
//         public float red;
//         public float green;
//         public float blue;
//     }

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
            Generate();
        }
        GUILayout.EndArea();
    }
}