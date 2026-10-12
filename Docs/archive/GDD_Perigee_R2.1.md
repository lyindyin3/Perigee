# PERIGEE — Game Design Document

**Reader:** Claude Opus 5.5 implementing the game in Unity. **Engine:** Unity 6.3, URP with the 2D Renderer, C#. **Target:** WebGL (Yandex Games), desktop and mobile browsers, landscape and portrait.
**Version:** R2.1. Merges the prototype playtest (old revision 1) and review R1, and makes all art swappable (section 9). There is one value per parameter. `Docs/archive/` is history: do not read it.

## 0. Rules for the implementing AI

1. Work milestone by milestone (section 13). After each one the project compiles and is playable. Report in 5 lines or fewer, then stop.
2. Implement only what this document says. No extra features, tests, README, logging, try/catch (one exception: SaveService, section 7), defensive null-checks, editor tools, or refactors of finished milestones. Serialized references are assumed assigned.
3. Every tunable number lives in `GameConfig` (ScriptableObject), or in a list inside it, with the default given here. No magic numbers in code. Systems read GameConfig values when they use them, not cached copies made in Awake, so a value changed in the Inspector during Play Mode applies at once.
4. If something is unspecified, take the simplest option consistent with this document and mention it in one line of the report. Ask only when blocked.
5. C# comments in Russian, only where logic is non-obvious. Namespace `Perigee`. Identifiers in English.
6. Unity MCP connected: create scene objects and settings through it. Not connected: write scripts only and give the manual steps from section 12 as a numbered checklist.
7. Use the Input System package (`UnityEngine.InputSystem`), never legacy `UnityEngine.Input`. Active Input Handling = Input System Package (New).
8. Art is data, not code (section 9). Everything visible is a prefab or a scene object whose sprites, colours, sizes and sorting are set in the Inspector, and the pictures that code needs are listed in `ArtSet`. Code moves, rotates, scales, tints, fades, enables and pools these objects and instantiates prefabs. It never builds sprites, textures or UI from scratch (`new GameObject`, `AddComponent`, `new Texture2D`).

## 1. Concept

One-finger arcade defense. A star sits at the center; 1 to 3 satellites orbit it. Enemies fly toward the star. A satellite damages whatever it touches.

- **Hold** the pointer: satellites move outward. **Release**: they fall inward.
- Close to the star = fast, strong, heats up. Far = slow, weak, cools down, reaches distant enemies.
- The star has HP. Leaked enemies hurt it. The player cannot kill everything; the skill is choosing what to kill.
- A run lasts 180 s and ends with a boss. Crystals from kills fill an XP bar; each fill opens a pick-1-of-3 card screen (every card has a plus and a minus). Two active abilities are used by tapping buttons.
- Defeat restarts instantly. Crystals become dust, spent on permanent blueprints.

## 2. World, units, camera

- 2D, 1 unit = 1 world unit. Star at (0,0). No Unity physics: no Rigidbody2D, no colliders. All motion and collisions are manual circle checks: `distSq <= (ra+rb)^2`. Collision radii are GameConfig values and do not depend on the sprites; the default art is drawn to match them (section 9.2).
- Camera: orthographic, black background. `orthoSize = H * max(1, 1/aspect)` with H = 6.2, recomputed when the screen size changes. A field of radius 6.2 stays fully visible in landscape and portrait.
- Enemies spawn on a circle of radius 7.0 (Arrows on 5.3). Along the long screen axis this circle is visible, so enemies fade in (section 5).

### 2.1 Default constants (all in GameConfig)

starRadius 1.0 | starMaxHp 120 | H 6.2 | spawnRadius 7.0 | arrowSpawnRadius 5.3 | rMin 1.35 | ringMax 6.2 | satMaxRadius 6.2 | ringGap 0.8 | satRadius 0.2 | maxSatellites 3 | radialSpeed 8 | tMin 1.0 | powerNear 3.0 | powerFar 1.0 | powerFarRadius 4.6 | powerFloor 1.0 | armorBase 2.0 | hitCooldown 0.2 | heatZoneMax 2.0 | coolZoneMin 2.6 | heatRate 0.35 | coolRate 0.25 | stunTime 1.0 | heatAfterStun 0.4 | resonanceHalfAngle 35 | resonanceMul 2.0 | crystalLife 10 | crystalRadius 0.14 | magnetSpeed 7 | spawnFadeIn 0.25 | telegraphRadius 6.6 | satFadeIn 0.3

Visual constants (also GameConfig): stunTint #B9BDC2 at alpha 0.6 | starHitFlashColor #D6281F | heatArcRadius 0.32 | prewarm: Swarmer 60, every other enemy type 8, crystal 60, shard 40, spark 20, each marker type 8, resonance thread 3.

kExp comes from the current era (section 7; 0.9 in era 1).

## 3. Simulation

### 3.1 Loop

`GameLoop` runs the simulation in `Update` with a fixed step of 1/60 s via an accumulator (`Time.deltaTime` clamped to 0.1, max 4 steps per frame). No steps run while the state is not `Playing` or while hit-stop is active (hit-stop is a real-time timer, section 10). Tick order each step: Satellites, Abilities, Enemies, Boss, Collisions, Crystals, Director, Star, Cards (XP check), Fx.

### 3.2 Satellite model

