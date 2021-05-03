using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BlockMap : EditorWindow {
    private BlockCollection blockList;
    private string blockName;
    private Color mapColor;
    private Texture2D blockTexture;
    private List<Texture2D> textures;
    private Material blocksMaterial;
    private Texture2DArray textureArray;

    [MenuItem("Window/Block Map")]
    public static void ShowWindow() {
        EditorWindow.GetWindow(typeof(BlockMap));
    }

    void OnEnable() {
        blockList = BlockManager.ReadBlocks();
        blocksMaterial.SetTexture("Textures", textureArray);
    }

    void OnGUI() {
        GUILayout.Space(20f);
        blocksMaterial = (Material)EditorGUILayout.ObjectField(blocksMaterial, typeof(Material), false);
        GUILayout.Space(20f);

        textures = new List<Texture2D>();
        GUILayout.Label("Blocks", EditorStyles.boldLabel);
        for (int i = 0; i < blockList.blocks.Count; i++) {
            Block block = blockList.blocks[i];
            textures.Add(GetTextureFromPath(block.texturePath));

            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            TextureField(textures[i]);
            GUILayout.BeginVertical();
            GUILayout.Label("Index: " + block.index);
            GUILayout.Label("Name: " + block.name);
            EditorGUILayout.ColorField("Map Color: ", block.color);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
        GUILayout.Space(30f);

        blockName = EditorGUILayout.TextField("Block Name: ", blockName);
        mapColor = EditorGUILayout.ColorField("Map Color: ", mapColor);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Texture: ");
        blockTexture = (Texture2D)EditorGUILayout.ObjectField(blockTexture, typeof(Texture2D), false);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(20f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-")) {
            BlockManager.RemoveBlock(blockList);
            blockList.blocks.Clear();
            blockList = BlockManager.ReadBlocks();
        }
        if (GUILayout.Button("+")) {
            if (!blockTexture) {
                blockTexture = null;
            }

            BlockManager.WriteBlocks(blockList, new Block(blockList.blocks.Count + 1, blockName, mapColor, GetPathFromTexture(blockTexture)));
            blockList.blocks.Clear();
            blockList = BlockManager.ReadBlocks();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(20f);
        if (GUILayout.Button("Update Texture2D Array")) {
            SaveTexture2DArray();
        }

        GUILayout.Space(20f);
        if (GUILayout.Button("Refresh")) {
            blockList = BlockManager.ReadBlocks();
        }
    }

    private static Texture2D TextureField(Texture2D texture) {
        var result = (Texture2D)EditorGUILayout.ObjectField(texture, typeof(Texture2D), false, GUILayout.Width(70), GUILayout.Height(70));
        return result;
    }

    private Texture2D GetTextureFromPath(string path) {
        try {
            var rawData = System.IO.File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(16, 16); // Create an empty Texture; size doesn't matter
            texture.LoadImage(rawData);
            texture.Apply();
            return texture;
        } catch (Exception e) {
            Debug.Log("Error reading texture from path " + e);
            return null;
        }
    }

    private string GetPathFromTexture(Texture2D texture) {
        string path = "Assets/Resources/Blocks/";
        return path + texture.name + ".png";
    }

    private void SaveTexture2DArray() {
        string path = "Assets/Resources/Blocks/TextureArray.Asset";

        Texture2D t = textures[0];
        textureArray = new Texture2DArray(t.width, t.height, textures.Count, t.format, t.mipmapCount > 1);
        textureArray.anisoLevel = t.anisoLevel;
        textureArray.filterMode = t.filterMode;
        textureArray.wrapMode = t.wrapMode;

        for (int i = 0; i < textures.Count; i++) {
            for (int m = 0; m < t.mipmapCount; m++) {
                Graphics.CopyTexture(textures[i], 0, m, textureArray, i, m);
            }
        }

        if (AssetDatabase.LoadAssetAtPath<Texture2DArray>(path) != null) { AssetDatabase.DeleteAsset(path); }
        AssetDatabase.CreateAsset(textureArray, path);

        AssetDatabase.Refresh();

        blocksMaterial.SetTexture("Textures", textureArray);
    }
}