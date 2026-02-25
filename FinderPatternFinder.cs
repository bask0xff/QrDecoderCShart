    using System;
    using System.Collections.Generic;

namespace QrDecoderCShart
{
    public class FinderPattern
    {
        public float X { get; }
        public float Y { get; }
        public float EstimatedModuleSize { get; }

        public FinderPattern(float x, float y, float moduleSize)
        {
            X = x;
            Y = y;
            EstimatedModuleSize = moduleSize;
        }

        public override string ToString() => $"({X:F1}, {Y:F1}) module≈{EstimatedModuleSize:F2}";
    }

    public static class FinderPatternFinder
    {
        // Допуск на соотношения 1:1:3:1:1 (по ISO ±50%, но мы возьмём строже ~30-40%)
        private const float MIN_RATIO = 0.7f;   // 1 → min 0.7
        private const float MAX_RATIO = 1.4f;   // 1 → max 1.4
        private const float CENTER3_MIN = 2.5f; // 3 → min 2.5
        private const float CENTER3_MAX = 3.8f; // 3 → max 3.8

        /// <summary>
        /// Ищет три finder pattern на бинарной матрице
        /// </summary>
        public static List<FinderPattern> Find(bool[,] grid, int width, int height)
        {
            var candidates = new List<FinderPattern>();

            // Проходим по каждой строке
            for (int y = 5; y < height - 5; y++) // пропускаем края, чтобы не вылезти
            {
                var runs = GetHorizontalRuns(grid, y, width);

                for (int i = 0; i < runs.Count - 4; i++)
                {
                    // Ищем последовательность: чёрный-белый-чёрный-белый-чёрный
                    if (runs[i].IsBlack &&
                        !runs[i + 1].IsBlack &&
                        runs[i + 2].IsBlack &&
                        !runs[i + 3].IsBlack &&
                        runs[i + 4].IsBlack)
                    {
                        float state1 = runs[i].Length;
                        float state2 = runs[i + 1].Length;
                        float state3 = runs[i + 2].Length;
                        float state4 = runs[i + 3].Length;
                        float state5 = runs[i + 4].Length;

                        // Нормализуем относительно state3 (центральный чёрный ~3)
                        float modSize = state3 / 3f;

                        // Проверяем соотношения
                        if (state1 / modSize >= MIN_RATIO && state1 / modSize <= MAX_RATIO &&
                            state2 / modSize >= MIN_RATIO && state2 / modSize <= MAX_RATIO &&
                            state3 / modSize >= CENTER3_MIN && state3 / modSize <= CENTER3_MAX &&
                            state4 / modSize >= MIN_RATIO && state4 / modSize <= MAX_RATIO &&
                            state5 / modSize >= MIN_RATIO && state5 / modSize <= MAX_RATIO)
                        {
                            // Примерный центр по горизонтали
                            int centerX = runs[i].Start + (int)(state1 + state2 + state3 / 2);
                            centerX /= 2; // грубо центр

                            // Проверяем вертикально в этой же колонке
                            if (CheckVerticalPattern(grid, centerX, y, modSize, height))
                            {
                                candidates.Add(new FinderPattern(centerX, y, modSize));
                            }
                        }
                    }
                }
            }

            // Теперь нужно отфильтровать дубликаты и найти 3 лучших (по размеру, расстоянию и т.д.)
            // Пока просто берём уникальные по позиции (грубый вариант)
            var unique = new List<FinderPattern>();
            foreach (var cand in candidates)
            {
                bool tooClose = false;
                foreach (var ex in unique)
                {
                    float dist = (float)Math.Sqrt(Math.Pow(cand.X - ex.X, 2) + Math.Pow(cand.Y - ex.Y, 2));
                    if (dist < ex.EstimatedModuleSize * 5) // слишком близко
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (!tooClose)
                    unique.Add(cand);
            }

            // Ожидаем ровно 3, но пока вернём все кандидаты
            return unique.Count >= 3 ? unique.GetRange(0, Math.Min(3, unique.Count)) : unique;
        }

        private static bool CheckVerticalPattern(bool[,] grid, int x, int centerY, float approxModSize, int height)
        {
            int mod = (int)(approxModSize + 0.5f);
            if (mod < 3) return false;

            // Проверяем примерно ± 3.5 модуля вверх и вниз от centerY
            int top = Math.Max(0, centerY - mod * 4);
            int bottom = Math.Min(height - 1, centerY + mod * 4);

            var vRuns = GetVerticalRuns(grid, x, top, bottom - top + 1);

            foreach (var run in vRuns)
            {
                // Аналогично ищем 1:1:3:1:1 по вертикали
                // ... (можно скопировать логику из горизонтального поиска, но упрощённо)
                // Пока просто проверяем, что в районе есть большой чёрный блок
                if (Math.Abs(run.Length - approxModSize * 3) < approxModSize * 1.2f &&
                    run.Start <= centerY && run.Start + run.Length >= centerY)
                {
                    return true; // грубая проверка — улучшить позже
                }
            }
            return false;
        }

        // Помощник: runs по горизонтали в строке y
        private static List<Run> GetHorizontalRuns(bool[,] grid, int y, int width)
        {
            var runs = new List<Run>();
            int start = 0;
            bool isBlack = grid[0, y];

            for (int x = 1; x < width; x++)
            {
                if (grid[x, y] != isBlack)
                {
                    runs.Add(new Run(start, x - start, isBlack));
                    start = x;
                    isBlack = !isBlack;
                }
            }
            runs.Add(new Run(start, width - start, isBlack));
            return runs;
        }

        // Аналогично по вертикали в столбце x, от startY, count строк
        private static List<Run> GetVerticalRuns(bool[,] grid, int x, int startY, int count)
        {
            var runs = new List<Run>();
            int y = startY;
            bool isBlack = grid[x, y];
            int runStart = y;

            for (int i = 1; i < count; i++)
            {
                y = startY + i;
                if (grid[x, y] != isBlack)
                {
                    runs.Add(new Run(runStart, y - runStart, isBlack));
                    runStart = y;
                    isBlack = !isBlack;
                }
            }
            runs.Add(new Run(runStart, (startY + count) - runStart, isBlack));
            return runs;
        }

        private class Run
        {
            public int Start { get; }
            public int Length { get; }
            public bool IsBlack { get; }

            public Run(int start, int length, bool isBlack)
            {
                Start = start;
                Length = length;
                IsBlack = isBlack;
            }
        }
    }
}