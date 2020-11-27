using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelStencil : MonoBehaviour {
    protected bool fillType;
    protected int centerX, centerY, radius;

    public virtual void Initialize(bool fillType, int radius) {
        this.fillType = fillType;
        this.radius = radius;
    }

    public virtual bool Apply(int x, int y, bool voxel) {
        return fillType;
    }

    public virtual void SetCenter(int x, int y) {
        centerX = x;
        centerY = y;
    }

    public int XStart {
        get {
            return centerX - radius;
        }
    }

    public int XEnd {
        get {
            return centerX + radius;
        }
    }

    public int YStart {
        get {
            return centerY - radius;
        }
    }

    public int YEnd {
        get {
            return centerY + radius;
        }
    }
}
