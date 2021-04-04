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
        WorldDataHandler[] handlers = FindObjectsOfType<WorldDataHandler>();

        foreach (WorldDataHandler handler in handlers) {
            if (this != handler) {
                Destroy(handler.gameObject);
            }
        }

        path = Application.persistentDataPath + "/worlds/";
        Directory.CreateDirectory(path);

        DontDestroyOnLoad(gameObject);
    }

    public bool ContainsWorld(string worldName) {
        string worldPath = path + "/" + worldName;

        return Directory.Exists(worldPath);
    }

    public void NewWorld(WorldData data) {
        currentWorld = data.name;
        PlayerPrefs.SetString("CurrentWorld", data.name);
        PlayerPrefs.Save();

        string worldPath = path + "/" + currentWorld;
        Directory.CreateDirectory(worldPath);
        string worldDataPath = worldPath + "/" + currentWorld + "_world.sav";

        var stream = new FileStream(worldDataPath, FileMode.Create);
        bf.Serialize(stream, data);

        stream.Close();
    }

    public WorldData LoadCurrentWorld() {
        string worldPath = path + "/" + currentWorld;
        string worldDataPath = worldPath + "/" + currentWorld + "_world.sav";

        var stream = new FileStream(worldDataPath, FileMode.Open);
        var worldData = (WorldData)bf.Deserialize(stream);

        stream.Close();

        return worldData;
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
            stream.Close();
        }

        return worldDataList;
    }

    public void UpdateWorld() {
        string worldPath = path + "/" + currentWorld;
        string worldDataPath = worldPath + "/" + currentWorld + "_world.sav";

        var stream = new FileStream(worldDataPath, FileMode.Open);
        var worldData = (WorldData)bf.Deserialize(stream);

        worldData.last_played = DateTime.Now.ToString();

        stream.SetLength(0);
        bf.Serialize(stream, worldData);

        stream.Close();
    }

    public void RemoveWorld(string worldName) {
        string worldPath = path + "/" + worldName;
        Directory.Delete(worldPath, true);
    }

    public void RemoveAllWorlds() {
        foreach (string dir in Directory.EnumerateDirectories(path)) {
            var parts = dir.Split('/');
            string worldName = parts[parts.Length - 1];
            string worldPath = path + "/" + worldName;

            Directory.Delete(worldPath, true);
        }
    }
}