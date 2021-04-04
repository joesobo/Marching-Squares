using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectionMenu : MonoBehaviour {
    public GameObject worldUIPrefab;
    public GameObject contentObject;

    private WorldDataHandler worldDataHandler;
    private List<WorldData> allWorlds;

    private void OnEnable() {
        worldDataHandler = FindObjectOfType<WorldDataHandler>();
        Setup();
    }

    private void Setup() {
        allWorlds = worldDataHandler.LoadAllWorlds();
        allWorlds.Sort(SortByTime);

        //setup content & clear old children
        foreach (Transform child in contentObject.transform) {
            Destroy(child.gameObject);
        }
        int worldCount = allWorlds.Count;
        var rt = contentObject.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, 200 * worldCount);

        CreateWorlds(allWorlds);
    }

    private static int SortByTime(WorldData w1, WorldData w2) {
        return DateTime.Parse(w2.last_played).ToBinary().CompareTo(DateTime.Parse(w1.last_played).ToBinary());
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

            var timeUI = infoParent.GetChild(1);
            timeUI.GetComponent<Text>().text = DateTime.Parse(world.last_played).ToShortDateString() + " " + DateTime.Parse(world.last_played).ToShortTimeString();

            var deleteButton = worldUI.transform.GetChild(2).GetComponent<Button>();
            deleteButton.onClick.AddListener(delegate {
                worldDataHandler.RemoveWorld(world.name);
                Setup();
            });
        }
    }

    public void SelectWorld(int index) {
        worldDataHandler.currentWorld = allWorlds[index].name;
    }

    public void LoadWorld() {
        PlayerPrefs.SetString("CurrentWorld", worldDataHandler.currentWorld);
        PlayerPrefs.Save();

        SceneManager.LoadScene(1);
    }
}