- `baseR` is the controlled radius. While holding, target = `ringMax - (n - 1) * ringGap` (n = satellite count), so the outermost satellite always reaches the outermost ring. Otherwise target = `rMin`. Each step: `baseR = MoveTowards(baseR, target, radialSpeed * stats.radialSpeedMul * dt)`. While ANCHOR is active, baseR does not change.
- Satellite `i` (0 = innermost) orbits clockwise (counter-clockwise never happens) at `r_i = min(baseR + i * ringGap, satMaxRadius)`.
- Angular speed in deg/s: `w(r) = (360 / tMin) * (rMin / r)^kExp * stats.spinMul`. With kExp 0.9 the period is 1.0 s at 1.35, 3.0 s at 4.6 and 3.9 s at 6.2.
- Start: one satellite at angle 90 deg, `baseR = rMin`. A new satellite becomes the outermost (highest i), spawns at the middle of the largest angular gap between existing satellites and fades in over satFadeIn (DOTween alpha). While fading it deals no damage, collects nothing and is ignored by resonance.
- `Power(r) = max(Lerp(powerNear, powerFar + stats.powerFarBonus, InverseLerp(rMin, powerFarRadius, r)) * stats.powerMul, powerFloor)`. InverseLerp is clamped, so Power stays at the far value beyond powerFarRadius. `stats.powerMul` is the total, including active ability multipliers.

### 3.3 Heat (per satellite)

- `r_i <= heatZoneMax`: heat += heatRate \* stats.heatMul \* dt. `r_i >= coolZoneMin`: heat -= coolRate \* dt. In between: unchanged. Clamp 0..1. With heatMul 1, a cold satellite at rest stuns after 2.9 s (1.7 s after a stun), and cooling from 1 to 0 takes 4 s.
- Heat reaches 1: the satellite is stunned for stunTime. It deals no damage, picks up nothing, keeps orbiting, and its heat stays at 1. After the stun heat = heatAfterStun.

### 3.4 Resonance

For any two non-stunned, non-fading satellites whose shortest angular distance is <= `resonanceHalfAngle * stats.resonanceAngleMul`, both deal damage x resonanceMul, and a `ResonanceThread` (9.4) is shown between them. The multiplier never stacks: a satellite in two pairs still deals x resonanceMul.

### 3.5 Hits

- A non-stunned, non-fading satellite overlaps an enemy and that enemy's hit cooldown for this satellite is 0: damage = `Power(r_i)` (x resonanceMul if in resonance). Set the cooldown to hitCooldown.
- Armored enemy (armor > 0): if damage < armor, no damage, silver sparks, 'clink' SFX. Otherwise normal damage. The resonance multiplier counts, so resonating satellites (Power x 2 >= 2) pierce armor at any radius. SURGE ignores armor.
- Normal hit: shards, SFX. Satellites are never damaged. Hit-stop rules are in section 10.
- Enemy HP <= 0: spawn crystals, remove, kill FX.

### 3.6 Armor ring

Scene object `ArmorRing` (9.3). It sits at the radius where the innermost satellite deals exactly armorBase: `t = (powerNear - armorBase / stats.powerMul) / (powerNear - (powerFar + stats.powerFarBonus))`, `r = rMin + clamp01(t) * (powerFarRadius - rMin)`. It is recomputed whenever the total powerMul or powerFarBonus changes, abilities included. If Power at powerFarRadius >= armorBase, the ring is hidden. During the first Bastion of the tutorial run it pulses until that Bastion dies.

## 4. Entities

### 4.1 Star

Radius 1.0, maxHp 120. When an enemy touches it (`dist <= starRadius + enemyRadius`), the enemy deals its starDamage and is removed (Parasite and Boss excepted). The boss grazes the star at periapsis by design. HP <= 0: Defeat.

### 4.2 Enemies (data-driven `EnemyDef` list in GameConfig)

| id | radius | hp | speed u/s | armor | starDamage | crystals | behavior |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Swarmer | 0.20 | 1 | 1.1 | 0 | 3 | 1 | Straight |
| Arrow | 0.15 | 1 | 2.6 | 0 | 9 | 2 | Straight, spawns at arrowSpawnRadius |
| Bolt | 0.15 | 1 | 3.5 | 0 | 4 | 0 | Straight |
| Bastion | 0.55 | 5 | 0.35 | 2.0 | 20 | 6 | Straight |
| Parasite | 0.25 | 5 | 1.6 | 0 | drain | 3 | Attach |
| Shooter | 0.30 | 2 | 1.0 | 0 | none | 4 | Stopper |
| Hive | 0.60 | 4 | 0.7 | 0 | none | 8 | Burster |
| Boss | 0.90 | 80 | n/a | 0 | 8 per pass | 30 | Ellipse |

Enemy hp is multiplied by `era.hpMul` only when its base hp >= `eraHpMinBaseHp` (default 2). Swarmers, Arrows and Bolts stay at 1.

`EnemyDef` also has `faceStar` (bool, 9.2) and uses the prefab `ArtSet.enemies[id]`.

Behaviors:

- **Straight:** moves toward the center at speed x the event's speedMul.
- **Attach (Parasite):** flies to the star. On contact (`dist <= starRadius + 0.25`) it snaps to center radius `starRadius + 0.15` on the contact angle, enables its `AttachMarker` child (9.3), and drains `min(1.5 + 0.4 * t, 5)` HP/s, where t = seconds attached. Only a satellite with `r_i <= 1.6` reaches it. If PULSE pushes it, it detaches, t resets and it flies back.
- **Stopper (Shooter):** moves to `shooterStopRadius` 5.8 and stops. It fires a Bolt at the star 2.0 s after stopping and then every `shooterFireInterval` 2.5 s. If pushed, it walks back to its stop radius. 5.8 is touched by the held outermost satellite for any satellite count.
- **Burster (Hive):** at `r <= hiveBurstRadius` 3.0 it dies and spawns `hiveSwarmers` 8 Swarmers at uniform positions in a disc of radius 0.5 around itself (simRng). There is no telegraph and no star damage. If killed earlier, it drops crystals and releases nothing.
- **Ellipse (Boss):** see 4.3.

### 4.3 Boss: Comet

