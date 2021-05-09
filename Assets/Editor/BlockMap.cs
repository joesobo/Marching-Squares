using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

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
    private static readonly int Textures = Shader.PropertyToID("Textures");

    [MenuItem("Window/Block Map")]
    public static void ShowWindow() {
        GetWindow(typeof(BlockMap));
    }

    private void OnEnable() {
        Refresh();
    }

    private void Reset() {
        Refresh();
    }

    private void OnGUI() {
        EditorGUILayout.LabelField("Warning: Remember to add to BlockManager Enum", EditorStyles.boldLabel);

        PrepareList();
        reorderableList.DoLayoutList();

        GUILayout.Space(10f);
        blocksMaterial = (Material)EditorGUILayout.ObjectField(blocksMaterial, typeof(Material), false);

        GUILayout.Space(20f);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Texture2D Array")) {
            SaveTexture2DArray();
        }
        if (GUILayout.Button("Save JSON structure")) {
            BlockManager.WriteBlocks(blockList, null);
        }
        EditorGUILayout.EndHorizontal();

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

        reorderableList.drawHeaderCallback = (rect) => {
            EditorGUI.LabelField(rect, "Blocks", EditorStyles.boldLabel);
        };

        reorderableList.drawElementCallback = (rect, index, isActive, isFocused) => {
            var block = (Block)reorderableList.list[index];

            textures.Add(GetTextureFromPath(block.texturePath));

            if (index < textures.Count) {
                textures[index] = (Texture2D)EditorGUI.ObjectField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), "Texture", textures[index], typeof(Texture2D), false);
                if (block.texturePath == "" && GetPathFromTexture(textures[index]) != "") {
                    block.texturePath = GetPathFromTexture(textures[index]);
                }
            }
            rect.y += 20;
            rect.height = 30;
            block.color = EditorGUI.ColorField(new Rect(rect.x, rect.y, 60, EditorGUIUtility.singleLineHeight), block.color);
            block.blockType = (BlockType)EditorGUI.EnumPopup(new Rect(rect.x + 70, rect.y, rect.width - 110, EditorGUIUtility.singleLineHeight), block.blockType);
            EditorGUI.TextField(new Rect(rect.x + rect.width - 30, rect.y, 30, EditorGUIUtility.singleLineHeight), index.ToString());
        };

        reorderableList.onSelectCallback = (list) => {
            selectedIndex = list.index;
        };

        reorderableList.onReorderCallback = (list) => {
            BlockManager.WriteBlocks(blockList, null);
            Refresh();
        };

        reorderableList.onAddCallback = (list) => {
            var block = new Block(BlockType.Empty, Color.black, "");
            BlockManager.WriteBlocks(blockList, block);
            Refresh();
        };

        reorderableList.onRemoveCallback = (list) => {
            if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to delete this block?", "Yes", "No")) {
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
                BlockManager.RemoveBlock(blockList, selectedIndex);
            }
        };

        if (textures.Count > blockList.blocks.Count) {
            textures = new List<Texture2D>();
        }
    }

    private void Refresh() {
        blockList = BlockManager.ReadBlocks();
        textures = new List<Texture2D>();
        reorderableList = new ReorderableList(blockList.blocks,
            typeof(Block),
            true, true, true, true);
        foreach (var block in blockList.blocks) {
            textures.Add(GetTextureFromPath(block.texturePath));
        }
    }

    private static Texture2D GetTextureFromPath(string path) {
        try {
            var rawData = System.IO.File.ReadAllBytes(path);
            var texture = new Texture2D(16, 16); // Create an empty Texture; size doesn't matter
            texture.LoadImage(rawData);
            texture.Apply();
            return texture;
        } catch (Exception e) {
            return null;
        }
    }

    private static string GetPathFromTexture(Object texture) {
        const string path = "Assets/Resources/Blocks/";

        if (texture == null || texture.name == "") {
            return "";
        }

        return path + texture.name + ".png";
    }

    private void SaveTexture2DArray() {
        const string path = "Assets/Resources/Blocks/TextureArray.Asset";

        var t = textures[1];
        textureArray = new Texture2DArray(t.width, t.height, textures.Count, t.format, t.mipmapCount > 1) {
            anisoLevel = t.anisoLevel,
            filterMode = t.filterMode,
            wrapMode = t.wrapMode
        };

        for (var i = 1; i < textures.Count; i++) {
            for (var m = 0; m < t.mipmapCount; m++) {
                Graphics.CopyTexture(textures[i], 0, m, textureArray, i, m);
            }
        }

        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(textureArray, path);

        AssetDatabase.Refresh();

        blocksMaterial.SetTexture(Textures, textureArray);
    }
}