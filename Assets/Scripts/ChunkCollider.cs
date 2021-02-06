using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkCollider : MonoBehaviour
{
    public void Generate2DCollider(VoxelChunk chunk) {
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
