# PERIGEE — GDD review R1

Basis: GDD with section 15 applied. Balance was checked with a re-implementation of sections 2–6 and 15 (fixed step 1/60 s, 200 runs per row). Bots stand in for players: they aim better than people, so read the numbers as relative.

## Часть A. Пять главных проблем (по влиянию)

1. **Урон идёт оттуда, где игрок почти ничего не решает.** Спутник устойчиво стоит только на 1.35 и 6.2, а Стрелок встаёт на 4.9: его Болты снимают 43–63 HP за забег, вместе со Стрелами это 58–71% урона. Улей до взрыва почти не убить, босс (40 HP) умирает за 8–10 с.
2. **Агент соберёт старые числа.** В «Читать» у M1, M6 и M7 нет раздела 15, поэтому M1 выйдет с kExp 1.1 и rMax 4.6. Способности (M8) стоят после обучения (M7), которому уже нужен PULSE.
3. **Пустое начало.** Первые 18 с обычного забега нет врагов, первая карточка появляется на 32–35 с. Каждый рестарт начинается с ожидания.
4. **Ложные выборы.** Карты с минусом к силе делают дальнюю орбиту бессильной против врагов с 1 HP (−9…−12 п.п. к шансу дойти до босса). Второй уровень LONG_ARM и RESONATOR при одном спутнике пусты. Двигающийся игрок не перегревается, поэтому нагрев, CHILL и COOLANT не работают. У REVERSE нет задачи, ADD_SAT обязателен.
5. **На телефоне плохо читается.** 1 единица ≈ 5 мм: Болт и кристалл ≈ 1 мм, Рой ≈ 1.5 мм. Горячий спутник краснеет, как враг. В альбомной ориентации полоса HP лежит на внешней орбите, кнопки ≈ 6 мм.

Итог: общая сложность после раздела 15 уже около цели. Поэтому патч не облегчает игру, а переносит урон на честные источники (B0, B8).

## Part B. Patch list

### B0. Arithmetic basis (check 2)

- Periods at kExp 0.9: r 1.35 → 1.00 s; 2.0 → 1.42; 2.975 (armor ring) → 2.04; 4.6 → 3.01; 6.2 → 3.94. Linear speed grows with r (8.5 → 9.9 u/s) because kExp < 1.
- Stable radii: rMin and the held target only (innermost 6.2 / 5.4 / 4.6 for n = 1 / 2 / 3). Mid radii exist only in transit: the full 4.85 u takes 0.6 s.
- Flight times: Swarmer 7.0 → star 5.3 s; Arrow 5.3 → star 1.6 s (+1.5 s telegraph); Bastion 15.6 s; Bolt 4.9 → star 1.1 s; Hive 7.0 → burst 6.6 s.
- Resting satellite (one pass per 1.0 s): catches ≈ 58% of Swarmers and ≈ 29% of Arrows, never touches a Shooter. Held at 6.2 (n = 1), it is 1.3 u from a Shooter at 4.9; contact is 0.5.
- Totals at 0.8× counts: 111 Swarmers (45 ambient), 13 Arrows, 5 Bastions, 5 Parasites, 4 Shooters, 2 Hives. That is 214 crystals max and 439 star damage if nothing dies (plus Bolts and Hive swarms) against 150 HP.
- XP 8 + 3L, cumulative: 8, 19, 33, 50, 70, 93, 119, 148.

| player proxy | now: reach boss | now: median death | patched: reach boss | patched: median death |
|---|---|---|---|---|
| never holds (camps rMin) | 0% | 2:03 | 0% | 1:32 |
| attempts 1–2 (zone heuristic) | 18% | 2:21 | 28% | 2:53 |
| attempts 3–5 (heuristic / noisy intercept) | 24–29% | 2:28–2:49 | 34–43% | 2:53 |
| practised (intercept) | 74% | 3:08 | 82% | 3:16 |

| metric | now | patched |
|---|---|---|
| Bolt + Arrow share of star damage (attempts 1–5) | 58–71% | 41–43% (Bolt 8–11%) |
| HP lost 1:30 → 2:00 (attempts 1–2) | −54 | −14 |
| first card / level 3 (attempts 1–5) | 32–35 s / 63–74 s | 16–24 s / 46–53 s |
| boss fight length | 8–10 s | 15–21 s |
| overheats per run, moving players | 0–1 | 1–7 |

Camping at rMin is not optimal: it dies at 2:03 today (1:32 patched) while active proxies reach 2:21–3:08.

