using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;

public class ChunkSaveLoadManager : MonoBehaviour {
    private BinaryFormatter bf = new BinaryFormatter();
    private List<FileStream> streams = new List<FileStream>();
    private List<Vector2> streamPositions = new List<Vector2>();

    public void OpenRegion(Vector2 pos) {
        String path = Application.persistentDataPath + "/region(" + pos.x + "," + pos.y + ").sav";

        if (!streamPositions.Contains(pos)) {
            streams.Add(new FileStream(path, FileMode.OpenOrCreate));
            streamPositions.Add(pos);
        }
    }

    public void CloseRegion(Vector2 pos) {
        if (streamPositions.Contains(pos)) {
            int index = streamPositions.IndexOf(pos);

            streams[index].Close();
            streams.RemoveAt(index);
            streamPositions.RemoveAt(index);
        }
    }

    public void UpdateRegionData(Vector2 pos, List<VoxelChunk> chunks) {
        String path = Application.persistentDataPath + "/region(" + pos.x + "," + pos.y + ").sav";

        if (streamPositions.Contains(pos)) {
            int index = streamPositions.IndexOf(pos);
            FileStream stream = streams[index];
            RegionData regionData = new RegionData(chunks);

            stream.SetLength(0);
            bf.Serialize(stream, regionData);
        }
    }

    public RegionData LoadRegionData(Vector2 pos) {
        String path = Application.persistentDataPath + "/region(" + pos.x + "," + pos.y + ").sav";

        if (File.Exists(path) && streamPositions.Contains(pos) && streams[streamPositions.IndexOf(pos)].Length > 0) {
            FileStream stream = streams[streamPositions.IndexOf(pos)];

            stream.Position = 0;
            RegionData regionData = (RegionData)bf.Deserialize(stream);

            return regionData;
        }

        return null;
    }
}

[Serializable]
public class RegionData {
    public List<ChunkData> chunkDatas = new List<ChunkData>();

    public RegionData(List<VoxelChunk> chunks) {
        foreach (VoxelChunk chunk in chunks) {
            chunkDatas.Add(new ChunkData(chunk));
        }
    }
}

[Serializable]
public class ChunkData {
    public float xPos;
    public float yPos;
    public List<float> voxelPositions = new List<float>();
    public List<int> voxelStates = new List<int>();

    public ChunkData(VoxelChunk chunk) {
        xPos = chunk.transform.position.x / 8;
        yPos = chunk.transform.position.y / 8;

        foreach (Voxel voxel in chunk.voxels) {
            voxelPositions.Add(voxel.position.x);
            voxelPositions.Add(voxel.position.y);
            voxelStates.Add(voxel.state);
        }
    }
}