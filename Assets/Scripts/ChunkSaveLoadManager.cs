using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;

public static class ChunkSaveLoadManager {
    public static void SaveChunk(VoxelChunk chunk) {
        BinaryFormatter bf = new BinaryFormatter();
        Vector2 pos = chunk.transform.localPosition;
        String path = Application.persistentDataPath + "/chunks(" + pos.x + "," + pos.y + ").sav";

        FileStream stream = new FileStream(path, FileMode.OpenOrCreate);
        ChunkData chunkData = new ChunkData(chunk);

        bf.Serialize(stream, chunkData);
        stream.Close();
    }

    public static ChunkData LoadChunk(Vector2 pos) {
        String path = Application.persistentDataPath + "/chunks(" + pos.x + "," + pos.y + ").sav";

        if (File.Exists(path)) {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            ChunkData test = (ChunkData)bf.Deserialize(stream);
            stream.Close();

            return test;
        }

        return null;
    }
}

[Serializable]
public class ChunkData {
    public List<float> voxelPositions = new List<float>();
    public List<int> voxelStates = new List<int>();

    public ChunkData(VoxelChunk chunk) {
        foreach (Voxel voxel in chunk.voxels) {
            voxelPositions.Add(voxel.position.x);
            voxelPositions.Add(voxel.position.y);
            voxelStates.Add(voxel.state);
        }
    }
}