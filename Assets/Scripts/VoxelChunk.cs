﻿using UnityEngine;
using System.Collections.Generic;
using System;

[SelectionBase]
public class VoxelChunk : MonoBehaviour {
    public int resolution;
    public bool useVoxelPoints;
    public GameObject voxelPointPrefab;
    [HideInInspector]
    public VoxelChunk xNeighbor, yNeighbor, xyNeighbor;
    private ComputeShader shader;

    public Voxel[] voxels;
    private float voxelSize, gridSize;
    private List<Material> voxelMaterials = new List<Material>();
    public Mesh mesh;
    public Vector3[] vertices;
    public int[] triangles;
    public Color32[] colors;
    public HashSet<int> checkedVertices;
    public List<List<int>> outlines;
    public Dictionary<Vector2, List<Triangle>> triangleDictionary;

    public void Initialize(bool useVoxelPoints, int resolution) {
        this.useVoxelPoints = useVoxelPoints;
        this.resolution = resolution;

        gridSize = 1f;
        voxelSize = 1f / resolution;
        voxels = new Voxel[resolution * resolution];

        for (int i = 0, y = 0; y < resolution; y++) {
            for (int x = 0; x < resolution; x++, i++) {
                CreateVoxelPoint(i, x, y);
            }
        }

        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "VoxelChunk Mesh";

        ResetValues();

        Refresh();
    }

    private void CreateVoxelPoint(int i, int x, int y) {
        if (useVoxelPoints) {
            GameObject o = Instantiate(voxelPointPrefab) as GameObject;
            o.transform.parent = transform;
            o.transform.localPosition = new Vector3((x + 0.5f) * voxelSize, (y + 0.5f) * voxelSize, -0.01f);
            o.transform.localScale = Vector3.one * voxelSize * 0.1f;
            voxelMaterials.Add(o.GetComponent<MeshRenderer>().material);
        }
        voxels[i] = new Voxel(x, y, voxelSize);
    }

    private void Refresh() {
        SetVoxelColors();
    }

    private void SetVoxelColors() {
        if (voxelMaterials.Count > 0) {
            for (int i = 0; i < voxels.Length; i++) {
                voxelMaterials[i].color = voxels[i].state == 0 ? Color.black : Color.white;
            }
        }
    }

    public void Apply(VoxelStencil stencil) {
        int xStart = stencil.XStart;
        if (xStart < 0) {
            xStart = 0;
        }
        int xEnd = stencil.XEnd;
        if (xEnd >= resolution) {
            xEnd = resolution - 1;
        }
        int yStart = stencil.YStart;
        if (yStart < 0) {
            yStart = 0;
        }
        int yEnd = stencil.YEnd;
        if (yEnd >= resolution) {
            yEnd = resolution - 1;
        }

        for (int y = yStart; y <= yEnd; y++) {
            int i = y * resolution + xStart;
            for (int x = xStart; x <= xEnd; x++, i++) {
                voxels[i].state = stencil.Apply(x, y, voxels[i].state);
            }
        }
        Refresh();
    }

    public void ResetValues() {
        mesh.Clear();
        checkedVertices = new HashSet<int>();
        outlines = new List<List<int>>();
        triangleDictionary = new Dictionary<Vector2, List<Triangle>>();
    }
}