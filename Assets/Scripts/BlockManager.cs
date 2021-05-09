using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public enum BlockType {
    Empty,
    Stone,
    Dirt,
    Rock,
    Grass,
    White
};

public static class BlockManager {
    private static readonly string Path = Application.persistentDataPath + "/blocks.json";

    public static Dictionary<BlockType, int> BlockIndexDictionary = new Dictionary<BlockType, int>();
    public static Dictionary<BlockType, Color> BlockColorDictionary = new Dictionary<BlockType, Color>();

    public static void WriteBlocks(BlockCollection collection, Block newBlock) {
        if (newBlock != null) {
            collection.blocks.Add(newBlock);
        }

        using var w = new StreamWriter(Path, false);
        var json = JsonUtility.ToJson(collection);
        w.Write(json);
        w.Flush();
        w.Close();
    }

    public static void RemoveBlock(BlockCollection collection, int index) {
        var tempCollection = new BlockCollection();
        if (index == collection.blocks.Count - 1) {
            tempCollection.blocks = collection.blocks.GetRange(0, index);
            tempCollection.blocks.AddRange(collection.blocks.GetRange(index + 1, collection.blocks.Count));
        } else {
            tempCollection.blocks = collection.blocks.GetRange(0, index);
        }
        WriteBlocks(tempCollection, null);
    }

    public static BlockCollection ReadBlocks() {
        var blocks = new BlockCollection();

        try {
            using var r = new StreamReader(Path);
            var json = r.ReadToEnd();
            blocks = JsonUtility.FromJson<BlockCollection>(json);
            r.Close();
        } catch (Exception e) {
            Debug.Log("ERROR: No block file found.");
            Debug.Log(e.Message);
        }

        BlockIndexDictionary.Clear();
        BlockColorDictionary.Clear();
        for (var i = 0; i < blocks.blocks.Count; i++) {
            var block = blocks.blocks[i];
            if (!BlockIndexDictionary.ContainsKey(block.blockType)) {
                BlockIndexDictionary.Add(block.blockType, i);
                BlockColorDictionary.Add(block.blockType, block.color);
            }
        }

        return blocks;
    }
}
