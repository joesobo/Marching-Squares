using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldManager : MonoBehaviour {
    public string worldPath;
    public string worldName;
    public int seed;

    private void Awake() {
        worldPath = Application.persistentDataPath + "/worlds/";
        worldName = PlayerPrefs.GetString("worldName");
        seed = PlayerPrefs.GetInt("seed");
        Debug.Log(1);
    }
}
