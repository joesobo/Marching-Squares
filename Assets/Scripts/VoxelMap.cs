using System.Collections.Generic;
using UnityEngine;

public partial class VoxelMap : MonoBehaviour {
    [Range(8, 56)]
    public int voxelResolution = 8;
    [Range(1, 16)]
    public int chunkResolution = 2;
    public ComputeShader shader;
    public bool useColliders = false;

    private VoxelMesh voxelMesh;
    private VoxelEditor voxelEditor;

    private List<VoxelChunk> chunks;

    private float voxelSize, halfSize;

    private void Awake() {
        voxelMesh = FindObjectOfType<VoxelMesh>();
        voxelEditor = FindObjectOfType<VoxelEditor>();

        VoxelChunk[] oldChunks = FindObjectsOfType<VoxelChunk>();
        for (int i = oldChunks.Length - 1; i >= 0; i--) {
            Destroy(oldChunks[i].gameObject);
        }

        chunks = new List<VoxelChunk>();
        voxelMesh.Startup(voxelResolution, chunkResolution, useColliders);
        voxelEditor.Startup(voxelResolution, chunkResolution, chunks, this);
        
        voxelMesh.PreloadChunks(chunks);

        GenerateTerrain();
    }

    private void Update() {
        GenerateTerrain();
    }

    public void GenerateTerrain() {
        halfSize = 0.5f * chunkResolution;
        voxelSize = 1f / voxelResolution;

        transform.parent.localScale = Vector3.one;

        voxelMesh.TriangulateChunks(chunks);

        transform.parent.localScale = Vector3.one * voxelResolution;
    }
}