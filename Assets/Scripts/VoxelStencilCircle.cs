using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelStencilCircle : VoxelStencil {
    private float squareRadius;

    public override void Initialize(bool fillType, float radius) {
        base.Initialize(fillType, radius);
        squareRadius = radius * radius;
    }

    public override void Apply(Voxel voxel) {
        float x = voxel.position.x - centerX;
        float y = voxel.position.y - centerY;
        if (x * x + y * y <= squareRadius) {
            voxel.state = fillType;
        }
    }

    protected override void FindHorizontalCrossing(Voxel xMin, Voxel xMax) {
        float y2 = xMin.position.y - centerY;
        y2 *= y2;
        if (xMin.state == fillType) {
            float x = xMin.position.x - centerX;
            if (x * x + y2 <= squareRadius) {
                x = centerX + Mathf.Sqrt(squareRadius - y2);
                if (xMin.xEdge == float.MinValue || xMin.xEdge < x) {
                    xMin.xEdge = x;
                }
            }
        } else if (xMax.state == fillType) {
            float x = xMax.position.x - centerX;
            if (x * x + y2 <= squareRadius) {
                x = centerX - Mathf.Sqrt(squareRadius - y2);
                if (xMin.xEdge == float.MinValue || xMin.xEdge > x) {
                    xMin.xEdge = x;
                }
            }
        }
    }

    protected override void FindVerticalCrossing(Voxel yMin, Voxel yMax) {
        float x2 = yMin.position.x - centerX;
        x2 *= x2;
        if (yMin.state == fillType) {
            float y = yMin.position.y - centerY;
            if (y * y + x2 <= squareRadius) {
                y = centerY + Mathf.Sqrt(squareRadius - x2);
                if (yMin.yEdge == float.MinValue || yMin.yEdge < y) {
                    yMin.yEdge = y;
                }
            }
        } else if (yMax.state == fillType) {
            float y = yMax.position.y - centerY;
            if (y * y + x2 <= squareRadius) {
                y = centerY - Mathf.Sqrt(squareRadius - x2);
                if (yMin.yEdge == float.MinValue || yMin.yEdge > y) {
                    yMin.yEdge = y;
                }
            }
        }
    }
}
