# PERIGEE — Game Design Document

**Reader:** Claude Opus 5.5 implementing the game in Unity. **Engine:** Unity 6.3, URP with the 2D Renderer, C#. **Target:** WebGL (Yandex Games), desktop and mobile browsers, landscape and portrait.
**Version:** R3, the full game. `Docs/archive/` is history: do not read it. `Docs/ArtPack.md` is for the owner (art prompts): do not read it.

R2.1 was built as M1–M8 and M5A. R3 keeps the core (hold and release, orbit trade-off, heat, resonance, star HP, 180 s run with a boss, cards with a plus and a minus, art as data) and adds the rest of the game:

- hit feedback that makes damage on big enemies visible, and animated HUD bars (M9);
- seven abilities instead of five: ANCHOR, CHILL and SURGE are removed; REVERSE, STASIS, ECHO, PULL and DOME are new (M10);
- 30 cards instead of 12, coloured by family, some rare, some only in one era (M11, M13);
- six new enemies (M12) and three new bosses (M13);
- four eras, each with its own rule, enemies, boss, cards and dust rate (M12, M13);
- a blueprint tree in the style of the runes of "20 Minutes Till Dawn", an ability shop and skins (M14);
- a main menu with settings, an era screen, Endless, Daily with a modifier, and Challenges (M15);
- a final pass (M16).

Sound assets and ads are out of R3: the existing stubs and the empty AudioBank stay as they are.

## 0. Rules for the implementing AI

1. Work milestone by milestone (section 16). After each one the project compiles and is playable. Report in 5 lines or fewer, then stop.
2. Implement only what this document says. No extra features, tests, README, logging, try/catch (one exception: SaveService, section 8.1), defensive null-checks, editor tools, or refactors that no milestone asks for. Serialized references are assumed assigned.
3. Every tunable number lives in `GameConfig` (ScriptableObject), or in a list inside it, with the default given here. No magic numbers in code. Systems read GameConfig values when they use them, not cached copies made in Awake, so a value changed in the Inspector during Play Mode applies at once.
4. If something is unspecified, take the simplest option consistent with this document and mention it in one line of the report. Ask only when blocked.
5. C# comments in Russian, only where logic is non-obvious. Namespace `Perigee`. Identifiers in English.
6. Unity MCP connected: create scene objects, prefabs and settings through it. Not connected: write scripts only and give the manual steps as a numbered checklist in plain words.
7. Use the Input System package (`UnityEngine.InputSystem`), never legacy `UnityEngine.Input`.
8. Art is data, not code (section 11). Everything visible is a prefab or a scene object whose sprites, colours, sizes and sorting are set in the Inspector; the pictures that code needs are listed in `ArtSet`. Code moves, rotates, scales, tints, fades, enables and pools these objects and instantiates prefabs. It never builds sprites, textures or UI from scratch (`new GameObject`, `AddComponent`, `new Texture2D`). New art in R3 starts as placeholders (11.3); the owner replaces them later.
9. R3 changes finished code. A milestone may change earlier code where its row in section 16 says so, and deletes what R3 removes (ANCHOR, CHILL, SURGE, the old card list, the old blueprints, `AnchorRing`, `SurgeTrail`). Names follow the existing code: `EnemyType`, `GameState`, `Visual`, `BossComet` and so on. `Docs/PROGRESS.md` (Notes) describes the current code: read it before changing a system.
10. GameConfig and ArtSet are assets: a new list or a changed default in code does not reach an existing asset. When a milestone adds or replaces list entries (cards, abilities, enemies, eras, blueprints), it writes them into the asset too and says so in the report.
11. `GameConfig.debugUnlockAll` (bool, default false): while true, every era, mode, ability, challenge and skin is available, nothing is written to the save for it, and dust is not spent. It lets each milestone be tested before the meta (M14) exists.

## 1. Concept

One-finger arcade defense. A star sits at the center; 1 to 4 satellites orbit it. Enemies fly toward the star. A satellite damages whatever it touches.

- **Hold** the pointer: satellites move outward. **Release**: they fall inward.
- Close to the star = fast, strong, heats up. Far = slow, weak, cools down, reaches distant enemies.
- The star has HP. Leaked enemies hurt it. The player cannot kill everything; the skill is choosing what to kill.
- A run lasts 180 s and ends with the era's boss. Crystals from kills fill an XP bar; each fill opens a pick-1-of-3 card screen (every card has a plus and a minus). Two active abilities are used by tapping buttons.
- Four eras (star systems). Each has a rule that changes how orbits play, two new enemies, its own boss and its own cards. A victory opens the next era.
- Crystals become dust. Dust buys blueprints (permanent passive upgrades in a tree), new abilities and skins.
- Modes: Run (era by era), Endless, Daily (one modifier a day), Challenges (fixed rule sets with rewards).

## 2. World, units, camera

- 2D, 1 unit = 1 world unit. Star at (0,0). No Unity physics: no Rigidbody2D, no colliders. All motion and collisions are manual circle checks: `distSq <= (ra+rb)^2`. Collision radii are GameConfig values and do not depend on the sprites; the default art is drawn to match them (section 11.2).
- Camera: orthographic. `orthoSize = H * max(1, 1/aspect)` with H = 6.2, recomputed when the screen size changes. A field of radius 6.2 stays fully visible in landscape and portrait. The background colour comes from the era (7.2).
- Enemies spawn on a circle of radius 7.0 (Arrows on 5.3). Along the long screen axis this circle is visible, so enemies fade in (section 5).

### 2.1 Default constants (all in GameConfig)

starRadius 1.0 | starMaxHp 120 | H 6.2 | spawnRadius 7.0 | arrowSpawnRadius 5.3 | rMin 1.35 | ringMax 6.2 | satMaxRadius 6.2 | ringGap 0.8 | satRadius 0.2 | maxSatellites 4 | radialSpeed 8 | tMin 1.0 | powerNear 3.0 | powerFar 1.0 | powerFarRadius 4.6 | powerFloor 1.0 | armorBase 2.0 | hitCooldown 0.2 | heatZoneMax 2.0 | coolZoneMin 2.6 | heatRate 0.35 | coolRate 0.25 | stunTime 1.0 | heatAfterStun 0.4 | resonanceHalfAngle 35 | resonanceMul 2.0 | critMul 2.0 | crystalLife 10 | crystalRadius 0.14 | magnetSpeed 7 | spawnFadeIn 0.25 | telegraphRadius 6.6 | satFadeIn 0.3

Visual constants (also GameConfig): stunTint #B9BDC2 at alpha 0.6 | starHitFlashColor #D6281F | heatArcRadius 0.32 | prewarm counts per pool (enemy prewarm lives in `EnemyDef.prewarm`; crystal 60, shard 40, spark 20, each marker type 8, resonance thread 3, damage number 40, echo 4).

`maxSatellites` grows from 3 to 4 in R3, but the 4th satellite is reachable only with the QUADRIGA blueprint (8.3). kExp, spinMul and the other era numbers come from the current era (7.2).

## 3. Simulation

### 3.1 Loop

`GameLoop` runs the simulation in `Update` with a fixed step of 1/60 s via an accumulator (`Time.deltaTime` clamped to 0.1, max 4 steps per frame). No steps run while the state is not `Playing` or while hit-stop is active (hit-stop is a real-time timer, section 12).

Tick order each step: Satellites (and echoes), Abilities, EraRule, Enemies, Boss, Collisions (hitters vs enemies and boss, saw thread, queued chain blasts), Crystals, Director, Star (regen, drain), Cards (beacon, XP check), Fx, Hints.

**Enemy time.** `GameLoop.EnemyTimeScale` is `stasisSlow` 0.25 while STASIS is active, otherwise 1. Enemies, Bolts, bosses, Shooter and Pulsar timers, Parasite drain, Phantom phases and the vortex drift use `dt * EnemyTimeScale`. Satellites, crystals, the Director, flares and ability timers use plain `dt`.

### 3.2 Satellite model

