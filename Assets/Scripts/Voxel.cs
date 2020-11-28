using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Voxel {
    public bool state;
    public Vector2 position;
    public float xEdge, yEdge;
    public Vector2 xNormal, yNormal;

    public Voxel() { }

    public Voxel(int x, int y, float size) {
        position.x = (x + 0.5f) * size;
        position.y = (y + 0.5f) * size;

        xEdge = float.MinValue;
        yEdge = float.MinValue;
    }

    public void BecomeXDummyOf(Voxel voxel, float offset) {
        state = voxel.state;
        position = voxel.position;
        position.x += offset;
        xEdge = voxel.xEdge + offset;
        yEdge = voxel.yEdge;
        yNormal = voxel.yNormal;
    }

    public void BecomeYDummyOf(Voxel voxel, float offset) {
        state = voxel.state;
        position = voxel.position;
        position.y += offset;
        xEdge = voxel.xEdge;
        yEdge = voxel.yEdge + offset;
        xNormal = voxel.xNormal;
    }

    public void BecomeXYDummyOf(Voxel voxel, float offset) {
        state = voxel.state;
        position = voxel.position;
        position.y += offset;
        position.x += offset;
        xEdge = voxel.xEdge + offset;
        yEdge = voxel.yEdge + offset;
    }

    public Vector2 XEdgePoint {
        get {
            return new Vector2(xEdge, position.y);
        }
    }

    public Vector2 YEdgePoint {
        get {
            return new Vector2(position.x, yEdge);
        }
    }
}
