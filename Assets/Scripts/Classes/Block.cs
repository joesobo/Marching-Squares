using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Block {
    public Texture2D texture;
    public int index;
    public string name;
    public Color color;

    public Block(int index, string name, Color color, Texture2D texture) {
        this.index = index;
        this.name = name;
        this.color = color;
        this.texture = texture;
    }
}

[Serializable]
public class BlockCollection {
    public List<Block> blocks = new List<Block>();
}