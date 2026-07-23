using UnityEngine;

// Yol üzerine serpiştirilen altın toplanabilir. Karonun child'ı olarak doğar:
// hem dünyayla birlikte kayar hem de karo düştüğünde otomatik temizlenir.
// Materyal ve mesh runtime'da kurulur — ayrı prefab/materyal asset'i gerekmez.
public class Orb : MonoBehaviour
{
    public int bonusPoints = 5;

    const float worldSize = 0.45f;   // dünya ölçeğinde çap
    const float hoverHeight = 0.55f; // karo yüzeyinden boşluk
    const float bobAmplitude = 0.12f;
    const float spinSpeed = 110f;

    static Material sharedMat;

    float baseLocalY;
    float bobScale;
    float phase;
    bool collected;

    const float eagleSizeMultiplier = 1.8f; // logo küçük kalıyordu, mücevherden daha iri olsun

    static GameObject eaglePrefab;
    static bool eagleLoadAttempted;
    static bool eagleBoundsCached;
    static float eagleMaxExtent;
    static Vector3 eagleCenter;

    public static Orb Create(Transform parentTile)
    {
        // Kök obje sadece fizik/konum taşır (collider, rigidbody, bu script) — görsel
        // ayrı bir child. Böylece Meshy'den gelen kartal modelinin bilinmeyen pivot/
        // ölçeği kök'ün konum matematiğini hiç etkilemez.
        var go = new GameObject("Orb");
        var t = go.transform;
        t.SetParent(parentTile, false);

        Vector3 s = parentTile.lossyScale;
        t.localPosition = new Vector3(0f, 0.5f + (hoverHeight + worldSize * 0.5f) / s.y, 0f);
        t.localScale = new Vector3(worldSize / s.x, worldSize / s.y, worldSize / s.z);

        BuildVisual(go);

        var col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.2f; // toplaması kolay olsun

        var rb = go.AddComponent<Rigidbody>(); // hareketli trigger'ın güvenilir algılanması için
        rb.isKinematic = true;
        rb.useGravity = false;

        return go.AddComponent<Orb>();
    }

    static void BuildVisual(GameObject root)
    {
        if (!eagleLoadAttempted)
        {
            eagleLoadAttempted = true;
            eaglePrefab = Resources.Load<GameObject>("Models/OrkaEagle");
        }

        if (eaglePrefab != null)
        {
            BuildEagleVisual(root);
            return;
        }

        BuildFallbackGemVisual(root);
    }

    // Meshy export'unun ölçeği/pivotu bilinmiyor: modeli önce parent'sız
    // instantiate edip gerçek boyutunu ölçüyoruz, sonra 1 birime normalize edip
    // köke bağlıyoruz — hangi ölçekte export edilmiş olursa olsun kök'ün
    // localScale'i (worldSize) sonucu belirliyor.
    static void BuildEagleVisual(GameObject root)
    {
        var visual = Instantiate(eaglePrefab);

        // Bounds ölçümü sadece ilk kez yapılır, sonrakiler cache'ten okur —
        // her orb doğumunda tekrar tekrar ölçmeye gerek yok
        if (!eagleBoundsCached)
        {
            Bounds b = ComputeBounds(visual);
            eagleMaxExtent = Mathf.Max(0.0001f, Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)));
            eagleCenter = b.center;
            eagleBoundsCached = true;
        }

        float scale = eagleSizeMultiplier / eagleMaxExtent;
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = -eagleCenter * scale; // merkezi köke ortala
        visual.transform.localScale = Vector3.one * scale;

        // Gölge en pahalı kısım — birkaç kartal aynı anda ekranda dönerken
        // performansı düşürüyordu, toplanabilir bir obje için gerek yok
        var renderers = visual.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static Bounds ComputeBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    // glTFast paketi kurulu değilse / model yüklenemezse oyun kırılmasın diye
    // eski küp-mücevher görselini geri düşüş olarak kullan
    static void BuildFallbackGemVisual(GameObject root)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "GemVisual";
        go.transform.SetParent(root.transform, false);
        go.transform.localScale = new Vector3(0.62f, 0.9f, 0.62f); // küpten ince/uzun, mücevher oranı
        go.transform.localRotation = Quaternion.Euler(35f, 45f, 0f);

        go.GetComponent<MeshRenderer>().sharedMaterial = GetMaterial();
        Destroy(go.GetComponent<BoxCollider>()); // kökte zaten SphereCollider var
    }

    void Start()
    {
        baseLocalY = transform.localPosition.y;
        float parentScaleY = transform.parent != null ? transform.parent.lossyScale.y : 1f;
        bobScale = bobAmplitude / Mathf.Max(parentScaleY, 0.0001f);
        phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        Vector3 lp = transform.localPosition;
        lp.y = baseLocalY + Mathf.Sin(Time.time * 2.6f + phase) * bobScale;
        transform.localPosition = lp;
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected || other.GetComponent<BallController>() == null) return;
        collected = true;

        if (GameManager.Instance != null)
            GameManager.Instance.AddPickup(bonusPoints);

        var trail = other.GetComponent<GoldTrail>();
        if (trail != null)
            trail.EmitBurst(transform.position, 22);

        Sfx.PlayCollect();
#if !UNITY_WEBGL
        if (Application.isMobilePlatform)
            Handheld.Vibrate(); // WebGL'de bu API derlemeye dahil değil
#endif

        Destroy(gameObject);
    }

    static Material GetMaterial()
    {
        if (sharedMat != null) return sharedMat;
        Color gold = new Color(0.788f, 0.651f, 0.42f); // #C9A66B
        sharedMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        sharedMat.SetColor("_BaseColor", gold);
        sharedMat.SetFloat("_Metallic", 0.8f);
        sharedMat.SetFloat("_Smoothness", 0.85f);
        sharedMat.EnableKeyword("_EMISSION");
        sharedMat.SetColor("_EmissionColor", gold * 2f); // HDR — bloom bunu parlatır
        return sharedMat;
    }
}
