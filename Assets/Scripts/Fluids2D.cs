﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Fluids2D : UdonSharpBehaviour
{
    private Dye dye;
    private Velocity velocity;
    private Divergence divergence;
    private Curl curl;
    private Pressure pressure;

    private Pourer pourer;
    private Config config;

    // Materials used for applying shaders (not sure how to apply shader without a material)
    public Material fluidsMaterial;

    private int SPLAT = 0;
    private int CURL = 1;
    private int VORTICITY = 2;
    private int DIVERGENCE = 3;
    private int CLEAR = 4;
    private int PRESSURE = 5;
    private int GRADIENT_SUBTRACT = 6;
    private int ADVECTION = 7;

    void Start()
    {
        config = GetComponent<Config>();

        dye = GetComponent<Dye>();
        dye.Initialize(config.DYE_RESOLUTION, config.DYE_RESOLUTION, RenderTextureFormat.ARGB32);

        velocity = GetComponent<Velocity>();
        velocity.Initialize(config.SIM_RESOLUTION, config.SIM_RESOLUTION, RenderTextureFormat.RGFloat);

        divergence = GetComponent<Divergence>();
        divergence.Initialize(config.SIM_RESOLUTION, config.SIM_RESOLUTION, RenderTextureFormat.RFloat);

        curl = GetComponent<Curl>();
        curl.Initialize(config.SIM_RESOLUTION, config.SIM_RESOLUTION, RenderTextureFormat.RFloat);

        pressure = GetComponent<Pressure>();
        pressure.Initialize(config.SIM_RESOLUTION, config.SIM_RESOLUTION, RenderTextureFormat.RFloat);

        pourer = GetComponent<Pourer>();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        // dt = Mathf.Min(dt, 0.016666f);
        ApplyInputs();
        if (!config.PAUSED)
        {
            Step(dt);
        }

        // TODO: Need to do the Render part for realz, because of the drawColor bit
    }

    private Vector2 ConvertToUV(Vector3 point)
    {
        Vector3 localPoint = this.gameObject.transform.InverseTransformPoint(point);

        // Convert local coordinates to UV coordinates
        Vector2 uvPoint;
        uvPoint.x = 1 - (localPoint.x + 5f) / 10f;
        uvPoint.y = 1 - (localPoint.z + 5f) / 10f;
        return uvPoint;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 point = collision.GetContact(0).point;
        Vector2 uvPoint = ConvertToUV(point);
        pourer.Initialize(uvPoint);
    }

    private void OnCollisionStay(Collision collision)
    {
        Vector3 point = collision.GetContact(0).point;
        Vector2 uvPoint = ConvertToUV(point);
        pourer.Move(uvPoint);
    }

    private void OnCollisionExit(Collision collision)
    {
        pourer.Reset();
    }


    void ApplyInputs()
    {
        if (pourer.moved)
        {
            pourer.moved = false;
            SplatPourer();
        }
    }

    void Step(float dt)
    {
        // CURL
        fluidsMaterial.SetTexture("_MainTex", velocity.GetTexture());
        fluidsMaterial.SetVector("_TexelSize", velocity.GetTexelSize());
        curl.Blit(fluidsMaterial, CURL);

        // VORTICITY
        fluidsMaterial.SetTexture("_MainTex", velocity.GetTexture());
        fluidsMaterial.SetTexture("_CalcTex", curl.GetTexture());
        fluidsMaterial.SetVector("_TexelSize", velocity.GetTexelSize());
        fluidsMaterial.SetFloat("_DeltaTime", dt);
        fluidsMaterial.SetInt("_Curl", config.CURL);
        velocity.Blit(fluidsMaterial, VORTICITY);
        velocity.Swap();

        // DIVERGENCE
        fluidsMaterial.SetTexture("_MainTex", velocity.GetTexture());
        fluidsMaterial.SetVector("_TexelSize", velocity.GetTexelSize());
        divergence.Blit(fluidsMaterial, DIVERGENCE);

        // CLEAR
        fluidsMaterial.SetTexture("_MainTex", pressure.GetTexture());
        fluidsMaterial.SetFloat("_Value", config.PRESSURE);
        pressure.Blit(fluidsMaterial, CLEAR);
        pressure.Swap();

        // PRESSURE
        fluidsMaterial.SetTexture("_MainTex", divergence.GetTexture());
        fluidsMaterial.SetVector("_TexelSize", velocity.GetTexelSize());
        for (int i = 0; i < config.PRESSURE_ITERATIONS; i++)
        {
            fluidsMaterial.SetTexture("_CalcTex", pressure.GetTexture());
            pressure.Blit(fluidsMaterial, PRESSURE);
            pressure.Swap();
        }

        // GRADIENT_SUBTRACT
        fluidsMaterial.SetTexture("_MainTex", pressure.GetTexture());
        fluidsMaterial.SetTexture("_CalcTex", velocity.GetTexture());
        fluidsMaterial.SetVector("_TexelSize", velocity.GetTexelSize());
        velocity.Blit(fluidsMaterial, GRADIENT_SUBTRACT);
        velocity.Swap();

        // ADVECTION
        fluidsMaterial.SetTexture("_MainTex", velocity.GetTexture());
        fluidsMaterial.SetTexture("_CalcTex", velocity.GetTexture());
        fluidsMaterial.SetVector("_TexelSize", velocity.GetTexelSize());
        // TODO: Not sure if I want to check for linear filtering support
        // if (!ext.supportLinearFiltering)
        // fluidsMaterial.SetVector("_DyeTexelSize", dye.GetTexelSize());        
        fluidsMaterial.SetFloat("_DeltaTime", dt);
        fluidsMaterial.SetFloat("_Dissipation", config.VELOCITY_DISSIPATION);
        velocity.Blit(fluidsMaterial, ADVECTION);
        velocity.Swap();

        // TODO: Not sure if I want to check for linear filtering support
        // if (!ext.supportLinearFiltering)
        // fluidsMaterial.SetVector("_DyeTexelSize", dye.GetTexelSize());        
        fluidsMaterial.SetTexture("_CalcTex", velocity.GetTexture());
        fluidsMaterial.SetTexture("_MainTex", dye.GetTexture());
        fluidsMaterial.SetFloat("_Dissipation", config.DENSITY_DISSIPATION);
        dye.Blit(fluidsMaterial, ADVECTION);
        dye.Swap();
    }

    void Render()
    {

    }

    public void SplatPourer()
    {
        if (pourer == null || config == null)
        {
            return;
        }
        Vector2 delta = pourer.delta * config.SPLAT_FORCE;
        Splat(pourer.texcoord, delta, pourer.color);
    }

    public void Splat(Vector3 position, Vector3 force, Color color)
    {
        Vector2 uvPosition = ConvertToUV(position);
        Vector2 uvForce = ConvertToUV(force);
        Splat(uvPosition, uvForce, color);
    }

    private void Splat(Vector2 position, Vector2 force, Color color)
    {
        Vector2 forceDirection = force.normalized;
        float radius = config.SPLAT_RADIUS / 100f;
        // Vector2 splatPosition = position + forceDirection * config.SPLAT_RADIUS;
        // SPLAT
        fluidsMaterial.SetTexture("_MainTex", velocity.GetTexture());
        // TODO: I don't think aspect ratio should ever be non-1, but maybe handle it anyways?
        fluidsMaterial.SetFloat("_AspectRatio", 1.0f);
        fluidsMaterial.SetVector("_Point", new Vector4(position.x, position.y, 0, 0));
        fluidsMaterial.SetVector("_Color", new Vector4(force.x, force.y, 0, 0));
        fluidsMaterial.SetFloat("_Radius", radius);
        velocity.Blit(fluidsMaterial, SPLAT);
        velocity.Swap();

        fluidsMaterial.SetTexture("_MainTex", dye.GetTexture());
        // fluidsMaterial.SetVector("_Point", new Vector4(splatPosition.x, splatPosition.y, 0, 0));
        fluidsMaterial.SetVector("_Color", new Vector4(color.r, color.g, color.b, 0));
        fluidsMaterial.SetFloat("_Radius", radius / 10f);
        dye.Blit(fluidsMaterial, SPLAT);
        dye.Swap();
    }
}
