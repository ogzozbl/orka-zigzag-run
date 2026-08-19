using UnityEngine;

// Topun ardında altın toz izi. Treadmill mimaride TrailRenderer çalışmaz:
// top world-space'te sabit durduğu için iz noktaları üst üste biner. Bunun
// yerine partiküller worldRoot'un uzayında simüle edilir (custom simulation
// space) — dünya geriye kayarken partiküller de onunla birlikte kayar.
public class GoldTrail : MonoBehaviour
{
    static readonly Color gold = new Color(0.788f, 0.651f, 0.42f); // #C9A66B

    ParticleSystem ps;
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
        // İz kısa ömürlü ve seyrek: partiküller worldRoot uzayında sabitlendiği için
        // yol boyunca birikiyor — uzun ömür/yüksek sayı olunca bloom'a girip ekranı
        // beyaz bir lekeye çeviriyordu.
        main.startLifetime = 0.5f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.13f);
        main.startColor = gold;
        main.maxParticles = 120;

        var emission = ps.emission;
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
            new[] { new GradientAlphaKey(0.45f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLife.color = gradient;

        var psRenderer = go.GetComponent<ParticleSystemRenderer>();
        psRenderer.material = MakeParticleMaterial();
        psRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        psRenderer.receiveShadows = false;

        ps.Play();
    }

    void Update()
    {
        if (ball == null || ps == null) return;

        // Modülü her kare taze çekiyoruz (class alanında cache'lemiyoruz) — Play modunda
        // script yeniden derlenirse (platform değişimi vb.) cache'li struct bozulup
        // "Do not create your own module instances" hatasına yol açabiliyordu
        // ball.IsGameStarted ölünce true kalmaya devam ediyor (sıfırlanmıyor) —
        // ball.enabled ise Freeze()'de false olur, o yüzden ikisini birden kontrol
        // ediyoruz. Aksi halde ölen topun tozu ekranda sonsuza kadar birikiyordu.
        var emission = ps.emission;
        bool active = ball.IsGameStarted && ball.enabled;
        emission.enabled = active;
        if (active)
        {
            // İz yoğunluğu hızla birlikte artar — oyuncu ivmelenmeyi gözle de hisseder
            float t = Mathf.InverseLerp(6f, ball.maxSpeed, ball.speed);
            emission.rateOverTime = Mathf.Lerp(10f, 26f, t);
        }
    }

    // Orb toplama patlaması da aynı particle sistemini kullanır
    public void EmitBurst(Vector3 worldPos, int count)
    {
        if (ps == null || worldRoot == null) return;
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