- Spawns at 180 s at apoapsis and fades in over 0.5 s, during which it takes no damage. The Director stops spawning. Enemies already on the field stay.
- The star is a focus of its ellipse: periapsis 1.8, apoapsis 5.5, so `a = 3.65`, `e = 0.507`, `b = a * sqrt(1 - e^2)`.
- Timing (Kepler): `M = pi + 2 * pi * t / bossPeriod` (bossPeriod 6.0 s, t = seconds since spawn). Solve `E - e * sin(E) = M` with 4 Newton steps starting from E = M. Position = `Rot(phi) * (a * (cos(E) - e), b * sin(E))`. It is slow at apoapsis and fast in the dive.
- Initial phi comes from simRng. Each time M passes a multiple of 2 pi (periapsis), the boss deals 8 star damage once and `phi += 40 deg`.
- On spawn and after every phi change, the next ellipse is shown by the `BossOrbitLine` (9.4) for 1.5 s.
- HP 80 x era.hpMul. A HUD bar shows its HP. Boss HP <= 0: Victory.

### 4.4 Crystals

On enemy death, spawn `crystals` crystals at uniform positions within 0.3 of the death position (simRng). Lifetime `crystalLife * stats.crystalLifeMul`, blinking during the last 2 s. A non-stunned, non-fading satellite picks a crystal up when `dist <= satRadius * stats.satRadiusMul + crystalRadius`: dust +1, XP +1. Magnet: crystals within `stats.magnetRadius` of any satellite fly to the nearest one at magnetSpeed.

## 5. Director (spawn timeline)

- `Director` reads `SpawnEvent { time, groups[] }` from GameConfig. Each group is `{ type, count, angleMode, spreadDeg = 30, speedMul = 1, radius = 0 }`. radius 0 means the default (spawnRadius; arrowSpawnRadius for Arrows).
- Counts are final; they are multiplied only by `era.countMul` (Mathf.RoundToInt, min 1). All angles come from spawnRng.
- `angleMode`:
  - **Random:** a new random centre angle.
  - **Opposite:** centre = the previous group's centre in the same event + 180 deg. For an event's first group, the last angle of the previous event + 180 deg.
  - **Spread90:** unit k at centre + k * 90 deg.
  - **Even:** unit k at centre + k * 360 / n.
  - **OppositePair:** two units 180 deg apart.
- A Swarmer group with count > 1 spawns inside an arc of spreadDeg around its centre, with 0 to 1.0 radius jitter beyond the spawn radius.
- **Telegraph:** it starts before `time`, so spawns happen exactly at `time`. Lead time: Arrow 1.5 s, Swarmer group 1.0 s, everything else 0.8 s.
  - Normal marker: a `TelegraphArrow` (9.3) on the spawn ray at radius `min(telegraphRadius, visible rect inset 0.5)`.
  - Arrow: a `TelegraphWedge` on ring 5.3 at the spawn angle, plus a `TelegraphLine` from it to the star.
  - Ambient spawns and Hive children have no telegraph.
- New enemies fade in over spawnFadeIn and do not collide while fading.

Ambient: 1 Swarmer every 3.5 s from `ambientStart` 6 s to 175 s, at a random angle.

Opening (every run except the tutorial): `2 Swarm x4 | 8 Swarm x6 | 14 Bastion x1`.

Main timeline (seconds; "Swarm" = Swarmer group):

20 Swarm x8 | 30 Swarm x8 | 36 Bastion x1 | 46 Arrow x2 Spread90 | 56 Swarm x10 | 62 Arrow x2 Spread90 | 70 Parasite x1 | 80 Bastion x1 + Swarm x8 Opposite | 88 Parasite x2 Spread90 | 98 Shooter x1 | 108 Arrow x3 Even | 116 Hive x1 | 126 Swarm x11 + Shooter x1 Opposite | 136 Bastion x2 OppositePair | 146 Hive x1 + Arrow x2 Spread90 | 156 Parasite x2 Spread90 + Swarm x10 | 164 Shooter x2 OppositePair + Bastion x1 | 172 Arrow x4 Even + Swarm x11 | 180 Boss

Groups without a mode are Random.

Tutorial run (`tutorialDone = false`): the opening is replaced by the table below, ambient starts at 20 s, and the main timeline runs as normal from 20 s. Hints are in section 8.

| t, s | event | lesson |
| --- | --- | --- |
| 0.5 | Swarm x3, spreadDeg 40, radius 5.4, speedMul 0.15 | only a held satellite reaches them |
| 8 | Swarm x6, speedMul 0.6 | release to defend near the star |
| 16 | Bastion x1 | armor ring (it pulses, 3.6) |

## 6. Cards and abilities

### 6.1 Cards

- XP: level L (starts at 0) needs `8 + 3 * L` crystals. When the bar fills, the state becomes `CardPick` (simulation paused) and shows 3 distinct random cards from the eligible pool (cardRng). If fewer than 3 cards are eligible, show fewer.
- A card is eligible unless:
  - it is at max stacks;
  - it is RESONATOR and the player has fewer than 2 satellites;
  - it is ABILITY_LONG and the player owns no ability with duration > 0.
- While satellites < 3, the first card screen of the run always includes ADD\_SAT.
- The 3rd level-up of a run opens the ability pick (6.2) instead of cards.
- The chosen card gets stack +1. `Stats` is recomputed: base × cards × blueprints × active abilities; multipliers multiply, bonuses add.
- Surplus XP carries over. Several level-ups at once open their screens one after another.

| id | plus | minus | max stacks |
| --- | --- | --- | --- |
| HEAVY | powerMul x1.4 | radialSpeedMul x0.7 | 2 |
| SWIFT | radialSpeedMul x1.5 | heatMul x1.2 | 2 |
| ADD\_SAT | +1 satellite | heatMul x1.25 | 2 |
| MAGNET | magnetRadius +1.3 | Arrow speed x1.15 | 1 |
| COOLANT | heatMul x0.6 | powerMul x0.9 | 2 |
| STAR\_PLATING | star maxHp +25 and heal 25 | spinMul x0.9 | 2 |
| RESONATOR | resonanceAngleMul x1.5 | powerMul x0.9 | 1 |
| APOGEE | powerFarBonus +0.5 (far Power 1.0 → 1.5) | heatMul x1.15 | 2 |
| BIG\_SAT | satRadiusMul x1.5 | heatMul x1.2 | 1 |
| BEACON | every 8 s the star emits a shockwave: Swarmers within 2.5 die, other enemies within 2.5 take 1 damage | crystalLifeMul x0.6 | 1 |
| ABILITY\_HASTE | all ability cooldowns x0.75 | powerMul x0.9 | 2 |
| ABILITY\_LONG | all ability durations x1.4 | ability cooldowns x1.15 | 1 |

