using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldManager : MonoBehaviour {
    public string worldPath;
    public string worldName;
    public int seed;
    public string last_played;

    private WorldDataHandler worldDataHandler;

    private void Awake() {
        worldPath = Application.persistentDataPath + "/worlds/";
        worldDataHandler = FindObjectOfType<WorldDataHandler>();

        var worldData = worldDataHandler.LoadCurrentWorld();
        worldName = worldData.name;
        seed = worldData.seed;
        last_played = worldData.last_played;
    }
}
