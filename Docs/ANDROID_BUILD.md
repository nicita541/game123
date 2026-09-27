# APK для установки на телефон

Для новой сборки текущего проекта сохраните сцены, закройте Unity и запустите `Build-Android.cmd` в корне проекта. Инструкция: [BUILD_SCRIPT.md](BUILD_SCRIPT.md). Ниже сохранены отчёты и способ сборки предыдущих APK.

Актуальная сборка: `F:/game/Builds/Android/Erudition-0.1.5-arm64.apk` (113,7 МБ, versionCode 6). Исправлены переход к следующей клетке и количество стартовых букв, добавлена автоматическая реклама после случайного числа побед, заменены музыка и звуки. У «Классики» восстановлено прежнее зелёное оформление, изменено только положение. Результат: Succeeded, 0 ошибок; подпись и выравнивание проверены. Описание и контрольная сумма: `Builds/Android/BUILD-0.1.5.md`. Журналы: `Docs/ValidationLogs/next-cell-apk.log` и `Docs/ValidationLogs/next-cell-tests.log`. На физическом телефоне не проверялась; рекламные блоки тестовые.

Предыдущая сборка 0.1.4 и её отчёт сохранены в `Builds/Android`.

Ниже сохранён отчёт предыдущей версии 0.1.2.

Готовый файл: `F:/game/Builds/Android/Erudition-0.1.2-arm64.apk`.
Размер: 109 023 690 байт (109,0 МБ / 104,0 МиБ).

Включены исправления перьев, сложности, повторов и ручной расстановки букв, SDK Яндекса с тестовым rewarded-блоком, сохранение ручной компоновки сцен и возврат прежнего магазина/рисованных кнопок. Предыдущие APK 0.1.0 и 0.1.1 оставлены в папке как архив.

Параметры сборки:

- Unity 6000.3.8f1, Android IL2CPP, ARM64.
- Идентификатор: `com.owlberrystudio.erudition`; версия `0.1.2`, код версии `3`.
- Минимальная версия Android: 8.0 / API 26.
- Целевая версия Android: API 36; графика OpenGL ES 3.0, Vulkan необязателен.
- Обычный APK, не AAB; данные внутри APK, без отдельного OBB.
- В сборке включены MainScene и GameplayScene. Интерфейс остаётся сохранённым в сценах.
- Development Build выключен; для установки вне магазина используется стандартная тестовая подпись Android SDK. Это не релизная подпись для публикации в Google Play.
- Имитация rewarded-рекламы и инструменты разработчика отключены. Подключён официальный Yandex Mobile Ads Unity Lite SDK 8.2.0; пока используется `demo-rewarded-yandex`. Перед публикацией нужен собственный ID блока и проверка показа на телефоне. Секретные ключи для этого не нужны.

Сборка выполняется в отдельной копии `F:/game/_feedback_validation`. Только в копии Input Handling переключён с Both на старый Input Manager: проект использует StandaloneInputModule, а пакет нового Input System отсутствует. Исправленные и проверенные сцены перенесены в основной проект с сохранением пользовательских правок; Input Handling основного проекта оставлен Both.

`Assets/Scripts/link.xml` сохраняет поля и конструкторы GameSave, PuzzleProgress и ActivityDay для XML-сохранений при IL2CPP stripping, а также Java-callback-классы SDK Яндекса.

Повторяемая точка входа сборки: `Tools/Build/AndroidApkBuild.cs`. Для запуска в изолированной копии скопировать файл в `Assets/Editor`, установить Input Handling = Input Manager и запустить Unity с `-batchmode -buildTarget Android -executeMethod AndroidApkBuild.Run -apkOutput <абсолютный-путь.apk>`. Подписи пользовательских ключей не читаются и не используются.

Лог актуальной сборки: `Docs/ValidationLogs/scene-editable-apk.log`. Лог Play Mode: `Docs/ValidationLogs/scene-editable-tests.log`. Снимки: `Review/SceneEditable` (41 экран/состояние).

## Результат проверки

- Unity BuildReport: `Succeeded`, 0 ошибок, 3 предупреждения о неиспользуемых полях заглушек стороннего SDK.
- `apksigner verify --verbose --print-certs`: `Verifies`, подпись APK Signature Scheme v2 действительна, сертификат Android Debug.
- `zipalign -c -P 16 4`: успешно.
- `aapt dump badging`: правильные название приложения, пакет, версия 0.1.2/code 3, minSdk 26, targetSdk 36, запускаемая UnityPlayerActivity и архитектура arm64-v8a. Разрешения: VIBRATE, INTERNET, ACCESS_NETWORK_STATE, AD_ID, служебные receiver/install-referrer разрешения рекламного SDK.
- В APK присутствуют игровые данные, IL2CPP metadata и библиотеки libunity, libil2cpp, libmain, libc++_shared, libswappywrapper.
- Сцены, скрипты и фоны в сборочной копии побайтово совпадают с основным проектом.
- 978 объектов сохранили Edit Mode-геометрию после Awake и полного игрового цикла; создание игрового UI не происходит.
- На физический телефон APK не устанавливался; запуск, управление и полный просмотр рекламы на устройстве ещё не проверены. Редакторские тесты не заменяют эту проверку.

SHA-256 APK:

```text
C588B1F3A769467B49DA1A207F1C41C8853B8244BB4EFC5090D9520E95BE7227
```

Для установки перенести APK на совместимый телефон, открыть файл и при запросе разрешить установку для используемого файлового менеджера или браузера. Пакет подписан тем же тестовым сертификатом, что 0.1.0 и 0.1.1, поэтому допускает обновление поверх этих версий без удаления приложения. SHA-256 сертификата: `c15a5da006f511217cfc6dfb6b30d21d43162597457f982cf6d93353e5a15297`. Сборка не содержит ключа для публикации в магазине.

## Особенность этого компьютера

Первая попытка прошла компиляцию IL2CPP, но упала в CMake/Prefab: служебный BAT-файл Gradle искажал кириллицу в пути `C:/Users/Никита/.gradle`, и Java не находила `com.google.prefab.cli.AppKt`. Лог сохранён отдельно в `Docs/ValidationLogs/android-apk-first-attempt.log`.

Для актуальной сборки переменная `GRADLE_USER_HOME` задана только процессу сборки: `F:/game/_feedback_validation/GradleCache`. Пользовательские настройки Gradle и системные переменные не менялись. Назначение этой переменной описано в [официальной документации Gradle](https://docs.gradle.org/current/userguide/build_environment.html#sec:gradle_environment_variables).
