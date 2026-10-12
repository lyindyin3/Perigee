# ПЕРИГЕЙ — арт-пак для генерации (к GDD R3)

Здесь описано, как выглядит каждая картинка игры, и для каждой есть готовый промпт для ИИ-генератора. Claude Code этот файл не читает: он ставит заглушки, а ты потом заменяешь их своими картинками.

## Как пользоваться, по шагам

1. Перед каждым промптом вставь общий **стиль-префикс** (ниже). Так все картинки выйдут в одном стиле.
2. Генерируй по одной картинке за раз. Текст на картинках не нужен: названия и цифры игра пишет сама.
3. Сохраняй в PNG. Иконкам, врагам и спутникам нужен прозрачный фон. Если генератор не умеет прозрачность, проси чистый белый фон и потом убери его (например, remove.bg или «Удалить фон» в Фотошопе).
4. Положи PNG в папку проекта:
   - иконки и рамки — в `Assets/Perigee/Art/UI`;
   - всё, что летает на поле, — в `Assets/Perigee/Art`.
5. В Unity выдели PNG и в Inspector поставь: Texture Type = **Sprite (2D and UI)**, Pixels Per Unit = **256**, Mesh Type = **Full Rect**. Нажми Apply.
6. Подставь картинку одним из двух способов:
   - **Заменить заглушку:** назови файл так же, как заглушку (например, `bastion.png`), и положи поверх старого. Unity сам подхватит новую картинку везде.
   - **Новая картинка:** открой `Assets/Perigee/ArtSet.asset` (или нужный префаб в `Assets/Perigee/Prefabs`) и перетащи спрайт в нужное поле. Номера полей указаны в таблицах ниже.
7. Если враг или спутник вышел не того размера, открой его префаб, выдели дочерний объект **Body** и меняй Scale, пока картинка не совпадёт с белым кругом попадания. Круг виден в окне Scene во время Play.

### Стиль-префикс (вставлять в начало каждого промпта)

```
Soviet constructivist propaganda poster style, flat bold shapes, thick black outlines,
limited palette: cream #EFE4C8, signal red #D6281F, near-black #0E0E0E, gold #E3B23C,
silver #B9BDC2, subtle risograph print grain, no gradients, no glow, strong simple
silhouette readable at small size, centered, 2D game asset,
```

### Негативный промпт (если генератор его поддерживает)

```
text, letters, numbers, words, watermark, signature, logo, photorealistic, 3D render,
soft gradient, bloom, blur, noisy background, extra colors, perspective, drop shadow
```

### Цвета семейств

| семейство | рамка карточки | цвет иконки на ней | смысл |
| --- | --- | --- | --- |
| ОРБИТА | кремовая | чёрный + красный | спутники, движение, кристаллы |
| УДАР | красная | кремовый + чёрный | урон |
| ЗВЕЗДА | чёрная | кремовый + золото | защита звезды, способности |
| редкая | любая + золотая кайма | как у семейства | сильные необычные карточки |

---

## 1. Рамки карточек (5 файлов)

По образцу, который ты прислал: прямоугольник со скруглёнными углами, толстая чёрная обводка, внутри тонкая вторая рамка, ровная заливка цветом с лёгким зерном. Иконка рисуется отдельно и ложится сверху. Отличие от образца одно: в нижней трети рамки тёмная плашка. На ней игра пишет название, плюс и минус.

Размер всех рамок **512 × 704 px**.

| файл | что это | промпт (после стиль-префикса) |
| --- | --- | --- |
| `card_frame_cream.png` | ОРБИТА | `blank game card frame, portrait rectangle with rounded corners, thick black outer border, thin black inner border inset, flat cream fill with paper grain, lower third is a solid near-black text panel separated by a thin cream line, empty center, no icon` |
| `card_frame_red.png` | УДАР | то же, но `flat signal red fill` |
| `card_frame_black.png` | ЗВЕЗДА | то же, но `flat near-black fill, inner border thin cream line` |
| `ability_frame.png` | карточка способности | `blank game card frame, rounded corners, near-black fill, thick gold border, thin cream inner line, lower third darker text panel, empty center` |
| `card_rim_rare.png` | золотая кайма редкой карточки (кладётся поверх) | `ornamental gold rim overlay for a rounded card, gold border with small constructivist triangles in the corners, completely transparent center, transparent background` |

