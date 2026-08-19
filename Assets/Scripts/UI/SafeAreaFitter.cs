using UnityEngine;

// Canvas içeriğini cihazın "güvenli alanına" sığdırır. Android'de
// androidRenderOutsideSafeArea açık olduğu için oyun çentiğin/delik kameranın
// altına kadar render ediliyor — bu bileşen olmadan üst köşedeki ROZETLER ve
// SES butonları çentiğin altında kalıyor.
//
// Mevcut anchor sistemiyle uyumlu: bu RectTransform ekranın güvenli alanına
// daralır, tüm UI onun child'ı olarak kurulduğu için hepsi otomatik içeri kayar.
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    RectTransform rect;
    Rect lastSafeArea;
    ScreenOrientation lastOrientation;
    Vector2Int lastResolution;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        Apply();
    }

    void Update()
    {
        // Ekran döndürülünce / çözünürlük değişince yeniden hesapla
        if (Screen.safeArea != lastSafeArea ||
            Screen.orientation != lastOrientation ||
            Screen.width != lastResolution.x || Screen.height != lastResolution.y)
        {
            Apply();
        }
    }

    void Apply()
    {
        lastSafeArea = Screen.safeArea;
        lastOrientation = Screen.orientation;
        lastResolution = new Vector2Int(Screen.width, Screen.height);

        if (Screen.width <= 0 || Screen.height <= 0) return;

        // Güvenli alanı 0..1 normalize edip anchor olarak uygula
        Vector2 min = lastSafeArea.position;
        Vector2 max = lastSafeArea.position + lastSafeArea.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
