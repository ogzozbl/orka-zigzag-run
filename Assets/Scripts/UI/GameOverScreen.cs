using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Game Over panelinin içeriğini çalışma anında kurar. TMP hiyerarşisini sahne
// YAML'ında elle tutmak kırılgan olduğu için tüm tasarım tek yerden, koddan yönetilir.
// Orka paleti: siyah #1A1A1A, bordo #7A1F2B, altın #C9A66B.
public class GameOverScreen : MonoBehaviour
{
    static readonly Color charcoal = new Color(0.102f, 0.102f, 0.102f);          // #1A1A1A
    static readonly Color cardColor = new Color(0.078f, 0.078f, 0.078f, 0.97f);  // #141414
    static readonly Color bordeaux = new Color(0.478f, 0.122f, 0.169f);          // #7A1F2B
    static readonly Color labelGray = new Color(0.6f, 0.58f, 0.55f);

    Color accent; // altın yerine: ölüm anında aktif olan hypercasual temanın rengi

    RectTransform card;
    CanvasGroup cardGroup;
    TextMeshProUGUI scoreValue;
    TextMeshProUGUI bestValue;
    TextMeshProUGUI lastValue;
    TextMeshProUGUI collectedValue;
    TextMeshProUGUI teaser;
    GameObject recordBadge;
    TextMeshProUGUI hint;
    Image glow;
    bool built;

    // Oswald Bold: gövde/etiket metinleri için (okunaklı, ince).
    public static TMP_FontAsset BrandFont
    {
        get
        {
            var f = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF");
            return f != null ? f : TMP_Settings.defaultFontAsset;
        }
    }

    // Anton: başlık ve skor gibi "vurgu" öğeleri için — kalın, dolgun, arcade hissi
    // veren bir display font (TMP örnek paketinde hazır geliyor).
    public static TMP_FontAsset DisplayFont
    {
        get
        {
            var f = Resources.Load<TMP_FontAsset>("Fonts & Materials/Anton SDF");
            return f != null ? f : BrandFont;
        }
    }

    public void Show(int score, int best, int lastScore, int pickups, int totalPickups, bool newRecord, Color glowColor)
    {
        accent = glowColor;
        if (!built) Build();
        glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.4f);
        recordBadge.SetActive(newRecord);
        collectedValue.text = $"{pickups} ROZET · {totalPickups} TOPLAM";

        if (newRecord)
        {
            teaser.gameObject.SetActive(false);
        }
        else
        {
            int remaining = best - score;
            teaser.text = remaining > 0 ? $"REKORA {remaining} PUAN KALDI" : "REKORU YAKALADIN — TEKRAR DENE!";
            teaser.gameObject.SetActive(true);
        }

