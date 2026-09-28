# Архив одноразовых инструментов

Эти C#-файлы **не входят в Assets** и не компилируются Unity. Для открытия, игры и редактирования готовых сцен они не нужны. Весь интерфейс уже сохранён в `Assets/Scenes/MainScene.unity`, `Assets/Scenes/GameplayScene.unity` и `Assets/Prefabs`.

- `NextCellRegression.cs` вместе с `SeptemberFeedbackTests.cs` — актуальные проверки ввода, стартовых букв, рекламы и записанного звука. Запуск `NextCellRegression.Run`. `NextCellFeedbackPass.cs` — авторинг финального меню/победы/аудио только в отдельной копии; использует исходную сцену из `Backups/before-next-cell-ads` как `Assets/RestoreReference_Main.unity` без исходного meta. Подробности: `Docs/NEXT_CELL_AUDIO_ADS.md`.

- `SeptemberFeedbackTests.cs` — актуальные проверки после удаления Турбо: игровые сценарии, сохранения, стартовые буквы, поражение, повтор и снимки. `SeptemberFeedbackPass.cs` — одноразовые правки из состояния `Backups/before-september-review`, только для отдельной копии. Не запускать миграцию повторно поверх готовых сцен. Подробности: `Docs/SEPTEMBER_REVIEW.md`.

- `FinishGameProject.cs`, `ReferencePolish.cs`, `AudioAuthoring.cs` — одноразовая подготовка сцены, импорт спрайтов, создание образцов префабов и WAV.
- `RuntimeSmoke.cs` — проверки настоящего игрового цикла в Play Mode с отдельным временным сохранением.
- `FeedbackPass.cs` — правки от 27 сентября; запускать только на исходных сценах из `Backups/before-full-feedback` в отдельной копии. `FeedbackTests.cs` дополняет UsabilityTests проверками запаса 90 перьев, лимита восстановления, отсутствия повторов и положения коллекций. Последний лог: `Docs/ValidationLogs/feedback-verified.log`.
- `ProjectReview.cs` — снимки готовых экранов.
- `UsabilityRevision.cs` — последующая одноразовая переработка ввода, сложности, вёрстки и разделение на две сцены; исходник — `Backups/before-usability-pass/Scenes/MainScene.unity`.
- `UsabilityTests.cs`, `UsabilityReview.cs`, `GameplayEntrySmoke.cs` — архив проверок версии с двумя режимами. RuntimeSmoke/ProjectReview рассчитаны на ещё более раннюю односценовую версию.
- `SceneLayoutOwnership.cs` — одноразовый перенос компоновки из runtime в сохранённые объекты и возврат прежнего магазина/рисованных кнопок. Исходные сцены: `Backups/before-layout-control/Scenes`. Для воспроизведения нужны также прежние сцены `Backups/before-full-feedback/Scenes`, скопированные в тестовый проект как `Assets/LayoutReference_Main.unity` и `Assets/LayoutReference_Gameplay.unity` **без их meta**, чтобы Unity выдала новые GUID. Не применять повторно к готовым сценам. `RunAndTest` включает `LayoutOwnershipTests` и `UsabilityTests`: 978 элементов сравниваются с Edit Mode после Awake и полного игрового цикла. Журнал: `Docs/ValidationLogs/scene-editable-tests.log`.
- `ReferenceRevision.cs` — вторая переработка по замечаниям пользователя: удаление выбора режима и более точное соответствие референсам. Исходная сцена для этого инструмента — `Backups/before-reference-polish/MainScene.unity`. Он не предназначен для повторного применения к готовой сцене.

Не запускайте `FinishGameProject.Run` поверх готовой сцены: инструмент рассчитан на исходный прототип, не на повторное применение. Для воспроизведения авторинга нужна отдельная копия проекта, исходная сцена из `Backups/2026-09-26/MainScene_before_finish.unity` и временное размещение этих файлов в `Assets/Editor` той копии. В рабочем проекте используйте обычный Inspector и Hierarchy.

Игровая логика находится отдельно в `Assets/Scripts`. Коллекции — исключение из прежнего правила о полностью готовом интерфейсе: `CollectionGallery` создаёт карточки из единственного образца сцены и `CollectionCatalog.asset`.

`ContentCatalogPass.Run` — одноразовая миграция коллекций, окна подсказок и трёх сердечек. Применялась в тестовой копии к снимку `Backups/before-content-catalog/Scenes`, затем `FinishLayout` сохранял слой модального окна и положение сердец. Повторно поверх обновлённой сцены миграции не запускать. `ContentCatalogRegression.Run` — актуальные проверки нового каталога, сохранений, поражения и подсказок со снимками. Запускать в отдельной копии без `-quit`, сохранение временное.
