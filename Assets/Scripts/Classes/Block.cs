using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Block {
    public string texturePath;
    public BlockType blockType;
    public Color color;

    public Block(BlockType blockType, Color color, string texturePath) {
        this.blockType = blockType;
        this.color = color;
        this.texturePath = texturePath;
    }
}

[Serializable]
public class BlockCollection {
    public List<Block> blocks = new List<Block>();
}