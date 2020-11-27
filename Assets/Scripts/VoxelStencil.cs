using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelStencil : MonoBehaviour {
    protected bool fillType;
    protected float centerX, centerY, radius;

    public virtual void Initialize(bool fillType, float radius) {
        this.fillType = fillType;
        this.radius = radius;
    }

    public virtual void Apply(Voxel voxel) {
        Vector2 p = voxel.position;
        if (p.x >= XStart && p.x <= XEnd && p.y >= YStart && p.y <= YEnd) {
            voxel.state = fillType;
        }
    }

    public virtual void SetCenter(float x, float y) {
        centerX = x;
        centerY = y;
    }

    public float XStart {
        get {
            return centerX - radius;
        }
    }

    public float XEnd {
        get {
            return centerX + radius;
        }
    }

    public float YStart {
        get {
            return centerY - radius;
        }
    }

    public float YEnd {
        get {
            return centerY + radius;
        }
    }
}
