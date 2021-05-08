using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class TerrainNoise : MonoBehaviour {
    public TerrainType terrainType = TerrainType.Perlin;

    public int seed = 0;
    public bool useRandomSeed;
    public float height1, height2, height3, height4 = 0;

    [Header("Height Noise")]
    [Range(0.1f, 1)]
    public float frequency = 1f;
    [Range(1, 8)]
    public int octaves = 1;
    [Range(1f, 4f)]
    public float lacunarity = 2f;
    [Range(0f, 1f)]
    public float persistence = 0.5f;
    [Range(0, 2)]
    public float amplitude = 1f;
    [Range(0, 2)]
    public float range = 1f;

    [Header("Cave Noise")]
    [Range(0.1f, 1)]
    public float caveFrequency = 1f;
    [Range(1, 8)]
    public int caveOctaves = 1;
    [Range(0f, 2f)]
    public float caveLacunarity = 2f;
    [Range(0f, 2f)]
    public float cavePersistence = 0.5f;
    [Range(0, 2)]
    public float caveAmplitude = 1f;
    // [Range(0, 2)]
    // public float caveRange = 1f;

    private int voxelResolution, chunkResolution;

    private float noiseHeight;
    private float maxNoiseVal;
    private float minNoiseVal;
    private float halfResolution;
    [Range(0.0001f, 2)]
    public float scale;
    public Vector2 offset = Vector2.zero;
    private Vector2[] octaveOffsets;

    public enum TerrainType {
        Off,
        On,
        Random,
        RandomFull,
        Perlin,
    }

    public void Startup(int voxelResolution, int chunkResolution, Transform player) {
        this.voxelResolution = voxelResolution;
        this.chunkResolution = chunkResolution;

        if (useRandomSeed) {
            seed = (int)Random.Range(0f, 10000f);
        }
        Random.InitState(seed);

        octaveOffsets = new Vector2[caveOctaves];
        for (int i = 0; i < caveOctaves; i++) {
            float offsetX = Random.Range(-100000, 100000) + offset.x;
            float offsetY = Random.Range(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        maxNoiseVal = float.MinValue;
        minNoiseVal = float.MaxValue;

        float halfResolution = (voxelResolution * chunkResolution) / 2f;
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
                    voxel.state = PerlinNoise(chunkX, chunkY, voxel);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private int PerlinNoise(float chunkX, float chunkY, Voxel voxel) {
        var x = Mathf.RoundToInt(voxel.position.x * (voxelResolution - 1) + chunkX * voxelResolution);
        var y = Mathf.RoundToInt(voxel.position.y * (voxelResolution - 1) + chunkY * voxelResolution);

        return PerlinCalculate(x, y);
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
        var amp = amplitude;
        var noiseRange = range;
        var sum = Mathf.PerlinNoise((scaledXHeight + seed) * freq, 0);

        for (var o = 1; o < octaves; o++) {
            freq *= lacunarity;
            amp *= persistence;
            noiseRange += amp;
            sum += Mathf.PerlinNoise((scaledXHeight + seed) * freq, 0) * amp;
        }
        noiseHeight = sum / noiseRange;
        noiseHeight *= chunkResolution * voxelResolution;
        return noiseHeight;
    }

    public int Perlin2D(float x, int y) {
        var noiseVal = Mathf.InverseLerp(minNoiseVal, maxNoiseVal, Noise2D(x, y));

        // return GetBlockTypeIndex(PercentChangeBlocks(noiseVal, 100 - caveRange, BlockType.Empty, PercentChangeBlocks(noiseVal, caveRange, BlockType.Stone, BlockType.Empty)));
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
        var noiseVal = 0f;
        var freq = caveFrequency;
        var amp = caveAmplitude;

        for (var o = 0; o < caveOctaves; o++) {
            float sampleX = (scaledX) / scale * freq + octaveOffsets[o].x;
            float sampleY = (scaledY) / scale * freq + octaveOffsets[o].y;

            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
            noiseVal += perlinValue * amp;

            freq *= caveLacunarity;
            amp *= cavePersistence;
        }
        if (noiseVal > maxNoiseVal) {
            maxNoiseVal = noiseVal;
        } else if (noiseVal < minNoiseVal) {
            minNoiseVal = noiseVal;
        }

        return noiseVal;
    }

    public int PerlinGrass(float x, int y) {
        var noiseHeight = Noise1D(x);
        var noiseVal = Mathf.InverseLerp(minNoiseVal, maxNoiseVal, Noise2D(x, y));

        if (InRange(y, Mathf.RoundToInt(noiseHeight), 3)) {
            return GetBlockTypeIndex(BlockType.Grass);
            // return GetBlockTypeIndex(PercentChangeBlocks(noiseVal, 33, BlockType.Grass, BlockType.Empty));
        } else if (InRange(y, Mathf.RoundToInt(noiseHeight), 8)) {
            return GetBlockTypeIndex(PercentChangeBlocks(noiseVal, 80, BlockType.Stone, PercentChangeBlocks(noiseVal, 25, BlockType.Dirt, BlockType.Empty)));
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