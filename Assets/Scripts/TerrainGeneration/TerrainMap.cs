using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TerrainMap : MonoBehaviour {
    private RenderType oldRenderType = RenderType.FullPerlin;
    public RenderType renderType = RenderType.FullPerlin;

    public GameObject map;
    [Range(64, 1028)]
    public int mapRenderResolution = 256;
    // [Range(0, 1)]
    // public float updateInterval = 0.1f;
    public int zoomInterval = 64;

    private Texture2D texture;
    private Color[] colors;
    private Material mapMaterial;
    private float stepSize;
    private Transform player;
    private TerrainNoise terrainNoise;
    private VoxelMap voxelMap;
    private bool isActive = true;

    private List<Color> colorList = new List<Color>();
    // private Dictionary<Vector2Int, int> voxelIndexPositionDictionary = new Dictionary<Vector2Int, int>();

    public enum RenderType {
        Off,
        FullPerlin,
        HeightPerlin,
        CavePerlin,
        GrassPerlin,
        LiveMap
    };

    private void Start() {
        mapMaterial = map.GetComponent<Image>().material;
        player = FindObjectOfType<PlayerController>().transform;
        terrainNoise = FindObjectOfType<TerrainNoise>();
        voxelMap = FindObjectOfType<VoxelMap>();

        BlockCollection blockList = BlockManager.ReadBlocks();
        colorList.Add(Color.black);
        foreach (Block block in blockList.blocks) {
            colorList.Add(block.color);
        }

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
            oldRenderType = renderType;
            RecalculateMap();
        }
    }

    private void NewTexture() {
        texture = new Texture2D(mapRenderResolution, mapRenderResolution, TextureFormat.RGB24, false) { filterMode = FilterMode.Point };
        texture.wrapMode = TextureWrapMode.Clamp;
        colors = new Color[mapRenderResolution * mapRenderResolution];
        stepSize = 1f / mapRenderResolution;

        RecalculateMap();
    }

    public void RecalculateMap() {
        // voxelIndexPositionDictionary.Clear();
        var position = player.position;
        var pos = new Vector3Int((int)position.x, (int)position.y, 0);
        var offset = new Vector2Int(mapRenderResolution / 2, mapRenderResolution / 2);

        for (int x = mapRenderResolution, index = 0; x > 0; x--) {
            for (var y = 0; y < mapRenderResolution; y++, index++) {
                colors[index] = Color.black;
                int pointState = FindNoise(x + pos.x - offset.x, y + pos.y - offset.y);
                if (pointState > 0) {
                    colors[index] = FindColor(pointState);
                }
            }
        }

        int radius = mapRenderResolution / zoomInterval;
        for (int x = offset.x - radius; x < offset.x + radius; x++) {
            for (int y = offset.y - radius; y < offset.y + radius; y++) {
                var playerIndex = y * mapRenderResolution + x;
                colors[playerIndex] = Color.white;
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        mapMaterial.SetTexture("MapTexture", texture);
    }

    private int FindNoise(int x, int y) {
        switch (renderType) {
            case RenderType.Off:
                return 0;
            case RenderType.FullPerlin:
                return terrainNoise.PerlinCalculate(x, y);
            case RenderType.HeightPerlin:
                return terrainNoise.Perlin1D(x, y);
            case RenderType.CavePerlin:
                return terrainNoise.Perlin2D(x, y);
            case RenderType.GrassPerlin:
                return terrainNoise.PerlinGrass(x, y);
            case RenderType.LiveMap:
                int halResolution = mapRenderResolution / 2;
                int halfChunksLength = halResolution / 8;

                int chunkX = (int)Mathf.Floor((x * 1.0f) / halfChunksLength);
                int chunkY = (int)Mathf.Floor((y * 1.0f) / halfChunksLength);

                int voxelX = (Mathf.Abs(x - (chunkX * halfChunksLength))) % 8;
                int voxelY = (Mathf.Abs(y - (chunkY * halfChunksLength))) % 8;

                if (chunkY < 0) {
                    voxelY = 8 - voxelY - 1;
                }

                //TODO: fix zooming
                Vector2Int chunkPos = new Vector2Int(chunkX, chunkY);
                if (voxelMap.existingChunks.ContainsKey(chunkPos)) {
                    VoxelChunk chunk = voxelMap.existingChunks[chunkPos];
                    Voxel voxel = chunk.voxels[(voxelY * 8) + voxelX];

                    return voxel.state;
                } else {
                    return 0;
                }
            default:
                return 1;
        }
    }

    private Color FindColor(int pointState) {
        return colorList[pointState + 1];
    }

    private void ToggleMap() {
        isActive = !isActive;
        map.SetActive(isActive);
    }
}
