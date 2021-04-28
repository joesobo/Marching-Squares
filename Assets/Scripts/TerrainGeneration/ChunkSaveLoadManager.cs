using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;
using System.Linq;
using Object = UnityEngine.Object;

public class ChunkSaveLoadManager : MonoBehaviour {
    private readonly BinaryFormatter bf = new BinaryFormatter();
    private readonly List<FileStream> streams = new List<FileStream>();
    private readonly List<Vector2> streamPositions = new List<Vector2>();
    private readonly List<Transform> regionList = new List<Transform>();
    private Transform parent;
    private int halfRes;

    private WorldManager worldManager;
    private WorldScriptableObject worldScriptableObject;
    private string worldPath;

    public void Startup(InfiniteGeneration ig, WorldScriptableObject worldObject, int regionResolution) {
        parent = ig.transform;
        worldScriptableObject = worldObject;
        worldPath = worldScriptableObject.pathName;
        halfRes = regionResolution / 2;

        worldManager = FindObjectOfType<WorldManager>();
    }

    private void OpenRegion(Vector2 regionPos) {
        if (worldManager.worldName != "") {
            Directory.CreateDirectory(worldPath);
            string path = worldPath + "/region(" + regionPos.x + "," + regionPos.y + ").sav";

            if (!streamPositions.Contains(regionPos)) {
                streams.Add(new FileStream(path, FileMode.OpenOrCreate));
                streamPositions.Add(regionPos);
            }
        }
    }

    private void CloseRegion(Vector2 regionPos) {
        if (streamPositions.Contains(regionPos)) {
            int index = streamPositions.IndexOf(regionPos);

            streams[index].Close();
            streams.RemoveAt(index);
            streamPositions.RemoveAt(index);
        }
    }

    private void UpdateRegionData(Vector2 regionPos, RegionData regionData) {
        if (streamPositions.Contains(regionPos)) {
            int index = streamPositions.IndexOf(regionPos);
            var stream = streams[index];

            stream.SetLength(0);
            bf.Serialize(stream, regionData);
        }
    }

    private RegionData LoadRegionData(Vector2 regionPos) {
        string path = worldPath + "/region(" + regionPos.x + "," + regionPos.y + ").sav";

        if (streamPositions.Contains(regionPos) && streams[streamPositions.IndexOf(regionPos)].Length > 0) {
            if (File.Exists(path)) {
                var stream = streams[streamPositions.IndexOf(regionPos)];
                stream.Position = 0;

                return (RegionData)bf.Deserialize(stream);
            }
        }

        return new RegionData(new List<VoxelChunk>());
    }

    private Vector2 RegionPosFromChunkPos(Vector2 chunkPos) {
        float xVal = chunkPos.x / (float)halfRes;
        float yVal = chunkPos.y / (float)halfRes;
        int regionX = xVal < 0f ? (int)Mathf.Ceil(xVal) : (int)Mathf.Floor(xVal);
        int regionY = yVal < 0f ? (int)Mathf.Ceil(yVal) : (int)Mathf.Floor(yVal);
        return new Vector2(regionX, regionY);
    }

    public Transform GetRegionTransformForChunk(Vector2 chunkPos) {
        var regionPos = RegionPosFromChunkPos(chunkPos);

        foreach (var region in from region in regionList
                               let check = GetRegionPosition(region)
                               where check == regionPos
                               select region) {
            return region;
        }

        var newRegion = new GameObject();
        newRegion.transform.parent = parent;
        newRegion.transform.name = "Region " + regionPos.x + "," + regionPos.y;
        regionList.Add(newRegion.transform);
        OpenRegion(regionPos);
        return newRegion.transform;
    }

    public void SaveChunk(Vector2 chunkPos, VoxelChunk saveChunk) {
        if (worldManager.worldName != "") {
            var regionPos = RegionPosFromChunkPos(chunkPos);
            var data = LoadRegionData(regionPos);
            bool hasBeenAdded = false;

            foreach (var chunkData in data.chunkDatas.Where(chunkData =>
                chunkData.xPos == chunkPos.x && chunkData.yPos == chunkPos.y)) {
                hasBeenAdded = true;
                for (int j = 0, count = 0; j < saveChunk.voxels.Length; j++, count += 2) {
                    chunkData.voxelPositions[count] = saveChunk.voxels[j].position.x;
                    chunkData.voxelPositions[count] = saveChunk.voxels[j].position.y;
                    chunkData.voxelStates[j] = saveChunk.voxels[j].state;
                }
            }

            if (!hasBeenAdded) {
                var newData = new ChunkData(chunkPos, saveChunk);
                data.chunkDatas.Add(newData);
            }

            UpdateRegionData(regionPos, data);
        }
    }

    public VoxelChunk LoadChunk(Vector2 chunkPos, VoxelChunk fillChunk) {
        GetRegionTransformForChunk(chunkPos);
        var regionPos = RegionPosFromChunkPos(chunkPos);
        var data = LoadRegionData(regionPos);

        foreach (var chunkData in data.chunkDatas.Where(chunkData =>
            chunkData.xPos == chunkPos.x && chunkData.yPos == chunkPos.y)) {
            for (int j = 0, count = 0; j < fillChunk.voxels.Length; j++, count += 2) {
                fillChunk.voxels[j].position =
                    new Vector2(chunkData.voxelPositions[count], chunkData.voxelPositions[count + 1]);
                fillChunk.voxels[j].state = chunkData.voxelStates[j];
            }

            return fillChunk;
        }

        return null;
    }

    public void CheckForEmptyRegions() {
        for (int i = 0; i < regionList.Count - 1; i++) {
            var region = regionList[i];
            if (region.childCount == 0) {
                CloseRegion(GetRegionPosition(region));
                regionList.RemoveAt(i);
                i--;
                Destroy(region.gameObject);
            }
        }
    }

    public void SaveAllChunks() {
        if (worldManager.worldName != "") {
            Debug.Log("Saving all regions");

            foreach (var chunk in from region in regionList
                                  from Transform child in region
                                  select child.GetComponent<VoxelChunk>()
                into chunk
                                  where chunk
                                  select chunk) {
                SaveChunk(chunk.transform.position / 8, chunk);
            }

            foreach (var region in regionList) {
                CloseRegion(GetRegionPosition(region));
            }
        }
    }

    private void OnApplicationQuit() {
        SaveAllChunks();
    }

    private static Vector2 GetRegionPosition(Object region) {
        string regionName = region.name.Substring(7);
        string[] checkPos = regionName.Split(',');
        int xPos = int.Parse(checkPos[0]);
        int yPos = int.Parse(checkPos[1]);
        return new Vector2(xPos, yPos);
    }
}