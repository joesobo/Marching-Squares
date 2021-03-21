using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InfiniteGeneration : MonoBehaviour {
    public VoxelChunk voxelChunkPrefab;

    private VoxelMesh voxelMesh;
    private Transform player;
    private TerrainNoise terrainNoise;

    private Vector2 p, playerOffset, offset;
    private Vector2Int playerCoord, testChunkPos, coord;
    private float sqrViewDist, sqrDst;
    private Queue<VoxelChunk> recycleableChunks;
    private int chunkResolution, voxelResolution, viewDistance;
    private List<VoxelChunk> chunks;
    private Dictionary<Vector2Int, VoxelChunk> existingChunks;
    private bool useVoxelReferences;
    private float colliderRadius;
    private bool useColliders;
    private ChunkCollider chunkCollider;

    public void StartUp(VoxelMap map) {
        voxelMesh = FindObjectOfType<VoxelMesh>();
        terrainNoise = FindObjectOfType<TerrainNoise>();
        chunkCollider = FindObjectOfType<ChunkCollider>();

        recycleableChunks = map.recycleableChunks;
        chunkResolution = map.chunkResolution;
        voxelResolution = map.voxelResolution;
        viewDistance = map.viewDistance;
        chunks = map.chunks;
        existingChunks = map.existingChunks;
        useVoxelReferences = map.useVoxelReferences;
        colliderRadius = map.colliderRadius;
        useColliders = map.useColliders;
        player = map.player;

        terrainNoise.Startup(voxelResolution, chunkResolution, player);
        voxelMesh.Startup(voxelResolution, chunkResolution, viewDistance, useColliders, colliderRadius);
    }

    public void UpdateAroundPlayer() {
        p = player.position / voxelResolution;
        playerCoord = new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));

        voxelMesh.CreateBuffers();

        RemoveOutOfBoundsChunks();

        CreateNewChunksInRange();

        UpdateNewChunks();

        RecreateUpdatedChunkMeshes();
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
                ChunkSaveLoadManager.SaveChunk(existingChunks[testChunkPos]);

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

                if (existingChunks.ContainsKey(coord)) continue;
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
                    } else {
                        currentChunk = CreateChunk(x, y);
                    }

                    currentChunk.SetNewChunk(coord.x, coord.y);
                    existingChunks.Add(coord, currentChunk);
                    currentChunk.shouldUpdateCollider = true;
                    chunks.Add(currentChunk);
                    ChunkData loadedChunkData = ChunkSaveLoadManager.LoadChunk(coord);
                    if (loadedChunkData != null) {
                        for (int j = 0, count = 0; j < currentChunk.voxels.Length; j++, count += 2) {
                            currentChunk.voxels[j].position = new Vector2(loadedChunkData.voxelPositions[count], loadedChunkData.voxelPositions[count + 1]);
                            currentChunk.voxels[j].state = loadedChunkData.voxelStates[j];
                        }
                    } else {
                        terrainNoise.GenerateNoiseValues(currentChunk);
                    }
                }
            }
        }
    }

    void OnApplicationQuit() {
        Debug.Log("Saving all chunks");

        foreach (var chunk in chunks) {
            ChunkSaveLoadManager.SaveChunk(chunk);
        }
    }

    private VoxelChunk CreateChunk(int x, int y) {
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
}