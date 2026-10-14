# PERIGEE — Game Design Document

**Reader:** Claude Opus 5.5 implementing the game in Unity. **Engine:** Unity 6.3, URP with the 2D Renderer, C#. **Target:** WebGL (Yandex Games), desktop and mobile browsers, landscape and portrait.
**Version:** R4, the full game. `Docs/archive/` is history: do not read it. `Docs/ArtPack.md` is for the owner (art prompts): do not read it.

R3 was built as M9–M15 (PROGRESS.md). R4 keeps everything R3 built and adds:

- a data foundation: one AudioBank with named slots for every sound and music track, labelled ArtSet slots, Russian and English text (M16);
- 6 new cards, 8 evolutions (two maxed cards merge into one), combo (M17);
- 5 new enemies, elite enemies, 4 new eras as data (8 in all) (M18);
- the rules of eras 5–8 (two of them with gravity objects), their 12 cards and 4 bosses, the final boss (M19);
- 5 star classes (M20);
- relics from bosses and the Herald, in-run events (M21);
- an effects pass (M22);
- the Atlas (guide: basics, bestiary, cards and abilities) and 36 achievements (M23);
- the intro and the ending told by a narrator (M24);
- the release pass (M25).

Ads stay as the existing stubs. Sounds and music are slots the owner fills (12.2).

## 0. Rules for the implementing AI

1. Work milestone by milestone (section 16). After each one the project compiles and is playable. Report in 5 lines or fewer, then stop.
2. Implement only what this document says. No extra features, tests, README, logging, try/catch (one exception: SaveService, section 8.1), defensive null-checks, editor tools, or refactors that no milestone asks for. Serialized references are assumed assigned.
3. Every tunable number lives in `GameConfig` (ScriptableObject), or in a list inside it, with the default given here. No magic numbers in code. Systems read GameConfig values when they use them, not cached copies made in Awake, so a value changed in the Inspector during Play Mode applies at once.
4. If something is unspecified, take the simplest option consistent with this document and mention it in one line of the report. Ask only when blocked.
5. C# comments in Russian, only where logic is non-obvious. Namespace `Perigee`. Identifiers in English.
6. Unity MCP connected: create scene objects, prefabs and settings through it. Not connected: write scripts only and give the manual steps as a numbered checklist in plain words.
7. Use the Input System package (`UnityEngine.InputSystem`), never legacy `UnityEngine.Input`.
8. Art is data, not code (section 11). Everything visible is a prefab or a scene object whose sprites, colours, sizes and sorting are set in the Inspector; the pictures that code needs are listed in `ArtSet`. Code moves, rotates, scales, tints, fades, enables and pools these objects and instantiates prefabs. It never builds sprites, textures or UI from scratch (`new GameObject`, `AddComponent`, `new Texture2D`). New art starts as placeholders (11.3); the owner replaces them later.
9. R4 extends finished code. `Docs/PROGRESS.md` (Notes) describes the current code: read the Notes of the systems a milestone touches before changing them. Names follow the existing code. Each system belongs to one milestone (its row in section 16 says which); a later milestone only calls the public methods an earlier one made and does not reopen its internals, except where its row says so.
10. GameConfig, ArtSet and AudioBank are assets: a new list or a changed default in code does not reach an existing asset. When a milestone adds list entries (cards, enemies, eras, relics, classes, achievements, sounds), it writes them into the asset too and says so in the report. New enum values are always appended at the end, so existing arrays and saves keep their meaning.
11. `GameConfig.debugUnlockAll` (bool, default false): while true, every era, mode, ability, challenge, skin and star class is available, nothing is written to the save for it, and dust is not spent.
12. Every player-visible text exists in Russian and English (10.11). The Russian text is given here; the English one is written by the milestone that adds the text (short, same meaning, terms from the glossary in 10.11).
13. Every new action that has a sound in 12.2 calls `AudioManager.Play` with its `SfxId` in the same milestone, even while the slot is empty.

## 1. Concept

One-finger arcade defense. A star sits at the center; 1 to 4 satellites orbit it (all of one kind). Enemies fly toward the star. A satellite damages whatever it touches.

- **Hold** the pointer: satellites move outward. **Release**: they fall inward.
- Close to the star = fast, strong, heats up. Far = slow, weak, cools down, reaches distant enemies.
- The star has HP. Leaked enemies hurt it. The player cannot kill everything; the skill is choosing what to kill.
- A run lasts 180 s and ends with the era's boss. Crystals from kills fill an XP bar; each fill opens a pick-1-of-3 card screen (every card has a plus and a minus). Two maxed cards can merge into an evolution. Quick kills build a combo that multiplies crystals. Two active abilities are used by tapping buttons.
- Mid-run the Herald (an elite) carries a relic; random events (meteors, a golden comet, storms) change a few seconds of play.
- Eight eras (star systems). Each has a rule that changes how orbits play (two of the late ones bend orbits with gravity), new enemies, its own boss and its own cards. A victory opens the next era. Era 8 ends with the final boss and the ending.
- Before a run the player picks a star class: the same eras play differently with a red giant or a white dwarf.
- Crystals become dust. Dust buys blueprints (permanent passive upgrades in a tree), abilities, a star class and skins. Achievements pay dust.
- Modes: Run (era by era), Endless (relic after every boss), Daily (one modifier a day), Challenges (fixed rule sets with rewards).
- A narrator tells the intro before the first run and the ending after the final boss.

## 2. World, units, camera

- 2D, 1 unit = 1 world unit. Star at (0,0). No Unity physics: no Rigidbody2D, no colliders. All motion and collisions are manual circle checks: `distSq <= (ra+rb)^2`. Collision radii are GameConfig values and do not depend on the sprites; the default art is drawn to match them (section 11.2).
- Camera: orthographic. `orthoSize = H * max(1, 1/aspect)` with H = 6.2, recomputed when the screen size changes. A field of radius 6.2 stays fully visible in landscape and portrait. The background colour comes from the era (7.2).
- Enemies spawn on a circle of radius 7.0 (Arrows on 5.3). Along the long screen axis this circle is visible, so enemies fade in (section 5).

### 2.1 Default constants (all in GameConfig)

starRadius 1.0 | starMaxHp 120 | H 6.2 | spawnRadius 7.0 | arrowSpawnRadius 5.3 | rMin 1.35 | ringMax 6.2 | satMaxRadius 6.2 | ringGap 0.8 | satRadius 0.2 | maxSatellites 4 | radialSpeed 8 | tMin 1.0 | powerNear 3.0 | powerFar 1.0 | powerFarRadius 4.6 | powerFloor 1.0 | armorBase 2.0 | hitCooldown 0.2 | heatZoneMax 2.0 | coolZoneMin 2.6 | heatRate 0.35 | coolRate 0.25 | stunTime 1.0 | heatAfterStun 0.4 | resonanceHalfAngle 35 | resonanceMul 2.0 | critMul 2.0 | crystalLife 10 | crystalRadius 0.14 | magnetSpeed 7 | spawnFadeIn 0.25 | telegraphRadius 6.6 | satFadeIn 0.3

Visual constants (also GameConfig): stunTint #B9BDC2 at alpha 0.6 | starHitFlashColor #D6281F | heatArcRadius 0.32 | prewarm counts per pool (enemy prewarm lives in `EnemyDef.prewarm`; crystal 60, shard 40, spark 20, each marker type 8, resonance thread 3, damage number 40, echo 4).

`maxSatellites` grows from 3 to 4 in R3, but the 4th satellite is reachable only with the QUADRIGA blueprint (8.3). kExp, spinMul and the other era numbers come from the current era (7.2).

`starRadius`, `starMaxHp` and `rMin` above are the Yellow class values; the chosen star class (8.6) replaces them for the run, and `heatZoneMax` and `coolZoneMin` move by `class.rMin - 1.35` with it.

## 3. Simulation

### 3.1 Loop

`GameLoop` runs the simulation in `Update` with a fixed step of 1/60 s via an accumulator (`Time.deltaTime` clamped to 0.1, max 4 steps per frame). No steps run while the state is not `Playing` or while hit-stop is active (hit-stop is a real-time timer, section 12).

Tick order each step: Satellites (and echoes, gravity offsets), Abilities, EraRule (clouds, moons, anomalies, darkness), RunEvents, Enemies, Boss, Collisions (hitters vs enemies, boss, event objects; saw thread; queued blasts), Crystals, Director, Star (regen, drain, class trait), Cards (beacon, harvest, XP check), Combo, Fx, Hints.

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
- **Gravity offset** (eras 6–7, the Graviton and the Abyss, 7.3 and 4.3). Each satellite has `gravOffset_i` (starts 0). Every gravity source s (centre `c_s`, radius `R_s`, bend `B_s`, sign +1 for a well, -1 for a repulsor) whose centre is within `R_s` of the satellite gives `target_s = sign * B_s * (1 - d / R_s) * Sign(|c_s| - r_i)`, where d = distance satellite–centre. `gravOffset_i = MoveTowards(gravOffset_i, clamp(sum of target_s, -gravMaxOffset 1.0, +gravMaxOffset), gravOffsetSpeed 2.0 * dt)`. The satellite flies at `r_i + gravOffset_i`, clamped to `[starRadius + 0.25, satMaxRadius]`; that radius is used for Power, heat, angular speed and the HeatArc. A well draws the orbit toward itself, a repulsor pushes it away. STABILIZER (6.3) keeps every offset at 0.
- **Slow and kick from enemies.** A Jelly hit sets that satellite's `slowTimer` to `jellyTime` 1.0 s; while it runs, the satellite's w(r) is x`jellySlow` 0.5. A Bumper hit adds `bumperKick` 1.2 to baseR at once (clamped to ringMax). Echo hits do neither.

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

**R4 additions to the hit steps:**

- Step 1 also multiplies: INERTIA `1 + stats.inertia` while baseR is changing (holding with baseR below target, or released with baseR above restR); moon assist x`moonAssistMul` 1.5 while that hitter's assist timer runs (7.3); `class.trait` factors (8.6); Storm event x`stormPowerMul` 1.5 (5.2); LAST\_LIGHT x1.4 while star HP < 30%.
- Step 2: HAMMER makes crits x`hammerCritMul` 3 and each crit lowers that enemy's armor by `hammerShred` 0.5 for good (not below 0).
- Steps 3 and 4: with PRISM a crit ignores the shield and the armor.
- Step 5: Jelly and Bumper effects (3.2) after the damage; a Mine killed by a satellite explodes and heats it (4.2).
- Event objects (meteors, the golden comet, 5.2) are hit like enemies with no armor.

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

### 3.8 Combo

- Every kill (any source, bosses and boss parts excluded) adds 1 to `combo` and restarts `comboTimer` = `comboWindow` 2.0 s (+ relic bonus). Timer 0, or star damage (not Parasite drain), sets combo to 0. `RunState.bestCombo` keeps the run maximum.
- Tiers: `comboTiers` [10, 25, 50, 100] give `comboMuls` [1.2, 1.5, 2.0, 3.0]; below 10 the multiplier is 1. A picked crystal gives XP and dust x the multiplier at the moment of pickup.
- HUD `ComboView` (10.5) is shown while combo >= `comboShowMin` 5. Sounds: ComboTier on reaching a tier, ComboBreak when a combo >= 10 ends.

## 4. Entities

### 4.1 Star

- Radius 1.0, max HP = starMaxHp + stats.starMaxHp bonuses (cards, HULL blueprint). HP <= 0: SECOND\_SUN if available, otherwise Defeat.
- **Regen:** stats.regen HP per second (PHOTOSPHERE card, CORE blueprint), never above max.
- **SECOND\_SUN** (blueprint, 8.3): once per run, when HP reaches 0, HP = `secondSunHp` 0.25 x max, a free PULSE wave fires (no cooldown change), and the star is immune for `reviveImmuneTime` 1.5 s. This happens before the Defeat screen and before the ad revive.
- **GlassStar** modifier (9.5): max HP x `glassStarHpMul` 0.4 and no healing of any kind.
- Low HP (< 30%): the Body is tinted toward red (as now).
- **Star class** (8.6): radius, max HP, rMin and the trait come from the class picked for the run. The Star's `Visual.SetSizeMul(class.starRadius)` and the class aura (11.2) are applied at run start.

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

Bosses (Boss = Comet, Twin, Colossus, Pulsar, and from R4 Sower, Mirror, Graviton, Abyss) are in 4.3.

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