Куда: ArtSet → `Card Frames` (0 = кремовая, 1 = красная, 2 = чёрная), `Ability Frame`, `Card Rare Rim`.

---

## 2. Иконки карточек (30 файлов)

Размер **512 × 512 px**, прозрачный фон, предмет занимает примерно 70% квадрата. Цвета берутся по семейству (таблица выше), чтобы иконка читалась на своей рамке.

Куда: ArtSet → `Card Icons`, номер в списке указан в первом столбце.

### Для всех эр (21)

| № | файл | карточка | что нарисовано | промпт (после стиль-префикса) |
| --- | --- | --- | --- | --- |
| 0 | `card_heavy.png` | ТЯЖЁЛЫЙ СПУТНИК (удар) | кремовый ромб-спутник, под ним чёрная гиря, линии падения вниз | `icon, heavy cream diamond-shaped satellite with a black iron kettlebell weight hanging below it, short speed lines pointing down, transparent background` |
| 1 | `card_far_strike.png` | ДАЛЬНИЙ УДАР (удар) | большая дуга орбиты, на краю маленький спутник бьёт по цели | `icon, wide cream orbit arc, small diamond satellite on the far end striking a black target mark, long curved arrow along the arc, transparent background` |
| 2 | `card_sharp.png` | ОСТРЫЕ ГРАНИ (удар) | гранёный кремовый кристалл-клинок, на острие золотая искра | `icon, faceted cream crystal blade with sharp edges, small gold spark star at the tip, transparent background` |
| 3 | `card_pierce.png` | БРОНЕБОЙ (удар) | чёрный бур пробивает серебряную пластину с заклёпками | `icon, black drill bit piercing a riveted silver armor plate, cracks around the hole, transparent background` |
| 4 | `card_execute.png` | РАСКОЛ (удар, редкая) | чёрный треугольник-враг, расколотый кремовой трещиной пополам | `icon, black triangle split cleanly in half by a jagged cream crack, halves slightly apart, transparent background` |
| 5 | `card_saw_thread.png` | НИТЬ-ПИЛА (удар, редкая) | два ромба, между ними золотая зигзаг-нить с зубьями пилы | `icon, two cream diamonds connected by a gold zigzag thread shaped like saw teeth, transparent background` |
| 6 | `card_burn.png` | РАСКАЛЁННЫЙ (удар) | кремовый ромб в языках пламени, волны жара | `icon, cream diamond satellite wrapped in stylized black-and-cream flames, heat wave lines, transparent background` |
| 7 | `card_chain.png` | ЦЕПНАЯ РЕАКЦИЯ (удар, редкая) | три взрыва-звёздочки, соединённые цепью | `icon, three angular starburst explosions in cream linked by a black chain, transparent background` |
| 8 | `card_add_sat.png` | НОВЫЙ СПУТНИК (орбита) | кольцо орбиты, на нём два ромба и крупный знак плюс | `icon, black orbit ring with two red diamond satellites and a bold red plus sign, transparent background` |
| 9 | `card_swift.png` | РЕАКТИВНЫЙ ДВИГАТЕЛЬ (орбита) | ромб с соплом и пламенем, стрелки вверх и вниз | `icon, red diamond satellite with a black rocket nozzle and flame, two arrows up and down beside it, transparent background` |
| 10 | `card_spin.png` | РАЗГОН (орбита) | спираль из стрелок вокруг точки | `icon, black spiral of curved arrows circling a red center dot, sense of speed, transparent background` |
| 11 | `card_big_sat.png` | КРУПНЫЙ СПУТНИК (орбита) | большой ромб рядом с маленьким, стрелки наружу | `icon, large red diamond next to a small black diamond, four short arrows pointing outward from the large one, transparent background` |
| 12 | `card_magnet.png` | МАГНИТ (орбита) | красная подкова-магнит тянет золотые кристаллы | `icon, red horseshoe magnet with black tips pulling small gold diamond crystals, attraction lines, transparent background` |
| 13 | `card_greed.png` | ЖАДНОСТЬ (орбита) | чёрная рука-клешня сгребает золотые кристаллы | `icon, black mechanical claw hand grabbing a pile of gold diamond crystals, transparent background` |
| 14 | `card_coolant.png` | ОХЛАДИТЕЛЬ (орбита) | ромб с рёбрами радиатора и геометричной снежинкой | `icon, black diamond satellite with radiator fins and a geometric red snowflake, transparent background` |
| 15 | `card_star_plating.png` | БРОНЯ ЗВЕЗДЫ (звезда) | кремовое солнце, закрытое пластинами с заклёпками | `icon, cream sun circle covered by riveted gold armor plates, short rays around, transparent background` |
| 16 | `card_photosphere.png` | ФОТОСФЕРА (звезда) | солнце с золотыми лечебными лучами-крестиками | `icon, cream sun with gold plus-shaped sparkles rising from it, transparent background` |
| 17 | `card_corona.png` | КОРОНА (звезда) | солнце с зубчатой короной, отбрасывает красную стрелу | `icon, cream sun with a jagged gold corona ring, a red arrow bouncing off the corona, transparent background` |
| 18 | `card_beacon.png` | МАЯК (звезда) | конструктивистская башня-маяк, от неё кольца волн | `icon, cream constructivist radio tower emitting concentric gold rings, transparent background` |
| 19 | `card_ability_haste.png` | ПЕРЕЗАРЯДКА (звезда) | секундомер с молнией | `icon, cream stopwatch with a gold lightning bolt across it, transparent background` |
| 20 | `card_ability_long.png` | ДОЛГОЕ ДЕЙСТВИЕ (звезда) | песочные часы и длинная стрелка вправо | `icon, cream hourglass with a long gold arrow stretching to the right, transparent background` |

