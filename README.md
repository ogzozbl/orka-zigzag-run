# ZigzagRun — Orka Edition

A hyper-casual browser game built in Unity 6 (URP), themed as a brand engagement piece for
**Orka Holding** — the Turkish menswear group behind Damat, D'S Damat and Tween.

One tap. The ball never stops. Tap to turn the corner, miss it and you fall.

**Target platform: WebGL.** It runs in a browser, no install, no app store.

---

## Why I built this

Two reasons, one professional and one personal.

**The professional one.** Brand awareness in retail usually means an ad someone skips. A
hyper-casual game is the opposite — it is voluntary, it is short, and people replay it. If
the brand lives inside the thing people are choosing to do, the impression is earned rather
than bought. So the brief I set out to answer was: *how do you put a fashion brand in
someone's hands for five minutes at a time?*

**The personal one.** I want to work in the games industry. An internship at a fashion
holding is not the obvious route into it, so I treated this project as the way to make it
one — a chance to ship something real, end to end, and to have the scars to show for it.
Everything here is mine: the mechanic, the procedural generation, the shader and pipeline
work, the backend, the UI, and the two days I lost to a rendering bug I document further
down. That last part turned out to be the most useful thing I learned all summer.

### Making the brand inseparable from the game

The goal was never "clone Zigzag." It was to take a mechanic already proven to be
replayable and bind the brand to it:

- **The collectible is the brand.** Instead of a generic coin, the player collects the Orka
  eagle — a 3D model generated from the corporate logo and imported as a GLB. It is the
  single most-repeated visual action in the game.
- **The palette is a wardrobe.** The game cycles through colour themes (Grape Royale, Rose
  Ember, Citrus Sunset) drawn from a warm jewel-tone family that sits naturally alongside
  the brand's burgundy and gold. Sky, fog and path shift together, so a theme change reads
  as one deliberate look rather than three independent colour loops.
- **Progression rewards loyalty, not spend.** A fourth theme — *Altın Kartal* (Golden Eagle)
  — stays locked until the player has collected 25 eagles across all sessions. It gives a
  reason to come back that outlives any single run.
- **A leaderboard makes it social.** Scores post to a Supabase backend with one row per
  player, so the game works as a shared scoreboard across an office or at an event rather
  than a solitary high-score file.

---

## Why the web, and not a mobile app

The project was originally aimed at the Play Store. It ships as a WebGL build instead, and
the reason is worth recording because the decision was driven by evidence rather than
preference.

