using UnityEngine;

// Zemin, gökyüzü ve sis artık BİRLİKTE değişiyor: her biri ayrı "tema" olarak
// paketlenmiş (ColorTheme), aynı zamanlayıcıyla senkron lerp ediliyor. Böylece
// yol rengi değiştiğinde arka plan da onunla birlikte, tutarlı bir bütün olarak
// değişiyor — iki bağımsız döngü yerine tek bir koordineli görünüm.
public class WallColorCycler : MonoBehaviour
{
    [System.Serializable]
    public struct ColorTheme
    {
        public Color skyTop;
        public Color skyHorizon;
        public Color skyBottom;
        public Color fog;

        public ColorTheme(Color skyTop, Color skyHorizon, Color skyBottom, Color fog)
        {
            this.skyTop = skyTop;
            this.skyHorizon = skyHorizon;
            this.skyBottom = skyBottom;
            this.fog = fog;
        }

        // Yol rengi artık hardcoded değil — skyHorizon'dan türetilir: aynı hue (tema
        // ailesiyle uyumlu), ama sabit açık/pastel Value+Saturation. Böylece hem
        // parlama riski (HDR doyma) kontrol altında kalır hem her yeni tema otomatik
        // uyumlu bir yol rengi alır, elle ayarlanan ground hex'i gerekmez.
        public Color Ground
        {
            get
            {
                Color.RGBToHSV(skyHorizon, out float h, out float s, out _);
                return Color.HSVToRGB(h, Mathf.Max(s * 0.55f, 0.12f), 0.9f);
            }
        }
    }

    [Header("Zemin")]
    public Renderer targetRenderer;      // Ground prefabındaki Mesh Renderer

    [Header("Temalar")]
    // Üçü de aynı sıcak mücevher-tonu ailesinde (mor/gül/turuncu) — altın (#C9A66B)
    // ve bordo (#7A1F2B) marka vurgusu bu ailenin bir parçası olduğu için hiçbir
    // temayla çakışmıyor. Eski "Tropic Teal" soğuk mavi-yeşil tondaydı ve bordo/altın
    // ile uyumsuzdu; yerine bordonun daha koyu bir kuzeni olan "Rose Ember" geldi.
    public ColorTheme[] themes = new ColorTheme[]
    {
        // Zeminler bilerek tam beyaz değil (~0.88): tam beyaza yakın olunca ışıkla
        // çarpıp HDR'de doyuyor ve bloom yolu beyaz bir lekeye çeviriyordu. Bu tonda
        // hem tema rengi okunuyor hem parlama eşiğine girmiyor.
        // Grape Royale
        new ColorTheme(
            HexColor("#3B1F8F"), HexColor("#FF4FA3"), HexColor("#230F52"), HexColor("#FF4FA3")),
        // Rose Ember
        new ColorTheme(
            HexColor("#5C1A2E"), HexColor("#FF6F91"), HexColor("#2B0A14"), HexColor("#FF6F91")),
        // Citrus Sunset
        new ColorTheme(
            HexColor("#B23A6B"), HexColor("#FFB13C"), HexColor("#3D1642"), HexColor("#FFB13C")),
        // Altın Kartal — kilitli başlar, yeterince rozet toplanınca açılır (GameManager.UnlockGoldTheme)
        new ColorTheme(
            HexColor("#6B4A18"), HexColor("#FFD166"), HexColor("#2E1F0A"), HexColor("#FFD166")),
    };

    // Son tema (Altın Kartal) kilitli başlar — döngüye dahil olmaz ta ki açılana kadar
    private int activeThemeCount;

    public float transitionSpeed = 2f;
    public float holdDuration = 20f; // sadece boşta bekleme animasyonu — asıl tetikleyici AdvanceTheme()

    private Material groundMat;
    private Material skyMat;
    private Color originalGroundColor;
    private Color originalSkyTop, originalSkyHorizon, originalSkyBottom;
    private Color originalFogColor;

    private int themeIndex = 0;
    private float timer = 0f;
    private bool transitioning = false;

    void Start()
    {
        activeThemeCount = Mathf.Max(1, themes.Length - 1); // son tema (Altın Kartal) hariç

        // sharedMaterial: tüm Ground kopyaları ve tek skybox aynı anda renk değiştirsin
        groundMat = targetRenderer.sharedMaterial;
        skyMat = RenderSettings.skybox;

        originalGroundColor = groundMat.color;
        originalFogColor = RenderSettings.fogColor;
        if (skyMat != null)
        {
            originalSkyTop = skyMat.GetColor("_TopColor");
            originalSkyHorizon = skyMat.GetColor("_HorizonColor");
            originalSkyBottom = skyMat.GetColor("_BottomColor");
        }
    }

    void OnDestroy()
    {
        // Paylaşılan asset'ler kalıcı boyanmasın diye Play modu bitince eski haline dön
        if (groundMat != null)
            groundMat.color = originalGroundColor;
        RenderSettings.fogColor = originalFogColor;
        if (skyMat != null)
        {
            skyMat.SetColor("_TopColor", originalSkyTop);
            skyMat.SetColor("_HorizonColor", originalSkyHorizon);
            skyMat.SetColor("_BottomColor", originalSkyBottom);
        }
    }

    // Skor/hız kilometre taşlarında GameManager tarafından çağrılır: bir sonraki
    // temaya hemen geçişi başlatır, boşta bekleme sayacını da sıfırlar. Böylece
    // renk değişimi rastgele bir zamanlayıcıya değil, oyuncunun ilerlemesine bağlı.
    public void AdvanceTheme()
    {
        timer = 0f;
        themeIndex = (themeIndex + 1) % activeThemeCount;
        transitioning = true;
    }

    // Yeterince rozet toplanınca çağrılır: Altın Kartal temasını döngüye katar
    public void UnlockGoldTheme()
    {
        activeThemeCount = themes.Length;
    }

    // Game Over kartının arkasındaki halo bu anlık, gerçekten ekranda olan rengi
    // kullanır (hedef temayı değil) — geçiş yarı yoldaysa bile doğru renk yakalanır.
    public Color CurrentHorizonColor =>
        skyMat != null ? skyMat.GetColor("_HorizonColor") : themes[themeIndex].skyHorizon;

    void Update()
    {
        if (themes.Length == 0) return;

        if (!transitioning)
        {
            timer += Time.deltaTime;
            if (timer >= holdDuration)
            {
                timer = 0f;
                transitioning = true;
            }
            return;
        }

        ColorTheme target = themes[themeIndex];
        float k = transitionSpeed * Time.deltaTime;

        groundMat.color = Color.Lerp(groundMat.color, target.Ground, k);
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, target.fog, k);
        if (skyMat != null)
        {
            skyMat.SetColor("_TopColor", Color.Lerp(skyMat.GetColor("_TopColor"), target.skyTop, k));
            skyMat.SetColor("_HorizonColor", Color.Lerp(skyMat.GetColor("_HorizonColor"), target.skyHorizon, k));
            skyMat.SetColor("_BottomColor", Color.Lerp(skyMat.GetColor("_BottomColor"), target.skyBottom, k));
        }

        if (Vector4.Distance(groundMat.color, target.Ground) < 0.02f)
        {
            themeIndex = (themeIndex + 1) % activeThemeCount;
            transitioning = false;
        }
    }

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
