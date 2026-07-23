using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

// Supabase REST'ine skor gönderir/okur (fire-and-forget). Config eksikse veya ağ
// isteği başarısız olursa sessizce loglar — internet yoksa/kurulum yapılmadıysa
// oyunu asla bloklamaz veya bozmaz.
public static class Leaderboard
{
    [System.Serializable]
    class ScorePayload
    {
        public string p_name;
        public int p_score;
        public int p_pickups;
    }

    [System.Serializable]
    public class Entry
    {
        public string player_name;
        public int score;
        public int pickups;
    }

    [System.Serializable]
    class Wrapper { public Entry[] items; }

    class Runner : MonoBehaviour { }
    static Runner runner;

    static Runner GetRunner()
    {
        if (runner == null)
        {
            var go = new GameObject("LeaderboardRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<Runner>();
        }
        return runner;
    }

    public static void Submit(SupabaseConfig config, string playerName, int score, int pickups)
    {
        if (config == null || string.IsNullOrEmpty(config.url) || string.IsNullOrEmpty(config.anonKey))
        {
            Debug.LogWarning("Leaderboard: SupabaseConfig eksik, gönderim atlanıyor.");
            return;
        }
        GetRunner().StartCoroutine(SubmitCoroutine(config, playerName, score, pickups));
    }

    // TOP 10'u skora göre azalan çeker. Hata/eksik config'te boş liste döner.
    public static void Fetch(SupabaseConfig config, int limit, Action<List<Entry>> onDone)
    {
        if (config == null || string.IsNullOrEmpty(config.url) || string.IsNullOrEmpty(config.anonKey))
        {
            onDone?.Invoke(new List<Entry>());
            return;
        }
        GetRunner().StartCoroutine(FetchCoroutine(config, limit, onDone));
    }

    static IEnumerator FetchCoroutine(SupabaseConfig config, int limit, Action<List<Entry>> onDone)
    {
        string endpoint = config.url.TrimEnd('/') +
            $"/rest/v1/scores?select=player_name,score,pickups&order=score.desc&limit={limit}";

        using (var req = UnityWebRequest.Get(endpoint))
        {
            req.SetRequestHeader("apikey", config.anonKey); // Authorization Bearer YOK — yazmadaki aynı ders

            yield return req.SendWebRequest();

            var list = new List<Entry>();
            if (req.result == UnityWebRequest.Result.Success)
            {
                // JsonUtility düz array'i parse edemez; {"items":[...]} ile sarıyoruz
                string wrapped = "{\"items\":" + req.downloadHandler.text + "}";
                try
                {
                    var w = JsonUtility.FromJson<Wrapper>(wrapped);
                    if (w != null && w.items != null) list.AddRange(w.items);
                }
                catch (Exception e) { Debug.LogWarning($"Leaderboard: parse hatası — {e.Message}"); }
            }
            else
            {
                Debug.LogWarning($"Leaderboard: okuma başarısız — {req.error}");
            }
            onDone?.Invoke(list);
        }
    }

    // Düz INSERT yerine submit_score RPC'sini çağırır: isim zaten varsa skor/rozet
    // sadece DAHA YÜKSEKSE güncellenir (sunucu tarafında, Postgres GREATEST ile) —
    // her oynanış ayrı satır açmaz, "hesap" gibi tek satır/kişi davranışı verir.
    static IEnumerator SubmitCoroutine(SupabaseConfig config, string playerName, int score, int pickups)
    {
        string json = JsonUtility.ToJson(new ScorePayload
        {
            p_name = playerName,
            p_score = score,
            p_pickups = pickups
        });
        byte[] body = Encoding.UTF8.GetBytes(json);

        string endpoint = config.url.TrimEnd('/') + "/rest/v1/rpc/submit_score";
        using (var req = new UnityWebRequest(endpoint, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("apikey", config.anonKey);
            // Authorization: Bearer KOYMA — yeni sb_publishable_ formatı JWT değil,
            // Bearer'a konunca gateway bozuk JWT sanıp reddediyor (42501 ile sonuçlanıyor).
            req.SetRequestHeader("Prefer", "return=minimal");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"Leaderboard: gönderim başarısız — {req.error}");
        }
    }
}