Cooldown multipliers from cards and blueprints multiply.

Each card and ability has an icon in `ArtSet` (`cardIcons[]` by `CardId`, `abilityIcons[]` by `AbilityId`). All 12 cards start with one shared placeholder `icon_card`; the owner assigns a picture per card.

### 6.2 Active abilities

- **Slots.** There are two.
  - Slot A: starting a run from Menu (Play or Daily) opens `AbilityPick` with all five abilities (name, pictogram, one-line effect, cooldown). Retry skips the screen and reuses the last slot-A choice. In the tutorial run, slot A is PULSE, assigned with no pick screen.
  - Slot B: the 3rd level-up of a run (tutorial included) opens a pick of 3 random abilities not yet owned (cardRng).

| id | source | effect | duration | cooldown |
| --- | --- | --- | --- | --- |
| ANCHOR | satellites | baseR freezes at its current value; hold and release are ignored; heat rules still apply | 4 s | 30 s |
| GROW | satellites | satRadiusMul x2 (the pickup distance grows with it), resonanceAngleMul x1.3 | 6 s | 35 s |
| PULSE | star | a shockwave grows from the star to radius 3.0 in 0.35 s. Swarmers and Arrows die (crystals drop). Other enemies take 2 damage and are pushed 1.0 outward. The boss takes 2 and is not pushed | instant | 45 s |
| CHILL | satellites | all heat = 0, stuns end, no heat gain | 5 s | 30 s |
| SURGE | satellites | powerMul x2, armor ignored | 4 s | 40 s |

- **Timing.** The cooldown starts at activation, not at the end of the duration. Pressing during an active effect or a cooldown does nothing. Timers tick inside the simulation step, so they stop in every state except Playing and during hit-stop. Abilities start ready. Revive does not change cooldowns.
- **Input and HUD.**
  - Two round buttons, 140 px with a 24 px margin, at the bottom right: slot B on the left, slot A on the right. Keys: Q = slot A, E = slot B.
  - Each button shows a cream radial fill for the cooldown and a pulsing gold ring when ready (DOTween).
  - Pictograms come from `ArtSet.abilityIcons`. Default art, cream on black, drawn once as PNGs in M5A: ANCHOR is a circle with a horizontal bar and a short vertical stem; GROW a growing circle; PULSE concentric rings; CHILL a cross with four ticks; SURGE three chevrons.
  - Buttons are UI above `HoldZone`, so a tap on a button never counts as hold.
- **Effects.**
  - ANCHOR: the `AnchorRing` scene object (silver `ring_dashed_40`) is enabled at the radius baseR while active.
  - GROW: smooth `SetSizeMul` change over 0.2 s.
  - PULSE: the `PulseRing` scene object (cream `circle_hollow`) expands to radius 3.0 in 0.35 s, plus shake (amp 0.15, 0.2 s).
  - CHILL: satellites `Flash` white for 0.2 s.
  - SURGE: the `SurgeTrail` child of each satellite (a gold TrailRenderer, off by default) is enabled for 4 s.
- **Implementation.** `AbilitySystem` holds the slots, cooldowns and active effects and ticks right after Satellites. Active effects apply as temporary multipliers in `Stats`, separate from card multipliers, and expire by timer.

## 7. Meta, modes, services

### 7.1 Save

- JSON in PlayerPrefs key `perigee_save` through `ISaveService`. Fields: dust, blueprint levels, bestTime, bestDaily, tutorialDone, erasUnlocked, runs, defeats, lastAbilityA, dailyBonusDate.
- Call `PlayerPrefs.Save()` after every write.
- SaveService may wrap the JSON parse in one try/catch; on failure it starts a fresh save.
- `tutorialDone` becomes true when the first run ends.

### 7.2 Dust

Crystals picked this run, +50 on victory. The first Daily run of each UTC date doubles that run's dust. The dust is added on the result screen.

### 7.3 Blueprints (menu screen)

| id | effect per level | levels | costs |
| --- | --- | --- | --- |
| HULL | star maxHp +10 | 5 | 50, 100, 200, 400, 800 |
| CAPACITOR | ability cooldowns x0.95 per level | 5 | 50, 100, 200, 400, 800 |
| COOLANT\_BP | heatMul x0.95 per level | 5 | 50, 100, 200, 400, 800 |
| MAGNETISM | magnetRadius +0.35 per level | 3 | 40, 80, 160 |

### 7.4 Eras

`EraConfig` list, data only, no unique art. Era n+1 unlocks when the boss dies in era n.

| era | kExp | spinMul | hpMul | countMul |
| --- | --- | --- | --- | --- |
| 1 Sun | 0.9 | 1.0 | 1.0 | 1.0 |
| 2 Red Dwarf | 0.9 | 1.3 | 1.3 | 1.2 |
| 3 Heavy Star | 1.2 | 1.0 | 1.6 | 1.4 |

### 7.5 Modes and randomness

- **Normal:** random seed, chosen era.
- **Daily:** seed = UTC date as yyyyMMdd, always era 1, best score in bestDaily.
- Gameplay randomness uses three `Rng` instances (System.Random): `spawnRng` (Director, seed), `cardRng` (card and ability offers, seed + 1) and `simRng` (crystal scatter, Hive burst, boss phi, seed + 2). Visual randomness uses UnityEngine.Random.

### 7.6 Score

Dust picked + 2 \* total run seconds (boss fight included) + (victory ? 500 : 0). The best survived time is stored.

