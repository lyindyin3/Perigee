# PERIGEE — Game Design Document

**Reader:** Claude Opus 5.5 implementing the game in Unity. **Engine:** Unity 6.3, URP with the 2D Renderer, C#. **Target:** WebGL (Yandex Games), desktop and mobile browsers, landscape and portrait.

## 0. Rules for the implementing AI

1. Work milestone by milestone (section 13). After each one the project compiles and is playable. Report in 5 lines or fewer, then stop.
2. Implement only what this document says. No extra features, tests, README, logging, try/catch, defensive null-checks, editor tools, or refactors of finished milestones. Serialized references are assumed assigned.
3. Every tunable number lives in `GameConfig` (ScriptableObject) with the default given here. No magic numbers in code.
4. If something is unspecified, take the simplest option consistent with this document and mention it in one line of the report. Ask only when blocked.
5. C# comments in Russian, only where logic is non-obvious. Namespace `Perigee`. Identifiers in English.
6. Unity MCP connected: create scene objects and settings through it. Not connected: write scripts only and give the manual steps from section 12 as a numbered checklist.
7. Use the Input System package (`UnityEngine.InputSystem`), never legacy `UnityEngine.Input`. Active Input Handling = Input System Package (New).

## 1. Concept

One-finger arcade defense. A star sits at the center; 1 to 3 satellites orbit it. Enemies fly toward the star. A satellite damages whatever it touches.

- **Hold** the pointer: satellites move outward. **Release**: they fall inward.
- Close to the star = fast, strong, heats up. Far = slow, weak, cools down, reaches distant enemies.
- The star has HP. Leaked enemies hurt it. The player cannot kill everything; the skill is choosing what to kill.
- A run lasts 180 s and ends with a boss. Crystals from kills fill an XP bar; each fill opens a pick-1-of-3 card screen (every card has a plus and a minus).
- Defeat restarts instantly. Crystals become dust, spent on permanent blueprints.

## 2. World, units, camera

- 2D, 1 unit = 1 world unit. Star at (0,0). No Unity physics: no Rigidbody2D, no colliders. All motion and collisions are manual circle checks: `distSq <= (ra+rb)^2`.
- Camera: orthographic, black background. `orthoSize = H * max(1, 1/aspect)` with H = 6.2, recomputed when the screen size changes. A field of radius 6.2 stays fully visible in landscape and portrait.
- Enemies spawn on a circle of radius 7.0 (off-screen).

### 2.1 Default constants (all in GameConfig)

starRadius 1.0 | starMaxHp 100 | H 6.2 | spawnRadius 7.0 | rMin 1.35 | rMax 4.6 | ringGap 0.8 | satRadius 0.2 | maxSatellites 3 | maxSatelliteRadius 6.6 | radialSpeed 8 | tMin 1.0 | kExp 1.1 | powerNear 3.0 | powerFar 1.0 | armorBase 2.0 | hitCooldown 0.2 | heatZoneMax 2.0 | coolZoneMin 2.6 | heatRate 0.25 | coolRate 0.35 | stunTime 1.5 | heatAfterStun 0.4 | resonanceHalfAngle 20 | resonanceMul 2.0 | crystalLife 8 | crystalRadius 0.1 | magnetSpeed 7

## 3. Simulation

### 3.1 Loop

`GameLoop` runs the simulation in `Update` with a fixed step of 1/60 s via an accumulator (`Time.deltaTime` clamped to 0.1, max 4 steps per frame). No steps run while the state is not `Playing` or while hit-stop is active (hit-stop is a real-time timer). Tick order each step: Satellites, Enemies, Boss, Collisions, Crystals, Director, Star, Cards (XP check), Fx.

### 3.2 Satellite model

- `baseR` is the controlled radius. Target = `rMax + stats.rMaxBonus` while holding, else `rMin`. Each step: `baseR = MoveTowards(baseR, target, radialSpeed * stats.radialSpeedMul * dt)`.
- Satellite `i` (0 = innermost) orbits clockwise at `r_i = min(baseR + i * ringGap, maxSatelliteRadius)`.
- Angular speed in deg/s: `w(r) = (360 / tMin) * (rMin / r)^kExp * stats.spinMul`. kExp comes from the current era. Defaults give a period of 1.0 s at rMin and about 3.9 s at rMax.
- Start: one satellite at angle 90 deg, `baseR = rMin`. A new satellite spawns at the middle of the largest angular gap between existing satellites and fades in over 0.3 s.
- `Power(r) = Lerp(powerNear, powerFar, InverseLerp(rMin, rMax, r)) * stats.powerMul` (InverseLerp clamped).