### BALANCE

- **B1 [BALANCE] MUST** — §5, runs other than the tutorial. Old: no events before 18 s, ambient starts at 18 s. New: `openingEvents` = `2 Swarm x4 | 8 Swarm x6 | 14 Bastion x1` (final counts), `ambientStart` 18 → 6 s. Why: each retry starts with 18 s of empty screen.
- **B2 [BALANCE] MUST** — §4.2 Shooter, §15.2 Bolt. Old: stopRadius 4.9, hp 3, fire every 2.0 s, Bolt starDamage 6. New: `shooterStopRadius` 5.8, hp 2, `shooterFireInterval` 2.5 s, Bolt starDamage 4. Why: 5.8 is touched by the held outermost satellite for any n; Bolt damage drops from 43–63 to 11–16 HP per run.
- **B3 [BALANCE] MUST** — §4.2 Hive. Old: hp 8, bursts at r ≤ 2.4. New: hp 4, `hiveBurstRadius` 3.0. Why: 8 hits at Power 1 take about 28 s of passes against 6.6 s of flight. After the change the early-kill rate is 20–77% and rises with skill.
- **B4 [BALANCE] MUST** — §3.2 Power. Old: no lower bound. New: `Power = max(…, powerFloor)`, `powerFloor` 1.0. Why: powerMul below 1 makes 1-HP enemies need two hits at the far orbit. These cards cost 9–12 pp of boss reach now and about 0 with the floor.
- **B5 [BALANCE] MUST** — §4.2/§7 era.hpMul. Old: applies to every enemy. New: only to enemies with base hp ≥ `eraHpMinBaseHp` 2. Why: hp 1.3 and 1.6 make every Swarmer, Arrow and Bolt a 2-hit target at the far orbit in eras 2–3.
- **B6 [BALANCE] MUST** — §3.3/§15.2. Old: heatRate 0.20, coolRate 0.45. New: 0.35 and 0.25 (stunTime 1.0 and heatAfterStun 0.4 unchanged). Why: today heat never triggers for a moving player. New timings: 2.9 s at rest from cold, 1.7 s after a stun, 4 s to cool fully. CHILL goes from ≈0 to +6–9 pp.
- **B7 [BALANCE] MUST** — §4.3/§15.2 Boss hp 40 → 80 (× era.hpMul). Why: today it is won 75–98% of the time once reached. After the change, 28–41% of attempt-1–5 proxies win once they reach it, and 78% of practised ones.
- **B8 [BALANCE] MUST, only together with B1–B4, B6, F1** — §15.2 starMaxHp 150 → 120; Swarmer starDamage 2 → 3. Why: those fixes remove about 40 HP per run of damage the player cannot influence. Without compensation, attempt-3–5 proxies reach the boss 55–80% of the time. Each ±10 HP ≈ ±3–8 pp.
- **B9 [BALANCE] MUST** — §6 LONG_ARM → APOGEE (C2). Why: the 15.1 clamp makes stack 1 worth +0.4 and stack 2 worth 0, and 6.6 is off-screen in landscape (half-height 6.2).
- **B10 [BALANCE] NICE** — §6 SWIFT minus: powerMul ×0.85 → heatMul ×1.2. Why: it is the weakest card even with B4 (−14 pp).
- **B11 [BALANCE] NICE** — §6 ADD_SAT. Old: guaranteed in the first card screen, max 2 stacks. New: the second satellite is granted at level 1 with a 1 s banner instead of a card screen; ADD_SAT max stacks 1. Why: it is the strongest card (+16–20 pp) and it is forced, so that screen is not a choice.
- **B12 [BALANCE] NICE** — §15.4 SURGE: "radialSpeed ×2, Power ×1.5" → "Power ×2 and armor ignored", 4 s, cooldown 40 s. Why: radial speed is never the bottleneck, and today SURGE adds about 0 for weaker players.
- **B13 [BALANCE] NICE** — §4.2 Parasite hp 3 → 5. Why: the resting satellite kills it in one pass; even a player who never holds kills 93% or more.
- **B14 [BALANCE] NICE** — §7 Blueprints ACTUATORS and MAGNETISM → C6. Why: +5% radial speed and +1 s of crystal life are invisible to the player.
- **B15 [BALANCE] NICE** — §5 variety: a SpawnEvent may carry `alt`, a second composition of equal threat, picked 50/50 by spawnRng. Why: only angles vary, so the script is memorised in about 10 runs.

### FEEL