        bestValue.text = best.ToString();
        lastValue.text = lastScore.ToString();
        StartCoroutine(CountUpScore(score));
        StartCoroutine(Appear());
        StartCoroutine(PulseAlpha(hint));
    }

    // Skor 0'dan hedefe hızlıca sayarak dolar — statik bir sayı yerine "kazanılmış"
    // hissi veren küçük ama etkili bir juice; en ucuz/en yüksek getirili ekleme.
    IEnumerator CountUpScore(int target)
    {
        const float duration = 0.6f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 2f); // ease-out
            scoreValue.text = Mathf.RoundToInt(Mathf.Lerp(0f, target, k)).ToString();
            yield return null;
        }
        scoreValue.text = target.ToString();
    }

    void Build()
    {
        built = true;

        // Bordo overlay'i tam ekran şık karartmaya çevir
        var bg = GetComponent<Image>();
        if (bg != null)
            bg.color = new Color(charcoal.r, charcoal.g, charcoal.b, 0.82f);

        // Kartın arkasında, ölüm anındaki temanın rengiyle boyanan yumuşak bir halo —
        // sabit koyu kart artık "hangi dünyada öldüysen" ona bağlı görünüyor
        glow = MakeImage("Glow", (RectTransform)transform, new Vector2(1100f, 1100f), Vector2.zero, Color.clear);
        glow.sprite = MakeGlowSprite();
        glow.type = Image.Type.Simple;

        // Kartın altında yumuşak gölge — derinlik/kabarıklık hissi (modern kart estetiği)
        var shadow = MakeImage("CardShadow", (RectTransform)transform, new Vector2(880f, 960f), new Vector2(0f, -18f), new Color(0f, 0f, 0f, 0.35f));
        shadow.sprite = MakeGlowSprite();
        shadow.type = Image.Type.Simple;

        var img = MakeRoundedImage("Card", (RectTransform)transform, new Vector2(820f, 900f), Vector2.zero, cardColor);
        card = img.rectTransform;
        cardGroup = card.gameObject.AddComponent<CanvasGroup>();

        // Üstte ince aksan şerit — "etiket" estetiği (renk = ölüm anındaki tema)
        MakeRoundedImage("AccentLine", card, new Vector2(300f, 8f), new Vector2(0f, 400f), accent);

        // Başlıklar/etiketler beyaz — aksan rengi sadece ince çizgi ve birkaç
        // rakamda kalır, ekran her renk döngüsünde "patlamasın" diye
        MakeText("Title", card, "OYUN BİTTİ", 84f, Color.white, new Vector2(0f, 320f), 6f, DisplayFont);

        MakeText("ScoreLabel", card, "SKOR", 34f, labelGray, new Vector2(0f, 205f), 8f);
        scoreValue = MakeText("ScoreValue", card, "0", 160f, Color.white, new Vector2(0f, 75f), 0f, DisplayFont);

        // LAST | BEST iki sütun: her biri hafif yuvarlak "stat panel" içinde — modern,
        // gruplu okunur görünüm. BEST'i kırmak zor ama son denemeni geçmek daha ulaşılabilir.
        MakeRoundedImage("LastPanel", card, new Vector2(320f, 190f), new Vector2(-175f, -155f), new Color(1f, 1f, 1f, 0.05f));
        MakeText("LastLabel", card, "SON", 28f, labelGray, new Vector2(-175f, -105f), 8f);
        lastValue = MakeText("LastValue", card, "0", 62f, Color.white, new Vector2(-175f, -185f), 0f, DisplayFont);

        MakeRoundedImage("BestPanel", card, new Vector2(320f, 190f), new Vector2(175f, -155f), new Color(accent.r, accent.g, accent.b, 0.09f));
        MakeText("BestLabel", card, "EN İYİ", 28f, labelGray, new Vector2(175f, -105f), 8f);
        bestValue = MakeText("BestValue", card, "0", 62f, accent, new Vector2(175f, -185f), 0f, DisplayFont);

        var badgeImg = MakeRoundedImage("RecordBadge", card, new Vector2(340f, 62f), new Vector2(0f, -300f), bordeaux);
        recordBadge = badgeImg.gameObject;
        MakeText("RecordText", badgeImg.rectTransform, "YENİ REKOR", 32f, Color.white, Vector2.zero, 4f, DisplayFont);
        recordBadge.SetActive(false);

        teaser = MakeText("Teaser", card, "", 26f, labelGray, new Vector2(0f, -300f), 4f);
        teaser.gameObject.SetActive(false);

        collectedValue = MakeText("Collected", card, "0 ROZET TOPLANDI", 30f, accent, new Vector2(0f, -390f), 4f);

        hint = MakeText("Hint", (RectTransform)transform, "DEVAM İÇİN DOKUN", 40f, Color.white, new Vector2(0f, -620f), 10f);
    }

    IEnumerator Appear()
    {
        const float duration = 0.25f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 3f); // ease-out
            card.localScale = Vector3.one * Mathf.Lerp(0.88f, 1f, k);
            cardGroup.alpha = k;
            yield return null;
        }
        card.localScale = Vector3.one;
        cardGroup.alpha = 1f;
    }

    static IEnumerator PulseAlpha(TextMeshProUGUI text)
    {
        while (true)
        {
            Color c = text.color;
            c.a = 0.55f + 0.45f * Mathf.Sin(Time.time * 3f);
            text.color = c;
            yield return null;
        }
    }

    static Sprite glowSprite;
    static Sprite roundedSprite;

    // ParticleTex'in yumuşak daire dokusunu (Assets/Scripts/FX/ParticleTex.cs) UI
    // Image için Sprite'a sarar — halo için ek asset gerekmiyor
    public static Sprite MakeGlowSprite()
    {
        if (glowSprite != null) return glowSprite;
        var tex = ParticleTex.MakeSoftCircle(256);
        glowSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        return glowSprite;
    }

    // 9-slice yuvarlak köşe sprite'ı — modern kart/buton/rozet köşeleri için.
    // Kenarlardan 32px border ile herhangi boyuta ölçeklenirken köşeler keskin kalır.
    static Sprite RoundedSprite()
    {
        if (roundedSprite != null) return roundedSprite;
        const int size = 96, radius = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float a = 1f;
                // Köşelere olan uzaklık — yumuşak (AA'lı) yuvarlatma
                float cx = Mathf.Min(x, size - 1 - x);
                float cy = Mathf.Min(y, size - 1 - y);
                if (cx < radius && cy < radius)
                {
                    float dx = radius - cx, dy = radius - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    a = Mathf.Clamp01(radius - d + 0.5f);
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        roundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        return roundedSprite;
    }

    // Bir Image'ı yuvarlak köşeli yapar (9-slice) — köşeler her boyutta net kalır
    public static void Roundify(Image img)
    {
        img.sprite = RoundedSprite();
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1f;
    }

    public static Image MakeImage(string name, RectTransform parent, Vector2 size, Vector2 pos, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    // Yuvarlak köşeli Image kısayolu
    public static Image MakeRoundedImage(string name, RectTransform parent, Vector2 size, Vector2 pos, Color color)
    {
        var img = MakeImage(name, parent, size, pos, color);
        Roundify(img);
        return img;
    }

    public static TextMeshProUGUI MakeText(string name, RectTransform parent, string content,
        float size, Color color, Vector2 pos, float spacing = 0f, TMP_FontAsset font = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(900f, size * 1.5f);
        rt.anchoredPosition = pos;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.font = font != null ? font : BrandFont;
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.characterSpacing = spacing;
        t.raycastTarget = false;
        return t;
    }

    // Ekranlar arası basit geçiş: CanvasGroup alpha'sını lerp'ler. Start/Leaderboard/
    // NameEntry ekranlarının hepsi bunu paylaşıyor — tutarlı, tek yerden ayarlanan his.
    public static IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        group.alpha = from;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
    }

    // Canlı/değişken candy arka plana karşı metin her koşulda net kalsın diye koyu
    // kontur ekler — bg rengi değişse de (gökyüzü/sis/zemin döngüsü) okunaklılık korunur.
    public static void ApplyOutline(TextMeshProUGUI t, Color outlineColor, float width = 0.18f)
    {
        var mat = t.fontMaterial;
        mat.SetColor("_OutlineColor", outlineColor);
        mat.SetFloat("_OutlineWidth", width);
        t.fontMaterial = mat;
    }
}

