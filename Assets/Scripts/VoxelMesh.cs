using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VoxelMesh : MonoBehaviour {
    private const int THREADS = 8;

    public ComputeShader shader;

    private int voxelResolution, chunkResolution, textureTileAmount;
    private bool useColliders;
    private int[] statePositions;
    private float viewDistance, colliderRadius;

    private Vector2[] uvs;

    private VoxelChunk testChunk, currentChunk;

    private Mesh mesh;

    private ComputeBuffer verticeBuffer, triangleBuffer, triCountBuffer, stateBuffer;

    public void Startup(int voxelResolution, int chunkResolution, float viewDistance,
        bool useColliders, float colliderRadius) {
        this.voxelResolution = voxelResolution;
        this.chunkResolution = chunkResolution;
        this.viewDistance = viewDistance;
        this.useColliders = useColliders;
        this.colliderRadius = colliderRadius;

        textureTileAmount = (voxelResolution * chunkResolution) / 2;

        statePositions = new int[(voxelResolution + 1) * (voxelResolution + 1)];
    }

    public void CreateBuffers() {
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