### 7.7 Services

Interfaces with stub implementations that complete instantly. No real SDK in v1.

- `IAdService`: ShowRewarded(Action\<bool>), ShowInterstitial().
- `ISaveService`.
- `ILeaderboardService`: Submit(score).
- `IPlatformService`: GameReady(), GameplayStart(), GameplayStop().

| call / event | when |
| --- | --- |
| GameReady() | Menu first becomes interactive |
| GameplayStart() | entering Playing (run start, Resume, after CardPick, after an ad) |
| GameplayStop() | leaving Playing (Paused, CardPick, AbilityPick, Defeat, Victory, ad open, focus lost) |
| focus or visibility lost | Playing → Paused; `AudioListener.pause = true`. On return audio resumes and the game stays Paused |
| ad open / close | `AudioListener.pause` true / false |

Ad usage:

- **Rewarded 'Revive':** once per run, offered on Defeat. Star HP = 50% of max. Every non-boss enemy within 4.0 of the star dies (crystals drop). All heat = 0. The star is immune for 1.5 s.
- **Rewarded 'x2 dust':** on the result screen.
- **Rewarded 'New cards':** once per run, a button on the card screen (not on the ability pick) that redraws the 3 cards from cardRng.
- **Interstitial:** shown when Retry or Menu is pressed on Result, only if all of these hold: runs >= 3, the run lasted >= 30 s, >= `interstitialMinInterval` 150 s have passed since the last interstitial, and >= 60 s since the last rewarded ad.

## 8. Flow and UI

States:

| state | what it is |
| --- | --- |
| Menu | menu screen |
| AbilityPick | slot-A pick before a run started from Menu |
| Playing | the simulation runs |
| Paused | pause screen |
| CardPick | card or slot-B ability pick |
| Defeat | revive offer overlay, simulation stopped; declining it or using the revive moves on |
| Victory | 1.5 s celebration |
| Result | summary screen |

Retry from Result restarts immediately with no loading screen. Esc or P toggles pause on desktop.

**Input:** `HoldZone` is a full-screen transparent UI Image under all other UI (raycast target) implementing IPointerDownHandler and IPointerUpHandler.

- Hold = a set of pointerIds that went down on HoldZone. An id is removed on its own pointer up; Unity sends OnPointerUp to the pressed object even after the pointer left it. Pointer exit is ignored.
- The Space key counts as one more holder.
- The set clears on leaving Playing and on focus loss. Input is active only in Playing.
- The player can hold with one finger and tap an ability button with another. Without a second finger the player must release to tap, and the orbit falls.

**Pick screens** (CardPick, AbilityPick): buttons ignore input for `pickInputDelay` 0.4 s after opening and accept only pointers pressed after the screen opened. After a pick, the overlay fades out over 0.4 s (DOTween) with the simulation still stopped, then the state returns to Playing.

**Canvas:** Screen Space Overlay, Scale With Screen Size, reference 1920x1080, match 0.5. HUD is anchored inside `Screen.safeArea`. Every HUD element stays outside the field circle of radius 6.2. Hud switches layout when `Screen.width > Screen.height` changes.

**UI art.** The UI is built in the scene as ordinary uGUI objects (Image, TMP_Text, Button) plus the prefabs `CardView` and `HudCardIcon`. Code only sets text, `fillAmount`, sprites from `ArtSet` and visibility. Bars are an Image of type Filled (Horizontal) over a frame Image; the cooldown overlay of an ability button is Filled Radial360. Frames, segmented looks and static icons (star, crystal, card) are plain sprites in the scene: decor, the owner may replace or remove them. A `HudCardIcon` is instantiated from its prefab when a card is first taken. Placeholder bars and frames are plain `rect` sprites.

**HUD, portrait:**

- Top centre: star HP bar (red, star icon on its left). Under it: XP bar (cream, card icon at its end). During the boss: boss HP bar under the XP bar.
- Top left: crystal icon + dust counter for this run. Top right: timer mm:ss counting up, pause button under it.
- Bottom left: icons of taken cards with stack numbers. Bottom right: ability buttons. Bottom centre: tutorial hint.

**HUD, landscape:** star HP, XP and boss bars at the top left; dust, timer and pause at the top right. In landscape the top 1 unit band is free only where |x| > 3.4 units. Card icons bottom left, ability buttons bottom right, tutorial hint in the left column.

**Tutorial hints** (tutorial run only, one at a time, cream text, 0.3 s DOTween fade; `TutorialHints`):

| id | shows when | hides when | text |
| --- | --- | --- | --- |
| H\_HOLD | run start | the 3 parked Swarmers die, or after 12 s | «Держи — спутник уходит наружу» |
| H\_RELEASE | first enemy within r 3.0 after H\_HOLD hides | release held for 0.3 s | «Отпусти — спутник падает к звезде» |
| H\_HEAT | first time any heat >= 0.7 | heat < 0.3 | «Перегрев! Держи, чтобы остыть» |
| H\_ARMOR | first Bastion spawns | it dies | «Броня: бей внутри серебряного кольца» |
| H\_PULSE | >= 4 enemies within 3.0 and PULSE ready | PULSE used | «Жми Q или кнопку: ИМПУЛЬС» |
| H\_RES | first resonance | after 2 s | «Резонанс: урон ×2» |

**Screens:**

- **CardPick:** dim overlay, 3 large `CardView` objects with icon, title, plus line (cream) and minus line (red). Tap to choose. The 'New cards (ad)' button sits under them.
- **AbilityPick:** the same layout with abilities.
- **Result:**
  - Defeat: survived time, 'N s short of the record' (or 'new record'), dust earned, buttons Retry (primary), x2 dust (rewarded), Menu.
  - Victory: time, dust, newly unlocked era, the same buttons.
- **Menu:** Play, Daily, era selector (unlocked eras only), Blueprints, best time.
- **Pause:** Resume, Restart, Menu.

