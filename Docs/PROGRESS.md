Done:
- M1: ShapeFactory, камера, звезда, 6 колец, один спутник, удержание (мышь, палец, пробел).

Next: M2

Deviations:
- До M6 меню нет: GameLoop.Start сразу вызывает StartRun, состояние Playing.
- Кольца в M1 неподвижны. Вращение, подсветка и полосы фона делаются в M7 (так в таблице этапов).
- Сцена называется game.unity (с маленькой буквы), лежит в Assets/Perigee/Scenes.
- Выбрано самому, в GDD нет: outlineWidth 0.02, ringWidth 0.06, ringDashFill 0.5, starRayFill 0.5 (лучи сужаются к концу),
  кольца цвета cream, лучи звезды крутятся по часовой, ромб спутника не поворачивается вдоль орбиты,
  радиус кольца = его внешний край. Все числа лежат в GameConfig.

Open:
- Build Settings: там 4 чужие сцены Assets/DeepRhytm/... (в проекте их нет), game.unity не добавлена. Починить до M8.
- Player Settings, Run In Background выключен: Play Mode стоит, пока окно Unity не в фокусе (для M8 так и надо).
- В Assets/Settings/Renderer2D.asset висит фича Pixelation. Сейчас картинку не меняет, но GDD требует без постобработки.
- Не проверено глазами: Arc, Triangle, Hexagon, Wedge, Rect из ShapeFactory (в M1 на экране только Circle, Diamond, Ring, Rays).

Notes:
- Конфиг: Assets/Perigee/GameConfig.asset. В нём все константы 2.1, eras[], rings[], палитра, sort*, simHz,
  spriteMaterial (URP Sprite-Unlit-Default, им же рисуется TrailRenderer).
- Скрипты: Assets/Perigee/Scripts/Core, Gameplay, Presentation. Asmdef нет (Assembly-CSharp).
- GameLoop (Systems): State, SetState, StartRun, Stats, Era. Тики добавлять в private Step(dt) в порядке GDD 3.1.
  Hit-stop ещё нет (M7). Камера: orthoSize и фон выставляются каждый кадр в Update.
- Stats: radialSpeedMul, spinMul (уже включает era.spinMul), Reset(era). Остальные поля добавлять по мере надобности.
- SatelliteSystem (Systems): пул на maxSatellites, private class Satellite { transform, trail, angle, r },
  ResetRun, Tick, AngularSpeed(r). Нагрева, оглушения, резонанса, появления новых спутников нет (M4).
  Наружу позиции спутников пока не отдаёт: для попаданий в M2 нужен доступ.
- InputHold (Canvas/HoldZone): IsHolding, Clear. Пробел через InputAction "<Keyboard>/space".
- StarController (World/Star): только вид (Body, Rays) в Update. HP и урон в M2.
- BackgroundAnimator (World/Background): создаёт кольца под Rings.
- ShapeFactory: Circle, Triangle, Diamond, Hexagon, Wedge (параметр outline), Ring(tickCount, width, fill),
  Arc(degrees, width), Rays(count, inner, fill), Rect(), NewRenderer(name, parent, sprite, color, order, material).
  outline и width в долях единичного радиуса. Спрайты кэшируются по ключу.
- Сцена: Main Camera; Systems; World (Star, Satellites, Background/Rings); Canvas (HoldZone); EventSystem.
- Проверка без фокуса на Unity: через execute_code вызывать GameLoop.Step рефлексией, кадры сами не идут.
