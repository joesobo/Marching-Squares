using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ChunkData {
    public float xPos;
    public float yPos;
    public List<float> voxelPositions = new List<float>();
    public List<int> voxelStates = new List<int>();

    public ChunkData(Vector2 chunkPos, VoxelChunk chunk) {
        xPos = chunkPos.x;
        yPos = chunkPos.y;

        foreach (var voxel in chunk.voxels) {
            voxelPositions.Add(voxel.position.x);
            voxelPositions.Add(voxel.position.y);
            voxelStates.Add(voxel.state);
        }
    }
}