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
    private static string path = Application.persistentDataPath + "/blocks.json";

    public static Dictionary<BlockType, int> blockIndexDictionary = new Dictionary<BlockType, int>();

    public static void WriteBlocks(BlockCollection collection, Block newBlock) {
        if (newBlock != null) {
            collection.blocks.Add(newBlock);
        }

        using (StreamWriter w = new StreamWriter(path, false)) {
            string json = JsonUtility.ToJson(collection);
            w.Write(json);
            w.Flush();
            w.Close();
        }
    }

    public static void RemoveBlock(BlockCollection collection, int index) {
        BlockCollection tempCollection = new BlockCollection();
        if (index == collection.blocks.Count - 1) {
            tempCollection.blocks = collection.blocks.GetRange(0, index);
            tempCollection.blocks.AddRange(collection.blocks.GetRange(index + 1, collection.blocks.Count));
        } else {
            tempCollection.blocks = collection.blocks.GetRange(0, index);
        }
        WriteBlocks(tempCollection, null);
    }

    public static BlockCollection ReadBlocks() {
        BlockCollection blocks = new BlockCollection();

        try {
            using (StreamReader r = new StreamReader(path)) {
                string json = r.ReadToEnd();
                blocks = JsonUtility.FromJson<BlockCollection>(json);
                r.Close();
            }
        } catch (Exception e) {
            Debug.Log("ERROR: No block file found.");
            Debug.Log(e.Message);
        }

        blockIndexDictionary.Clear();
        for (int i = 0; i < blocks.blocks.Count; i++) {
            Block block = blocks.blocks[i];
            if (!blockIndexDictionary.ContainsKey(block.blockType)) {
                blockIndexDictionary.Add(block.blockType, i);
            }
        }

        return blocks;
    }
}
