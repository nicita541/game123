using System;
using System.Collections.Generic;
using System.Linq;

namespace Erudition
{
    // Text generation only. Every visual cell and control is already authored in GameplayScene.
    public static class PuzzleGenerator
    {
        public static readonly int[] Thresholds = { 0, 60, 160, 320, 560 };
        public static int Tier(int erudition) => Math.Max(0, Array.FindLastIndex(Thresholds, value => value <= erudition));
        public static int HiddenLetters(int erudition, PuzzleMode mode)
            => (mode == PuzzleMode.Classic ? new[] { 2, 4, 6, 9, 12 } : new[] { 2, 3, 4, 5, 7 })[Tier(erudition)];
        public static int WordCount(string text) => text.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
        public static string TextKey(string text) => new string(text.ToUpperInvariant().Replace('Ё', 'Е').Where(char.IsLetterOrDigit).ToArray());
        public static int StartingClues(int letters, int erudition)
        {
            if (letters <= 1) return 0;
            // Scattered individual letters, like a printed cryptogram. Most of
            // the phrase (including repeated occurrences) remains to be solved.
            var fraction = new[] { .20, .18, .16, .14, .12 }[Tier(erudition)];
            return Math.Min(letters - 1, Math.Max(1, (int)Math.Round(letters * fraction)));
        }

        private static readonly string[][] Openings =
        {
            new[] { "Утро пахнет травой и дождём", "Тихий лес встречает первые лучи", "За окном просыпается старый сад", "Река несёт отражения белых облаков", "На тропинке блестят капли росы", "Тёплый ветер шевелит листву деревьев", "В траве звенит маленький ручей", "Над озером медленно поднимается туман" },
            new[] { "Доброе слово согревает лучше солнца", "Старый друг всегда найдёт время", "Дома нас ждут тепло и забота", "За большим столом собирается семья", "Искренняя улыбка делает день светлее", "Чашка чая согревает холодные ладони", "Долгая встреча начинается с улыбки", "Маленькая помощь бывает очень важной" },
            new[] { "Новая книга открывает целый мир", "Любопытство помогает замечать самые простые вещи", "На полке ждёт любимая сказка", "За каждым вопросом скрывается открытие", "Первые страницы пахнут новыми историями", "Тихая библиотека хранит много тайн", "Хорошая история начинается с вопроса", "Чтение учит видеть мир иначе" }
        };
        private static readonly string[][] Middles =
        {
            new[] { "Под старой берёзой можно послушать пение птиц", "На дальнем берегу покачиваются высокие камыши", "Солнечные пятна медленно скользят по зелёной траве", "Вдали слышен тихий шелест сосновых ветвей", "Каждая тропа ведёт к маленькому открытию", "У лесной опушки пахнет мятой и цветами", "Лёгкие облака меняют форму над самой рекой", "Даже знакомый пейзаж сегодня кажется новым" },
            new[] { "В такие минуты особенно приятно быть вместе", "Можно отложить дела и просто поговорить", "Внимание к другим делает обычный день особенным", "Самые тёплые воспоминания рождаются из простых вещей", "Рядом с близкими даже тишина кажется уютной", "Небольшой подарок напоминает о чьей-то доброте", "Для хорошего разговора не нужен особый повод", "Общая радость становится немного больше" },
            new[] { "Стоит перевернуть страницу и дать волю воображению", "Знакомые слова складываются в совершенно новые мысли", "Иногда один вопрос оказывается важнее готового ответа", "Не нужно спешить чтобы заметить интересную деталь", "Каждый день можно узнавать что-нибудь новое", "Сложная задача становится понятнее после первого шага", "Ошибки помогают искать другой путь к ответу", "В старой тетради хватает места для новых идей" }
        };
        private static readonly string[][] Endings =
        {
            new[] { "Хочется идти не спеша и замечать красоту вокруг", "Этот спокойный миг хочется сохранить в памяти", "Природа умеет удивлять даже внимательного путешественника", "Впереди ещё много незнакомых троп и тихих открытий" },
            new[] { "Хочется чаще говорить спасибо тем кто рядом", "Такие простые мгновения остаются с нами надолго", "Забота начинается с желания услышать другого человека", "Пусть в каждом дне найдётся место для доброго дела" },
            new[] { "Главное не бояться пробовать и задавать новые вопросы", "Так шаг за шагом мир становится чуть понятнее", "Маленькое открытие может стать началом большого увлечения", "Любознательность превращает обычный день в настоящее приключение" }
        };
        private static readonly string[] Short = { "Утро дарит надежду", "Книга ждёт читателя", "Добро возвращается", "Друзья всегда рядом", "Лес хранит тишину", "Мечты зовут вперёд", "Весна приносит радость", "Знание открывает двери" };

        public static PuzzleEntry Generate(PuzzleMode mode, int tier, int variant)
        {
            tier = Math.Max(0, Math.Min(4, tier));
            var theme = variant % 3;
            var a = (variant / 3) % 8;
            var b = (variant / 24 + variant * 3) % 8;
            var c = (b + 3 + variant / 8) % 8;
            if (c == b) c = (c + 1) % 8;
            var lines = new List<string>();
            if (mode == PuzzleMode.Turbo)
            {
                if (tier == 0) lines.Add(Short[variant % Short.Length]);
                else if (tier < 3) lines.Add(Openings[theme][a]);
                else { lines.Add(Openings[theme][a]); lines.Add(Middles[theme][b]); }
            }
            else
            {
                lines.Add(Openings[theme][a]);
                if (tier >= 1) lines.Add(Middles[theme][b]);
                if (tier >= 2) lines.Add(Endings[theme][variant % 4]);
                if (tier >= 3) lines.Insert(2, Middles[theme][c]);
                if (tier >= 4) lines.Add(new[] { "И завтра эта история получит своё продолжение", "Впереди нас ждёт ещё один удивительный день", "Нужно лишь остановиться и внимательно посмотреть вокруг" }[theme]);
            }
            return new PuzzleEntry
            {
                id = "generated_v1_" + (int)mode + "_" + tier + "_" + variant,
                mode = mode, text = string.Join(". ", lines) + ".", source = "Истории Совушки · " + new[] { "Природа", "Тепло рядом", "Мир открытий" }[theme],
                minimumErudition = Thresholds[tier], authorIndex = -1, themeIndex = theme, bookIndex = theme
            };
        }

        public static PuzzleEntry[] Build(PuzzleLibrary library)
        {
            var entries = new List<PuzzleEntry>(library.entries);
            // Stable order and IDs preserve saved progress across launches.
            for (var mode = 0; mode < 2; mode++)
                for (var tier = 0; tier < Thresholds.Length; tier++)
                    for (var variant = 0; variant < 96; variant++) entries.Add(Generate((PuzzleMode)mode, tier, variant));
            entries.AddRange(StoryCatalog.Build());
            return entries.ToArray();
        }
    }
}
