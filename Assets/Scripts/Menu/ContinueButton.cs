using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ContinueButton : MonoBehaviour {
    private Button button;
    private string currentWorldName;
    private WorldDataHandler worldDataHandler;

    private void Awake() {
        button = GetComponent<Button>();
        worldDataHandler = FindObjectOfType<WorldDataHandler>();

        UpdateState();
    }

    public void UpdateState() {
        currentWorldName = PlayerPrefs.GetString("CurrentWorld");
        if (currentWorldName != "") {
            button.interactable = true;
        } else {
            button.interactable = false;
        }
    }

    public void Continue() {
        worldDataHandler.currentWorld = currentWorldName;
        SceneManager.LoadScene(1);
    }
}
