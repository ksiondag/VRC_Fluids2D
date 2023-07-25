
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
    public Material splatMaterial;
    public Material curlMaterial;
    public Material vorticityMaterial;
    public Material divergenceMaterial;
    public Material clearMaterial;
    public Material pressureMaterial;
    public Material gradientSubtractMaterial;
    public Material advectionMaterial;

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
        curlMaterial.SetVector("_TexelSize", velocity.GetTexelSize());
        curlMaterial.SetTexture("_MainTex", velocity.GetTexture());
        curl.Blit(curlMaterial);

        vorticityMaterial.SetVector("_TexelSize", velocity.GetTexelSize());
        vorticityMaterial.SetTexture("_MainTex", velocity.GetTexture());
        vorticityMaterial.SetTexture("_CurlTex", curl.GetTexture());
        vorticityMaterial.SetInt("_Curl", config.CURL);
        vorticityMaterial.SetFloat("_DeltaTime", dt);
        velocity.Blit(vorticityMaterial);
        velocity.Swap();

        divergenceMaterial.SetVector("_TexelSize", velocity.GetTexelSize());
        divergenceMaterial.SetTexture("_MainTex", velocity.GetTexture());
        divergence.Blit(divergenceMaterial);

        clearMaterial.SetTexture("_MainTex", pressure.GetTexture());
        clearMaterial.SetFloat("_Value", config.PRESSURE);
        pressure.Blit(clearMaterial);
        pressure.Swap();

        pressureMaterial.SetVector("_TexelSize", velocity.GetTexelSize());
        pressureMaterial.SetTexture("_MainTex", divergence.GetTexture());
        for (int i = 0; i < config.PRESSURE_ITERATIONS; i++)
        {
            pressureMaterial.SetTexture("_PressureTex", pressure.GetTexture());
            pressure.Blit(pressureMaterial);
            pressure.Swap();
        }

        gradientSubtractMaterial.SetVector("_TexelSize", velocity.GetTexelSize());
        gradientSubtractMaterial.SetTexture("_MainTex", pressure.GetTexture());
        gradientSubtractMaterial.SetTexture("_VelocityTex", velocity.GetTexture());
        velocity.Blit(gradientSubtractMaterial);
        velocity.Swap();

        advectionMaterial.SetVector("_TexelSize", velocity.GetTexelSize());
        // TODO: Not sure if I want to check for linear filtering support
        // if (!ext.supportLinearFiltering)
        // advectionMaterial.SetVector("_DyeTexelSize", dye.GetTexelSize());        
        advectionMaterial.SetTexture("_VelocityTex", velocity.GetTexture());
        advectionMaterial.SetTexture("_MainTex", velocity.GetTexture());
        advectionMaterial.SetFloat("_DeltaTime", dt);
        advectionMaterial.SetFloat("_Dissipation", config.VELOCITY_DISSIPATION);
        velocity.Blit(advectionMaterial);
        velocity.Swap();

        // TODO: Not sure if I want to check for linear filtering support
        // if (!ext.supportLinearFiltering)
        // advectionMaterial.SetVector("_DyeTexelSize", dye.GetTexelSize());        
        advectionMaterial.SetTexture("_VelocityTex", velocity.GetTexture());
        advectionMaterial.SetTexture("_MainTex", dye.GetTexture());
        advectionMaterial.SetFloat("_Dissipation", config.DENSITY_DISSIPATION);
        dye.Blit(advectionMaterial);
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
        Splat_private(pourer.texcoord, delta, pourer.color);
    }

    public void Splat(Vector3 position, Vector3 force, Color color)
    {
        Vector2 uvPosition = ConvertToUV(position);
        Vector2 uvForce = ConvertToUV(force);
        Splat_private(uvPosition, uvForce, color);
    }

    private void Splat_private(Vector2 position, Vector2 force, Color color)
    {
        Vector2 forceDirection = force.normalized;
        float radius = config.SPLAT_RADIUS / 100f;
        // Vector2 splatPosition = position + forceDirection * config.SPLAT_RADIUS;
        splatMaterial.SetTexture("_MainTex", velocity.GetTexture());
        // TODO: I don't think aspect ratio should ever be non-1, but maybe handle it anyways?
        splatMaterial.SetFloat("_AspectRatio", 1.0f);
        splatMaterial.SetVector("_Point", new Vector4(position.x, position.y, 0, 0));
        splatMaterial.SetVector("_Color", new Vector4(force.x, force.y, 0, 0));
        splatMaterial.SetFloat("_Radius", radius);
        velocity.Blit(splatMaterial);
        velocity.Swap();

        splatMaterial.SetTexture("_MainTex", dye.GetTexture());
        // splatMaterial.SetVector("_Point", new Vector4(splatPosition.x, splatPosition.y, 0, 0));
        splatMaterial.SetVector("_Color", new Vector4(color.r, color.g, color.b, 0));
        splatMaterial.SetFloat("_Radius", radius / 10f);
        dye.Blit(splatMaterial);
        dye.Swap();
    }
}
