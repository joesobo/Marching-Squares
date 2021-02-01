using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainNoise : MonoBehaviour {
    [Range(0.3f, 100)]
    public float scaleNoise;
    public float seed = 0;
    public bool useRandomSeed;
    public TerrainType terrainType = TerrainType.Perlin;

    private MapDisplay mapDisplay;
    private float[,] noiseMap;

    private int voxelResolution, chunkResolution;
    private float halfSize;

    public enum TerrainType {
        Off,
        On,
        Random,
        RandomFull,
        Perlin
    }

    public void Startup(int voxelResolution, int chunkResolution) {
        this.voxelResolution = voxelResolution;
        this.chunkResolution = chunkResolution;
        halfSize = 0.5f * chunkResolution;

        if (!mapDisplay) {
            mapDisplay = FindObjectOfType<MapDisplay>();
        }

        noiseMap = new float[voxelResolution * chunkResolution, voxelResolution * chunkResolution];
    }

    public void GenerateNoise(VoxelChunk[] chunks) {
        if (useRandomSeed) {
            seed = Random.Range(0f, 10000f);
        }

        foreach (VoxelChunk chunk in chunks) {
            GenerateTerrainValues(chunk);
        }

        if (mapDisplay) { mapDisplay.DrawNoiseMap(noiseMap); }
    }

    private void GenerateTerrainValues(VoxelChunk chunk) {
        float centeredChunkX = chunk.transform.position.x + halfSize;
        float centeredChunkY = chunk.transform.position.y + halfSize;

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

        float scaledX = x / scaleNoise / voxelResolution;
        float scaledY = y / scaleNoise / voxelResolution;

        noiseMap[(int)x, (int)y] = Mathf.PerlinNoise(scaledX + seed, scaledY + seed);
        voxel.state = Mathf.PerlinNoise(scaledX + seed, scaledY + seed) > 0.5f ? 0 : Mathf.RoundToInt(Random.Range(1, 5));
    }
}