R4 appends: Jelly, Mine, Bumper, Blinker, Eclipser (enemies, table below), Sower, Mirror, Graviton, Abyss (bosses, 4.3), Meteor, GoldComet (event objects, 5.2).

| type | name (RU) | radius | hp | speed u/s | armor | starDamage | crystals | behavior | first era |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Jelly | Студень | 0.38 | 4 | 0.6 | 0 | 12 | 4 | Straight + slows the hitter | 5 |
| Mine | Мина | 0.30 | 2 | 1.2 | 0 | 14 | 3 | Mine | 6 |
| Bumper | Отбойник | 0.34 | 3 | 0.8 | 0 | 10 | 3 | Straight + kicks the orbit out | 6 |
| Blinker | Прыгун | 0.24 | 2 | 1.1 | 0 | 8 | 3 | Blink | 7 |
| Eclipser | Затмитель | 0.45 | 6 | 0.5 | 1.0 | 18 | 6 | Straight + cuts light | 8 |
| Meteor | Метеор | 0.22 | 1 | 4.0 | 0 | 3 | 0 (3 when a satellite breaks it) | Chord | event |
| GoldComet | Золотая комета | 0.40 | 6 | — | 0 | 0 | 40 | Chord | event |

New `EnemyDef` fields (0 = off): `slowHitter`, `kickHitter`, `lightCut`, `blinkInterval`, `blinkDeg`. `EnemyBehavior` gains `Mine`, `Blink`, `Chord`, `Sower`, `Mirror`, `Graviton`, `Abyss`.

- **Jelly:** Straight. 3.2.
- **Mine:** picks a stop radius uniform in [`mineStopMin` 2.6, `mineStopMax` 5.0] (simRng), flies there and stops for `mineWait` 12 s, then flies on to the star. Killed by a satellite: explodes (`MineBlast` uses the chain blast with radius `mineRadius` 1.3, damage `mineDamage` 4, Swarmers die) and that satellite gains `mineHeat` 0.6 x stats.heatMul heat (none with SAPPER). Killed any other way: explodes without heating. Sound Mine.
- **Bumper:** Straight. 3.2.
- **Blink:** Straight. Every `blinkInterval` 1.6 s (enemy time) its Body flashes for `blinkWarn` 0.25 s, then it jumps along its circle by `blinkDeg` 35 deg, side from simRng, and its hit cooldowns reset. LOCK (6.3): it stops blinking after its first damage. Sound Blink.
- **Eclipser:** Straight. While alive, the light radius (Darkness, 7.3) is cut by `lightCut` 0.8.
- **Chord** (event objects): spawns at spawnRadius and flies in a straight line to the opposite side of the field (target = a point uniform within `chordInner` 3.0 of the center, continued past it); removed at spawnRadius + 0.5. It does not count as a leak. Meteor: touching an enemy deals `meteorDamage` 3 to it once (Swarmers die) and flies on; touching the star deals its starDamage and ends it; hit by a satellite or echo: breaks into 3 crystals. GoldComet: takes hits normally (Power), never touches the star (its line passes no closer than 3.0), gives 40 crystals and a banner when killed.

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

**Sower** (era 5, EnemyType.Sower: radius 0.9, hp 260, speed 0.8, starDamage 0, crystals 40).

- Moves in to radius `sowerRadius` 5.0, then circles counter-clockwise at `sowerDeg` 15 deg/s.
- Every `sowerInterval` 4 s (3 s below 50% hp) it drops a Hive at its position (no telegraph, speedMul `sowerHiveSpeed` 1.2). Hives burst into Swarmers at r 3.0 as usual, so the player must catch them far out.

**Mirror** (era 6, EnemyType.Mirror: radius 0.8, hp 240, armor 1.5, starDamage 0, crystals 40).

- Moves in to radius `mirrorRadius` 4.2, then circles counter-clockwise at 20 deg/s.
- Two guards (children `Guard0`, `Guard1` of the prefab, radius `guardRadius` 0.25) orbit it at distance 1.1 at `guardDeg` 180 deg/s, opposite each other. A satellite touching a guard deals no damage, gets heat +0.3 and baseR -`guardKnock` 1.0, with Clink feedback (once per `hitCooldown`). Guards cannot be damaged.
- Every `mirrorFlip` 10 s it flips the player's orbitDir (with ReverseFx, no REVERSE damage bonus) and its guards' direction; a banner-less cream flash on its Body warns 1 s before.
- Fires 2 Bolts at the star (+-10 deg) every 5 s.

**Graviton** (era 7, EnemyType.Graviton: radius 1.0, hp 300, starDamage 0, crystals 40).

- Moves in to radius 4.6, then drifts counter-clockwise at 8 deg/s.
- It is a well (3.2) centred on itself: `R` 2.6, `B` 1.0. Every `gravitonFlip` 8 s it turns into a repulsor for 3 s (Body flashes 1 s before); at the moment it flips it fires 5 Bolts at the star in a fan of +-24 deg.

**Abyss** (era 8, final, EnemyType.Abyss: radius 1.3, hp 700, starDamage see phases, crystals 0). Three phases by hp; at each change it is invulnerable for `abyssShift` 1.5 s, flashes, the camera shakes (0.15) and a banner shows the phase name; sound BossPhase.

| phase | hp | name (RU) | behavior |
| --- | --- | --- | --- |
| 1 | 100–66% | ГЛАЗ | moves in to radius 5.2, circles counter-clockwise at 12 deg/s; every 3 s spawns 3 Arrows at its position aimed at the star (0, +-15 deg), speedMul 0.8 |
| 2 | 66–33% | ЗАТМЕНИЕ | armor 1.5; light radius -1.0 (Darkness); moves on a comet ellipse (periapsis 2.2, apoapsis 5.5, period 7 s, as the Comet) dealing 10 at each periapsis; spawns 4 Phantoms Even every 8 s |
| 3 | 33–0% | ПАСТЬ | armor 0; becomes a well (R 3.0, B 1.0); walks at the star at 0.15 u/s; star contact deals 40 and puts it back at radius 5.0; spawns Swarm x8 Even every 5 s |

Killing the Abyss is the victory of era 8; the first one plays the ending (10.10) before Result.

### 4.4 Crystals

- On enemy death, spawn `crystals` crystals at uniform positions within 0.3 of the death position (simRng). Lifetime `crystalLife * stats.crystalLifeMul`, blinking during the last 2 s.
- An active hitter picks a crystal up when `dist <= HitterRadius + crystalRadius`: dust +1, XP + `stats.xpPerCrystal` (1 x card multipliers; XP is a float in RunState from R3).
- Magnet: crystals within `stats.magnetRadius` of any hitter fly to the nearest one at magnetSpeed.
- Heavy Star era: crystals that are not being pulled by the magnet drift toward the star at `crystalSink` 0.35 u/s and vanish when they reach starRadius (ANCHOR\_FIELD: no drift).
- Devourers eat crystals (4.2).

### 4.5 Kill

Kill = crystals (plus eaten ones), kill shards and sound (as now), then: Splitter spawns its Swarmers; CHAIN queues a blast at the death position (radius `chainRadius` 1.0, damage `chainDamage` 1, Swarmers die), resolved in the next Collisions step, and kills by a blast may queue new blasts; ACCRETION heals the star by `accretionHeal` 1 if the death position is within `accretionRadius` 3.0 of the center; `RunState.kills` +1.

R4 kill additions: elites drop x3 crystals (4.6); SHOCKWAVE: a kill by a hitter pushes every non-boss enemy within `shockRadius` 1.2 outward by `shockPush` 0.8; WILDFIRE: a burning enemy that dies sets every enemy within 1.2 burning; a Mine explodes (4.2); combo +1 (3.8); Herald kill opens the relic pick (6.7). Crystal pickups count for TIDE: every `tideEvery` 25 picked heal `tideHeal` 2 per stack.

### 4.6 Elites

- Eligible: non-boss enemies with base hp >= 2 (not Meteor or GoldComet). Each eligible unit spawned by the Director becomes elite with chance `era.eliteChance` (eras 1–4: 0; 5: 0.06; 6: 0.08; 7: 0.10; 8: 0.12). Endless adds `endlessEliteStep` 0.03 per loop from loop 1, total capped at `eliteMax` 0.3. The tutorial has none.
- `EliteAffix { Armored, Swift, Giant, Regen }`, uniform by simRng. Every elite: hp x`eliteHpMul` 1.5, crystals x`eliteCrystalMul` 3.
  - Armored: armor +`eliteArmor` 1.5.
  - Swift: speed x`eliteSpeedMul` 1.5.
  - Giant: hp x`giantHpMul` 2 more, radius x`giantRadiusMul` 1.35 (`SetSizeMul`), starDamage x1.5.
  - Regen: heals `eliteRegen` 8% of maxHp per second while not damaged for `eliteRegenDelay` 1.5 s.
- Look: every enemy prefab has an [R] `EliteMark` child (gold dashed ring, hidden by default) and an [R] `AffixIcon` child (SpriteRenderer; sprite from `ArtSet.affixIcons[affix]`). Spawn: gold spark burst and sound Elite. Kill: gold shards instead of cream.
- The first elite of a save shows a banner «Элита: сильнее, но даёт втрое больше кристаллов» (10.6).
- **Herald** (5): an elite with fixed affixes.

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
- **Herald.** In Run, Daily and Challenge at `heraldTime` 95 s, and in Endless at `95 + 160 * L`: one `era.herald` with affixes Giant and Armored (4.6), normal telegraph, banner «Вестник несёт реликвию», sound Elite. Killing it opens the relic pick (6.7); if it reaches the star, nothing. Not in the tutorial. `era.herald`: 1 Bastion, 2 Splitter, 3 Shielder, 4 Devourer, 5 Jelly, 6 Bumper, 7 Blinker, 8 Eclipser.

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

**Era 5, Nebula.** Opening: `2 Swarm x4 | 8 Jelly x1 | 14 Bastion x1`.
Main: `20 Swarm x8 | 28 Jelly x2 OppositePair | 36 Bastion x1 + Splitter x2 Opposite | 46 Arrow x2 Spread90 + Jelly x1 | 56 Swarm x10 + Torch x2 OppositePair | 62 Arrow x2 Spread90 + Shielder x1 | 70 Parasite x1 + Jelly x2 Opposite | 80 Bastion x1 + Swarm x8 Opposite | 88 Hive x1 + Jelly x2 Spread90 | 98 Shooter x1 + Orbiter x2 OppositePair | 108 Arrow x3 Even + Splitter x2 | 116 Hive x2 OppositePair | 126 Swarm x11 + Jelly x3 Even | 136 Bastion x2 OppositePair + Devourer x1 | 146 Hive x1 + Arrow x2 Spread90 + Phantom x2 | 156 Parasite x2 Spread90 + Jelly x3 Even | 164 Shooter x2 OppositePair + Bastion x1 | 172 Arrow x4 Even + Jelly x2 OppositePair + Swarm x10`

**Era 6, Gas Giant.** Opening: `2 Swarm x4 | 8 Mine x2 OppositePair | 14 Bumper x1`.
Main: `20 Swarm x8 | 28 Mine x3 Even | 36 Bastion x1 + Bumper x1 Opposite | 46 Arrow x2 Spread90 + Mine x2 | 56 Swarm x10 + Bumper x2 OppositePair | 62 Arrow x2 Spread90 + Jelly x1 | 70 Parasite x1 + Mine x3 Even | 80 Bastion x1 + Swarm x8 Opposite + Bumper x1 | 88 Orbiter x3 Even | 98 Shooter x1 + Mine x2 Opposite | 108 Arrow x3 Even + Bumper x2 | 116 Hive x1 + Shielder x2 OppositePair | 126 Swarm x11 + Mine x4 Even | 136 Bastion x2 OppositePair + Bumper x2 OppositePair | 146 Hive x1 + Arrow x2 Spread90 + Jelly x2 | 156 Parasite x2 Spread90 + Mine x3 Even | 164 Shooter x2 OppositePair + Bastion x1 + Bumper x2 | 172 Arrow x4 Even + Mine x4 Even + Swarm x10`