// Oyun başlamadan önce görünen başlangıç ekranı: başlık + en iyi skor + dokunuş
// uyarısı, tek CanvasGroup altında. GameManager runtime'da yaratır, ilk dokunuşta
// Dismiss ile hepsi birlikte sönerek kaybolur.
public class StartScreen : MonoBehaviour
{
    static readonly Color outlineColor = new Color(0.102f, 0.102f, 0.102f);

    CanvasGroup group;
    TextMeshProUGUI prompt;
    bool dismissing;

    public static StartScreen Create(RectTransform canvas, int bestScore, SupabaseConfig config, Color accent)
    {
        var root = new GameObject("StartScreen", typeof(RectTransform));
        var rootRect = (RectTransform)root.transform;
        rootRect.SetParent(canvas, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;

        var group = root.AddComponent<CanvasGroup>();

        // Beyaz + koyu kontur: candy gökyüzü her renge dönse de okunaklı kalır,
        // hiçbir başlık artık tema rengini kullanmıyor — tutarlı/sakin görünüm
        var title = GameOverScreen.MakeText("Title", rootRect, "ZIGZAG RUN", 92f, Color.white, new Vector2(0f, 780f), 6f, GameOverScreen.DisplayFont);
        GameOverScreen.ApplyOutline(title, outlineColor, 0.22f);

        var best = GameOverScreen.MakeText("Best", rootRect, $"EN İYİ {bestScore}", 38f, Color.white, new Vector2(0f, 690f), 8f);
        GameOverScreen.ApplyOutline(best, outlineColor, 0.2f);

        // Liste artık gömülü değil — ayrı bir sayfa gibi açılır (SIRALAMA butonu)
        var rankBtnImg = GameOverScreen.MakeRoundedImage("RankButton", rootRect, new Vector2(320f, 90f), new Vector2(0f, 560f), new Color(1f, 1f, 1f, 0.1f));
        rankBtnImg.raycastTarget = true;
        var rankBtn = rankBtnImg.gameObject.AddComponent<Button>();
        var rankLabel = GameOverScreen.MakeText("RankLabel", rankBtnImg.rectTransform, "SIRALAMA", 34f, accent, Vector2.zero, 6f, GameOverScreen.DisplayFont);
        rankLabel.raycastTarget = false;
        rankBtn.onClick.AddListener(() => LeaderboardOverlay.Show(canvas, config, accent));

        var prompt = GameOverScreen.MakeText("Prompt", rootRect, "BAŞLAMAK İÇİN DOKUN", 48f, Color.white, new Vector2(0f, -760f), 10f);
        GameOverScreen.ApplyOutline(prompt, outlineColor, 0.2f);

        var screen = root.AddComponent<StartScreen>();
        screen.group = group;
        screen.prompt = prompt;
        screen.StartCoroutine(GameOverScreen.FadeCanvasGroup(group, 0f, 1f, 0.25f));
        return screen;
    }

    void Update()
    {
        if (dismissing || prompt == null) return;
        Color c = prompt.color;
        c.a = 0.55f + 0.45f * Mathf.Sin(Time.time * 3f);
        prompt.color = c;
    }

    public void Dismiss()
    {
        if (dismissing) return;
        dismissing = true;
        StartCoroutine(FadeOut());
    }

    System.Collections.IEnumerator FadeOut()
    {
        float t = 0f;
        float startAlpha = group.alpha;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, 0f, t / 0.3f);
            yield return null;
        }
        Destroy(gameObject);
    }
}

