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

    //TODO: connect to voxel placer / voxel shader

    [MenuItem("Window/Block Map")]
    public static void ShowWindow() {
        EditorWindow.GetWindow(typeof(BlockMap));
    }

    void OnEnable() {
        blockList = BlockManager.ReadBlocks();
    }

    void OnGUI() {
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
            BlockManager.WriteBlocks(blockList, new Block(blockList.blocks.Count, blockName, mapColor, GetPathFromTexture(blockTexture)));
            blockList.blocks.Clear();
            blockList = BlockManager.ReadBlocks();
        }
        GUILayout.EndHorizontal();
    }

    private static Texture2D TextureField(Texture2D texture) {
        var result = (Texture2D)EditorGUILayout.ObjectField(texture, typeof(Texture2D), false, GUILayout.Width(70), GUILayout.Height(70));
        return result;
    }

    private Texture2D GetTextureFromPath(string path) {
        var rawData = System.IO.File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(16, 16); // Create an empty Texture; size doesn't matter
        texture.LoadImage(rawData);
        texture.Apply();

        return texture;
    }

    private string GetPathFromTexture(Texture2D texture) {
        string path = "Assets/Resources/Blocks/";
        return path + texture.name + ".png";
    }
}