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
        if ( x * x + y * y <= squareRadius) {
            voxel.state = fillType;
        }
    }
}
