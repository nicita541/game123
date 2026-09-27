# Компоновка заголовков и настройки

Исправления по снимкам пользователя от 26 сентября 2026.

## Что изменено

- После уточнения пользователя сова, книги и декор объединены с библиотекой в цельные фоновые иллюстрации на всех основных экранах. Фоны сохранены в `Assets/Art/Backgrounds`; точные промты — в `Docs/INTEGRATED_BACKGROUNDS.md`.
- В коллекциях восстановлена композиция референса: сова слева, заголовок справа, светлые компактные карточки авторов без золотой рамки и мини-превью тем снизу.
- В статистике верхняя иллюстрация соответствует композиции референса; карточка эрудиции отделена от книг, выровнены четыре показателя и блок активности.
- Две строки «Перья закончились» помещаются в высокий свиток фоновой иллюстрации и не выступают за его границы.
- Настройки разбиты на четыре отдельные карточки с иконками, пояснениями, переключателями и явной подписью состояния. Кнопка возврата находится внизу; инструменты разработчика вынесены из основного блока.
- Все элементы сохранены в MainScene и GameplayScene. Переключатели только обновляют цвет, подпись и положение готового ползунка. Генерации UI при запуске нет. SettingsScreen сохранён также как редактируемый префаб; при отдельном использовании префаба требуется назначить владельца действий.
- `SceneBackdrop` вписывает существующий фон в Canvas, сохраняя его совмещение с интерфейсом внутри Safe Area. Узкие внешние поля заполняются отражением краёв этой же иллюстрации, без рамки из старого фона и без растягивания совы. Объекты не создаются.

## Новые иконки

Использован навык imagegen и встроенный инструмент генерации изображений (не CLI). Он дал четыре самостоятельных рисованных значка в стиле имеющихся спрайтов: музыка, звук, вибрация, крупный текст. Прозрачность сохранена. Unity импортирует атлас как четыре спрайта с мягкой фильтрацией и без компрессии.

Финальный атлас: `F:/game/Assets/Art/SpriteSheets/settings_icons_v1.png`.
Референс стиля: `Assets/Art/SpriteSheets/reference_icons_v2.png`.

Точный финальный промт:

```text
Use case: stylized-concept. Asset type: Unity game UI sprite atlas for a cozy magical library word game. The attached reference image is a STYLE REFERENCE ONLY, not an edit target. Generate ONE square 2x2 equal-cell atlas of four clean isolated dimensional painted icons, true transparent alpha background. Top left: two connected lavender and gold musical notes, for music. Top right: a rounded blue and gold speaker with two curved sound waves, for sound effects. Bottom left: a small violet smartphone with simple blank pale cream screen and two short curved vibration marks on each side, for haptics. Bottom right: a cream open book with a large violet capital A rising above it, for larger readable text; the single A is the only letter in the entire image. Match the reference's soft polished storybook game UI style, bright but gentle highlights, crisp complete silhouettes and thin golden trim. Four equally sized individual icons, centered in their four cells with at least 18 percent empty transparent margins. No panels, no scene, no owl, no borders, no extra objects, no explanatory labels, no shadows crossing cell boundaries. Do not include a checkerboard; use real transparency.
```

## Проверка

Проверено в отдельной локальной копии Unity 6000.3.8f1. Проверенные сцены, префабы, текстуры и настройки импорта перенесены в основной проект; SHA-256 всех runtime-файлов Assets совпадает с тестовой копией.

- `USABILITY_VERIFY_PASS`: две сцены, 672 сохранённые клетки, 66 клавиш, 984 задания; нет отсутствующих скриптов, шрифтов или потерянных обработчиков кнопок. На 11 экранах назначены цельные фоны, отдельных Image с совой не осталось.
- `USABILITY_RUNTIME_PASS`: переходы, новый профиль, миграция, ручное заполнение букв, сердечки, подсказки, продолжение, награды, длинный текст, прокрутка, настройки и сохранения. Число UI-объектов при игровом сценарии не увеличивается.
- `MANUAL_LETTER_PLACEMENT_PASS` отдельно для Classic и Turbo: одна клетка за ответ, постоянный зелёный цвет, отключение и зачёркивание исчерпанной буквы, частичное сохранение, старые сохранения и подсказка одной клетки.
- `DIRECT_GAMEPLAY_ENTRY_PASS`: отдельный запуск GameplayScene загружает оболочку ровно один раз, начинает игру и позволяет вернуться домой.
- 35 снимков в `Review/LayoutSettings`, включая 1080×2340, короткий экран 1080×1920, вырез 1080×2400, крупный текст и оба новых состояния клавиатуры. Проверено отсутствие обрезанного текста на статистике, коллекциях, настройках и экране перьев в этих вариантах.

Логи: `Docs/ValidationLogs/layout-and-manual-letters.log`, `integrated-backgrounds.log`, `direct-gameplay-entry.log`. В тестовой копии Unity SearchDatabase при старте пишет внутренний `ArgumentOutOfRangeException`; все перечисленные игровые проверки завершаются успешно. Ошибок компиляции игровых скриптов нет. Проверки запускаются программно через сохранённые действия кнопок, это не ручное тестирование касаний на телефоне.

SHA-256 итоговых сцен:

```text
MainScene.unity     09E910A506D73441F39A07EB6433AA5E95C5AD9ABB24A04B1ABF21B2E093AAD4
GameplayScene.unity 3F31B00FEDDFDD64AF130175ECF467D356D3D42961E2A88290C2B188AF338F7D
```

Резервная копия до изменений: `Backups/before-header-settings-layout`.
Инструмент разового авторинга: `Tools/SceneAuthoring/HeaderSettingsPolish.cs`; он не входит в Assets и не нужен для запуска игры.
Подписи клавиатуры обновлены инструментом `Tools/SceneAuthoring/ManualLettersPolish.cs`. Повторно запускать авторинг для игры не требуется.