All UI strings live in a static `Strings` class (Russian). A rewarded button's text names the reward and says it is an ad.

## 9. Visual spec and swappable art

### 9.1 Principle

Art is data, not code. Everything the player sees is a prefab or a scene object. Its sprite, colour, material, size and sorting are set in the Inspector. The owner changes the art without touching code, in either of two ways:

- Drag another sprite onto the `Body` SpriteRenderer of the prefab (or onto an Image in the scene).
- Overwrite the PNG in `Assets/Perigee/Art/` under the same file name; every reference survives.

Every part of an object is one of two kinds:

- **[R] required:** code reads or drives it. The owner may change its picture, size and colour, or hide it by disabling its renderer, but must not delete it.
- **[D] decor:** nothing in code refers to it. It animates itself with a small component (`Spin`, `Pulse`, `PulseAlpha`, `Scroll`, `TrailReset`). The owner may delete, replace or add decor freely. Decor numbers (speeds, alphas) are serialized fields of these components, not GameConfig: they are art, not gameplay.

### 9.2 ArtSet, Visual, sizes

- **`ArtSet`** (ScriptableObject, `Assets/Perigee/ArtSet.asset`, `CreateAssetMenu`) holds the pictures that code spawns or assigns, and nothing else (numbers stay in GameConfig): prefabs `satellite`, `crystal`, `enemies[]` (by `EnemyId`), `shard`, `spark`, `resonanceThread`, `telegraphArrow`, `telegraphWedge`, `telegraphLine`; sprites `cardIcons[]` (by `CardId`) and `abilityIcons[]` (by `AbilityId`). Objects that exist once (Star, ArmorRing, AnchorRing, PulseRing, BossOrbitLine, Background, all UI) are scene objects with serialized references. A prefab changed in Play Mode shows up after the next Restart, because pools are rebuilt at run start.
- **`Visual`** sits on the root of every prefab and on `Star`. It has a serialized `SpriteRenderer body` [R]. Awake stores `baseColor` and `baseScale` from the prefab. Methods: `SetAlpha(a)`, `Tint(color)`, `Untint()` (back to baseColor, alpha 1), `Flash(color, sec)` (DOTween, SetLink), `SetSizeMul(m)` (localScale = baseScale x m). Code never writes an absolute colour or scale, only changes relative to what the owner set. Fade-ins, crystal blinking, stun, the star hit flash, low-HP tint and GROW all go through these methods.
- **Orientation:** the tip of a sprite points to +Y. An enemy whose `EnemyDef.faceStar` is true (all except Parasite, Hive and Boss) has its root rotated so that +Y looks at the star.
- **Size:** collision radii are GameConfig values (`satRadius`, `EnemyDef.radius`, `crystalRadius`, `starRadius`) and never depend on a sprite. The default prefabs are drawn so that the visible radius equals the collision radius. To fit a new sprite to its hit circle, `SatelliteSystem`, `EnemySystem` and `CrystalSystem` draw their circles in `OnDrawGizmos` (Scene view, or Game view with Gizmos on; nothing in a build). Fit the picture by changing the prefab scale; change the radius in GameConfig only when the gameplay should change.

### 9.3 Default look (placeholder art)

Soviet constructivist poster. Flat shapes, no gradients, no post-processing, unlit sprite material only (no Light2D).

Palette: background #0E0E0E, cream #EFE4C8, red #D6281F, gold #E3B23C, silver #B9BDC2. Gold = beneficial (crystals, resonance). Red = enemy or danger. Satellites are the highest-contrast objects on screen.

The placeholder PNGs are made once in M5A through the MCP (`execute_code`; nothing is saved as a project tool). They reproduce what `ShapeFactory` draws today (read it before deleting it). The shapes: `circle`, `circle_hollow`, `diamond`, `triangle`, `wedge` (60 deg sector), `hexagon`, `rect`, `rays_36`, `ring_dashed_24`, `_28`, `_32`, `_40`, `_48`, `_60`. Import settings: 512 px, Sprite (2D and UI), PPU 256 (a unit-radius shape needs scale (r, r, 1)), Bilinear, no mipmaps, no compression, pivot Center. A black outline is baked into the PNG where the table says so. The ability pictograms (today `ShapeFactory.Pictogram`) and the shared `icon_card` (a cream hexagon) are made in M5A as well. A [D] detail below may also stay baked into its enemy's PNG instead of being a child object (Bastion rivets and Hive holes are baked today); keep whatever M1-M5 draw now. Without MCP, the checklist tells the owner to create the basic shapes with Assets > Create > 2D > Sprites.

| object | [R] required parts | [D] decor |
| --- | --- | --- |
| Star (scene object) | `Visual` Body: cream circle r1.0, black outline. Below 30% HP the Body is tinted toward red in proportion to the missing HP | `Rays` child: `rays_36`, 36 cream rays between r1.05 and r1.35, `Spin` 6 deg/s. `Pulse` on the star root: scale 1.0 to 1.03 at 1.5 Hz |
| Satellite (prefab) | Body: cream diamond r0.2, black outline. `HeatArc` child (9.4). `SurgeTrail` child: gold TrailRenderer, disabled, enabled by SURGE. Stunned: Body `Tint(stunTint)` | `Trail` child: TrailRenderer 0.35 s, width 0.12 to 0, cream, with `TrailReset` |
| Crystal (prefab) | Body: gold diamond r0.14. Blinks with `SetAlpha` in the last 2 s | `Spin` |
| Swarmer | Body: red triangle r0.2 | none |
| Arrow | Body: red triangle 0.2 wide and 0.6 long (scale (0.1, 0.3)) | 3 thin `rect` speed lines behind |
| Bolt | Body: red diamond r0.15 | none |
| Bastion | Body: red wedge r0.55, silver outline | 3 thin `rect` rivet lines |
| Parasite | Body: red circle r0.25. `AttachMarker` child: red `circle_hollow` with `PulseAlpha`, enabled while attached | 6 thin `rect` legs |
| Shooter | Body: red `circle_hollow` r0.3 | `rect` barrel pointing at the star |
| Hive | Body: red hexagon r0.6 | 3 small black `hexagon` holes |
| Boss | Body: red circle r0.9 | `Trail` child: TrailRenderer 1.2 s, width 0.9 to 0, red to cream, with `TrailReset` |
| Telegraphs (prefabs) | `TelegraphArrow` (red `triangle`), `TelegraphWedge` (red `wedge`), `TelegraphLine` (red thin `rect`, alpha 0.3). Code places, rotates and scales them | `PulseAlpha` on the arrow and the wedge |
| ArmorRing (scene object) | root `Visual` with Body = child `Ring`: silver `ring_dashed_60`. Code sets the root scale to the ring radius and its visibility | `Spin` 10 deg/s on `Ring`; `Shield` child at the top (small `diamond` placeholder) |
| AnchorRing, PulseRing (scene objects) | silver `ring_dashed_40` and cream `circle_hollow`, disabled by default (6.2) | none |

