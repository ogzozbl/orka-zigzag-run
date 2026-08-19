using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // İsim ekranı açıkken top "dokun = başla" girdisini yutmasın diye
    // BallController bunu kontrol ediyor.
    public static bool InputLocked;

    const string HighScoreKey = "ZigzagRun.HighScore";
    const string LastScoreKey = "ZigzagRun.LastScore";
    const string PlayerNameKey = "ZigzagRun.PlayerName";
    const string TotalPickupsKey = "ZigzagRun.TotalPickups";

    // Tüm zamanların rozet toplamı bu sayıya ulaşınca Altın Kartal teması açılır —
    // tek oyunluk skor kovalamanın ötesinde uzun vadeli bir sebep verir.
    const int goldThemeUnlockAt = 25;

    // Sahne her restart'ta yeniden yüklenir (SceneManager.LoadScene) — statik alan
    // domain reload olmadan hayatta kalır, böylece isim sadece bu oturumda BİR kez sorulur.
    static bool nameAskedThisSession;
    static readonly Color scoreColor = Color.white;
    static readonly Color scoreOutline = new Color(0.102f, 0.102f, 0.102f); // koyu kontur — candy gökyüzüne karşı her zaman net

    [Header("Referanslar")]
    public BallController ball;
    public TextMeshProUGUI scoreText;
    public GameObject gameOverPanel;
    public CameraOrbit cameraOrbit;
    public WallColorCycler wallColorCycler;
    public SupabaseConfig supabaseConfig;

    [Header("Ayarlar")]
    public float pointsPerUnit = 0.4f;   // Kat edilen mesafe başına puan (tileSize=3 → karo başına ~1.2)
    public float fallThreshold = -3f;
    public float restartDelay = 0.6f;   // Yanlışlıkla anında restart olmasın

    // Mekanik çok sade (tek tuş) olduğu için ilerleme hissi büyük ölçüde bu tür
    // ara kilometre taşlarından geliyor — hız her eşiği geçtiğinde kısa bir "HIZ ARTTI" anı.
    static readonly float[] speedMilestones = { 10f, 12f, 14f, 16f, 18f };

    // Altın yerine: UI aksanı her zaman o an aktif olan hypercasual temanın rengini
    // kullanır (WallColorCycler.CurrentHorizonColor) — sabit bir marka rengi yok artık.
    Color Accent => wallColorCycler != null ? wallColorCycler.CurrentHorizonColor : Color.white;

    private int lastDisplayedScore = -1;
    private float score;
    private int bonusScore;
    private int pickupCount;
    private bool isGameOver = false;
    private float gameOverTime;
    private StartScreen startScreen;
    private PathGenerator pathGenerator;
    private int nextMilestoneIndex = 0;
    private string playerName = "PLAYER";
    private TextMeshProUGUI pickupHudValue;
    private TextMeshProUGUI muteLabel;
    private RectTransform uiRoot;   // çentik/delik altında kalmayan güvenli alan

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        BuildSafeAreaRoot();

        // Marka tipografisi: Oswald Bold (TMP örnek paketindeki hazır SDF asset)
        scoreText.font = GameOverScreen.BrandFont;
        scoreText.color = scoreColor;
        scoreText.fontSize = 90f;
        GameOverScreen.ApplyOutline(scoreText, scoreOutline, 0.2f);
        pathGenerator = FindAnyObjectByType<PathGenerator>();
        BuildPickupHud();
        BuildMuteButton();

        if (nameAskedThisSession)
        {
            // İsim bu oturumda zaten alındı — tekrar sorma, kayıtlıyla devam et
            playerName = PlayerPrefs.GetString(PlayerNameKey, "OYUNCU");
            BeginStartScreen();
        }
        else
        {
            // İsim onaylanana kadar top "dokun = başla" girdisini işlemesin
            nameAskedThisSession = true;
            InputLocked = true;
            NameEntryScreen.Create(uiRoot, OnNameConfirmed, Accent);
        }
    }

    // Canvas ile UI arasına güvenli alana daralan bir katman koyar; sahnedeki
    // hazır UI (skor, Game Over paneli) ve runtime'da kurulan her şey buraya taşınır.
    void BuildSafeAreaRoot()
    {
        var canvas = (RectTransform)scoreText.canvas.transform;

        var go = new GameObject("SafeArea", typeof(RectTransform));
        uiRoot = (RectTransform)go.transform;
        uiRoot.SetParent(canvas, false);
        go.AddComponent<SafeAreaFitter>();

        // Sahnede Canvas altında duran mevcut UI'ı da güvenli alana al
        scoreText.rectTransform.SetParent(uiRoot, false);
        if (gameOverPanel != null)
            gameOverPanel.transform.SetParent(uiRoot, false);
    }

    // Oynarken sürekli görünen, kaç kartal toplandığını gösteren küçük bir rozet —
    // Game Over kartıyla aynı görsel dil (koyu şerit + altın Anton rakam + kontur)
    void BuildPickupHud()
    {
        var chip = GameOverScreen.MakeRoundedImage("PickupChip", uiRoot, new Vector2(220f, 130f), Vector2.zero,
            new Color(0.078f, 0.078f, 0.078f, 0.6f));
        var chipRect = chip.rectTransform;
        GameOverScreen.AnchorTopLeft(chipRect, 40f, 40f);

        GameOverScreen.MakeText("PickupLabel", chipRect, "ROZETLER", 26f, new Color(0.6f, 0.58f, 0.55f), new Vector2(0f, 32f), 6f);
        pickupHudValue = GameOverScreen.MakeText("PickupValue", chipRect, "0", 56f, Accent, new Vector2(0f, -22f), 0f, GameOverScreen.DisplayFont);
        GameOverScreen.ApplyOutline(pickupHudValue, scoreOutline, 0.18f);

        // İsim/başlangıç ekranları sonradan eklendiği için sibling sırasında üste
        // çıkıp kutuyu gizleyebilir — rozet kutusu her zaman en önde kalsın
        chipRect.SetAsLastSibling();
    }

    // Sağ üstte, rozet kutusunun simetriği — ses aç/kapa, tercih PlayerPrefs'te kalıcı
    void BuildMuteButton()
    {
        var btnImg = GameOverScreen.MakeRoundedImage("MuteButton", uiRoot, new Vector2(220f, 90f), Vector2.zero,
            new Color(0.078f, 0.078f, 0.078f, 0.6f));
        var btnRect = btnImg.rectTransform;
        GameOverScreen.AnchorTopRight(btnRect, 40f, 40f);
        btnImg.raycastTarget = true;
        var btn = btnImg.gameObject.AddComponent<Button>();

        muteLabel = GameOverScreen.MakeText("MuteLabel", btnRect, "", 26f, Color.white, Vector2.zero, 4f);
        RefreshMuteLabel();

        btn.onClick.AddListener(() =>
        {
            Sfx.Muted = !Sfx.Muted;
            RefreshMuteLabel();
        });

        btnRect.SetAsLastSibling();
    }

    void RefreshMuteLabel()
    {
        muteLabel.text = Sfx.Muted ? "SES: KAPALI" : "SES: AÇIK";
        muteLabel.color = Sfx.Muted ? new Color(0.6f, 0.58f, 0.55f) : Color.white;
    }

    void OnNameConfirmed(string name)
    {
        playerName = name;
        InputLocked = false;
        BeginStartScreen();
    }

    void BeginStartScreen()
    {
        int best = PlayerPrefs.GetInt(HighScoreKey, 0);
        startScreen = StartScreen.Create(uiRoot, best, supabaseConfig, Accent, playerName);
    }

    void Update()
    {
        if (isGameOver)
        {
            bool delayPassed = Time.time - gameOverTime > restartDelay;
            if (delayPassed && Input.GetMouseButtonDown(0))
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if (ball.IsGameStarted && startScreen != null)
        {
            startScreen.Dismiss();
            startScreen = null;
        }

        // Skor hıza değil kat edilen mesafeye bağlı: hız arttıkça skor doğal olarak
        // hızlanır ama çarpan üstüne çarpan binmez, eğri dengeli kalır
        if (ball.IsGameStarted)
            score = ball.DistanceTraveled * pointsPerUnit + bonusScore;

        int newScoreDisplay = Mathf.FloorToInt(score);
        if (newScoreDisplay != lastDisplayedScore)
        {
            lastDisplayedScore = newScoreDisplay;
            scoreText.text = newScoreDisplay.ToString();
            StopCoroutine(nameof(PopScoreText));
            StartCoroutine(nameof(PopScoreText));
        }

        if (nextMilestoneIndex < speedMilestones.Length && ball.speed >= speedMilestones[nextMilestoneIndex])
        {
            nextMilestoneIndex++;
            StartCoroutine(ShowToast("HIZ ARTTI!"));
            Sfx.PlayMilestone();
            if (wallColorCycler != null)
                wallColorCycler.AdvanceTheme();
            // HUD rozeti de yeni temaya anında geçsin — sabit renk kalmasın
            if (pickupHudValue != null)
                pickupHudValue.color = Accent;
        }

        // Ana tetikleyici artık "altımda karo yok" (ball.IsFalling); sabit Y eşiği
        // sadece beklenmedik bir durum için ihtiyati yedek olarak kalıyor
        if (ball.IsFalling || ball.transform.position.y < fallThreshold)
            GameOver();
    }

    // Ekranın üstünde beliren, büyüyüp-küçülen ve yukarı süzülerek kaybolan kısa
    // bildirim — hız artışı ve tema açılışı gibi anlar için ortak kullanılıyor.
    System.Collections.IEnumerator ShowToast(string message)
    {
        var t = GameOverScreen.MakeText("Toast", uiRoot, message, 54f, Accent, new Vector2(0f, 250f), 8f);
        GameOverScreen.ApplyOutline(t, scoreOutline, 0.2f);
        var rt = t.rectTransform;
        rt.localScale = Vector3.zero;

        const float appear = 0.18f, hold = 0.5f, fade = 0.35f;
        float time = 0f;
        while (time < appear)
        {
            time += Time.deltaTime;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(time / appear), 3f);
            rt.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1.15f, k);
            yield return null;
        }
        rt.localScale = Vector3.one;

        yield return new WaitForSeconds(hold);

        time = 0f;
        Color baseColor = t.color;
        Vector2 startPos = rt.anchoredPosition;
        while (time < fade)
        {
            time += Time.deltaTime;
            Color c = baseColor;
            c.a = Mathf.Lerp(baseColor.a, 0f, time / fade);
            t.color = c;
            rt.anchoredPosition = startPos + Vector2.up * (40f * time / fade);
            yield return null;
        }
        Destroy(t.gameObject);
    }

    // Rozet toplandığında: puan ekle, sayacı ve HUD'u güncelle, tüm-zamanlar
    // toplamını ilerlet ve eşiğe ulaşınca Altın Kartal temasını aç.
    public void AddPickup(int basePoints)
    {
        if (isGameOver) return;

        bonusScore += basePoints;
        pickupCount++;

        if (pickupHudValue != null)
        {
            pickupHudValue.text = pickupCount.ToString();
            StopCoroutine(nameof(PopPickupHud));
            StartCoroutine(nameof(PopPickupHud));
        }

        int total = PlayerPrefs.GetInt(TotalPickupsKey, 0) + 1;
        PlayerPrefs.SetInt(TotalPickupsKey, total);
        if (total == goldThemeUnlockAt)
        {
            if (wallColorCycler != null) wallColorCycler.UnlockGoldTheme();
            StartCoroutine(ShowToast("ALTIN KARTAL TEMASI AÇILDI!"));
            Sfx.PlayMilestone();
        }
    }

    System.Collections.IEnumerator PopPickupHud()
    {
        var t = pickupHudValue.transform;
        t.localScale = Vector3.one * 1.4f;
        float time = 0f;
        while (time < 0.2f)
        {
            time += Time.deltaTime;
            t.localScale = Vector3.Lerp(Vector3.one * 1.4f, Vector3.one, time / 0.2f);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    void GameOver()
    {
        isGameOver = true;
        gameOverTime = Time.time;

        int finalScore = Mathf.FloorToInt(score);
        int best = PlayerPrefs.GetInt(HighScoreKey, 0);
        bool newRecord = finalScore > best;
        if (newRecord)
        {
            best = finalScore;
            PlayerPrefs.SetInt(HighScoreKey, best);
        }

        // BEST'i her zaman kırmak zor; kendi son denemenle kıyaslamak çok daha
        // ulaşılabilir bir hedef — "bir kez daha dene" güdüsünü besliyor
        int lastScore = PlayerPrefs.GetInt(LastScoreKey, 0);
        PlayerPrefs.SetInt(LastScoreKey, finalScore);
        PlayerPrefs.Save();

        gameOverPanel.SetActive(true);
        var screen = gameOverPanel.GetComponent<GameOverScreen>();
        if (screen == null)
            screen = gameOverPanel.AddComponent<GameOverScreen>();
        int totalPickups = PlayerPrefs.GetInt(TotalPickupsKey, 0);
        screen.Show(finalScore, best, lastScore, pickupCount, totalPickups, newRecord, Accent);

        Leaderboard.Submit(supabaseConfig, playerName, finalScore, pickupCount);

        if (cameraOrbit != null)
        {
            cameraOrbit.StartOrbit();
            cameraOrbit.Shake();
        }

        // Top olduğu yerde donsun, dünya kaymayı ve karo üretimini bıraksın —
        // aksi halde Game Over ekranı açıkken oyun arka planda sonsuza dek çalışır
        ball.Freeze();
        if (pathGenerator != null)
            pathGenerator.enabled = false;

        var trail = ball.GetComponent<GoldTrail>();
        if (trail != null)
            trail.EmitBurst(ball.transform.position, 30);

        Sfx.PlayDeath();
#if !UNITY_WEBGL
        if (Application.isMobilePlatform)
            Handheld.Vibrate(); // WebGL'de bu API derlemeye dahil değil
#endif
    }

    System.Collections.IEnumerator PopScoreText()
    {
        scoreText.transform.localScale = Vector3.one * 1.3f;
        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            scoreText.transform.localScale = Vector3.Lerp(Vector3.one * 1.3f, Vector3.one, t / 0.15f);
            yield return null;
        }
        scoreText.transform.localScale = Vector3.one;
    }
}