**Era 7, Anomaly.** Opening: `2 Swarm x4 | 8 Blinker x2 OppositePair | 14 Bastion x1`.
Main: `20 Swarm x8 | 28 Blinker x3 Even | 36 Bastion x1 + Orbiter x2 Opposite | 46 Arrow x2 Spread90 + Blinker x2 | 56 Swarm x10 + Mine x2 OppositePair | 62 Arrow x2 Spread90 + Shielder x1 + Blinker x1 | 70 Parasite x1 + Bumper x2 Opposite | 80 Bastion x1 + Swarm x8 Opposite | 88 Blinker x4 Even | 98 Shooter x1 + Phantom x3 Even | 108 Arrow x3 Even + Blinker x2 | 116 Hive x1 + Devourer x1 Opposite | 126 Swarm x11 + Blinker x3 Even | 136 Bastion x2 OppositePair + Jelly x2 OppositePair | 146 Hive x1 + Arrow x2 Spread90 + Blinker x2 | 156 Parasite x2 Spread90 + Orbiter x3 Even | 164 Shooter x2 OppositePair + Bastion x1 + Blinker x2 | 172 Arrow x4 Even + Blinker x4 Even + Swarm x10`

**Era 8, Heart of Darkness.** Opening: `2 Swarm x4 | 8 Phantom x2 OppositePair | 14 Eclipser x1`.
Main: `20 Swarm x8 | 28 Phantom x3 Even | 36 Bastion x1 + Eclipser x1 Opposite | 46 Arrow x2 Spread90 + Blinker x2 | 56 Swarm x10 + Jelly x2 OppositePair | 62 Arrow x2 Spread90 + Mine x2 | 70 Parasite x1 + Eclipser x1 Opposite | 80 Bastion x1 + Swarm x8 Opposite + Bumper x1 | 88 Phantom x4 Even | 98 Shooter x1 + Devourer x1 Opposite | 108 Arrow x3 Even + Eclipser x1 | 116 Hive x1 + Blinker x2 Opposite | 126 Swarm x11 + Phantom x3 Even | 136 Bastion x2 OppositePair + Eclipser x1 | 146 Hive x1 + Arrow x2 Spread90 + Orbiter x2 | 156 Parasite x2 Spread90 + Phantom x4 Even | 164 Shooter x2 OppositePair + Eclipser x2 OppositePair | 172 Arrow x4 Even + Phantom x6 Even + Swarm x10`

**Tutorial opening** (era 1 only; hints in 10.7):

| t, s | event | lesson |
| --- | --- | --- |
| 0.5 | Swarm x3, spreadDeg 40, radius 5.4, speedMul 0.15 | only a held satellite reaches them |
| 8 | Swarm x6, speedMul 0.6 | release to defend near the star |
| 16 | Bastion x1 | armor ring (it pulses, 3.6) |

### 5.2 Events

`RunEventSystem` (Systems, ticks after EraRule). `RunEventType { MeteorShower, GoldenComet, Storm, CrystalTide, Invasion, Eclipse }`.

- Run, Daily, Challenge: at each of `eventTimes` [70, 130] s, with chance `eventChance` 0.6 (spawnRng), one event starts, picked uniformly among those not played this run (Eclipse is never picked in era 8). Endless: the same times + 160 x L. Never in the tutorial, never after bossTime in Run.
- Start: banner with the event name and text (2.5 s), sound EventStart. The HUD timer label shows the event name while it lasts.

| event | name (RU) | text (RU) | effect |
| --- | --- | --- | --- |
| MeteorShower | Метеорный дождь | «Сбивай метеоры — в них кристаллы» | `meteorCount` 10 Meteors (4.2), one every 0.5 s, all entering within +-40 deg of one random angle |
| GoldenComet | Золотая комета | «Успей сбить — 40 кристаллов» | one GoldComet crosses the field in `goldCometTime` 7 s |
| Storm | Солнечная буря | «Удар ×1.5, нагрев ×1.5» | 8 s: power x`stormPowerMul` 1.5, heat x`stormHeatMul` 1.5; `StormFx` scene object (gold rays over the background) shown |
| CrystalTide | Кристальный прилив | «Кристаллов вдвое больше» | 10 s: kills drop x2 crystals |
| Invasion | Нашествие | «Рой со всех сторон» | `invasionCount` 30 Swarmers Even, speedMul 0.7, with telegraph |
| Eclipse | Затмение | «Видно только у орбиты» | 8 s: the Darkness rule (7.3) with lightBase `eclipseLight` 2.5 |

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
- `CardDef { CardId id; CardColor color; CardRarity rarity; int era; int maxStacks; StatMod plus; StatMod minus; }`. `CardColor { Orbit, Strike, Star, Evolution }` (cream, red, black, gold frames). `CardRarity { Common, Rare }`. `era` 0 = every era.
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

### 6.3 Era cards (21: 9 for eras 2–4, 12 for eras 5–8)

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
| CONDENSATE | КОНДЕНСАТ | 5 | Strike | Common | 1 | Condensate 1 | HeatMul 1.1 | 2 с после выхода из облака: удар ×1.5 | Нагрев ×1.1 |
| SMOOTH\_HULL | ГЛАДКИЙ КОРПУС | 5 | Orbit | Common | 1 | Smooth 1 | PowerMul 0.95 | Студень не замедляет спутник | Удар ×0.95 |
| CLOUD\_SEED | ЗАСЕВ | 5 | Star | Rare | 1 | CloudSeed 1 | SpinMul 0.92 | Убийство в облаке: +2 кристалла | Вращение ×0.92 |
| GRAV\_ASSIST | ГРАВИМАНЁВР | 6 | Orbit | Common | 1 | GravAssist 1 | HeatMul 1.1 | Манёвр у луны: удар ×2 на 2.5 с | Нагрев ×1.1 |
| SAPPER | САПЁР | 6 | Strike | Common | 1 | Sapper 1 | SpinMul 0.95 | Мины не греют спутник, взрыв ×2 | Вращение ×0.95 |
| TIDAL | ПРИЛИВНЫЕ СИЛЫ | 6 | Star | Rare | 1 | Tidal 1 | CrystalLifeMul 0.8 | Враги ближе 1.2 к луне теряют 2 HP/с | Кристаллы живут ×0.8 |
| STABILIZER | СТАБИЛИЗАТОР | 7 | Orbit | Common | 1 | Stabilizer 1 | PowerMul 0.95 | Аномалии не сгибают орбиту | Удар ×0.95 |
| LOCK | ФИКСАТОР | 7 | Strike | Common | 1 | Lock 1 | SpinMul 0.95 | Раненый Прыгун больше не прыгает | Вращение ×0.95 |
| SINGULARITY | СИНГУЛЯРНОСТЬ | 7 | Star | Rare | 1 | Singularity 1 | StarMaxHp -10 | Враги в колодце теряют 2 HP/с | Макс. HP звезды −10 |
| LANTERN | ФОНАРЬ | 8 | Orbit | Common | 2 | Lantern 1.5 | SpinMul 0.95 | Свет вокруг орбиты +1.5 | Вращение ×0.95 |
| NIGHT\_SIGHT | НОЧНОЕ ЗРЕНИЕ | 8 | Star | Common | 1 | NightSight 1 | XpPerCrystalMul 0.9 | Во тьме видны силуэты врагов | Опыт за кристалл ×0.9 |
| LAST\_LIGHT | ПОСЛЕДНИЙ СВЕТ | 8 | Strike | Rare | 1 | LastLight 1 | StarMaxHp -10 | HP звезды ниже 30%: удар ×1.4 | Макс. HP звезды −10 |

- SOLAR\_SAIL window: from a flare's burst start to `solarSailAfter` 3 s after it ends.
- SLINGSHOT window: `slingshotTime` 1.0 s after the hold ends (the last holder releases). RadialOutMul multiplies the radial speed only while baseR grows.
- CONDENSATE window: `condensateTime` 2 s after the satellite leaves a cloud (7.3). CLOUD\_SEED counts kills whose position is inside a cloud. SAPPER doubles `mineDamage`. TIDAL and SINGULARITY damage is like burn (no number, no flash). LANTERN adds to the light radius; NIGHT\_SIGHT draws the darkness at alpha `nightSightAlpha` 0.65.

`CardId` order: the 21 cards of 6.2, the 9 era cards of eras 2–4 in 6.3, then (R4) the 6 cards of 6.5, the 12 era cards of eras 5–8 in 6.3, the 8 evolutions of 6.6 — 56 in all, in table order. `ArtSet.cardIcons` follows it.

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

### 6.5 New cards for every era (6)

| id | name | colour | rarity | max | plus (stat value) | minus (stat value) | plus text | minus text |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| SHOCKWAVE | УДАРНАЯ ВОЛНА | Strike | Common | 1 | Shockwave 1 | PowerMul 0.95 | Убитый враг отбрасывает соседей | Удар ×0.95 |
| PRISM | ПРИЗМА | Strike | Rare | 1 | Prism 1 | HeatMul 1.1 | Крит пробивает броню и щит | Нагрев ×1.1 |
| SOLAR\_WIND | СОЛНЕЧНЫЙ ВЕТЕР | Star | Common | 1 | SolarWind 1 | XpPerCrystalMul 0.9 | Враги у звезды вдвое медленнее | Опыт за кристалл ×0.9 |
| TIDE | ПРИЛИВ | Star | Common | 2 | TideHeal 1 | CrystalLifeMul 0.85 | Каждые 25 кристаллов лечат 2 HP | Кристаллы живут ×0.85 |
| INERTIA | ИНЕРЦИЯ | Orbit | Common | 2 | Inertia 0.3 | SpinMul 0.92 | Пока спутник летит вверх или вниз: удар +30% | Вращение ×0.92 |
| OVERLOAD | ПЕРЕГРУЗКА | Orbit | Rare | 1 | Overload 1 | HeatMul 1.1 | Оглушённый спутник взрывается: 3 урона рядом | Нагрев ×1.1 |

- SOLAR\_WIND: non-boss enemies within `starRadius + solarWindRange` 0.8 move x0.5.
- OVERLOAD: when a satellite is stunned, a blast at it (radius `overloadRadius` 1.5, damage `overloadDamage` 3, Swarmers die), using the chain blast.
- `StatId` appends: Shockwave, Prism, SolarWind, TideHeal, Inertia, Overload, Condensate, Smooth, CloudSeed, GravAssist, Sapper, Tidal, Stabilizer, Lock, Singularity, Lantern, NightSight, LastLight, String, Wildfire, Prominence, Hammer, Harvest, Guillotine, LivingStar, Reactor, ThirdHit, CritMulBonus, Aegis, AutoStasis, ComboWindow, LastBreath, Thorns, Feather.

### 6.6 Evolutions (8)

An evolution is a card (`CardColor.Evolution`, gold frame `card_frame_gold`, rarity Rare, max 1, no minus) that needs two source cards at max stacks.

- Offer: once both sources are maxed and the evolution is not taken, the next card screen always shows it in the first slot (the rest are drawn as usual). After that it stays in the pool with weight `weightEvolution` 6.
- Effect: the sources keep working; the evolution adds its own effect.
- Look on the card screen: under the title, the two source icons with a «+» between them (`EvoSources` child of CardView). On pick, the two small icons slide into the main icon over `evoMergeTime` 0.35 s, the card flashes white, the main icon punches, sound Evolution.
- Look in the HUD: the two source `HudCardIcon`s fly to the evolution's slot over 0.35 s, fade into it, and are replaced by one icon with a gold rim. From then on the HUD shows the evolution instead of its sources.

| id | name | sources | effect | text (RU) |
| --- | --- | --- | --- | --- |
| STRING | СТРУНА | SAW\_THREAD + SPIN | sawDamage x3, sawInterval 0.15 | «Нить режет втрое сильнее и чаще» |
| WILDFIRE | ПОЖАР | BURN + CHAIN | burnDps 2; a burning enemy's death sets enemies within 1.2 burning | «Огонь перекидывается с убитых на соседей» |
| PROMINENCE | ПРОТУБЕРАНЕЦ | BEACON + CORONA | beacon radius 3.5, damage 3, each wave heals 2 | «Волна маяка шире, сильнее и лечит звезду» |
| HAMMER | МОЛОТ | HEAVY + SHARP | crit x3, each crit lowers armor by 0.5 | «Крит ×3 и сбивает броню» |
| HARVEST | ЖАТВА | MAGNET + GREED | every `harvestInterval` 8 s all crystals fly to the nearest hitter | «Каждые 8 с все кристаллы летят к вам» |
| GUILLOTINE | ГИЛЬОТИНА | EXECUTE + PIERCE | executeFraction 0.4 | «Враг с HP ≤ 40% гибнет от удара» |
| LIVING\_STAR | ЖИВАЯ ЗВЕЗДА | STAR\_PLATING + PHOTOSPHERE | regen x2, max HP +40 | «Лечение вдвое и +40 к макс. HP» |
| REACTOR | РЕАКТОР | OVERLOAD + COOLANT | overload radius 2.5, damage 6, stunTime x0.5 | «Взрыв при оглушении больше, оглушение короче» |