**Background** (scene object, always animated, always dimmer than gameplay objects):

- [R] `Rings`: six children at radii 1.7, 2.6, 3.5, 4.4, 5.3, 6.2 using `ring_dashed_60`, `_48`, `_40`, `_32`, `_28`, `_24`, alpha 0.35. `RingHighlight` lerps the alpha of the ring nearest to the innermost satellite's radius to 0.9 (rate 8/s). [D] Their rotation: `Spin` of +24, -18, +13, -10, +7, -5 deg/s.
- [D] `Bars`: two parallel cream bars rotated 58 deg, widths 1.8 and 1.2, perpendicular offsets about -6.5 and +6.0, alpha 0.12 pulsing +-0.04 at 1.5 Hz (`PulseAlpha`). Each bar is two tiled `rect` copies sliding along its axis at 0.4 u/s with wrap-around (`Scroll`).

Sorting order is set in the prefabs and the scene, not by code: bars 0, rings 10, crystals 20, enemies 30, star 40, satellites 50, FX 60.

### 9.4 Procedural geometry

Three objects have a shape that depends on live values, so code fills in their points. They are still prefabs or scene objects; colour, width, material and sorting stay in the Inspector. Point arrays are preallocated and written with `SetPositions` (no per-frame allocation).

- **ResonanceThread** (prefab in `ArtSet`, one per resonating pair, pooled): a gold LineRenderer, 6 segments in a zigzag between the two satellites, points re-randomized every 0.05 s. A `TMP_Text` child "×2" sits at its middle [R].
- **HeatArc** (child of the Satellite prefab): a red LineRenderer arc around the satellite at radius `heatArcRadius` x satRadiusMul, length = heat x 360 deg, up to 48 points. Above heat 0.8 its alpha flickers. Hidden at heat 0. Heat does not tint the Body.
- **BossOrbitLine** (scene object): a red LineRenderer loop of 64 points on the next ellipse, alpha 0.3, dashed by the material texture. Shown for 1.5 s (4.3).

## 10. FX and audio

- **Fx:** `HitStop(sec)` and `Shake(amp, dur)` use real-time timers.
  - Hit-stop 0.04 s only on kills of enemies with base hp >= 2, boss hits and star damage; 0.02 s on clinks. A new hit-stop starts only if `hitStopMinInterval` 0.25 s has passed since the last one.
  - Star damage = `Flash(starHitFlashColor, 0.15 s)` on the star + shake (amp 0.12, dur 0.12). A new shake starts only if `shakeMinInterval` 0.2 s has passed.
  - Kill = 6 cream shard sprites flying outward for 0.4 s. Clink = 4 silver spark lines. Crystal pickup = small pop + punch-scale of the dust counter (DOTween).
  - Shards and sparks are pooled `shard` and `spark` prefabs from `ArtSet` (placeholders: a cream `rect` and a thin silver `rect`), moved by `Fx`, not ParticleSystem.
- **Audio:** `AudioManager.Play(SfxId)`; `enum SfxId { Hit, Clink, Kill, Crystal, Resonance, StarHit, Stun, CardOpen, CardPick, BossIn, Win, Lose }`. Clips live in an `AudioBank` ScriptableObject and may be empty (empty = no-op). One looped drone AudioSource with `pitch = Lerp(0.8, 1.5, avgAngularSpeed / w(rMin))`, where w(rMin) uses the current spinMul.

## 11. Code structure (folder Assets/Perigee/ with subfolders Scripts, Art, Prefabs, Scenes; namespace Perigee)

| group | files |
| --- | --- |
| Core | GameConfig, ArtSet, Visual, GameLoop (state machine, fixed step, hit-stop), RunState (hp, dust, xp, level, time, card stacks), Stats (computed modifiers), Rng, Strings |
| Gameplay | InputHold, StarController, SatelliteSystem (orbit, heat, stun, resonance), EnemySystem (pool up to 200, behaviors, collisions, star contact), Director, CrystalSystem, BossComet, CardSystem, AbilitySystem, Blueprints |
| Presentation | Spin, Pulse, PulseAlpha, Scroll, TrailReset (decor), RingHighlight, ArmorRing, ResonanceThread, HeatArc, BossOrbitLine, Fx, AudioManager, Hud, CardPickView, AbilityPickView, AbilityButtonView, PauseView, ResultView, MenuView, TutorialHints |
| Services | IAdService, ISaveService, ILeaderboardService, IPlatformService, Stub implementations |

Performance: one pool per prefab (each enemy type, crystal, shard, spark, marker types, thread), prewarmed at run start with the counts from 2.1; no per-frame allocations; no LINQ inside the loop. Component references are cached when an object is created, never inside a tick.

## 12. Scene setup (one scene `Game`)

