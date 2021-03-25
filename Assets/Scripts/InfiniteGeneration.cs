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
    private int chunkResolution, voxelResolution, viewDistance, regionResolution;
    private List<VoxelChunk> chunks;
    private Dictionary<Vector2Int, VoxelChunk> existingChunks;
    private bool useVoxelReferences;
    private float colliderRadius;
    private bool useColliders;
    private ChunkCollider chunkCollider;

    private ChunkSaveLoadManager chunkSaveLoadManager;
    private List<Transform> regionList;

    public void StartUp(VoxelMap map) {
        voxelMesh = FindObjectOfType<VoxelMesh>();
        terrainNoise = FindObjectOfType<TerrainNoise>();
        chunkCollider = FindObjectOfType<ChunkCollider>();

        recycleableChunks = map.recycleableChunks;
        regionResolution = map.regionResolution;
        chunkResolution = map.chunkResolution;
        voxelResolution = map.voxelResolution;
        viewDistance = map.viewDistance;
        chunks = map.chunks;
        existingChunks = map.existingChunks;
        useVoxelReferences = map.useVoxelReferences;
        colliderRadius = map.colliderRadius;
        useColliders = map.useColliders;
        player = map.player;
        chunkSaveLoadManager = map.chunkSaveLoadManager;

        terrainNoise.Startup(voxelResolution, chunkResolution, player);
        voxelMesh.Startup(voxelResolution, chunkResolution, viewDistance, useColliders, colliderRadius);

        regionList = new List<Transform>();
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
        List<Vector2> updatedPositions = new List<Vector2>();
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
                updatedPositions.Add(testChunkPos);

                existingChunks.Remove(testChunkPos);
                recycleableChunks.Enqueue(testChunk);
                chunks.RemoveAt(i);
            }
        }

        int halfRes = regionResolution / 2;
        List<Vector2> alreadyUpdated = new List<Vector2>();
        foreach (Vector2 pos in updatedPositions) {
            float xVal = pos.x / (float)halfRes;
            float yVal = pos.y / (float)halfRes;
            int regionX = xVal < 0f ? (int)Mathf.Ceil(xVal) : (int)Mathf.Floor(xVal);
            int regionY = yVal < 0f ? (int)Mathf.Ceil(yVal) : (int)Mathf.Floor(yVal);
            Vector2 regionPos = new Vector2(regionX, regionY);

            if (!alreadyUpdated.Contains(regionPos)) {
                chunkSaveLoadManager.UpdateRegionData(regionPos, GetRegionChunks(regionPos));

                alreadyUpdated.Add(regionPos);
            }
        }

        // Remove old regions
        for (int i = 0; i < regionList.Count; i++) {
            var temp = regionList[i];
            var pos = temp.transform.position;
            if (temp.childCount == 0) {
                chunkSaveLoadManager.CloseRegion(pos);
                regionList.RemoveAt(i);
                Destroy(temp.gameObject);
                i--;
            }
        }
    }

    private void CreateNewChunksInRange() {
        List<Vector2Int> newChunkPositions = new List<Vector2Int>();
        List<Vector2> activeRegions = new List<Vector2>();
        int halfRes = regionResolution / 2;

        for (int y = -chunkResolution / 2, i = 0; y < chunkResolution / 2; y++) {
            for (int x = -chunkResolution / 2; x < chunkResolution / 2; x++, i++) {
                coord = new Vector2Int(x, y) + playerCoord;

                float xVal = coord.x / (float)halfRes;
                float yVal = coord.y / (float)halfRes;
                int regionX = xVal < 0f ? (int)Mathf.Ceil(xVal) : (int)Mathf.Floor(xVal);
                int regionY = yVal < 0f ? (int)Mathf.Ceil(yVal) : (int)Mathf.Floor(yVal);
                Vector2 regionPos = new Vector2(regionX, regionY);

                if (!activeRegions.Contains(regionPos)) {
                    activeRegions.Add(regionPos);
                }

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
                    newChunkPositions.Add(coord);

                    currentChunk.SetNewChunk(coord.x, coord.y);
                    existingChunks.Add(coord, currentChunk);
                    currentChunk.shouldUpdateCollider = true;
                    chunks.Add(currentChunk);
                    currentChunk.transform.parent = GetRegionTransform(regionX, regionY);
                }
            }
        }

        // generates new data or loads from file
        foreach (Vector2Int chunkPos in newChunkPositions) {
            float xVal = chunkPos.x / (float)halfRes;
            float yVal = chunkPos.y / (float)halfRes;
            int regionX = xVal < 0f ? (int)Mathf.Ceil(xVal) : (int)Mathf.Floor(xVal);
            int regionY = yVal < 0f ? (int)Mathf.Ceil(yVal) : (int)Mathf.Floor(yVal);
            Vector2 regionPos = new Vector2(regionX, regionY);

            RegionData loadedRegionData = chunkSaveLoadManager.LoadRegionData(regionPos);
            var chunk = existingChunks[chunkPos];
            if (loadedRegionData != null) {
                foreach (ChunkData chunkData in loadedRegionData.chunkDatas) {
                    if (chunkData.xPos == chunkPos.x && chunkData.yPos == chunkPos.y) {
                        for (int j = 0, count = 0; j < chunk.voxels.Length; j++, count += 2) {
                            chunk.voxels[j].position = new Vector2(chunkData.voxelPositions[count], chunkData.voxelPositions[count + 1]);
                            chunk.voxels[j].state = chunkData.voxelStates[j];
                        }
                    }
                }
            } else {
                terrainNoise.GenerateNoiseValues(chunk);
            }
        }
    }

    void OnApplicationQuit() {
        Debug.Log("Saving all regions");

        foreach (Transform region in regionList) {
            var pos = region.position;

            chunkSaveLoadManager.UpdateRegionData(pos, GetRegionChunks(pos));
            chunkSaveLoadManager.CloseRegion(pos);
        }
    }

    private VoxelChunk CreateChunk(int x, int y) {
        var chunk = Instantiate(voxelChunkPrefab, null, true) as VoxelChunk;
        chunk.Initialize(useVoxelReferences, voxelResolution);
        chunk.transform.localPosition = new Vector3(x, y);
        chunk.gameObject.layer = 3;

        return chunk;
    }

    private Transform GetRegionTransform(int x, int y) {
        foreach (Transform region in regionList) {
            var name = region.name.Substring(7);
            string[] checkPos = name.Split(',');
            int checkX = int.Parse(checkPos[0]);
            int checkY = int.Parse(checkPos[1]);

            if (checkX == x && checkY == y) {
                return region;
            }
        }

        var newRegion = new GameObject();
        newRegion.transform.parent = transform;
        newRegion.transform.name = "Region " + x + "," + y;
        newRegion.transform.position = new Vector3(x, y);
        regionList.Add(newRegion.transform);
        chunkSaveLoadManager.OpenRegion(new Vector2(x, y));
        return newRegion.transform;
    }

    private List<VoxelChunk> GetRegionChunks(Vector2 pos) {
        foreach (Transform region in regionList) {
            Vector2 regionPos = region.localPosition;
            if (regionPos == pos) {
                List<VoxelChunk> chunks = new List<VoxelChunk>();

                foreach (Transform child in region) {
                    chunks.Add(child.GetComponent<VoxelChunk>());
                }

                return chunks;
            }
        }

        return null;
    }

    private bool RegionContains(Vector2 chunkPos, Vector2 regionPos) {
        int halfRes = regionResolution / 2;

        float a = (regionPos.x * regionResolution) + (regionPos.x < 0 ? 0 : -halfRes);
        float b = (regionPos.y * regionResolution) + (regionPos.y < 0 ? 0 : -halfRes);
        float c = (regionPos.x * regionResolution) + (regionPos.x < 0 ? 0 : halfRes);
        float d = (regionPos.y * regionResolution) + (regionPos.y < 0 ? 0 : halfRes);

        if (chunkPos.x > a && chunkPos.x < c && chunkPos.y > b && chunkPos.y < d) {
            return true;
        }

        return false;
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