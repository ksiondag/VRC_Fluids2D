
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DoubleFBO : UdonSharpBehaviour {

    private RenderTexture readTexture;
    private RenderTexture writeTexture;

    private int width;
    private int height;
    private float texelSizeX;
    private float texelSizeY;

    public MeshRenderer targetRenderer;

    public void Initialize(int w, int h, RenderTextureFormat format) {
        width = w;
        height = h;
        texelSizeX = 1.0f / width;
        texelSizeY = 1.0f / height;

        readTexture = new RenderTexture(width, height, 0, format);
        writeTexture = new RenderTexture(width, height, 0, format);

        if (targetRenderer != null) {
            targetRenderer.material.mainTexture = readTexture;
        }
    }

    public void Blit(Material shaderMaterial) {
        VRCGraphics.Blit(null, writeTexture, shaderMaterial);
    }

    public void Swap() {
        RenderTexture temp = readTexture;
        readTexture = writeTexture;
        writeTexture = temp;

        if (targetRenderer != null) {
            targetRenderer.material.mainTexture = readTexture;
        }
    }

    public Vector2 GetTexelSize() {
        return new Vector2(texelSizeX, texelSizeY);
    }

    public RenderTexture GetTexture() {
        return readTexture;
    }
}
