using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class BlockMap : EditorWindow {
    private BlockCollection blockList;
    private ReorderableList reorderableList;
    private string blockName;
    private Color mapColor;
    private Texture2D blockTexture;
    private List<Texture2D> textures;
    private Material blocksMaterial;
    private Texture2DArray textureArray;

    private int selectedIndex;

    [MenuItem("Window/Block Map")]
    public static void ShowWindow() {
        EditorWindow.GetWindow(typeof(BlockMap));
    }

    void OnEnable() {
        Refresh();
    }

    void OnGUI() {
        PrepareList();
        reorderableList.DoLayoutList();
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
            Refresh();
        }
    }

    private void PrepareList() {
        if (reorderableList == null || reorderableList.list != blockList.blocks) {
            reorderableList = new ReorderableList(blockList.blocks, typeof(Block), true, true, true, true);
        }

        reorderableList.elementHeight = EditorGUIUtility.singleLineHeight * 2f + 10f;

        reorderableList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Blocks", EditorStyles.boldLabel);
        };

        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            Block block = (Block)reorderableList.list[index];
            textures.Add(GetTextureFromPath(block.texturePath));

            EditorGUI.ObjectField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), "Texture", (UnityEngine.Object)textures[index], typeof(Texture2D), false);
            rect.y += 20;
            rect.height = 30;
            block.color = EditorGUI.ColorField(new Rect(rect.x, rect.y, 60, EditorGUIUtility.singleLineHeight), block.color);
            block.name = EditorGUI.TextField(new Rect(rect.x + 70, rect.y, rect.width - 110, EditorGUIUtility.singleLineHeight), block.name);
            EditorGUI.TextField(new Rect(rect.x + rect.width - 30, rect.y, 30, EditorGUIUtility.singleLineHeight), index.ToString());
        };

        reorderableList.onSelectCallback = (ReorderableList list) => {
            selectedIndex = list.index;
        };

        reorderableList.onReorderCallback = (ReorderableList list) => {
            BlockManager.WriteBlocks(blockList, null);
            Refresh();
        };

        reorderableList.onAddCallback = (ReorderableList list) => {
            Block block = new Block("", Color.black, null);
            BlockManager.WriteBlocks(blockList, block);
            Refresh();
        };

        reorderableList.onRemoveCallback = (ReorderableList list) => {
            if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to delete this block?", "Yes", "No")) {
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
                BlockManager.RemoveBlock(blockList, selectedIndex);
            }
        };


        //TODO: handle update element
    }

    private void Refresh() {
        blockList = BlockManager.ReadBlocks();
        textures = new List<Texture2D>();
        reorderableList = new ReorderableList(blockList.blocks,
            typeof(Block),
            true, true, true, true);
        foreach (Block block in blockList.blocks) {
            textures.Add(GetTextureFromPath(block.texturePath));
        }
    }

    private Texture2D GetTextureFromPath(string path) {
        try {
            var rawData = System.IO.File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(16, 16); // Create an empty Texture; size doesn't matter
            texture.LoadImage(rawData);
            texture.Apply();
            return texture;
        } catch (Exception e) {
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

static class IListExtensions {
    public static void Swap<T>(
        this IList<T> list,
        int firstIndex,
        int secondIndex
    ) {
        Contract.Requires(list != null);
        Contract.Requires(firstIndex >= 0 && firstIndex < list.Count);
        Contract.Requires(secondIndex >= 0 && secondIndex < list.Count);
        if (firstIndex == secondIndex) {
            return;
        }
        T temp = list[firstIndex];
        list[firstIndex] = list[secondIndex];
        list[secondIndex] = temp;
    }
}