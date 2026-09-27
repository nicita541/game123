# Переработка оформления по референсам

26 сентября 2026. Учтено замечание пользователя о лишнем экране выбора режима и неполноте визуального оформления.

## Что изменено

| Экран | Изменения относительно предыдущей версии |
| --- | --- |
| Главное меню | Удалена кнопка «Все режимы». Добавлены книги под совой, спящий кот, чернильница, лавровые ветви, круглая подложка стрелок. Заголовки и подписи кнопок — отдельные Text. |
| Классика / Турбо | Круглые верхние кнопки, счётчик подсказок на синем значке, фонарь, обновлённый пергамент, крупнее буквы и номера. Проверка перенесена в клавишу ✓, добавлена клавиша снятия выделения. Потерянные сердца остаются приглушёнными. |
| Победа | Корона и искры, декор цитаты, раздельные карточки эрудиции и коллекции, иконки наград, монеты, кнопка «Мне нравится», видео и подарок на рекламной кнопке. |
| Нет перьев | Сова на книгах, фонарь, двухстрочный заголовок, выделенный таймер с пером, крупная фиолетовая рекламная кнопка, иконка магазина. |
| Статистика | Верхняя иллюстрация с совой и котом, блок уровня, четыре цветные карточки с разными иконками, подписи значений графика и работающие переключатели периода. |
| Коллекции | Иллюстрированная шапка с песочными часами, шесть разных фонов авторов, круглые стрелки с работающим переходом, компактные карточки тем. Портреты и фоны остаются отдельными Image. |
| Достижения | Иллюстрированная шапка, семь карточек с отдельными значками, прогресс, поясняющее окно по нажатию стрелки, фильтры с переразмещением видимых карточек. |
| Магазин | Два раздельных счётчика, сова с пером на книгах, иллюстрации пачек перьев, монеты на ценовых кнопках, иконки особых товаров. Исправлены отступы и наложения заголовков. |

Экран `Screen_ModeSelection` удалён. На его месте в массиве ссылок находится `Screen_CollectionDetails`, чтобы сохранить остальные сериализованные номера экранов. Это не дополнительный выбор режима: он открывается только из карточек коллекций.

