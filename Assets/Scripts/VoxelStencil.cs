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

    public void SetHorizontalCrossing(Voxel xMin, Voxel xMax) {
        if (xMin.state != xMax.state) {
            FindHorizontalCrossing(xMin, xMax);
        }
    }

    public void SetVirtualCrossing(Voxel yMin, Voxel yMax) {
        if (yMin.state != yMax.state) {
            FindVirtualCrossing(yMin, yMax);
        }
    }

    protected virtual void FindHorizontalCrossing(Voxel xMin, Voxel xMax) {
        if (xMin.position.y < YStart || xMin.position.y > YEnd) {
            return;
        }
        if (xMin.state == fillType) {
            if (xMin.position.x <= XEnd && xMax.position.x >= XEnd) {
                xMin.xEdge = XEnd;
            }
        } else if (xMax.state == fillType) {
            if (xMin.position.x <= XStart && xMin.position.x >= XStart) {
                xMin.xEdge = XStart;
            }
        }
    }

    protected virtual void FindVirtualCrossing(Voxel yMin, Voxel yMax) {
        if (yMin.position.x < XStart || yMin.position.x > XEnd) {
            return;
        }
        if (yMin.state == fillType) {
            if (yMin.position.y <= YEnd && yMax.position.y >= YEnd) {
                yMin.yEdge = YEnd;
            }
        } else if (yMax.state == fillType) {
            if (yMin.position.y <= YStart && yMin.position.y >= YStart) {
                yMin.yEdge = YStart;
            }
        }
    }
}
