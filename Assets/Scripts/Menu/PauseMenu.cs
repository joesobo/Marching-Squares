using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour {
    private bool activeState = false;
    private bool needsUpdating = true;

    private ChunkSaveLoadManager chunkSaveLoadManager;

    private void Awake() {
        chunkSaveLoadManager = FindObjectOfType<ChunkSaveLoadManager>();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            activeState = !activeState;
            needsUpdating = true;
        }

        if (needsUpdating) {
            foreach (Transform child in transform) {
                child.gameObject.SetActive(activeState);
            }
        }

        needsUpdating = false;
    }

    public void Deactivate() {
        activeState = false;
        needsUpdating = true;
    }

    public void OpenControls() {

    }

    public void OpenOptions() {
        
    }

    public void SaveAndQuit() {
        chunkSaveLoadManager.SaveAllChunks();

        SceneManager.LoadScene(0);
    }
}