- Main Camera (orthographic, URP 2D Renderer)
- Systems (empty object): GameLoop, SatelliteSystem, EnemySystem, CrystalSystem, Director, BossComet, CardSystem, AbilitySystem, Fx, AudioManager, with references to the `GameConfig` and `ArtSet` assets
- World: Star (Visual, Rays, Pulse), Satellites (parent, filled at runtime from `ArtSet.satellite`), ArmorRing, AnchorRing, PulseRing, BossOrbitLine, Background (Bars, Rings)
- Canvas: HoldZone (first child), Hud, CardPickView (3 `CardView`), AbilityPickView, PauseView, ResultView, MenuView; all ordinary uGUI objects
- EventSystem with InputSystemUIInputModule
- Project: `Assets/Perigee/GameConfig.asset` and `Assets/Perigee/ArtSet.asset` (both with `CreateAssetMenu`), URP asset with the 2D Renderer, build target WebGL, TextMeshPro essentials imported.

## 13. Milestones

| # | Что делаем | Читать | Готово, когда |
|---|---|---|---|
| M1 | ShapeFactory, камера, звезда, кольца, спутник, управление удержанием | 2, 3, 8, 9, 11, 12 | В Play Mode спутник кружит вокруг звезды; пока зажата мышь или пробел, он уходит наружу до внешнего кольца, при отпускании падает к звезде примерно за 0.6 с |
| M2 | Рой, попадания, HP звезды, поражение | 3, 4, 5, 9, 11 | Рои летят к звезде (вступление и фоновый поток из раздела 5), спутник убивает их касанием, прорвавшиеся отнимают HP; при HP 0 состояние Defeat, клавиша R перезапускает мгновенно |
| M3 | Кристаллы, шкала опыта, HUD, карточки, пауза | 4, 6, 8, 11 | Убитые враги роняют кристаллы, спутник их собирает, шкала заполняется, открывается выбор из 3 карточек, выбор применяется, пауза работает |
| M4 | Все враги, расписание, кольцо брони, нагрев, 2–3 спутника, нить | 3, 4, 5, 9, 11 | За 180 с приходят все типы врагов по расписанию; Бастион не получает урон снаружи серебряного кольца, если нет резонанса; спутник получает красную дугу нагрева и «глохнет»; нить вспыхивает при сближении |
| M5 | Способности | 3, 6, 8, 9, 11 | Перед забегом из меню выбирается способность, Retry её сохраняет; кнопка перезаряжается; все 5 работают как в 6.2; второй слот открывается на 3-м повышении уровня |
| M5A | Картинки в префабы (переделка M1–M5): PNG-заглушки, ArtSet, Visual, префабы и объекты сцены вместо ShapeFactory | 2, 6, 8, 9, 11, 12 | Игра выглядит и играется как после M5, но в коде нет ShapeFactory и нет `new GameObject` / `AddComponent` для картинок; в Project лежат Art/*.png, ArtSet.asset и префабы (спутник, кристалл, враги, метки, нить); если подменить спрайт в префабе Satellite или Swarmer (или перезаписать PNG), в следующем Play картинка новая, а орбита и столкновения те же; в Scene view видны круги попаданий |
| M6 | Босс, победа, итоги, сохранение, чертежи, меню, режимы | 4, 7, 8, 9, 11 | На 3:00 появляется Комета; победа и поражение показывают итоги; пыль и чертежи сохраняются после перезапуска |
| M7 | Эффекты, фон, звук | 9, 10 | Работают hit-stop, тряска, осколки, анимация колец и полос; звуковые вызовы срабатывают (клипы могут быть пустыми) |
| M8 | Сервисы-заглушки, обучение, WebGL | 5, 7, 8, 11 | Заглушки рекламы и платформы вызываются в нужные моменты; при сворачивании вкладки игра встаёт на паузу и молчит; первый забег показывает подсказки; WebGL-сборка запускается в браузере |

M1–M5 были сделаны с `ShapeFactory`, который рисует всё кодом. M5A — единственный этап, которому разрешено переделывать готовое: он заменяет ShapeFactory префабами и объектами сцены по разделу 9. С M6 код картинки не рисует.

Что делает M5A, по порядку:

1. Сохраняет PNG-заглушки (9.3), повторяющие то, что ShapeFactory рисует сейчас: контуры, штрихи колец, заклёпки Бастиона, дырки Улья, лучи звезды, пиктограммы способностей.
2. Создаёт `ArtSet`, `Visual` и префабы из таблицы 9.3. Спутники, кристаллы, враги, метки телеграфа и нить резонанса создаются через Instantiate префаба из ArtSet; пулы остаются.
3. Star, ArmorRing, AnchorRing, PulseRing, Background и весь UI (Hud, кнопки способностей, экраны выбора) становятся настоящими объектами сцены со спрайтами в SpriteRenderer или Image и не создаются кодом.
4. Вращение, пульс и бегущие полосы переходят в компоненты-декор (`Spin`, `Pulse`, `PulseAlpha`, `Scroll`, `TrailReset`). `BackgroundAnimator` и `ShapeFactory` удаляются.
5. Из GameConfig убираются поля, которые только описывали вид (ширины контуров и колец, доли штрихов, размеры деталей, порядки сортировки, толщины линий): теперь они в префабах. Геймплейные числа не трогать. Оглушённый спутник окрашивается `stunTint`, отдельный пустой ромб не нужен. Список убранных полей идёт в отчёт.
6. Поведение игры не меняется: те же числа, радиусы столкновений из GameConfig. В отчёте: что стало префабом, что осталось в коде и почему.

## 14. Out of scope for v1

Real Yandex SDK, cosmetics, localization beyond the `Strings` class, sprite atlases, sprite animation clips (the owner may add Animators to prefabs as decor), unique era mechanics (black hole pull and similar), unit tests, editor tooling, analytics. v2 note: all blueprints cost 4,930 dust (about 30 runs), after which dust has no use; cosmetics (an alternative `satellite` prefab in `ArtSet`) should be its sink.
