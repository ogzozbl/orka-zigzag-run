using UnityEngine;

// Projede hiç ses asset'i yok; efektler runtime'da PCM örnekleriyle üretilir
// (GoldTrail'in runtime doku üretimiyle aynı yaklaşım) — ek asset bağımlılığı yok.
public static class Sfx
{
    const int sampleRate = 44100;

    static AudioSource source;
    static AudioClip tapClip, collectClip, deathClip, milestoneClip;

    static AudioSource Source
    {
        get
        {
            if (source == null)
            {
                var go = new GameObject("Sfx");
                Object.DontDestroyOnLoad(go);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
            }
            return source;
        }
    }

    public static void PlayTap()
    {
        if (tapClip == null) tapClip = MakeTone(520f, 0.06f, 16f);
        Source.PlayOneShot(tapClip, 0.5f);
    }

    public static void PlayCollect()
    {
        if (collectClip == null) collectClip = MakeArpeggio(new[] { 740f, 988f, 1318f }, 0.07f);
        Source.PlayOneShot(collectClip, 0.6f);
    }

    public static void PlayDeath()
    {
        if (deathClip == null) deathClip = MakeThud();
        Source.PlayOneShot(deathClip, 0.7f);
    }

    public static void PlayMilestone()
    {
        if (milestoneClip == null) milestoneClip = MakeArpeggio(new[] { 523f, 659f, 784f, 1046f }, 0.06f);
        Source.PlayOneShot(milestoneClip, 0.5f);
    }

    static AudioClip MakeTone(float freq, float duration, float decay)
    {
        int samples = Mathf.CeilToInt(duration * sampleRate);
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * decay);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope;
        }
        return BuildClip("Tap", data);
    }

    static AudioClip MakeArpeggio(float[] freqs, float noteDuration)
    {
        int samplesPerNote = Mathf.CeilToInt(noteDuration * sampleRate);
        var data = new float[samplesPerNote * freqs.Length];
        for (int n = 0; n < freqs.Length; n++)
        {
            for (int i = 0; i < samplesPerNote; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 10f);
                data[n * samplesPerNote + i] = Mathf.Sin(2f * Mathf.PI * freqs[n] * t) * envelope;
            }
        }
        return BuildClip("Collect", data);
    }

    static AudioClip MakeThud()
    {
        float duration = 0.35f;
        int samples = Mathf.CeilToInt(duration * sampleRate);
        var data = new float[samples];
        var rng = new System.Random(1);
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 9f);
            float tone = Mathf.Sin(2f * Mathf.PI * 110f * t);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            data[i] = (tone * 0.7f + noise * 0.3f) * envelope;
        }
        return BuildClip("Death", data);
    }

    static AudioClip BuildClip(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
