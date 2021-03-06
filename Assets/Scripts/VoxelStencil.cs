using UnityEngine;

public class VoxelStencil {
    public int fillType;
    protected int centerX, centerY;
    private int radius;

    public int XStart => centerX - radius;

    public int XEnd => centerX + radius;

    public int YStart => centerY - radius;

    public int YEnd => centerY + radius;

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