Late in development a rendering artifact appeared **only in Android builds** — a fine
speckle pattern scattered across the path surfaces (the full investigation is
[below](#the-speckle-bug)). The WebGL build was unaffected and looked correct. With the
internship on a fixed clock, that left a choice between spending the remaining time
chasing a platform-specific graphics bug, or shipping on the platform where the game
already looked right.

Project management — Samet Bey — made the call to ship the web version. It was the correct
one, and not only because of the bug:

- **Zero friction.** A link opens the game. No store listing, no review process, no
  install, no 25 USD developer account, no keystore to lose.
- **It fits how the game would actually be shared.** A QR code in a store window or a link
  in an internal message is exactly the distribution model a brand engagement piece wants.
  Asking someone to install an app to play for three minutes loses most of them.
- **Iteration is immediate.** A fix is a re-upload, not a store submission.

The Android work is not wasted — the project still contains the mobile-specific fixes
(touch handling, safe-area insets, ARM64/IL2CPP configuration) and can be switched back
whenever the rendering issue is resolved.

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

1. **The isometric framing is exact, forever.** No camera lerp means no drift, no jitter and
   no easing artifacts when the ball changes direction at speed.
2. **Floating-point precision never degrades.** The ball stays near the origin no matter how
   long the run lasts, so a 10-minute session is as stable as a 10-second one.
3. **Cleanup is free.** Tiles are children of `World`; when one scrolls out of range it is
   already in the right local space relative to everything else that moves with it.

The trade-off is that anything world-space has to be told about it explicitly. Trail
effects, for instance, cannot use a standard `TrailRenderer` — it would smear across the
screen as the world slides underneath it. `GoldTrail` instead uses a `ParticleSystem` with
a **custom simulation space** parented to `worldRoot`, so particles stay pinned to the
world they were emitted into.

---

## Running it

### What you need

| | |
|---|---|
| **Unity** | `6000.5.2f1` (Unity 6). The exact version is in `ProjectSettings/ProjectVersion.txt` |
| **Unity module** | **WebGL Build Support** — tick it during installation, or add it later via Unity Hub → Installs → ⚙ → Add modules |
| **Render pipeline** | Universal RP 17.5.0 (resolves automatically) |
| **Backend** | Optional. Only needed for the online leaderboard |

### 1. Get the project

```bash
git clone https://github.com/ogzozbl/orka-zigzag-run.git
```

### 2. Open it in Unity

1. Open **Unity Hub → Add → Add project from disk**, and select the cloned folder.
2. Open it with Unity `6000.5.2f1`.
3. **The first import takes several minutes.** Unity is compiling shaders and building the
   `Library` folder from scratch. This is normal and only happens once — do not interrupt it.
4. Open the scene: `Assets/Scenes/SampleScene.unity`.

### 3. Play in the Editor

Press **Play**. Tap or click anywhere to start, then click at each corner to turn.

> **Set the Game view to a portrait aspect ratio** — 9:16 or similar. The game is
> portrait-locked, and in a landscape viewport the UI will not sit where it belongs.

### 4. Build for the web

1. **File → Build Profiles**.
2. Select the **Web** profile (already configured in this repo) and click **Switch
   Platform**. The first switch re-imports every asset for WebGL and takes a while.
3. Click **Build**, and pick an output folder such as `Builds/Web`.

A WebGL build takes considerably longer than a desktop one — IL2CPP has to transpile the
whole game to WebAssembly. Ten to twenty minutes on a first build is normal.

### 5. Run the build locally

**A WebGL build will not work if you open `index.html` directly from your file system.**
The browser blocks the requests Unity needs over the `file://` protocol. You have to serve
it over HTTP:

```bash
cd Builds/Web
python -m http.server 8000
```

Then open **http://localhost:8000** in your browser.

Any static server works — `npx serve`, the VS Code Live Server extension, whatever you have.
Unity's own **Build And Run** also handles this automatically by spinning up a temporary
local server, which is the quickest way to check a build.

### 6. Publish it

The build folder is fully static, so anywhere that serves static files will host it:
itch.io (drag the folder in as a zip and tick "this file will be played in the browser"),
GitHub Pages, Netlify, or any web server.

If your host serves the compressed build files incorrectly you will see a console warning
about content encoding. The straightforward fix is **Project Settings → Player → Publishing
Settings → Compression Format → Disabled**, which trades a larger download for a build that
works anywhere without server configuration.

### Optional: enabling the leaderboard

Without a backend the game plays perfectly well — scores just stay local. To turn the
online leaderboard on:

1. Create a project at [supabase.com](https://supabase.com) (the free tier is plenty).
2. Run this in the Supabase SQL editor:

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

3. In Unity: **Create → ZigzagRun → Supabase Config**, paste your Project URL and `anon`
   public key into the asset, then drag it onto the `GameManager` component in the scene.

---

## <a name="the-speckle-bug"></a>The speckle bug — an investigation

This is the part of the project I would most want to talk about in an interview, so it gets
a section rather than a footnote.

### The symptom

After switching the build target to Android, the path surfaces developed a fine white
speckle pattern — scattered bright pixels across both the light top faces and the dark side
walls of the tiles. It was distracting enough to hurt readability of the path itself. The
WebGL build did not show it. Neither, at first, did the Editor.

### What I got wrong

My first several attempts were guesses dressed up as diagnoses. I would notice a plausible
culprit, change it, declare it fixed, and be told the problem was still there. I did this
with bloom thresholds, ground colours, material emission and light intensity. Each round
cost a build and a test cycle, and none of it converged, because I was reasoning from
"what could plausibly cause bright pixels" rather than from evidence.

The turning point was being told, bluntly, that I was going in circles and needed a
roadmap. That was fair. I stopped changing things and started eliminating them.

### The elimination

Each of these was tested in isolation — one change, one build, one verdict:

| Suspect | Method | Verdict |
|---|---|---|
| `GoldTrail` / `DustBurst` particles | Both systems disabled entirely in code | **Ruled out** — speckles persisted |
| SSAO | Found configured with blue-noise AO at 1 sample and no blur — a genuinely noisy setup. Disabled | Real defect, **not this one** |
| Bloom | Disabled permanently | Reduced glare, **speckles unaffected** |
| Shadow acne | Shadows disabled on the tile prefab | **Ruled out** |
| Specular highlights | `Mat_Ground` and `Mat_Wall` — smoothness lowered, specular and reflections off | **Ruled out** |
| NaN pixels | `Stop NaN` enabled on the camera | Fixed a separate blow-out, **speckles remained** |
| SMAA + MSAA interaction | SMAA disabled | **Ruled out** — mobile has MSAA off anyway |
| Render scale upscaling | Mobile renders at 0.8 with a bilinear filter | **Ruled out** — blurs, cannot add noise |
| Reflection probes | None active in the scene | **Ruled out** |

### The single most important thing I learned

Halfway through, I realised why my Editor testing had been worthless: **Unity selects a
quality tier per platform.** The Editor was rendering through `PC_RPAsset`, while the
Android build used `Mobile_RPAsset` — two separate pipeline assets with independent
settings. I had been "fixing" things in the Editor, confirming them in the Editor, and
shipping to a device that never read those settings at all.

If a visual bug is platform-specific, check the pipeline asset that platform actually uses,
before anything else. I would now check this first, not tenth.

### Where it stands

The current and untested-to-conclusion hypothesis is the LDR colour grading mode in the
mobile pipeline asset: with grading baked into a low-precision 8-bit LUT, the dithering
Unity applies to hide the resulting banding is a plausible source of a fine scattered
pattern, and it would explain why the artifact appears at equal density on both light and
dark surfaces — something no material or lighting explanation accounts for.

It is a hypothesis, not a conclusion. I have written it down as one rather than claiming a
fix I have not verified on a device, because that is the habit that cost me two days here.

The web build is unaffected, and that is what ships.

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
synthesised sample by sample into an `AudioClip`, and every UI panel is constructed in C#
rather than authored as a prefab. It keeps the download small — which matters a great deal
for a game that has to load in a browser tab — and keeps the repository readable as source
rather than as a pile of binaries.

---

## Other things I learned the hard way

- **`EventSystem.IsPointerOverGameObject()` lies on touchscreens.** The parameterless
  overload only inspects the mouse pointer. On touch devices you have to pass the active
  touch's `fingerId`, or a tap on a UI button registers as a gameplay tap as well.
- **Don't cache `ParticleSystem` module structs in fields.** Holding something like an
  `EmissionModule` across a domain reload throws *"Do not create your own module
  instances"*. Fetch the module fresh each time you need it.
- **`Handheld.Vibrate()` does not exist in WebGL builds.** It has to be guarded with
  `#if !UNITY_WEBGL` or the build fails to compile — one of several places where targeting
  the browser is not simply a smaller version of targeting a phone.
- **A `Prefer: return=representation` header on a Supabase insert requires a SELECT policy.**
  I read a `42501` error as an authentication problem for hours when the write itself was
  fine — it was the implicit read-back afterwards being refused.

## Known issues

- The Android-only speckle artifact described above. Not present in the web build.
- Tiles and collectibles are instantiated and destroyed rather than pooled. Harmless in a
  browser on a desktop machine; it would want fixing before any mobile release.

## Note on credentials

`Assets/Resources/SupabaseConfig.asset` contains a Supabase **publishable (anon) key**. This
type of key is designed to be shipped inside client applications and is safe to expose — all
access is constrained by row-level security policies on the database. No service-role key is
present in this repository, and none should ever be added.

---

Built with Unity 6 during my internship at Orka Holding.
