using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using System;

public class WorldDataHandler : MonoBehaviour {
    private string path;
    private readonly BinaryFormatter bf = new BinaryFormatter();

    public string currentWorld;

    private void Awake() {
        path = Application.persistentDataPath + "/worlds/";
        Directory.CreateDirectory(path);

        DontDestroyOnLoad(this.gameObject);
    }

    public void NewWorld(WorldData data) {
        currentWorld = data.name;
        string worldPath = path + "/" + currentWorld;
        Directory.CreateDirectory(worldPath);
        string worldDataPath = worldPath + "/" + currentWorld + "_world.sav";

        var stream = new FileStream(worldDataPath, FileMode.OpenOrCreate);
        bf.Serialize(stream, data);
    }


    public WorldData LoadCurrentWorld() {
        string worldPath = path + "/" + currentWorld;
        string worldDataPath = worldPath + "/" + currentWorld + "_world.sav";

        var stream = new FileStream(worldDataPath, FileMode.Open);

        return (WorldData)bf.Deserialize(stream);
    }

    public List<WorldData> LoadAllWorlds() {
        List<WorldData> worldDataList = new List<WorldData>();

        foreach (string dir in Directory.EnumerateDirectories(path)) {
            var parts = dir.Split('/');
            string worldName = parts[parts.Length - 1];
            string worldPath = path + "/" + worldName;
            string worldDataPath = worldPath + "/" + worldName + "_world.sav";

            var stream = new FileStream(worldDataPath, FileMode.Open);
            worldDataList.Add((WorldData)bf.Deserialize(stream));
        }

        return worldDataList;
    }
}