- `baseR` is the controlled radius. While holding, target = `ringMax - (n - 1) * ringGap` (n = satellite count), so the outermost satellite always reaches the outermost ring. Otherwise target = `restR` (`rMin`; `highOrbitMin` 3.0 with the HighOrbit modifier, 9.5).
- Each step: `baseR = MoveTowards(baseR, target, radialSpeed * stats.radialSpeedMul * k * dt)`, where k = `era.riseMul * stats.radialOutMul` when baseR grows and `era.fallMul` when it shrinks (era values are 1 except in the Heavy Star era, 7.3; radialOutMul comes from SLINGSHOT's minus).
- Satellite `i` (0 = innermost) orbits at `r_i = min(baseR + i * ringGap, satMaxRadius)`.
- **Direction.** `orbitDir` is -1 (clockwise, angle decreases) at run start. Each step: `angle_i += orbitDir * w(r_i) * dt`. REVERSE (6.4) and the AutoReverse modifier (9.5) flip it.
- Angular speed in deg/s: `w(r) = (360 / tMin) * (rMin / r)^era.kExp * stats.spinMul`. With kExp 0.9 the period is 1.0 s at 1.35, 3.0 s at 4.6 and 3.9 s at 6.2.
- **Count.** satellites = min(1 + stats.extraSatellites, 3 + stats.addSatBonus) (the cap is 4 only with QUADRIGA, 8.3). extraSatellites = ADD\_SAT stacks + the TWIN blueprint (8.3). SingleSat modifier: always 1.
- Start: one satellite at angle 90 deg (two with TWIN, the second at 270), `baseR = restR`. A new satellite becomes the outermost (highest i), spawns at the middle of the largest angular gap and fades in over satFadeIn. While fading it deals no damage, collects nothing and is ignored by resonance.
- `Power(r) = max(Lerp(powerNear, powerFar + stats.powerFarBonus, InverseLerp(rMin, powerFarRadius, r)) * stats.powerMul, powerFloor)`. InverseLerp is clamped.

**Hitters and echoes.** Everything that can hit or collect is a hitter. `SatelliteSystem` exposes `HitterCount`, `HitterPosition(k)`, `HitterActive(k)`, `HitterDamage(k)`, `HitterHeat(k)` and `HitterRadius`. Hitters 0..count-1 are the satellites. While ECHO is active, hitters count..2*count-1 are echoes: echo i sits on satellite i's orbit at `angle_i - orbitDir * echoLag` (35 deg, i.e. behind it), deals `echoDamageMul` 0.6 of what satellite i deals without resonance, collects crystals, never heats, never stuns and is ignored by resonance. An echo is active while its satellite is active. Echo visuals are a pooled `Echo` prefab from ArtSet. Enemy hit-cooldown arrays have `2 * maxSatellites` slots.

### 3.3 Heat (per satellite)

- `r_i <= heatZoneMax`: heat += heatRate \* stats.heatMul \* dt. `r_i >= coolZoneMin`: heat -= coolRate \* dt. In between: unchanged. Clamp 0..1. With heatMul 1, a cold satellite at rest stuns after 2.9 s (1.7 s after a stun), and cooling from 1 to 0 takes 4 s.
- Extra heat sources: flares (7.3) and hitting a Torch (4.2), both multiplied by stats.heatMul.
- Heat reaches 1: the satellite is stunned for stunTime. It deals no damage, picks up nothing, keeps orbiting, and its heat stays at 1. After the stun heat = heatAfterStun.

### 3.4 Resonance

For any two active satellites whose shortest angular distance is <= `resonanceHalfAngle * stats.resonanceAngleMul`, both deal damage x `stats.resonanceMul` (= resonanceMul 2.0 + the TUNING\_FORK bonus), and a `ResonanceThread` (11.4) is shown between them. Its label shows the multiplier ("×2", "×2.3"). The multiplier never stacks: a satellite in two pairs still deals it once.

**Saw thread** (card SAW\_THREAD): every enemy or boss whose circle touches the straight segment of a shown thread (`distance(center, segment) <= radius + sawWidth` 0.08) takes `sawDamage` 1 at most every `sawInterval` 0.25 s per enemy. Saw damage ignores armor and shields.

### 3.5 Hits

For each active hitter k and each enemy (or boss part) that is not fading, not phased (Phantom), overlaps it and has hit cooldown 0 for k:

1. `damage = Power(r_k) * resonance * stats.hitMul(k) * echo`. resonance = stats.resonanceMul if k is a resonating satellite, else 1. echo = echoDamageMul for an echo, else 1. `hitMul(k)` multiplies the active windows: REVERSE x`reverseHitMul` 1.5; SLINGSHOT x`slingshotMul` 1.5; SOLAR\_SAIL x`solarSailMul` 2; FIREBIRD x`firebirdMul` 1.5 when the hitter's heat >= `firebirdHeat` 0.7.
2. Crit: if `simRng.Range(0, 1) < stats.critChance`, damage x critMul.
3. Shield (Shielder, 4.2): if the shield is up and faces the hitter, the hit is blocked: shield hits left -1 (CROWBAR: the shield breaks at once), clink feedback, no damage.
4. Armor: `effectiveArmor = max(armor - stats.armorPierce, 0)`. If damage < effectiveArmor: no damage, clink feedback.
5. Otherwise `hp -= damage`, hit feedback (3.7). Then, in order:
   - Torch: the hitter (if a satellite) gains `heatOnHit` x stats.heatMul heat, unless HEATPROOF;
   - BURN: if the hitter's heat >= `burnHeatMin` 0.5, the enemy burns for `burnTime` 3 s (a new burn restarts the timer);
   - EXECUTE: a non-boss enemy with `hp <= maxHp * executeFraction` (0.25) dies;
   - CROWBAR: Orbiters take this hit's damage twice.
6. hp <= 0: kill (4.5). Set the hit cooldown for k to hitCooldown.

Armor and shields apply to satellite and echo hits only. PULSE, BEACON, flares, the saw thread, chain blasts, burn and the horizon ignore both.

**Damage over time** (Enemies tick, with enemy time): burning enemies lose `burnDps` 1 per second; with EVENT\_HORIZON every enemy and boss part within `horizonRadius` 1.8 of the center loses `horizonDps` 2 per second.

**Star contact.** An enemy that touches the star (`dist <= starRadius + radius`) deals `starDamage * stats.starDamageMul` and is removed (Parasite and bosses excepted). stats.starDamageMul = CORONA x REFLECTOR blueprint. While DOME is active (6.4) the star takes nothing from any source and enemies that touch it are destroyed with shards and no crystals.

### 3.6 Armor ring

Silver dashed ring (scene object `ArmorRing`, 11.3). It sits at the radius where the innermost satellite deals exactly the effective armor of a Bastion: `A = armorBase - stats.armorPierce`; `t = (powerNear - A / stats.powerMul) / (powerNear - (powerFar + stats.powerFarBonus))`, `r = rMin + clamp01(t) * (powerFarRadius - rMin)`. Recomputed whenever powerMul, powerFarBonus or armorPierce changes, abilities included. Hidden if A <= 0 or Power at powerFarRadius >= A. During the first Bastion of the tutorial run it pulses until that Bastion dies.

### 3.7 Hit feedback

So that damage on big enemies reads clearly:

- **Flash.** Every damaging hit shows the enemy's `FlashOverlay` (alpha `hitFlashAlpha` 0.85 fading to 0 in `hitFlashTime` 0.1 s). FlashOverlay is a SpriteRenderer child of Body with the same sprite as Body (code copies `Body.sprite` when the object is created, so it follows the owner's art), material `SpriteWhite` (an unlit sprite Shader Graph that draws the sprite's shape in solid white, alpha from the renderer colour) and sorting one above Body. A colour tint cannot turn a red sprite white, so `Visual.Flash` alone is not enough here.
- **Recoil.** The enemy's `Body` child (not the root, so the position stays exact) is punched by `recoilDistance` 0.08 (bosses `bossRecoilDistance` 0.04) along the direction from the hitter to the enemy and returns in `recoilTime` 0.12 s (DOTween `DOPunchPosition`, SetLink).
- **HP ring.** Every enemy prefab and boss prefab has an `HpRing` child (11.4). It is used when the enemy's max HP after era scaling is >= `hpRingMinHp` 3, and shown according to Settings (10.4): AfterHit (default; it appears at the first damage and stays), Always, Never. Its arc = hp / maxHp x 360 deg, shrinking smoothly (`hpRingLerp` 10 per second) so each hit's bite is visible.
- **Damage stages.** Optional `Cracks` child with a `DamageStages` component (`Sprite[] stages`). Stage index = `floor((1 - hp / maxHp) * (stages.Length + 1)) - 1`; -1 = hidden. An empty array shows nothing. The owner adds crack sprites for big enemies and bosses.
- **Damage numbers.** `Fx.Number(position, value, kind)`, pooled `DamageNumber` prefab (TextMeshPro world text). Only for enemies with max HP >= `numberMinHp` 2 and bosses, and only while Settings.damageNumbers is on. Kinds: Normal (cream), Crit (gold, x1.4 size, text "N!"), Blocked (silver "0"). Text format "0.#". The number rises `numberRise` 0.5 over `numberTime` 0.6 s, fades in the last half, starts at a random x offset of +-0.15, and its scale is `numberScale` 0.35 x (1 + 0.08 x min(value, 10)).
- **Big kills.** Killing an enemy with radius >= `bigRadius` 0.4 doubles the kill shards and adds a shake (amp `bigKillShake` 0.06, 0.08 s).
- **Clink.** Sparks (as now) plus a Blocked number.

## 4. Entities

### 4.1 Star

- Radius 1.0, max HP = starMaxHp + stats.starMaxHp bonuses (cards, HULL blueprint). HP <= 0: SECOND\_SUN if available, otherwise Defeat.
- **Regen:** stats.regen HP per second (PHOTOSPHERE card, CORE blueprint), never above max.
- **SECOND\_SUN** (blueprint, 8.3): once per run, when HP reaches 0, HP = `secondSunHp` 0.25 x max, a free PULSE wave fires (no cooldown change), and the star is immune for `reviveImmuneTime` 1.5 s. This happens before the Defeat screen and before the ad revive.
- **GlassStar** modifier (9.5): max HP x `glassStarHpMul` 0.4 and no healing of any kind.
- Low HP (< 30%): the Body is tinted toward red (as now).

### 4.2 Enemies (data-driven `EnemyDef` list in GameConfig)

`EnemyType` keeps its existing order and appends the new types, so existing arrays stay valid: Swarmer, Arrow, Bolt, Bastion, Parasite, Shooter, Hive, Boss (= Comet), Splitter, Torch, Orbiter, Shielder, Devourer, Phantom, Twin, Colossus, Pulsar.

| type | name (RU) | radius | hp | speed u/s | armor | starDamage | crystals | behavior | first era |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Swarmer | Рой | 0.20 | 1 | 1.1 | 0 | 3 | 1 | Straight | 1 |
| Arrow | Стрела | 0.15 | 1 | 2.6 | 0 | 9 | 2 | Straight, spawns at arrowSpawnRadius | 1 |
| Bolt | Снаряд | 0.15 | 1 | 3.5 | 0 | 4 | 0 | Straight | 1 |
| Bastion | Бастион | 0.55 | 5 | 0.35 | 2.0 | 20 | 6 | Straight | 1 |
| Parasite | Паразит | 0.25 | 5 | 1.6 | 0 | drain | 3 | Attach | 1 |
| Shooter | Стрелок | 0.30 | 2 | 1.0 | 0 | none | 4 | Stopper | 1 |
| Hive | Улей | 0.60 | 4 | 0.7 | 0 | none | 8 | Burster | 1 |
| Splitter | Дробитель | 0.32 | 2 | 0.8 | 0 | 8 | 2 | Straight + split | 2 |
| Torch | Факел | 0.28 | 3 | 0.9 | 0 | 10 | 3 | Straight + heat on hit | 2 |
| Orbiter | Кружащий | 0.25 | 3 | 1.0 | 0 | 8 | 3 | Orbit | 3 |
| Shielder | Щитник | 0.35 | 3 | 0.6 | 0 | 12 | 4 | Straight + shield | 3 |
| Devourer | Пожиратель | 0.40 | 4 | 0.6 | 0 | 15 | 3 + eaten | Seek | 4 |
| Phantom | Призрак | 0.22 | 1.5 | 1.3 | 0 | 6 | 2 | Straight + phase | 4 |

Bosses (Boss = Comet, Twin, Colossus, Pulsar) are in 4.3.

- Enemy hp is multiplied by `era.hpMul` only when its base hp >= `eraHpMinBaseHp` 2. Swarmers, Arrows, Bolts and Phantoms keep their base hp. Bosses are never multiplied by the era.
- New `EnemyDef` fields (0 = off): `splitCount`, `heatOnHit`, `shieldHits`, `phaseOn`, `phaseOff`, `seekRadius`, `orbitRadius`, `orbitDeg`, `orbitInSpeed`. `EnemyBehavior` gains `Orbit`, `Seek`, `Colossus`, `Pulsar`.
- Each enemy keeps `maxHp` (after era scaling) for the HP ring, EXECUTE and damage stages.

Behaviors:

- **Straight:** moves toward the center at speed x the event's speedMul. In the Black Hole era it also drifts around the center (vortex, 7.3).
- **Attach (Parasite):** flies to the star. On contact (`dist <= starRadius + 0.25`) it snaps to center radius `starRadius + 0.15` on the contact angle, enables its `AttachMarker` child, and drains `min(1.5 + 0.4 * t, 5)` HP/s, where t = seconds attached. Only a satellite with `r_i <= 1.6` reaches it. If PULSE or PULL moves it, it detaches, t resets and it flies back. DOME makes an attached Parasite fall off and die (no crystals).
- **Stopper (Shooter):** moves to `shooterStopRadius` 5.8 and stops. It fires a Bolt at the star 2.0 s after stopping and then every `shooterFireInterval` 2.5 s. If pushed or pulled, it walks back to its stop radius.
- **Burster (Hive):** at `r <= hiveBurstRadius` 3.0 it dies and spawns `hiveSwarmers` 8 Swarmers at uniform positions in a disc of radius 0.5 around itself (simRng). No star damage. If killed earlier, it drops crystals and releases nothing.
- **Split (Splitter, `splitCount` 3):** Straight. When killed (not when it reaches the star) it spawns `splitCount` Swarmers at uniform positions in a disc of radius `splitSpread` 0.3 around itself (simRng) with speedMul `splitSpeedMul` 1.3. They have no telegraph.
- **Torch (`heatOnHit` 0.25):** Straight. Each satellite hit on it heats that satellite (3.5). Echo hits do not heat.
- **Orbit (Orbiter, `orbitRadius` 4.2, `orbitDeg` 50, `orbitInSpeed` 0.2):** moves straight in until `r <= orbitRadius`, then circles counter-clockwise (angle grows by orbitDeg per second, against the default satellite direction) while its radius shrinks by orbitInSpeed per second, until it touches the star. Faces its motion direction.
- **Shield (Shielder, `shieldHits` 3):** Straight. At spawn spawnRng picks the shield side: Ccw or Cw. A hitter moving clockwise (orbitDir -1) reaches the enemy from its counter-clockwise side, so a Ccw shield blocks it; with orbitDir +1 a Cw shield blocks. Each blocked hit removes one shield hit; at 0 the shield breaks (its `Shield` child hides with a burst of sparks). The `Shield` child is rotated by code to cover the shield side. CROWBAR breaks it with the first blocked hit.
- **Seek (Devourer, `seekRadius` 2.5):** if a crystal lies within seekRadius of it, it moves toward the nearest one; otherwise toward the star (with vortex drift). Touching a crystal (`dist <= radius + crystalRadius`) eats it: eaten +1 (max `devourMax` 6), hp and maxHp +`devourHp` 1, radius +`devourGrow` 0.04 (`Visual.SetSizeMul(radius / baseRadius)`). On death it drops its own crystals plus the eaten ones.
- **Phase (Phantom, `phaseOn` 1.5, `phaseOff` 1.0):** Straight. It cycles solid for phaseOn s and phased for phaseOff s, starting at a random point of the cycle (simRng). While phased its alpha is `phantomAlpha` 0.25 and hitters pass through it (no hit, no cooldown). It still damages the star while phased. DARK\_MATTER: never phased.

**PULL and STASIS** act on all of these (6.4). EXECUTE never applies to bosses.

### 4.3 Bosses

General rules:

- The era's boss (`era.boss`) appears at `bossTime` 180 s (Marathon 300 s) and fades in over 0.5 s, during which it takes no damage. A banner shows its name (10.6). The HUD boss bar shows its HP (10.5).
- Run mode: the Director stops spawning; enemies already on the field stay. Endless: see 9.2.
- Bosses ignore EXECUTE, PULL and PUSH. STASIS slows them (enemy time). DOME blocks their star damage. Armor and the HP ring work as for enemies.
- Boss HP <= 0 (all parts): Victory (in Endless the run goes on, 9.2).
- `BossComet` becomes `BossSystem`, which runs the boss of the current era. Comet and Twins share one comet-body routine.

**Comet** (era 1, EnemyType.Boss: radius 0.9, hp 80, starDamage 8, crystals 30).

- The star is a focus of its ellipse: periapsis 1.8, apoapsis 5.5, so `a = 3.65`, `e = 0.507`, `b = a * sqrt(1 - e^2)`.
- Timing (Kepler): `M = pi + 2 * pi * t / bossPeriod` (bossPeriod 6.0 s, t = seconds since spawn, in enemy time). Solve `E - e * sin(E) = M` with 4 Newton steps starting from E = M. Position = `Rot(phi) * (a * (cos(E) - e), b * sin(E))`. Slow at apoapsis, fast in the dive.
- Initial phi comes from simRng. Each time M passes a multiple of 2 pi (periapsis), the boss deals 8 star damage once and `phi += 40 deg`. On spawn and after every phi change, the next ellipse is shown by the `BossOrbitLine` for 1.5 s.

**Twins** (era 2, EnemyType.Twin: radius 0.75, hp 70 each, starDamage 6, crystals 15 each).

- Two comet bodies on ellipses with the Comet's periapsis and apoapsis and period `twinPeriod` 6.5 s. Body B starts with M offset by pi and phi offset by 180 deg, so one dives while the other is far.
- Each deals 6 at its own periapsis and turns its own phi by 40 deg. Each has its own orbit line (a second `BossOrbitLine`).
- When one dies, the survivor's period becomes `twinEnragedPeriod` 4.5 s; its mean anomaly stays continuous (no jump).
- The HUD bar shows the sum of both HPs out of 140. Victory when both are dead.

**Colossus** (era 3, EnemyType.Colossus: radius 1.1, hp 220, speed 0.25, armor 2.5, starDamage 25, crystals 40).

- Spawns at spawnRadius at a random angle (simRng) and walks straight at the star, facing it.
- On star contact it deals 25, is pushed back to radius `colossusBounceRadius` 5.5 at its angle + `colossusBounceDeg` 60 (counter-clockwise), and walks in again.
- Phases: each time hp drops below 75%, 50% and 25% of max, it releases `colossusShed` 6 Swarmers evenly around itself (at its radius + 0.3) and its armor drops by `colossusArmorStep` 0.5 (2.5, 2.0, 1.5, 1.0). The armor ring uses the Colossus armor while it lives.

**Pulsar** (era 4, EnemyType.Pulsar: radius 1.0, hp 160, speed 1.0, starDamage 0, crystals 40).

- Spawns at spawnRadius, moves straight in to radius 5.0, then circles counter-clockwise at `pulsarDeg` 22 deg/s on a breathing radius `r(t) = pulsarMid 4.0 + pulsarAmp 1.0 * cos(2 pi t / pulsarSwing 16 s)` (t from arrival, so it starts at 5.0).
- Every `pulsarFireInterval` 3.5 s it fires 3 Bolts at the star: straight at it and +-`pulsarFan` 12 deg.
- Every `pulsarWaveInterval` 12 s (first one 6 s after arrival): its body pulses cream for `pulsarWaveWarn` 1.0 s, then a ring centred on it grows from 0 to `pulsarWaveRadius` 3.2 in `pulsarWaveTime` 1.2 s (`PulsarWave` prefab). A satellite whose distance to the Pulsar is within `satRadius + 0.1` of the ring's current radius is stunned (heat 1, stun stunTime) once per wave. Echoes are not affected.

### 4.4 Crystals

- On enemy death, spawn `crystals` crystals at uniform positions within 0.3 of the death position (simRng). Lifetime `crystalLife * stats.crystalLifeMul`, blinking during the last 2 s.
- An active hitter picks a crystal up when `dist <= HitterRadius + crystalRadius`: dust +1, XP + `stats.xpPerCrystal` (1 x card multipliers; XP is a float in RunState from R3).
- Magnet: crystals within `stats.magnetRadius` of any hitter fly to the nearest one at magnetSpeed.
- Heavy Star era: crystals that are not being pulled by the magnet drift toward the star at `crystalSink` 0.35 u/s and vanish when they reach starRadius (ANCHOR\_FIELD: no drift).
- Devourers eat crystals (4.2).

### 4.5 Kill

Kill = crystals (plus eaten ones), kill shards and sound (as now), then: Splitter spawns its Swarmers; CHAIN queues a blast at the death position (radius `chainRadius` 1.0, damage `chainDamage` 1, Swarmers die), resolved in the next Collisions step, and kills by a blast may queue new blasts; ACCRETION heals the star by `accretionHeal` 1 if the death position is within `accretionRadius` 3.0 of the center; `RunState.kills` +1.

## 5. Director (spawn timeline)

- `Director` reads `SpawnEvent { time, groups[] }` from the current era (`era.opening` and `era.timeline`, 7.2). Each group is `{ type, count, angleMode, spreadDeg = 30, speedMul = 1, radius = 0 }`. radius 0 means the default (spawnRadius; arrowSpawnRadius for Arrows).
- Final count = `Mathf.RoundToInt(count * era.countMul * endlessCountMul * modifierMul)`, min 1. endlessCountMul is 1 outside Endless (9.2). modifierMul: DoubleArrows doubles Arrow groups (9.5). All angles come from spawnRng.
- SwarmOnly modifier: a group of Bastion, Parasite, Shooter, Hive, Splitter, Torch, Orbiter, Shielder, Devourer or Phantom becomes a Swarmer group with count x3 and the same angle mode.
- `angleMode`:
  - **Random:** a new random centre angle.
  - **Opposite:** centre = the previous group's centre in the same event + 180 deg. For an event's first group, the last angle of the previous event + 180 deg.
  - **Spread90:** unit k at centre + k * 90 deg.
  - **Even:** unit k at centre + k * 360 / n.
  - **OppositePair:** two units 180 deg apart.
- A Swarmer group with count > 1 spawns inside an arc of spreadDeg around its centre, with 0 to 1.0 radius jitter beyond the spawn radius.
- **Telegraph:** it starts before `time`, so spawns happen exactly at `time`. Lead time: Arrow 1.5 s, Swarmer group 1.0 s, everything else 0.8 s.
  - Normal marker: a `TelegraphArrow` on the spawn ray at radius `min(telegraphRadius, visible rect inset 0.5)`.
  - Arrow: a `TelegraphWedge` on ring 5.3 at the spawn angle, plus a `TelegraphLine` from it to the star.
  - Ambient spawns, Hive children, Splitter children, Colossus sheds and Pulsar Bolts have no telegraph.
- New enemies fade in over spawnFadeIn and do not collide while fading.
- **Ambient:** 1 Swarmer every `era.ambientInterval` s from `ambientStart` 6 s to `ambientEnd` 175 s, at a random angle.

### 5.1 Timelines

Format: `time Type xCount Mode`; "+" joins groups of one event; groups without a mode are Random; "Swarm" = Swarmer group. Every era ends with its boss at 180 (section 4.3). The tutorial run (era 1, `tutorialDone = false`) replaces era 1's opening with the tutorial table below, starts ambient at 20 s and then runs era 1's main timeline.

**Era 1, Sun.** Opening: `2 Swarm x4 | 8 Swarm x6 | 14 Bastion x1`.
Main: `20 Swarm x8 | 30 Swarm x8 | 36 Bastion x1 | 46 Arrow x2 Spread90 | 56 Swarm x10 | 62 Arrow x2 Spread90 | 70 Parasite x1 | 80 Bastion x1 + Swarm x8 Opposite | 88 Parasite x2 Spread90 | 98 Shooter x1 | 108 Arrow x3 Even | 116 Hive x1 | 126 Swarm x11 + Shooter x1 Opposite | 136 Bastion x2 OppositePair | 146 Hive x1 + Arrow x2 Spread90 | 156 Parasite x2 Spread90 + Swarm x10 | 164 Shooter x2 OppositePair + Bastion x1 | 172 Arrow x4 Even + Swarm x11`

**Era 2, Red Dwarf.** Opening: `2 Swarm x4 | 8 Splitter x2 | 14 Torch x1`.
Main: `20 Swarm x8 | 30 Splitter x3 | 36 Bastion x1 | 46 Arrow x2 Spread90 | 50 Torch x1 | 56 Swarm x10 | 62 Arrow x2 Spread90 | 70 Parasite x1 + Splitter x2 Opposite | 80 Bastion x1 + Swarm x8 Opposite | 88 Parasite x2 Spread90 | 96 Torch x2 OppositePair + Shooter x1 | 108 Arrow x3 Even | 116 Hive x1 + Splitter x2 Opposite | 126 Swarm x11 + Shooter x1 Opposite | 136 Bastion x2 OppositePair | 140 Torch x2 Spread90 | 146 Hive x1 + Arrow x2 Spread90 | 156 Parasite x2 Spread90 + Splitter x3 | 164 Shooter x2 OppositePair + Bastion x1 | 172 Arrow x4 Even + Splitter x3 + Swarm x8`

**Era 3, Heavy Star.** Opening: `2 Swarm x4 | 8 Orbiter x1 | 14 Bastion x1`.
Main: `20 Swarm x8 | 28 Shielder x1 | 36 Bastion x1 + Orbiter x1 Opposite | 46 Arrow x2 Spread90 | 52 Orbiter x2 OppositePair | 56 Swarm x10 | 62 Arrow x2 Spread90 + Shielder x1 Opposite | 70 Parasite x1 + Splitter x2 | 80 Bastion x1 + Swarm x8 Opposite | 88 Orbiter x3 Even | 98 Shooter x1 + Shielder x1 Opposite | 108 Arrow x3 Even | 116 Hive x1 + Torch x1 Opposite | 126 Swarm x11 + Shielder x2 OppositePair | 136 Bastion x2 OppositePair + Orbiter x2 OppositePair | 146 Hive x1 + Arrow x2 Spread90 | 152 Orbiter x4 Even | 156 Parasite x2 Spread90 + Swarm x10 | 164 Shooter x2 OppositePair + Shielder x2 Spread90 | 172 Arrow x4 Even + Orbiter x3 Even + Swarm x8`

**Era 4, Black Hole.** Opening: `2 Swarm x4 | 8 Phantom x2 OppositePair | 14 Devourer x1`.
Main: `20 Swarm x8 | 28 Phantom x3 Even | 36 Bastion x1 + Devourer x1 Opposite | 46 Arrow x2 Spread90 + Phantom x2 OppositePair | 56 Swarm x10 + Orbiter x2 OppositePair | 62 Arrow x2 Spread90 + Shielder x1 | 70 Parasite x1 + Devourer x1 Opposite | 80 Bastion x1 + Swarm x8 Opposite | 88 Phantom x4 Even | 98 Shooter x1 + Torch x1 Opposite | 108 Arrow x3 Even + Splitter x2 | 116 Hive x1 + Devourer x1 Opposite | 126 Swarm x11 + Phantom x3 Even | 136 Bastion x2 OppositePair + Shielder x2 OppositePair | 146 Hive x1 + Arrow x2 Spread90 + Devourer x1 | 156 Parasite x2 Spread90 + Phantom x4 Even | 164 Shooter x2 OppositePair + Bastion x1 + Orbiter x2 OppositePair | 172 Arrow x4 Even + Phantom x6 Even + Swarm x8`

**Tutorial opening** (era 1 only; hints in 10.7):

| t, s | event | lesson |
| --- | --- | --- |
| 0.5 | Swarm x3, spreadDeg 40, radius 5.4, speedMul 0.15 | only a held satellite reaches them |
| 8 | Swarm x6, speedMul 0.6 | release to defend near the star |
| 16 | Bastion x1 | armor ring (it pulses, 3.6) |

## 6. Cards and abilities

### 6.1 Card rules

- XP: level L (starts at 0) needs `(8 + 3 * L) * stats.xpNeedMul` XP. When the bar fills, the state becomes `CardPick` (simulation paused) and shows `cardChoices` 3 (+ stats.extraChoices) distinct cards, drawn by weight without replacement from the eligible pool (cardRng).
- Weights: Common `weightCommon` 3, Rare `weightRare` 1, era card `weightEra` 4.
- A card is eligible unless:
  - it is at max stacks (ADD\_SAT has no stack limit: it is eligible only while satellites < 3 + stats.addSatBonus);
  - its `era` is not 0 and not the current era;
  - it is SAW\_THREAD and the player has fewer than 2 satellites;
  - it is ABILITY\_LONG and no ability in a slot has duration > 0;
  - it is ABILITY\_HASTE or ABILITY\_LONG under NoAbilities, or ADD\_SAT under SingleSat (9.5).
- While satellites are below the cap, the first card screen of the run always includes ADD\_SAT (not under SingleSat).
- The 3rd level-up of a run opens the slot-B ability pick (6.4) instead of cards (cards under NoAbilities).
- The chosen card gets stack +1 and `Stats` is recomputed: base x era x cards x blueprints x modifiers x active abilities. Stats whose name ends in Mul multiply; all others add; a flag stat is on when >= 1.
- Surplus XP carries over. Several level-ups at once open their screens one after another.
- Rerolls: the existing "New cards (ad)" button (once per run) stays. Above it, while `stats.freeRerolls` (CHOICE blueprint) is not used up this run, a free button «Заменить (N)» redraws the cards the same way.
- `CardDef { CardId id; CardColor color; CardRarity rarity; int era; int maxStacks; StatMod plus; StatMod minus; }`. `CardColor { Orbit, Strike, Star }` (cream, red, black frames). `CardRarity { Common, Rare }`. `era` 0 = every era.
- The plus and minus lines shown on the card are the Russian texts below (`Strings.CardPlus[id]`, `Strings.CardMinus[id]`).

`StatId` in R3: PowerMul, RadialSpeedMul, RadialOutMul, HeatMul, SpinMul, ResonanceAngleMul, ResonanceBonus, SatRadiusMul, CrystalLifeMul, ArrowSpeedMul, AbilityCooldownMul, AbilityDurationMul, XpPerCrystalMul, XpNeedMul, StarDamageMul, DustMul, MagnetRadius, PowerFarBonus, StarMaxHp, Regen, Satellites, AddSatBonus, Beacon, CritChance, ArmorPierce, Execute, SawThread, Burn, Chain, Heatproof, SolarSail, Firebird, Slingshot, AnchorField, Crowbar, Horizon, DarkMatter, Accretion, SecondSun, FreeRerolls, ExtraChoices.

### 6.2 Cards for every era (21)

| id | name | colour | rarity | max | plus (stat value) | minus (stat value) | plus text | minus text |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| HEAVY | ТЯЖЁЛЫЙ СПУТНИК | Strike | Common | 2 | PowerMul 1.35 | RadialSpeedMul 0.75 | Удар ×1.35 | Выход и падение медленнее ×0.75 |
| FAR\_STRIKE | ДАЛЬНИЙ УДАР | Strike | Common | 2 | PowerFarBonus 0.5 | HeatMul 1.15 | Удар на дальней орбите +0.5 | Нагрев ×1.15 |
| SHARP | ОСТРЫЕ ГРАНИ | Strike | Common | 2 | CritChance 0.12 | PowerMul 0.92 | Шанс крита +12% (крит бьёт ×2) | Удар ×0.92 |
| PIERCE | БРОНЕБОЙ | Strike | Common | 1 | ArmorPierce 1 | SpinMul 0.9 | Броня врагов −1 | Вращение ×0.9 |
| EXECUTE | РАСКОЛ | Strike | Rare | 1 | Execute 1 | CrystalLifeMul 0.7 | Враг с HP ≤ 25% гибнет от любого удара | Кристаллы живут ×0.7 |
| SAW\_THREAD | НИТЬ-ПИЛА | Strike | Rare | 1 | SawThread 1 | PowerMul 0.9 | Нить резонанса режет врагов | Удар ×0.9 |
| BURN | РАСКАЛЁННЫЙ | Strike | Common | 1 | Burn 1 | HeatMul 1.15 | Горячий спутник поджигает: 1 урон/с, 3 с | Нагрев ×1.15 |
| CHAIN | ЦЕПНАЯ РЕАКЦИЯ | Strike | Rare | 1 | Chain 1 | XpPerCrystalMul 0.9 | Убитый враг взрывается: 1 урон рядом | Опыт за кристалл ×0.9 |
| ADD\_SAT | НОВЫЙ СПУТНИК | Orbit | Common | — | Satellites 1 | HeatMul 1.2 | +1 спутник | Нагрев ×1.2 |
| SWIFT | РЕАКТИВНЫЙ ДВИГАТЕЛЬ | Orbit | Common | 2 | RadialSpeedMul 1.5 | HeatMul 1.2 | Выход и падение ×1.5 | Нагрев ×1.2 |
| SPIN | РАЗГОН | Orbit | Common | 2 | SpinMul 1.25 | HeatMul 1.15 | Вращение ×1.25 | Нагрев ×1.15 |
| BIG\_SAT | КРУПНЫЙ СПУТНИК | Orbit | Common | 2 | SatRadiusMul 1.35 | SpinMul 0.9 | Спутник крупнее ×1.35 | Вращение ×0.9 |
| MAGNET | МАГНИТ | Orbit | Common | 2 | MagnetRadius 1.3 | ArrowSpeedMul 1.15 | Притяжение кристаллов +1.3 | Стрелы быстрее ×1.15 |
| GREED | ЖАДНОСТЬ | Orbit | Common | 2 | XpPerCrystalMul 1.3 | StarMaxHp -15 | Опыт за кристалл ×1.3 | Макс. HP звезды −15 |
| COOLANT | ОХЛАДИТЕЛЬ | Orbit | Common | 2 | HeatMul 0.6 | PowerMul 0.9 | Нагрев ×0.6 | Удар ×0.9 |
| STAR\_PLATING | БРОНЯ ЗВЕЗДЫ | Star | Common | 2 | StarMaxHp 25 | SpinMul 0.9 | Макс. HP +25 и лечение 25 | Вращение ×0.9 |
| PHOTOSPHERE | ФОТОСФЕРА | Star | Common | 2 | Regen 0.4 | XpPerCrystalMul 0.85 | Звезда лечится 0.4 HP/с | Опыт за кристалл ×0.85 |
| CORONA | КОРОНА | Star | Common | 1 | StarDamageMul 0.7 | CrystalLifeMul 0.8 | Урон по звезде ×0.7 | Кристаллы живут ×0.8 |
| BEACON | МАЯК | Star | Common | 2 | Beacon 1 | CrystalLifeMul 0.6 | Волна от звезды каждые 8 с | Кристаллы живут ×0.6 |
| ABILITY\_HASTE | ПЕРЕЗАРЯДКА | Star | Common | 2 | AbilityCooldownMul 0.75 | PowerMul 0.9 | Перезарядка способностей ×0.75 | Удар ×0.9 |
| ABILITY\_LONG | ДОЛГОЕ ДЕЙСТВИЕ | Star | Common | 1 | AbilityDurationMul 1.4 | AbilityCooldownMul 1.15 | Способности действуют ×1.4 дольше | Перезарядка ×1.15 |

Notes:

- STAR\_PLATING also heals by the same amount (as now). GREED's minus lowers the current HP too if it is above the new max.
- BEACON: every `beaconInterval` 8 s x 0.7^(stacks - 1), a wave from the star to `beaconRadius` 2.5: Swarmers die, every other enemy and the boss take `beaconDamage` 1 (`BeaconRing` effect, as now).
- Removed from R2.1: RESONATOR (replaced by SAW\_THREAD), APOGEE (renamed FAR\_STRIKE with a clearer text).

### 6.3 Era cards (9)

They appear only in their era (Run, Endless, Challenges and Daily of that era).

| id | name | era | colour | rarity | max | plus (stat value) | minus (stat value) | plus text | minus text |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| HEATPROOF | ЖАРОСТОЙКОСТЬ | 2 | Orbit | Common | 1 | Heatproof 1 | SpinMul 0.95 | Вспышки и Факелы не греют | Вращение ×0.95 |
| SOLAR\_SAIL | СОЛНЕЧНЫЙ ПАРУС | 2 | Strike | Common | 1 | SolarSail 1 | HeatMul 1.1 | Во время вспышки и 3 с после: удар ×2 | Нагрев ×1.1 |
| FIREBIRD | ЖАР-ПТИЦА | 2 | Strike | Rare | 1 | Firebird 1 | HeatMul 1.2 | Спутник с нагревом от 70% бьёт ×1.5 | Нагрев ×1.2 |
| SLINGSHOT | ПРАЩА | 3 | Strike | Common | 1 | Slingshot 1 | RadialOutMul 0.9 | 1 с после отпускания: удар ×1.5 | Выход ×0.9 |
| ANCHOR\_FIELD | ГРАВИЗАХВАТ | 3 | Orbit | Common | 1 | AnchorField 1 | PowerMul 0.95 | Кристаллы не тонут в звезде | Удар ×0.95 |
| CROWBAR | ЛОМ | 3 | Strike | Rare | 1 | Crowbar 1 | SpinMul 0.9 | Щит Щитника ломается с 1 удара, Кружащие получают ×2 | Вращение ×0.9 |
| EVENT\_HORIZON | ГОРИЗОНТ СОБЫТИЙ | 4 | Star | Common | 1 | Horizon 1 | StarMaxHp -10 | Враги ближе 1.8 к центру теряют 2 HP/с | Макс. HP звезды −10 |
| DARK\_MATTER | ТЁМНАЯ МАТЕРИЯ | 4 | Strike | Common | 1 | DarkMatter 1 | PowerMul 0.95 | Призраки не уходят в фазу | Удар ×0.95 |
| ACCRETION | АККРЕЦИЯ | 4 | Star | Rare | 1 | Accretion 1 | CrystalLifeMul 0.8 | Убийство ближе 3.0 к центру лечит 1 HP | Кристаллы живут ×0.8 |

- SOLAR\_SAIL window: from a flare's burst start to `solarSailAfter` 3 s after it ends.
- SLINGSHOT window: `slingshotTime` 1.0 s after the hold ends (the last holder releases). RadialOutMul multiplies the radial speed only while baseR grows.

`CardId` order: the 21 cards of 6.2 in table order, then the 9 cards of 6.3 in table order. `ArtSet.cardIcons` follows it.

### 6.4 Active abilities

`AbilityId` in R3: Reverse, Grow, Pulse, Stasis, Echo, Pull, Dome (ANCHOR, CHILL and SURGE are removed with their code, `AnchorRing` and `SurgeTrail`). `AbilityDef { id, duration, cooldown, price }`; price 0 = owned from the start, otherwise bought in the Workshop (8.4).

| id | name | effect | duration | cooldown | price |
| --- | --- | --- | --- | --- | --- |
| Reverse | РЕВЕРС | orbitDir flips at once. For the duration satellites and echoes hit x`reverseHitMul` 1.5. Every enemy's hit cooldowns reset, so a satellite can hit what it just passed | 2 s | 12 s | 0 |
| Grow | РОСТ | satRadiusMul x`growRadiusMul` 2 (the pickup distance grows with it), resonanceAngleMul x1.3, smooth 0.2 s in and out | 6 s | 35 s | 0 |
| Pulse | ИМПУЛЬС | a wave from the star to radius 3.0 in 0.35 s: Swarmers and Arrows die (crystals drop); other enemies take 2 damage and are pushed 1.0 outward; bosses take 2 and are not pushed | instant | 40 s | 0 |
| Stasis | СТАЗИС | enemy time x`stasisSlow` 0.25 (3.1); enemies are tinted `stasisTint` (silver at 50%) | 4 s | 40 s | 300 |
| Echo | ЭХО | every satellite gets an echo (3.2) | 6 s | 35 s | 500 |
| Pull | ПРИТЯЖЕНИЕ | every non-boss enemy stops its own movement and moves radially toward the outermost satellite's orbit radius at `pullSpeed` 3 u/s, stopping there. Attached Parasites detach. When it ends, each enemy resumes its behavior from where it is | 3 s | 35 s | 500 |
| Dome | КУПОЛ | the star takes no damage; enemies that touch it are destroyed (shards, no crystals); attached Parasites fall off and die; boss star damage is blocked | 5 s | 50 s | 700 |

One-line texts for the pick screen (`Strings.AbilityText`): РЕВЕРС «Орбита разворачивается, 2 с удар ×1.5»; РОСТ «Спутники вдвое больше на 6 с»; ИМПУЛЬС «Волна от звезды: мелочь гибнет, остальных отбрасывает»; СТАЗИС «Враги в 4 раза медленнее на 4 с»; ЭХО «У каждого спутника тень-двойник на 6 с»; ПРИТЯЖЕНИЕ «Враги стягиваются на вашу орбиту на 3 с»; КУПОЛ «Звезда неуязвима 5 с, враги разбиваются о купол».

- **Slots.** There are two.
  - Slot A: starting a run from the menu opens `AbilityPick` with all seven abilities (name, pictogram, one-line effect, cooldown). Abilities not owned are shown greyed with a lock and their price and cannot be picked. Retry skips the screen and reuses the last slot-A choice. In the tutorial run, slot A is PULSE, assigned with no pick screen. NoAbilities: no slots, no screen.
  - Slot B: the 3rd level-up of a run (tutorial included) offers up to 3 random owned abilities not in slot A (cardRng). If none is left, that level-up opens cards instead.
- **Timing.** The cooldown starts at activation. Pressing during an active effect or a cooldown does nothing. Timers tick inside the simulation step, so they stop in every state except Playing and during hit-stop. Abilities start ready. Revive does not change cooldowns. Duration x stats.abilityDurationMul, cooldown x stats.abilityCooldownMul.
- **Input and HUD.**
  - Two round buttons, 140 px with a 24 px margin, at the bottom right: slot B on the left, slot A on the right. Keys: Q = slot A, E = slot B.
  - Each button shows a radial cooldown fill, the remaining whole seconds while cooling (`TMP_Text`, hidden when ready) and a pulsing gold ring when ready (DOTween).
  - Buttons are UI above `HoldZone`, so a tap on a button never counts as hold.
- **Effects** (scene objects under World, hidden by default; all [R] in 11.3):
  - REVERSE: `ReverseFx` (cream ring with arrowheads) appears at the outermost orbit radius, turns once in the new direction over 0.4 s and fades.
  - GROW: smooth size change.
  - PULSE: `PulseRing` expands, plus shake (amp 0.15, 0.2 s).
  - STASIS: `StasisFx` (silver ring at radius 6.2, pulsing) while active; enemies tinted.
  - ECHO: echo ghosts (pooled `Echo` prefab).
  - PULL: `PullFx` (cream dashed ring) at the target radius while active, pulsing inward.
  - DOME: `DomeFx` (cream ring around the star, radius 1.25) while active, alpha pulsing.
- **Implementation.** `AbilitySystem` holds the slots, cooldowns and active effects and ticks right after Satellites. Active effects apply as temporary multipliers or flags in `Stats`, separate from cards, and expire by timer. `GameLoop.EnemyTimeScale` reads STASIS. Owned abilities come from the save (8.1) or `debugUnlockAll`.

## 7. Eras

### 7.1 What an era gives the player

An era is a star system with its own identity. Each one adds: one rule that changes how orbits play (7.3), two new enemies (4.2), its own boss (4.3), three cards that exist only there (6.3), more dust per crystal, a separate best time, and a first-victory reward. A first victory in an era opens the next one. The era screen (10.2) shows all of this before the run.

### 7.2 EraDef (list `eras` in GameConfig, index 0..3)

`EraDef { kExp, spinMul, hpMul, countMul, ambientInterval, dustMul, riseMul, fallMul, EraRule rule, EnemyType boss, SpawnEvent[] opening, SpawnEvent[] timeline, int victoryDust, int firstWinDust, Color background, Color starTint, Color raysTint }`. Names, rule lines and boss names live in `Strings`. Optional art per era lives in ArtSet: `eraIcons[]` (menu), `eraBackdrops[]` (a big dim picture behind the rings; empty = none), `eraStarSprites[]` (replaces the star Body sprite in that era; empty = keep the skin).

| # | name (RU) | kExp | spinMul | hpMul | countMul | ambient s | dustMul | rise / fall | rule | boss | victory dust | first win |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | СОЛНЦЕ | 0.9 | 1.0 | 1.0 | 1.0 | 3.5 | 1.0 | 1 / 1 | None | Comet (Boss) | 100 | 200 dust, opens era 2 and Endless |
| 2 | КРАСНЫЙ КАРЛИК | 0.9 | 1.15 | 1.4 | 1.2 | 3.3 | 1.25 | 1 / 1 | Flares | Twin | 150 | 400 dust, opens era 3, skin «Луна» |
| 3 | ТЯЖЁЛАЯ ЗВЕЗДА | 1.2 | 1.0 | 1.8 | 1.35 | 3.1 | 1.5 | 0.8 / 1.3 | Heavy | Colossus | 200 | 600 dust, opens era 4, star skin «Мотор» |
| 4 | ЧЁРНАЯ ДЫРА | 1.0 | 1.1 | 2.3 | 1.5 | 3.0 | 1.8 | 1 / 1 | Vortex | Pulsar | 250 | 800 dust, skin «Глаз» |

Colours: background #0E0E0E, #160B0A, #0B0E14, #050505; starTint (multiplies the star Body colour) white, #FF9A80, #C9D8FF, #262626; raysTint (multiplies the Rays colour) white, #FF9A80, #C9D8FF, white (the black hole keeps a cream rim of rays). The camera background takes the era colour at run start.

Rule lines (`Strings.EraRule`): 1 «Спокойная звезда. Здесь учатся держать орбиту.»; 2 «Вспышки: красный сектор греет спутники и жжёт врагов.»; 3 «Тяжесть: спутники взлетают медленнее, падают быстрее, кристаллы тонут в звезде.»; 4 «Воронка: враги летят к центру по спирали.»

Unlocks: era n+1 opens with the first victory in era n in Run mode (Daily and Challenges do not open eras). Endless opens with the first victory in era 1. The first victory in an era also pays `firstWinDust` and gives its skin (8.5).

### 7.3 Era rules

`EraRule { None, Flares, Heavy, Vortex }`, run by `EraRuleSystem` (ticks after Abilities).

**Flares (Red Dwarf).**

- The first flare starts at `flareFirst` 25 s, then one every `flareInterval` 25 s, also during the boss.
- Each flare picks a centre angle (simRng) at least `flareMinGap` 60 deg away from the previous one and covers `flareArc` 90 deg from the center to the field edge.
- Warning: `flareWarn` 1.5 s, the `FlareSector` scene object shows the sector at low alpha, pulsing.
- Burst: `flareTime` 1.0 s at full alpha (its rays decor animate). At the start of the burst every non-boss enemy inside the sector takes `flareDamage` 2 (Swarmers die). During the burst every satellite inside the sector gains `flareHeatRate` 0.8 x stats.heatMul heat per second (none with HEATPROOF). Small shake at the burst (amp 0.05, 0.1 s).
- Inside the sector = shortest angular distance to the centre angle <= flareArc / 2.

**Heavy (Heavy Star).** riseMul 0.8 and fallMul 1.3 (3.2), crystals sink into the star (4.4).

**Vortex (Black Hole).** Straight and Seek movement also turns around the center by `vortexDeg` 14 deg/s counter-clockwise (enemy time), so enemies arrive on a spiral. Telegraph markers stay on the spawn ray. The star uses the era tint (and `eraStarSprites[3]` when set) and looks like a black hole with a cream rim.

## 8. Meta

### 8.1 Save

- JSON in PlayerPrefs key `perigee_save` through `ISaveService`. Call `PlayerPrefs.Save()` after every write. SaveService may wrap the JSON parse in one try/catch; on failure it starts a fresh save.
- `SaveData` in R3 (new fields get these defaults):

```
int version = 2
int dust
int[] blueprints          // levels by index in config.blueprints (16)
int abilitiesOwned        // bit per AbilityId; fresh save: Reverse, Grow, Pulse
int skinsOwned            // bit per SkinId; fresh save: Romb, Sun
int satelliteSkin, starSkin
int erasUnlocked = 1      // 1..4
int erasWon               // bit per era index
float[] bestTimes         // by era, Run mode
float[] bestEndless       // by era
int bestDaily, dailyBonusDate
int challengesDone        // bit per challenge
int seenEnemies           // bit per EnemyType, for first-meeting banners (10.6)
bool tutorialDone
int runs, defeats, totalKills
int lastAbilityA, lastEra, lastMode
SettingsData settings     // 10.4
float bestTime            // R2.1 field, read only by the migration
```

- **Migration** (on load, when `version < 2`): add to dust the full price of every old blueprint level bought (old order Hull, Capacitor, CoolantBp, Magnetism; prices in GameConfig `legacyBlueprintCosts`: 50, 100, 200, 400, 800 for the first three and 40, 80, 160 for Magnetism), then reset `blueprints` to the new length; `bestTimes[0] = bestTime`; `erasWon` = bits of every era below erasUnlocked; `abilitiesOwned` = Reverse, Grow, Pulse; `skinsOwned` = Romb, Sun; `lastAbilityA`: old Grow and Pulse keep their meaning, anything else becomes Reverse; `version = 2`.
- `tutorialDone` becomes true when the first run ends.

### 8.2 Dust

- Run: `round(crystals picked x era.dustMul x stats.dustMul) + (victory ? era.victoryDust : 0) + (first victory in this era ? era.firstWinDust : 0)`.
- Daily: as Run; the first Daily run of each UTC date doubles it (`dailyDustMul` 2).
- Endless: crystals as in Run, plus `endlessBossDust` 60 x era.dustMul for every boss killed. No victory dust.
- Challenge: as Run, plus the challenge reward on its first completion (9.4).
- The existing "x2 dust (ad)" button stays.
- The result screen shows the parts: crystals x era, victory, first victory, reward, total.

Rough economy (balance check with the R2 simulator): a run gives about 200 crystals in era 1 and 250 in era 4, so with eras' dustMul and victory dust about 230, 360, 470 and 600 dust per run in eras 1 to 4. Everything bought in 8.3 to 8.5 costs about 17,400; with first wins and challenges a player owns it all after about 40 to 50 runs. Full blueprints are worth roughly one skill level: in the simulator a weak bot's win rate in era 1 goes from about 20% to almost 100%, and later eras stay hard.

### 8.3 Blueprints (a tree in four branches)

`BlueprintDef { BlueprintId id; BlueprintBranch branch; int tier; BlueprintLevel[] levels }`, `BlueprintLevel { int cost; StatMod mod }`. The effect of a node = the mods of all bought levels. `BlueprintBranch { Star, Strike, Orbit, Crystal }` (black, red, cream, gold).

- Tier 1 nodes are open from the start. A node of tier t opens when the node of tier t - 1 in the same branch has at least one level.
- Buying: pay the next level's cost, level +1, save.
- Blueprints apply in every mode (Run, Endless, Daily, Challenges).

| branch | tier | id | name | per level | levels | costs |
| --- | --- | --- | --- | --- | --- | --- |
| Star | 1 | Hull | КОРПУС | StarMaxHp +8 | 5 | 40, 70, 110, 160, 220 |
| Star | 2 | Core | ЯДРО | Regen +0.07 | 3 | 120, 200, 300 |
| Star | 3 | Reflector | ОТРАЖАТЕЛЬ | StarDamageMul 0.95 | 3 | 200, 320, 480 |
| Star | 4 | SecondSun | ВТОРОЕ СОЛНЦЕ | SecondSun 1 (4.1) | 1 | 1200 |
| Strike | 1 | Forge | ЗАКАЛКА | PowerMul 1.03 | 5 | 40, 70, 100, 140, 190 |
| Strike | 2 | TuningFork | КАМЕРТОН | ResonanceBonus +0.1 | 3 | 120, 200, 300 |
| Strike | 3 | Edge | ОСТРИЁ | CritChance +0.03 | 3 | 200, 320, 480 |
| Strike | 4 | Twin | ПАРА | Satellites +1 (start with 2) | 1 | 1200 |
| Orbit | 1 | Thrust | ТЯГА | RadialSpeedMul 1.05 | 4 | 30, 60, 90, 130 |
| Orbit | 2 | Radiator | РАДИАТОР | HeatMul 0.94 | 4 | 80, 130, 190, 270 |
| Orbit | 3 | Capacitor | КОНДЕНСАТОР | AbilityCooldownMul 0.95 | 4 | 160, 240, 340, 450 |
| Orbit | 4 | Quadriga | КВАДРИГА | AddSatBonus +1 (a 4th satellite) | 1 | 1400 |
| Crystal | 1 | Magnetism | МАГНЕТИЗМ | MagnetRadius +0.3 | 4 | 30, 50, 80, 120 |
| Crystal | 2 | Insight | ОЗАРЕНИЕ | XpNeedMul 0.95 | 3 | 120, 200, 300 |
| Crystal | 3 | Alchemy | АЛХИМИЯ | DustMul 1.08 | 3 | 240, 400, 600 |
| Crystal | 4 | Choice | ВЫБОР | level 1: FreeRerolls +1; level 2: ExtraChoices +1 | 2 | 700, 1100 |

Texts (`Strings.BlueprintText`, `{0}` = total of bought levels): КОРПУС «Звезда: +8 к макс. HP за уровень. Сейчас +{0}»; ЯДРО «Звезда лечится +0.07 HP/с за уровень. Сейчас +{0}»; ОТРАЖАТЕЛЬ «Урон по звезде −5% за уровень. Сейчас −{0}%»; ВТОРОЕ СОЛНЦЕ «Раз за забег звезда вспыхивает вместо гибели: 25% HP и волна»; ЗАКАЛКА «Удар +3% за уровень. Сейчас +{0}%»; КАМЕРТОН «Резонанс +0.1 за уровень. Сейчас ×{0}»; ОСТРИЁ «Шанс крита +3% за уровень. Сейчас {0}%»; ПАРА «Забег начинается с двумя спутниками»; ТЯГА «Выход и падение +5% за уровень. Сейчас +{0}%»; РАДИАТОР «Нагрев −6% за уровень. Сейчас −{0}%»; КОНДЕНСАТОР «Перезарядка способностей −5% за уровень. Сейчас −{0}%»; КВАДРИГА «Можно взять четвёртый спутник»; МАГНЕТИЗМ «Притяжение кристаллов +0.3 за уровень. Сейчас +{0}»; ОЗАРЕНИЕ «Опыта на уровень −5% за уровень. Сейчас −{0}%»; АЛХИМИЯ «Пыль за забег +8% за уровень. Сейчас +{0}%»; ВЫБОР «Ур. 1: бесплатная замена карточек раз в забег. Ур. 2: на выбор 4 карточки».

Satellite cap: satellites never exceed `3 + stats.addSatBonus` (so 4 only with QUADRIGA). ADD\_SAT has no stack limit of its own: it is eligible while the count is below the cap.

### 8.4 Ability shop

Abilities with price > 0 (6.4) are bought once in the Workshop. Owned abilities appear in both slot picks.

### 8.5 Skins

`SkinId` (one enum): satellite skins Romb, Sputnik, Gear, Moon, RedStar, Eye; star skins Sun, Oko, Sunflower, Motor. `SkinDef { SkinId id; SkinKind kind; int price; int eraWin; int challenge }`: at most one of price, eraWin (1..4) or challenge (1..8) is set; all three 0 = owned from the start.

| id | name | kind | how to get |
| --- | --- | --- | --- |
| Romb | Ромб | Satellite | owned |
| Sputnik | Спутник-1 | Satellite | 300 dust |
| Gear | Шестерня | Satellite | 400 dust |
| Moon | Луна | Satellite | first victory in era 2 |
| RedStar | Звёздочка | Satellite | challenge «Жара» |
| Eye | Глаз | Satellite | first victory in era 4 |
| Sun | Солнце | Star | owned |
| Oko | Око | Star | 400 dust |
| Sunflower | Подсолнух | Star | challenge «Стеклянная звезда» |
| Motor | Мотор | Star | first victory in era 3 |

- A satellite skin is a full satellite prefab (`ArtSet.satelliteSkins[]`, by SkinId order of satellite skins): sprite, trail and HeatArc. SatelliteSystem instantiates the selected one at run start. All of them keep the required children of 11.3.
- A star skin is a pair of sprites (`ArtSet.starSkins[]`: `body`, `rays`) assigned to the Star's Body and Rays at run start. `eraStarSprites` (7.2) wins over the skin body when set.
- Skins change nothing in gameplay.

## 9. Modes

`RunMode { Run, Endless, Daily, Challenge }` and `RunModifier` flags live in RunState. `GameLoop.StartFromMenu(era, mode, challenge)` replaces the old `StartFromMenu(era, daily)`.

### 9.1 Run

The chosen era (unlocked only), its timeline and boss. Victory, Defeat, revive and Result work as now. A first victory opens the next era (7.2).

### 9.2 Endless

- Opens with the first victory in era 1. Any unlocked era.
- The opening plays once. Then loop L = 0, 1, 2… replays the era's main timeline: an event at time τ (20 <= τ < 180) fires at `τ + 160 * L`. The era's boss spawns at `180 + 160 * L` if no boss is alive. Spawning never stops, also during a boss, and ambient never stops.
- Loop L scales: enemy hp x (1 + `endlessHpStep` 0.35 x L), counts x (1 + `endlessCountStep` 0.2 x L), boss hp x (1 + `endlessBossHpStep` 0.5 x L), ambient interval x max(0.5, 1 - 0.1 x L).
- A killed boss gives a banner «+N пыли» (8.2), its bar hides and the run goes on. The run ends only in Defeat (ad revive allowed once, as now).
- Best survived time per era in `bestEndless`. Score = whole seconds survived.

### 9.3 Daily

- Opens after the tutorial. Always era 1. Seed = UTC date as yyyyMMdd.
- One modifier per day: `dailyMods[daysSinceEpoch % 6]` = DoubleArrows, FastEnemies, DoubleHeat, SwarmOnly, GlassStar, HighOrbit.
- The first finished Daily of the date doubles its dust. Best score in `bestDaily`.

### 9.4 Challenges

Fixed runs with modifiers. Each opens when the tutorial is done and its era is unlocked. Completed = victory; the reward is paid once; replays are allowed.

| # | name | era | modifiers | reward |
| --- | --- | --- | --- | --- |
| 1 | ОДИНОЧКА | 1 | SingleSat | 300 dust |
| 2 | СТЕКЛЯННАЯ ЗВЕЗДА | 1 | GlassStar | star skin «Подсолнух» |
| 3 | ВЫСОКАЯ ОРБИТА | 1 | HighOrbit | 300 dust |
| 4 | РОЙ | 1 | SwarmOnly | 300 dust |
| 5 | БЕЗ СПОСОБНОСТЕЙ | 2 | NoAbilities | 400 dust |
| 6 | ЖАРА | 2 | DoubleHeat | satellite skin «Звёздочка» |
| 7 | ВСТРЕЧНЫЙ ВЕТЕР | 3 | AutoReverse | 500 dust |
| 8 | МАРАФОН | 3 | Marathon | 600 dust |

`ChallengeDef { int era; RunModifier mods; int rewardDust; SkinId rewardSkin; bool hasSkin }` in GameConfig.

### 9.5 Modifiers

| flag | name (RU) | text (RU) | effect |
| --- | --- | --- | --- |
| SingleSat | Одиночка | Всегда один спутник | 3.2; ADD\_SAT and TWIN do nothing |
| GlassStar | Стеклянная звезда | HP звезды ×0.4, лечения нет | 4.1 |
| HighOrbit | Высокая орбита | Спутники не опускаются ниже 3.0 | rest radius `highOrbitMin` 3.0 (3.2) |
| NoAbilities | Без способностей | Способностей нет | no slots, no pick screens, ability cards not offered |
| SwarmOnly | Рой | Вместо крупных врагов — тройные стаи | 5 |
| DoubleHeat | Жара | Нагрев ×2 | heatMul x2 |
| AutoReverse | Встречный ветер | Орбита разворачивается каждые 8 с | orbitDir flips every `autoReverseInterval` 8 s; REVERSE is not offered |
| Marathon | Марафон | Босс на 5:00 | bossTime 300; from 180 to 300 the main-timeline events from 60 to 180 replay shifted by +120 s with enemy hp x1.3 |
| FastEnemies | Спешка | Враги быстрее ×1.2 | enemy speed x`fastEnemiesMul` 1.2 |
| DoubleArrows | Ливень стрел | Стрел вдвое больше | Arrow group counts x2 |

### 9.6 Score

Run, Daily and Challenge: dust picked + 2 x run seconds (boss fight included) + (victory ? 500 : 0). Endless: whole seconds survived. The best Run time per era is stored in `bestTimes`.

## 10. Flow and UI

### 10.1 States and input

| state | what it is |
| --- | --- |
| Menu | the menu with its panels (10.2) |
| AbilityPick | slot-A pick before a run started from the menu |
| Playing | the simulation runs |
| Paused | pause screen |
| CardPick | card or slot-B ability pick |
| Defeat | revive offer overlay, simulation stopped; declining it or using the revive moves on |
| Victory | 1.5 s celebration |
| Result | summary screen |

Retry from Result restarts immediately with the same era, mode, modifiers and slot A. Esc or P toggles pause on desktop.

**Input:** `HoldZone` is a full-screen transparent UI Image under all other UI (raycast target) implementing IPointerDownHandler and IPointerUpHandler.

- Hold = a set of pointerIds that went down on HoldZone. An id is removed on its own pointer up; Unity sends OnPointerUp to the pressed object even after the pointer left it. Pointer exit is ignored.
- The Space key counts as one more holder.
- The set clears on leaving Playing and on focus loss. Input is active only in Playing.
- The player can hold with one finger and tap an ability button with another.

**Canvas:** Screen Space Overlay, Scale With Screen Size, reference 1920x1080, match 0.5. HUD is anchored inside `Screen.safeArea`. Every HUD element stays outside the field circle of radius 6.2. Hud switches layout when `Screen.width > Screen.height` changes. Menu panels use the whole safe area and work in both orientations (portrait stacks vertically, landscape side by side).

**UI art.** The UI is built in the scene as ordinary uGUI objects (Image, TMP_Text, Button, Slider, Toggle, ScrollRect) plus prefabs (`CardView`, `HudCardIcon`, `EraCard`, `ChallengeRow`, `BlueprintNode`, `ShopRow`, `SkinTile`). Code only sets text, `fillAmount`, sprites from `ArtSet`, interactable and visibility. Frames, backgrounds and static icons are plain sprites in the scene: decor the owner may replace or remove.

### 10.2 Menu

`MenuView` has panels; one is visible at a time; Back returns to Main.

- **Main:** title ПЕРИГЕЙ (logo Image, decor), dust counter top right, buttons ИГРАТЬ, ИСПЫТАНИЯ, МАСТЕРСКАЯ, НАСТРОЙКИ, and a line «Забегов: {runs} · Лучшее: {best of bestTimes}». While `tutorialDone` is false, ИГРАТЬ starts the tutorial run at once (as now) and the other buttons are hidden.
- **Eras (ИГРАТЬ):**
  - Mode tabs ЗАБЕГ / БЕСКОНЕЧНЫЙ / ДЕНЬ. A locked tab shows a lock and its condition («После победы в эре 1»).
  - ЗАБЕГ and БЕСКОНЕЧНЫЙ show four `EraCard`s in a horizontal ScrollRect (all four visible in landscape; swipe in portrait). An EraCard has: era picture (`ArtSet.eraIcons`), name, rule line, «Босс: {name}», «Новые враги: {two names}» (era 1: «Первая звезда»), best time for the tab's mode, a gold victory mark when the era is won, and a lock overlay «Победи босса эры {n − 1}» when locked. Tapping a card selects it (outline). The last played era is preselected.
  - ДЕНЬ shows one card: «ИСПЫТАНИЕ ДНЯ», the modifier name and text, «Сегодня: пыль ×2» or «Сегодня уже сыграно», best daily score.
  - Button СТАРТ starts `AbilityPick` (or the run directly under NoAbilities).
- **Challenges (ИСПЫТАНИЯ):** 8 `ChallengeRow`s: number, name, era, modifier text, reward (dust amount or skin picture), status: «Пройдено» / «Откроется в эре {n}» / button «Играть».
- **Workshop (МАСТЕРСКАЯ):** dust counter and three tabs.
  - ЧЕРТЕЖИ: four columns (ЗВЕЗДА, УДАР, ОРБИТА, КРИСТАЛЛЫ) x four tiers of `BlueprintNode`s (icon, level pips «●●○○○», locked look). Tapping a node fills the details strip at the bottom: name, text with the current total (8.3), next-level cost, button «Купить · {cost}», «Макс.», or «Нужен уровень: {previous node}».
  - СПОСОБНОСТИ: seven `ShopRow`s: pictogram, name, one-line effect, duration and cooldown, and «Есть» or «Купить · {price}».
  - ОБЛИК: two rows of `SkinTile`s (Спутник, Звезда): picture, name, and «Выбран» / «Выбрать» / «Купить · {price}» / «Победа в эре {n}» / «Испытание «{name}»».
- **Settings (НАСТРОЙКИ):** 10.4.
- **Confirm dialog:** text and Да / Нет, used by Settings.

### 10.3 Pick screens

- **CardPick:** dim overlay, 3 or 4 `CardView`s. A CardView has:
  - Frame (`ArtSet.cardFrames[color]`: cream, red, black; every frame has a dark text panel in its lower third);
  - RareRim (Image shown for Rare cards) and EraBadge (`ArtSet.eraIcons[era - 1]`, shown for era cards);
  - Icon, Title, stack pips («●●○» = after taking it / max), Plus line (cream), Minus line (red).
  - Under the cards: «Заменить ({n})» while free rerolls remain, then the ad button «Новые карточки (реклама)» (once per run).
- **AbilityPick:** the same layout with `ArtSet.abilityFrame`; abilities not owned are dimmed with a lock and «{price} пыли».
- Buttons ignore input for `pickInputDelay` 0.4 s after opening and accept only pointers pressed after the screen opened. After a pick, the overlay fades out over 0.4 s with the simulation still stopped, then the state returns to Playing.

### 10.4 Settings

`SettingsView` is one object, opened from Main and from Pause. Every change is saved at once into `SettingsData`:

```
float sfxVolume = 1        // Звуки, slider: AudioManager effect volume
float droneVolume = 0.6    // Гул, slider: drone volume
bool shake = true          // Тряска экрана: Fx.Shake does nothing when off
bool damageNumbers = true  // Числа урона
int hpRings = 0            // Полоски HP врагов: 0 После удара, 1 Всегда, 2 Никогда
bool simpleBackground      // Фон: Полный / Простой — Простой hides Background/Bars and Star/Rays and stops ring Spin
```

Also: button «Пройти обучение снова» (confirm; sets tutorialDone = false), button «Сбросить прогресс» (confirm «Сбросить всё? Пыль, чертежи, эры, способности и облик пропадут.»; fresh save, settings kept), label «Версия {Application.version}», button «Закрыть».

### 10.5 HUD

Layout as before: portrait — bars top centre, dust top left, timer and pause top right, card icons bottom left, ability buttons bottom right, hint bottom centre; landscape — bars top left, dust, timer and pause top right, hint in the left column; the top band of 1 unit is free only where |x| > 3.4 units.

`BarView` (one component for every bar) has layers Back, Ghost, Fill, Frame and an optional Text; `Set(value, max)`:

- **Star HP:** on damage the Fill drops at once; the Ghost (light cream) holds for `barGhostDelay` 0.35 s, then shrinks to the Fill in `barGhostTime` 0.4 s (Ease.OutQuad); the bar punches 6 px for 0.15 s and the Frame flashes red for 0.15 s. On heal the Ghost (gold) jumps to the new value and the Fill grows to it in barGhostTime. Text «{ceil hp} / {max}». Below 30% the Fill's alpha pulses between 0.7 and 1 at 2 Hz.
- **XP:** the Fill tweens to the new value in `xpFillTime` 0.25 s. On a level-up it fills to 1, flashes gold for 0.2 s, then restarts from 0 with the carried XP. A level badge left of it shows the level number.
- **Boss:** like Star HP, with the boss name above it (Twins: the sum).
- Under the timer: the modifier name (Daily, Challenge) or «Цикл {L + 1}» (Endless).
- Ability buttons show cooldown seconds (6.4).

All tweens SetLink to their object; numbers in GameConfig.

### 10.6 Banners

`BannerView`, top centre under the bars: a line of text and an optional icon; fade in 0.2 s, hold, fade out 0.3 s; one at a time, the rest wait in a queue.

- Boss arrival: the boss name, 2 s.
- First meeting of an enemy type (Run, Endless, Challenge; not the tutorial; not Swarmer or Bolt): its tip from `Strings.EnemyTip`, 3 s; the type is added to `seenEnemies`.
- Eras 2 to 4: the era rule line at run start, 3 s.
- Endless boss kill: «+{N} пыли», 2 s.

Enemy tips: Стрела «Стрела: быстрая и больно бьёт»; Бастион «Бастион: в броне — бей у звезды или в резонансе»; Паразит «Паразит: присасывается к звезде — сбей его у самой звезды»; Стрелок «Стрелок: стоит вдали и стреляет»; Улей «Улей: у звезды лопается роем»; Дробитель «Дробитель: после гибели распадается на рой»; Факел «Факел: греет спутник при каждом ударе»; Кружащий «Кружащий: кружит навстречу спутникам»; Щитник «Щитник: щит держит удар с одной стороны. Бей с другой или жми РЕВЕРС»; Пожиратель «Пожиратель: ест кристаллы и растёт»; Призрак «Призрак: когда прозрачный, его не задеть».

### 10.7 Tutorial hints

Unchanged (tutorial run only, one at a time, cream text, 0.3 s fade; `TutorialHints`):

| id | shows when | hides when | text |
| --- | --- | --- | --- |
| H\_HOLD | run start | the 3 parked Swarmers die, or after 12 s | «Держи — спутник уходит наружу» |
| H\_RELEASE | first enemy within r 3.0 after H\_HOLD hides | release held for 0.3 s | «Отпусти — спутник падает к звезде» |
| H\_HEAT | first time any heat >= 0.7 | heat < 0.3 | «Перегрев! Держи, чтобы остыть» |
| H\_ARMOR | first Bastion spawns | it dies | «Броня: бей внутри серебряного кольца» |
| H\_PULSE | >= 4 enemies within 3.0 and PULSE ready | PULSE used | «Жми Q или кнопку: ИМПУЛЬС» |
| H\_RES | first resonance | after 2 s | «Резонанс: урон ×2» |

### 10.8 Result and pause

- **Result:** Defeat — survived time, 'N s short of the record' (or 'new record'), dust breakdown (8.2); Victory — time, dust breakdown; Endless — time, loops, bosses killed. Then «Открыто:» lines for anything new (era, Endless, skin, challenge reward). Buttons Retry (primary), «Пыль ×2 (реклама)», Menu.
- **Pause:** Продолжить, Заново, Настройки, Меню.

All UI strings live in the static `Strings` class (Russian). A rewarded button's text names the reward and says it is an ad.

## 11. Visual spec and swappable art

The owner's art brief (card frames, icons, enemies, skins, eras) is `Docs/ArtPack.md`; the code only needs the names and slots below.

### 11.1 Principle

Art is data, not code. Everything the player sees is a prefab or a scene object. Its sprite, colour, material, size and sorting are set in the Inspector. The owner changes the art without touching code, in either of two ways:

- Drag another sprite onto the `Body` SpriteRenderer of the prefab (or onto an Image in the scene).
- Overwrite the PNG in `Assets/Perigee/Art/` under the same file name; every reference survives.

Every part of an object is one of two kinds:

- **[R] required:** code reads or drives it. The owner may change its picture, size and colour, or hide it by disabling its renderer, but must not delete it.
- **[D] decor:** nothing in code refers to it. It animates itself with a small component (`Spin`, `Pulse`, `PulseAlpha`, `Scroll`, `TrailReset`). The owner may delete, replace or add decor freely. Decor numbers are serialized fields of these components, not GameConfig.

### 11.2 ArtSet, Visual, sizes

- **`ArtSet`** (`Assets/Perigee/ArtSet.asset`) holds the pictures that code spawns or assigns, and nothing else:
  - prefabs: `satelliteSkins[]` (by satellite SkinId; entry 0 = the former `satellite`), `echo`, `crystal`, `enemies[]` (by EnemyType, 17 entries), `shard`, `spark`, `resonanceThread`, `telegraphArrow`, `telegraphWedge`, `telegraphLine`, `damageNumber`, `pulsarWave`;
  - sprites: `cardIcons[]` (by CardId, 30), `abilityIcons[]` (by AbilityId, 7), `cardFrames[]` (by CardColor, 3), `cardRareRim`, `abilityFrame`, `blueprintIcons[]` (by BlueprintId, 16), `enemyIcons[]` (by EnemyType; an empty entry means "use the Body sprite of the enemy prefab"), `eraIcons[]` (4), `eraBackdrops[]` (4, may be empty), `eraStarSprites[]` (4, may be empty), `starSkins[]` (pairs `body`, `rays`, by star SkinId).
  - Objects that exist once (Star, ArmorRing, PulseRing, ReverseFx, StasisFx, PullFx, DomeFx, FlareSector, BeaconRing, BossOrbitLine x2, Background, all UI) are scene objects with serialized references. A prefab changed in Play Mode shows up after the next Restart, because pools are rebuilt at run start.
- **`Visual`** sits on the root of every prefab and on `Star`; `body` is the SpriteRenderer of the child `Body`. Methods: `SetAlpha`, `Tint`, `Untint`, `Flash`, `SetSizeMul` (as now). Code never writes an absolute colour or scale, only changes relative to what the owner set. Recoil (3.7) moves the Body child, never the root.
- **Orientation:** the tip of a sprite points to +Y. `EnemyDef.faceStar` is true for all enemies except Parasite, Hive, Boss, Twin and Pulsar; an Orbiter faces its direction of motion instead.
- **Size:** collision radii are GameConfig values and never depend on a sprite. Default prefabs are drawn so that the visible radius equals the collision radius. Hit circles are drawn in `OnDrawGizmos` by the systems (Scene view; nothing in a build). Fit a picture by changing the prefab's Body scale; change the radius in GameConfig only when the gameplay should change.

### 11.3 Default look (placeholder art)

Soviet constructivist poster. Flat shapes, no gradients, no post-processing, unlit sprite material only (no Light2D). Palette: background #0E0E0E, cream #EFE4C8, red #D6281F, gold #E3B23C, silver #B9BDC2. Gold = beneficial (crystals, resonance, rare). Red = enemy or danger. Satellites are the highest-contrast objects on screen.

Existing placeholders (`Assets/Perigee/Art`, 512 px, PPU 256, see PROGRESS Notes) stay. New placeholders are made through the MCP the first time a milestone needs them (`execute_code`, nothing saved as a tool), with the same import settings: `arc_half` (180 deg ring arc), `sector_90` (90 deg filled sector, apex at the pivot), `ring_arrows` (ring with three arrowheads), `card_frame_cream`, `card_frame_red`, `card_frame_black` (rounded card 512x704 with a thick black border, flat colour, dark text panel in the lower third), `card_rim_rare` (gold rim overlay), `ability_frame` (black card with gold border), `icon_reverse` (two arrows on a circle), `icon_stasis` (hourglass), `icon_echo` (two overlapping diamonds), `icon_pull` (three arrows into a ring), `icon_dome` (half circle over a dot), `era_1`..`era_4` (a circle in the era's star tint on the era background), `icon_blueprint` (shared by all 16 nodes until the owner draws them). All 30 cards start with the shared `icon_card`. Material `SpriteWhite` (Assets/Perigee/Art, Sprite Unlit Shader Graph: colour white, alpha = texture alpha x vertex alpha) is made in M9.

| object | [R] required parts | [D] decor |
| --- | --- | --- |
| Star (scene object) | `Visual` Body: cream circle r1.0, black outline; `Rays` (sprite set by skin). Low HP tint | `Spin` on Rays 6 deg/s; `Pulse` on the root |
| Satellite skins (prefabs) | Body: cream diamond r0.2. `HeatArc` child (11.4). Stunned: `Tint(stunTint)` | `Trail` (TrailRenderer 0.35 s, with `TrailReset`) |
| Echo (prefab) | Body: the satellite sprite at alpha 0.4 | none |
| Crystal (prefab) | Body: gold diamond r0.14; blinks in the last 2 s | `Spin` |
| Every enemy and boss prefab | Body; `Body/FlashOverlay` (material `SpriteWhite`, alpha 0, 3.7); `HpRing` child (11.4); `Cracks` child with `DamageStages` (empty stages) | as listed below |
| Swarmer | Body: red triangle r0.2 | none |
| Arrow | Body: red triangle 0.2 x 0.6 | speed lines |
| Bolt | Body: red diamond r0.15 | none |
| Bastion | Body: red wedge r0.55, silver outline | rivets |
| Parasite | Body: red circle r0.25; `AttachMarker` child | legs |
| Shooter | Body: red `circle_hollow` r0.3 | barrel |
| Hive | Body: red hexagon r0.6 | holes |
| Splitter | Body: red diamond r0.32 | 3 thin black `rect` seams across it |
| Torch | Body: red circle r0.28 | `Flame`: red `triangle` behind it (away from the star), `Pulse`; small cream circle core |
| Orbiter | Body: red `circle_hollow` r0.25 | small cream dot inside; red `triangle` tail |
| Shielder | Body: red circle r0.35; `Shield` child: silver `arc_half` at radius 0.45, rotated by code to the shield side, hidden when broken | none |
| Devourer | Body: red circle r0.40 | `Teeth`: small cream `ring_dashed_24` inside |
| Phantom | Body: red triangle r0.22 | small cream circle eye |
| Boss (Comet) | Body: red circle r0.9 | `Trail` 1.2 s red to cream |
| Twin | Body: red circle r0.75 | `Trail` 1.0 s red to cream |
| Colossus | Body: red `bastion` shape r1.1, silver outline | silver plates (`rect`s) |
| Pulsar | Body: red circle r1.0 | `Beams`: two cream `rect`s in a cross with `Spin` 90 deg/s |
| PulsarWave (prefab) | Body: red `ring_thin`, scaled by code to the wave radius | none |
| DamageNumber (prefab) | TextMeshPro (world), cream, sorting 70 | none |
| Telegraphs (prefabs) | `TelegraphArrow`, `TelegraphWedge`, `TelegraphLine` | `PulseAlpha` |
| ArmorRing (scene) | root `Visual`, Body = child `Ring`: silver `ring_dashed_60` | `Spin` on Ring; `Shield` icon |
| PulseRing, BeaconRing (scene) | cream `circle_hollow` / `ring_thin` | none |
| ReverseFx (scene) | cream `ring_arrows` | none |
| StasisFx (scene) | silver `ring_dashed_60` at radius 6.2 | `PulseAlpha` |
| PullFx (scene) | cream `ring_dashed_40` | `PulseAlpha` |
| DomeFx (scene) | cream `circle_hollow` at radius 1.25 | `PulseAlpha` |
| FlareSector (scene) | red `sector_90`, radius 6.6, rotated by code | cream thin `rect` rays inside, `PulseAlpha` |

**Background** (scene object): [R] `Backdrop` (SpriteRenderer for `eraBackdrops`, hidden when empty, alpha 0.25, sorting -10); [R] `Rings` with `RingHighlight` (as now); [D] `Bars` (as now). The camera colour is the era background (7.2).

Sorting order (set in prefabs and the scene, not by code): backdrop -10, bars 0, rings 10, flare 15, crystals 20, enemies 30, star 40, satellites 50, FX 60, damage numbers 70.

### 11.4 Procedural geometry

Code fills in the points of these objects; colour, width, material and sorting stay in the Inspector. Point arrays are preallocated and written with `SetPositions`.

- **ResonanceThread** (prefab, pooled): gold LineRenderer zigzag between two satellites; `TMP_Text` label with the multiplier.
- **HeatArc** (child of every satellite skin): red arc around the satellite, length = heat x 360 deg.
- **HpRing** (child of every enemy and boss prefab): cream LineRenderer arc of up to 40 points around the enemy at radius `radius + 0.12`, length = shown HP fraction x 360 deg, starting at the top and going clockwise; a darker full circle behind it is decor.
- **BossOrbitLine** (two scene objects, the second for Twins): red dashed loop of 64 points on the next ellipse, alpha 0.3, shown for 1.5 s.

## 12. FX and audio

- **Fx:** `HitStop(sec)` and `Shake(amp, dur)` use real-time timers. Shake does nothing when Settings.shake is off.
  - Hit-stop 0.04 s only on kills of enemies with base hp >= 2, boss hits and star damage; 0.02 s on clinks. A new hit-stop starts only if `hitStopMinInterval` 0.25 s has passed since the last one.
  - Star damage = `Flash(starHitFlashColor, 0.15 s)` on the star + shake (amp 0.12, dur 0.12). A new shake starts only if `shakeMinInterval` 0.2 s has passed.
  - Kill = 6 cream shards flying outward for 0.4 s (x2 for big enemies, 3.7). Clink = 4 silver sparks + a Blocked number. Crystal pickup = small pop + punch-scale of the dust counter.
  - Shards, sparks and damage numbers are pooled prefabs from ArtSet, moved by `Fx`, not ParticleSystem. When the number pool is empty the oldest number is reused.
- **Audio:** unchanged: `AudioManager.Play(SfxId)` with `enum SfxId { Hit, Clink, Kill, Crystal, Resonance, StarHit, Stun, CardOpen, CardPick, BossIn, Win, Lose }`, clips in `AudioBank` (empty = silence), looped drone with pitch from the average angular speed. R3 adds no sounds: shield blocks play Clink, every boss arrival plays BossIn, a Pulsar wave stun plays Stun. Volumes come from Settings (10.4).

## 13. Services

Interfaces with stub implementations that complete instantly; no real SDK. Unchanged from R2.1 and M8:

- `IAdService`: ShowRewarded(Action\<bool>), ShowInterstitial(). `ISaveService`. `ILeaderboardService`: Submit(score). `IPlatformService`: GameReady(), GameplayStart(), GameplayStop().
- GameReady when the menu first becomes interactive; GameplayStart on entering Playing; GameplayStop on leaving it; focus or visibility lost → Playing becomes Paused, `AudioListener.pause = true`.
- Rewarded: Revive (once per run, on Defeat: HP 50%, enemies within 4.0 die, heat 0, 1.5 s immune), x2 dust on Result, New cards on the card screen (once per run). Interstitial on Retry or Menu from Result only if runs >= 3, the run lasted >= 30 s, >= 150 s since the last interstitial and >= 60 s since the last rewarded ad.
- R3 adds no ad placements. Submit receives the score of every finished run (Endless: seconds).

## 14. Code structure (folder Assets/Perigee/: Scripts, Art, Prefabs, Scenes; namespace Perigee)

| group | files |
| --- | --- |
| Core | GameConfig, ArtSet, Visual, GameLoop (state machine, fixed step, hit-stop, EnemyTimeScale, StartFromMenu(era, mode, challenge)), RunState (hp, dust, float xp, level, time, card stacks, era, mode, modifiers, loop, kills), Stats, Rng, Strings |
| Gameplay | InputHold, StarController, SatelliteSystem (orbit, direction, heat, stun, resonance, saw thread, hitters and echoes), EnemySystem (behaviors, collisions, shields, burn, chain, star contact), Director (era timelines, Endless loops, modifiers), CrystalSystem, BossSystem (Comet, Twins, Colossus, Pulsar; replaces BossComet), EraRuleSystem, CardSystem, AbilitySystem, Blueprints (tree), Shop (abilities, skins), Progress (unlocks, rewards, dust) |
| Presentation | Spin, Pulse, PulseAlpha, Scroll, TrailReset (decor), RingHighlight, ArmorRing, ResonanceThread, HeatArc, HpRing, DamageStages, BossOrbitLine, FlareSector, Fx, AudioManager, Hud, BarView, BannerView, CardView, CardPickView, AbilityPickView, AbilityButtonView, PauseView, ReviveView, ResultView, MenuView, EraCard, ChallengeRow, BlueprintNode, ShopRow, SkinTile, SettingsView, ConfirmDialog, TutorialHints |
| Services | IAdService, ISaveService (SaveData, SettingsData), ILeaderboardService, IPlatformService, SaveService, stub implementations |

Performance: one pool per prefab, prewarmed at run start; at most 200 enemies; no per-frame allocations; no LINQ inside the loop; component references cached when an object is created, never inside a tick. Target: 60 fps on a mid-range phone browser with 150 enemies and numbers on.

## 15. Scene setup (one scene `game`)

- Main Camera (orthographic, URP 2D Renderer, background from the era)
- Systems: GameLoop, SatelliteSystem, EnemySystem, CrystalSystem, Director, BossSystem, EraRuleSystem, CardSystem, AbilitySystem, Fx, AudioManager (children Sfx and Drone), with references to `GameConfig` and `ArtSet`
- World: Star (Body, Rays), Satellites, ArmorRing, PulseRing, BeaconRing, ReverseFx, StasisFx, PullFx, DomeFx, FlareSector, BossOrbitLine, BossOrbitLine2, Background (Backdrop, Bars, Rings), Enemies, Boss, Telegraphs, Crystals, Fx
- Canvas: HoldZone (first child), Hud (bars as BarViews, Dust, Clock, ModeLabel, Cards, Abilities, Banner, hints), CardPickView, AbilityPickView, PauseView, ReviveView, ResultView, MenuView (Main, Eras, Challenges, Workshop, Settings panel host), SettingsView, ConfirmDialog
- EventSystem with InputSystemUIInputModule
- Project: `Assets/Perigee/GameConfig.asset`, `ArtSet.asset`, `AudioBank.asset`; URP asset with the 2D Renderer; build target WebGL; only `game.unity` in Build Settings.

## 16. Milestones

Сделано по R2.1 (не переделывать без строки ниже): M1 управление и орбита, M2 Рой и HP звезды, M3 кристаллы, опыт, карточки, HUD, пауза, M4 все враги, расписание, броня, нагрев, резонанс, M5 способности, M5A картинки в префабы, M6 Комета, итоги, сохранение, чертежи, меню, M7 эффекты и звук, M8 заглушки сервисов, обучение, WebGL.

Новые этапы R3:

| # | Что делаем | Читать | Готово, когда |
|---|---|---|---|
| M9 | Видимый урон и живые полоски: вспышка, отдача, кольцо HP, стадии трещин, числа урона, большие убийства; BarView для HP, опыта и босса; секунды перезарядки на кнопках; SettingsData в сохранении (без экрана) | 3, 8, 10, 11, 12 | Удар по Бастиону: он белеет и вздрагивает, вокруг появляется кремовое кольцо HP и убывает с каждым ударом, над ним всплывают цифры урона; удар в броню показывает серебряный «0»; при уроне звезды полоска HP падает, а светлый «хвост» догоняет её через 0.35 с; опыт растёт плавно; на кнопке способности идут секунды |
| M10 | Способности R3: убрать ЯКОРЬ, ХОЛОД, ВСПЛЕСК (код, AnchorRing, SurgeTrail, поля конфига); добавить РЕВЕРС, СТАЗИС, ЭХО, ПРИТЯЖЕНИЕ, КУПОЛ; направление орбиты; хиттеры и эхо; время врагов; владение способностями (поле abilitiesOwned, по умолчанию Реверс, Рост, Импульс; lastAbilityA по 8.1) | 3, 6, 8, 10, 11 | В выборе 7 способностей, 4 закрыты замком (открыты при debugUnlockAll); РЕВЕРС разворачивает орбиту и 2 с бьёт ×1.5; СТАЗИС замедляет врагов и Комету вчетверо; у ЭХА тени бьют и собирают кристаллы; ПРИТЯЖЕНИЕ стягивает врагов на внешнюю орбиту; под КУПОЛОМ звезда не теряет HP; ИМПУЛЬС, РОСТ и обучение работают как раньше |
| M11 | Карточки R3: формат CardDef (цвет, редкость, эра), 21 карточка из 6.2 с эффектами (крит, бронебой, раскол, нить-пила, поджог, цепная реакция, жадность, фотосфера, корона, второй маяк, разгон), XP дробный, выбор по весам, CardView с рамкой по цвету и золотой каймой | 3, 6, 10, 11 | Экран выбора показывает карточки в кремовых, красных и чёрных рамках, редкие с золотой каймой, с плюсом и минусом из 6.2; каждая карточка работает как написано (крит даёт золотые цифры, БРОНЕБОЙ сдвигает кольцо брони, НИТЬ-ПИЛА режет врагов нитью, ФОТОСФЕРА лечит с золотым «хвостом» полоски); иконки взятых карточек видны в HUD |
| M12 | Новые враги и эры как данные: Дробитель, Факел, Кружащий, Щитник, Пожиратель, Призрак (поведение, префабы-заглушки, кольцо HP); EraDef с 4 эрами (числа, расписания 5.1, фоновый поток, цвета, множитель пыли); BannerView и подсказки при первой встрече; до M13 эры 2–4 тоже заканчиваются Кометой | 4, 5, 7, 10, 11 | Эру можно выбрать в старом меню; в эре 2 летят Дробители и Факелы, в 3 — Кружащие и Щитники, в 4 — Пожиратели и Призраки, каждый ведёт себя как в 4.2 (Щитник держит удар с одной стороны, РЕВЕРС его открывает); при первой встрече типа сверху появляется подсказка; у эры свой цвет фона и звезды; пыль умножается на множитель эры |
| M13 | Правила эр, карточки эр, боссы: вспышки, тяжесть, воронка; 9 карточек из 6.3; BossComet → BossSystem; Близнецы, Колосс, Пульсар; баннер босса; вторая линия орбиты | 4, 6, 7, 10, 11 | В Красном карлике каждые 25 с предупреждение и вспышка сектора, она греет спутники и бьёт врагов; в Тяжёлой звезде спутники взлетают медленнее, падают быстрее, кристаллы тонут; в Чёрной дыре враги идут по спирали; в каждой эре предлагаются её 3 карточки и работают; на 3:00 приходит босс эры и ведёт себя как в 4.3; победа над ним даёт Victory |
| M14 | Мета: дерево чертежей 4×4, магазин способностей, облик (скины спутника и звезды), сохранение v2 с переносом старого, пыль по 8.2 с разбивкой на итогах, открытие эр и награды первой победы; экран Мастерской | 7, 8, 10, 11 | В Мастерской дерево из 4 веток: покупка поднимает уровень, эффект виден в забеге, следующий узел открывается после первого уровня предыдущего; способности покупаются и появляются в выборе; выбранный облик виден со следующего забега; старое сохранение переносится, пыль за старые чертежи возвращается; первая победа в эре открывает следующую и даёт награду |
| M15 | Главное меню, экран эр, режимы, испытания, настройки: Main, Eras (Забег, Бесконечный, День), Испытания, Настройки; Endless, Daily с модификатором, 8 испытаний, модификаторы 9.5; пауза с настройками; «Открыто» на итогах | 7, 8, 9, 10, 11 | Из главного меню открываются все панели; Забег, Бесконечный, День и все 8 испытаний запускаются и работают по разделу 9 (Бесконечный крутит циклы с боссом каждые 160 с); настройки сохраняются после перезапуска и действуют сразу (тряска, числа, кольца HP, простой фон, громкость); сброс прогресса спрашивает подтверждение |
| M16 | Финальный проход: проверить, что нигде нет ссылок на удалённое; обучение со всеми системами; статический TMP-шрифт с кириллицей и знаками (× ● ○ « » —) вместо динамического Fallback; Product Name «Перигей», в Build Settings только game.unity; убрать фичу Pixelation из Renderer2D; WebGL-сборка; замер 150 врагов с числами урона | 0, 10, 11, 12, 13, 14, 15 | Обучение проходит от начала до конца; WebGL-сборка запускается, текст с кириллицей виден сразу после сборки; в эре 4 со 150 врагами и цифрами игра держит 60 кадров в редакторе; в отчёте список того, что не проверено руками |

Notes for M9–M16:

- Each milestone writes new list entries into `GameConfig.asset` and `ArtSet.asset` (rule 10) and makes the placeholders it needs (11.3).
- M10 keeps the tutorial's slot A = PULSE. ABILITY\_LONG stays eligible only with a timed ability (REVERSE counts: its 2 s window).
- M11 replaces the card list; old card stacks in a running session do not matter (cards are per run).
- M12 to M13: bosses other than the Comet do not exist until M13; `era.boss` is read from M13 on.
- M14 owns the full save migration (8.1); M10 only adds `abilitiesOwned` and fixes `lastAbilityA`.
- Balance numbers are first guesses for the new content. Every one is in GameConfig; M16 reports any number that felt wrong in play.

## 17. Out of scope

Real Yandex SDK, new sounds or music, new ad placements, localization beyond the `Strings` class (Russian only), a leaderboard screen, achievements, cloud save, analytics, unit tests, editor tooling, sprite atlases, animation clips (the owner may add Animators to prefabs as decor).