Шрифт заменён на два статических начертания Nunito — Black и SemiBold. [Источник Google Fonts](https://github.com/google/fonts/tree/main/ofl/nunito); SIL OFL сохранена в `Assets/Art/Fonts/OFL.txt`.

## Поддержка

Этот документ описывает предыдущую визуальную версию. Актуальное разделение на две сцены, генератор текстов и новые проверки — в [USABILITY_PASS.md](USABILITY_PASS.md).

Основная сцена: `Assets/Scenes/MainScene.unity`. Вся визуальная структура сохранена в сцене: 1343 объекта, 117 ячеек, 66 буквенных клавиш, 114 компонентов UiAction. Никакой генерации UI в runtime нет.

`ReferenceUiPresenter` обновляет только заранее назначенные элементы статистики, окна достижений и подробностей коллекций. `ReferenceRevision.cs` находится **вне Assets**, в архиве `Tools/SceneAuthoring`; для открытия и запуска проекта не нужен.

Резервная копия предыдущей сцены и скриптов: `Backups/before-reference-polish`. Не запускайте одноразовый авторинг повторно поверх готовой сцены.

## Проверка

Повторно выполнены проверки отсутствующих компонентов/шрифтов, сохранённых действий всех кнопок и полного алфавита. В Play Mode пройдены начало, ошибки, подсказка, продолжение, победа, бонус, поражение, повтор, энергия, покупка и сохранения. Дополнительно проверены подробности коллекции и возврат, 7/12 столбцов графика, запись активности, защита от повторного лайка и окно достижения. Число объектов не меняется во время теста.

Снимки Unity в `Review/ReferenceRevision`: 18 видов в PNG и JPEG. `StatisticsProgress` — отдельная визуальная проверка на демонстрационных значениях, не записываемых в прогресс игрока. Остальные снимки используют отдельное временное сохранение. Журнал: `Docs/ValidationLogs/reference-revision.log`.

После переноса проверено полное совпадение файлов `Assets` основной версии с протестированной копией (кроме временных редакторских утилит, которые в проект не переносились). SHA-256 готовой сцены: `48D7CA87D6E93F4DA3F1623F1CD44EA293971443B13896148FACB31708BE76E5`.

Остаются прежние границы: реальный рекламный SDK и платежи не подключены; мобильные APK/IPA не собирались. В headless-журнале может присутствовать внутреннее исключение индексатора `UnityEditor.Search.SearchDatabase`, не относящееся к игровой логике.

## Новые ассеты

Использован встроенный imagegen. Референсы — изображения из корня проекта. Исходные экраны не использованы как запечённый UI. PNG скопированы в проект; альфа сохранена, Unity нарезает листы на отдельные Sprite.

- `Assets/Art/SpriteSheets/reference_decor_v2.png`: Books, Cat, Ink, Lantern, Laurel, Hourglass.
- `Assets/Art/SpriteSheets/reference_icons_v2.png`: Fire, Target, Star, Calendar, Cap, Moon, Gift, FeatherBundle, FeatherBag, Video, NoAds, Sparkles.
- `Assets/Art/SpriteSheets/reference_controls_v2.png`: CreamCircle, LavenderCircle, BlueCircle, Capsule.
- `Assets/Art/SpriteSheets/reference_collection_scenes_v2.png`: шесть отдельных панорам для карточек авторов.

### Промт декора

Use case: stylized-concept. Asset type: production transparent Unity decoration sprite sheet. The two images are STYLE REFERENCES ONLY. Generate a 2-column x 3-row equal-cell atlas, 1536x1536 square or similar, genuine transparent background with alpha. SIX independent fully visible props, generously padded by 12 percent inside each cell, NO overlap between cells, no text, no UI panels, no owl, no background. Match the exact warm painterly fantasy library illustration style and rich blue/gold book bindings of the reference. Row1 LEFT: a horizontal low stack of three large antique books, navy blue with gold leaves, turquoise, and brown red, a foundation for the owl to sit on. Row1 RIGHT: a sleeping cute ginger-and-white cat curled on two green blue antique books, facing forward. Row2 LEFT: deep navy blue ornate ink bottle with golden botanical pattern and one tall ivory quill inserted, complete quill. Row2 RIGHT: a glowing amber vintage lantern next to two antique books. Row3 LEFT: one elegant curved green laurel branch with 9 leaves, like the leaves framing the score in the reference, no other elements. Row3 RIGHT: a golden wooden hourglass with amber sand and small blue book beneath it. All props isolated, no table or ground plane. Polished cohesive game art, clean edges, softly dimensional.

### Промт иконок

Use case: stylized-concept. Asset type: transparent production UI icon atlas for the exact cozy mobile game shown in the references. References are STYLE AND SUBJECT GUIDANCE, not edit targets. One 4-column by 3-row equal-cell sheet, landscape aspect 4:3, genuine transparent alpha. Exactly TWELVE separate centered icons, each contained well inside its cell with at least 15% empty gutters. NO text, numbers, labels, background panels, checkerboard or borders. Soft rounded brightly lit polished painted game art matching the references. Reading left-to-right top-to-bottom: row1: orange red flame; red white bullseye with blue arrow; golden five-point star; red blue desk calendar with blank small blue squares. Row2: navy graduation cap with gold tassel; yellow crescent moon with three little stars; red present box with gold bow; fanned bundle of FIVE ivory quill feathers tied at their base. Row3: rich royal blue cloth bag with gold botanical pattern overflowing with ivory feathers; violet movie-ticket icon with play triangle; navy blue no-ads icon as a movie tile crossed with a red circle slash WITHOUT any letters; small cluster of three golden four-point sparkles. Every icon must be completely inside its own cell. No other elements.

### Промт круглых кнопок

Use case: stylized-concept. Asset type: four blank UI button surface sprites, exact production game art. The two attached images are style references only. Generate a 2x2 square sprite atlas, true transparent background, four completely separated equal cells with 15% padding. No icons, no letters, no text, no numbers, no labels. Row1 left: perfectly circular smooth warm white cream button with a very subtle pale gold rim and soft dimensional edge, like the settings circle in reference1. Row1 right: perfectly circular pale lavender button, with soft smooth edge, like the small collection-arrow circle in reference2. Row2 left: perfectly circular bright blue button with soft cyan edge and subtle highlight. Row2 right: a smooth warm white wide rounded capsule badge, ratio 2:1, thin pastel cream border, blank interior. All surfaces viewed straight on, not in perspective, all four blank, no shadows beyond small soft contact edging, no dark/black outline, no extra decoration. Genuine transparent alpha, crisp clean edges, high quality for scaling to 100 pixels.

### Промт панорам

Use case: stylized-concept. Asset type: Unity collectible card background illustrations. Attached image is STYLE REFERENCE ONLY. Generate a 2-column by 3-row atlas of six horizontal 2:1 landscape illustrations, evenly sized and aligned, with generous transparent gutters. No characters, no portraits, no UI, no text, no frames, no cards containing labels. Each individual landscape is a fully filled rectangular painted panorama. EXACT reading order: top left: bright romantic old European city with river bridge and green leaves; top right: sunny Russian country estate with birch trees and cream mansion; middle left: purple sunset city with onion domes and lanterns; middle right: spring garden with pink blossom trees and manor house; bottom left: blue twilight fairytale village with glowing windows, moonlight and autumn leaves; bottom right: golden autumn park with yellow trees, path and a classical mansion. Match the detailed warm storybook mobile game painting style of the reference collection portraits. Each landscape separate, rectangular ratio 2:1, no overlap. Transparency only in gutters outside the six rectangles. The center lower half of each rectangle is quiet enough for a separately layered portrait.
