using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TerrainMap : MonoBehaviour {
    private RenderType oldRenderType = RenderType.Perlin;
    public RenderType renderType = RenderType.Perlin;

    public GameObject map;
    [Range(64, 1028)]
    public int mapRenderResolution = 256;
    [Range(0, 1)]
    public float updateInterval = 0.1f;
    public int zoomInterval = 64;

    private Texture2D texture;
    private Color[] colors;
    private Material mapMaterial;
    private Transform player;
    private TerrainNoise terrainNoise;
    private VoxelMap voxelMap;
    private bool isActive = true;

    private List<Color> colorList = new List<Color>();
    private static readonly int MapTexture = Shader.PropertyToID("MapTexture");

    public enum RenderType {
        Off,
        Perlin,
        HeightPerlin,
        CavePerlin,
        GrassPerlin,
        LiveMap,
        FullMap,
        BWCave
    };

    private void Start() {
        mapMaterial = map.GetComponent<Image>().material;
        player = FindObjectOfType<PlayerController>().transform;
        terrainNoise = FindObjectOfType<TerrainNoise>();
        voxelMap = FindObjectOfType<VoxelMap>();

        RefreshColors();

        NewTexture();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.M)) {
            ToggleMap();
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            RecalculateMap();
        }

        if (Input.GetKeyDown(KeyCode.RightBracket)) {
            if (mapRenderResolution <= 1028 - zoomInterval) {
                mapRenderResolution += zoomInterval;
                NewTexture();
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftBracket)) {
            if (mapRenderResolution >= 64 + zoomInterval) {
                mapRenderResolution -= zoomInterval;
                NewTexture();
            }
        }

        if (renderType != oldRenderType) {
            RecalculateMap();
            oldRenderType = renderType;
        }

    }

    private void NewTexture() {
        texture = new Texture2D(mapRenderResolution, mapRenderResolution, TextureFormat.RGB24, false) {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        colors = new Color[mapRenderResolution * mapRenderResolution];

        RecalculateMap();
    }

    public void RecalculateMap() {
        var position = player.position;
        var pos = new Vector3Int((int)position.x, (int)position.y, 0);
        var offset = new Vector2Int(mapRenderResolution / 2, mapRenderResolution / 2);

        RefreshColors();

        for (int x = mapRenderResolution, index = 0; x > 0; x--) {
            for (var y = 0; y < mapRenderResolution; y++, index++) {
                colors[index] = Color.black;
                float pointState = FindNoise(x + pos.x - offset.x, y + pos.y - offset.y);
                if (renderType == RenderType.BWCave) {
                    pointState = terrainNoise.Perlin2D(x + pos.x - offset.x, y + pos.y - offset.y);
                }
                if (pointState > 0) {
                    colors[index] = FindColor(pointState);
                }
            }
        }

        var radius = mapRenderResolution / zoomInterval;
        for (var x = offset.x - radius; x < offset.x + radius; x++) {
            for (var y = offset.y - radius; y < offset.y + radius; y++) {
                var playerIndex = y * mapRenderResolution + x;
                colors[playerIndex] = Color.white;
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        mapMaterial.SetTexture(MapTexture, texture);
    }

    private void RefreshColors() {
        var blockList = BlockManager.ReadBlocks();
        colorList.Clear();
        foreach (var block in blockList.blocks) {
            colorList.Add(BlockManager.BlockColorDictionary[block.blockType]);
        }
    }

    private int FindNoise(int x, int y) {
        switch (renderType) {
            case RenderType.Off:
                return 0;
            case RenderType.Perlin:
                return terrainNoise.PerlinCalculate(x, y);
            case RenderType.HeightPerlin:
                return terrainNoise.Perlin1D(x, y);
            case RenderType.CavePerlin:
                return terrainNoise.PerlinCaves(y, terrainNoise.Perlin2D(x, y));
            case RenderType.GrassPerlin:
                return terrainNoise.PerlinGrass(x, y);
            case RenderType.LiveMap:
                return LiveNoise(x, y);
            case RenderType.FullMap:
                var state = LiveNoise(x, y);
                if (state == -1) {
                    state = terrainNoise.PerlinCalculate(x, y);
                }
                return state;
            default:
                return 1;
        }
    }

    private int LiveNoise(int x, int y) {
        const int halfChunksLength = 8;

        var chunkX = (int)Mathf.Floor((x * 1.0f) / halfChunksLength);
        var chunkY = (int)Mathf.Floor((y * 1.0f) / halfChunksLength);

        var voxelX = (Mathf.Abs(x - (chunkX * halfChunksLength))) % 8;
        var voxelY = (Mathf.Abs(y - (chunkY * halfChunksLength))) % 8;

        var chunkPos = new Vector2Int(chunkX, chunkY);
        if (voxelMap.existingChunks.ContainsKey(chunkPos)) {
            var chunk = voxelMap.existingChunks[chunkPos];
            var voxel = chunk.voxels[(voxelY * 8) + voxelX];

            return voxel.state;
        } else {
            return -1;
        }
    }

    private Color FindColor(float pointState) {
        return renderType == RenderType.BWCave ? Color.Lerp(Color.black, Color.white, pointState) : colorList[(int)pointState];
    }

    private void ToggleMap() {
        isActive = !isActive;
        map.SetActive(isActive);
    }
}
