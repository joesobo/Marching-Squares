using System;
using System.Collections.Generic;

[Serializable]
public class RegionData {
    public List<ChunkData> chunkDatas = new List<ChunkData>();

    public RegionData(IEnumerable<VoxelChunk> chunks) {
        foreach (var chunk in chunks) {
            chunkDatas.Add(new ChunkData(chunk.transform.position, chunk));
        }
    }
}

