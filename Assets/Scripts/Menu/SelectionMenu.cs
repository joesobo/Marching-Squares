using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectionMenu : MonoBehaviour {
    public GameObject worldUIPrefab;
    public GameObject contentObject;

    private WorldDataHandler worldDataHandler;

    private void Awake() {
        worldDataHandler = FindObjectOfType<WorldDataHandler>();
        List<WorldData> allWorlds = worldDataHandler.LoadAllWorlds();

        int worldCount = allWorlds.Count;
        var rt = contentObject.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, 200 * worldCount);

        foreach (WorldData world in allWorlds) {
            var worldUI = Instantiate(worldUIPrefab, new Vector2(288, -400), Quaternion.identity, contentObject.transform);
            worldUI.name = world.name;

            var infoParent = worldUI.transform.GetChild(1);
            var titleUI = infoParent.GetChild(0);
            titleUI.GetComponent<Text>().text = world.name;
        }
    }
}