// Her oturumda (StartScreen'den önce) isim sorar — aynı cihazı farklı kullanıcıların
// deneyeceği bir kiosk/demo senaryosu varsayıldığı için isim kalıcı tutulmaz, sadece
// bir sonraki dolum için hatırlanır. Onaylanınca callback ile ismi döner, kendini yok eder.
public class NameEntryScreen : MonoBehaviour
{
    const string NameKey = "ZigzagRun.PlayerName";
    static readonly Color bordeaux = new Color(0.478f, 0.122f, 0.169f);
    static readonly Color outlineColor = new Color(0.102f, 0.102f, 0.102f);

    TMP_InputField input;
    CanvasGroup group;
    System.Action<string> onConfirm;
    bool confirmed;

    public static NameEntryScreen Create(RectTransform canvas, System.Action<string> onConfirm, Color accent)
    {
        var root = new GameObject("NameEntryScreen", typeof(RectTransform));
        var rootRect = (RectTransform)root.transform;
        rootRect.SetParent(canvas, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;

        var group = root.AddComponent<CanvasGroup>();

        var dim = GameOverScreen.MakeImage("Dim", rootRect, new Vector2(4000f, 4000f), Vector2.zero, new Color(0.102f, 0.102f, 0.102f, 0.78f));
        dim.raycastTarget = true; // arkadaki oyuna tıklama geçmesin

        // İçeriği toplayan modern kart — başlık/alan/buton boşlukta yüzmesin
        var panelShadow = GameOverScreen.MakeImage("PanelShadow", rootRect, new Vector2(800f, 720f), new Vector2(0f, 42f), new Color(0f, 0f, 0f, 0.35f));
        panelShadow.sprite = GameOverScreen.MakeGlowSprite();
        panelShadow.type = Image.Type.Simple;
        GameOverScreen.MakeRoundedImage("Panel", rootRect, new Vector2(740f, 660f), new Vector2(0f, 60f), new Color(0.078f, 0.078f, 0.078f, 0.97f));

        GameOverScreen.MakeRoundedImage("AccentLine", rootRect, new Vector2(240f, 8f), new Vector2(0f, 330f), accent);

        var title = GameOverScreen.MakeText("Title", rootRect, "KİM OYNUYOR?", 56f, Color.white, new Vector2(0f, 240f), 4f, GameOverScreen.DisplayFont);
        GameOverScreen.ApplyOutline(title, outlineColor, 0.2f);

        // Klasik form kutusu: açık zemin + koyu yazı — "burası bir yazı alanı" hemen
        // anlaşılsın, karanlık fon üstünde saydam kutu ne yazdığını belirsizleştiriyordu
        var fieldBg = GameOverScreen.MakeRoundedImage("Field", rootRect, new Vector2(600f, 110f), new Vector2(0f, 60f), new Color(0.95f, 0.95f, 0.95f, 1f));
        fieldBg.raycastTarget = true;
        var fieldRect = fieldBg.rectTransform;

        var fieldBorder = fieldBg.gameObject.AddComponent<Outline>();
        fieldBorder.effectColor = new Color(accent.r, accent.g, accent.b, 0f); // odaklanınca görünür yapılır
        fieldBorder.effectDistance = new Vector2(3f, -3f);

        var textArea = new GameObject("TextArea", typeof(RectTransform));
        var textAreaRect = (RectTransform)textArea.transform;
        textAreaRect.SetParent(fieldRect, false);
        StretchFull(textAreaRect);
        textAreaRect.offsetMin = new Vector2(24f, 8f);
        textAreaRect.offsetMax = new Vector2(-24f, -8f);
        textArea.AddComponent<RectMask2D>();

        var placeholder = GameOverScreen.MakeText("Placeholder", textAreaRect, "ADIN", 36f, new Color(0.102f, 0.102f, 0.102f, 0.45f), Vector2.zero);
        StretchFull(placeholder.rectTransform);

        var inputText = GameOverScreen.MakeText("Text", textAreaRect, "", 36f, new Color(0.102f, 0.102f, 0.102f), Vector2.zero);
        StretchFull(inputText.rectTransform);

        var inputField = fieldBg.gameObject.AddComponent<TMP_InputField>();
        inputField.textViewport = textAreaRect;
        inputField.textComponent = inputText;
        inputField.placeholder = placeholder;
        inputField.characterLimit = 16;
        inputField.text = PlayerPrefs.GetString(NameKey, "");
        inputField.customCaretColor = true;
        inputField.caretColor = new Color(0.102f, 0.102f, 0.102f);
        inputField.caretWidth = 3;
        inputField.selectionColor = new Color(accent.r, accent.g, accent.b, 0.4f);

        // Odaklanınca ince çerçeve belirir — hangi alanda olduğun net görünsün
        inputField.onSelect.AddListener(_ => fieldBorder.effectColor = new Color(accent.r, accent.g, accent.b, 1f));
        inputField.onDeselect.AddListener(_ => fieldBorder.effectColor = new Color(accent.r, accent.g, accent.b, 0f));

        var playBtnImg = GameOverScreen.MakeRoundedImage("PlayButton", rootRect, new Vector2(320f, 90f), new Vector2(0f, -140f), bordeaux);
        playBtnImg.raycastTarget = true;
        var button = playBtnImg.gameObject.AddComponent<Button>();
        var playLabel = GameOverScreen.MakeText("PlayLabel", playBtnImg.rectTransform, "OYNA", 40f, Color.white, Vector2.zero, 8f, GameOverScreen.DisplayFont);
        playLabel.raycastTarget = false;

        var screen = root.AddComponent<NameEntryScreen>();
        screen.input = inputField;
        screen.group = group;
        screen.onConfirm = onConfirm;

        button.onClick.AddListener(screen.Confirm);
        inputField.onSubmit.AddListener(_ => screen.Confirm());

        screen.StartCoroutine(GameOverScreen.FadeCanvasGroup(group, 0f, 1f, 0.25f));
        return screen;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void Confirm()
    {
        if (confirmed) return;
        confirmed = true;

        string name = input.text.Trim();
        if (string.IsNullOrEmpty(name)) name = "OYUNCU";
        PlayerPrefs.SetString(NameKey, name);
        PlayerPrefs.Save();

        onConfirm?.Invoke(name);
        StartCoroutine(FadeOutAndDestroy());
    }

    System.Collections.IEnumerator FadeOutAndDestroy()
    {
        // StartScreen aynı anda içeri süzülürken bu ekran dışarı süzülür — crossfade hissi
        yield return GameOverScreen.FadeCanvasGroup(group, group.alpha, 0f, 0.2f);
        Destroy(gameObject);
    }
}

// SIRALAMA butonuna basınca açılan tam ekran sayfa: karartma + başlık + liste +
// GERİ butonu. Açıkken top "dokun = başla" girdisini yutmasın diye InputLocked kilitlenir.
public class LeaderboardOverlay : MonoBehaviour
{
    static readonly Color outlineColor = new Color(0.102f, 0.102f, 0.102f);
    CanvasGroup group;
    bool closing;

    public static void Show(RectTransform canvas, SupabaseConfig config, Color accent)
    {
        GameManager.InputLocked = true;

        var root = new GameObject("LeaderboardOverlay", typeof(RectTransform));
        var rootRect = (RectTransform)root.transform;
        rootRect.SetParent(canvas, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;

        var group = root.AddComponent<CanvasGroup>();

        var dim = GameOverScreen.MakeImage("Dim", rootRect, new Vector2(4000f, 4000f), Vector2.zero, new Color(0.102f, 0.102f, 0.102f, 0.92f));
        dim.raycastTarget = true;

        GameOverScreen.MakeText("Header", rootRect, "SIRALAMA", 60f, Color.white, new Vector2(0f, 780f), 6f, GameOverScreen.DisplayFont);

        LeaderboardPanel.Create(rootRect, config, accent);

        var backImg = GameOverScreen.MakeRoundedImage("BackButton", rootRect, new Vector2(280f, 90f), new Vector2(0f, -780f), new Color(1f, 1f, 1f, 0.1f));
        backImg.raycastTarget = true;
        var backBtn = backImg.gameObject.AddComponent<Button>();
        var backLabel = GameOverScreen.MakeText("BackLabel", backImg.rectTransform, "GERİ", 34f, Color.white, Vector2.zero, 6f, GameOverScreen.DisplayFont);
        backLabel.raycastTarget = false;

        var overlay = root.AddComponent<LeaderboardOverlay>();
        overlay.group = group;
        backBtn.onClick.AddListener(overlay.Close);

        overlay.StartCoroutine(GameOverScreen.FadeCanvasGroup(group, 0f, 1f, 0.2f));
    }

    void Close()
    {
        if (closing) return;
        closing = true;
        GameManager.InputLocked = false;
        StartCoroutine(FadeOutAndDestroy());
    }

    System.Collections.IEnumerator FadeOutAndDestroy()
    {
        yield return GameOverScreen.FadeCanvasGroup(group, group.alpha, 0f, 0.2f);
        Destroy(gameObject);
    }
}

// Başlangıç ekranındaki TOP 10 skor listesi. Runtime'da 10 satır kurar, Supabase'den
// çeker; gelene kadar "YÜKLENİYOR…", boşsa/hatada "HENÜZ SKOR YOK".
public class LeaderboardPanel : MonoBehaviour
{
    const int rows = 10;
    static readonly Color labelGray = new Color(0.6f, 0.58f, 0.55f);
    static readonly Color outlineColor = new Color(0.102f, 0.102f, 0.102f);

    RectTransform[] rowRoots;
    TextMeshProUGUI[] rowRankTexts;
    TextMeshProUGUI[] rowNameTexts;
    TextMeshProUGUI[] rowScoreTexts;
    TextMeshProUGUI status;
    Color accent;

    public static LeaderboardPanel Create(RectTransform parent, SupabaseConfig config, Color accent)
    {
        const float rowH = 72f, panelW = 620f;
        float panelH = rows * rowH + 40f;

        var root = new GameObject("LeaderboardPanel", typeof(RectTransform));
        var rootRect = (RectTransform)root.transform;
        rootRect.SetParent(parent, false);
        rootRect.anchoredPosition = new Vector2(0f, 40f);
        rootRect.sizeDelta = new Vector2(panelW + 40f, panelH + 160f);

        GameOverScreen.MakeText("Header", rootRect, "EN İYİLER", 42f, Color.white, new Vector2(0f, panelH / 2f + 70f), 8f, GameOverScreen.DisplayFont);

        // Listeyi toplayan yuvarlak panel — modern, gruplu kart görünümü
        GameOverScreen.MakeRoundedImage("ListPanel", rootRect, new Vector2(panelW, panelH), Vector2.zero, new Color(1f, 1f, 1f, 0.045f));

        var panel = root.AddComponent<LeaderboardPanel>();
        panel.accent = accent;
        panel.rowRoots = new RectTransform[rows];
        panel.rowRankTexts = new TextMeshProUGUI[rows];
        panel.rowNameTexts = new TextMeshProUGUI[rows];
        panel.rowScoreTexts = new TextMeshProUGUI[rows];

        float top = panelH / 2f - rowH / 2f - 10f;
        for (int i = 0; i < rows; i++)
        {
            float y = top - i * rowH;
            bool first = i == 0;

            // 1. sıra aksan-renkli vurgu barı; diğerleri şeffaf (zebra için tek/çift hafif ton)
            Color rowBg = first ? new Color(accent.r, accent.g, accent.b, 0.16f)
                                 : (i % 2 == 0 ? new Color(1f, 1f, 1f, 0.03f) : Color.clear);
            var rowImg = GameOverScreen.MakeRoundedImage($"Row{i}", rootRect, new Vector2(panelW - 24f, rowH - 8f), new Vector2(0f, y), rowBg);
            panel.rowRoots[i] = rowImg.rectTransform;

            Color txt = first ? accent : Color.white;
            var rank = GameOverScreen.MakeText("Rank", rowImg.rectTransform, "", 32f, first ? accent : labelGray, new Vector2(-panelW / 2f + 55f, 0f), 0f, GameOverScreen.DisplayFont);
            rank.alignment = TMPro.TextAlignmentOptions.Center;
            var nm = GameOverScreen.MakeText("Name", rowImg.rectTransform, "", 32f, txt, new Vector2(-30f, 0f), 1f, GameOverScreen.BrandFont);
            nm.alignment = TMPro.TextAlignmentOptions.Left;
            nm.rectTransform.sizeDelta = new Vector2(360f, rowH);
            var sc = GameOverScreen.MakeText("Score", rowImg.rectTransform, "", 34f, txt, new Vector2(panelW / 2f - 90f, 0f), 0f, GameOverScreen.DisplayFont);
            sc.alignment = TMPro.TextAlignmentOptions.Right;
            sc.rectTransform.sizeDelta = new Vector2(150f, rowH);

            panel.rowRankTexts[i] = rank;
            panel.rowNameTexts[i] = nm;
            panel.rowScoreTexts[i] = sc;
            rowImg.gameObject.SetActive(false);
        }

        panel.status = GameOverScreen.MakeText("Status", rootRect, "YÜKLENİYOR…", 30f, labelGray, Vector2.zero, 4f);

        Leaderboard.Fetch(config, rows, panel.OnFetched);
        return panel;
    }

    void OnFetched(System.Collections.Generic.List<Leaderboard.Entry> entries)
    {
        if (this == null) return; // ekran bu arada kapandıysa

        if (entries == null || entries.Count == 0)
        {
            status.text = "HENÜZ SKOR YOK";
            return;
        }

        status.gameObject.SetActive(false);
        for (int i = 0; i < rowRoots.Length && i < entries.Count; i++)
        {
            var e = entries[i];
            string name = string.IsNullOrEmpty(e.player_name) ? "—" : e.player_name;
            rowRankTexts[i].text = (i + 1).ToString();
            rowNameTexts[i].text = name;
            rowScoreTexts[i].text = e.score.ToString();
            rowRoots[i].gameObject.SetActive(true);
        }
    }
}
