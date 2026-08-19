# ZigzagRun — Orka Edition

A hyper-casual mobile game built in Unity 6 (URP), themed as a brand engagement piece for
**Orka Holding** — the Turkish menswear group behind Damat, D'S Damat and Tween.

One tap. The ball never stops. Tap to turn the corner, miss it and you fall.

---

## Why I built this

This project was developed during my internship at Orka Holding.

The brief I set out to answer was a marketing one, not a gameplay one: **how do you put a
fashion brand in someone's hands for five minutes at a time?** Brand awareness in retail
usually means an ad someone skips. A hyper-casual game is the opposite — it is voluntary,
it is short, and people replay it. If the brand lives inside the thing people are choosing
to do, the impression is earned rather than bought.

So the goal was never "clone Zigzag." It was to take a mechanic proven to be replayable and
make the brand inseparable from the experience:

- **The collectible is the brand.** Instead of a generic coin, the player collects the Orka
  eagle — a 3D model generated from the corporate logo and imported as a GLB. It is the
  single most-repeated visual action in the game.
- **The palette is a wardrobe.** The game cycles through colour themes (Grape Royale, Rose
  Ember, Citrus Sunset) drawn from a warm jewel-tone family that sits naturally alongside
  the brand's burgundy and gold. Sky, fog, and path all shift together, so a theme change
  reads as one deliberate look rather than three independent colour loops.
- **Progression rewards loyalty, not spend.** A fourth theme — *Altın Kartal* (Golden Eagle)
  — stays locked until the player has collected 25 eagles across all sessions. It gives a
  reason to come back that outlives any single run.
- **A leaderboard makes it social.** Scores are posted to a Supabase backend with one row
  per player, so the game works as a shared scoreboard at an event or across an office
  rather than a solitary high-score file.

The engineering side of the internship was the more interesting half, and most of what
follows in this README is about that.

---

## The core architectural trick

The ball appears to run forward along the path. **It does not.**

The ball is locked to the Y axis — gravity is the only force acting on it. It never
translates in X or Z. Instead, an empty parent object named `World` slides in the opposite
direction of travel, carrying every tile, collectible and hazard with it. The camera is
completely static: no follow script, no smoothing, no look-at.

```
Ball:   moves only on Y (gravity)
World:  worldRoot.position -= currentDirection * speed * dt
Camera: fixed at rotation (50°, 45°, 0°) — never moves
```

This "treadmill" approach buys three things:

1. **The isometric framing is exact, forever.** No camera lerp means no drift, no jitter,
   and no easing artifacts when the ball changes direction at speed.
2. **Floating-point precision never degrades.** The ball stays near the origin no matter
   how long the run lasts, so a 10-minute session is as stable as a 10-second one.
3. **Cleanup is free.** Tiles are children of `World`; when one scrolls out of range it is
   already in local space relative to everything else that needs to move with it.

The trade-off is that anything world-space has to be told about it explicitly. Trail
effects, for instance, cannot use a standard `TrailRenderer` — it would smear across the
screen as the world slides underneath it. `GoldTrail` instead uses a `ParticleSystem` with
a **custom simulation space** parented to `worldRoot`, so particles stay pinned to the
world they were emitted into.

---

## Requirements

| | |
|---|---|
| **Unity** | `6000.5.2f1` (Unity 6) — the exact version is in `ProjectSettings/ProjectVersion.txt` |
| **Render pipeline** | Universal RP 17.5.0 |
| **Modules** | Android Build Support (with OpenJDK + Android SDK/NDK) for mobile builds; WebGL Build Support for browser builds |
| **Backend** *(optional)* | A free Supabase project, only if you want the online leaderboard |

All Unity packages resolve automatically on first open — including `com.unity.cloud.gltfast`,
which is what loads the eagle GLB at runtime.

## Running it

```bash
git clone https://github.com/ogzozbl/orka-zigzag-run.git
```

1. Open **Unity Hub → Add → Add project from disk** and select the cloned folder.
2. Open it with Unity `6000.5.2f1`. The first import takes a few minutes — it is compiling
   shaders and building the Library folder from scratch. This is normal and only happens once.
3. Open `Assets/Scenes/SampleScene.unity`.
4. Press **Play**.

Tap or click anywhere to start, then tap again at each corner to turn. Collect eagles.
Don't fall off.

> The game is portrait-locked. In the Game view, pick a portrait aspect ratio (e.g. 9:16)
> or the UI will not sit where it is meant to.

### Optional: enabling the leaderboard

Without a backend the game plays perfectly well — scores just stay local. To turn the
online leaderboard on:

