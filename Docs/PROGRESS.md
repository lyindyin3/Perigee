Done:
- M1: ShapeFactory, камера, звезда, 6 колец, один спутник, удержание (мышь, палец, пробел).
- M2: Рой (вступление, фоновый поток, телеграф, плавное появление), попадания, HP звезды, Defeat, клавиша R.
- M3: Кристаллы (подбор, магнит), опыт и уровни, 12 карточек с выбором из 3, МАЯК, HUD (портрет и альбом), пауза (Esc, P, кнопка).
- M4: Все 7 типов врагов с поведением и видом, расписание 20–172 с, режимы углов, телеграф Стрелы, нагрев и оглушение, 2–3 спутника, резонанс с нитью, кольцо брони.

Next: M5

Deviations:
- До M6 меню и итогов нет: GameLoop.Start сразу вызывает StartRun. В Defeat клавиша R вызывает StartRun. В паузе нет кнопки «Меню».
- Кольца фона неподвижны: вращение, подсветка и полосы в M7. Сцена называется game.unity, лежит в Assets/Perigee/Scenes.
- M3: 3-й уровень пока открывает карточки, а не выбор способности (M5). ABILITY_LONG не выпадает до M5 (CardSystem.Eligible). Кнопка «Новые карточки (реклама)»: M8.
- M4: искры и звон брони, осколки, hit-stop отложены на M7 (нужны Fx и AudioManager): удар слабее брони сейчас просто ничего не делает.
  Пульс кольца брони в обучении: M8. События «180 Boss» в timeline нет: Комету ставит M6. Толчки PULSE (Паразит, Стрелок): M5.
- Выбрано самому (M1): outlineWidth 0.02, ringWidth 0.06, ringDashFill 0.5, starRayFill 0.5, кольца цвета cream,
  лучи звезды крутятся по часовой, ромб спутника не поворачивается вдоль орбиты, радиус кольца = его внешний край.
- Выбрано самому (M2): телеграф = красный треугольник telegraphSize 0.3, мигает 4 Гц, пул maxMarkers 8; звезда ниже 30% HP краснеет линейно; углы всех групп выбираются при старте забега.
- Выбрано самому (M3): полосы HP и опыта плавные, делений 12 и 8; иконка карточки = рамка с двумя буквами и числом стаков; волна МАЯКА = кремовое кольцо 0.35 с, урон сразу, без учёта брони;
  crystalBlinkHz 6, crystalSpin 180, maxCrystals 256; цвета и размеры UI записаны в сцене, не в конфиге.
- Выбрано самому (M4): враг = тело + деталь (числа в конфиге, «Вид врагов»); дуга нагрева = LineRenderer из 32 отрезков, мигает 12 Гц; оглушённый = пустой ромб со стенкой 0.3;
  щит на кольце брони = серебряный клин 0.2, кольцо alpha 0.7, 36 штрихов; «×2» = TextMeshPro размера 3 сбоку от нити; телеграф Стрелы = клин 0.35 с пульсом 3 Гц и линия 0.04;
  у стаи Роя одна метка на группу, у остальных по метке на врага; Opposite = +180 к предыдущей группе (и через границу событий); Stats.arrowSpeedMul множит скорость Стрел; в ShapeFactory добавлены Spike, Stripes, Hive, Bastion и параметр hollow.

Open:
- Build Settings: там 4 чужие сцены Assets/DeepRhytm/... (в проекте их нет), game.unity не добавлена. Починить до M8.
- Player Settings, Run In Background выключен: Play Mode стоит, пока окно Unity не в фокусе (для M8 так и надо).
- В Assets/Settings/Renderer2D.asset висит фича Pixelation. Сейчас картинку не меняет, но GDD требует без постобработки.
- Не проверено руками (M4): удержание и отпускание в живой игре, Бастион снаружи кольца брони (проверен только расчёт Power: 1 вдали, 3 у звезды, ×2 в резонансе). До Defeat честно не дойти (starMaxHp = 9 до Play).
- Шрифт штатный (LiberationSans SDF), кириллица идёт через динамический Fallback: его ассет меняется при игре в редакторе. Свой шрифт: Default Font Asset в TMP Settings.

Notes:
- Конфиг: Assets/Perigee/GameConfig.asset. Правка инициализатора в коде существующее поле (enemies[], opening[], timeline[], cards[]) не меняет: править сам ассет (или скопировать поле из CreateInstance<GameConfig>()).
- GameLoop (Systems): State, SetState (включает PauseView), StartRun, TogglePause, RecomputeStats, Stats, Run, Era. Step(dt): RecomputeStats,
  Run.time, satellites.Tick, enemies.Tick, enemies.Collide, crystals.Tick, director.Tick, star.Tick, cards.Tick. Hit-stop нет (M7).
- RunState: seed, hp, time, dust, xp, level, cardStacks[] (по индексу в config.cards), simRng (seed + 2). Rng: Range(float), Range(int), CardOffset, SimOffset.
- Stats.Recompute(config, era, cardStacks): карточка = CardDef {plus, minus} из StatMod {StatId, value}; способности и чертежи добавлять сюда. Пока никто не читает: abilityCooldownMul, abilityDurationMul.
- SatelliteSystem: Count, Position(i), Radius, Active(i) (не оглушён и уже появился), Damage(i) (с резонансом), Power(r), AngularSpeed(r). Спутники добавляет Tick по Stats.extraSatellites. ANCHOR (M5): не менять baseR в Tick.
- EnemySystem: Spawn, Tick (поведение по def.behavior), Collide, Blast(radius, damage), Kill(i) = кристаллы + Remove (сюда эффекты M7), Advance, Burst, Dress (вид по типу). Enemy.engaged/timer: Паразит на звезде, Стрелок на позиции;
  для PULSE сбросить engaged. В enemies[] есть Boss (behavior Ellipse), Tick его не обрабатывает. CrystalSystem: Spawn(pos, amount), Tick.
- CardSystem: Need(level), Offer(slot), OfferCount, Pick(slot), Tick (МАЯК + проверка опыта), ResetRun(seed + 1). Director: Pending[] из config.opening + config.timeline (обучение M8 подменит opening), метки: markers, arrowWedges, arrowLines.
- UI: Hud (полосы, пыль, таймер, иконки; раскладка через пустышки BarsPortrait/BarsLandscape/DustPortrait/DustLandscape), CardPickView (Show, Pick), PauseView.
- Сцена: Main Camera; Systems; World (Star, Satellites, Background/Rings, Background/ArmorRing, Enemies, Telegraphs, Crystals, Fx); Canvas (HoldZone, Hud, CardPickView, PauseView); EventSystem.
- Проверка без фокуса: execute_code, GameLoop.Step рефлексией, твины DOTween.CompleteAll(); снимок с UI: manage_camera screenshot без camera, папка Temp/Shots. Следы спутников при этом рисуются прямыми: это артефакт проверки.
