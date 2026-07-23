using UnityEngine;
using System.Collections;

// Topun "cansız kayıyor" hissini gideren görsel katman: sürekli yuvarlanma dönüşü
// + yön değiştirirken kısa squash&stretch tepkisi. Görsel mesh, fizik gövdesinden
// (collider/rigidbody root'ta kalır) ayrı bir child'a taşınır — böylece squash
// animasyonu collider boyutunu/fizik davranışını hiç etkilemez.
public class BallJuice : MonoBehaviour
{
    const float ballRadius = 0.5f;
    const float squashDuration = 0.16f;
    const float squashAmount = 0.28f;

    BallController ball;
    Transform visual;
    Coroutine squashRoutine;

    void Awake()
    {
        ball = GetComponent<BallController>();
        BuildVisual();
    }

    void BuildVisual()
    {
        var meshFilter = GetComponent<MeshFilter>();
        var meshRenderer = GetComponent<MeshRenderer>();
        if (meshFilter == null || meshRenderer == null) return;

        var go = new GameObject("BallVisual");
        go.transform.SetParent(transform, false);
        var vf = go.AddComponent<MeshFilter>();
        vf.sharedMesh = meshFilter.sharedMesh;
        var vr = go.AddComponent<MeshRenderer>();
        vr.sharedMaterials = meshRenderer.sharedMaterials;
        vr.shadowCastingMode = meshRenderer.shadowCastingMode;

        meshRenderer.enabled = false; // Destroy() bu frame'in sonuna kadar gerçekleşmez, çift render'ı önle
        Destroy(meshRenderer);
        Destroy(meshFilter);

        visual = go.transform;
    }

    void Update()
    {
        if (visual == null || ball == null || !ball.IsGameStarted) return;

        // Top X/Z'de fiziksel olarak hareket etmiyor ama görsel olarak yuvarlanmalı —
        // aksi halde dünya kayarken top havada donmuş gibi durur
        Vector3 axis = Vector3.Cross(Vector3.up, ball.CurrentDirection);
        float angularSpeed = (ball.speed / ballRadius) * Mathf.Rad2Deg * Time.deltaTime;
        visual.Rotate(axis, angularSpeed, Space.World);
    }

    public void Squash()
    {
        if (visual == null) return;
        if (squashRoutine != null) StopCoroutine(squashRoutine);
        squashRoutine = StartCoroutine(SquashRoutine());
    }

    IEnumerator SquashRoutine()
    {
        Vector3 squashed = new Vector3(1f + squashAmount, 1f - squashAmount, 1f + squashAmount);
        float t = 0f;
        while (t < squashDuration)
        {
            t += Time.deltaTime;
            float k = t / squashDuration;
            // hızlı sıkışma, yay gibi geri sekme
            float ease = k < 0.3f ? k / 0.3f : 1f - Mathf.Pow((k - 0.3f) / 0.7f, 2f);
            visual.localScale = Vector3.LerpUnclamped(Vector3.one, squashed, ease);
            yield return null;
        }
        visual.localScale = Vector3.one;
    }
}