1. Create a project at [supabase.com](https://supabase.com).
2. Create the scores table and the upsert function:

```sql
create table public.scores (
  id          bigint generated always as identity primary key,
  player_name text not null unique,
  score       int  not null default 0,
  created_at  timestamptz not null default now()
);

alter table public.scores enable row level security;

create policy "public read"  on public.scores for select to anon using (true);
create policy "public write" on public.scores for insert to anon with check (true);

-- One row per player: a new score only replaces the old one if it is higher.
create or replace function public.submit_score(p_name text, p_score int)
returns void
language plpgsql
security definer
as $$
begin
  insert into public.scores (player_name, score)
  values (p_name, p_score)
  on conflict (player_name)
  do update set score = greatest(public.scores.score, excluded.score);
end;
$$;

grant execute on function public.submit_score(text, int) to anon;
```

3. In Unity: **Create → ZigzagRun → Supabase Config**, then paste your Project URL and
   `anon` public key into the asset and drag it onto the `GameManager` component.

## Building

**Android** — `File → Build Profiles → Android → Switch Platform`, then *Build And Run*
with a device connected in USB debugging mode. The project already targets ARM64 with
IL2CPP (both are Play Store requirements) and requests the internet permission needed by
the leaderboard.

**WebGL** — `File → Build Profiles → Web → Switch Platform → Build`. The output must be
served over HTTP, not opened as a `file://` path.

---

## How the code is organised

```
Assets/Scripts/
├── BallController.cs      Input, gravity, ground detection, and the world-scroll loop
├── PathGenerator.cs       Procedural path: tiles, collectibles, and the fork/decoy hazard
├── GameManager.cs         Score, milestones, theme unlocks, and the whole UI flow
├── TileAnimator.cs        Tiles rising into place and tumbling away behind the player
├── WallColorCycler.cs     Themes — ground, sky and fog transitioning as one unit
├── CameraOrbit.cs         Idle camera motion on the menu only (never during play)
├── Orb.cs                 The eagle collectible: GLB loading, bobbing, pickup
├── FX/
│   ├── GoldTrail.cs       World-space particle trail (see the treadmill note above)
│   ├── DustBurst.cs       Impact puff when a tile falls
│   ├── BallJuice.cs       Squash-and-stretch on every direction change
│   ├── ParticleTex.cs     Runtime-generated particle sprite — no texture asset needed
│   └── Sfx.cs             Procedurally synthesised audio — no audio files in the repo
├── UI/
│   ├── GameOverScreen.cs  Start, name entry, game over and leaderboard screens
│   └── SafeAreaFitter.cs  Keeps UI clear of notches and punch-hole cameras
└── Backend/
    ├── Leaderboard.cs     Supabase REST calls (submit via RPC, fetch top 10)
    └── SupabaseConfig.cs  Credentials as a ScriptableObject, not hardcoded
```

A deliberate constraint throughout: **no sprite, audio or material assets are shipped for
effects.** Particle textures are drawn into a `Texture2D` at runtime, sound effects are
synthesised sample-by-sample into an `AudioClip`, and every UI panel is constructed in
C# rather than authored as a prefab. It keeps the build small and the repository readable
as source rather than as a pile of binaries.

---

## Things I learned the hard way

A few notes that cost me real debugging time, left here because they are the kind of thing
that is obvious only in hindsight:

- **The Editor and the device do not use the same quality settings.** Unity picks a quality
  tier per platform. The Editor was rendering through `PC_RPAsset` while the Android build
  used `Mobile_RPAsset` — so a rendering problem I had "fixed" in the Editor was still
  there on the phone, because I had never touched the asset the phone was actually using.
  If a visual bug is platform-specific, check the pipeline asset for that platform first.
- **`EventSystem.IsPointerOverGameObject()` lies on touchscreens.** The parameterless
  overload only inspects the mouse pointer. On mobile you have to pass the active touch's
  `fingerId`, or a tap on a UI button registers as a gameplay tap as well.
- **Don't cache `ParticleSystem` module structs in fields.** Storing something like an
  `EmissionModule` across a domain reload throws *"Do not create your own module
  instances"*. Fetch the module fresh each time you need it.
- **`Handheld.Vibrate()` does not exist in WebGL builds.** It has to be guarded with
  `#if !UNITY_WEBGL` or the build fails to compile.
- **A `Prefer: return=representation` header on a Supabase insert requires a SELECT policy.**
  I spent hours reading a `42501` error as an authentication problem when the write was
  fine — it was the implicit read-back afterwards that was being refused.

## Known issues

- A faint speckle pattern is visible on path surfaces in Android builds. SSAO, bloom, the
  particle systems, specular highlights and anti-aliasing have each been isolated and ruled
  out; the current suspect is LDR colour grading in the mobile pipeline asset. Under
  investigation.
- Tiles and collectibles are still instantiated and destroyed rather than pooled, which can
  cause GC hitches on lower-end devices.

## Note on credentials

`Assets/Resources/SupabaseConfig.asset` contains a Supabase **publishable (anon) key**.
This key is designed to be shipped in client applications and is safe to expose — all
access is constrained by row-level security policies on the database. No service-role key
is present in this repository, and none should ever be added.

---

Built with Unity 6 during my internship at Orka Holding.
