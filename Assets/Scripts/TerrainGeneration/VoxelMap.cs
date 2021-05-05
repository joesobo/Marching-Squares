using System.Collections.Generic;
using UnityEngine;

public partial class VoxelMap : MonoBehaviour {
    public int regionResolution = 8;
    [Range(8, 56)] public int voxelResolution = 8;
    [HideInInspector] public int chunkResolution;
    [Range(1, 16)] public int viewDistance = 3;
    public float colliderRadius = 1;
    public bool useColliders;
    public bool useVoxelReferences;

    private VoxelEditor voxelEditor;
    private InfiniteGeneration infiniteGeneration;
    private WorldManager worldManager;
    [HideInInspector] public ChunkSaveLoadManager chunkSaveLoadManager;
    [HideInInspector] public Transform player;
    private Rigidbody2D playerRb;

    [HideInInspector] public List<VoxelChunk> chunks;
    public Dictionary<Vector2Int, VoxelChunk> existingChunks;
    public Queue<VoxelChunk> recycleableChunks;

    public WorldScriptableObject worldScriptableObject;

    private void Awake() {
        voxelEditor = FindObjectOfType<VoxelEditor>();
        infiniteGeneration = FindObjectOfType<InfiniteGeneration>();
        worldManager = FindObjectOfType<WorldManager>();
        chunkSaveLoadManager = FindObjectOfType<ChunkSaveLoadManager>();
        player = FindObjectOfType<PlayerController>().transform;
        BlockCollection blockList = BlockManager.ReadBlocks();

        chunkResolution = 16;

        recycleableChunks = new Queue<VoxelChunk>();

        worldScriptableObject.pathName = worldManager.worldPath + "/" + worldManager.worldName;
        worldScriptableObject.seed = worldManager.seed;

        FreshGeneration();
    }

    private void Update() {
        GenerateTerrain();
    }

    private void GenerateTerrain() {
        transform.parent.localScale = Vector3.one;

        infiniteGeneration.UpdateAroundPlayer();

        transform.parent.localScale = Vector3.one * voxelResolution;
    }

    // ReSharper disable Unity.PerformanceAnalysis
    public void FreshGeneration() {
        var oldChunks = FindObjectsOfType<VoxelChunk>();
        for (int i = oldChunks.Length - 1; i >= 0; i--) {
            Destroy(oldChunks[i].gameObject);
        }

        chunks = new List<VoxelChunk>();
        existingChunks = new Dictionary<Vector2Int, VoxelChunk>();
        voxelEditor.Startup(this);
        infiniteGeneration.StartUp(this, worldScriptableObject);
        chunkSaveLoadManager.Startup(infiniteGeneration, worldScriptableObject, regionResolution);

        GenerateTerrain();
    }

    private void OnDrawGizmosSelected() {
        if (player) {
            Gizmos.color = new Color(0, 0, 1, 0.25f);
            Gizmos.DrawSphere(player.position, voxelResolution * chunkResolution / 2f);
        }
    }
}