- **F1 [FEEL] MUST** — §4.2/§9 sizes (collision = sprite). Swarmer r 0.15 → 0.20; Arrow r 0.12 → 0.15 (sprite 0.6 × 0.2); Bolt r 0.10 → 0.15; `crystalRadius` 0.10 → 0.14. Why: on a 6.1" phone 1 u ≈ 5 mm. The balance effect (+4–15 pp) is already absorbed by B8.
- **F2 [FEEL] MUST** — §9 Satellite heat. Old: the body lerps to red. New: the body stays cream; heat is a red arc around it (r 0.32 u, 0 → 360° with heat) that flickers above 0.8; stunned = a silver hollow diamond. Why: red means enemy.
- **F3 [FEEL] MUST** — §3.5/§10. Old: hit-stop on every hit. New: hit-stop only on kills of enemies with base hp ≥ 2, clinks, boss hits and star damage; `hitStopMinInterval` 0.25 s, `shakeMinInterval` 0.2 s. Why: a 10-Swarmer wave makes 0.4 s of freezes in about 2 s, which looks like lag on WebGL.
- **F4 [FEEL] MUST** — §5/§9/§15.3. Old: telegraph at the screen edge; spawn circle 7.0 called "off-screen". New: the marker sits on the spawn ray at r = min(`telegraphRadius` 6.6, visible rect inset 0.5); enemies fade in over `spawnFadeIn` 0.25 s with no collisions. Why: 7.0 is on-screen along the long axis (half-extent 11.0–13.4), so today the enemy pops in at 7.0 while the marker sits near 10.5.
- **F5 [FEEL] MUST** — §8/§15.4 HUD. Old: bars at top centre, hint at bottom centre, ability buttons 88 px. New: every HUD element stays outside the r 6.2 circle. Landscape: bars top-left; dust, timer and pause top-right; hint in the left column. The top 1 u band is free where |x| > 3.4 u. Portrait keeps today's layout. Ability buttons 88 → 140 px (≈ 9 mm), margin 16 → 24 px. Why: in landscape the outer orbit touches the top edge under the HP bar, and 88 px ≈ 5.8 mm is below 48 dp.
- **F6 [FEEL] NICE** — §4.3 Boss timing. Old: uniform E. New: `M = π + 2π·t / bossPeriod`, solve `E − e·sin E = M` with 4 Newton steps from E = M. Why: with uniform E the comet is as fast at periapsis as at apoapsis, so the dive is not the dangerous moment.
- **F7 [FEEL] NICE** — §4.3 Boss telegraph: draw the next ellipse as a red dashed line (alpha 0.3) for 1.5 s after spawn and after each `phi += 40°`. Why: the trail shows the last dive, not the next one.
- **F8 [FEEL] NICE** — §8 CardPick exit: the overlay fades for 0.4 s with the simulation still stopped, then the state returns to Playing. Why: the player needs a moment to put the finger back.

### SPEC

