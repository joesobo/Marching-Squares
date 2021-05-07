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

        if (useRandomSeed) {
            seed = (int)Random.Range(0f, 10000f);
        }
        Random.InitState(seed);
    }

    public void GenerateNoiseValues(VoxelChunk chunk) {
        var position = chunk.transform.position;
        var chunkX = position.x;
        var chunkY = position.y;

        foreach (var voxel in chunk.voxels) {
            switch (terrainType) {
                case TerrainType.Off:
                    voxel.state = GetBlockTypeIndex(BlockType.Empty);
                    break;
                case TerrainType.On:
                    voxel.state = GetBlockTypeIndex(BlockType.Stone);
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

        voxel.state = PerlinCalculate(x, y);
    }

    public int PerlinCalculate(int x, int y) {
        var voxelState = 0;

        voxelState = Perlin1D(x, y);
        if (voxelState == 0) return voxelState;
        if (PerlinGrass(x, y) != -1) {
            return PerlinGrass(x, y);
        }
        voxelState = Perlin2D(x, y);

        return voxelState;
    }

    public int Perlin1D(float x, int y) {
        var noiseHeight = Noise1D(x);

        return y > noiseHeight ? 0 : 1;
    }

    private float Noise1D(float x) {
        var scaledXHeight = x / 1f / voxelResolution;
        var noiseHeight = 0f;
        var freq = frequency;
        var amplitude = 1f;
        var range = 1f;
        var sum = Mathf.PerlinNoise((scaledXHeight + seed) * freq, 0);

        for (var o = 1; o < octaves; o++) {
            freq *= lacunarity;
            amplitude *= persistence;
            range += amplitude;
            sum += Mathf.PerlinNoise((scaledXHeight + seed) * freq, 0) * amplitude;
        }
        noiseHeight = sum / range;
        noiseHeight *= chunkResolution * voxelResolution;
        return noiseHeight;
    }

    public int Perlin2D(float x, int y) {
        var noiseVal = Noise2D(x, y);

        if (y < height1) {
            return GetBlockTypeIndex(PercentChangeBlocks(noiseVal, 33, BlockType.Rock, BlockType.Empty));
        } else if (y < height2) {
            return GetBlockTypeIndex(PercentChangeBlocks(noiseVal, 66, BlockType.Rock, PercentChangeBlocks(noiseVal, 33, BlockType.Stone, BlockType.Empty)));
        } else if (y < height3) {
            return GetBlockTypeIndex(PercentChangeBlocks(noiseVal, 66, BlockType.Dirt, PercentChangeBlocks(noiseVal, 33, BlockType.Stone, BlockType.Empty)));
        } else {
            return GetBlockTypeIndex(PercentChangeBlocks(noiseVal, 50, BlockType.Dirt, PercentChangeBlocks(noiseVal, 33, BlockType.Stone, BlockType.Empty)));
        }
    }

    private float Noise2D(float x, int y) {
        var scaledX = x / 1f / voxelResolution;
        var scaledY = y / 1f / voxelResolution;
        var noiseVal = Mathf.PerlinNoise(scaledX + seed, scaledY + seed);
        return noiseVal;
    }

    public int PerlinGrass(float x, int y) {
        var noiseHeight = Noise1D(x);
        var noiseVal = Noise2D(x, y);

        if (InRange(y, Mathf.RoundToInt(noiseHeight), 3)) {
            return GetBlockTypeIndex(PercentChangeBlocks(noiseVal, 33, BlockType.Grass, BlockType.Empty));
        } else if (InRange(y, Mathf.RoundToInt(noiseHeight), 8)) {
            return GetBlockTypeIndex(PercentChangeBlocks(noiseVal, 80, BlockType.Stone, PercentChangeBlocks(noiseVal, 33, BlockType.Dirt, BlockType.Empty)));
        } else {
            return -1;
        }
    }

    private BlockType PercentChangeBlocks(float noiseVal, int percent, BlockType block1, BlockType block2) {
        return noiseVal > (percent / 100f) ? block1 : block2;
    }

    private static int GetBlockTypeIndex(BlockType type) {
        return BlockManager.blockIndexDictionary[type];
    }

    private static bool InRange(float input, float value, float range) {
        return input + range > value && input - range < value;
    }
}