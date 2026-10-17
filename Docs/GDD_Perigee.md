# PERIGEE — Visual GDD (V1)

**Reader:** Claude Opus 5.5 working in Unity. **Engine:** Unity 6.3, URP 2D Renderer. **Target:** WebGL (Yandex Games), desktop and phone browsers.
**What this is:** the game is finished by R4 (M1–M25, see `Docs/PROGRESS.md`). This document changes only how it looks. Gameplay, numbers, flow and save stay as they are. `Docs/archive/` is history: do not read it. `Docs/ArtPack.md` is for the owner: do not read it.

## 0. Rules

1. Work stage by stage (section 12). After each stage the game compiles, plays exactly as before, and looks better. Report in 5 lines or fewer, then stop.
2. Do not change gameplay: no collision radii, speeds, damage, simulation timings or UI flow. Slow motion and hit-stop stay as they are.
3. Art stays data (as in R4): looks live in prefabs, scene objects, materials and sprites set in the Inspector. Code only moves, scales, tints, fades, enables, pools and instantiates. New visual numbers go into GameConfig in a group «Вид» with the defaults given here, or into serialized fields of decor components.
4. Read `Docs/PROGRESS.md` Notes before touching a system. Each system belongs to one stage (column «Системы»); later stages only call it.
5. Do not touch the parallax background the owner added, except where a stage says so. The owner adds the font himself: never assign or create fonts; use the TMP default font asset.
6. Every effect obeys Settings «Качество» (section 9): Low must look clean and run fast.
7. New placeholder sprites are made through the MCP with the existing import settings (512 px, PPU 256), flat colours, transparent background. The owner replaces them later.
8. Comments in Russian, namespace `Perigee`, Input System only, DOTween only for visuals, always with `SetLink`.

## 1. Look

Soviet space poster printed on old paper: flat shapes, bold silhouettes, hard offset shadows, two tones per shape, a warm glow from the star, paper grain over everything.

- **Printed.** Paper texture and grain, hard shadows, two-tone shading. No soft gradients on objects (glow halos are the only soft thing).
- **The star is the light.** The star glows; the field gets darker toward the edge.
- **Alive.** Nothing stands still: idle motion on every enemy, squash on hits, particles on impacts.
- **Readable first.** In greyscale the satellite and the star still read at once; enemies are red, good things are gold.

## 2. Palette and shape rules

| token | colour | use |
| --- | --- | --- |
| Ink | #0E0E0E | background base, outlines, shadows |
| Cream | #EFE4C8 | satellite, star body, UI text, friendly lines |
| Cream Dark | #C9BC9A | shaded half of cream shapes |
| Red | #D6281F | enemies, danger, UI plates |
| Red Dark | #8E1A14 | shaded half of red shapes |
| Gold | #E3B23C | crystals, resonance, rare, rewards |
| Silver | #B9BDC2 | armor, shields, stun |

- One outline width on objects: 0.03 units (Ink), baked into the sprite.
- Two tones: every enemy, boss, crystal and the star has a darker half (lower-right) baked into its sprite. Placeholders made here follow this.
- Hard shadow under every gameplay object (3.2).
- UI panels have corners cut at 45 deg, never rounded.
- Gold only on beneficial things; Red never on the player's things.

## 3. Rendering

### 3.1 Materials (Assets/Perigee/Art/Materials)

| material | shader | use |
| --- | --- | --- |
| `SpriteUnlit` | URP Sprite Unlit (existing default) | every Body |
| `SpriteWhite` | existing | hit flash, unchanged |
| `Glow` | Shader Graph, sprite unlit, Additive, colour x alpha | halos, beams, light |
| `Dissolve` | Shader Graph, sprite unlit, alpha clip by a noise texture with `_Amount` 0..1 and an edge (`_EdgeColor`, `_EdgeWidth` 0.08) | enemy and boss deaths |
| `PaperMultiply` | Shader Graph, unlit, Multiply blend, tiling texture with `_Offset` | paper overlay |
| `UIGlow` | UI version of Glow | ready ability ring, rare card rim |

### 3.2 Shadows

- Every enemy, boss, satellite skin, crystal, Moon and the Star gets a child `Shadow` [R]: a SpriteRenderer with Body's sprite (code copies it once when the object is created, as with FlashOverlay), colour Ink at alpha `shadowAlpha` 0.35, offset `shadowOffset` (0.05, -0.05) in world space (code keeps it world-aligned when the object rotates), sorting one below Body.
- Shadow follows Body scale and squash; hidden while the object fades in and while a Phantom is phased.
- Quality Low: off.

### 3.3 Glow halos (fake bloom)

A `Glow` child [D]: soft round sprite `glow_soft` with the Glow material.

