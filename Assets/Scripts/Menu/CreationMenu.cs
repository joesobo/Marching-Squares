using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CreationMenu : MonoBehaviour {
    public InputField worldNameField;
    public InputField seedField;
    public Text errorText;

    private string worldName;
    private int seed;
    private WorldDataHandler worldDataHandler;

    private void Awake() {
        worldDataHandler = FindObjectOfType<WorldDataHandler>();
        errorText.text = "";
    }

    public void UpdateWorldTitle() {
        worldName = worldNameField.text;
        errorText.text = "";
    }

    public void UpdateSeed() {
        seed = int.Parse(seedField.text);
    }

    public void Play() {
        if (worldNameField.text != null) {
            if (worldDataHandler.ContainsWorld(worldName)) {
                errorText.text = "Error: World Name is already taken";
            } else {
                worldDataHandler.NewWorld(new WorldData(worldName, seed));
                SceneManager.LoadScene(1);
            }
        }
    }
}
