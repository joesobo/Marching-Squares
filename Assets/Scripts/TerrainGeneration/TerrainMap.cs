using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TerrainMap : MonoBehaviour {
    public GameObject map;
    [Range(64, 1028)]
    public int mapRenderResolution = 512;
    [Range(0, 1)]
    public float updateInterval = 0.1f;
    public int zoomInterval = 128;

    private Texture2D texture;
    private Color[] colors;
    private Material mapMaterial;
    private float stepSize;
    private Transform player;
    private TerrainNoise terrainNoise;
    private bool isActive = true;

    public List<Color> colorList = new List<Color>();

    private void Awake() {
        mapMaterial = map.GetComponent<Image>().material;
        player = FindObjectOfType<PlayerController>().transform;
        terrainNoise = FindObjectOfType<TerrainNoise>();

        NewTexture();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.M)) {
            ToggleMap();
        }

        if (Input.GetKeyDown(KeyCode.RightBracket)) {
            if (mapRenderResolution <= 1028 - zoomInterval) {
                mapRenderResolution += zoomInterval;
            }
            NewTexture();
        }

        if (Input.GetKeyDown(KeyCode.LeftBracket)) {
            if (mapRenderResolution >= 64 + zoomInterval) {
                mapRenderResolution -= zoomInterval;
            }
            NewTexture();
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
        var position = player.position;
        var pos = new Vector3Int((int)position.x, (int)position.y, 0);
        var offset = new Vector2Int(mapRenderResolution / 2, mapRenderResolution / 2);

        for (int x = mapRenderResolution, index = 0; x > 0; x--) {
            for (var y = 0; y < mapRenderResolution; y++, index++) {
                colors[index] = Color.black;
                int pointState = terrainNoise.Perlin(x + pos.x - offset.x, y + pos.y - offset.y);
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

    private Color FindColor(int pointState) {
        switch (pointState) {
            case 1:
                return colorList[1];
            case 2:
                return colorList[2];
            case 3:
                return colorList[3];
            case 4:
                return colorList[4];
            default:
                return colorList[0];
        }
    }

    private void ToggleMap() {
        isActive = !isActive;
        map.SetActive(isActive);
    }
}