### Только в своей эре (9)

| № | файл | карточка | что нарисовано | промпт (после стиль-префикса) |
| --- | --- | --- | --- | --- |
| 21 | `card_heatproof.png` | ЖАРОСТОЙКОСТЬ (эра 2, орбита) | ромб за щитом, пламя отражается в стороны | `icon, red diamond satellite behind a black shield, flames splitting around the shield, transparent background` |
| 22 | `card_solar_sail.png` | СОЛНЕЧНЫЙ ПАРУС (эра 2, удар) | ромб с треугольным парусом, в парус бьют лучи | `icon, cream diamond satellite with a triangular black sail catching straight sun rays, transparent background` |
| 23 | `card_firebird.png` | ЖАР-ПТИЦА (эра 2, удар, редкая) | конструктивистская птица из языков пламени | `icon, stylized constructivist firebird made of angular cream and black flame shapes, wings spread, transparent background` |
| 24 | `card_slingshot.png` | ПРАЩА (эра 3, удар) | рогатка метает ромб вниз к звезде | `icon, black slingshot launching a cream diamond downward toward a small sun, motion lines, transparent background` |
| 25 | `card_anchor_field.png` | ГРАВИЗАХВАТ (эра 3, орбита) | якорь держит золотые кристаллы в кольце | `icon, black anchor inside a red ring, gold crystals held around the ring, transparent background` |
| 26 | `card_crowbar.png` | ЛОМ (эра 3, удар, редкая) | лом отрывает серебряный полукруглый щит | `icon, black crowbar prying a silver half-ring shield off a round body, transparent background` |
| 27 | `card_event_horizon.png` | ГОРИЗОНТ СОБЫТИЙ (эра 4, звезда) | чёрная дыра с кремовым кольцом, красные стрелы затягиваются внутрь | `icon, black hole disc with a bright cream rim ring, small red arrows spiraling into it, transparent background` |
| 28 | `card_dark_matter.png` | ТЁМНАЯ МАТЕРИЯ (эра 4, удар) | глаз, видящий пунктирный треугольник-призрак | `icon, cream eye looking at a dashed-outline black triangle ghost, transparent background` |
| 29 | `card_accretion.png` | АККРЕЦИЯ (эра 4, звезда, редкая) | спираль золотых частиц втекает в маленькую звезду | `icon, spiral of gold particles flowing into a small cream star with a plus sparkle, transparent background` |

---

## 3. Способности (7 файлов)

Пиктограмма кремового цвета на прозрачном фоне: кнопка в игре уже чёрный круг. Те же картинки идут и на карточки выбора способности. Размер **512 × 512 px**.

Куда: ArtSet → `Ability Icons` (номер в первом столбце). Заглушки: `Art/UI/icon_*.png`, их можно заменить файлом с тем же именем.

