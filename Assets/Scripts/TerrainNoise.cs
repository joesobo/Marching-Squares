using UnityEngine;

public class TerrainNoise : MonoBehaviour {
    [Range(0.3f, 100)]
    public float scaleHeightNoise;
    [Range(0.3f, 100)]
    public float scaleTerrainNoise;
    public float seed = 0;
    public bool useRandomSeed;
    public TerrainType terrainType = TerrainType.Perlin;

    private float[,] noiseMap;

    private int voxelResolution, chunkResolution;
    private float halfSize;

    private Transform player;

    public float height1, height2, height3, height4 = 0;

    public enum TerrainType {
        Off,
        On,
        Random,
        RandomFull,
        Perlin
    }

    public void Startup(int voxelResolution, int chunkResolution, Transform player) {
        this.voxelResolution = voxelResolution;
        this.chunkResolution = chunkResolution;
        this.player = player;
        halfSize = 0.5f * chunkResolution;

        noiseMap = new float[voxelResolution * chunkResolution, voxelResolution * chunkResolution];
    }

    public void GenerateNoise(VoxelChunk chunk) {
        if (useRandomSeed) {
            seed = Random.Range(0f, 10000f);
        }

        GenerateTerrainValues(chunk);
    }

    private void GenerateTerrainValues(VoxelChunk chunk) {
        float centeredChunkX = chunk.transform.position.x;
        float centeredChunkY = chunk.transform.position.y;

        foreach (Voxel voxel in chunk.voxels) {
            switch (terrainType) {
                case TerrainType.Off:
                    voxel.state = 0;
                    break;
                case TerrainType.On:
                    voxel.state = 1;
                    break;
                case TerrainType.Random:
                    voxel.state = UnityEngine.Random.Range(0, 5);
                    break;
                case TerrainType.RandomFull:
                    voxel.state = UnityEngine.Random.Range(1, 5);
                    break;
                case TerrainType.Perlin:
                    PerlinNoise(centeredChunkX, centeredChunkY, voxel);
                    break;
            }
        }
    }

    private void PerlinNoise(float centeredChunkX, float centeredChunkY, Voxel voxel) {
        int x = Mathf.RoundToInt(voxel.position.x * (voxelResolution - 1) + centeredChunkX * voxelResolution);
        int y = Mathf.RoundToInt(voxel.position.y * (voxelResolution - 1) + centeredChunkY * voxelResolution);

        float scaledX = x / scaleTerrainNoise / voxelResolution;
        float scaledY = y / scaleTerrainNoise / voxelResolution;

        float scaledXHeight = x / scaleHeightNoise / voxelResolution;

        float noiseVal = Mathf.PerlinNoise(scaledX + seed, scaledY + seed);
        float maxHeight = Mathf.PerlinNoise(scaledXHeight + seed, 0) * (chunkResolution * voxelResolution);

        if (y > maxHeight) {
            voxel.state = 0;
        } else {
            if (y < height1 * chunkResolution * voxelResolution) {
                //random 3/0
                voxel.state = noiseVal > 0.33f ? 3 : 0;
            } else if (y < height2 * chunkResolution * voxelResolution) {
                //random 3/1/0
                voxel.state = noiseVal > 0.65f ? 3 : noiseVal > 0.33f ? 1 : 0;
            } else if (y < height3 * chunkResolution * voxelResolution) {
                //random 2/1/0
                voxel.state = noiseVal > 0.66f ? 2 : noiseVal > 0.33f ? 1 : 0;
            } else {
                voxel.state = noiseVal > 0.5f ? 2 : 1;
            }

            if (InRange(y, Mathf.RoundToInt(maxHeight), 3)) {
                voxel.state = 4;
            } else if (InRange(y, Mathf.RoundToInt(maxHeight), 8)) {
                voxel.state = noiseVal > 0.2f ? 2 : 1;
            }
        }
    }

    private bool InRange(float input, float value, float range) {
        if (input + range > value && input - range < value) {
            return true;
        }

        return false;
    }
}
