using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CreationMenu : MonoBehaviour {
    public InputField worldTitleField;
    public InputField seedField;

    private string worldName;
    private int seed;
    private WorldDataHandler worldDataHandler;

    private void Awake() {
        worldDataHandler = FindObjectOfType<WorldDataHandler>();
    }

    public void UpdateWorldTitle() {
        worldName = worldTitleField.text;
    }

    public void UpdateSeed() {
        seed = int.Parse(seedField.text);
    }

    public void Play() {
        if (worldTitleField.text != null) {
            worldDataHandler.NewWorld(new WorldData(worldName, seed));
            SceneManager.LoadScene(1);
        }
    }
}
