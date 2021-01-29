using UnityEngine;

public class VoxelMap : MonoBehaviour {
    const int threadSize = 8;

    private static string[] fillTypeNames = { "Filled", "Empty" };
    private static string[] radiusNames = { "0", "1", "2", "3", "4", "5" };
    private static string[] stencilNames = { "Square", "Circle" };

    [Range(1, 4)]
    public int chunkSize = 2;
    [Range(8,104)]
    public int voxelResolution = 8;
    public int chunkResolution = 2;
    public VoxelChunk voxelGridPrefab;
    public bool useVoxelReferences;
    public ComputeShader shader;

    private VoxelChunk[] chunks;
    private float voxelSize, halfSize;
    private int fillTypeIndex, radiusIndex, stencilIndex;
    private int[] statePositions;

    ComputeBuffer verticeBuffer;
    ComputeBuffer triangleBuffer;
    ComputeBuffer triCountBuffer;
    ComputeBuffer stateBuffer;

    public float[,] noiseMap;
    private MapDisplay mapDisplay;
    [Range(0.3f, 100)]
    public float scaleNoise;
    public bool useRandomSeed;
    public float seed = 0;

    public Texture2D[] textures;
    public Material[] materials;

    private VoxelStencil[] stencils = {
        new VoxelStencil(),
        new VoxelStencilCircle()
    };

    private void Awake() {
        Generate();
    }

    private void Generate() {
        noiseMap = new float[voxelResolution * chunkResolution, voxelResolution * chunkResolution];
        materials = new Material[chunkResolution * chunkResolution];
        textures = new Texture2D[chunkResolution * chunkResolution];
        mapDisplay = FindObjectOfType<MapDisplay>();

        halfSize = chunkSize * 0.5f * chunkResolution;
        voxelSize = (float)chunkSize / (float)voxelResolution;
        statePositions = new int[(voxelResolution + 1) * (voxelResolution + 1)];
        chunks = new VoxelChunk[chunkResolution * chunkResolution];

        // Clear old chunks
        foreach (Transform child in this.transform) {
            GameObject.DestroyImmediate(child.gameObject);
        }

        CreateBuffers();

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
            GenerateTerrain(chunk);
        }
        if (mapDisplay) { mapDisplay.DrawNoiseMap(noiseMap); }

        //generate textures
        for (int i = 0; i < chunks.Length; i++) {
            textures[i] = GenerateTexture(chunks[i]);
        }

        //create mesh
        foreach (VoxelChunk chunk in chunks) {
            TriangulateChunk(chunk);
        }

        //set material texture
        for (int i = 0; i < chunks.Length; i++) {
            Material material;
            chunks[i].GetComponent<MeshRenderer>().material = material = new Material(Shader.Find("Shader Graphs/Point URP GPU"));

            material.SetTexture("Texture2D_dcdc1b921dbd46d19dddb9cff45955b7", textures[i]);
            material.SetVector("offsetRef", new Vector2(-((1f / voxelResolution) / 2f), -((1f / voxelResolution) / 2f)));
            material.mainTexture = textures[i];
            materials[i] = material;
        }

        BoxCollider box = gameObject.GetComponent<BoxCollider>();
        if (box != null) {
            DestroyImmediate(box);

        }
        box = gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(chunkSize * chunkResolution, chunkSize * chunkResolution);
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

    private void OnDestroy() {
        if (Application.isPlaying) {
            ReleaseBuffers();
        }

        Debug.Log(1);
        foreach (Transform child in transform) {
            GameObject.DestroyImmediate(child.gameObject);
        }
    }

    private void CreateChunk(int i, int x, int y) {
        VoxelChunk chunk = Instantiate(voxelGridPrefab) as VoxelChunk;
        chunk.Initialize(useVoxelReferences, voxelResolution, chunkSize);
        chunk.transform.parent = transform;
        chunk.transform.localPosition = new Vector3(x * chunkSize - halfSize, y * chunkSize - halfSize);
        materials[i] = chunk.GetComponent<MeshRenderer>().material;
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
        activeStencil.Initialize(fillTypeIndex == 0, radiusIndex);

        int voxelYOffset = yEnd * voxelResolution;
        for (int y = yEnd; y >= yStart; y--) {
            int i = y * chunkResolution + xEnd;
            int voxelXOffset = xEnd * voxelResolution;
            for (int x = xEnd; x >= xStart; x--, i--) {
                activeStencil.SetCenter(centerX - voxelXOffset, centerY - voxelYOffset);
                chunks[i].Apply(activeStencil);
                textures[i] = GenerateTexture(chunks[i]);
                TriangulateChunk(chunks[i]);
                Material material;
                chunks[i].GetComponent<MeshRenderer>().material = material = new Material(Shader.Find("Shader Graphs/Point URP GPU"));

                material.SetTexture("Texture2D_dcdc1b921dbd46d19dddb9cff45955b7", textures[i]);
                // material.SetVector("offsetRef", new Vector2(-0.005f, -0.005f));
                material.mainTexture = textures[i];
                materials[i] = material;
                voxelXOffset -= voxelResolution;
            }
            voxelYOffset -= voxelResolution;
        }
    }

    private void CreateBuffers() {
        int numPoints = (voxelResolution + 1) * (voxelResolution + 1);
        int numVoxelsPerResolution = voxelResolution - 1;
        int numVoxels = numVoxelsPerResolution * numVoxelsPerResolution;
        int maxTriangleCount = numVoxels * 4;

        if (!Application.isPlaying || (verticeBuffer == null || numPoints != verticeBuffer.count)) {
            if (Application.isPlaying) {
                ReleaseBuffers();
            }
            verticeBuffer = new ComputeBuffer(numPoints, sizeof(float) * 3);
            triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 2 * 3, ComputeBufferType.Append);
            triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
            stateBuffer = new ComputeBuffer(numPoints, sizeof(int));
        }
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

