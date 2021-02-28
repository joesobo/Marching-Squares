using UnityEngine;
using System.Collections.Generic;
using System;

[SelectionBase]
public class VoxelChunk : MonoBehaviour {
    public int resolution;
    public bool useVoxelPoints;
    public bool shouldUpdateMesh = false;
    public bool shouldUpdateCollider = false;
    public GameObject voxelPointPrefab;
    [HideInInspector]
    public VoxelChunk xNeighbor, yNeighbor, xyNeighbor;
    private ComputeShader shader;

    public Voxel[] voxels;
    private float voxelSize, halfSize;
    private List<Material> voxelMaterials = new List<Material>();
    public Mesh mesh;
    public Material material;
    public Vector3[] vertices;
    public int[] triangles;
    public Color32[] colors;
    public HashSet<int> checkedVertices;
    public List<List<int>> outlines;
    public Dictionary<Vector2, List<Triangle>> triangleDictionary;

    public void Initialize(bool useVoxelPoints, int resolution) {
        this.useVoxelPoints = useVoxelPoints;
        this.resolution = resolution;

        voxelSize = 1f / resolution;
        halfSize = 0.5f * resolution;
        voxels = new Voxel[resolution * resolution];

        for (int i = 0, y = 0; y < resolution; y++) {
            for (int x = 0; x < resolution; x++, i++) {
                CreateVoxelPoint(i, x, y);
            }
        }

        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        material.SetVector("Resolution", Vector2.one * resolution);
        GetComponent<MeshRenderer>().material = material;
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

    public bool Apply(VoxelStencil stencil) {
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

        bool didUpdate = false;
        int i;
        for (int y = yStart; y <= yEnd; y++) {
            i = y * resolution + xStart;
            for (int x = xStart; x <= xEnd; x++, i++) {
                if (voxels[i].state != stencil.fillType) {
                    voxels[i].state = stencil.Apply(x, y, voxels[i].state);
                    didUpdate = true;
                }
            }
        }
        Refresh();
        return didUpdate;
    }

    public void ResetValues() {
        mesh.Clear();
        checkedVertices = new HashSet<int>();
        outlines = new List<List<int>>();
        triangleDictionary = new Dictionary<Vector2, List<Triangle>>();
    }

    public void SetNewChunk(int x, int y) {
        xNeighbor = null;
        yNeighbor = null;
        xyNeighbor = null;
        transform.position = new Vector3(x, y);
        shouldUpdateMesh = true;
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(new Vector3(transform.position.x + halfSize, transform.position.y + halfSize), Vector3.one * resolution);
    }
}