- **S1 [SPEC] MUST** — §13 "Читать": add 15 to M1, M6 and M7. Why: /gdd-milestone reads only the listed sections.
- **S2 [SPEC] MUST** — §13 order: M8 Abilities becomes M5; old M5–M7 become M6–M8. Why: the tutorial needs PULSE, and 15.6 already says "after M4".
- **S3 [SPEC] NICE** — §15: fold it into §2–13 (one value per parameter) and delete the overlay (question E2). Why: about 25 parameters are written twice.
- **S4 [SPEC] MUST** — §8/§15.4 input. New: hold = the set of pointerIds that went down on HoldZone. An id is removed on its own pointer up (Unity sends OnPointerUp to the pressed object even after exit); pointer exit is ignored; the set clears on leaving Playing and on focus loss; Space counts as one more holder. Why: with §8 "exit = release", a holding finger that slides over an ability button drops the orbit.
- **S5 [SPEC] MUST** — §8/§15.4 pick screens: buttons ignore input for `pickInputDelay` 0.4 s and accept only pointers pressed after the screen opened. Why: the screen opens under a holding finger, and lifting it picks a card blind.
- **S6 [SPEC] MUST** — §15.4: the run-start ability pick appears only when starting from Menu (Play, Daily); Retry reuses the last slot-A choice. Why: it conflicts with "Retry restarts immediately".
- **S7 [SPEC] MUST** — §7 Rng: `spawnRng` (Director), `cardRng` (card and ability offers), `simRng` (crystal scatter, Hive burst, boss phi), seeded with seed, seed+1, seed+2. Why: with one stream, Daily waves diverge after the first kill.
- **S8 [SPEC] MUST** — §6 card PULSE → BEACON (same stats). Why: it clashes with the PULSE ability.
- **S9 [SPEC] MUST** — §6/§15.5 offers: RESONATOR only when satellites ≥ 2; ABILITY_LONG only when an owned ability has duration > 0. Why: otherwise both are dead picks.
- **S10 [SPEC] MUST** — §3.4 `stats.resonanceMul2` → `stats.resonanceAngleMul` (RESONATOR ×1.5, GROW ×1.3). The ×2 damage never stacks: a satellite in two pairs still deals ×2. Why: the name is undefined.
- **S11 [SPEC] MUST** — §3.5/§13 M4: resonance damage counts against armor, so resonating satellites pierce armor at any radius (Power × 2 ≥ 2). The thread shows "×2". M4 test: no damage outside the ring unless in resonance. Why: this is an emergent rule, and M4's acceptance test fails as written.
- **S12 [SPEC] MUST** — §4.3: E starts at π (apoapsis, as the text says); initial phi comes from simRng; the boss takes no damage during its 0.5 s fade-in. Why: the formula starts at periapsis.
- **S13 [SPEC] MUST** — §5 SpawnEvent: add `speedMul` (default 1) and `radius` (default spawnRadius; Arrow 5.3). `angleMode` ∈ {Random, Opposite, Spread90, Even, OppositePair}. In "A + B Opposite", B's centre = A's centre + 180°. The telegraph starts `telegraph` s before `time`, so the spawn happens exactly at `time`. Ambient spawns have no telegraph. Why: "speed x0.6", "one per quadrant" and "Shooter x2 Opposite" cannot be expressed today.
- **S14 [SPEC] MUST** — §5/§15.2: drop the 0.8 multiplier and write final counts: `20 Swarm x8 | 30 Swarm x8 | 36 Bastion x1 | 46 Arrow x2 Spread90 | 56 Swarm x10 | 62 Arrow x2 Spread90 | 70 Parasite x1 | 80 Bastion x1 + Swarm x8 Opposite | 88 Parasite x2 Spread90 | 98 Shooter x1 | 108 Arrow x3 Even | 116 Hive x1 | 126 Swarm x11 + Shooter x1 Opposite | 136 Bastion x2 OppositePair | 146 Hive x1 + Arrow x2 Spread90 | 156 Parasite x2 Spread90 + Swarm x10 | 164 Shooter x2 OppositePair + Bastion x1 | 172 Arrow x4 Even + Swarm x11 | 180 Boss`. Why: Mathf.RoundToInt rounds half to even, and "x4 one per quadrant" silently becomes 3. These are the simulated counts.
- **S15 [SPEC] MUST** — §3.2/§15.1: one cap, `satMaxRadius` 6.2 (= ringMax), for every satellite; delete maxSatelliteRadius 6.6 and rMaxBonus. Why: the two sections give two caps, and 6.6 is off-screen.
- **S16 [SPEC] MUST** — §7 EraConfig fields: kExp, spinMul, hpMul, countMul. Era 1: 0.9 / 1.0 / 1.0 / 1.0. Era 2: 0.9 / 1.3 / 1.3 / 1.2. Era 3: 1.2 / 1.0 / 1.6 / 1.4 (kExp was 1.5). Why: 15.1 does not say which kExp changed, era 2 uses a spinMul that EraConfig lacks, and kExp 1.5 gives a 9.8 s outer period (question E4).
- **S17 [SPEC] MUST** — §15.4 definitions. "Level 3" = the 3rd level-up of the run (tutorial included). GROW doubles satRadius, so the pickup distance (satRadius × satRadiusMul + crystalRadius) doubles with it; magnetRadius is unchanged. PULSE detaches a Parasite: t resets and it flies back. A pushed Shooter walks back to its stop radius. A new satellite deals no damage and collects nothing during its 0.3 s fade-in.
- **S18 [SPEC] MUST** — §8 states. Defeat = revive overlay (simulation stopped), then Result. Victory = 1.5 s celebration, then Result. Result = summary screen. Daily = era 1 with its own best key `bestDaily`.
- **S19 [SPEC] MUST** — §7 Services: add `IPlatformService` (C4). On focus or visibility loss and on ad open: state Paused and `AudioListener.pause = true`. Why: Yandex requirements 1.3, 4.7 and 1.19.2–1.19.3.
- **S20 [SPEC] MUST** — §7 Interstitial. Old: after a defeat, when runs ≥ 3 and defeats is even. New: when Retry or Menu is pressed on Result, if runs ≥ 3, the run lasted ≥ 30 s, ≥ `interstitialMinInterval` 150 s have passed since the last interstitial and ≥ 60 s since the last rewarded ad. Why: on defeat it collides with the Revive offer, and Yandex 4.4 allows ads only in logical pauses.
- **S21 [SPEC] MUST** — §7 Save: call PlayerPrefs.Save() after every write. Why: on WebGL, a closed tab can lose unsaved data (Yandex 1.9).

