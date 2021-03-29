using UnityEngine;

public struct Triangle {
#pragma warning disable 649 // disable unassigned variable warning
    public Vector2 a;
    public Vector2 b;
    public Vector2 c;
    public readonly float red;
    public readonly float green;
    public readonly float blue;

    public Triangle(Vector2 a, Vector2 b, Vector2 c, float red, float green, float blue) {
        this.a = a;
        this.b = b;
        this.c = c;
        this.red = red;
        this.green = green;
        this.blue = blue;
    }

    public Vector2 this[int i] {
        get {
            switch (i) {
                case 0:
                    return a;
                case 1:
                    return b;
                default:
                    return c;
            }
        }
    }
}