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
    private Color[] colors;
    private Material mapMaterial;
    public int mapRenderResolution = 512;

    private int voxelResolution, chunkResolution;
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

        mapMaterial = map.GetComponent<Renderer>().material;
        texture = new Texture2D(mapRenderResolution, mapRenderResolution) { filterMode = FilterMode.Point };
        colors = new Color[mapRenderResolution * mapRenderResolution];
        RecalculateMap();

        if (useRandomSeed) {
            seed = (int)Random.Range(0f, 10000f);
        }
    }

    public void RecalculateMap() {
        var position = player.position;
        var pos = new Vector3Int((int)position.x, (int)position.y, 0);
        var offset = new Vector2Int(mapRenderResolution / 2, mapRenderResolution / 2);

        for (int x = mapRenderResolution, index = 0; x > 0; x--) {
            for (var y = 0; y < mapRenderResolution; y++, index++) {
                colors[index] = Color.black;
                if (Perlin(x + pos.x - offset.x, y + pos.y - offset.y) > 0) {
                    colors[index] = Color.white;
                }
            }
        }

        int radius = mapRenderResolution / 128;
        for (int x = offset.x - radius; x < offset.x + radius; x++) {
            for (int y = offset.y - radius; y < offset.y + radius; y++) {
                var playerIndex = y * mapRenderResolution + x;
                colors[playerIndex] = Color.red;
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
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

        texture.Apply();
        mapMaterial.SetTexture("MapTexture", texture);
    }

    private void PerlinNoise(float chunkX, float chunkY, Voxel voxel) {
        var x = Mathf.RoundToInt(voxel.position.x * (voxelResolution - 1) + chunkX * voxelResolution);
        var y = Mathf.RoundToInt(voxel.position.y * (voxelResolution - 1) + chunkY * voxelResolution);

        voxel.state = Perlin(x, y);
    }

    private int Perlin(int x, int y) {
        var scaledX = x / scaleTerrainNoise / voxelResolution;
        var scaledY = y / scaleTerrainNoise / voxelResolution;

        var scaledXHeight = x / scaleHeightNoise / voxelResolution;

        var noiseVal = Mathf.PerlinNoise(scaledX + seed, scaledY + seed);
        var maxHeight = Mathf.PerlinNoise(scaledXHeight + seed, 0) * (chunkResolution * voxelResolution);

        var voxelState = 0;

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

    private static bool InRange(float input, float value, float range) {
        return input + range > value && input - range < value;
    }
}