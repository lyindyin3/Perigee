Done:
- M1: ShapeFactory, камера, звезда, 6 колец, один спутник, удержание (мышь, палец, пробел).
- M2: Рой (вступление, фоновый поток, телеграф, плавное появление), попадания, HP звезды, Defeat, клавиша R.
- M3: Кристаллы (подбор, магнит), опыт и уровни, 12 карточек с выбором из 3, МАЯК, HUD (портрет и альбом), пауза (Esc, P, кнопка).

Next: M4

Deviations:
- До M6 меню и итогов нет: GameLoop.Start сразу вызывает StartRun. В Defeat клавиша R вызывает StartRun. В паузе нет кнопки «Меню».
- Кольца неподвижны: вращение, подсветка и полосы фона в M7 (так в таблице этапов). Сцена называется game.unity, лежит в Assets/Perigee/Scenes.
- M2 сделан только для Роя: в enemies[] один Swarmer, в opening[] только 2 с x4 и 8 с x6. На M4 оставлены: Бастион на 14 с,
  остальные враги и их вид, поле behavior, angleMode у групп, основное расписание, телеграф Стрелы, искры и звук брони.
- M3: ADD_SAT пишет стак и heatMul, сам спутник появится в M4 (читать Stats.extraSatellites). 3-й уровень пока открывает карточки,
  а не выбор способности (M5). ABILITY_LONG не выпадает до M5 (CardSystem.Eligible). Кнопка «Новые карточки (реклама)»: M8. Эффекты подбора: M7.
- Выбрано самому (M1): outlineWidth 0.02, ringWidth 0.06, ringDashFill 0.5, starRayFill 0.5, кольца цвета cream,
  лучи звезды крутятся по часовой, ромб спутника не поворачивается вдоль орбиты, радиус кольца = его внешний край.
- Выбрано самому (M2): телеграф = красный треугольник telegraphSize 0.3, мигает вкл/выкл 4 Гц, пул maxMarkers 8;
  звезда ниже 30% HP краснеет линейно до red при HP 0 (тело и лучи); углы всех групп выбираются при старте забега.
- Выбрано самому (M3): полосы HP и опыта заполняются плавно, делений 12 и 8; иконка взятой карточки = рамка с двумя буквами и числом стаков; названия карточек в Strings;
  волна МАЯКА = кремовое кольцо 0.35 с, урон сразу, без учёта брони; crystalBlinkHz 6, crystalSpin 180, maxCrystals 256; цвета и размеры UI записаны в сцене, не в конфиге.

Open:
- Build Settings: там 4 чужие сцены Assets/DeepRhytm/... (в проекте их нет), game.unity не добавлена. Починить до M8.
- Player Settings, Run In Background выключен: Play Mode стоит, пока окно Unity не в фокусе (для M8 так и надо).
- В Assets/Settings/Renderer2D.asset висит фича Pixelation. Сейчас картинку не меняет, но GDD требует без постобработки.
- Не проверено глазами: Hexagon, Wedge и неполная дуга Arc. До Defeat честно не дойти (проверка: starMaxHp = 9 до Play).
- Шрифт штатный (LiberationSans SDF), кириллица идёт через динамический Fallback: его ассет меняется при игре в редакторе. Свой шрифт: Default Font Asset в TMP Settings.

Notes:
- Конфиг: Assets/Perigee/GameConfig.asset. Правка инициализатора в коде существующее поле (enemies[], opening[], cards[]) не меняет: править сам ассет.
- GameLoop (Systems): State, SetState (включает PauseView), StartRun, TogglePause, RecomputeStats, Stats, Run, Era. Step(dt): RecomputeStats,
  Run.time, satellites.Tick, enemies.Tick, enemies.Collide, crystals.Tick, director.Tick, star.Tick, cards.Tick. Hit-stop нет (M7).
- RunState: seed, hp, time, dust, xp, level, cardStacks[] (по индексу в config.cards), simRng (seed + 2). Rng: Range(float), Range(int), CardOffset, SimOffset.
- Stats.Recompute(config, era, cardStacks): карточка = CardDef {plus, minus} из StatMod {StatId, value}; способности и чертежи добавлять сюда. Пока никто не читает: heatMul, resonanceAngleMul, arrowSpeedMul, abilityCooldownMul, abilityDurationMul, extraSatellites.
- SatelliteSystem: Count, Position(i), Radius (с satRadiusMul), Damage(i) (резонанс добавлять сюда), Power(r), AngularSpeed(r). Нагрева, оглушения, новых спутников нет (M4).
- EnemySystem: Spawn, Tick, Collide, Blast(radius, damage), Kill(i) = кристаллы + Remove (сюда эффекты M7); все враги рисуются Triangle. CrystalSystem: Spawn(pos, amount), Tick; подбирает ближайший спутник (в M4 добавить проверку оглушения и появления).
- CardSystem: Need(level), Offer(slot), OfferCount, Pick(slot), Tick (МАЯК + проверка опыта), ResetRun(seed + 1). Director: Pending[] из config.opening.
- UI: Hud (полосы, пыль, таймер, иконки; раскладка через пустышки BarsPortrait/BarsLandscape/DustPortrait/DustLandscape), CardPickView (Show, Pick), PauseView.
- Сцена: Main Camera; Systems; World (Star, Satellites, Background/Rings, Enemies, Telegraphs, Crystals, Fx); Canvas (HoldZone, Hud, CardPickView, PauseView); EventSystem.
- Проверка без фокуса: execute_code, GameLoop.Step рефлексией, твины DOTween.CompleteAll(); снимок с UI: manage_camera screenshot без camera, папка Temp/Shots.
