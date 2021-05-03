using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Block {
    public string texturePath;
    public int index;
    public string name;
    public Color color;

    public Block(int index, string name, Color color, string texturePath) {
        this.index = index;
        this.name = name;
        this.color = color;
        this.texturePath = texturePath;
    }
}

[Serializable]
public class BlockCollection {
    public List<Block> blocks = new List<Block>();
}