### 6.7 Relics

A relic is a strong passive for the rest of the run. Relics come only from the Herald (5) and, in Endless, from every boss kill (9.2).

- Pick: the state becomes `CardPick` with `relicChoices` 3 `CardView`s in the relic look (frame `ArtSet.relicFrame`, one effect line, stack pips), sound Relic. No rerolls. Relics at max stacks are never offered, so the same relic does not come twice unless it stacks. If none is left: «+50 пыли» banner and dust instead.
- Relics are per run, apply through `Stats` like cards and are shown in the HUD as a row of `HudRelicIcon`s (icon and stack number) above the card icons.
- `RelicDef { RelicId id; int maxStacks; StatMod[] mods; }`, list `relics` in GameConfig; `ArtSet.relicIcons` by RelicId. `RunState.relicStacks[]`.

| id | name | max | mods | text (RU) |
| --- | --- | --- | --- | --- |
| CometShard | ОСКОЛОК КОМЕТЫ | 1 | ThirdHit 2 | «Каждый 3-й удар спутника: +2 урона» |
| Lens | ЛИНЗА | 1 | CritChance 0.08, CritMulBonus 0.5 | «Крит +8% и бьёт ×2.5» |
| Heartwood | СЕРДЦЕВИНА | 3 | StarMaxHp 20 | «Макс. HP звезды +20 и лечение 20» |
| Compass | КОМПАС | 1 | CrystalLifeMul 2, MagnetRadius 0.5 | «Кристаллы живут вдвое дольше, притяжение +0.5» |
| Chronometer | ХРОНОМЕТР | 2 | AbilityCooldownMul 0.85 | «Перезарядка способностей ×0.85» |
| Aegis | ЩИТ ПРЕДКОВ | 1 | Aegis 1 | «Раз в 20 с удар по звезде гасится» |
| Hourglass | ПЕСОЧНЫЕ ЧАСЫ | 1 | AutoStasis 1 | «Каждые 30 с враги замирают на 2 с» |
| GravLens | ГРАВИЛИНЗА | 2 | SatRadiusMul 1.15 | «Спутник крупнее ×1.15» |
| LifeCrystal | КРИСТАЛЛ ЖИЗНИ | 3 | TideHeal 1 | «Каждые 25 кристаллов лечат 2 HP» |
| IceCore | ЛЕДЯНОЕ ЯДРО | 2 | HeatMul 0.8 | «Нагрев ×0.8» |
| Nozzle | СОПЛО | 2 | RadialSpeedMul 1.2 | «Выход и падение ×1.2» |
| Trophy | ТРОФЕЙ | 1 | DustMul 1.2 | «Пыль за забег ×1.2» |
| Laurel | ЛАВРОВЫЙ ВЕНОК | 1 | ComboWindow 1 | «Комбо держится на 1 с дольше» |
| LastBreath | ПОСЛЕДНИЙ ВЗДОХ | 1 | LastBreath 1 | «Когда HP впервые ниже 25%: мощная волна» |
| Thorns | ШИПЫ | 2 | Thorns 1 | «Враги у звезды теряют 1 HP/с» |
| Feather | ПЕРО ЖАР-ПТИЦЫ | 1 | Feather 1 | «Убийство горячим спутником: +2 кристалла» |

- ThirdHit: every 3rd damaging hit of each satellite adds the stat value. Aegis: the first star damage after `aegisInterval` 20 s of no blocking is cancelled (white flash on the star). AutoStasis: every 30 s, enemy time x`stasisSlow` for 2 s (StasisFx shown). LastBreath: once per run when HP first drops below 25%, a PULSE wave with radius 4.0 and damage 3. Thorns: enemies within `starRadius + 0.6` lose 1 HP/s per stack (like burn). Feather: kill by a hitter with heat >= 0.7 drops 2 more crystals. Heartwood heals like STAR\_PLATING.

## 7. Eras

### 7.1 What an era gives the player

An era is a star system with its own identity. Each one adds: one rule that changes how orbits play (7.3), new enemies (4.2), its own boss (4.3), three cards that exist only there (6.3), more dust per crystal, a separate best time, its music (12.2) and a first-victory reward. A first victory in an era opens the next one. Era 1 has no rule. The era screen (10.2) shows all of this before the run.

### 7.2 EraDef (list `eras` in GameConfig, index 0..7)

`EraDef { kExp, spinMul, hpMul, countMul, ambientInterval, dustMul, riseMul, fallMul, EraRule rule, EnemyType boss, EnemyType herald, float eliteChance, MusicId music, SpawnEvent[] opening, SpawnEvent[] timeline, int victoryDust, int firstWinDust, Color background, Color starTint, Color raysTint }`. Names, rule lines and boss names live in `Strings`. Optional art per era lives in ArtSet: `eraIcons[]` (menu), `eraBackdrops[]` (a big dim picture behind the rings; empty = none), `eraStarSprites[]` (replaces the star Body sprite in that era; empty = keep the skin). All three have 8 entries.

| # | name (RU) | kExp | spinMul | hpMul | countMul | ambient s | dustMul | rise / fall | rule | boss | victory dust | first win |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | СОЛНЦЕ | 0.9 | 1.0 | 1.0 | 1.0 | 3.5 | 1.0 | 1 / 1 | None | Comet (Boss) | 100 | 200 dust, opens era 2 and Endless |
| 2 | КРАСНЫЙ КАРЛИК | 0.9 | 1.15 | 1.4 | 1.2 | 3.3 | 1.25 | 1 / 1 | Flares | Twin | 150 | 400 dust, opens era 3, skin «Луна» |
| 3 | ТЯЖЁЛАЯ ЗВЕЗДА | 1.2 | 1.0 | 1.8 | 1.35 | 3.1 | 1.5 | 0.8 / 1.3 | Heavy | Colossus | 200 | 600 dust, opens era 4, star skin «Мотор» |
| 4 | ЧЁРНАЯ ДЫРА | 1.0 | 1.1 | 2.3 | 1.5 | 3.0 | 1.8 | 1 / 1 | Vortex | Pulsar | 250 | 800 dust, skin «Глаз» |
| 5 | ТУМАННОСТЬ | 1.0 | 1.1 | 2.6 | 1.55 | 2.9 | 2.1 | 1 / 1 | Nebula | Sower | 300 | 1000 dust, opens era 6 |
| 6 | ГАЗОВЫЙ ГИГАНТ | 0.95 | 1.1 | 2.9 | 1.6 | 2.8 | 2.4 | 1 / 1 | Moons | Mirror | 350 | 1200 dust, opens era 7, class Neutron |
| 7 | АНОМАЛИЯ | 1.0 | 1.15 | 3.2 | 1.65 | 2.7 | 2.7 | 1 / 1 | Anomalies | Graviton | 400 | 1400 dust, opens era 8 |
| 8 | СЕРДЦЕ ТЬМЫ | 1.0 | 1.15 | 3.5 | 1.7 | 2.6 | 3.0 | 1 / 1 | Darkness | Abyss | 500 | 2000 dust, the ending (10.10) |

Colours: background #0E0E0E, #160B0A, #0B0E14, #050505; starTint (multiplies the star Body colour) white, #FF9A80, #C9D8FF, #262626; raysTint (multiplies the Rays colour) white, #FF9A80, #C9D8FF, white (the black hole keeps a cream rim of rays). The camera background takes the era colour at run start.

Rule lines (`Strings.EraRule`): 1 «Спокойная звезда. Здесь учатся держать орбиту.»; 2 «Вспышки: красный сектор греет спутники и жжёт врагов.»; 3 «Тяжесть: спутники взлетают медленнее, падают быстрее, кристаллы тонут в звезде.»; 4 «Воронка: враги летят к центру по спирали.»

Unlocks: era n+1 opens with the first victory in era n in Run mode (Daily and Challenges do not open eras). Endless opens with the first victory in era 1. The first victory in an era also pays `firstWinDust` and gives its skin (8.5).

R4 also adds to era 2 the class RedGiant and to era 4 the class WhiteDwarf as first-win rewards (8.6). Music: eras 1–2 `Chapter1`, 3–4 `Chapter2`, 5–6 `Chapter3`, 7–8 `Chapter4`.

Colours of eras 5–8: background #0D0A12, #100C07, #06100E, #000000; starTint #E8D2FF, #FFD9A0, #C8FFF0, white; raysTint #E8D2FF, #FFD9A0, #C8FFF0, #D6281F.

Rule lines 5–8: 5 «Туманность: облака остужают спутник, а враги в них вязнут.»; 6 «Луны: их притяжение гнёт орбиту, пролёт рядом даёт удар ×1.5.»; 7 «Аномалии: колодцы тянут орбиту, отталкиватели толкают.»; 8 «Тьма: видно только то, что рядом с орбитой.»

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

R4 appends `Nebula, Moons, Anomalies, Darkness` to `EraRule`. Their objects are scene objects under World (11.3); all of them move with plain dt.

**Nebula (era 5).**

- Three `NebulaCloud`s (radius `nebulaRadius` 1.3) circle the star at radii [2.2, 3.6, 5.0] with angular speeds [+8, -6, +5] deg/s, start angles 0, 120, 240 deg plus one simRng offset.
- A satellite inside a cloud (`dist <= nebulaRadius`) cools at `nebulaCool` 0.6 per second instead of heating. An enemy inside moves x`nebulaSlow` 0.6.

**Moons (era 6).**

- Two `Moon`s (radius `moonSize` 0.5) orbit at [3.0, 5.0] with periods [20, 32] s, the first counter-clockwise, the second clockwise.
- Each moon is a well for satellites (3.2) with R `moonWellRadius` 1.4 and B `moonBend` 0.4.
- Enemies within `moonPullRadius` 1.8 of a moon also move toward it at `moonPull` 0.8 u/s, so they bunch up around it. Moons do not collide with anything.
- Gravity assist: a satellite passing within `moonAssistRadius` 0.9 of a moon's centre gets its assist timer = `moonAssistTime` 1.5 s (GRAV\_ASSIST: x2 and 2.5 s), once per moon per 1.5 s; gold flash on the satellite, sound GravAssist.

**Anomalies (era 7).**

- From `anomalyFirst` 10 s, every `anomalyInterval` 9 s, an anomaly opens at radius uniform [2.2, 5.2] and a random angle (simRng); at most 2 open at once. Type: Well or Repulsor, 50/50.
- Warning `anomalyWarn` 1.0 s (prefab fades in, no effect yet, sound AnomalyOpen), then active for `anomalyLife` 7 s, then fades out over 0.5 s.
- Active: a gravity source for satellites (3.2) with R `anomalyRadius` 2.0 and B `anomalyBend` 0.7; enemies within R move toward (well) or away from (repulsor) its centre at `anomalyPull` 1.2 u/s; a well also pulls crystals within R at 2 u/s.
- Pooled prefabs `AnomalyWell` and `AnomalyRepulsor` from ArtSet.

**Darkness (era 8; also the Eclipse event).**

- Light radius `L = max(lightMin 1.8, max(lightBase 2.6, outermost satellite radius + lightAround 1.2) + stats.lantern - sum of Eclipser lightCut - (Abyss phase 2 ? 1.0 : 0))`. L changes smoothly (`lightLerp` 4 per second).
- The `DarknessMask` scene object (a black sprite with a soft transparent hole, sorting 45) is scaled so that its hole radius equals L. Everything below sorting 45 outside L is hidden: enemies, crystals, the backdrop. Satellites, telegraphs (sorting 47), FX and numbers stay visible. NIGHT\_SIGHT draws the mask at alpha 0.65.
- Holding shows more of the field: the trade-off of distance now also decides what the player sees.

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
- **R4, `version = 3`.** New fields: `int starClassesOwned` (bit per StarClassId; fresh save: Yellow), `int lastStarClass`, `long achievements` (bit per AchievementId), `long seenCards` (bit per CardId, set when a card is first offered), `int seenRelics`, `int evolutionsTaken`, `int classesWon` (bit per class with any victory), `int bestCombo`, `int eliteKills, heraldKills, goldComets, dailyPlayed, endlessBossBest`, `bool introSeen, endingSeen`. `erasUnlocked` goes up to 8; `bestTimes` and `bestEndless` have 8 entries. `SettingsData` gains `musicVolume` 0.7, `vibration` true, `language` (-1 = from the platform, 0 Ru, 1 En).
- **Migration v2 → v3:** resize the era arrays to 8 (new entries 0); `starClassesOwned` = Yellow + RedGiant if era 2 is won + WhiteDwarf if era 4 is won; `introSeen = tutorialDone` (an old player does not get the intro forced; it can be watched from Settings); the rest take their defaults.

