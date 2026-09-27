# Дополнительные UI-спрайты по референсам

Использован встроенный imagegen; API/CLI не применялся. Референсы: `01_main_menu.png`, `03_gameplay_classic.png` и предоставленный пользователем `sprite_sheet_user_reference.png`.

Итоговые изображения находятся внутри проекта и используются объектами сцены:

- `Assets/Art/SpriteSheets/ui_surfaces_reference.png`: GreenButton, PurpleButton, BlueButton, GoldButton, Parchment, CreamCard, KeyboardKey, TitleRibbon.
- `Assets/Art/SpriteSheets/ui_icons_reference.png`: Feather, Crown, Heart, Bulb, Book, Lightning, Gear, Statistics, Home, Trophy, Shop, Coin.
- `Assets/Art/owl_victory_v2.png`, `owl_sad_v2.png`, `owl_shop_v2.png`: три отдельных прозрачных спрайта совы; они заменяют в сцене позы из листа, где соседние рисунки попадали в прямоугольники нарезки.

Изображения нарезаны через Unity TextureImporter; пиксели исходных PNG после генерации не перерисовывались. Альфа-канал сохранён. Надписи находятся в Text-компонентах сцены. Ранее созданные листы авторов и коллекций также подключены; старый лист состояний совы сохранён как исходный материал.

## Промт поверхностей

Use case: stylized-concept. Asset type: production transparent UI sprite atlas for this exact cozy Russian cryptogram mobile game. The two input images are STYLE REFERENCES ONLY, not edit targets. Generate a clean 2-column by 4-row atlas on genuine transparent alpha, 1536x2048 or similar portrait size. Each cell has generous transparent gutter, all eight sprites are isolated, no overlap, no text, no symbols, no letters, no numbers. Match the soft painted polished fantasy UI of the references, warm ivory outlines, subtle dimensional shading. EXACT grid reading left-to-right, top-to-bottom: Row 1: wide emerald green rounded rectangular main button; wide violet purple rounded rectangular main button. Row 2: royal blue wide rounded button; warm golden amber wide rounded button. Row 3: large blank cream parchment panel with softly torn edges and faint gold leaf corners, lots of empty center; blank softly rounded warm ivory card with subtle bevel and shadow. Row 4: blank white lavender-tinted rounded keyboard key, square; blank cream parchment title ribbon/banner. All content strictly centered inside its equal-size cell with at least 40px gutter. Buttons ratio about 3:1, no fixed icons. All panels should be adaptable as Unity sliced sprites. Preserve genuinely transparent background, no checkerboard texture, no mockup, no device, no background scene.

## Промт иконок

Use case: stylized-concept. Asset type: transparent UI icon sprite atlas for the exact cozy library cryptogram game shown in the three style references. Generate one 4-column x 3-row sprite sheet, equal cells, isolated icons each centered with generous 20% transparent gutters. Genuine transparent alpha, no checkerboard, no background, no labels, no text, no numbers, no circles/tiles surrounding icons. Match references: softly painted dimensional mobile game UI, rounded shapes, warm light, clean silhouette, gold and pastel accents. EXACT grid: row1 left-to-right: ivory feather quill with gold stem; golden crown; coral red heart; glowing golden lightbulb. Row2: open cream book with brown cover; golden lightning bolt; purple blue settings gear; blue statistics bars. Row3: blue home house icon; golden achievement trophy; small shop front with pink striped canopy; shiny golden coin without letter or currency symbol. All 12 icons separate and entirely inside their cells. No extra decorations outside silhouettes. Detailed enough for polished mobile game, readable at 64px.

## Промты отдельных поз совы

Общий промт (вместо POSE — один из трёх вариантов ниже), референс `Assets/Art/owl_mascot.png`:

Use case: identity-preserve. Asset type: one isolated transparent PNG character sprite for a Unity mobile game. The attached owl is the exact character identity and style reference. Create ONE owl only, preserve its brown and ivory feathers, tufted ears, huge amber eyes, round thin dark glasses, orange beak and feet, soft warmly lit painted style. Pose: POSE Center the complete owl with generous transparent padding on all four sides: every wingtip, sparkle, feather and toe must fit comfortably in the image, with 15 percent margin. Genuinely transparent alpha background. Square image. No text, no panels, no scene, no collage, no duplicate characters, no cropped body parts. Match the supplied owl rather than designing another character.

- Victory: Happy victory pose, both wings raised in celebration, three small golden sparkles close to the owl. No book.
- Sad: Gently disappointed but adorable, wings lowered, slightly droopy eyebrows, one ivory feather resting beside its feet. No book, no tears.
- Shop: Cheerful welcoming pose holding a large ivory quill in one wing, the other wing open to welcome the player. No book, no other props.