| № | файл | способность | что нарисовано | промпт (после стиль-префикса) |
| --- | --- | --- | --- | --- |
| 0 | `icon_reverse.png` | РЕВЕРС | две изогнутые стрелки по кругу в разные стороны (как на твоей красной карточке) | `pictogram, two curved cream arrows forming a circle and pointing in opposite directions, transparent background` |
| 1 | `icon_grow.png` | РОСТ | ромб и четыре стрелки наружу | `pictogram, cream diamond with four arrows pointing outward from its sides, transparent background` |
| 2 | `icon_pulse.png` | ИМПУЛЬС | точка и три концентрических кольца | `pictogram, cream dot with three concentric rings expanding from it, transparent background` |
| 3 | `icon_stasis.png` | СТАЗИС | песочные часы с замёрзшими гранями | `pictogram, cream hourglass with frozen angular ice shards, sand stopped mid-fall, transparent background` |
| 4 | `icon_echo.png` | ЭХО | ромб и две его тающие копии позади | `pictogram, cream diamond followed by two fading outline copies of itself, transparent background` |
| 5 | `icon_pull.png` | ПРИТЯЖЕНИЕ | три стрелки внутрь к кольцу | `pictogram, three cream arrows pointing inward to a cream ring, transparent background` |
| 6 | `icon_dome.png` | КУПОЛ | полукруглый купол над звездой | `pictogram, cream half-dome shield arching over a small sun, transparent background` |

---

## 4. Враги и боссы

Вид сверху, прозрачный фон, фигура по центру и заполняет круг до краёв картинки. Это важно: так размер в игре совпадёт с кругом попадания. Если враг **смотрит на звезду**, нос рисуй **вверх**. Основной цвет красный, детали кремовые, серебряные или чёрные, толстая чёрная обводка.

Размер: обычные враги **512 × 512 px**, боссы **1024 × 1024 px**.

Куда: перетащи в Body префаба (`Assets/Perigee/Prefabs/<Имя>.prefab` → Body → Sprite) или замени PNG-заглушку с тем же именем.

### Уже есть в игре (можно перерисовать)

| префаб | имя | вид | промпт (после стиль-префикса) |
| --- | --- | --- | --- |
| Swarmer | Рой | маленький красный треугольник, нос вверх | `top-down tiny red arrowhead drone pointing up, black outline, transparent background` |
| Arrow | Стрела | длинный узкий красный клин, три линии скорости сзади | `top-down long narrow red dart pointing up, three cream speed lines at the tail, transparent background` |
| Bolt | Снаряд | красный ромбик-пуля | `top-down small red diamond-shaped bullet pointing up, cream core, transparent background` |
| Bastion | Бастион | красный клин-крепость, серебряная окантовка, заклёпки, нос вверх | `top-down red wedge-shaped fortress pointing up, silver armored rim, three rivet lines, transparent background` |
| Parasite | Паразит | красное круглое тело с шестью лапками | `top-down round red parasite body with six thin black legs around it, cream eye dots, transparent background` |
| Shooter | Стрелок | красное кольцо со стволом вверх | `top-down hollow red ring turret with a black cannon barrel pointing up, transparent background` |
| Hive | Улей | красный шестиугольник с тремя чёрными сотами | `top-down red hexagon hive with three black hexagonal holes, transparent background` |

### Новые враги

| префаб | имя | вид | промпт (после стиль-префикса) |
| --- | --- | --- | --- |
| Splitter | Дробитель | красный ромб, расчерченный на 3 части чёрными швами (видно, что он развалится) | `top-down red diamond made of three segments separated by black seams, about to split, pointing up, transparent background` |
| Torch | Факел | красный шар с кремовым ядром, пламя-хвост вниз (от звезды) | `top-down red fireball with a cream core and a flame tail trailing downward, pointing up, transparent background` |
| Orbiter | Кружащий | красный полумесяц с точкой в центре и коротким хвостом | `top-down red crescent drone with a cream center dot and a short tail, pointing up, transparent background` |
| Shielder | Щитник | красное круглое тело, нос вверх (щит отдельно, см. ниже) | `top-down round red armored body with a small black visor pointing up, transparent background` |
| Shielder → Shield | щит Щитника | серебряная дуга-полукольцо | `silver half-ring arc shield with rivets, transparent background` |
| Devourer | Пожиратель | красная круглая пасть с кремовыми зубами по кругу, раскрыта вверх | `top-down round red maw creature with a ring of cream triangular teeth, mouth opening upward, transparent background` |
| Phantom | Призрак | полупрозрачный красный треугольник с пунктирной обводкой и кремовым глазом | `top-down red triangle ghost with a dashed outline and one cream eye, pointing up, slightly translucent, transparent background` |