### 8.2 Dust

- Run: `round(crystals picked x era.dustMul x stats.dustMul) + (victory ? era.victoryDust : 0) + (first victory in this era ? era.firstWinDust : 0)`.
- Daily: as Run; the first Daily run of each UTC date doubles it (`dailyDustMul` 2).
- Endless: crystals as in Run, plus `endlessBossDust` 60 x era.dustMul for every boss killed. No victory dust.
- Challenge: as Run, plus the challenge reward on its first completion (9.4).
- The existing "x2 dust (ad)" button stays.
- The result screen shows the parts: crystals x era, victory, first victory, reward, total.

Rough economy (balance check with the R2 simulator): a run gives about 200 crystals in era 1 and 250 in era 4, so with eras' dustMul and victory dust about 230, 360, 470 and 600 dust per run in eras 1 to 4. Everything bought in 8.3 to 8.5 costs about 17,400; with first wins and challenges a player owns it all after about 40 to 50 runs. Full blueprints are worth roughly one skill level: in the simulator a weak bot's win rate in era 1 goes from about 20% to almost 100%, and later eras stay hard.

R4 economy: eras 5–8 pay roughly 750, 900, 1000 and 1150 dust per run. New sinks: the Cepheid class (1500). New income: achievements (about 6,500 in all, 8.7).

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

### 8.6 Star classes

The class is picked before every run (10.2) and works in every mode; the tutorial is always Yellow. `StarClassDef { StarClassId id; float starRadius; int maxHp; float rMin; StatMod[] mods; StarTrait trait; Color tint; int price; int eraWin; }`, list `starClasses` in GameConfig; `ArtSet.starClasses[]` holds per class an `icon` (sprite for tiles) and an `aura` (decor prefab instantiated as a child of Star at run start). The class tint multiplies the star Body colour together with the era tint. Star skins (8.5) still choose the sprites.

| id | name | radius | max HP | rMin | mods | trait | how to get |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Yellow | ЖЁЛТЫЙ КАРЛИК | 1.0 | 120 | 1.35 | — | None | owned |
| RedGiant | КРАСНЫЙ ГИГАНТ | 1.35 | 190 | 1.7 | SpinMul 0.9, StarDamageMul 0.85, Thorns 1 | None (Thorns is its mantle) | first victory in era 2 |
| WhiteDwarf | БЕЛЫЙ КАРЛИК | 0.7 | 80 | 1.0 | SpinMul 1.2, PowerMul 1.15, HeatMul 1.25 | Glint: crit chance +`glintCrit` 0.1 for a satellite with heat >= 0.5 | first victory in era 4 |
| Neutron | НЕЙТРОННАЯ ЗВЕЗДА | 0.8 | 110 | 1.2 | XpNeedMul 1.1 | AutoPulse: every `autoPulseInterval` 10 s a wave from the star to radius 2.4: Swarmers and Arrows die, others take 1.5, no push | first victory in era 6 |
| Cepheid | ЦЕФЕИДА | 1.0 | 130 | 1.35 | — | Breath: a 12 s cycle of 6 s Bright (star radius x1.25 over 0.5 s, regen +1.0/s, power x0.9) and 6 s Dim (power x1.3); the HUD level badge shows ☀ / ☾ | 1500 dust |

Tints: white, #FF8A6A, #DDE8FF, #B9BDC2, #FFE7A0.

Texts (RU, `Strings.ClassText`): ЖЁЛТЫЙ КАРЛИК «Обычная звезда. Ничего лишнего.»; КРАСНЫЙ ГИГАНТ «Большая и крепкая. Враги у поверхности горят, спутник медленнее.»; БЕЛЫЙ КАРЛИК «Маленькая и хрупкая. Спутник быстрый и злой, но греется сильнее.»; НЕЙТРОННАЯ ЗВЕЗДА «Каждые 10 с сама бьёт волной. Опыта нужно больше.»; ЦЕФЕИДА «Дышит: 6 с светит и лечит, 6 с тускнеет и бьёт сильнее.»

### 8.7 Achievements

`AchievementDef { AchievementId id; AchKind kind; int target; int dust; }`, list `achievements` in GameConfig, icons `ArtSet.achievementIcons[]`. `Achievements.Check()` runs at Result (run counters + save totals) and after every Workshop purchase. A new one: bit set, dust paid at once, toast (10.9), sound Achievement.

| # | name (RU) | condition | dust |
| --- | --- | --- | --- |
| 1–8 | ПЕРВЫЙ СВЕТ, КРАСНЫЙ РАССВЕТ, ПОД ТЯЖЕСТЬЮ, ГОРИЗОНТ, СКВОЗЬ ТУМАН, ЛУННЫЙ ТАНЕЦ, ИЗЛОМ, ПОСЛЕДНИЙ РАССВЕТ | win era 1 … 8 (any mode) | 50, 80, 110, 140, 170, 200, 230, 400 |
| 9–11 | ОХОТНИК, ИСТРЕБИТЕЛЬ, ГРОЗА ТЬМЫ | totalKills 500 / 5000 / 25000 | 50, 150, 400 |
| 12–14 | СЕРИЯ, ЛАВИНА, ЦЕПЬ МОЛНИЙ | bestCombo 25 / 50 / 100 | 50, 120, 300 |
| 15–16 | ЭВОЛЮЦИЯ, ДВЕ ЭВОЛЮЦИИ | evolutions in one run 1 / 2 | 60, 200 |
| 17 | ВСЕ ЭВОЛЮЦИИ | evolutionsTaken has all 8 bits | 400 |
| 18–19 | ХРАНИТЕЛЬ, РЕЛИКВАРИЙ | relics in one run 3 / 5 | 80, 250 |
| 20 | ПЕРЕХВАТ | heraldKills 10 | 100 |
| 21 | ОХОТА НА ЭЛИТУ | eliteKills 100 | 150 |
| 22–25 | ГИГАНТ, КАРЛИК, НЕЙТРОН, ЦЕФЕИДА | a victory with that class | 100 each |
| 26 | НЕПРИКОСНОВЕННЫЙ | a victory with no star damage in the run | 300 |
| 27 | ТИХАЯ ПОБЕДА | a victory without pressing an ability | 150 |
| 28 | ОДИН В ПОЛЕ | a victory with one satellite all run | 150 |
| 29 | ЧЕТВЁРКА | 4 satellites at once | 80 |
| 30 | ВЕЧНОСТЬ | 10:00 in Endless | 200 |
| 31 | ЦИКЛ БОССОВ | 5 bosses in one Endless run | 250 |
| 32 | ИСПЫТАННЫЙ | all 8 challenges | 300 |
| 33 | ПОСТОЯНСТВО | dailyPlayed 7 | 150 |
| 34 | ИНЖЕНЕР | every blueprint at max | 300 |
| 35 | АТЛАС ЗАПОЛНЕН | every enemy and boss seen | 200 |
| 36 | ЗОЛОТОИСКАТЕЛЬ | goldComets 5 | 100 |

Descriptions (`Strings.AchText`) are the condition in plain words, for example «Победи в эре 3», «Набери комбо 50».

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
- R4: after every boss kill a relic pick (6.7); the Herald every loop (5); events (5.2); elites (4.6).

### 9.3 Daily

- Opens after the tutorial. Always era 1. Seed = UTC date as yyyyMMdd.
- One modifier per day: `dailyMods[daysSinceEpoch % 6]` = DoubleArrows, FastEnemies, DoubleHeat, SwarmOnly, GlassStar, HighOrbit.
- The first finished Daily of the date doubles its dust. Best score in `bestDaily`.
- Every finished Daily adds 1 to `dailyPlayed` (8.7).

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
| Story | intro or ending slides (10.10); simulation stopped |

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

**R4 menu changes:**

