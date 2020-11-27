using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelStencilCircle : VoxelStencil {
    private int squareRadius;

    public override void Initialize(bool fillType, int radius) {
        base.Initialize(fillType, radius);
        squareRadius = radius * radius;
    }

    public override bool Apply(int x, int y, bool voxel) {
        x -= centerX;
        y -= centerY;
        if (x * x + y * y <= squareRadius) {
            return fillType;
        }

        return voxel;
    }
}
