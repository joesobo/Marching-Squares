using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class VoxelMap : MonoBehaviour {
    [Range(8, 56)] public int voxelResolution = 8;
    private int chunkResolution;
    [Range(1, 16)] public int viewDistance = 3;
    public float colliderRadius = 1;
    public ComputeShader shader;
    public bool useColliders = false;
    public VoxelChunk voxelChunkPrefab;
    public bool useVoxelReferences = false;

    private VoxelMesh voxelMesh;
    private VoxelEditor voxelEditor;
    private TerrainNoise terrainNoise;
    private ChunkCollider chunkCollider;

    private float voxelSize, halfSize;

    private List<VoxelChunk> chunks;
    private Dictionary<Vector2Int, VoxelChunk> existingChunks;
    
    private Vector2 p, playerOffset, offset;
    private Vector2Int playerCoord, testChunkPos, coord;
    private float sqrViewDist, sqrDst;
    private Queue<VoxelChunk> recycleableChunks;

    private Transform player;

    private void Awake() {
        voxelMesh = FindObjectOfType<VoxelMesh>();
        voxelEditor = FindObjectOfType<VoxelEditor>();
        terrainNoise = FindObjectOfType<TerrainNoise>();
        chunkCollider = FindObjectOfType<ChunkCollider>();
        player = FindObjectOfType<PlayerController>().transform;

        chunkResolution = viewDistance * 4;
        halfSize = 0.5f * chunkResolution;
        voxelSize = 1f / voxelResolution;
        
        recycleableChunks = new Queue<VoxelChunk>();

        FreshGeneration();
    }

    private void Update() {
        GenerateTerrain();
    }

    private void GenerateTerrain() {
        transform.parent.localScale = Vector3.one;
        
        p = player.position / voxelResolution;
        playerCoord = new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));

        voxelMesh.CreateBuffers();

        RemoveOutOfBoundsChunks();

        CreateNewChunksInRange();

        UpdateNewChunks();

        RecreateUpdatedChunkMeshes();

        transform.parent.localScale = Vector3.one * voxelResolution;
    }

    public void FreshGeneration() {
        var oldChunks = FindObjectsOfType<VoxelChunk>();
        for (int i = oldChunks.Length - 1; i >= 0; i--) {
            Destroy(oldChunks[i].gameObject);
        }

        chunks = new List<VoxelChunk>();
        existingChunks = new Dictionary<Vector2Int, VoxelChunk>();
        terrainNoise.Startup(voxelResolution, chunkResolution, player);
        voxelMesh.Startup(voxelResolution, chunkResolution, viewDistance, useColliders, colliderRadius);
        voxelEditor.Startup(voxelResolution, chunkResolution, viewDistance, existingChunks, chunks, this);

        GenerateTerrain();
    }
    
    private void RemoveOutOfBoundsChunks() {
        sqrViewDist = viewDistance * viewDistance;
        
        for (int i = chunks.Count - 1; i >= 0; i--) {
            var testChunk = chunks[i];
            var position = testChunk.transform.position;
            testChunkPos = new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y));
            playerOffset = playerCoord - testChunkPos;
            offset = new Vector2(Mathf.Abs(playerOffset.x), Mathf.Abs(playerOffset.y)) -
                     (Vector2.one * viewDistance) / 2;
            sqrDst = new Vector2(Mathf.Max(offset.x, 0), Mathf.Max(offset.y, 0)).sqrMagnitude;

            if (sqrDst > sqrViewDist) {
                existingChunks.Remove(testChunkPos);
                recycleableChunks.Enqueue(testChunk);
                chunks.RemoveAt(i);
            }
        }
    }

    private void CreateNewChunksInRange() {
        for (int y = -chunkResolution / 2, i = 0; y < chunkResolution / 2; y++) {
            for (int x = -chunkResolution / 2; x < chunkResolution / 2; x++, i++) {
                coord = new Vector2Int(x, y) + playerCoord;

                if (!existingChunks.ContainsKey(coord)) {
                    playerOffset = p - coord;
                    offset = new Vector2(Mathf.Abs(playerOffset.x), Mathf.Abs(playerOffset.y)) -
                             (Vector2.one * viewDistance) / 2;
                    sqrDst = offset.sqrMagnitude;

                    if (sqrDst <= sqrViewDist - 4) {
                        VoxelChunk currentChunk;
                        if (recycleableChunks.Count > 0) {
                            currentChunk = recycleableChunks.Dequeue();
                            var currentColliders = currentChunk.gameObject.GetComponents<EdgeCollider2D>();
                            foreach (var t in currentColliders) {
                                Destroy(t);
                            }
                        }
                        else {
                            currentChunk = CreateChunk(i, x, y);
                        }

                        currentChunk.SetNewChunk(coord.x, coord.y);
                        existingChunks.Add(coord, currentChunk);
                        currentChunk.shouldUpdateCollider = true;
                        chunks.Add(currentChunk);
                        terrainNoise.GenerateNoiseValues(currentChunk);
                    }
                }
            }
        }
    }
    
    private VoxelChunk CreateChunk(int i, int x, int y) {
        var chunk = Instantiate(voxelChunkPrefab, transform, true) as VoxelChunk;
        chunk.Initialize(useVoxelReferences, voxelResolution);
        chunk.transform.localPosition = new Vector3(x, y);
        chunk.gameObject.layer = 3;

        return chunk;
    }

    private void UpdateNewChunks() {
        foreach (var chunk in chunks.Where(chunk => chunk.shouldUpdateMesh)) {
            var position = chunk.transform.position;
            coord = new Vector2Int(Mathf.RoundToInt(position.x),
                Mathf.RoundToInt(position.y));
            SetupChunkNeighbors(coord, chunk);
        }
    }
    
    private void SetupChunkNeighbors(Vector2Int setupCoord, VoxelChunk chunk) {
        var axcoord = new Vector2Int(setupCoord.x - 1, setupCoord.y);
        var aycoord = new Vector2Int(setupCoord.x, setupCoord.y - 1);
        var axycoord = new Vector2Int(setupCoord.x - 1, setupCoord.y - 1);
        var bxcoord = new Vector2Int(setupCoord.x + 1, setupCoord.y);
        var bycoord = new Vector2Int(setupCoord.x, setupCoord.y + 1);
        var bxycoord = new Vector2Int(setupCoord.x + 1, setupCoord.y + 1);
        VoxelChunk tempChunk;

        if (!existingChunks.ContainsKey(setupCoord)) return;
        if (existingChunks.ContainsKey(axcoord)) {
            tempChunk = existingChunks[axcoord];
            tempChunk.shouldUpdateMesh = true;
            tempChunk.xNeighbor = chunk;
        }

        if (existingChunks.ContainsKey(aycoord)) {
            tempChunk = existingChunks[aycoord];
            tempChunk.shouldUpdateMesh = true;
            tempChunk.yNeighbor = chunk;
        }

        if (existingChunks.ContainsKey(axycoord)) {
            tempChunk = existingChunks[axycoord];
            tempChunk.shouldUpdateMesh = true;
            tempChunk.xyNeighbor = chunk;
        }

        if (existingChunks.ContainsKey(bxcoord)) {
            tempChunk = existingChunks[bxcoord];
            chunk.xNeighbor = tempChunk;
        }

        if (existingChunks.ContainsKey(bycoord)) {
            tempChunk = existingChunks[bycoord];
            chunk.yNeighbor = tempChunk;
        }

        if (existingChunks.ContainsKey(bxycoord)) {
            tempChunk = existingChunks[bxycoord];
            chunk.xyNeighbor = tempChunk;
        }
    }
    
    private void RecreateUpdatedChunkMeshes() {
        foreach (var chunk in chunks) {
            if (chunk.shouldUpdateMesh) {
                voxelMesh.TriangulateChunkMesh(chunk);
                chunk.shouldUpdateMesh = false;
            }

            if (!useColliders || !chunk.shouldUpdateCollider) continue;
            if (Vector3.Distance(p, chunk.transform.position) < colliderRadius) {
                chunkCollider.Generate2DCollider(chunk, chunkResolution);
                chunk.shouldUpdateCollider = false;
            }
        }
    }

    private void OnDrawGizmosSelected() {
        if (player) {
            Gizmos.color = new Color(0, 0, 1, 0.25f);
            Gizmos.DrawSphere(player.position, voxelResolution * chunkResolution / 2f);
        }
    }
}