| object | diameter (units) | colour, alpha |
| --- | --- | --- |
| Star | 4.5 | era star tint, 0.35, `Pulse` 0.95–1.05 at 0.3 Hz |
| Satellite skins | 0.9 | Cream, 0.4; code moves it toward Red with heat |
| Crystal | 0.5 | Gold, 0.45 |
| Bosses | 1.6 x radius | Red, 0.25 |
| Resonance thread | a second LineRenderer, 3x width, Glow material | Gold, 0.3 |
| Flare sector, pulse, beacon, dome rings | a Glow copy at 2x width | their colour, 0.3 |

### 3.4 Paper, grain, edge shade

- `PaperOverlay` (scene): a sprite in front of the field covering the whole screen at any aspect, material `PaperMultiply`, texture `paper_tile` (512 px tileable, near-white cream with fibres and stains, so it barely darkens). World sorting 90 (above the world, below UI).
- Grain: decor component `GrainJitter` sets `_Offset` to a random value `grainFps` 12 times per second. Low: no jitter.
- `EdgeShade` (scene): sprite `vignette_soft` (transparent centre, Ink edges, alpha 0.5) scaled to the screen, sorting 89. Separate from the red `DangerVignette`.

### 3.5 Bloom (High only)

URP Volume with Bloom only: threshold 0.9, intensity 0.6, scatter 0.6. Only glows and the star reach it. Enabled only on High (9). Pixelation stays removed; no other post-processing.

## 4. Objects

### 4.1 Star

- Body: two-tone (skins keep working).
- `Corona` [D]: the existing Rays plus two copies at scales 1.25 and 1.5, alpha 0.5 and 0.25, spinning +6, -4, +2 deg/s.
- `Surface` [D]: sprite `star_churn` (soft blotches) inside the Body at alpha 0.15, `Spin` 3 deg/s, masked to the Body with a `SpriteMask`.
- Glow (3.3). On damage the Glow alpha jumps to 0.7 and returns in 0.3 s (on top of the existing flash and punch).
- Low HP: the Corona spins x2 and tints toward Red.

### 4.2 Satellite

- Glow (3.3); trail as now.
- **Squash and stretch** of Body (not the root): while baseR changes, scale along the motion 1 + `satStretch` 0.25 x (radial speed / radialSpeed), across its inverse; back smoothly (`squashLerp` 12 per second). On a hit: punch 0.15 for 0.1 s.
- Stunned: the existing grey tint plus `StunStars` [R]: 3 small silver `star_spark` sprites circling it while stunned.

### 4.3 Enemies

- Two-tone placeholders for all 18 enemies and both event objects.
- **Idle motion** with decor components on Body children (never the root): Swarmer wiggle (new decor component `Wiggle`: rotation amplitude, frequency, random phase; +-8 deg at 4 Hz), Arrow speed lines `Scroll`, Bastion plates `Pulse` 0.98–1.02, Parasite legs `Wiggle`, Shooter barrel punch on each shot (code punches child `Barrel` [R]), Hive holes `PulseAlpha`, Jelly non-uniform `Pulse` 0.92–1.08, Mine spikes `Spin`, Blinker dashes fast `PulseAlpha`, Eclipser rim `Spin`, Orbiter tail `Wiggle`, Torch flame `Pulse` and `Wiggle`.
- **Hit squash:** on a damaging hit Body scales 0.85 along the hit direction and 1.12 across, back in 0.12 s (with the existing recoil). Bosses: half.
- **Death:** the pooled object stays for `deathTime` 0.18 s with no collision; Body material becomes `Dissolve`, `_Amount` 0 → 1, edge Gold (Cream for Swarmers, so mass deaths do not flood the screen with gold); then back to the pool and material restored. Low: the existing quick fade.
- **Spawn:** the existing fade-in plus scale 0.6 → 1 (Ease.OutBack, over spawnFadeIn).
- Elites: the `EliteMark` spins and gets a Glow copy.

### 4.4 Bosses

- Glow (3.3) and a ground ring [D] (`ring_thin`, Red, alpha 0.3, slow `Pulse`).
- Death: Dissolve over 0.8 s with a Gold edge during the existing boss-kill slow motion, plus BossBurst (5).
- Phase change: the existing white flash, and the ground ring punches to 1.5x and back.

### 4.5 Crystals, rings, lines

- Crystal: two-tone diamond, Glow, `Glint` [D] (a four-point sparkle flashing every 1.2–2.0 s, random phase; decor component `Glint`).
- Orbit rings: tick marks every 15 deg (sprite `ring_ticks`); the highlighted ring (existing RingHighlight) gets a Glow copy at alpha 0.15.
- Armor ring: a Glow copy at alpha 0.15.
- Telegraphs: red wedge with an Ink outline, pulsing as now.
- Crit numbers: a small gold `star_spark` behind the number.

