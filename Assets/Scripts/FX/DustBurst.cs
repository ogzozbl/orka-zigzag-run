using UnityEngine;

// Tek seferlik, kısa ömürlü toz patlaması — karo düşmeye başladığı anda tetiklenir.
// GoldTrail'in aksine tek kullanımlık: kendi GameObject'inde doğar, oynar, kendini yok eder.
public static class DustBurst
{
    static Material sharedMat;

    public static void Emit(Vector3 worldPos, Color color)
    {
        var go = new GameObject("DustBurst");
        go.transform.position = worldPos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.startLifetime = 0.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.32f);
        main.startColor = color;
        main.maxParticles = 24;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLife.color = gradient;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        // Üç eksen de AYNI modda (TwoConstants) olmalı — Unity karışık mod kabul etmiyor
        vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.y = new ParticleSystem.MinMaxCurve(-2.5f, -1f); // yer çekimi hissi
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var psRenderer = go.GetComponent<ParticleSystemRenderer>();
        psRenderer.material = GetMaterial();
        psRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        ps.Play();
        Object.Destroy(go, main.startLifetime.constantMax + 0.3f);
    }

    static Material GetMaterial()
    {
        if (sharedMat != null) return sharedMat;
        sharedMat = new Material(Shader.Find("Sprites/Default"));
        sharedMat.mainTexture = ParticleTex.MakeSoftCircle(32);
        return sharedMat;
    }
}
