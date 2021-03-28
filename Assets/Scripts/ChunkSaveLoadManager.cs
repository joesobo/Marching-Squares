using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;

public class ChunkSaveLoadManager : MonoBehaviour {
    private BinaryFormatter bf = new BinaryFormatter();
    private List<FileStream> streams = new List<FileStream>();
    private List<Vector2> streamPositions = new List<Vector2>();
    private List<Transform> regionList = new List<Transform>();
    private Transform parent;
    private int regionResolution;
    private int halfRes;

    public void Startup(InfiniteGeneration ig, int regionResolution) {
        this.parent = ig.transform;
        this.regionResolution = regionResolution;
        halfRes = regionResolution / 2;
    }

    private void OpenRegion(Vector2 regionPos) {
        String path = Application.persistentDataPath + "/region(" + regionPos.x + "," + regionPos.y + ").sav";

        if (!streamPositions.Contains(regionPos)) {
            streams.Add(new FileStream(path, FileMode.OpenOrCreate));
            streamPositions.Add(regionPos);
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
        String path = Application.persistentDataPath + "/region(" + regionPos.x + "," + regionPos.y + ").sav";

        if (streamPositions.Contains(regionPos)) {
            int index = streamPositions.IndexOf(regionPos);
            FileStream stream = streams[index];

            stream.SetLength(0);
            bf.Serialize(stream, regionData);
        }
    }

    private RegionData LoadRegionData(Vector2 regionPos) {
        String path = Application.persistentDataPath + "/region(" + regionPos.x + "," + regionPos.y + ").sav";

        if (streamPositions.Contains(regionPos) && streams[streamPositions.IndexOf(regionPos)].Length > 0) {
            if (File.Exists(path)) {
                FileStream stream = streams[streamPositions.IndexOf(regionPos)];

                stream.Position = 0;
                RegionData regionData = (RegionData)bf.Deserialize(stream);

                return regionData;
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
        Vector2 regionPos = RegionPosFromChunkPos(chunkPos);

        foreach (Transform region in regionList) {
            var name = region.name.Substring(7);
            string[] checkPos = name.Split(',');
            int checkX = int.Parse(checkPos[0]);
            int checkY = int.Parse(checkPos[1]);

            if (checkX == regionPos.x && checkY == regionPos.y) {
                return region;
            }
        }

        var newRegion = new GameObject();
        newRegion.transform.parent = parent;
        newRegion.transform.name = "Region " + regionPos.x + "," + regionPos.y;
        newRegion.transform.position = regionPos;
        regionList.Add(newRegion.transform);
        OpenRegion(regionPos);
        return newRegion.transform;
    }

    public void SaveChunk(Vector2 chunkPos, VoxelChunk saveChunk) {
        Vector2 regionPos = RegionPosFromChunkPos(chunkPos);
        RegionData data = LoadRegionData(regionPos);
        bool hasBeenAdded = false;

        foreach (ChunkData chunkData in data.chunkDatas) {
            if (chunkData.xPos == chunkPos.x && chunkData.yPos == chunkPos.y) {
                hasBeenAdded = true;
                for (int j = 0, count = 0; j < saveChunk.voxels.Length; j++, count += 2) {
                    chunkData.voxelPositions[count] = saveChunk.voxels[j].position.x;
                    chunkData.voxelPositions[count] = saveChunk.voxels[j].position.y;
                    chunkData.voxelStates[j] = saveChunk.voxels[j].state;
                }
            }
        }
        if (!hasBeenAdded) {
            ChunkData newData = new ChunkData(chunkPos, saveChunk);
            data.chunkDatas.Add(newData);
        }
        UpdateRegionData(regionPos, data);
    }

    public VoxelChunk LoadChunk(Vector2 chunkPos, VoxelChunk fillChunk) {
        Transform region = GetRegionTransformForChunk(chunkPos);
        Vector2 regionPos = RegionPosFromChunkPos(chunkPos);
        RegionData data = LoadRegionData(regionPos);

        if (regionPos == new Vector2(1, 1)) {
            Debug.Log('x');
        }

        foreach (ChunkData chunkData in data.chunkDatas) {
            if (chunkData.xPos == chunkPos.x && chunkData.yPos == chunkPos.y) {
                for (int j = 0, count = 0; j < fillChunk.voxels.Length; j++, count += 2) {
                    fillChunk.voxels[j].position = new Vector2(chunkData.voxelPositions[count], chunkData.voxelPositions[count + 1]);
                    fillChunk.voxels[j].state = chunkData.voxelStates[j];
                }
                return fillChunk;
            }
        }
        return null;
    }

    public void CheckForEmptyRegions() {
        for (int i = 0; i < regionList.Count - 1; i++) {
            Transform region = regionList[i];
            var regionPos = region.position;
            if (region.childCount == 0) {
                CloseRegion(regionPos);
            }
        }
    }

    private void OnApplicationQuit() {
        Debug.Log("Saving all regions");

        foreach (Transform region in regionList) {
            foreach (Transform child in region) {
                VoxelChunk chunk = child.GetComponent<VoxelChunk>();

                if (chunk) {
                    SaveChunk(chunk.transform.position / 8, chunk);
                }
            }
        }

        foreach (Transform region in regionList) {
            var regionPos = region.localPosition;
            CloseRegion(regionPos);
        }
    }
}

[Serializable]
public class RegionData {
    public List<ChunkData> chunkDatas = new List<ChunkData>();

    public RegionData(List<VoxelChunk> chunks) {
        foreach (VoxelChunk chunk in chunks) {
            chunkDatas.Add(new ChunkData(chunk.transform.position, chunk));
        }
    }
}

[Serializable]
public class ChunkData {
    public float xPos;
    public float yPos;
    public List<float> voxelPositions = new List<float>();
    public List<int> voxelStates = new List<int>();

    public ChunkData(Vector2 chunkPos, VoxelChunk chunk) {
        xPos = chunkPos.x;
        yPos = chunkPos.y;

        foreach (Voxel voxel in chunk.voxels) {
            voxelPositions.Add(voxel.position.x);
            voxelPositions.Add(voxel.position.y);
            voxelStates.Add(voxel.state);
        }
    }
}