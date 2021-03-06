using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VoxelMesh : MonoBehaviour {
    private const int THREADS = 8;

    public ComputeShader shader;

    public VoxelChunk voxelChunkPrefab;
    public bool useVoxelReferences = false;

    private int voxelResolution, chunkResolution, textureTileAmount;
    private bool useColliders;
    private int[] statePositions;
    private float viewDistance, colliderRadius;

    private Vector2 p, playerOffset, offset;
    private Vector2Int playerCoord, testChunkPos, coord;
    private Vector2[] uvs;
    private VoxelChunk testChunk, currentChunk;
    private float sqrViewDist, sqrDst;
    private Mesh mesh;

    private Queue<VoxelChunk> recycleableChunks;
    private Dictionary<Vector2Int, VoxelChunk> existingChunks;

    private ChunkCollider chunkCollider;
    private Transform player;
    private TerrainNoise terrainNoise;

    private ComputeBuffer verticeBuffer, triangleBuffer, triCountBuffer, stateBuffer;

    public void Startup(int voxelResolution, int chunkResolution, float viewDistance,
        Dictionary<Vector2Int, VoxelChunk> existingChunks, bool useColliders, float colliderRadius) {
        this.voxelResolution = voxelResolution;
        this.chunkResolution = chunkResolution;
        this.existingChunks = existingChunks;
        this.viewDistance = viewDistance;
        this.useColliders = useColliders;
        this.colliderRadius = colliderRadius;

        textureTileAmount = (voxelResolution * chunkResolution) / 2;

        statePositions = new int[(voxelResolution + 1) * (voxelResolution + 1)];

        chunkCollider = FindObjectOfType<ChunkCollider>();
        player = FindObjectOfType<PlayerController>().transform;

        recycleableChunks = new Queue<VoxelChunk>();

        terrainNoise = FindObjectOfType<TerrainNoise>();
        terrainNoise.Startup(voxelResolution, chunkResolution, player);
    }

    public void TriangulateChunks(List<VoxelChunk> chunks) {
        CreateBuffers();

        p = player.position / voxelResolution;
        playerCoord = new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));

        sqrViewDist = viewDistance * viewDistance;

        // Remove chunks out of range
        RemoveOutOfBoundsChunks(chunks);

        // Create new chunks in range
        CreateNewChunksInRange(chunks);

        // update chunk neighbors for new chunks
        UpdateNewChunks(chunks);

        // recreate all chunk meshes
        RecreateUpdatedChunkMeshes(chunks);
    }

    private void RecreateUpdatedChunkMeshes(IEnumerable<VoxelChunk> chunks) {
        foreach (var chunk in chunks) {
            if (chunk.shouldUpdateMesh) {
                TriangulateChunkMesh(chunk);
                chunk.shouldUpdateMesh = false;
            }

            if (useColliders && chunk.shouldUpdateCollider)
                if (Vector3.Distance(p, chunk.transform.position) < colliderRadius) {
                    chunkCollider.Generate2DCollider(chunk, chunkResolution);
                    chunk.shouldUpdateCollider = false;
                }
        }
    }

    private void UpdateNewChunks(IEnumerable<VoxelChunk> chunks) {
        foreach (var chunk in chunks.Where(chunk => chunk.shouldUpdateMesh)) {
            var position = chunk.transform.position;
            coord = new Vector2Int(Mathf.RoundToInt(position.x),
                Mathf.RoundToInt(position.y));
            SetupChunkNeighbors(coord, chunk);
        }
    }

    private void CreateNewChunksInRange(List<VoxelChunk> chunks) {
        for (int y = -chunkResolution / 2, i = 0; y < chunkResolution / 2; y++) {
            for (int x = -chunkResolution / 2; x < chunkResolution / 2; x++, i++) {
                coord = new Vector2Int(x, y) + playerCoord;

                if (!existingChunks.ContainsKey(coord)) {
                    playerOffset = p - coord;
                    offset = new Vector2(Mathf.Abs(playerOffset.x), Mathf.Abs(playerOffset.y)) -
                             (Vector2.one * viewDistance) / 2;
                    sqrDst = offset.sqrMagnitude;

                    if (sqrDst <= sqrViewDist - 4) {
                        if (recycleableChunks.Count > 0) {
                            currentChunk = recycleableChunks.Dequeue();
                            var currentColliders = currentChunk.gameObject.GetComponents<EdgeCollider2D>();
                            foreach (var t in currentColliders) {
                                Destroy(t);
                            }
                        }
                        else {
                            currentChunk = CreateChunk(i, x, y, chunks);
                        }

                        currentChunk.SetNewChunk(coord.x, coord.y);
                        existingChunks.Add(coord, currentChunk);
                        currentChunk.shouldUpdateCollider = true;
                        chunks.Add(currentChunk);
                        terrainNoise.GenerateNoiseValues(currentChunk);
                    }
                }
            }
        }
    }

    private void RemoveOutOfBoundsChunks(IList<VoxelChunk> chunks) {
        for (int i = chunks.Count - 1; i >= 0; i--) {
            testChunk = chunks[i];
            var position = testChunk.transform.position;
            testChunkPos = new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y));
            playerOffset = playerCoord - testChunkPos;
            offset = new Vector2(Mathf.Abs(playerOffset.x), Mathf.Abs(playerOffset.y)) -
                     (Vector2.one * viewDistance) / 2;
            sqrDst = new Vector2(Mathf.Max(offset.x, 0), Mathf.Max(offset.y, 0)).sqrMagnitude;

            if (sqrDst > sqrViewDist) {
                existingChunks.Remove(testChunkPos);
                recycleableChunks.Enqueue(testChunk);
                chunks.RemoveAt(i);
            }
        }
    }

    private VoxelChunk CreateChunk(int i, int x, int y, List<VoxelChunk> chunks) {
        var chunk = Instantiate(voxelChunkPrefab, transform, true) as VoxelChunk;
        chunk.Initialize(useVoxelReferences, voxelResolution);
        chunk.transform.localPosition = new Vector3(x, y);
        chunk.gameObject.layer = 3;

        return chunk;
    }

    private void SetupChunkNeighbors(Vector2Int setupCoord, VoxelChunk chunk) {
        var axcoord = new Vector2Int(setupCoord.x - 1, setupCoord.y);
        var aycoord = new Vector2Int(setupCoord.x, setupCoord.y - 1);
        var axycoord = new Vector2Int(setupCoord.x - 1, setupCoord.y - 1);
        var bxcoord = new Vector2Int(setupCoord.x + 1, setupCoord.y);
        var bycoord = new Vector2Int(setupCoord.x, setupCoord.y + 1);
        var bxycoord = new Vector2Int(setupCoord.x + 1, setupCoord.y + 1);
        VoxelChunk tempChunk;

        if (!existingChunks.ContainsKey(setupCoord)) return;
        if (existingChunks.ContainsKey(axcoord)) {
            tempChunk = existingChunks[axcoord];
            tempChunk.shouldUpdateMesh = true;
            tempChunk.xNeighbor = chunk;
        }

        if (existingChunks.ContainsKey(aycoord)) {
            tempChunk = existingChunks[aycoord];
            tempChunk.shouldUpdateMesh = true;
            tempChunk.yNeighbor = chunk;
        }

        if (existingChunks.ContainsKey(axycoord)) {
            tempChunk = existingChunks[axycoord];
            tempChunk.shouldUpdateMesh = true;
            tempChunk.xyNeighbor = chunk;
        }

        if (existingChunks.ContainsKey(bxcoord)) {
            tempChunk = existingChunks[bxcoord];
            chunk.xNeighbor = tempChunk;
        }

        if (existingChunks.ContainsKey(bycoord)) {
            tempChunk = existingChunks[bycoord];
            chunk.yNeighbor = tempChunk;
        }

        if (existingChunks.ContainsKey(bxycoord)) {
            tempChunk = existingChunks[bxycoord];
            chunk.xyNeighbor = tempChunk;
        }
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

    public void TriangulateChunkMesh(VoxelChunk chunk) {
        mesh = chunk.mesh;
        chunk.ResetValues();

        ShaderTriangulate(chunk, out chunk.vertices, out chunk.triangles, out chunk.colors);

        GetUVs(chunk);

        AddVerticeToDictionary(chunk);

        mesh.vertices = chunk.vertices;
        mesh.triangles = chunk.triangles;
        mesh.uv = uvs;
        mesh.colors32 = chunk.colors;
        mesh.RecalculateNormals();
    }

    private void GetUVs(VoxelChunk chunk) {
        uvs = new Vector2[chunk.vertices.Length];
        for (int i = 0; i < chunk.vertices.Length; i++) {
            float percentX = Mathf.InverseLerp(0, chunkResolution * voxelResolution, chunk.vertices[i].x) *
                             textureTileAmount;
            float percentY = Mathf.InverseLerp(0, chunkResolution * voxelResolution, chunk.vertices[i].y) *
                             textureTileAmount;
            uvs[i] = new Vector2(percentX, percentY);
        }
    }

    private static void AddVerticeToDictionary(VoxelChunk chunk) {
        for (int i = 0; i < chunk.vertices.Length; i++) {
            if (!chunk.verticeDictionary.ContainsKey(chunk.vertices[i])) {
                chunk.verticeDictionary.Add(chunk.vertices[i], i);
            }
        }
    }

    private void ShaderTriangulate(VoxelChunk chunk, out Vector3[] vertices, out int[] triangles,
        out Color32[] colors) {
        int numThreadsPerResolution = Mathf.CeilToInt(voxelResolution / THREADS);

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
        int[] triCountArray = {0};
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        var tris = new Triangle[numTris];
        triangleBuffer.GetData(tris, 0, 0, numTris);

        vertices = new Vector3[numTris * 3];
        triangles = new int[numTris * 3];
        colors = new Color32[numTris * 3];

        GetShaderData(numTris, tris, vertices, triangles, colors, chunk);
    }

    private void GetShaderData(int numTris, IList<Triangle> tris, IList<Vector3> vertices, IList<int> triangles,
        IList<Color32> colors,
        VoxelChunk chunk) {
        for (int i = 0; i < numTris; i++) {
            for (int j = 0; j < 3; j++) {
                colors[i * 3 + j] = new Color32((byte) (tris[i].red * 255), (byte) (tris[i].green * 255),
                    (byte) (tris[i].blue * 255), 255);

                triangles[i * 3 + j] = i * 3 + j;

                var vertex = tris[i][j];
                vertex.x *= chunkResolution;
                vertex.y *= chunkResolution;

                vertices[i * 3 + j] = vertex;

                AddTriangleToDictionary(i * 3 + j, tris[i], chunk);
            }
        }
    }

    private static void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle, VoxelChunk chunk) {
        if (chunk.triangleDictionary.ContainsKey(chunk.vertices[vertexIndexKey])) {
            chunk.triangleDictionary[chunk.vertices[vertexIndexKey]].Add(triangle);
        }
        else {
            var triangleList = new List<Triangle> {triangle};
            chunk.triangleDictionary.Add(chunk.vertices[vertexIndexKey], triangleList);
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
                statePositions[y * voxelResolution + voxelResolution + y] =
                    chunk.xNeighbor.voxels[y * voxelResolution].state;
            }
            else {
                statePositions[y * voxelResolution + voxelResolution + y] = -1;
            }
        }

        for (int x = 0; x < voxelResolution; x++) {
            if (chunk.yNeighbor) {
                statePositions[(voxelResolution + 1) * voxelResolution + x] = chunk.yNeighbor.voxels[x].state;
            }
            else {
                statePositions[(voxelResolution + 1) * voxelResolution + x] = -1;
            }
        }

        if (chunk.xyNeighbor) {
            statePositions[(voxelResolution + 1) * (voxelResolution + 1) - 1] = chunk.xyNeighbor.voxels[0].state;
        }
        else {
            statePositions[(voxelResolution + 1) * (voxelResolution + 1) - 1] = -1;
        }
    }

    private void OnDestroy() {
        if (Application.isPlaying) {
            ReleaseBuffers();
        }
    }
}