- Main gains АТЛАС and ДОСТИЖЕНИЯ (hidden while `tutorialDone` is false, like the others). The first ИГРАТЬ with `introSeen` false plays the intro (10.10), then the tutorial run.
- Eras: eight `EraCard`s (landscape: four visible, scroll; portrait: swipe). Between the cards and СТАРТ, a row of five `ClassTile`s (class icon, name; locked: dark with «Победа в эре {n}» or «{price} пыли»). Tapping a tile selects it (outline) and shows its text (8.6) in one line under the row. The last class is preselected; Retry keeps it.
- Workshop gains a 4th tab ЗВЁЗДЫ: five `ClassTile`s with the full text, stats («HP {maxHp} · радиус {r}») and «Есть» / «Купить · {price}» / «Победа в эре {n}».

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
float musicVolume = 0.7    // Музыка, slider
bool vibration = true      // Вибрация (phones only; hidden on desktop)
int language = -1          // Язык: Русский / English (-1 = from the platform until the player picks)
```

Also: button «Пройти обучение снова» (confirm; sets tutorialDone = false), button «Сбросить прогресс» (confirm «Сбросить всё? Пыль, чертежи, эры, способности и облик пропадут.»; fresh save, settings kept), label «Версия {Application.version}», button «Закрыть».

Also R4: buttons «Смотреть вступление» and «Смотреть финал» (the second only when `endingSeen`).

### 10.5 HUD

Layout as before: portrait — bars top centre, dust top left, timer and pause top right, card icons bottom left, ability buttons bottom right, hint bottom centre; landscape — bars top left, dust, timer and pause top right, hint in the left column; the top band of 1 unit is free only where |x| > 3.4 units.

`BarView` (one component for every bar) has layers Back, Ghost, Fill, Frame and an optional Text; `Set(value, max)`:

- **Star HP:** on damage the Fill drops at once; the Ghost (light cream) holds for `barGhostDelay` 0.35 s, then shrinks to the Fill in `barGhostTime` 0.4 s (Ease.OutQuad); the bar punches 6 px for 0.15 s and the Frame flashes red for 0.15 s. On heal the Ghost (gold) jumps to the new value and the Fill grows to it in barGhostTime. Text «{ceil hp} / {max}». Below 30% the Fill's alpha pulses between 0.7 and 1 at 2 Hz.
- **XP:** the Fill tweens to the new value in `xpFillTime` 0.25 s. On a level-up it fills to 1, flashes gold for 0.2 s, then restarts from 0 with the carried XP. A level badge left of it shows the level number.
- **Boss:** like Star HP, with the boss name above it (Twins: the sum).
- Under the timer: the modifier name (Daily, Challenge) or «Цикл {L + 1}» (Endless).
- Ability buttons show cooldown seconds (6.4).

R4 HUD:

- `ComboView` under the bars (portrait) or under the timer (landscape): «×{combo}» with a punch on every kill, the tier line «КОМБО ×{mul}» in gold from tier 1, a thin bar showing comboTimer. Hidden below comboShowMin; on a break it shakes and fades in 0.3 s.
- Relic row (`HudRelicIcon` prefab: icon, stack number) above the card icons.
- While an event lasts, the label under the timer shows its name.

All tweens SetLink to their object; numbers in GameConfig.

### 10.6 Banners

`BannerView`, top centre under the bars: a line of text and an optional icon; fade in 0.2 s, hold, fade out 0.3 s; one at a time, the rest wait in a queue.

- Boss arrival: the boss name, 2 s.
- First meeting of an enemy type (Run, Endless, Challenge; not the tutorial; not Swarmer or Bolt): its tip from `Strings.EnemyTip`, 3 s; the type is added to `seenEnemies`.
- Eras 2 to 4: the era rule line at run start, 3 s.
- Endless boss kill: «+{N} пыли», 2 s.

Enemy tips: Стрела «Стрела: быстрая и больно бьёт»; Бастион «Бастион: в броне — бей у звезды или в резонансе»; Паразит «Паразит: присасывается к звезде — сбей его у самой звезды»; Стрелок «Стрелок: стоит вдали и стреляет»; Улей «Улей: у звезды лопается роем»; Дробитель «Дробитель: после гибели распадается на рой»; Факел «Факел: греет спутник при каждом ударе»; Кружащий «Кружащий: кружит навстречу спутникам»; Щитник «Щитник: щит держит удар с одной стороны. Бей с другой или жми РЕВЕРС»; Пожиратель «Пожиратель: ест кристаллы и растёт»; Призрак «Призрак: когда прозрачный, его не задеть».

- Herald spawn: «Вестник несёт реликвию», 2.5 s. First elite of the save: 4.6. Events: 5.2. Abyss phases: the phase name, 2 s. No relic left: «+50 пыли», 2 s.

New enemy tips: Студень «Студень: вязкий — спутник после удара замедляется»; Мина «Мина: висит на орбите. Взорвётся от удара и нагреет спутник»; Отбойник «Отбойник: удар по нему подбрасывает орбиту»; Прыгун «Прыгун: перескакивает вбок — бей, пока он мигает»; Затмитель «Затмитель: в броне, и пока он жив, тьма гуще».

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

All UI strings live in the static `Strings` class (Russian and English, 10.11). A rewarded button's text names the reward and says it is an ad.

### 10.9 Atlas and achievements

**Atlas** (`AtlasView`, panel of MenuView, button АТЛАС). Four tabs:

- ОСНОВЫ: nine pages, «‹ ›» arrows and swipe; each page = picture (`ArtSet.atlasPictures[i]`), title, text.

| # | title | text (RU) |
| --- | --- | --- |
| 1 | УДЕРЖАНИЕ | Держи палец (или пробел) — спутник уходит от звезды. Отпусти — он падает обратно. Больше ничего нажимать не нужно. |
| 2 | СИЛА УДАРА | У звезды спутник быстрый и бьёт сильно. На дальней орбите он медленный и слабее, зато достаёт врагов, пока они далеко. |
| 3 | НАГРЕВ | У звезды спутник греется: красная дуга вокруг него — это жар. Полная дуга — оглушение на секунду. Дальние орбиты остужают. |
| 4 | БРОНЯ | Серебряное кольцо — граница брони. Внутри него удар пробивает броню, снаружи даёт «0». |
| 5 | РЕЗОНАНС | Два спутника рядом соединяются золотой нитью и бьют вдвое сильнее. Новый спутник даёт карточка НОВЫЙ СПУТНИК. |
| 6 | КАРТОЧКИ | Кристаллы дают опыт. Полная полоска — выбор карточки. У каждой карточки есть плюс и минус: выбирай под свою игру. |
| 7 | ЭВОЛЮЦИИ | Две карточки на максимуме сливаются в эволюцию — она сильнее обеих. Рецепты — во вкладке КАРТОЧКИ. |
| 8 | СПОСОБНОСТИ | Две кнопки внизу справа. Первую выбираешь перед забегом, вторую дают на 3-м уровне. После нажатия идёт перезарядка. |
| 9 | КОМБО И РЕЛИКВИИ | Убивай без пауз — растёт комбо, и кристаллы дороже. Вестник в середине забега несёт реликвию — сильный бонус до конца забега. |

- БЕСТИАРИЙ: a grid of 26 `AtlasEntry` tiles (18 enemies, then 8 bosses; not event objects). A seen type (`seenEnemies`; bosses are marked seen when they spawn) shows its icon and name; an unseen one a black silhouette of its icon and «???». Tapping a seen tile shows: big icon, name, «HP {hp} · скорость {speed} · броня {armor}» (base values), its tip, «Эра {first era}». Boss tips (bestiary only): Комета «Падает к звезде по эллипсу — бей у перигея»; Близнецы «Ныряют по очереди; оставшийся злее»; Колосс «В броне, на каждой четверти HP сбрасывает рой»; Пульсар «Стреляет и бьёт волной — держи спутник дальше от кольца»; Сеятель «Роняет Ульи — лови их на дальней орбите»; Зеркало «Стражи отталкивают спутник, а само оно разворачивает орбиту»; Гравитон «Тянет орбиту к себе, потом отталкивает»; Тьма «Три фазы. В последней сама идёт к звезде».
- КАРТОЧКИ: all 56 cards as small `CardView`s grouped by colour, then the 16 relics. Never offered (`seenCards`, `seenRelics`): card back with «?». Evolutions always show their recipe (the two source icons), even before they are seen.
- СПОСОБНОСТИ: seven rows (pictogram, name, effect, duration, cooldown), as in the Workshop without buying.

**Achievements** (`AchievementsView`, button ДОСТИЖЕНИЯ): «{done} / 36» and a scroll list of `AchievementRow`s: icon, name, condition, reward, progress «12 / 25» for counters, gold mark when done (not done: icon dimmed).

**Toast** (`AchievementToast`, top centre, above everything, also in the HUD): icon, «Достижение: {name}», «+{dust} пыли»; slides in 0.25 s, holds 2.5 s, out 0.25 s; queue.

### 10.10 Story

`StoryView` (Canvas, full screen, above the menu): `Background` Image (slide picture; cross-fade 0.5 s; slow zoom 1.0 → 1.06 over the slide), a dark `Panel` along the bottom with `Speaker` («РАССКАЗЧИК»), `Text` (typed with `maxVisibleCharacters` at `storyCharsPerSec` 40; sound StoryType every 3 characters), `NextHint` («Нажми, чтобы продолжить», blinking) and `SkipButton` («Пропустить», top right). Tap while typing = show the whole line; tap after = next slide. Music `Story`. Text Animator is not used here.

- **Intro** (`ArtSet.storyIntro[6]`): before the first run, once (`introSeen`); then the tutorial starts.
- **Ending** (`ArtSet.storyEnding[6]`): after the first victory in era 8, between Victory and Result (`endingSeen`). The last slide adds «Бесконечный режим ждёт» under the text.

| # | intro (RU) | ending (RU) |
| --- | --- | --- |
| 1 | Когда-то небо было полно звёзд. Их было так много, что ночь казалась светлой. | Сердце Тьмы треснуло. Впервые за тысячу лет в пустоте стало тихо. |
| 2 | Потом пришла Тьма. Она не горела и не светила. Она просто гасила — звезду за звездой. | Звезда не погасла. Она вспыхнула так ярко, что свет дошёл до самых дальних орбит. |
| 3 | Тьма посылала своих слуг: рои, стрелы, бастионы. Каждый летел к свету, чтобы его потушить. | Из её пламени родились новые звёзды. Сначала одна, потом десять, потом целое небо. |
| 4 | Осталась одна звезда. Маленькая, упрямая, на самом краю неба. | А спутник всё кружил. Ему некуда было спешить. |
| 5 | У неё был один спутник. Он не умел стрелять. Он умел только кружить — и бить всё, что встанет на пути. | Тьма ещё вернётся. Но теперь у неба есть кому светить. |
| 6 | Держи — и он уйдёт к дальним орбитам. Отпусти — и упадёт к звезде. Так началась оборона Перигея. | КОНЕЦ. Спасибо, что дошли до рассвета. |

### 10.11 Localization

- `Loc.Lang { Ru, En }`. Every text in `Strings` exists in both languages; the API of `Strings` stays the same and returns the current language. Static scene labels carry a `LocText` component (a key into Strings); code-set texts are re-applied when `Loc.Changed` fires. Switching in Settings changes every visible text at once.
- First launch: `IPlatformService.Language()` (stub: browser language, 2 letters); ru, be, kk, uk, uz → Ru, anything else → En. A choice in Settings overrides it.
- One static TMP font asset (no dynamic fallback) with Latin, Cyrillic, digits and every symbol used (× ● ○ « » — − ≤ ☀ ☾ ★ ‹ › …). Text that does not fit in English uses TMP auto-size within the same rect.
- Glossary: ПЕРИГЕЙ = PERIGEE; звезда = star; спутник = satellite; кристалл = crystal; пыль = stardust; чертёж = blueprint; эра = era; эволюция = evolution; реликвия = relic; Вестник = Herald; Мастерская = Workshop; Атлас = Atlas; комбо = combo; резонанс = resonance; нагрев = heat; броня = armor; способность = ability; Бесконечный = Endless; Испытание дня = Daily; испытания = Challenges. Enemy, boss, card, relic, class and achievement names get short English names in the same spirit (КРАСНЫЙ ГИГАНТ = RED GIANT, НИТЬ-ПИЛА = SAW THREAD).

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
- **Labels (R4).** Every array in ArtSet and AudioBank is a list of small serializable entries whose first field is a read-only `label` string, so the Inspector shows «Bastion — Бастион» instead of «Element 3». `OnValidate` sets each label from what the array follows (EnemyType, CardId with the Russian name, AbilityId, BlueprintId, SkinId, RelicId, StarClassId, AchievementId, era «1 СОЛНЦЕ», atlas page title, story slide number, affix) and resizes the array to that length, keeping existing entries. Single fields have a `[Tooltip]` saying what they are and where they appear, grouped with `[Header]` (Игрок, Враги, Карточки, Меню, Эры, Сюжет). The migration to labelled entries keeps every existing reference (M16).
- **R4 entries:** `enemies[]` 28 (by EnemyType), `cardIcons[]` 56, `cardFrames[]` 4 (by CardColor, Evolution = `card_frame_gold`), `relicFrame`, `relicIcons[]` 16, `affixIcons[]` 4, `starClasses[]` 5 (`icon`, `aura`), `achievementIcons[]` 36, `atlasPictures[]` 9, `storyIntro[]` 6, `storyEnding[]` 6, `eraIcons[]`, `eraBackdrops[]`, `eraStarSprites[]` 8 each, prefabs `anomalyWell`, `anomalyRepulsor`. An empty sprite entry shows a shared placeholder (`icon_card`, `icon_relic`, `icon_achievement`, `story_bg`).

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

Sorting order: see the R4 list below.

R4 placeholders (made through the MCP when first needed, same import settings): `card_frame_gold`, `relic_frame` (black card with a silver border), `icon_relic`, `icon_achievement`, `affix_armored`, `affix_swift`, `affix_giant`, `affix_regen`, `cloud` (soft circle, alpha in the PNG), `moon` (cream circle with three darker craters), `anomaly_well` (gold spiral ring), `anomaly_repulsor` (silver ring with outward ticks), `darkness_hole` (1024 px black square with a soft transparent circle of half its size in the middle), `warp_lines` (radial streaks), `vignette_red` (transparent centre, red edges), `story_bg` (dark field with a few cream dots), `era_5`..`era_8`.

R4 objects:

| object | [R] required parts | [D] decor |
| --- | --- | --- |
| Jelly | Body: red circle r0.38 | cream wavy `rect` inside, `Pulse` |
| Mine | Body: red `star`-shaped spikes r0.30 | cream dot, `Spin` |
| Bumper | Body: red square r0.34 | silver bar across |
| Blinker | Body: red diamond r0.24 | two cream dashes beside it |
| Eclipser | Body: black circle r0.45 with red rim | silver outline (armor) |
| Sower | Body: red circle r0.9 | six cream dots in a ring, `Spin` |
| Mirror | Body: silver diamond r0.8; `Guard0`, `Guard1`: cream circles r0.25 | `Spin` on a thin ring |
| Graviton | Body: red ring r1.0 with gold core | `anomaly_well` behind it, `Spin` |
| Abyss | Body: black circle r1.3 with red ring | `Eye`: cream circle with black pupil, `Pulse` |
| Meteor | Body: silver hexagon r0.22 | `Trail` silver |
| GoldComet | Body: gold circle r0.4 | `Trail` gold 1 s |
| Every enemy prefab (R4 adds) | `EliteMark` (gold `ring_dashed_24`, hidden), `AffixIcon` (SpriteRenderer, hidden) | — |
| NebulaCloud x3 (scene) | Body: `cloud`, violet-cream, alpha 0.35, scaled to nebulaRadius | `Spin` slow |
| Moon x2 (scene) | Body: `moon` r0.5 | none |
| AnomalyWell, AnomalyRepulsor (prefabs) | Body: `anomaly_well` / `anomaly_repulsor` scaled to anomalyRadius, alpha 0.4 | `Spin` (well inward, repulsor outward) |
| DarknessMask (scene) | Body: `darkness_hole`, black, sorting 45 | none |
| StormFx (scene) | gold thin `rect` rays over the field, alpha 0.25 | `PulseAlpha` |
| WarpFx, LevelUpRing, StarHitRing (scene) | `warp_lines` / gold `ring_thin` / red `ring_thin` | none |
| Class auras (prefabs) | none (decor only) | Yellow: none; RedGiant: wide red `ring_thin` pulsing; WhiteDwarf: small silver spikes `Spin`; Neutron: two cream `rect` beams `Spin` 120; Cepheid: gold `ring_dashed_40` `Pulse` |

Sorting order R4: backdrop -10, bars 0, rings 10, flare and clouds 15, crystals 20, moons and anomalies 25, enemies 30, star 40, darkness 45, telegraphs 47, satellites 50, FX 60, damage numbers 70.

### 11.4 Procedural geometry

Code fills in the points of these objects; colour, width, material and sorting stay in the Inspector. Point arrays are preallocated and written with `SetPositions`.

- **ResonanceThread** (prefab, pooled): gold LineRenderer zigzag between two satellites; `TMP_Text` label with the multiplier.
- **HeatArc** (child of every satellite skin): red arc around the satellite, length = heat x 360 deg.
- **HpRing** (child of every enemy and boss prefab): cream LineRenderer arc of up to 40 points around the enemy at radius `radius + 0.12`, length = shown HP fraction x 360 deg, starting at the top and going clockwise; a darker full circle behind it is decor.
- **BossOrbitLine** (two scene objects, the second for Twins): red dashed loop of 64 points on the next ellipse, alpha 0.3, shown for 1.5 s.

## 12. FX and audio

### 12.1 Effects

Base (R3, unchanged):

- **Fx:** `HitStop(sec)` and `Shake(amp, dur)` use real-time timers. Shake does nothing when Settings.shake is off.
  - Hit-stop 0.04 s only on kills of enemies with base hp >= 2, boss hits and star damage; 0.02 s on clinks. A new hit-stop starts only if `hitStopMinInterval` 0.25 s has passed since the last one.
  - Star damage = `Flash(starHitFlashColor, 0.15 s)` on the star + shake (amp 0.12, dur 0.12). A new shake starts only if `shakeMinInterval` 0.2 s has passed.
  - Kill = 6 cream shards flying outward for 0.4 s (x2 for big enemies, 3.7). Clink = 4 silver sparks + a Blocked number. Crystal pickup = small pop + punch-scale of the dust counter.
  - Shards, sparks and damage numbers are pooled prefabs from ArtSet, moved by `Fx`, not ParticleSystem. When the number pool is empty the oldest number is reused.

Existing effects stay (hit-stop, shake, shards, sparks, numbers, 3.7). R4 adds (all numbers in GameConfig, all tweens SetLink, real-time timers):

- **Slow motion.** `GameLoop.fxTimeScale` multiplies the simulation dt; slow-mo effects set it for a real-time duration and restore 1.
- **Boss arrival:** the camera moves 30% toward the boss and orthoSize x`bossZoom` 0.85 over 0.4 s, fxTimeScale `bossIntroSlow` 0.3 for 0.8 s, `BossIntro` UI (big boss name, one-line subtitle = its bestiary tip) for 1.6 s, then the camera returns in 0.4 s.
- **Boss kill:** fxTimeScale 0.25 for 0.8 s, `ScreenFlash` (full-screen white Image) alpha 0.6 → 0 in 0.4 s, 30 shards, shake 0.3 for 0.4 s, sound BossKill.
- **Boss phase** (Colossus thresholds, first Twin death, Abyss phases): ScreenFlash 0.3, shake 0.15, sound BossPhase.
- **Run start:** `WarpFx` scales 0.2 → 1.6 and fades over 0.8 s; the star punches 0.15; satellites fade in over 0.6 s.
- **Level-up:** `LevelUpRing` grows from the star to radius 6.5 in 0.5 s, sound LevelUp, then the card screen.
- **Card screen:** cards come in from below one after another (stagger 0.07 s, 0.3 s, Ease.OutBack) with a flip (scale x 0 → 1); Rare rims pulse; Evolution cards have a `Shine` stripe crossing them every 1.5 s. The picked card punches and flies toward the HUD card icons shrinking (0.35 s); the others drop and fade (0.25 s).
- **Abilities:** on press, ScreenFlash in `abilityColors[id]` at alpha 0.15 and the button punches 1.15; when one becomes ready, a punch and sound AbilityReady.
- **Star hit:** the star punches 0.08 for 0.15 s and `StarHitRing` grows from the star by 1.2 in 0.3 s, fading.
- **Low HP:** `DangerVignette` (UI, `vignette_red`) pulses alpha 0 ↔ 0.35 at 1.5 Hz below 30% HP, 0 ↔ 0.55 below 15%.
- **Directional hits:** hit shards fly in a 70 deg cone along hitter → enemy.
- **Crystals to the counter:** each pickup sends a `CrystalFly` UI icon along an arc to the dust counter in 0.45 s (at most 10 alive; extra pickups skip it); the counter punches when one lands.
- **Heat:** the satellite trail colour moves toward red with heat; at heat >= 0.8 the Body jitters +-0.02; a stun releases 6 silver sparks (steam) and sound Stun.
- **Resonance:** 8 gold sparks at the middle of a new thread.
- **Elites:** gold ring burst on spawn, gold shards x2 on kill. **Crit:** extra hit-stop 0.02 and a bigger number punch.
- **Combo:** ComboView punches on each kill; a new tier flashes the view gold and the screen edges gold (ScreenFlash in gold at 0.1).
- **Evolution:** 6.6 plus a gold ScreenFlash 0.25.
- **Menu:** buttons punch 0.92 → 1 with UiClick; Back plays UiBack; panels slide in 40 px with a 0.25 s fade; the dust counter counts up to a new value in 0.6 s; purchases play Buy and punch the bought item.
- **Result:** lines appear one by one 0.12 s apart, the dust total counts up, and a record shows a «НОВЫЙ РЕКОРД» stamp (scale 2 → 1, rotation -8 deg, 0.3 s).
- **Vibration** (Settings.vibration, `IPlatformService.Vibrate(ms)`): star hit 40 ms, stun 30 ms, elite kill 20 ms, boss kill 150 ms.

### 12.2 Audio

`AudioBank` (asset `Assets/Perigee/AudioBank.asset`) is the only place for sound files. The owner drags clips into labelled slots; an empty slot is silence.

- `SoundSlot { label; AudioClip[] clips; float volume = 1; float pitchJitter = 0.05; float minInterval = 0.03; }`, one per `SfxId`. A play picks a random clip of the slot, volume x Settings.sfxVolume, pitch 1 +- pitchJitter, and skips if the same id played less than minInterval ago. `AudioManager` keeps a pool of `maxVoices` 16 AudioSources under Systems/Sfx.
- `MusicSlot { label; AudioClip clip; float volume = 0.8; }`, one per `MusicId { Menu, Chapter1, Chapter2, Chapter3, Chapter4, Boss, FinalBoss, Story }`. `AudioManager.PlayMusic(id)` crossfades two looping sources over `musicFade` 1.0 s; the same id does not restart. Menu and Result: Menu; Playing: `era.music`; boss arrival: Boss (era 8: FinalBoss); Story: Story. Pause lowers music to x0.4. Volume x Settings.musicVolume.
- The drone stays (`drone` clip and volume in AudioBank).
- The old `Clips` array moves into the first 12 slots (M16).
- Import (owner): effects mono, Vorbis 50%, Decompress On Load; music Vorbis 40%, Streaming. All audio together about 8 MB or less.

`SfxId` (the label shown in AudioBank is «Id — описание»):

| id | when | id | when |
| --- | --- | --- | --- |
| Hit | удар спутника по врагу | ComboBreak | комбо от 10 и выше оборвалось |
| Clink | удар в броню или щит | Evolution | взята эволюция |
| Kill | враг убит | Relic | экран реликвий открылся |
| Crystal | кристалл подобран | EventStart | началось событие |
| Resonance | возник резонанс | Elite | появилась элита или Вестник |
| StarHit | урон по звезде | Mine | взрыв мины |
| Stun | спутник оглушён | Blink | прыжок Прыгуна |
| CardOpen | открылся экран карточек | GravAssist | гравиманёвр у луны |
| CardPick | карточка выбрана | AnomalyOpen | открылась аномалия |
| BossIn | пришёл босс | Achievement | получено достижение |
| Win | победа | Unlock | открыта эра, класс или облик (итоги) |
| Lose | поражение | Revive | возрождение за рекламу |
| UiClick | нажатие кнопки | SecondSun | сработало ВТОРОЕ СОЛНЦЕ |
| UiBack | кнопка «Назад» и закрытие | StoryType | печать текста сюжета |
| Buy | покупка в Мастерской | StoryNext | следующий слайд сюжета |
| LevelUp | новый уровень | Reverse | РЕВЕРС |
| AbilityReady | способность готова | Grow | РОСТ |
| Overheat | нагрев дошёл до 0.8 (раз до остывания ниже 0.5) | Pulse | ИМПУЛЬС, МАЯК, волна Нейтронной |
| BigKill | убит крупный враг | Stasis | СТАЗИС |
| ShieldBreak | щит Щитника сломан | Echo | ЭХО |
| FlareWarn | предупреждение вспышки | Pull | ПРИТЯЖЕНИЕ |
| FlareBurst | удар вспышки | Dome | КУПОЛ |
| BossKill | босс убит | ComboTier | новый уровень комбо |
| BossPhase | смена фазы босса | | |

`SfxId` order: Hit, Clink, Kill, Crystal, Resonance, StarHit, Stun, CardOpen, CardPick, BossIn, Win, Lose (existing), then UiClick, UiBack, Buy, LevelUp, AbilityReady, Reverse, Grow, Pulse, Stasis, Echo, Pull, Dome, Overheat, BigKill, ShieldBreak, FlareWarn, FlareBurst, BossKill, BossPhase, ComboTier, ComboBreak, Evolution, Relic, EventStart, Elite, Mine, Blink, GravAssist, AnomalyOpen, Achievement, Unlock, Revive, SecondSun, StoryType, StoryNext (47).

## 13. Services

Interfaces with stub implementations that complete instantly; no real SDK. Unchanged from R2.1 and M8:

- `IAdService`: ShowRewarded(Action\<bool>), ShowInterstitial(). `ISaveService`. `ILeaderboardService`: Submit(score). `IPlatformService`: GameReady(), GameplayStart(), GameplayStop().
- GameReady when the menu first becomes interactive; GameplayStart on entering Playing; GameplayStop on leaving it; focus or visibility lost → Playing becomes Paused, `AudioListener.pause = true`.
- Rewarded: Revive (once per run, on Defeat: HP 50%, enemies within 4.0 die, heat 0, 1.5 s immune), x2 dust on Result, New cards on the card screen (once per run). Interstitial on Retry or Menu from Result only if runs >= 3, the run lasted >= 30 s, >= 150 s since the last interstitial and >= 60 s since the last rewarded ad.
- R3 adds no ad placements. Submit receives the score of every finished run (Endless: seconds).
- `IPlatformService` gains `Language()` (2-letter browser language) and `Vibrate(int ms)`. In WebGL both go through a small `Platform.jslib` (`navigator.language`, `navigator.vibrate`); in the editor they return "ru" and do nothing.

## 14. Code structure (folder Assets/Perigee/: Scripts, Art, Prefabs, Scenes; namespace Perigee)

| group | files |
| --- | --- |
| Core | GameConfig, ArtSet, Visual, GameLoop (state machine, fixed step, hit-stop, EnemyTimeScale, StartFromMenu(era, mode, challenge)), RunState (hp, dust, float xp, level, time, card stacks, era, mode, modifiers, loop, kills), Stats, Rng, Strings |
| Gameplay | InputHold, StarController, SatelliteSystem (orbit, direction, heat, stun, resonance, saw thread, hitters and echoes), EnemySystem (behaviors, collisions, shields, burn, chain, star contact), Director (era timelines, Endless loops, modifiers), CrystalSystem, BossSystem (Comet, Twins, Colossus, Pulsar; replaces BossComet), EraRuleSystem, CardSystem, AbilitySystem, Blueprints (tree), Shop (abilities, skins), Progress (unlocks, rewards, dust) |
| Presentation | Spin, Pulse, PulseAlpha, Scroll, TrailReset (decor), RingHighlight, ArmorRing, ResonanceThread, HeatArc, HpRing, DamageStages, BossOrbitLine, FlareSector, Fx, AudioManager, Hud, BarView, BannerView, CardView, CardPickView, AbilityPickView, AbilityButtonView, PauseView, ReviveView, ResultView, MenuView, EraCard, ChallengeRow, BlueprintNode, ShopRow, SkinTile, SettingsView, ConfirmDialog, TutorialHints |
| Services | IAdService, ISaveService (SaveData, SettingsData), ILeaderboardService, IPlatformService, SaveService, stub implementations |
| R4 additions | Core: Loc, LocText. Gameplay: ComboSystem, RunEventSystem, RelicSystem, Achievements, star classes in StarController, gravity sources in EraRuleSystem (read by SatelliteSystem). Presentation: ComboView, HudRelicIcon, ClassTile, DarknessMask, ScreenFx (ScreenFlash, DangerVignette, WarpFx, LevelUpRing, StarHitRing, CrystalFly), BossIntro, AtlasView, AtlasEntry, AchievementsView, AchievementRow, AchievementToast, StoryView. AudioBank and AudioManager reworked (12.2) |

Performance: one pool per prefab, prewarmed at run start; at most 200 enemies; no per-frame allocations; no LINQ inside the loop; component references cached when an object is created, never inside a tick. Target: 60 fps on a mid-range phone browser with 150 enemies and numbers on.

## 15. Scene setup (one scene `game`)

- Main Camera (orthographic, URP 2D Renderer, background from the era)
- Systems: GameLoop, SatelliteSystem, EnemySystem, CrystalSystem, Director, BossSystem, EraRuleSystem, CardSystem, AbilitySystem, Fx, AudioManager (children Sfx and Drone), with references to `GameConfig` and `ArtSet`
- World: Star (Body, Rays), Satellites, ArmorRing, PulseRing, BeaconRing, ReverseFx, StasisFx, PullFx, DomeFx, FlareSector, BossOrbitLine, BossOrbitLine2, Background (Backdrop, Bars, Rings), Enemies, Boss, Telegraphs, Crystals, Fx
- Canvas: HoldZone (first child), Hud (bars as BarViews, Dust, Clock, ModeLabel, Cards, Abilities, Banner, hints), CardPickView, AbilityPickView, PauseView, ReviveView, ResultView, MenuView (Main, Eras, Challenges, Workshop, Settings panel host), SettingsView, ConfirmDialog
- EventSystem with InputSystemUIInputModule
- R4 scene additions — Systems: ComboSystem, RunEventSystem, RelicSystem. World: NebulaCloud x3, Moon x2, Anomalies (pool parent), DarknessMask, StormFx, WarpFx, LevelUpRing, StarHitRing. Canvas: ComboView, Relics row in Hud, ScreenFlash, DangerVignette, CrystalFly pool, BossIntro, MenuView panels Atlas and Achievements, Workshop tab Stars, ClassTiles on Eras, AchievementToast, StoryView (top).
- Project: `Assets/Perigee/GameConfig.asset`, `ArtSet.asset`, `AudioBank.asset`; URP asset with the 2D Renderer; build target WebGL; only `game.unity` in Build Settings.

## 16. Milestones

Сделано (не переделывать без строки ниже): M1–M8 и M5A по R2.1, M9–M15 по R3 (PROGRESS.md)разнесён: шрифт ушёл в M16, сборка и замер в M25. Не делай сборку вообще, пока не дойдёшь до M25

Новые этапы R4. Колонка «Системы» — чьи это системы: другой этап их только вызывает (правило 9).

| # | Что делаем | Системы | Читать | Готово, когда |
|---|---|---|---|---|
| M16 | Основа. AudioBank и AudioManager по 12.2 (слоты с подписями, 47 SfxId, 8 MusicId, перенос старых Clips, голоса, музыка с переходом) и вызовы Play для всех уже существующих действий; ползунок «Музыка». Подписи в ArtSet и AudioBank (11.2) с переносом ссылок. Языки 10.11: Loc, LocText, английский для всех существующих строк, выбор языка, статический шрифт. `Language()` и `Vibrate()` в IPlatformService (jslib), переключатель «Вибрация». SaveData v3 со всеми полями 8.1 и переносом. `StatId` дописан целиком (6.5) | AudioManager, AudioBank, ArtSet, Loc, Strings, SaveService, IPlatformService, SettingsView | 0, 8.1, 10.4, 10.11, 11.2, 12.2, 13 | В AudioBank у каждого слота подпись «Hit — удар спутника по врагу»; клип, перетащенный в слот, звучит в игре; меню, эры и босс играют свою музыку с плавным переходом; в ArtSet элементы подписаны (Bastion — Бастион, ТЯЖЁЛЫЙ СПУТНИК…), старые картинки на месте; English в настройках сразу переводит все тексты; после WebGL-сборки видны кириллица и латиница; сохранение владельца переносится в v3 без потерь |
| M17 | Карточки и комбо: 6 карточек 6.5, 8 эволюций 6.6 (появление, слияние на карточке и в HUD, золотая рамка), комбо 3.8 и ComboView, seenCards, поле `EnemySystem.Field` (см. заметки). Заодно вид реликвии в CardView и префаб HudRelicIcon (логика реликвий — M21) | CardSystem, CardView, CardPickView, Stats, ComboSystem, ComboView, Hud, EnemySystem (Field) | 3.5, 3.8, 4.5, 6.1, 6.5, 6.6, 6.7 (только вид), 10.3, 10.5 | Каждая новая карточка работает как написано; когда ТЯЖЁЛЫЙ и ОСТРЫЕ ГРАНИ на максимуме, следующий экран показывает МОЛОТ в золотой рамке с двумя значками-источниками; при выборе значки сливаются в один, а в HUD две иконки слетаются в одну; серия убийств показывает «×12» и «КОМБО ×1.2», кристаллы дают больше опыта и пыли, удар по звезде обрывает комбо |
| M18 | Враги, элита и 8 эр как данные: 5 врагов 4.2 и поведение Chord (Meteor, GoldComet — появятся в M21); элита 4.6 (префабы с EliteMark и AffixIcon); EraDef до 8 эр (7.2, расписания 5.1, herald, eliteChance, music), экран эр на 8 карточек; до M19 эры 5–8 без правила и с Кометой | EnemySystem, Director, EraDef, MenuView (эры), BannerView | 4.2, 4.6, 5, 7.1, 7.2, 10.2, 10.6, 11.3 | В эре 5 летят Студни (спутник после удара медленнее), в 6 — Мины (взрыв греет спутник) и Отбойники (орбита подскакивает), в 7 — Прыгуны, в 8 — Затмители; с эры 5 встречается элита с золотым кольцом и значком свойства, она крепче и даёт втрое больше кристаллов; меню показывает 8 эр |
| M19 | Правила эр 5–8 и боссы: облака, луны, аномалии, тьма (7.3), гравитационный сдвиг орбиты (3.2), 12 карточек эр 5–8 (6.3), Сеятель, Зеркало, Гравитон, Тьма с тремя фазами (4.3) | EraRuleSystem, SatelliteSystem (сдвиг), BossSystem, DarknessMask | 3.2, 3.5, 4.3, 6.3, 7.3, 11.3 | В Туманности облако остужает спутник, а враги в нём вязнут; у Газового гиганта орбита прогибается к луне, пролёт рядом даёт золотую вспышку и удар ×1.5; в Аномалии колодцы тянут орбиту, отталкиватели толкают; в Сердце Тьмы видно только у орбиты, удержание открывает больше; каждый босс ведёт себя по 4.3, Тьма меняет фазы с баннером |
| M20 | Классы звёзд: 8.6, плитки классов на экране эр, вкладка ЗВЁЗДЫ в Мастерской, черты (Шипы, Блеск, Автоимпульс, Дыхание), ауры, классы за первые победы в эрах 2, 4, 6, покупка Цефеиды | StarController, Shop, Progress, MenuView (эры, Мастерская), ClassTile | 4.1, 7.2, 8.6, 10.2 | Перед забегом можно выбрать класс; Красный гигант большой, крепкий и жжёт врагов у поверхности; Белый карлик маленький, быстрый и критует горячим; Нейтронная сама бьёт волной каждые 10 с; Цефеида заметно «дышит»; закрытые классы показывают условие; выбор сохраняется |
| M21 | Реликвии, Вестник, события: 6.7 (выбор, стаки, без повторов, HUD), Вестник в 95 с, реликвия после каждого босса в Бесконечном, события 5.2 (метеоры, золотая комета, буря, прилив, нашествие, затмение) | RelicSystem, RunEventSystem, Director (Вестник), Progress (Бесконечный) | 5, 5.2, 6.7, 9.2, 10.5, 10.6 | На 95-й секунде приходит Вестник с баннером; его смерть открывает выбор из 3 реликвий, взятая видна в HUD и работает; максимальные больше не предлагаются; в Бесконечном реликвия после каждого босса; на 70 или 130 с бывает событие с баннером, каждое работает по 5.2 |
| M22 | Эффекты: всё из 12.1 и вибрация | Fx, ScreenFx, Hud, CardPickView (анимации), MenuView (анимации), ResultView, BossIntro | 12.1, 13 | Каждый пункт 12.1 виден в игре: приход босса с приближением и замедлением, смерть босса со вспышкой, разгон при старте, кольцо уровня, вылет и переворот карточек, красная виньетка при малом HP, кристаллы летят к счётчику, итоги появляются по строке со штампом рекорда; на телефоне есть вибрация, выключатель её убирает |
| M23 | Атлас и достижения: 10.9 (Основы, Бестиарий, Карточки, Способности), 36 достижений 8.7, тост, счётчики сохранения | AtlasView, AchievementsView, Achievements, Progress | 8.7, 10.9 | В Атласе 9 страниц основ листаются, бестиарий показывает встреченных врагов и «???» для остальных, карточки показывают рецепты эволюций; достижение даёт тост и пыль один раз; список показывает прогресс «12 / 25» |
| M24 | Сюжет: StoryView, вступление перед первым забегом и финал после первой победы в эре 8, кнопки в настройках | StoryView, GameLoop (состояние Story) | 10.10 | На новом сохранении ИГРАТЬ показывает 6 слайдов с печатающимся текстом рассказчика, тап дописывает строку, потом следующий слайд, «Пропустить» сразу запускает обучение; финал идёт перед итогами после первой победы над Тьмой; оба можно пересмотреть из настроек |
| M25 | Релиз: проверить, что нет ссылок на удалённое; Product Name «Перигей»; в Build Settings только game.unity; убрать Pixelation из Renderer2D; английский на всех экранах (ничего не вылезает); WebGL-сборка; замер в эре 8 со 150 врагами и числами; список непроверенного руками и чисел баланса, которые выглядят неверно | всё (только проверка и правки) | 0, 2, 14, 15 | Обучение проходит от начала до конца на обоих языках; WebGL-сборка запускается; 60 кадров в редакторе на 150 врагах; отчёт со списком |

Notes for M16–M25:

- Each milestone writes its list entries into `GameConfig.asset`, `ArtSet.asset` and `AudioBank.asset` (rule 10) and makes the placeholders it needs (11.3).
- Every new string is written in Russian and English in the same milestone (rule 12); every new action calls its sound (rule 13).
- M16 adds all R4 `StatId`, `SfxId` and save fields at once, so later milestones do not reopen Stats, AudioManager or SaveService; they only use them.
- M17 builds the relic look of CardView and the HudRelicIcon prefab so that M21 does not reopen the card screen.
- M17 adds `EnemySystem.Field(centre, radius, pull, speedMul, dps)`: a per-step list of circular areas that move enemies toward (pull > 0) or away from (pull < 0) a centre, scale their speed and damage them like burn. SOLAR\_WIND uses it first; M19 (clouds, moons, anomalies, TIDAL, SINGULARITY), M20 (Red Giant) and M21 (Thorns) only add fields.
- M18 adds the Chord behavior that M21 spawns.
- M19 adds the 12 era cards as entries and their effects in the systems of that milestone; the card screen itself is not touched.
- Balance numbers for R4 content are first guesses; every one is in GameConfig.

## 17. Owner assets

The game runs with placeholders; the owner replaces them when ready (prompts in `Docs/ArtPack.md`):

- Sounds: one or more clips per `SfxId` slot (47) and 8 music tracks (12.2), dragged into `AudioBank.asset`.
- Pictures dragged into the labelled `ArtSet` slots or into prefab Bodies: 5 new enemies, 4 new bosses (+ Mirror guards), Meteor, GoldComet, 26 new card icons, gold card frame, relic frame and 16 relic icons, 4 affix icons, 5 class icons and auras, 36 achievement icons, 9 atlas pictures, 12 story slides (landscape 16:9 and safe in portrait: the important part in the middle third), era icons and backdrops 5–8, cloud, moon, anomalies, darkness mask.
- Optional: English names in Strings if the owner wants other wording.

## 18. Out of scope

Real Yandex SDK, new ad placements, a leaderboard screen, cloud save, analytics, languages other than Russian and English, satellite types (all satellites are one kind; skins are looks only), story beyond the intro and the ending, voice-over, unit tests, editor tooling, sprite atlases, animation clips (the owner may add Animators to prefabs as decor).
