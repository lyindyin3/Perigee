Done:
- M1: ShapeFactory, камера, звезда, 6 колец, один спутник, удержание (мышь, палец, пробел).
- M2: Рой (вступление, фоновый поток, телеграф, плавное появление), попадания, HP звезды, Defeat, клавиша R.
- M3: Кристаллы (подбор, магнит), опыт и уровни, 12 карточек с выбором из 3, МАЯК, HUD (портрет и альбом), пауза (Esc, P, кнопка).
- M4: Все 7 типов врагов с поведением и видом, расписание 20–172 с, режимы углов, телеграф Стрелы, нагрев и оглушение, 2–3 спутника, резонанс с нитью, кольцо брони.
- M5: 5 способностей (ЯКОРЬ, РОСТ, ИМПУЛЬС, ХОЛОД, ВСПЛЕСК), слот A перед забегом, слот B на 3-м уровне, кнопки с перезарядкой, клавиши Q и E, ABILITY_LONG в пуле карточек.

Next: M5A (картинки в префабы: Docs/GDD_Perigee.md, раздел 9 и строка M5A в разделе 13), затем M6

Deviations:
- До M6 меню и итогов нет: GameLoop.Start вызывает StartFromMenu (выбор слота A при каждом запуске игры). R в Defeat и «Заново» в паузе = Retry (StartRun, слот A прежний). В паузе нет кнопки «Меню».
- Кольца фона неподвижны: вращение, подсветка и полосы в M7. Сцена называется game.unity, лежит в Assets/Perigee/Scenes.
- M4: искры и звон брони, осколки, hit-stop отложены на M7 (нужны Fx и AudioManager): удар слабее брони сейчас просто ничего не делает. Пульс кольца брони в обучении и кнопка «Новые карточки (реклама)»: M8. События «180 Boss» в timeline нет: Комету ставит M6.
- M5: тряска ИМПУЛЬСА (amp 0.15, 0.2 с) отложена на M7: вызвать Fx.Shake в AbilitySystem.Pulse. Урон ИМПУЛЬСА по боссу (pulseDamage, без толчка) и lastAbilityA в сохранении: M6.
- Выбрано самому (M1): outlineWidth 0.02, ringWidth 0.06, ringDashFill 0.5, starRayFill 0.5, кольца цвета cream, лучи звезды крутятся по часовой, ромб спутника не поворачивается вдоль орбиты, радиус кольца = его внешний край.
- Выбрано самому (M2): телеграф = красный треугольник telegraphSize 0.3, мигает 4 Гц, пул maxMarkers 8; звезда ниже 30% HP краснеет линейно; углы всех групп выбираются при старте забега.
- Выбрано самому (M3): полосы HP и опыта плавные, делений 12 и 8; иконка карточки = рамка с двумя буквами и числом стаков; волна МАЯКА = кремовое кольцо 0.35 с, урон сразу, без учёта брони; crystalBlinkHz 6, crystalSpin 180, maxCrystals 256; цвета и размеры UI записаны в сцене, не в конфиге.
- Выбрано самому (M4): враг = тело + деталь (числа в конфиге, «Вид врагов»); дуга нагрева = LineRenderer из 32 отрезков, мигает 12 Гц; оглушённый = пустой ромб со стенкой 0.3; щит на кольце брони = серебряный клин 0.2, кольцо alpha 0.7, 36 штрихов; «×2» = TextMeshPro размера 3 сбоку от нити; телеграф Стрелы = клин 0.35 с пульсом 3 Гц и линия 0.04; у стаи Роя одна метка на группу, у остальных по метке на врага; Opposite = +180 к предыдущей группе; Stats.arrowSpeedMul множит скорость Стрел.
- Выбрано самому (M5): ИМПУЛЬС бьёт всех в радиусе 3.0 сразу при нажатии, кольцо 0.35 с только рисунок (как у МАЯКА), броню не учитывает; РОСТ надувается и сдувается по 0.2 с внутри своих 6 с, вид и радиус столкновений вместе; длительность и перезарядка запоминаются в момент нажатия; способность срабатывает сразу в Press, а не в шаге симуляции; экран выбора = сетка карточек (в портрете 3 + 2, последний ряд прижат влево); кнопка = чёрный диск, кремовая дуга по ободу, золотое кольцо при готовности (пульс ×1.08 за 0.6 с), нажатие через Button.onClick (по отпусканию); геометрия пиктограмм зашита в ShapeFactory.Pictogram; кольцо ЯКОРЯ: 48 штрихов, ширина 0.02 радиуса.

