using UnityEngine;

public class VoxelStencil {

    public int fillType;
    protected int centerX, centerY, radius;

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

    public virtual void Initialize(int fillType, int radius) {
        this.fillType = fillType;
        this.radius = radius;
    }

    public virtual void SetCenter(int x, int y) {
        centerX = x;
        centerY = y;
    }

    public virtual int Apply(int x, int y, int voxel) {
        return fillType;
    }
}