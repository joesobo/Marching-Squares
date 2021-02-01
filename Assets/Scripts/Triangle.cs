using UnityEngine;

struct Triangle {
#pragma warning disable 649 // disable unassigned variable warning
        public Vector2 a;
        public Vector2 b;
        public Vector2 c;
        public float red;
        public float green;
        public float blue;

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