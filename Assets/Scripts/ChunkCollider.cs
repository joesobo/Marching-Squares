using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkCollider : MonoBehaviour {
    private float chunkResolution;
    private EdgeCollider2D[] currentColliders;
    private EdgeCollider2D currentCollider;
    private Vector2[] edgePoints;

    public void Generate2DCollider(VoxelChunk chunk, int chunkResolution) {
        this.chunkResolution = (float)chunkResolution;

        currentColliders = chunk.gameObject.GetComponents<EdgeCollider2D>();
        for (int i = 0; i < currentColliders.Length; i++) {
            Destroy(currentColliders[i]);
        }

        CalculateMeshOutlines(chunk);

        foreach (List<int> outline in chunk.outlines) {
            currentCollider = chunk.gameObject.AddComponent<EdgeCollider2D>();
            edgePoints = new Vector2[outline.Count];

            for (int i = 0; i < outline.Count; i++) {
                edgePoints[i] = new Vector2(chunk.vertices[outline[i]].x, chunk.vertices[outline[i]].y);
            }
            currentCollider.points = edgePoints;
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
        chunk.outlines[chunk.outlines.Count - 1].Add(vertexIndex);
        chunk.checkedVertices.Add(vertexIndex);
        int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex, chunk);

        if (nextVertexIndex != -1) {
            FollowOutline(nextVertexIndex, chunk);
        }
    }

    private int GetConnectedOutlineVertex(int vertexIndex, VoxelChunk chunk) {
        foreach (Triangle triangle in chunk.triangleDictionary[chunk.vertices[vertexIndex]]) {
            for (int i = 0; i < 3; i++) {
                if (triangle[i].x == chunk.vertices[vertexIndex].x / chunkResolution && triangle[i].y == chunk.vertices[vertexIndex].y / chunkResolution) {
                    Vector3 findVertice = triangle[(i + 1) % 3] * chunkResolution;
                    int nextVertexIndex = chunk.verticeDictionary[findVertice];
                    // int nextVertexIndex = System.Array.IndexOf(chunk.vertices, findVertice);
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
        Vector2 chunkVertice = new Vector2(chunk.vertices[vertexB].x, chunk.vertices[vertexB].y);

        for (int i = 0; i < trianglesContainingVertexA.Count; i++) {
            if (trianglesContainingVertexA[i].a * chunkResolution == chunkVertice ||
                trianglesContainingVertexA[i].b * chunkResolution == chunkVertice ||
                trianglesContainingVertexA[i].c * chunkResolution == chunkVertice
            ) {
                sharedTriangleCount++;
                if (sharedTriangleCount > 1) {
                    break;
                }
            }
        }
        return sharedTriangleCount == 1;
    }
}
