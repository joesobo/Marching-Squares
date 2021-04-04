using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectionMenu : MonoBehaviour {
    public GameObject worldUIPrefab;
    public GameObject contentObject;

    private WorldDataHandler worldDataHandler;
    private List<WorldData> allWorlds;

    private void Awake() {
        worldDataHandler = FindObjectOfType<WorldDataHandler>();
        allWorlds = worldDataHandler.LoadAllWorlds();

        //setup content
        int worldCount = allWorlds.Count;
        var rt = contentObject.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, 200 * worldCount);

        CreateWorlds(allWorlds);
    }

    private void CreateWorlds(List<WorldData> allWorlds) {
        for (int i = 0; i < allWorlds.Count; i++) {
            WorldData world = allWorlds[i];
            var worldUI = Instantiate(worldUIPrefab, new Vector2(288, -400), Quaternion.identity, contentObject.transform);
            worldUI.name = world.name;

            var worldButton = worldUI.GetComponent<Button>();
            int index = new int();
            index = i;
            worldButton.onClick.AddListener(delegate { SelectWorld(index); });

            var infoParent = worldUI.transform.GetChild(1);
            var titleUI = infoParent.GetChild(0);
            titleUI.GetComponent<Text>().text = world.name;
        }
    }

    public void SelectWorld(int index) {
        worldDataHandler.currentWorld = allWorlds[index].name;
    }

    public void LoadWorld() {
        SceneManager.LoadScene(1);
    }
}