### 3.3 Heat (per satellite)

- `r_i <= heatZoneMax`: heat += heatRate \* stats.heatMul \* dt. `r_i >= coolZoneMin`: heat -= coolRate \* dt. In between: unchanged. Clamp 0..1.
- Heat reaches 1: the satellite is stunned for stunTime. It deals no damage, picks up nothing, keeps orbiting. After the stun heat = heatAfterStun.

### 3.4 Resonance

For any two non-stunned satellites whose shortest angular distance is <= `resonanceHalfAngle * stats.resonanceMul2` both deal damage x resonanceMul, and a gold zigzag thread is drawn between them.

### 3.5 Hits

- A non-stunned satellite overlaps an enemy and that enemy's hit cooldown for this satellite is 0: damage = `Power(r_i)` (x resonanceMul if in resonance). Set the cooldown to hitCooldown.
- Armored enemy (armor > 0): if damage < armor, no damage, silver sparks, 'clink' SFX, hit-stop 0.02 s. Otherwise normal damage.
- Normal hit: hit-stop 0.04 s, shards, SFX. Satellites are never damaged.
- Enemy HP <= 0: spawn crystals, remove, kill FX.

### 3.6 Armor ring

Silver dashed ring with a shield icon at the top. Radius where the innermost satellite deals exactly armorBase: `t = (powerNear - armorBase / stats.powerMul) / (powerNear - powerFar)`, `r = rMin + clamp01(t) * (rMax - rMin)`. Recomputed when powerMul changes. Rotates 10 deg/s. During the first Bastion of a tutorial run it pulses until the Bastion dies.

## 4. Entities

### 4.1 Star

Radius 1.0, maxHp 100. When an enemy touches it (`dist <= starRadius + enemyRadius`) the enemy deals its starDamage and is removed (Parasite and Boss excepted). HP <= 0: defeat.

### 4.2 Enemies (data-driven `EnemyDef` list in GameConfig)

| id | radius | hp | speed u/s | armor | starDamage | crystals | behavior |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Swarmer | 0.15 | 1 | 1.1 | 0 | 3 | 1 | Straight |
| Arrow | 0.12 | 1 | 5.5 | 0 | 12 | 2 | Straight |
| Bolt | 0.10 | 1 | 3.5 | 0 | 8 | 0 | Straight |
| Bastion | 0.55 | 6 | 0.55 | 2.0 | 25 | 6 | Straight |
| Parasite | 0.25 | 3 | 1.6 | 0 | drain | 3 | Attach |
| Shooter | 0.30 | 3 | 1.0 | 0 | none | 4 | Stopper |
| Hive | 0.60 | 8 | 0.7 | 0 | none | 8 | Burster |
| Boss | 0.90 | 45 | n/a | 0 | 10 per pass | 30 | Ellipse |

Enemy hp is multiplied by `era.hpMul`.

Behaviors:

- **Straight:** moves toward the center at its speed.
- **Attach (Parasite):** flies to the star. On contact it stops with its center at radius `starRadius + 0.15` on the contact angle, shows a pulsing red marker ring, and drains `min(2 + 0.6 * t, 8)` HP/s where t = seconds attached. It is hit by satellites only through normal overlap, so only a satellite with `r_i <= 1.6` reaches it.
- **Stopper (Shooter):** moves to radius 4.9, stops, and every 2.0 s fires a Bolt from its position at the star.
- **Burster (Hive):** when `r <= 2.4` it dies and spawns 8 Swarmers at random angles within 0.5 of itself (no star damage). If killed earlier it drops crystals and releases nothing.
- **Ellipse (Boss):** see 4.3.

### 4.3 Boss: Comet

Spawns at 180 s (fades in at apoapsis); the Director stops spawning. The star is a focus of its ellipse: periapsis 1.8, apoapsis 5.5, so `a = 3.65`, `e = 0.507`, `b = a * sqrt(1 - e^2)`. Position = `Rot(phi) * (a * (cos(E) - e), b * sin(E))`, `E = 2 * pi * t / 6.0`. At every periapsis pass it deals 10 star damage once and `phi += 40 deg`. A HUD bar shows its HP under the XP bar. Boss HP <= 0: victory. Its trail is long, so the player can see where it will dive.