### Боссы

| префаб | имя | вид | промпт (после стиль-префикса) |
| --- | --- | --- | --- |
| Boss | Комета | большой красный шар с кремовыми полосами (хвост рисует игра) | `top-down large red comet head sphere with cream stripes and a black outline, transparent background` |
| Twin | Близнецы | красный шар с половиной кремового диска (два одинаковых) | `top-down red sphere with one half covered by a cream disc pattern, black outline, transparent background` |
| Colossus | Колосс | огромный красный клин-крепость, серебряные плиты, нос вверх | `top-down huge red wedge war machine pointing up, layered silver armor plates, black rivets, transparent background` |
| Pulsar | Пульсар | красный шар с кремовым крестом лучей (лучи крутятся отдельно) | `top-down red sphere core with a cream four-pointed beam cross, black outline, transparent background` |

### Трещины для крупных (по желанию)

По 3 стадии для Бастиона, Улья, Колосса и Пульсара: чёрные трещины на прозрачном фоне того же размера, что и враг. Стадия 1 — пара трещин, стадия 3 — почти раскрошен. Куда: префаб → `Cracks` → `DamageStages` → `Stages` (по порядку 1, 2, 3).

Промпт: `black jagged crack lines only, stage {1|2|3} of 3, increasing damage, transparent background, no object`

---

## 5. Облик: спутники и звёзды

### Спутники (6), 512 × 512 px, вид сверху, прозрачный фон

Спутник — самый контрастный объект на экране: кремовый с чёрной обводкой.

| скин | имя | вид | промпт (после стиль-префикса) |
| --- | --- | --- | --- |
| Romb | Ромб | кремовый ромб (сейчас в игре) | `top-down cream diamond satellite with black outline, transparent background` |
| Sputnik | Спутник-1 | кремовый шар с четырьмя антеннами назад | `top-down cream sphere satellite with four long thin black antennas swept back, transparent background` |
| Gear | Шестерня | кремовая шестерёнка с отверстием | `top-down cream gear wheel satellite with a black center hole, transparent background` |
| Moon | Луна | кремовый полумесяц | `top-down cream crescent moon satellite with black outline, transparent background` |
| RedStar | Звёздочка | красная пятиконечная звезда с кремовой каймой | `top-down red five-pointed star satellite with a cream border, transparent background` |
| Eye | Глаз | кремовый глаз с чёрным зрачком | `top-down cream eye-shaped satellite with a black pupil, transparent background` |

Куда: у каждого скина свой префаб. Скопируй `Prefabs/Satellite.prefab` (Ctrl+D), в копии замени Sprite у Body и перетащи копию в ArtSet → `Satellite Skins` по порядку таблицы. Дочерние HeatArc и Trail не удаляй.

### Звёзды (4), 1024 × 1024 px: два файла на скин — тело и лучи

| скин | имя | тело | лучи |
| --- | --- | --- | --- |
| Sun | Солнце | `top-down cream sun disc with black outline, transparent background` | `ring of 36 thin cream rays around an empty center, transparent background` |
| Oko | Око | `top-down cream sun disc with a large black constructivist eye in the middle, transparent background` | `ring of long and short alternating cream rays around an empty center, transparent background` |
| Sunflower | Подсолнух | `top-down cream sun disc with a black seed-grid pattern in the middle, transparent background` | `ring of wide rounded cream petals around an empty center, transparent background` |
| Motor | Мотор | `top-down cream sun disc designed as an engine hub with black bolts, transparent background` | `ring of cream gear teeth around an empty center, transparent background` |

Куда: ArtSet → `Star Skins` → Body и Rays.

---

## 6. Эры

Для каждой эры три картинки: иконка для меню, фон поля и, если нужно, своя звезда.

