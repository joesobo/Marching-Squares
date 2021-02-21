using System.Collections.Generic;
using UnityEngine;

public class VoxelMesh : MonoBehaviour {
    const int THREADS = 8;

    public ComputeShader shader;
    public float viewDistance = 3;

    public VoxelChunk voxelChunkPrefab;
    public bool useVoxelReferences = false;

    private int voxelResolution, chunkResolution;
    private bool useColliders;
    private int textureTileAmount;
    private int[] statePositions;

    private ChunkCollider chunkCollider;
    private Transform player;

    ComputeBuffer verticeBuffer;
    ComputeBuffer triangleBuffer;
    ComputeBuffer triCountBuffer;
    ComputeBuffer stateBuffer;

    private Queue<VoxelChunk> recycleableChunks;
    private Dictionary<Vector2Int, VoxelChunk> existingChunks;

    private TerrainNoise terrainNoise;

    public void Startup(int voxelResolution, int chunkResolution, bool useColliders) {
        this.voxelResolution = voxelResolution;
        this.chunkResolution = chunkResolution;
        this.useColliders = useColliders;

        textureTileAmount = (voxelResolution * chunkResolution) / 2;

        statePositions = new int[(voxelResolution + 1) * (voxelResolution + 1)];

        chunkCollider = FindObjectOfType<ChunkCollider>();
        player = FindObjectOfType<PlayerController>().transform;

        recycleableChunks = new Queue<VoxelChunk>();
        existingChunks = new Dictionary<Vector2Int, VoxelChunk>();

        terrainNoise = FindObjectOfType<TerrainNoise>();
        terrainNoise.Startup(voxelResolution, chunkResolution, player);
    }

    public void TriangulateChunks(List<VoxelChunk> chunks) {
        CreateBuffers();

        Vector2 p = player.position / voxelResolution;
        Vector2Int playerCoord = new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));

        float sqrViewDist = viewDistance * viewDistance;

        for (int i = chunks.Count - 1; i >= 0; i--) {
            VoxelChunk chunk = chunks[i];
            Vector2Int chunkPos = new Vector2Int(Mathf.RoundToInt(chunk.transform.position.x), Mathf.RoundToInt(chunk.transform.position.y));
            Vector2 playerOffset = p - chunkPos;
            Vector2 o = new Vector2(Mathf.Abs(playerOffset.x), Mathf.Abs(playerOffset.y)) - (Vector2.one * viewDistance) / 2;
            float sqrDst = new Vector2(Mathf.Max(o.x, 0), Mathf.Max(o.y, 0)).sqrMagnitude;

            if (sqrDst > sqrViewDist) {
                existingChunks.Remove(chunkPos);
                recycleableChunks.Enqueue(chunk);
                chunks.RemoveAt(i);
            }
        }

        for (int y = -chunkResolution / 2, i = 0; y < chunkResolution / 2; y++) {
            for (int x = -chunkResolution / 2; x < chunkResolution / 2; x++, i++) {
                Vector2Int coord = new Vector2Int(x, y) + playerCoord;

                if (existingChunks.ContainsKey(coord)) {
                    continue;
                }

                Vector2 playerOffset = p - coord;
                Vector2 o = new Vector2(Mathf.Abs(playerOffset.x), Mathf.Abs(playerOffset.y)) - (Vector2.one * viewDistance) / 2;
                float sqrDst = o.sqrMagnitude;

                if (sqrDst <= sqrViewDist) {
                    if (recycleableChunks.Count > 0) {
                        VoxelChunk recycleChunk = recycleableChunks.Dequeue();
                        recycleChunk.transform.position = new Vector3(coord.x, coord.y);
                        existingChunks.Add(coord, recycleChunk);
                        chunks.Add(recycleChunk);
                        terrainNoise.GenerateNoise(recycleChunk);
                        TriangulateChunkMesh(recycleChunk);
                    } else {
                        VoxelChunk newChunk = CreateChunk(i, x, y, chunks);
                        existingChunks.Add(coord, newChunk);
                        chunks.Add(newChunk);
                        terrainNoise.GenerateNoise(newChunk);
                        TriangulateChunkMesh(newChunk);
                    }
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

        if (x > 0) {
            chunks[i - 1].xNeighbor = chunk;
        }
        if (y > 0) {
            chunks[i - chunkResolution].yNeighbor = chunk;
            if (x > 0) {
                chunks[i - chunkResolution - 1].xyNeighbor = chunk;
            }
        }

        return chunk;
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
        Mesh mesh = chunk.mesh;
        chunk.ResetValues();

        ShaderTriangulate(chunk, out chunk.vertices, out chunk.triangles, out chunk.colors);

        if (useColliders) {
            chunkCollider.Generate2DCollider(chunk, chunkResolution);
        }

        Vector2[] uvs = GetUVs(chunk);

        mesh.vertices = chunk.vertices;
        mesh.triangles = chunk.triangles;
        mesh.uv = uvs;
        mesh.colors32 = chunk.colors;
        mesh.RecalculateNormals();
    }

    private Vector2[] GetUVs(VoxelChunk chunk) {
        Vector2[] uvs = new Vector2[chunk.vertices.Length];
        for (int i = 0; i < chunk.vertices.Length; i++) {
            float percentX = Mathf.InverseLerp(0, chunkResolution * voxelResolution, chunk.vertices[i].x) * textureTileAmount;
            float percentY = Mathf.InverseLerp(0, chunkResolution * voxelResolution, chunk.vertices[i].y) * textureTileAmount;
            uvs[i] = new Vector2(percentX, percentY);
        }

        return uvs;
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

    public void PreloadChunks(List<VoxelChunk> chunks) {
        for (int y = -chunkResolution / 2, i = 0; y < chunkResolution / 2; y++) {
            for (int x = -chunkResolution / 2; x < chunkResolution / 2; x++, i++) {
                chunks.Add(CreateChunk(i, x, y, chunks));
            }
        }
    }
}
