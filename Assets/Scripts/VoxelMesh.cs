using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class VoxelMesh : MonoBehaviour {
    const int threadSize = 8;

    private int voxelResolution, chunkResolution;

    private int[] statePositions;

    public ComputeShader shader;

    ComputeBuffer verticeBuffer;
    ComputeBuffer triangleBuffer;
    ComputeBuffer triCountBuffer;
    ComputeBuffer stateBuffer;

    public void Startup(int voxelResolution, int chunkResolution) {
        this.voxelResolution = voxelResolution;
        this.chunkResolution = chunkResolution;

        statePositions = new int[(voxelResolution + 1) * (voxelResolution + 1)];
    }

    public void TriangulateChunks(VoxelChunk[] chunks) {
        CreateBuffers();

        foreach (VoxelChunk chunk in chunks) {
            TriangulateChunkMesh(chunk);
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
        Mesh mesh = chunk.mesh;
        chunk.ResetValues();

        ShaderTriangulate(chunk, out chunk.vertices, out chunk.triangles, out chunk.colors);

        Generate2DCollider(chunk);

        mesh.vertices = chunk.vertices;
        mesh.triangles = chunk.triangles;
        mesh.colors32 = chunk.colors;
        mesh.RecalculateNormals();
    }

    private void ShaderTriangulate(VoxelChunk chunk, out Vector3[] vertices, out int[] triangles, out Color32[] colors) {
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



    // Refactor to its own file
    private void Generate2DCollider(VoxelChunk chunk) {
        EdgeCollider2D[] currentColliders = chunk.gameObject.GetComponents<EdgeCollider2D>();
        for (int i = 0; i < currentColliders.Length; i++) {
            Destroy(currentColliders[i]);
        }

        CalculateMeshOutlines(chunk);

        foreach (List<int> outline in chunk.outlines) {
            EdgeCollider2D edgeCollider = chunk.gameObject.AddComponent<EdgeCollider2D>();
            Vector2[] edgePoints = new Vector2[outline.Count];

            for (int i = 0; i < outline.Count; i++) {
                edgePoints[i] = new Vector2(chunk.vertices[outline[i]].x, chunk.vertices[outline[i]].y);
            }
            edgeCollider.points = edgePoints;
        }
    }

    private void CalculateMeshOutlines(VoxelChunk chunk) {
        for (int vertexIndex = 0; vertexIndex < chunk.vertices.Length; vertexIndex++) {
            if (!chunk.checkedVertices.Contains(vertexIndex)) {
                int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex, chunk);
                if (newOutlineVertex != -1) {
                    chunk.checkedVertices.Add(vertexIndex);

                    List<int> newOutline = new List<int>();
                    newOutline.Add(vertexIndex);
                    chunk.outlines.Add(newOutline);
                    FollowOutline(newOutlineVertex, chunk);
                    chunk.outlines[chunk.outlines.Count - 1].Add(vertexIndex);
                }
            }
        }
    }

    private void FollowOutline(int vertexIndex, VoxelChunk chunk) {
        int outlineIndex = chunk.outlines.Count - 1;

        chunk.outlines[outlineIndex].Add(vertexIndex);
        chunk.checkedVertices.Add(vertexIndex);
        int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex, chunk);

        if (nextVertexIndex != -1) {
            FollowOutline(nextVertexIndex, chunk);
        }
    }

    private int GetConnectedOutlineVertex(int vertexIndex, VoxelChunk chunk) {
        List<Triangle> trianglesContainingVertex = chunk.triangleDictionary[chunk.vertices[vertexIndex]];

        foreach (Triangle triangle in trianglesContainingVertex) {
            for (int i = 0; i < 3; i++) {
                Vector2 chunkVertice = new Vector2(chunk.vertices[vertexIndex].x, chunk.vertices[vertexIndex].y);

                if (chunkVertice == triangle[i]) {
                    int nextVertexIndex = System.Array.IndexOf(chunk.vertices, triangle[(i + 1) % 3]);
                    if (!chunk.checkedVertices.Contains(nextVertexIndex) && IsOutlineEdge(vertexIndex, nextVertexIndex, chunk)) {
                        return nextVertexIndex;
                    }
                }
            }
        }

        return -1;
    }

    private bool IsOutlineEdge(int vertexA, int vertexB, VoxelChunk chunk) {
        List<Triangle> trianglesContainingVertexA = chunk.triangleDictionary[chunk.vertices[vertexA]];
        int sharedTriangleCount = 0;

        for (int i = 0; i < trianglesContainingVertexA.Count; i++) {
            Vector2 chunkVertice = new Vector2(chunk.vertices[vertexB].x, chunk.vertices[vertexB].y);
            if (trianglesContainingVertexA[i].a == chunkVertice || trianglesContainingVertexA[i].b == chunkVertice || trianglesContainingVertexA[i].c == chunkVertice) {
                sharedTriangleCount++;
                if (sharedTriangleCount > 1) {
                    break;
                }
            }
        }
        return sharedTriangleCount == 1;
    }
}
