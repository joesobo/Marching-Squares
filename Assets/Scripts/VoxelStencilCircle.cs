using UnityEngine;

public class VoxelStencilCircle : VoxelStencil {
	
	private int sqrRadius;
	
	public override void Initialize (int fillType, int radius) {
		base.Initialize (fillType, radius);
		sqrRadius = radius * radius;
	}
	
	public override int Apply (int x, int y, int voxel) {
		x -= centerX;
		y -= centerY;
		if (x * x + y * y <= sqrRadius) {
			return fillType;
		}
		return voxel;
	}
}