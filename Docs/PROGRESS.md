Done:
- M1: ShapeFactory, камера, звезда, 6 колец, один спутник, удержание (мышь, палец, пробел).
- M2: Рой (вступление, фоновый поток, телеграф, плавное появление), попадания, HP звезды, Defeat, клавиша R.

Next: M3

Deviations:
- До M6 меню и итогов нет: GameLoop.Start сразу вызывает StartRun. В Defeat клавиша R вызывает StartRun.
- Кольца неподвижны. Вращение, подсветка и полосы фона делаются в M7 (так в таблице этапов).
- Сцена называется game.unity (с маленькой буквы), лежит в Assets/Perigee/Scenes.
- M2 сделан только для Роя: в enemies[] один Swarmer, в opening[] только 2 с x4 и 8 с x6. На M4 оставлены: Бастион на 14 с,
  остальные враги и их вид, поле behavior, angleMode у групп, основное расписание, телеграф Стрелы, искры и звук брони.
- Выбрано самому (M1): outlineWidth 0.02, ringWidth 0.06, ringDashFill 0.5, starRayFill 0.5, кольца цвета cream,
  лучи звезды крутятся по часовой, ромб спутника не поворачивается вдоль орбиты, радиус кольца = его внешний край.
- Выбрано самому (M2): телеграф = красный треугольник telegraphSize 0.3, мигает вкл/выкл 4 Гц, пул maxMarkers 8;
  звезда ниже 30% HP краснеет линейно до red при HP 0 (тело и лучи); углы всех групп выбираются при старте забега.

Open:
- Build Settings: там 4 чужие сцены Assets/DeepRhytm/... (в проекте их нет), game.unity не добавлена. Починить до M8.
- Player Settings, Run In Background выключен: Play Mode стоит, пока окно Unity не в фокусе (для M8 так и надо).
- В Assets/Settings/Renderer2D.asset висит фича Pixelation. Сейчас картинку не меняет, но GDD требует без постобработки.
- Не проверено глазами: Arc, Hexagon, Wedge, Rect из ShapeFactory.
- До Defeat в M2 честно не дойти: без удержания за весь забег уходит 72 HP из 120. Проверка: starMaxHp = 9 до Play.

Notes:
- Конфиг: Assets/Perigee/GameConfig.asset. Значения в ассете сохранены: правка инициализатора в коде уже существующее
  поле (enemies[], opening[]) не меняет, править сам ассет. Новые поля берут значение из кода до первого сохранения.
- GameLoop (Systems): State, SetState, StartRun, Stats, Run, Era. Step(dt): Run.time, satellites.Tick, enemies.Tick,
  enemies.Collide, director.Tick, star.Tick. Hit-stop нет (M7). Камера: orthoSize и фон каждый кадр в Update.
- RunState: seed, hp, time, Reset(config, seed). Stats: radialSpeedMul, spinMul (с era.spinMul), powerMul, powerFarBonus.
  Rng(seed).Range(min, max) поверх System.Random; пока есть только spawnRng в Director (cardRng seed+1, simRng seed+2).
- SatelliteSystem: Count, Position(i), Damage(i) (сейчас Power(r_i); резонанс добавлять сюда), Power(r), AngularSpeed(r).
  Нагрева, оглушения, резонанса, новых спутников нет (M4). Радиус попадания = satRadius, без satRadiusMul.
- EnemySystem (Systems): пул maxEnemies, живые в pool[0..count). Spawn(type, pos, speedMul), Tick, Collide, ResetRun.
  Смерть от спутника = Remove(i) в Collide: сюда кристаллы (M3) и эффекты (M7). Все враги рисуются Triangle.
- Director (Systems): Pending[] из config.opening, ResetRun(seed), Tick. Событие опаздывает на 1 шаг (сумма float).
- StarController (World/Star): Damage(amount), Tick (HP 0 = Defeat), вид в Update. InputHold (Canvas/HoldZone): IsHolding, Clear.
- ShapeFactory: Circle, Triangle, Diamond, Hexagon, Wedge, Ring, Arc, Rays, Rect, NewRenderer. Asmdef нет.
- Сцена: Main Camera; Systems; World (Star, Satellites, Background/Rings, Enemies, Telegraphs); Canvas (HoldZone); EventSystem.
- Проверка без фокуса: execute_code, GameLoop.Step рефлексией; снимок: manage_camera, camera "Main Camera", папка Temp/Shots.
