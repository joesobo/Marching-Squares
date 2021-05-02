using System;
using System.IO;
using UnityEngine;

public static class BlockManager {
    private static string path = Application.persistentDataPath + "/blocks.json";
    private static int blockCount = 0;

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

    public static void RemoveBlock(BlockCollection collection) {
        BlockCollection tempCollection = new BlockCollection();
        tempCollection.blocks = collection.blocks.GetRange(0, collection.blocks.Count - 1);
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

        blockCount = blocks.blocks.Count;
        return blocks;
    }
}
