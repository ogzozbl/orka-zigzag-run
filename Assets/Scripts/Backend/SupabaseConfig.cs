using UnityEngine;

// Supabase proje bilgileri — Inspector'dan doldurulur, koda gömülmez.
// Create > ZigzagRun > Supabase Config ile bir asset oluşturup GameManager'a sürükleyin.
[CreateAssetMenu(fileName = "SupabaseConfig", menuName = "ZigzagRun/Supabase Config")]
public class SupabaseConfig : ScriptableObject
{
    [Tooltip("Supabase Project Settings > API > Project URL")]
    public string url;

    [Tooltip("Supabase Project Settings > API > anon public key")]
    public string anonKey;
}
