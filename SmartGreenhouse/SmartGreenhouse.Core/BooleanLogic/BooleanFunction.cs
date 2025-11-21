using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartGreenhouse.Core.BooleanLogic
{
    /// <summary>
    /// ЛР4: Класс для работы с булевыми функциями
    /// </summary>
    public class BooleanFunction
    {
        private readonly Func<bool[], bool> _evaluator;
        private readonly int _variableCount;
        private readonly string[] _variableNames;

        public BooleanFunction(Func<bool[], bool> evaluator, int variableCount, string[] variableNames = null)
        {
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _variableCount = variableCount;
            
            if (variableNames != null && variableNames.Length == variableCount)
            {
                _variableNames = variableNames;
            }
            else
            {
                _variableNames = Enumerable.Range(0, variableCount).Select(i => $"x{i + 1}").ToArray();
            }
        }

        /// <summary>
        /// Создает функцию по номеру (num ∈ [0..2^(2^n)−1])
        /// </summary>
        public static BooleanFunction FromNumber(int n, int num)
        {
            if (n < 1 || n > 10)
                throw new ArgumentException("Количество переменных должно быть от 1 до 10");
            
            int maxNum = (1 << (1 << n)) - 1;
            if (num < 0 || num > maxNum)
                throw new ArgumentException($"Номер функции должен быть от 0 до {maxNum}");

            int rowCount = 1 << n;
            bool[] truthTable = new bool[rowCount];
            
            for (int i = 0; i < rowCount; i++)
            {
                truthTable[i] = (num & (1 << i)) != 0;
            }

            return new BooleanFunction(
                (values) => {
                    int index = 0;
                    for (int i = 0; i < n; i++)
                    {
                        if (values[i]) index |= (1 << i);
                    }
                    return truthTable[index];
                },
                n
            );
        }

        /// <summary>
        /// Создает функцию из формулы
        /// </summary>
        public static BooleanFunction FromFormula(string formula)
        {
            var parser = new BooleanFunctionParser();
            var evaluator = parser.Parse(formula);
            var variables = BooleanFunctionParser.GetVariables(formula);
            
            int varCount = variables.Count;
            string[] varNames = variables.ToArray();
            
            return new BooleanFunction(evaluator, varCount, varNames);
        }

        /// <summary>
        /// Вычисляет значение функции для заданных значений переменных
        /// </summary>
        public bool Evaluate(bool[] values)
        {
            if (values == null || values.Length != _variableCount)
                throw new ArgumentException($"Требуется {_variableCount} значений переменных");
            
            return _evaluator(values);
        }

        /// <summary>
        /// Строит таблицу истинности
        /// </summary>
        public TruthTable BuildTruthTable()
        {
            var table = new TruthTable(_variableCount, _variableNames);
            int rowCount = 1 << _variableCount;

            for (int i = 0; i < rowCount; i++)
            {
                bool[] values = new bool[_variableCount];
                for (int j = 0; j < _variableCount; j++)
                {
                    values[j] = (i & (1 << j)) != 0;
                }
                bool result = Evaluate(values);
                table.AddRow(values, result);
            }

            return table;
        }

        /// <summary>
        /// Генерирует DNF (дизъюнктивная нормальная форма) в базисе {¬, ∧, ∨}
        /// </summary>
        public string GenerateDNF()
        {
            var table = BuildTruthTable();
            var terms = new List<string>();

            foreach (var row in table.Rows)
            {
                if (row.Result)
                {
                    var conjunct = new List<string>();
                    for (int i = 0; i < _variableCount; i++)
                    {
                        if (row.Values[i])
                        {
                            conjunct.Add(_variableNames[i]);
                        }
                        else
                        {
                            conjunct.Add($"¬{_variableNames[i]}");
                        }
                    }
                    terms.Add($"({string.Join(" ∧ ", conjunct)})");
                }
            }

            if (terms.Count == 0)
                return "0"; // Константа ложь
            
            return string.Join(" ∨ ", terms);
        }

        /// <summary>
        /// Генерирует KNF (конъюнктивная нормальная форма) в базисе {¬, ∧, ∨}
        /// </summary>
        public string GenerateKNF()
        {
            var table = BuildTruthTable();
            var terms = new List<string>();

            foreach (var row in table.Rows)
            {
                if (!row.Result)
                {
                    var disjunct = new List<string>();
                    for (int i = 0; i < _variableCount; i++)
                    {
                        if (row.Values[i])
                        {
                            disjunct.Add($"¬{_variableNames[i]}");
                        }
                        else
                        {
                            disjunct.Add(_variableNames[i]);
                        }
                    }
                    terms.Add($"({string.Join(" ∨ ", disjunct)})");
                }
            }

            if (terms.Count == 0)
                return "1"; // Константа истина
            
            return string.Join(" ∧ ", terms);
        }

        /// <summary>
        /// Проверяет эквивалентность двух функций
        /// </summary>
        public static (bool Equivalent, string CounterExample) CheckEquivalence(BooleanFunction f1, BooleanFunction f2)
        {
            if (f1._variableCount != f2._variableCount)
                return (false, "Разное количество переменных");

            int rowCount = 1 << f1._variableCount;
            for (int i = 0; i < rowCount; i++)
            {
                bool[] values = new bool[f1._variableCount];
                for (int j = 0; j < f1._variableCount; j++)
                {
                    values[j] = (i & (1 << j)) != 0;
                }
                
                bool result1 = f1.Evaluate(values);
                bool result2 = f2.Evaluate(values);
                
                if (result1 != result2)
                {
                    var counterExample = string.Join(", ", values.Select((v, idx) => $"{f1._variableNames[idx]}={(v ? "1" : "0")}"));
                    return (false, counterExample);
                }
            }

            return (true, null);
        }

        /// <summary>
        /// Подсчитывает стоимость формулы (литералы, конъюнкты, дизъюнкты)
        /// </summary>
        public static FormulaCost CalculateCost(string formula)
        {
            // Упрощенный подсчет - считаем операторы и переменные
            int literals = 0;
            int conjunctions = 0;
            int disjunctions = 0;

            // Подсчет литералов (переменных и их отрицаний)
            var variables = BooleanFunctionParser.GetVariables(formula);
            literals = variables.Count * 2; // Каждая переменная может быть с отрицанием или без

            // Подсчет операций
            string normalized = formula.Replace(" ", "").ToLower();
            conjunctions = CountOccurrences(normalized, "&") + CountOccurrences(normalized, "and");
            disjunctions = CountOccurrences(normalized, "|") + CountOccurrences(normalized, "or");

            return new FormulaCost
            {
                Literals = literals,
                Conjunctions = conjunctions,
                Disjunctions = disjunctions
            };
        }

        private static int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                count++;
                index += pattern.Length;
            }
            return count;
        }

        public int VariableCount => _variableCount;
        public string[] VariableNames => _variableNames;
    }

    /// <summary>
    /// Таблица истинности
    /// </summary>
    public class TruthTable
    {
        public List<TruthTableRow> Rows { get; } = new List<TruthTableRow>();
        public int VariableCount { get; }
        public string[] VariableNames { get; }

        public TruthTable(int variableCount, string[] variableNames)
        {
            VariableCount = variableCount;
            VariableNames = variableNames ?? Enumerable.Range(0, variableCount).Select(i => $"x{i + 1}").ToArray();
        }

        public void AddRow(bool[] values, bool result)
        {
            Rows.Add(new TruthTableRow { Values = (bool[])values.Clone(), Result = result });
        }
    }

    public class TruthTableRow
    {
        public bool[] Values { get; set; }
        public bool Result { get; set; }
    }

    public class FormulaCost
    {
        public int Literals { get; set; }
        public int Conjunctions { get; set; }
        public int Disjunctions { get; set; }
    }
}

