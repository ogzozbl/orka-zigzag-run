using UnityEngine;
using System.Collections;

// Karo yaşam döngüsü animasyonları. PathGenerator tarafından runtime'da eklenir:
// yeni karolar alttan yükselerek doğar; geride kalanlar kısa bir "kick" tepkisiyle
// (beyaz flaş + toz patlaması) parçalanırcasına savrularak, küçülerek ve hızlanan
// bir tumble ile düşer. Tüm hareket lokal uzayda — karolar worldRoot child'ı
// olduğu için treadmill ile uyumlu.
public class TileAnimator : MonoBehaviour
{
    const float riseDuration = 0.22f;
    const float riseDepth = 2f;

    const float kickDuration = 0.1f;
    const float kickScale = 1.12f;
    const float fallDuration = 0.75f;
    const float fallGravity = 30f;
    const float driftRadius = 1.4f;
    static readonly Color flashColor = Color.white;
    static readonly Color dustColor = new Color(0.8f, 0.8f, 0.8f);

    bool falling;

    public void PlayRise()
    {
        StartCoroutine(Rise());
    }

    IEnumerator Rise()
    {
        Vector3 target = transform.localPosition;
        Vector3 from = target + Vector3.down * riseDepth;
        float t = 0f;
        while (t < riseDuration && !falling)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / riseDuration), 3f); // ease-out
            transform.localPosition = Vector3.Lerp(from, target, k);
            yield return null;
        }
        if (!falling)
            transform.localPosition = target;
    }

    public void FallAway()
    {
        if (falling) return;
        falling = true;
        StopAllCoroutines();
        StartCoroutine(Fall());
    }

    IEnumerator Fall()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        DustBurst.Emit(transform.position, dustColor);
        FlashWhite();

        Vector3 startScale = transform.localScale;

        // Kick: yumuşak başlamak yerine kısa/keskin bir "itilme" tepkisi
        float kt = 0f;
        while (kt < kickDuration)
        {
            kt += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(kt / kickDuration) * Mathf.PI);
            transform.localScale = Vector3.LerpUnclamped(startScale, startScale * kickScale, k);
            yield return null;
        }
        transform.localScale = startScale;

        Vector3 drift = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized * driftRadius;
        Vector3 tiltAxis = new Vector3(Random.Range(-1f, 1f), Random.Range(-0.3f, 0.3f), Random.Range(-1f, 1f)).normalized;

        float velocity = 0f;
        float t = 0f;
        while (t < fallDuration)
        {
            float dt = Time.deltaTime;
            t += dt;
            velocity += fallGravity * dt;
            float k = t / fallDuration;

            transform.localPosition += Vector3.down * velocity * dt + drift * (dt / fallDuration);
            transform.Rotate(tiltAxis, (60f + k * 340f) * dt, Space.World); // tumble hızlanarak artar
            transform.localScale = Vector3.LerpUnclamped(startScale, Vector3.zero, k * k);
            yield return null;
        }
        Destroy(gameObject);
    }

    void FlashWhite()
    {
        // Oyuncunun gördüğü yüzey artık TopCap (döngülü renkli üst katman);
        // flaş orada olmalı, koyu duvar gövdesinde değil
        var topCap = transform.Find("TopCap");
        var mr = topCap != null ? topCap.GetComponent<MeshRenderer>() : GetComponent<MeshRenderer>();
        if (mr == null) return;
        var block = new MaterialPropertyBlock();
        mr.GetPropertyBlock(block);
        block.SetColor("_BaseColor", flashColor);
        mr.SetPropertyBlock(block);
    }
}
