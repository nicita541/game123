# Официальные источники

Проверка: 28 сентября 2026 года. Названия пунктов и доступность функций зависят от типа приложения, региона и аккаунта. Часть старых Android-страниц Huawei возвращает пустой документ без содержимого; точные лимиты текущих Android-полей не подтверждены через открытый веб.

1. [Huawei AppGallery](https://developer.huawei.com/consumer/en/appgallery/) — официальный вход к распространению приложений.
2. [AppGallery Connect](https://developer.huawei.com/consumer/en/agconnect/) — кабинет публикации.
3. [Overall Process — AGC Help Center](https://developer.huawei.com/consumer/es/doc/app/agc-help-process-0000001146716611) — регистрация разработчика и возможность создать приложение без проекта при отсутствии используемых сервисов Huawei.
4. [App Signing for a New App](https://developer.huawei.com/consumer/en/doc/appgallery-connect-guides/agc-appsigning-newapp-0000001052418290) — схемы управления ключами. Страница индексируется, но прямой веб-просмотр вернул пустой текст; детали выбранной схемы проверить в кабинете.
5. [Uploading an Android App Package](https://developer.huawei.com/consumer/en/doc/appgallery-connect-Guides/agcapi-circleci-uploadfile-0000001262051553) — официальный Android-пример загрузки APK/AAB. Это техническая документация API, а не инструкция включать CI/CD для этой игры.
6. [Localized Game Information](https://developer.huawei.com/consumer/en/doc/app/agc-help-release-game-intro-0000002400209657) — справка о локализованных игровых описаниях, упоминает до 80 символов краткого текста для языков, кроме китайского. Подготовленный текст укладывается в 80; точное поле Android следует проверить в кабинете.
7. [Asset Specifications — HarmonyOS 5 or Later](https://developer.huawei.com/consumer/es/doc/app/agc-help-app-visual-asset-spec-0000002277607976) — **только контекст**. Эта таблица не подтверждает Android-требования; не переносить её автоматически на APK.
8. [Yandex Mobile Ads Unity SDK 8: GDPR](https://ads.yandex.com/helpcenter/ru/dev/unity/gdpr) — назначение SetUserConsent; отсутствие согласия не является универсальной гарантией отсутствия любых данных.
9. [Yandex Mobile Ads Unity SDK 8: COPPA](https://ads.yandex.com/helpcenter/ru/dev/unity/coppa) — настройки возрастных ограничений; использовать по фактической аудитории.
10. [Yandex: безопасность и конфиденциальность](https://ads.yandex.com/helpcenter/ru/easy/integration/unity/advanced-settings/security-privacy) — категории данных для конфигурации по умолчанию в руководстве Простой монетизации. Поведение прямой интеграции проекта нужно подтвердить отдельно.
11. [AppMetrica: безопасность и соблюдение политик](https://appmetrica.yandex.com/docs/ru/new-users/data-security) и [сведения о данных Android SDK](https://appmetrica.yandex.com/docs/en/data-security/google-data-safety) — для проверки компонентов AppMetrica; требования формы Google Play не выдаются за поля Huawei.
12. [Политика конфиденциальности Яндекса](https://yandex.ru/legal/confidential/) — политика стороннего сервиса; не заменяет политику игры.

Игровые функции, текущая версия, реклама, локальные сохранения и технические ограничения проверялись по локальным исходникам и документам, перечисленным в `TECHNICAL_AUDIT.md`.
