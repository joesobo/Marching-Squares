using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WorldDataHandler : MonoBehaviour {
    public InputField worldTitleField;
    public InputField seedField;

    public void SaveWorldTitle() {
        Debug.Log(worldTitleField.text);
        PlayerPrefs.SetString("worldName", worldTitleField.text);
        PlayerPrefs.Save();
    }

    public void SaveSeed() {
        PlayerPrefs.SetInt("seed", int.Parse(seedField.text));
        PlayerPrefs.Save();
    }

    public void Play() {
        if (worldTitleField.text != null) {
            SceneManager.LoadScene(1);
        }
    }
}
