using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SmartGreenhouse.Core.WpCalculator
{
    public class WpFragment
    {
        public string Name { get; set; }
        public string PostCondition { get; set; }
        public string PostConditionDescription { get; set; }
        public string Code { get; set; }
    }

    public class WpCalculationResult
    {
        public List<string> TraceSteps { get; set; } = new List<string>();
        public string FinalWp { get; set; }
        public string FinalWpDescription { get; set; }
        public string Triad { get; set; }
    }

    public class WpCalculator
    {
        public static WpCalculationResult CalculateWp(WpFragment fragment)
        {
            var result = new WpCalculationResult();
            var traceSteps = new List<string>();
            string currentPost = fragment.PostCondition;

            var statements = ParseStatements(fragment.Code);
            
            for (int i = statements.Count - 1; i >= 0; i--)
            {
                var statement = statements[i];
                string previousPost = currentPost;
                string stepDescription = "";
                
                if (statement.Trim().StartsWith("if"))
                {
                    currentPost = ProcessIfStatement(statement, currentPost, traceSteps, out stepDescription);
                }
                else if (statement.Contains(":="))
                {
                    currentPost = ProcessAssignment(statement, currentPost, traceSteps, out stepDescription);
                }
                
                int stepNumber = statements.Count - i;
                traceSteps.Add($"ШАГ {stepNumber}: Через оператор '{statement.Trim()}'\n\n" +
                              $"Промежуточное условие (WP): {currentPost}\n\n" +
                              $"Расшифровка: {stepDescription}");
            }

            result.TraceSteps = traceSteps;
            result.FinalWp = SimplifyExpression(currentPost);
            result.FinalWpDescription = TranslateToHumanLanguage(result.FinalWp);
            result.Triad = $"{{ {result.FinalWp} }}\n\n{fragment.Code}\n\n{{ {fragment.PostCondition} }}";

            return result;
        }

        private static List<string> ParseStatements(string code)
        {
            var statements = new List<string>();
            code = code.Replace("\r\n", "\n").Replace("\r", "\n");
            
            string currentStatement = "";
            int braceCount = 0;
            bool inIfStatement = false;

            var lines = code.Split('\n');
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) && !inIfStatement) continue;

                if (trimmed.StartsWith("if"))
                {
                    inIfStatement = true;
                }

                if (currentStatement.Length > 0 && !inIfStatement)
                {
                    currentStatement += " ";
                }
                else if (currentStatement.Length > 0)
                {
                    currentStatement += "\n" + line;
                }
                else
                {
                    currentStatement = line;
                }

                braceCount += trimmed.Count(c => c == '{') - trimmed.Count(c => c == '}');

                if (inIfStatement && braceCount == 0)
                {
                    inIfStatement = false;
                }

                if (braceCount == 0 && currentStatement.Length > 0 && !inIfStatement)
                {
                    var stmt = currentStatement.Trim();
                    if (!string.IsNullOrEmpty(stmt))
                    {
                        if (stmt.Contains(";") && !stmt.Trim().StartsWith("if"))
                        {
                            var parts = stmt.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var part in parts)
                            {
                                var p = part.Trim();
                                if (!string.IsNullOrEmpty(p))
                                {
                                    statements.Add(p);
                                }
                            }
                        }
                        else
                        {
                            statements.Add(stmt);
                        }
                    }
                    currentStatement = "";
                }
            }

            if (currentStatement.Length > 0)
            {
                var stmt = currentStatement.Trim();
                if (!string.IsNullOrEmpty(stmt))
                {
                    if (stmt.Contains(";") && !stmt.Trim().StartsWith("if"))
                    {
                        var parts = stmt.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var part in parts)
                        {
                            var p = part.Trim();
                            if (!string.IsNullOrEmpty(p))
                            {
                                statements.Add(p);
                            }
                        }
                    }
                    else
                    {
                        statements.Add(stmt);
                    }
                }
            }

            return statements;
        }

        private static string ProcessAssignment(string statement, string postCondition, List<string> traceSteps, out string description)
        {
            description = "";
            var match = Regex.Match(statement, @"(\w+)\s*:=\s*(.+?)(?:\s*;|$)");
            if (!match.Success) return postCondition;

            string variable = match.Groups[1].Value.Trim();
            string expression = match.Groups[2].Value.Trim();

            string oldCondition = postCondition;
            string newCondition = ReplaceVariableInCondition(postCondition, variable, expression);
            
            var wellDefined = CheckWellDefined(expression);
            if (!string.IsNullOrEmpty(wellDefined))
            {
                newCondition = $"{wellDefined} && {newCondition}";
            }

            string varName = TranslateVariableName(variable);
            string exprDesc = TranslateExpression(expression);
            
            if (oldCondition != newCondition)
            {
                description = $"Применяем правило wp(x := e, R): заменяем все вхождения переменной '{varName}' на выражение '{exprDesc}' в постусловии '{oldCondition}'. " +
                             $"Получаем условие: {newCondition}";
            }
            else
            {
                description = $"Применяем правило wp(x := e, R): переменная '{varName}' не встречается в постусловии '{oldCondition}', " +
                             $"поэтому условие не изменяется и остается: {newCondition}";
            }

            return newCondition;
        }

        private static string ReplaceVariableInCondition(string condition, string variable, string expression)
        {
            var pattern = $@"\b{Regex.Escape(variable)}\b";
            return Regex.Replace(condition, pattern, $"({expression})");
        }

        private static string ProcessIfStatement(string statement, string postCondition, List<string> traceSteps, out string description)
        {
            description = "";
            
            string normalized = statement.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
            normalized = Regex.Replace(normalized, @"\s+", " ");
            
            var ifMatch = Regex.Match(normalized, @"if\s*\(([^)]+)\)\s*\{([^}]+)\}\s*else\s*\{([^}]+)\}", RegexOptions.IgnoreCase);
            if (!ifMatch.Success)
            {
                return postCondition;
            }

            string condition = ifMatch.Groups[1].Value.Trim();
            string thenBranch = ifMatch.Groups[2].Value.Trim();
            string elseBranch = ifMatch.Groups[3].Value.Trim();

            string wpThen = ProcessStatement(thenBranch, postCondition);
            string wpElse = ProcessStatement(elseBranch, postCondition);

            string result = $"({condition} && {wpThen}) || (!({condition}) && {wpElse})";
            
            description = $"Применяем правило wp(if B then S1 else S2, R) = (B ∧ wp(S1,R)) ∨ (¬B ∧ wp(S2,R)). " +
                         $"Вычисляем WP для ветки 'then': {TranslateToHumanLanguage(wpThen)}. " +
                         $"Вычисляем WP для ветки 'else': {TranslateToHumanLanguage(wpElse)}. " +
                         $"Объединяем: если {TranslateCondition(condition)}, то {TranslateToHumanLanguage(wpThen)}, иначе {TranslateToHumanLanguage(wpElse)}.";

            return result;
        }

        private static string ProcessStatement(string statement, string postCondition)
        {
            var statements = ParseStatements(statement);
            string currentPost = postCondition;
            
            for (int i = statements.Count - 1; i >= 0; i--)
            {
                var stmt = statements[i];
                if (stmt.Contains(":="))
                {
                    string dummy;
                    currentPost = ProcessAssignment(stmt, currentPost, new List<string>(), out dummy);
                }
            }
            
            return currentPost;
        }

        private static string CheckWellDefined(string expression)
        {
            var conditions = new List<string>();

            if (expression.Contains("/"))
            {
                var divMatch = Regex.Match(expression, @"([^/]+)\s*/\s*([^/]+)");
                if (divMatch.Success)
                {
                    string denominator = divMatch.Groups[2].Value.Trim();
                    string denomDesc = TranslateExpression(denominator);
                    conditions.Add($"{denominator} != 0");
                }
            }

            if (expression.Contains("abs("))
            {
                var absMatch = Regex.Match(expression, @"abs\s*\(\s*([^)]+)\s*\)");
                if (absMatch.Success)
                {
                    string arg = absMatch.Groups[1].Value.Trim();
                    conditions.Add($"{arg} определено");
                }
            }

            return conditions.Count > 0 ? string.Join(" && ", conditions) : "";
        }

        private static string SimplifyExpression(string expression)
        {
            expression = expression.Replace("(true)", "true");
            expression = expression.Replace("(false)", "false");
            expression = expression.Replace("true && ", "");
            expression = expression.Replace(" && true", "");
            expression = expression.Replace("false || ", "");
            expression = expression.Replace(" || false", "");
            
            return expression.Trim();
        }

        private static string TranslateToHumanLanguage(string expression)
        {
            string result = expression;

            result = Regex.Replace(result, @"\(([^)]+)\)\s*>\s*(\d+(?:\.\d+)?)", "($1) больше $2");
            result = Regex.Replace(result, @"\(([^)]+)\)\s*<\s*(\d+(?:\.\d+)?)", "($1) меньше $2");
            result = Regex.Replace(result, @"\(([^)]+)\)\s*>=\s*(\d+(?:\.\d+)?)", "($1) больше или равно $2");
            result = Regex.Replace(result, @"\(([^)]+)\)\s*<=\s*(\d+(?:\.\d+)?)", "($1) меньше или равно $2");
            result = Regex.Replace(result, @"\(([^)]+)\)\s*==\s*(\d+(?:\.\d+)?)", "($1) равно $2");
            result = Regex.Replace(result, @"\(([^)]+)\)\s*!=\s*(\d+(?:\.\d+)?)", "($1) не равно $2");

            result = Regex.Replace(result, @"(\w+)\s*>\s*(\d+(?:\.\d+)?)", "$1 больше $2");
            result = Regex.Replace(result, @"(\w+)\s*<\s*(\d+(?:\.\d+)?)", "$1 меньше $2");
            result = Regex.Replace(result, @"(\w+)\s*>=\s*(\d+(?:\.\d+)?)", "$1 больше или равно $2");
            result = Regex.Replace(result, @"(\w+)\s*<=\s*(\d+(?:\.\d+)?)", "$1 меньше или равно $2");
            result = Regex.Replace(result, @"(\w+)\s*==\s*(\d+(?:\.\d+)?)", "$1 равно $2");
            result = Regex.Replace(result, @"(\w+)\s*!=\s*(\d+(?:\.\d+)?)", "$1 не равно $2");
            
            result = result.Replace("&&", "и");
            result = result.Replace("||", "или");
            result = result.Replace("!", "не");
            result = result.Replace("true", "истина");
            result = result.Replace("false", "ложь");

            result = Regex.Replace(result, @"soilMoisture", "влажность почвы");
            result = Regex.Replace(result, @"temperature", "температура");
            result = Regex.Replace(result, @"co2Level", "уровень CO₂");
            result = Regex.Replace(result, @"lightIntensity", "освещенность");
            result = Regex.Replace(result, @"waterValve", "клапан полива");
            result = Regex.Replace(result, @"heater", "обогрев");
            result = Regex.Replace(result, @"ventilation", "вентиляция");
            result = Regex.Replace(result, @"lights", "освещение");

            result = Regex.Replace(result, @"\(([^)]+)\s*\+\s*(\d+(?:\.\d+)?)\)", "$1 плюс $2");
            result = Regex.Replace(result, @"\(([^)]+)\s*-\s*(\d+(?:\.\d+)?)\)", "$1 минус $2");
            result = Regex.Replace(result, @"\(([^)]+)\s*\*\s*(\d+(?:\.\d+)?)\)", "$1 умножить на $2");
            result = Regex.Replace(result, @"\(([^)]+)\s*/\s*([^)]+)\)", "$1 разделить на $2");

            return result;
        }

        private static string TranslateVariableName(string variable)
        {
            var translations = new Dictionary<string, string>
            {
                { "soilMoisture", "влажность почвы" },
                { "temperature", "температура" },
                { "co2Level", "уровень CO₂" },
                { "lightIntensity", "освещенность" },
                { "waterValve", "клапан полива" },
                { "heater", "обогрев" },
                { "ventilation", "вентиляция" },
                { "lights", "освещение" }
            };
            return translations.ContainsKey(variable) ? translations[variable] : variable;
        }

        private static string TranslateExpression(string expression)
        {
            string result = expression;
            result = Regex.Replace(result, @"soilMoisture", "влажность почвы");
            result = Regex.Replace(result, @"temperature", "температура");
            result = Regex.Replace(result, @"co2Level", "уровень CO₂");
            result = Regex.Replace(result, @"lightIntensity", "освещенность");
            result = result.Replace("+", " плюс ");
            result = result.Replace("-", " минус ");
            result = result.Replace("*", " умножить на ");
            result = result.Replace("/", " разделить на ");
            result = Regex.Replace(result, @"abs\s*\(", "модуль от ");
            result = result.Replace(")", "");
            return result.Trim();
        }

        private static string TranslateCondition(string condition)
        {
            string result = condition;
            result = Regex.Replace(result, @"soilMoisture", "влажность почвы");
            result = Regex.Replace(result, @"temperature", "температура");
            result = Regex.Replace(result, @"co2Level", "уровень CO₂");
            result = Regex.Replace(result, @"lightIntensity", "освещенность");
            result = result.Replace(">", " больше ");
            result = result.Replace("<", " меньше ");
            result = result.Replace(">=", " больше или равно ");
            result = result.Replace("<=", " меньше или равно ");
            result = result.Replace("==", " равно ");
            result = result.Replace("!=", " не равно ");
            return result;
        }
    }
}

