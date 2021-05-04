using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class BlockMap : EditorWindow {
    private BlockCollection blockList;
    private ReorderableList reorderableBlocks;
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
        textures = new List<Texture2D>();

        blocksMaterial.SetTexture("Textures", textureArray);
    }

    void OnGUI() {
        PrepareList();
        reorderableBlocks.DoLayoutList();
        GUILayout.Space(20f);

        blockName = EditorGUILayout.TextField("Block Name: ", blockName);
        mapColor = EditorGUILayout.ColorField("Map Color: ", mapColor);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Texture: ");
        blockTexture = (Texture2D)EditorGUILayout.ObjectField(blockTexture, typeof(Texture2D), false);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(20f);
        blocksMaterial = (Material)EditorGUILayout.ObjectField(blocksMaterial, typeof(Material), false);
        GUILayout.Space(20f);

        GUILayout.Space(20f);
        if (GUILayout.Button("Update Texture2D Array")) {
            SaveTexture2DArray();
        }

        GUILayout.Space(20f);
        if (GUILayout.Button("Refresh")) {
            blockList = BlockManager.ReadBlocks();
            reorderableBlocks = new ReorderableList(blockList.blocks,
                typeof(Block),
                true, true, true, true);
        }
    }

    private void PrepareList() {
        if (reorderableBlocks == null || reorderableBlocks.list != blockList.blocks) {
            reorderableBlocks = new ReorderableList(blockList.blocks, typeof(Block), true, true, true, true);
        }

        reorderableBlocks.elementHeight = EditorGUIUtility.singleLineHeight * 2f + 20f;

        reorderableBlocks.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Blocks", EditorStyles.boldLabel);
        };

        reorderableBlocks.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            Block block = (Block)reorderableBlocks.list[index];
            textures.Add(GetTextureFromPath(block.texturePath));
            
            EditorGUI.ObjectField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), "Texture", (UnityEngine.Object)textures[block.index], typeof(Texture2D), false);
            rect.y += 30;
            rect.height = 60;
            EditorGUI.ColorField(new Rect(rect.x, rect.y, 60, EditorGUIUtility.singleLineHeight), block.color);
            EditorGUI.TextField(new Rect(rect.x + 70, rect.y, rect.width - 110, EditorGUIUtility.singleLineHeight), block.name);
            EditorGUI.TextField(new Rect(rect.x + rect.width - 30, rect.y, 30, EditorGUIUtility.singleLineHeight), block.index.ToString());
        };
    }

    // private static Texture2D TextureField(Texture2D texture) {
    //     var result = (Texture2D)EditorGUILayout.ObjectField(texture, typeof(Texture2D), false, GUILayout.Width(70), GUILayout.Height(70));
    //     return result;
    // }

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