using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TerrainMap : MonoBehaviour {
    public GameObject map;
    public int mapRenderResolution = 512;
    [Range(0,1)]
    public float updateInterval = 0.1f;

    private Texture2D texture;
    private Color[] colors;
    private Material mapMaterial;
    private float stepSize;
    private Transform player;
    private TerrainNoise terrainNoise;

    void Awake() {
        mapMaterial = map.GetComponent<Image>().material;
        texture = new Texture2D(mapRenderResolution, mapRenderResolution, TextureFormat.RGB24, false) { filterMode = FilterMode.Point };
        texture.wrapMode = TextureWrapMode.Clamp;
        colors = new Color[mapRenderResolution * mapRenderResolution];
        stepSize = 1f / mapRenderResolution;
        player = FindObjectOfType<PlayerController>().transform;
        terrainNoise = FindObjectOfType<TerrainNoise>();
    }

    public void RecalculateMap() {
        var position = player.position;
        var pos = new Vector3Int((int)position.x, (int)position.y, 0);
        var offset = new Vector2Int(mapRenderResolution / 2, mapRenderResolution / 2);

        for (int x = mapRenderResolution, index = 0; x > 0; x--) {
            for (var y = 0; y < mapRenderResolution; y++, index++) {
                colors[index] = Color.black;
                if (terrainNoise.Perlin(x + pos.x - offset.x, y + pos.y - offset.y) > 0) {
                    colors[index] = new Color((x + 0.5f) * stepSize, (y + 0.5f) * stepSize, 0f);
                }
            }
        }

        int radius = mapRenderResolution / 128;
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
}
