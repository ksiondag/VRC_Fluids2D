
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Pourer : UdonSharpBehaviour
{
    public Color color = new Color(0, 0, 0, 0);

    // Public for scripting purposes, but should not be modified in Unity Editor
    public Vector2 texcoord;
    public Vector2 prevTexcoord;
    public Vector2 delta;
    public bool pouring;
    public bool moved;

    void Start() {
        texcoord = Vector2.zero;
        prevTexcoord = Vector2.zero;
        delta = Vector2.zero;
        pouring = false;
        moved = false;
    }

    public void Initialize(Vector2 point) {
        texcoord = point;
        prevTexcoord = point;
        delta = Vector2.zero;
        pouring = true;
        moved = false;
    }

    public void Move(Vector2 point) {
        prevTexcoord = texcoord;
        texcoord = point;
        delta = texcoord - prevTexcoord;
        moved = Mathf.Abs(delta.x) > 0.01 || Mathf.Abs(delta.y) > 0.01;
    }

    public void Reset() {
        pouring = false;
        moved = false;
    }
}
