
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class FBO : UdonSharpBehaviour
{

    private RenderTexture texture;

    private int width;
    private int height;
    private float texelSizeX;
    private float texelSizeY;

    public MeshRenderer targetRenderer;

    public void Initialize(int w, int h, RenderTextureFormat format)
    {
        width = w;
        height = h;
        texelSizeX = 1.0f / width;
        texelSizeY = 1.0f / height;

        texture = new RenderTexture(width, height, 0, format);

        if (targetRenderer != null)
        {
            targetRenderer.material.mainTexture = texture;
        }
    }

    public void Blit(Material shaderMaterial, int pass)
    {
        VRCGraphics.Blit(null, texture, shaderMaterial, pass);
    }

    public Vector2 GetTexelSize()
    {
        return new Vector2(texelSizeX, texelSizeY);
    }

    public RenderTexture GetTexture()
    {
        return texture;
    }
}
