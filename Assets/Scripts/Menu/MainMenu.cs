using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour {
    private WorldDataHandler worldDataHandler;

    public GameObject continueButton;

    private void Awake() {
        worldDataHandler = FindObjectOfType<WorldDataHandler>();
    }

    public void Reset() {
        PlayerPrefs.SetString("CurrentWorld", "");
        worldDataHandler.RemoveAllWorlds();
        continueButton.GetComponent<ContinueButton>().UpdateState();
    }
}