### SCOPE

- **X1 [SCOPE] NICE** — originality. Closest games: Orbt XL / "Keep Orbt" (same hold-out / release-in control, used for avoidance), Super Hexagon (centre with inward threats, one-axis control), Vampire Survivors-style level-up cards. On Yandex: "Орбитальный Рубеж" (planet defence + roguelite, tap controls). What keeps PERIGEE distinct: the orbit is the weapon, heat and resonance, and HP triage. Store screenshots and the first 5 s should show the resonance thread killing a swarm. Never call it an orbit runner. Why: Yandex 3.6 and 5.1.1.1.
- **X2 [SCOPE] NICE** — §14: all blueprints cost 4,930 dust, about 30 runs at 130–190 dust per run, after which dust has no use. Plan cosmetics (satellite shape and trail) as the dust sink in v2.

## Part C. New content

Milestones after S2, still 8: M1–M4 unchanged, M5 Abilities, M6 Boss/meta (old M5), M7 FX/audio (old M6), M8 Services/tutorial/WebGL (old M7). What is cut: REVERSE, LONG_ARM, ACTUATORS, the per-run ability pick on Retry, and the static control hint.

### C1. ANCHOR ability (replaces REVERSE) — MUST

| id | source | effect | duration | cooldown |
|---|---|---|---|---|
| ANCHOR | satellites | baseR freezes at its current value; hold and release are ignored; heat rules still apply | 4 s | 30 s |

Pictogram: cream circle with a horizontal bar and a short vertical stem. While active, a silver dashed ring is drawn at baseR. Why: with binary control the mid radii (armor ring 2.975, Hive, Shooter band) exist only in transit, so this gives a deliberate way to hold one. REVERSE has no use case: flipping direction does not change how often a satellite passes an angle. Cost: no new files (an AbilitySystem branch and a ShapeFactory icon), M5.

### C2. APOGEE card (replaces LONG_ARM) — MUST

| id | plus | minus | max stacks |
|---|---|---|---|
| APOGEE | `powerFarBonus` +0.5: Power at r ≥ powerFarRadius goes from 1.0 to 1.5 | heatMul ×1.15 | 2 |

The armor-ring formula uses `powerFar + powerFarBonus`. It gives the far orbit a build without touching the radius cap. Cost: one Stats field, no new files, M3.

### C3. Tutorial run, first 30 s (replaces the §5 "First run" line and the static hint) — MUST

| t, s | event (final counts) | lesson |
|---|---|---|
| 0.5 | Swarm x3, arc 40°, `radius` 5.4, `speedMul` 0.15 | only a held satellite reaches them |
| 8 | Swarm x6, `speedMul` 0.6 | release to defend near the star |
| 16 | Bastion x1 | armor ring (it already pulses per §3.6) |
| 24 | Swarm x8 | — |
| 20+ | ambient as in normal runs | — |

| hint id | shows when | hides when | text (`Strings`) |
|---|---|---|---|
| H_HOLD | run start | the 3 parked Swarmers die, or 12 s pass | «Держи — спутник уходит наружу» |
| H_RELEASE | first enemy within r 3.0 after H_HOLD | release held ≥ 0.3 s | «Отпусти — спутник падает к звезде» |
| H_HEAT | first time any heat ≥ 0.7 | heat < 0.3 | «Перегрев! Держи, чтобы остыть» |
| H_ARMOR | first Bastion spawns | it dies | «Броня: бей внутри серебряного кольца» |
| H_PULSE | ≥ 4 enemies within 3.0 and PULSE ready | PULSE used | «Жми Q или кнопку: ИМПУЛЬС» |
| H_RES | first resonance | after 2 s | «Резонанс: урон ×2» |

One hint at a time, cream text, 0.3 s DOTween fade, placed by the F5 rules. Cost: SpawnEvent fields from S13, one TMP label in Hud, a new `TutorialHints` file (Presentation), M8.

