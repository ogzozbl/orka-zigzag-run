using UnityEngine;
using System.Collections.Generic;

public class PathGenerator : MonoBehaviour
{
    [Header("Ayarlar")]
    public GameObject tilePrefab;
    public BallController ball;
    public Transform worldRoot;      // Tile'lar bunun child'ı olarak doğacak
    public float tileSize = 3f;
    public int startTiles = 10;
    public int tilesAhead = 15;
    public int maxTiles = 40;

    [Header("Karo Düşüşü")]
    public int keepBehind = 4;       // Top bu kadar karo geçince en arkadaki düşer

    [Header("Orb")]
    // Kartal modeli eski küp-mücevherden çok daha ağır (yüksek poligon/büyük texture) —
    // aynı anda ekranda az sayıda olsun diye şans düşürüldü, performans için
    [Range(0f, 1f)] public float orbChance = 0.12f;

    [Header("Çatal (Sahte Dal)")]
    // Yol ara sıra ikiye ayrılır: biri gerçek (devam eden), biri kısa sahte çıkmaz.
    // Yanlış dala giren top 2 karo sonra boşluğa düşer (BallController.IsFalling).
    [Range(0f, 1f)] public float forkChance = 0.15f;
    public int decoyLength = 2;

    private Vector3 nextSpawnPos = Vector3.zero;
    private Queue<GameObject> activeTiles = new Queue<GameObject>();
    private int tilesSpawnedCount = 0;
    private int tilesDroppedCount = 0;
    private bool lastTileHadOrb = false;
    private bool lastWasFork = false;

    // Sahte dallar ana yol sayımına dahil değil — ayrı listede, mesafeye göre temizlenir
    private struct DecoyStub { public GameObject tile; public float killDistance; }
    private List<DecoyStub> decoyStubs = new List<DecoyStub>();

    void Start()
    {
        for (int i = 0; i < startTiles; i++)
            SpawnTile(Vector3.forward);

        for (int i = 0; i < tilesAhead; i++)
            SpawnRandomTile();
    }

    void Update()
    {
        // Kaç karo "geride kaldı" hesapla, önümüzde yeterince karo yoksa yenisini ekle
        int tilesPassed = Mathf.FloorToInt(ball.DistanceTraveled / tileSize);
        if (tilesSpawnedCount - tilesPassed < tilesAhead)
            SpawnRandomTile();

        // Klasik Zigzag hissi: geride kalan karolar düşerek kaybolur
        while (activeTiles.Count > 0 && tilesPassed - tilesDroppedCount > keepBehind)
            DropOldestTile();

        // Sahte dallar: top çatalı geçtikten kısa süre sonra arkada çökerek kaybolur
        for (int i = decoyStubs.Count - 1; i >= 0; i--)
        {
            if (ball.DistanceTraveled > decoyStubs[i].killDistance)
            {
                DropDecoy(decoyStubs[i].tile);
                decoyStubs.RemoveAt(i);
            }
        }
    }

    void SpawnRandomTile()
    {
        // İlk diziden sonra, ardışık olmayacak şekilde ara sıra çatal kur
        bool canFork = tilesSpawnedCount > startTiles + tilesAhead && !lastWasFork;
        if (canFork && Random.value < forkChance)
        {
            SpawnFork();
            return;
        }
        lastWasFork = false;

        Vector3 direction = (Random.value > 0.5f) ? Vector3.forward : Vector3.right;
        SpawnTile(direction);
    }

    // Çatal: ana yol mainDir'e devam eder, decoyDir'e 2 karoluk sahte çıkmaz spawn edilir.
    void SpawnFork()
    {
        lastWasFork = true;

        bool mainForward = Random.value > 0.5f;
        Vector3 mainDir = mainForward ? Vector3.forward : Vector3.right;
        Vector3 decoyDir = mainForward ? Vector3.right : Vector3.forward;

        Vector3 junctionPos = nextSpawnPos;

        // Sahte dal: junction'dan decoyDir yönünde kısa çıkmaz (orb yok, sonu boşluk)
        float junctionDistance = tilesSpawnedCount * tileSize; // ~ çatalın kat edilen mesafesi
        float killDistance = junctionDistance + keepBehind * tileSize;
        for (int i = 1; i <= decoyLength; i++)
        {
            GameObject decoy = MakeTile(junctionPos + decoyDir * tileSize * i);
            decoyStubs.Add(new DecoyStub { tile = decoy, killDistance = killDistance });
        }

        // Ana yol: normal akış mainDir ile devam etsin
        SpawnTile(mainDir);
    }

    void SpawnTile(Vector3 direction)
    {
        GameObject tile = MakeTile(nextSpawnPos);
        activeTiles.Enqueue(tile);
        tilesSpawnedCount++;

        TrySpawnOrb(tile.transform);

        nextSpawnPos += direction * tileSize;

        if (activeTiles.Count > maxTiles)
            DropOldestTile();
    }

    // Ortak karo üretimi (ana yol + sahte dal paylaşır)
    GameObject MakeTile(Vector3 localPos)
    {
        GameObject tile = Instantiate(tilePrefab, worldRoot);
        tile.transform.localPosition = localPos;   // World'e göre LOKAL konum

        // Başlangıç dizilimi animasyonsuz — top ilk karoların üstünde duruyor
        if (ball.IsGameStarted)
            tile.AddComponent<TileAnimator>().PlayRise();

        return tile;
    }

    void TrySpawnOrb(Transform tile)
    {
        // İlk karolar (topun altı) hariç, ardışık olmayacak şekilde serpiştir
        if (tilesSpawnedCount <= startTiles + 2 || lastTileHadOrb || Random.value > orbChance)
        {
            lastTileHadOrb = false;
            return;
        }
        Orb.Create(tile);
        lastTileHadOrb = true;
    }

    void DropOldestTile()
    {
        GameObject tile = activeTiles.Dequeue();
        tilesDroppedCount++;
        DropDecoy(tile);
    }

    // Bir karoyu düşürerek yok et (ana yol ve sahte dal aynı animasyonu kullanır)
    void DropDecoy(GameObject tile)
    {
        if (tile == null) return;
        TileAnimator anim = tile.GetComponent<TileAnimator>();
        if (anim == null) anim = tile.AddComponent<TileAnimator>();
        anim.FallAway();
    }
}
