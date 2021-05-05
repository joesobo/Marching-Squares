using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class TerrainNoise : MonoBehaviour {
    public TerrainType terrainType = TerrainType.Perlin;

    public int seed = 0;
    public bool useRandomSeed;
    public float height1, height2, height3, height4 = 0;

    [Range(0.1f, 1)]
    public float frequency = 1f;

    [Range(1, 8)]
    public int octaves = 1;

    [Range(1f, 4f)]
    public float lacunarity = 2f;

    [Range(0f, 1f)]
    public float persistence = 0.5f;

    [Range(0, 2)]
    public float exponent = 0.5f;

    private int voxelResolution, chunkResolution;
    private float halfSize;
    private Transform player;
    private TerrainMap terrainMap;

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

        if (useRandomSeed) {
            seed = (int)Random.Range(0f, 10000f);
        }
        Random.InitState(seed);

        terrainMap = FindObjectOfType<TerrainMap>();
    }

    public void GenerateNoiseValues(VoxelChunk chunk) {
        var position = chunk.transform.position;
        var chunkX = position.x;
        var chunkY = position.y;

        foreach (var voxel in chunk.voxels) {
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
                    PerlinNoise(chunkX, chunkY, voxel);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void PerlinNoise(float chunkX, float chunkY, Voxel voxel) {
        var x = Mathf.RoundToInt(voxel.position.x * (voxelResolution - 1) + chunkX * voxelResolution);
        var y = Mathf.RoundToInt(voxel.position.y * (voxelResolution - 1) + chunkY * voxelResolution);

        voxel.state = Perlin(x, y);
    }

    public int Perlin(int x, int y) {
        var scaledX = x / 1f / voxelResolution;
        var scaledY = y / 1f / voxelResolution;

        var scaledXHeight = x / 1f / voxelResolution;

        var noiseVal = Mathf.PerlinNoise(scaledX + seed, scaledY + seed);
        var noiseHeight = 0f;
        var voxelState = 0;

        float freq = frequency;
        float amplitude = 1f;
        float range = 1f;
        float sum = Mathf.PerlinNoise((scaledXHeight + seed) * freq, 0);

        for (int o = 1; o < octaves; o++) {
            freq *= lacunarity;
            amplitude *= persistence;
            range += amplitude;
            sum += Mathf.PerlinNoise((scaledXHeight + seed) * freq, 0) * amplitude;
        }
        noiseHeight = sum / range;
        noiseHeight *= chunkResolution * voxelResolution;

        if (y > noiseHeight) {
            voxelState = 0;
        } else {
            if (y < height1) {
                voxelState = GetBlockTypeIndex(PercentChangeBlocks(noiseVal, 33, BlockType.Rock, BlockType.Empty));
            } else if (y < height2) {
                voxelState = GetBlockTypeIndex(PercentChangeBlocks(noiseVal, 66, BlockType.Rock, PercentChangeBlocks(noiseVal, 33, BlockType.Stone, BlockType.Empty)));
            } else if (y < height3) {
                voxelState = GetBlockTypeIndex(PercentChangeBlocks(noiseVal, 66, BlockType.Dirt, PercentChangeBlocks(noiseVal, 33, BlockType.Stone, BlockType.Empty)));
            } else {
                voxelState = GetBlockTypeIndex(PercentChangeBlocks(noiseVal, 50, BlockType.Dirt, PercentChangeBlocks(noiseVal, 33, BlockType.Stone, BlockType.Empty)));
            }

            if (InRange(y, Mathf.RoundToInt(noiseHeight), 3)) {
                voxelState = GetBlockTypeIndex(PercentChangeBlocks(noiseVal, 33, BlockType.Grass, BlockType.Empty));
            } else if (InRange(y, Mathf.RoundToInt(noiseHeight), 8)) {
                voxelState = GetBlockTypeIndex(PercentChangeBlocks(noiseVal, 80, BlockType.Stone, PercentChangeBlocks(noiseVal, 33, BlockType.Dirt, BlockType.Empty)));
            }
        }

        return voxelState;
    }

    private BlockType PercentChangeBlocks(float noiseVal, int percent, BlockType block1, BlockType block2) {
        return noiseVal > (percent / 100f) ? block1 : block2;
    }

    private int GetBlockTypeIndex(BlockType type) {
        return BlockManager.blockIndexDictionary[type];
    }

    private static bool InRange(float input, float value, float range) {
        return input + range > value && input - range < value;
    }
}