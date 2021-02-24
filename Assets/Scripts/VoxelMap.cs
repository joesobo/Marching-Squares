using System.Collections.Generic;
using UnityEngine;

public partial class VoxelMap : MonoBehaviour {
    [Range(8, 56)]
    public int voxelResolution = 8;
    private int chunkResolution;
    [Range(1, 16)]
    public int viewDistance = 3;
    public ComputeShader shader;
    public bool useColliders = false;

    private VoxelMesh voxelMesh;
    private VoxelEditor voxelEditor;

    private float voxelSize, halfSize;

    private List<VoxelChunk> chunks;
    private Dictionary<Vector2Int, VoxelChunk> existingChunks;

    private Transform player;

    private void Awake() {
        voxelMesh = FindObjectOfType<VoxelMesh>();
        voxelEditor = FindObjectOfType<VoxelEditor>();

        VoxelChunk[] oldChunks = FindObjectsOfType<VoxelChunk>();
        for (int i = oldChunks.Length - 1; i >= 0; i--) {
            Destroy(oldChunks[i].gameObject);
        }

        chunkResolution = viewDistance * 4;
        player = FindObjectOfType<PlayerController>().transform;

        chunks = new List<VoxelChunk>();
        existingChunks = new Dictionary<Vector2Int, VoxelChunk>();
        voxelMesh.Startup(voxelResolution, chunkResolution, viewDistance, existingChunks, useColliders);
        voxelEditor.Startup(voxelResolution, chunkResolution, viewDistance, existingChunks, chunks, this);

        halfSize = 0.5f * chunkResolution;
        voxelSize = 1f / voxelResolution;

        GenerateTerrain();
    }

    private void Update() {
        GenerateTerrain();
    }

    public void GenerateTerrain() {
        transform.parent.localScale = Vector3.one;

        voxelMesh.TriangulateChunks(chunks);

        transform.parent.localScale = Vector3.one * voxelResolution;
    }

    private void OnDrawGizmosSelected() {
        if (player) {
            Gizmos.color = new Color(0, 0, 1, 0.25f);
            Gizmos.DrawSphere(player.position, voxelResolution * chunkResolution / 2);
        }
    }
}