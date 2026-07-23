using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    public Transform ball;
    public float orbitSpeed = 15f;
    private bool isOrbiting = false;

    const float shakeDuration = 0.35f;
    const float shakeMagnitude = 0.6f;
    private float shakeTimer;

    void Start()
    {
        // Inspector'da atanmamışsa topu otomatik bul — bağlantı unutulunca orbit sessizce ölmesin
        if (ball == null)
        {
            var bc = FindAnyObjectByType<BallController>();
            if (bc != null) ball = bc.transform;
        }
    }

    public void StartOrbit()
    {
        isOrbiting = true;
    }

    // Çarpma hissi: shake, kamera POZİSYONUNU değil sadece bakış hedefini titretir.
    // Pozisyonu titretseydik her kare RotateAround yeni (kaymış) pozisyondan devam
    // edeceği için kamera kalıcı olarak sürüklenirdi.
    public void Shake()
    {
        shakeTimer = shakeDuration;
    }

    void Update()
    {
        if (!isOrbiting || ball == null) return;

        transform.RotateAround(ball.position, Vector3.up, orbitSpeed * Time.deltaTime);

        Vector3 lookTarget = ball.position + Vector3.up * 0.5f;
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float k = shakeTimer / shakeDuration;
            lookTarget += Random.insideUnitSphere * shakeMagnitude * k;
        }
        transform.LookAt(lookTarget);
    }
}