| эра | иконка `era_N.png` (512 × 704, как карточка) | фон `backdrop_N.png` (2048 × 2048, тёмный, центр пустой) |
| --- | --- | --- |
| 1 Солнце | `poster illustration, cream sun with rays over a black sky, small orbit rings, constructivist` | `very dark near-black background, faint cream diagonal constructivist bars and thin orbit circles at the edges, empty center, low contrast` |
| 2 Красный карлик | `poster illustration, large red dwarf star with a flare sector shooting out, dark red sky` | `very dark red-black background, faint red flare wedges at the edges, empty center, low contrast` |
| 3 Тяжёлая звезда | `poster illustration, small dense blue-white star with heavy gravity rings bending space` | `very dark blue-black background, faint silver concentric gravity rings at the edges, empty center, low contrast` |
| 4 Чёрная дыра | `poster illustration, black hole with a bright cream accretion ring, spiral of red debris` | `pure black background, faint cream spiral arms at the edges, empty center, low contrast` |

Звезда для эры 4 (по желанию), `star_blackhole.png`, 1024 × 1024: `top-down black disc with a thin bright cream rim, transparent background`.

Куда: ArtSet → `Era Icons`, `Era Backdrops`, `Era Star Sprites` (номер = эра − 1). Пустое поле = нет картинки, игра обойдётся.

---

## 7. Чертежи (16 иконок, 256 × 256 px, кремовые на прозрачном)

| № | чертёж | что нарисовано |
| --- | --- | --- |
| 0 | КОРПУС | солнце в квадратной броне |
| 1 | ЯДРО | солнце с маленьким крестом-плюсом в центре |
| 2 | ОТРАЖАТЕЛЬ | щит, от которого отскакивает стрелка |
| 3 | ВТОРОЕ СОЛНЦЕ | два солнца, одно встаёт из-за другого |
| 4 | ЗАКАЛКА | молот над наковальней |
| 5 | КАМЕРТОН | камертон с волнами |
| 6 | ОСТРИЁ | наконечник копья |
| 7 | ПАРА | два ромба на одной орбите |
| 8 | ТЯГА | реактивное сопло с пламенем |
| 9 | РАДИАТОР | рёбра радиатора |
| 10 | КОНДЕНСАТОР | батарея с молнией |
| 11 | КВАДРИГА | четыре ромба на кольце |
| 12 | МАГНЕТИЗМ | подкова-магнит |
| 13 | ОЗАРЕНИЕ | лампочка-звезда |
| 14 | АЛХИМИЯ | колба с пылью |
| 15 | ВЫБОР | веер из трёх карточек |

Общий промпт: `blueprint icon, cream line-and-fill pictogram of {что нарисовано}, technical drawing feel, transparent background`. Куда: ArtSet → `Blueprint Icons`.

---

## 8. Интерфейс

| файл | что | размер | промпт (после стиль-префикса) |
| --- | --- | --- | --- |
| `bar_frame.png` | рамка полоски HP и опыта | 1024 × 96 | `horizontal bar frame, thick black border, 12 evenly spaced thin black segment ticks, transparent inside, rounded ends` |
| `bar_fill.png` | заливка полоски (игра красит её сама) | 1024 × 96 | `flat white horizontal bar fill with subtle grain, no border` |
| `button_round.png` | круглая кнопка способности | 256 × 256 | `round black button disc with a thin cream inner ring, flat, transparent background` |
| `panel.png` | фон окон меню | 1024 × 1024 | `dark near-black panel with thin cream double border and small red constructivist corner triangles, empty center` |
| `logo_emblem.png` | эмблема для меню (без букв) | 1024 × 1024 | `constructivist emblem, cream sun with a red diamond satellite on a bold orbit ring, diagonal red bar, no text` |

Название «ПЕРИГЕЙ» лучше не генерировать: ИИ плохо пишет кириллицу. Сделай его обычным текстом в Unity красивым шрифтом поверх эмблемы.

Куда: это объекты сцены. Открой сцену `game`, найди объект на Canvas (например, `Hud/Bars/...`) и перетащи спрайт в поле Image → Source Image.

---

## 9. Проверка после замены

1. Нажми Play и посмотри, что картинка на месте и не растянута.
2. Если картинка сплющена, проверь, что Mesh Type = Full Rect, а у Image в UI включено Preserve Aspect.
3. Если враг выглядит больше или меньше своего круга попадания, меняй Scale у Body в префабе (не у корня).
4. Новая картинка префаба появляется со следующего забега, потому что пулы собираются при старте.
