using System;
using System.Collections.Generic;

namespace SmartGreenhouse.Core.WpCalculator
{
    /// <summary>
    /// ЛР2: Движок слабейшего предусловия для планирования действий
    /// </summary>
    public static class WpEngine
    {
        // ЛР2: WP для присваивания
        public static string WpAssign(string variable, string expression, string postCondition)
        {
            // Заменяем переменную в postCondition на expression
            return postCondition.Replace(variable, $"({expression})");
        }

        // ЛР2: WP для последовательности
        public static string WpSequence(List<string> statements, string postCondition)
        {
            string currentCondition = postCondition;

            // Тянем условие с конца к началу
            for (int i = statements.Count - 1; i >= 0; i--)
            {
                currentCondition = WpStatement(statements[i], currentCondition);
            }

            return currentCondition;
        }

        // ЛР2: WP для условного оператора
        public static string WpIf(string condition, string thenBranch, string elseBranch, string postCondition)
        {
            string wpThen = WpStatement(thenBranch, postCondition);
            string wpElse = WpStatement(elseBranch, postCondition);

            return $"({condition} && {wpThen}) || (!{condition} && {wpElse})";
        }

        public static string WpStatement(string statement, string postCondition)
        {
            if (statement.Contains(":="))
            {
                var parts = statement.Split(new[] { ":=" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    return WpAssign(parts[0].Trim(), parts[1].Trim(), postCondition);
                }
            }

            return postCondition;
        }

        // ЛР2: Пример использования для теплицы
        public static string CalculateWateringPrecondition()
        {
            var statements = new List<string>
            {
                "valveOpen := true",
                "waterFlow := 5.0",
                "timerStart := currentTime"
            };

            string postCondition = "valveOpen && waterFlow > 0 && timerStart <= currentTime";

            return WpSequence(statements, postCondition);
        }
    }
}