        var vertices = new Vector3[numTris * 3];
        var triangles = new int[numTris * 3];

        for (int i = 0; i < numTris; i++) {
            for (int j = 0; j < 3; j++) {
                triangles[i * 3 + j] = i * 3 + j;

                var vertex = tris[i][j];
                vertex.x = vertex.x * chunkResolution * chunkSize;
                vertex.y = vertex.y * chunkResolution * chunkSize;

                vertices[i * 3 + j] = vertex;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    private void SetupStates(VoxelChunk chunk) {
        for (int i = 0, y = 0; y < voxelResolution; y++) {
            for (int x = 0; x < voxelResolution; x++, i++) {
                statePositions[y * voxelResolution + x + y] = chunk.voxels[i].state ? 1 : 0;
            }
        }

        for (int y = 0; y < voxelResolution; y++) {
            if (chunk.xNeighbor) {
                statePositions[y * voxelResolution + voxelResolution + y] = chunk.xNeighbor.voxels[y * voxelResolution].state ? 1 : 0;
            }
            else {
                statePositions[y * voxelResolution + voxelResolution + y] = -1;
            }
        }

        for (int x = 0; x < voxelResolution; x++) {
            if (chunk.yNeighbor) {
                statePositions[(voxelResolution + 1) * voxelResolution + x] = chunk.yNeighbor.voxels[x].state ? 1 : 0;
            }
            else {
                statePositions[(voxelResolution + 1) * voxelResolution + x] = -1;
            }
        }

        if (chunk.xyNeighbor) {
            statePositions[(voxelResolution + 1) * (voxelResolution + 1) - 1] = chunk.xyNeighbor.voxels[0].state ? 1 : 0;
        }
        else {
            statePositions[(voxelResolution + 1) * (voxelResolution + 1) - 1] = -1;
        }
    }



    private void GenerateTerrain(VoxelChunk chunk) {
        float centeredChunkX = chunk.transform.position.x + halfSize;
        float centeredChunkY = chunk.transform.position.y + halfSize;

        foreach (Voxel voxel in chunk.voxels) {
            //OFF
            // voxel.state = false;

            //ON
            // voxel.state = true;

            //RANDOM
            //voxel.state = UnityEngine.Random.Range(0, 2) == 0 ? false : true;

            //PERLIN
            int x = Mathf.RoundToInt(voxel.position.x * (voxelResolution - 1) + centeredChunkX * voxelResolution);
            int y = Mathf.RoundToInt(voxel.position.y * (voxelResolution - 1) + centeredChunkY * voxelResolution);

            float scaledX = x / scaleNoise / voxelResolution;
            float scaledY = y / scaleNoise / voxelResolution;

            noiseMap[(int)x, (int)y] = Mathf.PerlinNoise(scaledX + seed, scaledY + seed);
            voxel.state = Mathf.PerlinNoise(scaledX + seed, scaledY + seed) > 0.5f ? false : true;

            //SIMPLEX
        }
    }

    private Texture2D GenerateTexture(VoxelChunk chunk) {
        Texture2D texture = new Texture2D(voxelResolution, voxelResolution, TextureFormat.RGB48, false);
        var voxels = chunk.voxels;

        for (int i = 0; i < voxels.Length; i++) {
            if (voxels[i].state ||
               (i + 1 < voxels.Length && voxels[i + 1].state) ||
               (i + voxelResolution < voxels.Length && voxels[i + voxelResolution].state) ||
               (i + voxelResolution + 1 < voxels.Length && voxels[i + voxelResolution + 1].state)) {
                texture.SetPixel(i % voxelResolution, i / voxelResolution, UnityEngine.Random.ColorHSV());
            }
            else {
                texture.SetPixel(i % voxelResolution, i / voxelResolution, Color.black);
            }
        }

        // int i = 0;
        // foreach (Voxel voxel in chunk.voxels) {
        //     if (voxel.state) {
        //         texture.SetPixel(i % voxelResolution, i / voxelResolution, UnityEngine.Random.ColorHSV());
        //     }
        //     else {
        //         texture.SetPixel(i % voxelResolution, i / voxelResolution, Color.black);
        //     }

        //     i++;
        // }

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Point;
        texture.Apply();
        return texture;
    }


    struct Triangle {
#pragma warning disable 649 // disable unassigned variable warning
        public Vector2 a;
        public Vector2 b;
        public Vector2 c;

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

    private void OnGUI() {
        GUILayout.BeginArea(new Rect(4f, 4f, 300f, 1000f));
        GUILayout.Label("Fill Type");
        fillTypeIndex = GUILayout.SelectionGrid(fillTypeIndex, fillTypeNames, 2);
        GUILayout.Label("Radius");
        radiusIndex = GUILayout.SelectionGrid(radiusIndex, radiusNames, 6);
        GUILayout.Label("Stencil");
        stencilIndex = GUILayout.SelectionGrid(stencilIndex, stencilNames, 2);
        GUILayout.Label("Regenerate");
        if (GUI.Button(new Rect(0, 175, 150f, 20f), "Generate")) {
            foreach (Transform child in this.transform) {
                Destroy(child.gameObject);
            }
            Generate();
        }
        GUILayout.EndArea();
    }
}