## 5. Particles

V1 allows `ParticleSystem` prefabs for decor effects. `ParticleFx` (Systems) keeps one pool per prefab (prewarm 4–8), plays at a position and returns finished systems (stop action callback). Gameplay never depends on them. Prefabs live in `ArtSet.particles[]` (labelled), unlit materials from 3.1.

| prefab | when | look | max particles |
| --- | --- | --- | --- |
| HitSparks | damaging hit | 6 cream sparks in a 70 deg cone along the hit, 0.2 s | 12 |
| KillBurst | kill | 10 red + 4 cream square bits with drag, 0.4 s | 16 |
| BigKillBurst | big or elite kill | 20 bits and a ring flash; gold for elites | 30 |
| BossBurst | boss death | 60 bits in 3 colours, 1.2 s, and 12 slow embers | 80 |
| CrystalPop | crystal picked | 5 gold sparks upward, 0.25 s | 8 |
| ResonanceSparks | thread forms | 8 gold sparks along the thread | 10 |
| StunSteam | stun | 6 silver puffs rising, 0.6 s | 8 |
| StarEmbers | star hit | 10 orange-red embers outward from the hit point, 0.5 s | 12 |
| Clink | armor or shield block | 5 fast silver sparks | 8 |
| DustTrail | satellite at full radial speed | 1 cream dot every 0.05 s behind it, 0.3 s | 10 |

- The existing shards and sparks stay. Low: half emission.
- Alive particles are capped at `particleCap` 300 (High) / 120 (Low); over the cap new bursts are skipped.

## 6. Camera

- **Hold zoom:** orthoSize = base x (1 + `holdZoom` 0.06 x t); t moves toward 1 while the outermost satellite is beyond 4.0, else toward 0, at `zoomLerp` 2 per second. The camera stays centred. The R4 boss zoom and shakes add on top. The parallax keeps working as it does.

## 7. Eras

Each era gets an ambient decor prefab (`ArtSet.eraAmbient[8]`, labelled), instantiated under World/Ambient at run start (sorting 5). All decor:

| era | ambient |
| --- | --- |
| 1 Солнце | slow cream dust motes drifting outward from the star |
| 2 Красный карлик | red-orange embers rising and fading; inside a warned flare sector they speed up |
| 3 Тяжёлая звезда | silver dust falling inward to the star |
| 4 Чёрная дыра | thin cream streaks spiralling inward counter-clockwise |
| 5 Туманность | large soft violet wisps drifting |
| 6 Газовый гигант | slow drifting ice specks (the striped planet is the backdrop picture) |
| 7 Аномалия | faint teal ripple rings spreading from random points |
| 8 Сердце Тьмы | black tendrils at the screen corners (`Wiggle`), sparse red embers |

Clouds, moons and anomalies get Glow copies at alpha 0.2. The star Glow takes the era star tint.

## 8. UI

### 8.1 Style kit (Assets/Perigee/Art/UI/Kit)

| sprite | 9-slice | use |
| --- | --- | --- |
| `panel` | yes | Ink panel, cream 3 px border, cut corners |
| `panel_red` | yes | headers, banners |
| `button`, `button_pressed`, `button_disabled` | yes | cream button with Ink text; pressed = shifted down 4 px without shadow; disabled = grey |
| `button_red` | yes | primary actions (ИГРАТЬ, СТАРТ, Купить) |
| `tab`, `tab_active` | yes | tabs |
| `plate_shadow` | yes | Ink rectangle placed as a hard shadow under panels and buttons, offset (6, -6) px |
| `divider` | no | thin cream line with a star in the middle |
| `star_icon` | no | five-point star for titles and decor |

- Every panel, button, tab, card and row uses the kit instead of plain images. Buttons have `plate_shadow` behind and swap to `button_pressed` while held (uGUI SpriteSwap), plus the existing punch.
- Main menu title plate is rotated -2 deg; nowhere else.

### 8.2 Screens

- **Main menu as a poster:** `MenuScene` (World, shown only in Menu): the star with Corona and Glow on the left third (portrait: top third), one decor satellite circling it with a trail (`OrbitDecor`, no simulation), the era 1 ambient, two diagonal red stripes behind the title, a few large faint `star_icon`s.
- **Transitions:** `ScreenWipe` (full-screen UI): a red diagonal band sweeps across in 0.35 s and the screen changes under it (Menu ↔ Playing, Playing → Result, Result → Menu). Low: a 0.2 s fade.
- **HUD:** bars sit on a `panel` strip with `plate_shadow`; the level badge is a red star plate; ability buttons get a cream ring frame and a `UIGlow` ring when ready (replacing the gold ring).
- **Cards:** `plate_shadow` behind each card; `UIGlow` rim on Rare and Evolution.
- **Result:** a big header plate (Victory: gold, Defeat: red) with a star; the dust total in a gold-framed box.
- **Banners and toasts:** `panel_red` with a star at the left.

