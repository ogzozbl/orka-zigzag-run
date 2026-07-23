using UnityEngine;

// Topun ardında altın toz izi. Treadmill mimaride TrailRenderer çalışmaz:
// top world-space'te sabit durduğu için iz noktaları üst üste biner. Bunun
// yerine partiküller worldRoot'un uzayında simüle edilir (custom simulation
// space) — dünya geriye kayarken partiküller de onunla birlikte kayar.
public class GoldTrail : MonoBehaviour
{
    static readonly Color gold = new Color(0.788f, 0.651f, 0.42f); // #C9A66B

    ParticleSystem ps;
    ParticleSystem.EmissionModule emission;
    Transform worldRoot;
    BallController ball;

    public void Init(Transform worldRoot)
    {
        this.worldRoot = worldRoot;
        ball = GetComponent<BallController>();
        Build();
    }

    void Build()
    {
        var go = new GameObject("GoldTrailFX");
        go.transform.SetParent(transform, false);
        ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Custom;
        main.customSimulationSpace = worldRoot;
        main.startLifetime = 0.9f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);
        main.startColor = gold;
        main.maxParticles = 500;

        emission = ps.emission;
        emission.rateOverTime = 40f;
        emission.enabled = false;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.18f;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(gold, 0f), new GradientColorKey(gold, 1f) },
            new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLife.color = gradient;

        var psRenderer = go.GetComponent<ParticleSystemRenderer>();
        psRenderer.material = MakeParticleMaterial();
        psRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        psRenderer.receiveShadows = false;

        ps.Play();
    }

    void Update()
    {
        if (ball == null) return;
        emission.enabled = ball.IsGameStarted;
        if (ball.IsGameStarted)
        {
            // İz yoğunluğu hızla birlikte artar — oyuncu ivmelenmeyi gözle de hisseder
            float t = Mathf.InverseLerp(6f, ball.maxSpeed, ball.speed);
            emission.rateOverTime = Mathf.Lerp(25f, 75f, t);
        }
    }

    // Orb toplama patlaması da aynı particle sistemini kullanır
    public void EmitBurst(Vector3 worldPos, int count)
    {
        var p = new ParticleSystem.EmitParams
        {
            position = worldRoot.InverseTransformPoint(worldPos)
        };
        for (int i = 0; i < count; i++)
        {
            p.velocity = Random.insideUnitSphere * 2.5f;
            ps.Emit(p, 1);
        }
    }

    static Material MakeParticleMaterial()
    {
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.mainTexture = ParticleTex.MakeSoftCircle();
        return mat;
    }
}
