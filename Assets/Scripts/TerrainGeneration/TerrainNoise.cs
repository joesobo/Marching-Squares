using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class TerrainNoise : MonoBehaviour {
    public TerrainType terrainType = TerrainType.Perlin;

    // [DrawIf(nameof(terrainType), TerrainType.Perlin, ComparisonType.Equals)]
    [Range(0.3f, 100)]
    public float scaleHeightNoise;

    // [DrawIf(nameof(terrainType), TerrainType.Perlin, ComparisonType.Equals)]
    [Range(0.3f, 100)]
    public float scaleTerrainNoise;

    // [DrawIf(nameof(terrainType), TerrainType.Perlin, ComparisonType.Equals)]
    public int seed = 0;

    // [DrawIf(nameof(terrainType), TerrainType.Perlin, ComparisonType.Equals)]
    public bool useRandomSeed;

    // [DrawIf(nameof(terrainType), TerrainType.Perlin, ComparisonType.Equals)]
    public float height1, height2, height3, height4 = 0;

    public GameObject map;
    private Texture2D texture;
    private int mapIndex = 0;

    private int voxelResolution, chunkResolution, mapRenderResolution;
    private float halfSize;
    private Transform player;

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

        mapRenderResolution = 256;
        texture = new Texture2D(mapRenderResolution, mapRenderResolution);
        texture.filterMode = FilterMode.Point;
        mapIndex = 0;
        RecalculateMap();

        if (useRandomSeed) {
            seed = (int)Random.Range(0f, 10000f);
        }
    }

    public void RecalculateMap() {
        Vector3Int pos = new Vector3Int((int)player.position.x, (int)player.position.y, 0);
        Vector2Int offset = new Vector2Int(mapRenderResolution / 2, mapRenderResolution / 2);

        for (int x = 0; x < mapRenderResolution; x++) {
            for (int y = 0; y < mapRenderResolution; y++) {
                texture.SetPixel(x, y, Color.black);
                if (Perlin(x + pos.x - offset.x, y + pos.y - offset.y) > 0) {
                    texture.SetPixel(x, y, Color.white);
                }
            }
        }
        texture.Apply();
    }

    public void GenerateNoiseValues(VoxelChunk chunk) {
        var position = chunk.transform.position;
        float chunkX = position.x;
        float chunkY = position.y;

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

        texture.Apply();
        map.GetComponent<Renderer>().material.SetTexture("MapTexture", texture);
    }

    private void PerlinNoise(float chunkX, float chunkY, Voxel voxel) {
        int x = Mathf.RoundToInt(voxel.position.x * (voxelResolution - 1) + chunkX * voxelResolution);
        int y = Mathf.RoundToInt(voxel.position.y * (voxelResolution - 1) + chunkY * voxelResolution);

        voxel.state = Perlin(x, y);
    }

    private int Perlin(int x, int y) {
        float scaledX = x / scaleTerrainNoise / voxelResolution;
        float scaledY = y / scaleTerrainNoise / voxelResolution;

        float scaledXHeight = x / scaleHeightNoise / voxelResolution;

        float noiseVal = Mathf.PerlinNoise(scaledX + seed, scaledY + seed);
        float maxHeight = Mathf.PerlinNoise(scaledXHeight + seed, 0) * (chunkResolution * voxelResolution);

        int voxelState = 0;

        if (y > maxHeight) {
            voxelState = 0;
        } else {
            if (y < height1 * chunkResolution * voxelResolution) {
                //random 3/0
                voxelState = noiseVal > 0.33f ? 3 : 0;
            } else if (y < height2 * chunkResolution * voxelResolution) {
                //random 3/1/0
                voxelState = noiseVal > 0.65f ? 3 : noiseVal > 0.33f ? 1 : 0;
            } else if (y < height3 * chunkResolution * voxelResolution) {
                //random 2/1/0
                voxelState = noiseVal > 0.66f ? 2 : noiseVal > 0.33f ? 1 : 0;
            } else {
                voxelState = noiseVal > 0.5f ? 2 : 1;
            }

            if (InRange(y, Mathf.RoundToInt(maxHeight), 3)) {
                voxelState = 4;
            } else if (InRange(y, Mathf.RoundToInt(maxHeight), 8)) {
                voxelState = noiseVal > 0.2f ? 2 : 1;
            }
        }

        return voxelState;
    }

    private bool InRange(float input, float value, float range) {
        if (input + range > value && input - range < value) {
            return true;
        }

        return false;
    }
}