### 4.4 Crystals

On enemy death spawn `crystals` crystals within 0.3 of the death position. Lifetime `crystalLife * stats.crystalLifeMul`, blinking during the last 2 s. A non-stunned satellite overlapping a crystal picks it up: dust +1, XP +1. Magnet: crystals within `stats.magnetRadius` of any satellite fly to it at magnetSpeed.

## 5. Director (spawn timeline)

`Director` reads `SpawnEvent { time, type, count, angleMode, spreadDeg }` from GameConfig. Angles come from `Rng`. Every event shows a telegraph before spawning: a blinking red marker at the screen edge on the spawn angle, lasting 1.2 s for Arrow, 1.0 s for Swarmer groups, 0.8 s for the rest. Swarmer groups spawn inside an arc of `spreadDeg` (default 30 deg) with 0 to 1.0 radius jitter beyond spawnRadius. `angleMode`: Random; Opposite (180 deg from the previous event's angle); Spread (multi-enemy events: angles separated by 90 deg, or evenly spread when stated). Counts are multiplied by `era.countMul` and rounded.

Ambient: 1 Swarmer every 3.0 s from 18 s to 175 s at a random angle. Events (seconds):

20 Swarm x10 | 30 Swarm x10 | 36 Bastion x1 | 46 Arrow x2 (Spread) | 56 Swarm x12 | 62 Arrow x3 (Spread) | 70 Parasite x1 | 80 Bastion x1 + Swarm x10 Opposite | 88 Parasite x2 (Spread) | 98 Shooter x1 | 108 Arrow x4 (one per quadrant) | 116 Hive x1 | 126 Swarm x14 + Shooter x1 Opposite | 136 Bastion x2 Opposite | 146 Hive x1 + Arrow x3 | 156 Parasite x2 + Swarm x12 | 164 Shooter x2 Opposite + Bastion x1 | 172 Arrow x5 (evenly spread) + Swarm x14 | 180 Boss

First run (`tutorialDone = false`): ignore everything before 20 s and use instead: 1 Swarmer x1 at speed x0.6 | 7 Swarm x8 | 14 Bastion x1. Ambient starts at 20 s.

## 6. Cards

XP: level L (starts at 0) needs `10 + 4 * L` crystals. When filled: state `CardPick` (simulation paused), 3 distinct random cards from the pool, excluding cards at max stacks. While satellites < 3 the first pick always includes ADD\_SAT. The chosen card gets stack +1, `Stats` is recomputed (base x cards x blueprints; multipliers multiply, bonuses add), surplus XP carries over.

| id | plus | minus | max stacks |
| --- | --- | --- | --- |
| HEAVY | powerMul x1.4 | radialSpeedMul x0.7 | 2 |
| SWIFT | radialSpeedMul x1.5 | powerMul x0.85 | 2 |
| ADD\_SAT | +1 satellite | heatMul x1.25 | 2 |
| MAGNET | magnetRadius +1.3 | Arrow speed x1.15 | 1 |
| COOLANT | heatMul x0.6 | powerMul x0.9 | 2 |
| STAR\_PLATING | star maxHp +25 and heal 25 | spinMul x0.9 | 2 |
| RESONATOR | resonance angle x1.8 | powerMul x0.9 | 1 |
| LONG\_ARM | rMaxBonus +0.6 | radialSpeedMul x0.85 | 2 |
| BIG\_SAT | satellite radius x1.5 | heatMul x1.2 | 1 |
| PULSE | every 8 s the star emits a shockwave: Swarmers within 2.5 die, other enemies within 2.5 take 1 damage | crystalLifeMul x0.6 | 1 |

## 7. Meta, modes, services

- **Save:** JSON in PlayerPrefs key `perigee_save` through `ISaveService`: dust, blueprint levels, bestTime, tutorialDone, erasUnlocked, runs, defeats. `tutorialDone` becomes true when the first run ends.
- **Dust:** crystals picked this run, +50 on victory, added on the result screen.
- **Blueprints** (menu screen), cost doubles per level:

| id | effect per level | levels | costs |
| --- | --- | --- | --- |
| HULL | star maxHp +10 | 5 | 50, 100, 200, 400, 800 |
| ACTUATORS | radialSpeedMul +5% | 5 | 50, 100, 200, 400, 800 |
| COOLANT\_BP | heatMul x0.95 | 5 | 50, 100, 200, 400, 800 |
| MAGNETISM | crystalLifeMul +12.5% (about +1 s) | 3 | 40, 80, 160 |

- **Eras** (`EraConfig` list, data only, no unique art): 1 Sun: kExp 1.1, hpMul 1.0, countMul 1.0. 2 Red Dwarf: spinMul x1.3, hpMul 1.3, countMul 1.2. 3 Heavy Star: kExp 1.5, hpMul 1.6, countMul 1.4. Era n+1 unlocks when the boss dies in era n.
- **Modes:** Normal (random seed) and Daily (seed = UTC date as yyyyMMdd). All gameplay randomness goes through `Rng` (System.Random); visual randomness uses UnityEngine.Random.
- **Score:** dust picked + 2 \* seconds survived + (victory ? 500 : 0). Best survived time is stored.
- **Services:** interfaces `IAdService` (ShowRewarded(Action\<bool>), ShowInterstitial()), `ISaveService`, `ILeaderboardService` (Submit(score)) with stub implementations that complete instantly. No real SDK in v1. Usage:
  - Rewarded 'Revive', once per run, offered on defeat: star HP = 50% of max, every non-boss enemy within 4.0 of the star dies (crystals drop), all heat = 0, star immune for 1.5 s.
  - Rewarded 'x2 dust' on the result screen.
  - Interstitial only after a defeat, only when runs >= 3 and defeats is even.

## 8. Flow and UI

States: Menu, Playing, Paused, CardPick, Victory, Defeat, Result. Retry restarts immediately with no loading screen.

**Input:** `HoldZone` is a full-screen transparent UI Image under all other UI (raycast target) implementing IPointerDownHandler and IPointerUpHandler. Pointer down = hold, pointer up or exit = release. Space key also = hold. Active only in `Playing`.

**Canvas:** Screen Space Overlay, Scale With Screen Size, reference 1920x1080, match 0.5, HUD anchored inside `Screen.safeArea`.

**HUD:**

- Top center: star HP bar (red, segmented, star icon on its left). Under it: XP bar (cream, segmented, card icon at its end). During the boss: boss HP bar under the XP bar.
- Top left: crystal icon + dust counter for this run. Top right: timer mm:ss counting up, pause button under it.
- Bottom left: icons of taken cards with stack numbers.
- Bottom center: control hint (hand icon, arrow outward = hold, arrow inward = release), visible only while `tutorialDone` is false.

**Screens:**

- CardPick: dim overlay, 3 large cards: title, plus line (cream), minus line (red). Tap to choose.
- Result, defeat: survived time, 'N s short of the record' (or 'new record'), dust earned, buttons Retry (primary), x2 dust (rewarded), Menu. Victory: time, dust, newly unlocked era, same buttons.
- Menu: Play, Daily, era selector (unlocked eras only), Blueprints, best time. Pause: Resume, Restart, Menu.
- All UI strings live in a static `Strings` class (Russian).

## 9. Visual spec

Soviet constructivist poster. Flat shapes, no gradients, no post-processing, unlit sprite material only (no Light2D).

Palette: background #0E0E0E, cream #EFE4C8, red #D6281F, gold #E3B23C, silver #B9BDC2. Gold = beneficial (crystals, resonance). Red = enemy or danger. Satellites are the highest-contrast objects on screen.

All sprites are generated at runtime by `ShapeFactory` (512 px textures, PPU 256, so a unit-radius shape uses `localScale = (r, r, 1)`): Circle, Ring(tickCount) (dashed ring), Triangle, Diamond, Wedge (60 deg sector), Hexagon, Rays(36), Rect.

| object | look |
| --- | --- |
| Star | cream circle r1.0 with black outline; 36 cream rays between r1.05 and r1.35 rotating 6 deg/s; scale pulse 1.0 to 1.03 at 1.5 Hz; HP below 30%: pulse 3 Hz and tint toward red |
| Satellite | cream diamond r0.2, black outline; TrailRenderer 0.35 s, width 0.12 to 0, cream; color lerps to red with heat, flickers above heat 0.8; stunned = gray and no trail |
| Crystal | gold diamond r0.1, spinning, blinks near expiry |
| Swarmer | red triangle, apex toward the star |
| Arrow | long thin red triangle (0.5 x 0.14) with 3 speed lines behind |
| Bolt | small red diamond |
| Bastion | red wedge, apex toward the star, silver outline, 3 rivet lines |
| Parasite | red circle with 6 legs; pulsing red ring when attached |
| Shooter | red hollow circle with a barrel line toward the star |
| Hive | red hexagon with 3 black hexagonal holes |
| Boss | red circle r0.9, TrailRenderer 1.2 s, width 0.9 to 0, red to cream |
| Resonance thread | gold LineRenderer, 6 segments, points re-randomized every 0.05 s |
| Telegraph | red arrow marker at the screen edge on the spawn angle (ray from the center clamped to the visible rect inset by 0.5); Arrow also gets a faint red line (alpha 0.3) toward the star |

**Background (always animated, always dimmer than gameplay objects):**

- Two parallel cream bars rotated 58 deg, widths 1.8 and 1.2, perpendicular offsets about -6.5 and +6.0, alpha 0.12 pulsing +-0.04 at 1.5 Hz. Each bar is two tiled copies sliding along its axis at 0.4 u/s with wrap-around.
- Six dashed rings at radii 1.7, 2.6, 3.5, 4.4, 5.3, 6.2 with tick counts 60, 48, 40, 32, 28, 24 and rotation in deg/s of +24, -18, +13, -10, +7, -5; alpha 0.35. The ring nearest to the innermost satellite's radius lerps to alpha 0.9 (rate 8/s).
- The armor ring from 3.6.

Sorting order: bars 0, rings 10, crystals 20, enemies 30, star 40, satellites 50, FX 60.

## 10. FX and audio

- **Fx:** `HitStop(sec)`, `Shake(amp, dur)`. Star damage = red flash 0.15 s + shake (amp 0.12, dur 0.12). Kill = 6 cream shard sprites flying outward for 0.4 s. Clink = 4 silver spark lines. Crystal pickup = small pop + punch-scale of the dust counter. Shards and sparks are pooled sprite objects ticked by `Fx`, not ParticleSystem.
- **Audio:** `AudioManager.Play(SfxId)`; `enum SfxId { Hit, Clink, Kill, Crystal, Resonance, StarHit, Stun, CardOpen, CardPick, BossIn, Win, Lose }`. Clips live in an `AudioBank` ScriptableObject and may be empty (empty = no-op). One looped drone AudioSource with `pitch = Lerp(0.8, 1.5, avgAngularSpeed / maxAngularSpeed)`.

## 11. Code structure (folder Assets/Perigee/, namespace Perigee)

| group | files |
| --- | --- |
| Core | GameConfig, GameLoop (state machine, fixed step, hit-stop), RunState (hp, dust, xp, level, time, card stacks), Stats (computed modifiers), Rng, Strings |
| Gameplay | InputHold, StarController, SatelliteSystem (orbit, heat, stun, resonance), EnemySystem (pool up to 200, behaviors, collisions, star contact), Director, CrystalSystem, BossComet, CardSystem, Blueprints |
| Presentation | ShapeFactory, BackgroundAnimator, ArmorRing, Fx, AudioManager, Hud, CardPickView, PauseView, ResultView, MenuView |
| Services | IAdService, ISaveService, ILeaderboardService, Stub implementations |

Performance: pools for enemies, crystals, shards and markers; no per-frame allocations; no LINQ inside the loop.

## 12. Scene setup (one scene `Game`)

- Main Camera (orthographic, URP 2D Renderer)
- Systems (empty object): GameLoop, SatelliteSystem, EnemySystem, CrystalSystem, Director, BossComet, CardSystem, Fx, AudioManager
- World: Star, Satellites (parent, filled at runtime), Background (Bars, Rings, ArmorRing)
- Canvas: HoldZone (first child), Hud, CardPickView, PauseView, ResultView, MenuView
- EventSystem with InputSystemUIInputModule
- Project: URP asset with the 2D Renderer, build target WebGL, TextMeshPro essentials imported.

## 13. Milestones


| # | Что делаем | Читать | Готово, когда |
|---|---|---|---|
| M1 | ShapeFactory, камера, звезда, кольца, спутник, управление удержанием | 2, 3, 9, 11, 12 | В Play Mode спутник кружит вокруг звезды; пока зажата мышь или пробел он уходит наружу до внешнего кольца, при отпускании падает к звезде за полсекунды |
| M2 | Рой, попадания, HP звезды, поражение | 3, 4, 9, 11, 15 | Враги летят к звезде, спутник убивает их касанием, прорвавшиеся отнимают HP, при HP 0 экран поражения, «Ещё раз» перезапускает мгновенно |
| M3 | Кристаллы, шкала опыта, HUD, карточки, пауза | 4, 6, 8, 11, 15 | Убитые враги роняют кристаллы, спутник их собирает, шкала заполняется, открывается выбор из 3 карточек, выбор применяется, пауза работает |
| M4 | Все враги, расписание, кольцо брони, нагрев, 2-3 спутника, нить | 3, 4, 5, 9, 11, 15 | За 180 с приходят все типы врагов по расписанию; Бастион не получает урон снаружи серебряного кольца; спутник краснеет и «глохнет»; нить вспыхивает при сближении |
| M5 | Босс, победа, итоги, сохранение, чертежи, меню, режимы | 4, 7, 8, 11, 15 | На 3:00 появляется Комета; победа и поражение показывают итоги; пыль и чертежи сохраняются после перезапуска |
| M6 | Эффекты, фон, звук | 9, 10 | Работают hit-stop, тряска, осколки, анимация колец и полос; звуковые вызовы срабатывают (клипы могут быть пустыми) |
| M7 | Сервисы-заглушки, обучение, WebGL | 5, 7, 8 | Заглушки рекламы вызываются при воскрешении, x2 и после поражения; первый забег показывает подсказки; WebGL-сборка запускается в браузере |
| M8 | Способности | 3, 4, 6, 8, 9, 11, 15 | Перед забегом выбирается способность; кнопка перезаряжается; все 5 работают как в 15.4; второй слот открывается на 3 уровне |

## 14. Out of scope for v1

Real Yandex SDK, cosmetics, localization beyond the `Strings` class, unique era mechanics (black hole pull and similar), unit tests, editor tooling, analytics.

## 15. Revision 1 (after prototype playtest)

This section overrides every conflicting value or rule in sections 2-13. Implement it as written and leave the older sections untouched.

**Playtest result:** best run 1:20, the boss was never reached, so the game is too hard. Approved as they are: the animated background rings, the star, and the resonance thread. `perigee-prototype.html` is frozen: it is a visual and feel reference only. Never modify it and never copy its balance numbers.

### 15.1 Reach fix (outer orbits must work)

Problem: the background rings extended to 6.2, but satellites reached only 4.6 (5.4 for the second one). The outer rings were dead and Arrows spawned out of reach.

- `ringMax = 6.2`, the outermost background ring. It replaces `maxSatelliteRadius`.
- The controlled range depends on the satellite count n: while holding, `target = ringMax - (n - 1) * ringGap + stats.rMaxBonus`, clamped so that the outermost satellite is at most 6.6. The outermost satellite therefore always reaches the outermost ring.
- `rMax` no longer limits control. It remains only as `powerFarRadius = 4.6`: `Power(r)` interpolates over \[rMin, powerFarRadius\] and stays at powerFar beyond it. The armor ring formula uses powerFarRadius instead of rMax.
- `kExp` 1.1 becomes 0.9, so the orbital period at ring 6.2 is about 3.9 s (it would be 5.3 s otherwise).

### 15.2 Value changes

| parameter | old | new |
| --- | --- | --- |
| starMaxHp | 100 | 150 |
| Arrow speed / starDamage / telegraph | 5.5 / 12 / 1.2 s | 2.6 / 9 / 1.5 s |
| Bastion hp / speed / starDamage | 6 / 0.55 / 25 | 5 / 0.35 / 20 |
| Swarmer starDamage | 3 | 2 |
| Bolt starDamage | 8 | 6 |
| Parasite drain per second | min(2 + 0.6t, 8) | min(1.5 + 0.4t, 5) |
| Ambient Swarmer interval | 3.0 s | 3.5 s |
| Director count multiplier (applied before era.countMul, min 1 per event) | 1.0 | 0.8 |
| heatRate / coolRate / stunTime | 0.25 / 0.35 / 1.5 | 0.20 / 0.45 / 1.0 |
| XP needed at level L | 10 + 4L | 8 + 3L |
| crystalLife | 8 s | 10 s |
| resonanceHalfAngle | 20 deg | 35 deg |
| RESONATOR card angle multiplier | x1.8 | x1.5 |
| Boss hp / starDamage per pass | 45 / 10 | 40 / 8 |

Design target: a new player reaches the boss on roughly the 3rd to 5th attempt. Tune through GameConfig only.

### 15.3 Arrow spawn

- Arrows spawn on ring 5.3 (second from the outermost), not at 7.0. All other enemies still spawn at 7.0.
- Arrow telegraph: a pulsing red wedge on ring 5.3 at the spawn angle plus the faint red line to the star, for 1.5 s. Other telegraphs are unchanged.

### 15.4 Active abilities (new)

Passive cards stay. On top of them the player gets abilities used by tapping a button.

**Slots.** Two. Before the run starts a pick screen shows all five abilities (name, pictogram, one-line effect, cooldown); the choice fills slot A. When level 3 is reached, the normal card screen is replaced once by a pick of 3 random abilities not yet owned; the choice fills slot B. Tutorial run (`tutorialDone = false`): slot A is PULSE, assigned automatically with no pick screen.

| id | source | effect | duration | cooldown |
| --- | --- | --- | --- | --- |
| REVERSE | satellites | rotation direction flips and stays flipped until the next use; spin x1.5 for 3 s | 3 s | 30 s |
| GROW | satellites | satellite and pickup radius x2, resonance angle x1.3 | 6 s | 35 s |
| PULSE | star | shockwave grows from the star to radius 3.0 in 0.35 s: Swarmers and Arrows die (crystals drop), other enemies take 2 damage and are pushed 1.0 outward; the boss takes 2 and is not pushed | instant | 45 s |
| CHILL | satellites | all heat = 0, stuns end, no heat gain | 5 s | 30 s |
| SURGE | satellites | radialSpeed x2 and Power x1.5 | 4 s | 40 s |

Cooldown starts at activation, not at the end of the duration. Pressing during an active effect or a cooldown does nothing. Timers tick only in state `Playing` (not in CardPick or Paused). Abilities start ready. Values live in GameConfig; the original suggestion was about one use per minute, so raise the cooldowns if abilities feel too frequent.

**Input and HUD.** Two round buttons (88 px, 16 px margin) at the bottom right: slot B on the left, slot A on the right. Keys: Q = slot A, E = slot B. A button shows a cream radial fill for the cooldown and a pulsing gold ring when ready; the pictogram is cream on black (REVERSE two circular arrows, GROW growing circle, PULSE concentric rings, CHILL cross with four ticks, SURGE three chevrons). Buttons are UI above `HoldZone`; a tap on a button never counts as hold. Hold state is `activePointers > 0` on `HoldZone`, so the player can hold with one finger and tap a button with another. Without a second finger the player releases to tap, and the orbit falls.

**Effects.** REVERSE: cream arc flash on the current orbit, satellite trails gold for 3 s. GROW: smooth scale over 0.2 s. PULSE: expanding cream ring plus shake (amp 0.15, 0.2 s). CHILL: satellites flash white for 0.2 s. SURGE: gold trails for 4 s.

**Implementation.** `AbilitySystem` holds the slots, cooldowns and active effects and ticks right after Satellites. Active effects apply as temporary multipliers in `Stats` (separate from card multipliers) and expire by timer.

### 15.5 New cards

The pool gains two cards (stack limits as in section 6):

| id | plus | minus | max stacks |
| --- | --- | --- | --- |
| ABILITY\_HASTE | all ability cooldowns x0.75 | powerMul x0.9 | 2 |
| ABILITY\_LONG | all ability durations x1.4 | cooldowns x1.15 | 1 |

### 15.6 Code and milestones

- New files: `AbilitySystem` (Gameplay), `AbilityButtonView` and `AbilityPickView` (Presentation).
- New milestone **M8 Abilities** after M4 (it may also be built before M5): AbilitySystem, five abilities, HUD buttons, run-start pick, slot B pick at level 3, the two new cards, effects. Read sections 03, 04, 06, 08, 09, 11 and 15.
- When implementing M1-M7, apply 15.1-15.3 and the value changes in 15.2 instead of the older numbers.
