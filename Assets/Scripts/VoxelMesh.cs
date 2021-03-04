using System.Collections.Generic;
using UnityEngine;

public class VoxelMesh : MonoBehaviour {
    const int THREADS = 8;

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

    ComputeBuffer verticeBuffer, triangleBuffer, triCountBuffer, stateBuffer;

    public void Startup(int voxelResolution, int chunkResolution, float viewDistance, Dictionary<Vector2Int, VoxelChunk> existingChunks, bool useColliders, float colliderRadius) {
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
        for (int i = chunks.Count - 1; i >= 0; i--) {
            testChunk = chunks[i];
            testChunkPos = new Vector2Int(Mathf.RoundToInt(testChunk.transform.position.x), Mathf.RoundToInt(testChunk.transform.position.y));
            playerOffset = playerCoord - testChunkPos;
            offset = new Vector2(Mathf.Abs(playerOffset.x), Mathf.Abs(playerOffset.y)) - (Vector2.one * viewDistance) / 2;
            sqrDst = new Vector2(Mathf.Max(offset.x, 0), Mathf.Max(offset.y, 0)).sqrMagnitude;

            if (sqrDst > sqrViewDist) {
                existingChunks.Remove(testChunkPos);
                recycleableChunks.Enqueue(testChunk);
                chunks.RemoveAt(i);
            }
        }

        // Create new chunks in range
        for (int y = -chunkResolution / 2, i = 0; y < chunkResolution / 2; y++) {
            for (int x = -chunkResolution / 2; x < chunkResolution / 2; x++, i++) {
                coord = new Vector2Int(x, y) + playerCoord;

                if (existingChunks.ContainsKey(coord)) {
                    continue;
                }

                playerOffset = p - coord;
                offset = new Vector2(Mathf.Abs(playerOffset.x), Mathf.Abs(playerOffset.y)) - (Vector2.one * viewDistance) / 2;
                sqrDst = offset.sqrMagnitude;

                if (sqrDst <= sqrViewDist - 4) {
                    if (recycleableChunks.Count > 0) {
                        currentChunk = recycleableChunks.Dequeue();
                        EdgeCollider2D[] currentColliders = currentChunk.gameObject.GetComponents<EdgeCollider2D>();
                        for (int j = 0; j < currentColliders.Length; j++) {
                            Destroy(currentColliders[j]);
                        }
                    } else {
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

        // update chunk neighbors for new chunks
        foreach (VoxelChunk chunk in chunks) {
            if (chunk.shouldUpdateMesh) {
                coord = new Vector2Int(Mathf.RoundToInt(chunk.transform.position.x), Mathf.RoundToInt(chunk.transform.position.y));
                SetupChunkNeighbors(coord, chunk);
            }
        }

        // recreate all chunk meshes
        foreach (VoxelChunk chunk in chunks) {
            if (chunk.shouldUpdateMesh) {
                TriangulateChunkMesh(chunk);
                chunk.shouldUpdateMesh = false;
            }

            if (useColliders && chunk.shouldUpdateCollider) {
                if (Vector3.Distance(p, chunk.transform.position) < colliderRadius) {
                    chunkCollider.Generate2DCollider(chunk, chunkResolution);
                    chunk.shouldUpdateCollider = false;
                }
            }
        }
    }

    private VoxelChunk CreateChunk(int i, int x, int y, List<VoxelChunk> chunks) {
        VoxelChunk chunk = Instantiate(voxelChunkPrefab) as VoxelChunk;
        chunk.Initialize(useVoxelReferences, voxelResolution);
        chunk.transform.parent = transform;
        chunk.transform.localPosition = new Vector3(x, y);
        chunk.gameObject.layer = 3;

        return chunk;
    }

    private void SetupChunkNeighbors(Vector2Int coord, VoxelChunk chunk) {
        Vector2Int Axcoord = new Vector2Int(coord.x - 1, coord.y);
        Vector2Int Aycoord = new Vector2Int(coord.x, coord.y - 1);
        Vector2Int Axycoord = new Vector2Int(coord.x - 1, coord.y - 1);
        Vector2Int Bxcoord = new Vector2Int(coord.x + 1, coord.y);
        Vector2Int Bycoord = new Vector2Int(coord.x, coord.y + 1);
        Vector2Int Bxycoord = new Vector2Int(coord.x + 1, coord.y + 1);
        VoxelChunk tempChunk;

        if (existingChunks.ContainsKey(coord)) {
            if (existingChunks.ContainsKey(Axcoord)) {
                tempChunk = existingChunks[Axcoord];
                tempChunk.shouldUpdateMesh = true;
                tempChunk.xNeighbor = chunk;
            }
            if (existingChunks.ContainsKey(Aycoord)) {
                tempChunk = existingChunks[Aycoord];
                tempChunk.shouldUpdateMesh = true;
                tempChunk.yNeighbor = chunk;
            }
            if (existingChunks.ContainsKey(Axycoord)) {
                tempChunk = existingChunks[Axycoord];
                tempChunk.shouldUpdateMesh = true;
                tempChunk.xyNeighbor = chunk;
            }

            if (existingChunks.ContainsKey(Bxcoord)) {
                tempChunk = existingChunks[Bxcoord];
                chunk.xNeighbor = tempChunk;
            }
            if (existingChunks.ContainsKey(Bycoord)) {
                tempChunk = existingChunks[Bycoord];
                chunk.yNeighbor = tempChunk;
            }
            if (existingChunks.ContainsKey(Bxycoord)) {
                tempChunk = existingChunks[Bxycoord];
                chunk.xyNeighbor = tempChunk;
            }
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
            float percentX = Mathf.InverseLerp(0, chunkResolution * voxelResolution, chunk.vertices[i].x) * textureTileAmount;
            float percentY = Mathf.InverseLerp(0, chunkResolution * voxelResolution, chunk.vertices[i].y) * textureTileAmount;
            uvs[i] = new Vector2(percentX, percentY);
        }
    }

    private void AddVerticeToDictionary(VoxelChunk chunk) {
        for (int i = 0; i < chunk.vertices.Length; i++) {
            if(!chunk.verticeDictionary.ContainsKey(chunk.vertices[i])) {
                chunk.verticeDictionary.Add(chunk.vertices[i], i);
            }
        }
    }

    private void ShaderTriangulate(VoxelChunk chunk, out Vector3[] vertices, out int[] triangles, out Color32[] colors) {
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
        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        Triangle[] tris = new Triangle[numTris];
        triangleBuffer.GetData(tris, 0, 0, numTris);

        vertices = new Vector3[numTris * 3];
        triangles = new int[numTris * 3];
        colors = new Color32[numTris * 3];

        GetShaderData(numTris, tris, vertices, triangles, colors, chunk);
    }

    private void GetShaderData(int numTris, Triangle[] tris, Vector3[] vertices, int[] triangles, Color32[] colors, VoxelChunk chunk) {
        for (int i = 0; i < numTris; i++) {
            for (int j = 0; j < 3; j++) {
                colors[i * 3 + j] = new Color32((byte)(tris[i].red * 255), (byte)(tris[i].green * 255), (byte)(tris[i].blue * 255), 255);

                triangles[i * 3 + j] = i * 3 + j;

                var vertex = tris[i][j];
                vertex.x = vertex.x * chunkResolution;
                vertex.y = vertex.y * chunkResolution;

                vertices[i * 3 + j] = vertex;

                AddTriangleToDictionary(i * 3 + j, tris[i], chunk);
            }
        }
    }

    void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle, VoxelChunk chunk) {
        if (chunk.triangleDictionary.ContainsKey(chunk.vertices[vertexIndexKey])) {
            chunk.triangleDictionary[chunk.vertices[vertexIndexKey]].Add(triangle);
        } else {
            List<Triangle> triangleList = new List<Triangle>();
            triangleList.Add(triangle);
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

    private void OnDestroy() {
        if (Application.isPlaying) {
            ReleaseBuffers();
        }
    }
}
