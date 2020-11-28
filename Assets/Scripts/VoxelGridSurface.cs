using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelGridSurface : MonoBehaviour {
    private Mesh mesh;

    private List<Vector3> vertices;
    private List<int> triangles;

    private int[] cornersMin, cornersMax;
    private int[] xEdgesMin, xEdgesMax;
    private int yEdgeMin, yEdgeMax;

    public void Initialize(int resolution) {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "VoxelGridSurface Mesh";
        vertices = new List<Vector3>();
        triangles = new List<int>();
        cornersMin = new int[resolution + 1];
        cornersMax = new int[resolution + 1];
        xEdgesMin = new int[resolution];
        xEdgesMax = new int[resolution];
    }

    public void Clear() {
        vertices.Clear();
        triangles.Clear();
        mesh.Clear();
    }

    public void Apply() {
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
    }

    // Caching
    public void CacheFirstCorner(Voxel voxel) {
        cornersMax[0] = vertices.Count;
        vertices.Add(voxel.position);
    }

    public void CacheNextCorner(int i, Voxel voxel) {
        cornersMax[i + 1] = vertices.Count;
        vertices.Add(voxel.position);
    }

    public void CacheXEdge(int i, Voxel voxel) {
        xEdgesMin[i] = vertices.Count;
        vertices.Add(voxel.XEdgePoint);
    }

    public void CacheYEdge(Voxel voxel) {
        yEdgeMax = vertices.Count;
        vertices.Add(voxel.YEdgePoint);
    }

    public void PrepareCacheForNextCell() {
        yEdgeMin = yEdgeMax;
    }

    public void PrepareCacheForNextRow() {
        int[] rowSwap = cornersMin;
        cornersMin = cornersMax;
        cornersMax = rowSwap;

        rowSwap = xEdgesMax;
        xEdgesMax = xEdgesMin;
        xEdgesMin = rowSwap;
    }

    // Basic Shapes
    public void AddTriangle(int a, int b, int c) {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
    }

    public void AddQuad(int a, int b, int c, int d) {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
        triangles.Add(a);
        triangles.Add(c);
        triangles.Add(d);
    }

    public void AddPentagon(int a, int b, int c, int d, int e) {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
        triangles.Add(a);
        triangles.Add(c);
        triangles.Add(d);
        triangles.Add(a);
        triangles.Add(d);
        triangles.Add(e);
    }

    public void AddHexagon(int a, int b, int c, int d, int e, int f) {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
        triangles.Add(a);
        triangles.Add(c);
        triangles.Add(d);
        triangles.Add(a);
        triangles.Add(d);
        triangles.Add(e);
        triangles.Add(a);
        triangles.Add(e);
        triangles.Add(f);
    }

    // Add Shapes
    public void AddTriangleA(int i) {
        AddTriangle(cornersMin[i], yEdgeMin, xEdgesMax[i]);
    }

    public void AddTriangleB(int i) {
        AddTriangle(cornersMin[i + 1], xEdgesMax[i], yEdgeMax);
    }

    public void AddTriangleC(int i) {
        AddTriangle(cornersMax[i], xEdgesMin[i], yEdgeMin);
    }

    public void AddTriangleD(int i) {
        AddTriangle(cornersMax[i + 1], yEdgeMax, xEdgesMin[i]);
    }

    public void AddQuadA(int i, Vector2 extraVertex) {
        AddQuad(vertices.Count, xEdgesMax[i], cornersMin[i], yEdgeMin);
        vertices.Add(extraVertex);
    }

    public void AddQuadB(int i, Vector2 extraVertex) {
        AddQuad(vertices.Count, yEdgeMax, cornersMin[i + 1], xEdgesMax[i]);
        vertices.Add(extraVertex);
    }

    public void AddQuadC(int i, Vector2 extraVertex) {
        AddQuad(vertices.Count, yEdgeMin, cornersMax[i], xEdgesMin[i]);
        vertices.Add(extraVertex);
    }

    public void AddQuadD(int i, Vector2 extraVertex) {
        AddQuad(vertices.Count, xEdgesMin[i], cornersMax[i + 1], yEdgeMax);
        vertices.Add(extraVertex);
    }

    public void AddQuadAB(int i) {
        AddQuad(cornersMin[i], yEdgeMin, yEdgeMax, cornersMin[i + 1]);
    }

    public void AddQuadAC(int i) {
        AddQuad(cornersMin[i], cornersMax[i], xEdgesMin[i], xEdgesMax[i]);
    }

    public void AddQuadBD(int i) {
        AddQuad(xEdgesMax[i], xEdgesMin[i], cornersMax[i + 1], cornersMin[i + 1]);
    }

    public void AddQuadCD(int i) {
        AddQuad(yEdgeMin, cornersMax[i], cornersMax[i + 1], yEdgeMax);
    }

    public void AddQuadABCD(int i) {
        AddQuad(cornersMin[i], cornersMax[i], cornersMax[i + 1], cornersMin[i + 1]);
    }

    public void AddQuadBCToA(int i) {
        AddQuad(yEdgeMin, cornersMax[i], cornersMin[i + 1], xEdgesMax[i]);
    }

    public void AddQuadBCToD(int i) {
        AddQuad(yEdgeMax, cornersMin[i + 1], cornersMax[i], xEdgesMin[i]);
    }

    public void AddQuadADToB(int i) {
        AddQuad(xEdgesMax[i], cornersMin[i], cornersMax[i + 1], yEdgeMax);
    }

    public void AddQuadADToC(int i) {
        AddQuad(xEdgesMin[i], cornersMax[i + 1], cornersMin[i], yEdgeMin);
    }

    public void AddPentagonACD(int i) {
        AddPentagon(cornersMax[i], cornersMax[i + 1], yEdgeMax, xEdgesMax[i], cornersMin[i]);
    }

    public void AddPentagonABD(int i) {
        AddPentagon(cornersMin[i + 1], cornersMin[i], yEdgeMin, xEdgesMin[i], cornersMax[i + 1]);
    }

    public void AddPentagonBCD(int i) {
        AddPentagon(cornersMax[i + 1], cornersMin[i + 1], xEdgesMax[i], yEdgeMin, cornersMax[i]);
    }

    public void AddPentagonAB(int i, Vector2 extraVertex) {
        AddPentagon(vertices.Count, yEdgeMax, cornersMin[i + 1], cornersMin[i], yEdgeMin);
        vertices.Add(extraVertex);
    }

    public void AddPentagonAC(int i, Vector2 extraVertex) {
        AddPentagon(vertices.Count, xEdgesMax[i], cornersMin[i], cornersMax[i], xEdgesMin[i]);
        vertices.Add(extraVertex);
    }

    public void AddPentagonBD(int i, Vector2 extraVertex) {
        AddPentagon(vertices.Count, xEdgesMin[i], cornersMax[i + 1], cornersMin[i + 1], xEdgesMax[i]);
        vertices.Add(extraVertex);
    }

    public void AddPentagonCD(int i, Vector2 extraVertex) {
        AddPentagon(vertices.Count, yEdgeMin, cornersMax[i], cornersMax[i + 1], yEdgeMax);
        vertices.Add(extraVertex);
    }

    public void AddPentagonABC(int i) {
        AddPentagon(cornersMin[i], cornersMax[i], xEdgesMin[i], yEdgeMax, cornersMin[i + 1]);
    }

    public void AddPentagonBCToA(int i, Vector2 extraVertex) {
        AddPentagon(vertices.Count, yEdgeMin, cornersMax[i], cornersMin[i + 1], xEdgesMax[i]);
        vertices.Add(extraVertex);
    }

    public void AddPentagonBCToD(int i, Vector2 extraVertex) {
        AddPentagon(vertices.Count, yEdgeMax, cornersMin[i + 1], cornersMax[i], xEdgesMin[i]);
        vertices.Add(extraVertex);
    }

    public void AddPentagonADToB(int i, Vector2 extraVertex) {
        AddPentagon(vertices.Count, xEdgesMax[i], cornersMin[i], cornersMax[i + 1], yEdgeMax);
        vertices.Add(extraVertex);
    }

    public void AddPentagonADToC(int i, Vector2 extraVertex) {
        AddPentagon(vertices.Count, xEdgesMin[i], cornersMax[i + 1], cornersMin[i], yEdgeMin);
        vertices.Add(extraVertex);
    }

    public void AddHexagonABC(int i, Vector2 extraVertex) {
        AddHexagon(
            vertices.Count, yEdgeMax, cornersMin[i + 1],
            cornersMin[i], cornersMax[i], xEdgesMin[i]);
        vertices.Add(extraVertex);
    }

    public void AddHexagonABD(int i, Vector2 extraVertex) {
        AddHexagon(
            vertices.Count, xEdgesMin[i], cornersMax[i + 1],
            cornersMin[i + 1], cornersMin[i], yEdgeMin);
        vertices.Add(extraVertex);
    }

    public void AddHexagonACD(int i, Vector2 extraVertex) {
        AddHexagon(
            vertices.Count, xEdgesMax[i], cornersMin[i],
            cornersMax[i], cornersMax[i + 1], yEdgeMax);
        vertices.Add(extraVertex);
    }

    public void AddHexagonBCD(int i, Vector2 extraVertex) {
        AddHexagon(
            vertices.Count, yEdgeMin, cornersMax[i],
            cornersMax[i + 1], cornersMin[i + 1], xEdgesMax[i]);
        vertices.Add(extraVertex);
    }
}
