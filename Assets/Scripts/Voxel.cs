using UnityEngine;
using System;

[Serializable]
public class Voxel {
    public int state;

    public Vector2 position;

    public Voxel(int x, int y, float size) {
        position.x = (x + 0.5f) * size;
        position.y = (y + 0.5f) * size;
    }
}