using UnityEngine;

public class BallController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float speed = 8f;
    public float speedIncrease = 0.25f;
    public float maxSpeed = 18f;
    public float extraGravity = 25f;

    [Header("Referans")]
    public Transform worldRoot;   // "World" objesi buraya sürüklenecek

    [Header("Zemin Kontrolü")]
    // Ölüm artık sabit bir Y mesafesi düşmeye değil, "altımda gerçekten karo var mı"
    // kontrolüne bağlı. Eskiden yer çekimi hıza bağlı olmadığı için sabit bir düşüş
    // süresi vardı; top hızlandıkça o süre içinde çok daha fazla karo geçtiğinden
    // yanlış yöne gidilse bile şans eseri bir sonraki karoya "yetişip" ölüm atlatılıyordu.
    public float groundCheckTolerance = 0.15f; // top yarıçapının (0.5) altında izin verilen boşluk
    public float ungroundedGrace = 0.12f;      // gerçek düşüş sayılmadan önceki kısa tolerans

    private Rigidbody rb;
    private Vector3 currentDirection;
    private bool isGameStarted = false;
    private BallJuice juice;
    private float ungroundedTimer = 0f;

    public bool IsGameStarted => isGameStarted;
    public Vector3 CurrentDirection => currentDirection;
    public bool IsFalling => ungroundedTimer > ungroundedGrace;
    public float DistanceTraveled { get; private set; } // PathGenerator bunu okuyacak

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentDirection = Vector3.forward;
        juice = gameObject.AddComponent<BallJuice>();
        gameObject.AddComponent<GoldTrail>().Init(worldRoot);
    }

    void Update()
    {
        if (GameManager.InputLocked) return; // isim ekranı açıkken dokunuş oyunu başlatmasın

        if (Input.GetMouseButtonDown(0))
        {
            // Tıklama bir UI butonunun/alanının üzerindeyse (SIRALAMA, GERİ, isim
            // kutusu vb.) oyun girdisi tetiklenmesin — aksi halde aynı tıklama hem
            // butona hem "dokun = başla/dön" girdisine aynı anda gidiyordu.
            if (IsPointerOverUI()) return;

            Sfx.PlayTap();
            if (!isGameStarted)
                isGameStarted = true;
            else
                SwitchDirection();
        }
    }

    // Mobilde IsPointerOverGameObject() parametresiz hali dokunmatikte güvenilmez —
    // hangi parmağın sorulduğunu bilemez. Dokunmatikte aktif dokunuşun fingerId'siyle,
    // masaüstünde parametresiz haliyle sormak gerekiyor.
    static bool IsPointerOverUI()
    {
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null) return false;

        if (Input.touchCount > 0)
            return es.IsPointerOverGameObject(Input.GetTouch(0).fingerId);

        return es.IsPointerOverGameObject();
    }

    // Oyun bitince çağrılır: top olduğu yerde donar, dünya kaymayı bırakır.
    // Aksi halde ekranda Game Over paneli açıkken top sonsuza dek düşmeye,
    // worldRoot kaymaya devam eder — gereksiz iş ve anlamsız görüntü.
    public void Freeze()
    {
        enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    void FixedUpdate()
    {
        if (!isGameStarted) return;

        speed = Mathf.Min(speed + speedIncrease * Time.fixedDeltaTime, maxSpeed);

        // Top artık X/Z'de HİÇ hareket etmiyor — sadece Y'de (yer çekimi) serbest
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);

        // Ray, topun merkezinden aşağı — merkez kendi collider'ının içinde olduğu için
        // Unity bu raycast'te topun kendi collider'ına hiç çarpmaz (Unity'nin standart davranışı)
        bool grounded = Physics.Raycast(transform.position, Vector3.down, 0.5f + groundCheckTolerance);
        ungroundedTimer = grounded ? 0f : ungroundedTimer + Time.fixedDeltaTime;

        // Bunun yerine TÜM DÜNYA ters yönde kayıyor — top hep ekranda aynı noktada duruyor
        float step = speed * Time.fixedDeltaTime;
        worldRoot.position -= currentDirection * step;
        DistanceTraveled += step;
    }

    void SwitchDirection()
    {
        currentDirection = (currentDirection == Vector3.forward)
            ? Vector3.right
            : Vector3.forward;
        if (juice != null) juice.Squash();
    }
}