Open:
- Build Settings: там 4 чужие сцены Assets/DeepRhytm/... (в проекте их нет), game.unity не добавлена. Починить до M8.
- Player Settings, Run In Background выключен: Play Mode стоит, пока окно Unity не в фокусе (для M8 так и надо).
- В Assets/Settings/Renderer2D.asset висит фича Pixelation. Сейчас картинку не меняет, но GDD требует без постобработки.
- Не проверено руками (M4): удержание и отпускание в живой игре, Бастион снаружи кольца брони (проверен только расчёт Power). До Defeat честно не дойти (starMaxHp = 9 до Play).
- Не проверено руками (M5): нажатие кнопок способностей и карточек выбора мышью или пальцем (проверены вызовы Press и Pick и клавиша Q), удержание одним пальцем + кнопка другим, экран выбора в портрете (проверен только расчёт сетки).
- Шрифт штатный (LiberationSans SDF), кириллица идёт через динамический Fallback: его ассет меняется при игре в редакторе. Свой шрифт: Default Font Asset в TMP Settings.

Notes:
- Конфиг: Assets/Perigee/GameConfig.asset. Правка инициализатора в коде существующее поле (enemies[], opening[], timeline[], cards[], abilities[]) не меняет: править сам ассет (или скопировать поле из CreateInstance<GameConfig>()).
- GameLoop (Systems): State, SetState (включает PauseView), StartFromMenu (StartRun + AbilityPick), StartRun, TogglePause, RecomputeStats, Stats, Run, Era. Step(dt): RecomputeStats, Run.time, satellites.Tick, abilities.Tick, enemies.Tick, enemies.Collide, crystals.Tick, director.Tick, star.Tick, cards.Tick. Hit-stop нет (M7).
- RunState: seed, hp, time, dust, xp, level, cardStacks[] (по индексу в config.cards), simRng (seed + 2). Rng: Range(float), Range(int), CardOffset, SimOffset.
- Stats.Recompute(config, era, cardStacks, abilities): карточка = CardDef {plus, minus} из StatMod {StatId, value}; множители РОСТА и ВСПЛЕСКА идут после карточек; чертежи (M6) добавлять сюда.
- SatelliteSystem: Count, Position(i), Radius, Active(i) (не оглушён и уже появился), Damage(i) (с резонансом), Power(r), AngularSpeed(r). Спутники добавляет Tick по Stats.extraSatellites. ЯКОРЬ, ХОЛОД и золотой след читают abilities.Active прямо в Tick, Heat и Place.
- EnemySystem: Spawn, Tick (поведение по def.behavior), Collide (ВСПЛЕСК игнорирует броню), Blast(radius, damage), Pulse(radius, damage, push), Kill(i) = кристаллы + Remove (сюда эффекты M7), Advance, Burst, Dress. Enemy.engaged/timer: Паразит на звезде, Стрелок на позиции. В enemies[] есть Boss (behavior Ellipse), Tick его не обрабатывает. CrystalSystem: Spawn(pos, amount), Tick.
- CardSystem: Need(level), Offer(slot), OfferCount, Pick(slot), Tick (МАЯК + проверка опыта), ResetRun(seed + 1); уровень config.abilityPickLevel открывает AbilityPickView. Director: Pending[] из config.opening + config.timeline (обучение M8 подменит opening), метки: markers, arrowWedges, arrowLines.
- AbilitySystem: SlotA = 0, SlotB = 1; Press(slot), Filled(slot), Id(slot), Charge(slot) (1 = готова, для H_PULSE), Active(id), Elapsed(id), GrowBlend, HasTimed, ResetRun (слот A не трогает, слот B очищает). Таймеры лежат по номеру AbilityId. Выбор: OfferAll() или OfferRandom(cardRng), потом AbilityPickView.Show(), карточка зовёт Pick(i). Без экрана (обучение M8, слот A = ИМПУЛЬС): OfferAll(); Pick(индекс в config.abilities).
- UI: Hud (полосы, пыль, таймер, иконки; раскладка через пустышки BarsPortrait/BarsLandscape/DustPortrait/DustLandscape), CardPickView (Show, Pick), AbilityPickView (Show, Pick), AbilityButtonView (номер слота в сцене), PauseView.
- Сцена: Main Camera; Systems; World (Star, Satellites, Background/Rings, Background/ArmorRing, Enemies, Telegraphs, Crystals, Fx); Canvas (HoldZone, Hud (... Abilities/AbilityB, AbilityA), CardPickView, AbilityPickView, PauseView); EventSystem.
- Проверка без фокуса: execute_code (компилятор C# 6), GameLoop.Step рефлексией, твины DOTween.CompleteAll(); снимок с UI: manage_camera screenshot без camera, папка Temp/Shots (следы спутников при этом прямые: артефакт проверки). После правки скрипта дождаться конца компиляции и только потом Play: иначе домен перезагрузится в Play Mode и Hud.Update даст NullReferenceException (это не ошибка игры).
