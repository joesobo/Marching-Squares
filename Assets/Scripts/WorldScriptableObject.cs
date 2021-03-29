using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/WorldScriptableObject", order = 1)]
public class WorldScriptableObject : ScriptableObject {
    public string pathName;
    public int seed;
}