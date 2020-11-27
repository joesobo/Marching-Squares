using System;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class VoxelGrid : MonoBehaviour {

    public int resolution;
    public GameObject voxelPrefab;

    public VoxelGrid xNeighbor, yNeighbor, xyNeighbor;

    private Voxel[] voxels;
    private float voxelSize, gridSize;
    private Material[] voxelMaterials;
    private Voxel dummyX, dummyY, dummyT;

    private Mesh mesh;
    private List<Vector3> vertices;
    private List<int> triangles;

    private int[] rowCacheMin, rowCacheMax;
    private int edgeCacheMin, edgeCacheMax;

    public void Initialize(int resolution, float size) {
        this.resolution = resolution;
        voxelSize = size / resolution;
        gridSize = size;
        voxels = new Voxel[resolution * resolution];
        voxelMaterials = new Material[voxels.Length];

        dummyX = new Voxel();
        dummyY = new Voxel();
        dummyT = new Voxel();

        for (int i = 0, y = 0; y < resolution; y++) {
            for (int x = 0; x < resolution; x++, i++) {
                CreateVoxel(i, x, y);
            }
        }

        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "VoxelGrid Mesh";
        vertices = new List<Vector3>();
        triangles = new List<int>();

        rowCacheMax = new int[resolution * 2 + 1];
        rowCacheMin = new int[resolution * 2 + 1];

        Refresh();
    }

    public void Apply(VoxelStencil stencil) {
        int xStart = (int)(stencil.XStart / voxelSize);
        if (xStart < 0) {
            xStart = 0;
        }
        int xEnd = (int)(stencil.XEnd / voxelSize);
        if (xEnd >= resolution) {
            xEnd = resolution - 1;
        }
        int yStart = (int)(stencil.YStart / voxelSize);
        if (yStart < 0) {
            yStart = 0;
        }
        int yEnd = (int)(stencil.YEnd / voxelSize);
        if (yEnd >= resolution) {
            yEnd = resolution - 1;
        }

        for (int y = yStart; y <= yEnd; y++) {
            int i = y * resolution + xStart;
            for (int x = xStart; x <= xEnd; x++, i++) {
                stencil.Apply(voxels[i]);
            }
        }

        Refresh();
    }

    private void SetVoxelColors() {
        for (int i = 0; i < voxels.Length; i++) {
            voxelMaterials[i].color = voxels[i].state ? Color.black : Color.white;
        }
    }

    private void CreateVoxel(int i, int x, int y) {
        GameObject go = Instantiate(voxelPrefab);
        go.transform.parent = transform;
        go.transform.localPosition = new Vector3((x + 0.5f) * voxelSize, (y + 0.5f) * voxelSize, -0.01f);
        go.transform.localScale = Vector3.one * voxelSize * 0.1f;
        voxelMaterials[i] = go.GetComponent<MeshRenderer>().material;
        voxels[i] = new Voxel(x, y, voxelSize);
    }

    private void Refresh() {
        SetVoxelColors();
        Triangulate();
    }

    private void Triangulate() {
        vertices.Clear();
        triangles.Clear();
        mesh.Clear();

        FillFirstRowCache();
        TriangulateCellRows();
        if (yNeighbor != null) {
            TriangulateGapRow();
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
    }

    private void TriangulateCell(int i, Voxel a, Voxel b, Voxel c, Voxel d) {
        int cellType = 0;
        if (a.state) {
            cellType |= 1;
        }
        if (b.state) {
            cellType |= 2;
        }
        if (c.state) {
            cellType |= 4;
        }
        if (d.state) {
            cellType |= 8;
        }
        switch (cellType) {
            case 0:
                return;
            case 1:
                AddTriangle(rowCacheMin[i], edgeCacheMin, rowCacheMin[i + 1]);
                break;
            case 2:
                AddTriangle(rowCacheMin[i + 2], rowCacheMin[i + 1], edgeCacheMax);
                break;
            case 3:
                AddQuad(rowCacheMin[i], edgeCacheMin, edgeCacheMax, rowCacheMin[i + 2]);
                break;
            case 4:
                AddTriangle(rowCacheMax[i], rowCacheMax[i + 1], edgeCacheMin);
                break;
            case 5:
                AddQuad(rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 1], rowCacheMin[i + 1]);
                break;
            case 6:
                AddTriangle(rowCacheMin[i + 2], rowCacheMin[i + 1], edgeCacheMax);
                AddTriangle(rowCacheMax[i], rowCacheMax[i + 1], edgeCacheMin);
                break;
            case 7:
                AddPentagon(
                    rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 1], edgeCacheMax, rowCacheMin[i + 2]);
                break;
            case 8:
                AddTriangle(rowCacheMax[i + 2], edgeCacheMax, rowCacheMax[i + 1]);
                break;
            case 9:
                AddTriangle(rowCacheMin[i], edgeCacheMin, rowCacheMin[i + 1]);
                AddTriangle(rowCacheMax[i + 2], edgeCacheMax, rowCacheMax[i + 1]);
                break;
            case 10:
                AddQuad(rowCacheMin[i + 1], rowCacheMax[i + 1], rowCacheMax[i + 2], rowCacheMin[i + 2]);
                break;
            case 11:
                AddPentagon(
                    rowCacheMin[i + 2], rowCacheMin[i], edgeCacheMin, rowCacheMax[i + 1], rowCacheMax[i + 2]);
                break;
            case 12:
                AddQuad(edgeCacheMin, rowCacheMax[i], rowCacheMax[i + 2], edgeCacheMax);
                break;
            case 13:
                AddPentagon(
                    rowCacheMax[i], rowCacheMax[i + 2], edgeCacheMax, rowCacheMin[i + 1], rowCacheMin[i]);
                break;
            case 14:
                AddPentagon(
                    rowCacheMax[i + 2], rowCacheMin[i + 2], rowCacheMin[i + 1], edgeCacheMin, rowCacheMax[i]);
                break;
            case 15:
                AddQuad(rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 2], rowCacheMin[i + 2]);
                break;
        }
    }

    private void TriangulateGapCell(int i) {
        Voxel dummySwap = dummyT;
        dummySwap.BecomeXDummyOf(xNeighbor.voxels[i + 1], gridSize);
        dummyT = dummyX;
        dummyX = dummySwap;
        int cacheIndex = (resolution - 1) * 2;
        CacheNextEdgeAndCorner(cacheIndex, voxels[i + resolution], dummyX);
        CacheNextMiddleEdge(dummyT, dummyX);
        TriangulateCell(cacheIndex, voxels[i], dummyT, voxels[i + resolution], dummyX);
    }

    private void TriangulateCellRows() {
        int cells = resolution - 1;
        for (int i = 0, y = 0; y < cells; y++, i++) {
            SwapRowCaches();
            CacheFirstCorner(voxels[i + resolution]);
            CacheNextMiddleEdge(voxels[i], voxels[i + resolution]);

            for (int x = 0; x < cells; x++, i++) {
                Voxel
                    a = voxels[i],
                    b = voxels[i + 1],
                    c = voxels[i + resolution],
                    d = voxels[i + resolution + 1];
                int cacheIndex = x * 2;
                CacheNextEdgeAndCorner(cacheIndex, c, d);
                CacheNextMiddleEdge(b, d);
                TriangulateCell(cacheIndex, a, b, c, d);
            }
            if (xNeighbor != null) {
                TriangulateGapCell(i);
            }
        }
    }

    private void TriangulateGapRow() {
        dummyY.BecomeYDummyOf(yNeighbor.voxels[0], gridSize);
        int cells = resolution - 1;
        int offset = cells * resolution;
        SwapRowCaches();
        CacheFirstCorner(dummyY);
        CacheNextMiddleEdge(voxels[cells * resolution], dummyY);

        for (int x = 0; x < cells; x++) {
            Voxel dummySwap = dummyT;
            dummySwap.BecomeYDummyOf(yNeighbor.voxels[x + 1], gridSize);
            dummyT = dummyY;
            dummyY = dummySwap;
            int cacheIndex = x * 2;
            CacheNextEdgeAndCorner(cacheIndex, dummyT, dummyY);
            CacheNextMiddleEdge(voxels[x + offset + 1], dummyY);
            TriangulateCell(cacheIndex, voxels[x + offset], voxels[x + offset + 1], dummyT, dummyY);
        }

        if (xNeighbor != null) {
            dummyT.BecomeXYDummyOf(xyNeighbor.voxels[0], gridSize);
            int cacheIndex = cells * 2;
            CacheNextEdgeAndCorner(cacheIndex, dummyY, dummyT);
            CacheNextMiddleEdge(dummyX, dummyT);
            TriangulateCell(cacheIndex, voxels[voxels.Length - 1], dummyX, dummyY, dummyT);
        }
    }

    private void AddTriangle(int a, int b, int c) {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
    }

    private void AddQuad(int a, int b, int c, int d) {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
        triangles.Add(a);
        triangles.Add(c);
        triangles.Add(d);
    }

    private void AddPentagon(int a, int b, int c, int d, int e) {
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

    private void FillFirstRowCache() {
        CacheFirstCorner(voxels[0]);
        int i;
        for (i = 0; i < resolution - 1; i++) {
            CacheNextEdgeAndCorner(i * 2, voxels[i], voxels[i + 1]);
        }
        if (xNeighbor != null) {
            dummyX.BecomeXDummyOf(xNeighbor.voxels[0], gridSize);
            CacheNextEdgeAndCorner(i * 2, voxels[i], dummyX);
        }
    }

    private void CacheFirstCorner(Voxel voxel) {
        if (voxel.state) {
            rowCacheMax[0] = vertices.Count;
            vertices.Add(voxel.position);
        }
    }

    private void CacheNextEdgeAndCorner(int i, Voxel xMin, Voxel xMax) {
        if (xMin.state != xMax.state) {
            rowCacheMax[i + 1] = vertices.Count;
            Vector3 p;
            p.x = xMin.xEdge;
            p.y = xMin.position.y;
            p.z = 0f;
            vertices.Add(p);
        }
        if (xMax.state) {
            rowCacheMax[i + 2] = vertices.Count;
            vertices.Add(xMax.position);
        }
    }

    private void CacheNextMiddleEdge(Voxel yMin, Voxel yMax) {
        edgeCacheMin = edgeCacheMax;
        if (yMin.state != yMax.state) {
            edgeCacheMax = vertices.Count;
            Vector3 p;
            p.x = yMin.position.x;
            p.y = yMin.yEdge;
            p.z = 0f;
            vertices.Add(p);
        }
    }

    private void SwapRowCaches() {
        int[] rowSwap = rowCacheMin;
        rowCacheMin = rowCacheMax;
        rowCacheMax = rowSwap;
    }
}