## 9. Quality

`SettingsData.quality` (0 Low, 1 High; first launch: Low if `Application.isMobilePlatform`, else High). Settings row «Качество: Высокое / Низкое» (RU and EN). Applies at once.

| feature | High | Low |
| --- | --- | --- |
| Bloom | on | off |
| Shadows | on | off |
| Dissolve deaths | on | quick fade |
| Particles | full, cap 300 | half, cap 120 |
| Grain jitter | on | off |
| Era ambient | full | half emission |
| Screen wipe | band | fade |

«Простой фон» still hides background decor on top of this.

## 10. Budgets

- 60 fps with 150 enemies: Low on a mid phone, High on desktop.
- New textures: up to 1024 px for paper, backdrops and vignette, 512 px for the rest; new texture memory about 20 MB or less; compressed build grows by 4 MB or less.
- Every Body keeps `SpriteUnlit` so batching works; all glows share one `Glow` material.

## 11. Owner assets

Placeholders work until replaced: `paper_tile`, `vignette_soft`, `glow_soft`, `star_churn`, `glint`, `star_spark`, `ring_ticks`, the UI kit (8.1), two-tone enemy, boss, crystal and star sprites, era ambient pictures (tendrils, wisps), backdrops. Font: added by the owner in TMP Settings.

## 12. Stages

Колонка «Системы» — чьи это системы: другой этап их только вызывает.

| # | Что делаем | Системы | Читать | Готово, когда |
|---|---|---|---|---|
| V1 | Основа вида: материалы 3.1, бумага, зерно и затемнение краёв 3.4, Bloom 3.5, настройка «Качество» 9 (выключатели для будущих эффектов уже есть), заглушки `paper_tile`, `vignette_soft`, `glow_soft` | материалы, PaperOverlay, EdgeShade, Volume, SettingsData.quality, SettingsView | 0, 1, 2, 3.1, 3.4, 3.5, 9, 10 | Экран выглядит напечатанным на бумаге, зерно живое, края поля темнее центра; на «Высоком» звезда и кристаллы светятся, на «Низком» свечения нет; игра идёт как раньше |
| V2 | Объекты поля: тени 3.2, ореолы 3.3, звезда 4.1, спутник 4.2, враги 4.3 (двухтоновые заглушки, движение в покое, сжатие при ударе, растворение при смерти, появление), боссы 4.4, кристаллы, кольца, линии 4.5 | Fx (тени, ореолы, сжатие, растворение), Visual, префабы врагов, боссов, спутников, кристалла, Star, Rings, ArmorRing | 2, 3.2, 3.3, 4 | У каждого объекта тень со сдвигом; у звезды свечение и три слоя лучей; спутник вытягивается при взлёте и падении; враги шевелятся, сплющиваются при ударе и растворяются с золотым краем; кристаллы поблёскивают |
| V3 | Частицы 5: пул ParticleFx, 10 префабов, лимит | ParticleFx, вызовы из Fx | 5 | Удар, убийство, подбор кристалла, оглушение, удар по звезде и смерть босса дают частицы из таблицы; на «Низком» вдвое меньше; со 150 врагами не больше лимита |
| V4 | Камера 6 и эры 7: отъезд при удержании, 8 префабов окружения, ореолы облаков, лун и аномалий | камера, EraAmbient | 6, 7 | При удержании камера чуть отъезжает и плавно возвращается; у каждой эры своё живое окружение; параллакс владельца работает как раньше |
| V5 | Интерфейс 8: набор 8.1 на всех экранах, тени под панелями и кнопками, нажатая кнопка, меню-плакат, переходы-шторки, HUD, карточки, итоги, баннеры | все View, Hud, MenuScene, ScreenWipe | 8 | Все панели и кнопки одного стиля со срезанными углами и тенью; кнопка «вдавливается» при нажатии; в меню за кнопками крутится звезда со спутником; между меню, боем и итогами проходит красная полоса |
| V6 | Проверка: обе настройки качества на всех экранах и в эрах 1–8, портрет и альбом, WebGL-сборка, замер 150 врагов, размер сборки, список непроверенного руками | всё (только проверка и правки) | 9, 10 | Сборка запускается; 60 кадров на «Низком» со 150 врагами; сборка выросла не больше чем на 4 МБ; отчёт со списком |

## 13. Out of scope

Gameplay changes, new content, fonts, changes to the owner's parallax, Light2D, normal maps, post-processing other than Bloom, screen-reading shaders (distortion, blur), new sounds.