### C4. IPlatformService (Yandex hooks) — MUST

| call / event | when |
|---|---|
| GameReady() | Menu first becomes interactive |
| GameplayStart() | entering Playing (run start, Resume, after CardPick, after an ad) |
| GameplayStop() | leaving Playing (Paused, CardPick, Defeat, Victory, ad open, focus lost) |
| focus or visibility lost | Playing → Paused; `AudioListener.pause = true`; audio resumes on return, the game stays Paused |
| ad open / close | `AudioListener.pause` true / false; the simulation is already stopped |

Cost: `IPlatformService` and `StubPlatformService` (Services, 2 new files), M8.

### C5. Rewarded card reroll — NICE

CardPickView gets a button «Новые карты (реклама)», once per run. On success the 3 cards are redrawn from cardRng. The button is hidden on the ability pick. Cost: no new files, M8.

### C6. Blueprints and Daily bonus — NICE

| id | effect per level | levels | costs |
|---|---|---|---|
| CAPACITOR (replaces ACTUATORS) | ability cooldowns ×0.95 per level (×0.95^level) | 5 | 50, 100, 200, 400, 800 |
| MAGNETISM | magnetRadius +0.35 per level (was crystalLife +12.5%) | 3 | 40, 80, 160 |

Daily: the first Daily run of each UTC date doubles that run's dust (stacks with the rewarded ×2). Save field: `dailyBonusDate`. Cost: no new files, M6.

## Part D. Contradictions and ambiguities

Contradictions already fixed by S10–S18, F4, F6, B5, B9 and C3 are not repeated here.

- §3.2: "period ≈ 3.9 s at rMax" is stale after 15.1 (3.0 s at 4.6; 3.9 s at 6.2).
- §3.1: the tick order omits AbilitySystem (15.4 says right after Satellites).
- §3.3: heat during a stun is undefined (simplest: frozen, then set to heatAfterStun).
- §3.6: the armor ring under SURGE or APOGEE should use the total Power multiplier including temporary effects; the text only says powerMul.
- §4.1: periapsis 1.8 < starRadius + bossRadius = 1.9, so the boss overlaps the star on every pass; confirm this is intended.
- §4.2 Parasite: contact happens at distance 1.25, then it snaps to 1.15; say so. Two Parasites at one angle overlap.
- §4.2 Hive: "random angles within 0.5" → uniform positions in a disc of r 0.5 from simRng; the children get no telegraph.
- §6: "the first pick always includes ADD_SAT" — only the first screen, or every screen while satellites < 3?
- §6: when fewer than 3 cards are eligible, is it OK to show fewer?
- §7: is the score's "seconds survived" the total run time including the boss?
- §7 Revive: ability cooldowns are not mentioned (simplest: unchanged).
- §8: no desktop pause key is defined (Esc / P).
- §10: drone pitch `maxAngularSpeed` — w(rMin) with the current spinMul, or a fixed 360°/s?
- §13 M1: "falls to the star in half a second" — 4.85 u at 8 u/s is 0.61 s.
- §13 M2: the acceptance test uses the Retry button, but Result and Strings arrive in M5 (M6 after S2).
- §15.3: Arrows spawn at 5.3, inside the held orbit 6.2 (n = 1), so a held satellite never meets a fresh Arrow. Fine if catching Arrows is meant to be an intercept skill; say so.
- §15.4: do ABILITY_HASTE ×0.75 and ABILITY_LONG ×1.15 combine by multiplying?
- §0.2 "no try/catch" vs a corrupted `perigee_save`: JsonUtility throws on malformed JSON. Allow one try/catch in SaveService, or define a fallback.
- Claude.md points to Docs/PROGRESS.md, which does not exist and which the GDD never mentions.

## Часть E. Вопросы

1. Звезда 150 → 120 и урон Роя 2 → 3 (B8): согласен? Без этого, после остальных правок, игроки на 3–5 попытке будут доходить до босса в 55–80% забегов.
2. Влить раздел 15 в разделы 2–13 и убрать надстройку (S3)? Или тебе важно сохранить историю правок в таком виде?
3. Второй спутник выдавать автоматически на 1-м уровне (B11) или оставить его обязательной картой в первом выборе?
4. Эра 3: kExp 1.5 (внешний круг ≈ 9.8 с, дальняя орбита почти бесполезна) — так задумано, или поставить 1.2 (≈ 6.2 с)?
