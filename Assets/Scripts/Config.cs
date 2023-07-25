
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Config : UdonSharpBehaviour {
    public int SIM_RESOLUTION = 128;
    public int DYE_RESOLUTION = 1024;
    public int CAPTURE_RESOLUTION = 512;
    public float DENSITY_DISSIPATION = 1f;
    public float VELOCITY_DISSIPATION = 0.2f;
    public float PRESSURE = 0.8f;
    public int PRESSURE_ITERATIONS = 20;
    public int CURL = 30;
    public float SPLAT_RADIUS = 0.01f;
    public int SPLAT_FORCE = 6000;
    public bool SHADING = true;
    public bool COLORFUL = true;
    public int COLOR_UPDATE_SPEED = 10;
    public bool PAUSED = false;
    public Color BACK_COLOR = new Color(0, 0, 0, 1);
    public bool TRANSPARENT = false;
    public bool BLOOM = true;
    public int BLOOM_ITERATIONS = 8;
    public int BLOOM_RESOLUTION = 256;
    public float BLOOM_INTENSITY = 0.8f;
    public float BLOOM_THRESHOLD = 0.6f;
    public float BLOOM_SOFT_KNEE = 0.7f;
    public bool SUNRAYS = true;
    public int SUNRAYS_RESOLUTION = 196;
    public float SUNRAYS_WEIGHT = 1.0f;
}
