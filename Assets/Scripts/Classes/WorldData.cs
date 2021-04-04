using System;

[Serializable]
public class WorldData {
    public string name;
    public int seed;

    public WorldData(string name, int seed) {
        this.name = name;
        this.seed = seed;
    }
}