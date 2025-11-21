using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartGreenhouse.Core.BooleanLogic
{
    /// <summary>
    /// ЛР4: Парсер булевых формул с поддержкой операций: !/not, &/and, |/or, ^/xor, ->/импликация, =/эквивалентность
    /// </summary>
    public class BooleanFunctionParser
    {
        public enum TokenType
        {
            Variable,
            Not,
            And,
            Or,
            Xor,
            Implication,
            Equivalence,
            LeftParen,
            RightParen,
            End
        }

        public class Token
        {
            public TokenType Type { get; set; }
            public string Value { get; set; }
        }

        private string _input;
        private int _position;
        private List<Token> _tokens;

        /// <summary>
        /// Парсит формулу и возвращает функцию для вычисления значения
        /// </summary>
        public Func<bool[], bool> Parse(string formula)
        {
            if (string.IsNullOrWhiteSpace(formula))
                throw new ArgumentException("Формула не может быть пустой");

            _input = formula.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");
            _position = 0;
            _tokens = Tokenize(_input);
            _position = 0;

            var rpn = ShuntingYard();
            return BuildEvaluator(rpn);
        }

        /// <summary>
        /// Лексер - разбивает строку на токены
        /// </summary>
        public List<Token> Tokenize(string input)
        {
            var tokens = new List<Token>();
            int i = 0;

            while (i < input.Length)
            {
                if (char.IsLetter(input[i]) || input[i] == 'x')
                {
                    // Переменная (x1, x2, x3, ... или A, B, C, ...)
                    var sb = new StringBuilder();
                    while (i < input.Length && (char.IsLetterOrDigit(input[i]) || input[i] == 'x'))
                    {
                        sb.Append(input[i]);
                        i++;
                    }
                    tokens.Add(new Token { Type = TokenType.Variable, Value = sb.ToString() });
                }
                else if (input[i] == '!')
                {
                    tokens.Add(new Token { Type = TokenType.Not, Value = "!" });
                    i++;
                }
                else if (input[i] == '&' || (i < input.Length - 2 && input.Substring(i, 3).ToLower() == "and"))
                {
                    tokens.Add(new Token { Type = TokenType.And, Value = "&" });
                    i += input[i] == '&' ? 1 : 3;
                }
                else if (input[i] == '|' || (i < input.Length - 1 && input.Substring(i, 2).ToLower() == "or"))
                {
                    tokens.Add(new Token { Type = TokenType.Or, Value = "|" });
                    i += input[i] == '|' ? 1 : 2;
                }
                else if (input[i] == '^' || (i < input.Length - 2 && input.Substring(i, 3).ToLower() == "xor"))
                {
                    tokens.Add(new Token { Type = TokenType.Xor, Value = "^" });
                    i += input[i] == '^' ? 1 : 3;
                }
                else if (input[i] == '-' && i < input.Length - 1 && input[i + 1] == '>')
                {
                    tokens.Add(new Token { Type = TokenType.Implication, Value = "->" });
                    i += 2;
                }
                else if (input[i] == '=' && (i == 0 || input[i - 1] != '-' && input[i - 1] != '<' && input[i - 1] != '>'))
                {
                    tokens.Add(new Token { Type = TokenType.Equivalence, Value = "=" });
                    i++;
                }
                else if (input[i] == '(')
                {
                    tokens.Add(new Token { Type = TokenType.LeftParen, Value = "(" });
                    i++;
                }
                else if (input[i] == ')')
                {
                    tokens.Add(new Token { Type = TokenType.RightParen, Value = ")" });
                    i++;
                }
                else
                {
                    throw new ArgumentException($"Неожиданный символ: {input[i]} на позиции {i}");
                }
            }

            tokens.Add(new Token { Type = TokenType.End, Value = "" });
            return tokens;
        }

        /// <summary>
        /// Алгоритм Shunting Yard для преобразования в обратную польскую запись
        /// </summary>
        private List<Token> ShuntingYard()
        {
            var output = new List<Token>();
            var operators = new Stack<Token>();

            while (_position < _tokens.Count && _tokens[_position].Type != TokenType.End)
            {
                var token = _tokens[_position++];

                if (token.Type == TokenType.Variable)
                {
                    output.Add(token);
                }
                else if (token.Type == TokenType.LeftParen)
                {
                    operators.Push(token);
                }
                else if (token.Type == TokenType.RightParen)
                {
                    while (operators.Count > 0 && operators.Peek().Type != TokenType.LeftParen)
                    {
                        output.Add(operators.Pop());
                    }
                    if (operators.Count > 0 && operators.Peek().Type == TokenType.LeftParen)
                    {
                        operators.Pop();
                    }
                }
                else if (IsOperator(token.Type))
                {
                    while (operators.Count > 0 && operators.Peek().Type != TokenType.LeftParen &&
                           GetPrecedence(operators.Peek().Type) >= GetPrecedence(token.Type))
                    {
                        output.Add(operators.Pop());
                    }
                    operators.Push(token);
                }
            }

            while (operators.Count > 0)
            {
                if (operators.Peek().Type == TokenType.LeftParen)
                    throw new ArgumentException("Несбалансированные скобки");
                output.Add(operators.Pop());
            }

            return output;
        }

        private bool IsOperator(TokenType type)
        {
            return type == TokenType.Not || type == TokenType.And || type == TokenType.Or ||
                   type == TokenType.Xor || type == TokenType.Implication || type == TokenType.Equivalence;
        }

        private int GetPrecedence(TokenType type)
        {
            switch (type)
            {
                case TokenType.Not: return 5;
                case TokenType.And: return 4;
                case TokenType.Or: return 3;
                case TokenType.Xor: return 3;
                case TokenType.Implication: return 2;
                case TokenType.Equivalence: return 1;
                default: return 0;
            }
        }

        /// <summary>
        /// Строит функцию-вычислитель из ОПЗ
        /// </summary>
        private Func<bool[], bool> BuildEvaluator(List<Token> rpn)
        {
            var variables = new Dictionary<string, int>();
            int varIndex = 0;

            // Собираем все переменные
            foreach (var token in rpn)
            {
                if (token.Type == TokenType.Variable && !variables.ContainsKey(token.Value))
                {
                    variables[token.Value] = varIndex++;
                }
            }

            var varCount = variables.Count;
            if (varCount == 0)
                throw new ArgumentException("Формула должна содержать хотя бы одну переменную");

            return (bool[] values) =>
            {
                if (values.Length < varCount)
                    throw new ArgumentException($"Недостаточно значений: требуется {varCount}, получено {values.Length}");

                var stack = new Stack<bool>();

                foreach (var token in rpn)
                {
                    if (token.Type == TokenType.Variable)
                    {
                        int index = variables[token.Value];
                        stack.Push(values[index]);
                    }
                    else if (token.Type == TokenType.Not)
                    {
                        if (stack.Count < 1) throw new ArgumentException("Недостаточно операндов для !");
                        stack.Push(!stack.Pop());
                    }
                    else if (token.Type == TokenType.And)
                    {
                        if (stack.Count < 2) throw new ArgumentException("Недостаточно операндов для &");
                        bool b = stack.Pop();
                        bool a = stack.Pop();
                        stack.Push(a && b);
                    }
                    else if (token.Type == TokenType.Or)
                    {
                        if (stack.Count < 2) throw new ArgumentException("Недостаточно операндов для |");
                        bool b = stack.Pop();
                        bool a = stack.Pop();
                        stack.Push(a || b);
                    }
                    else if (token.Type == TokenType.Xor)
                    {
                        if (stack.Count < 2) throw new ArgumentException("Недостаточно операндов для ^");
                        bool b = stack.Pop();
                        bool a = stack.Pop();
                        stack.Push(a ^ b);
                    }
                    else if (token.Type == TokenType.Implication)
                    {
                        if (stack.Count < 2) throw new ArgumentException("Недостаточно операндов для ->");
                        bool b = stack.Pop();
                        bool a = stack.Pop();
                        stack.Push(!a || b); // A -> B эквивалентно !A | B
                    }
                    else if (token.Type == TokenType.Equivalence)
                    {
                        if (stack.Count < 2) throw new ArgumentException("Недостаточно операндов для =");
                        bool b = stack.Pop();
                        bool a = stack.Pop();
                        stack.Push(a == b); // A = B эквивалентно (A & B) | (!A & !B)
                    }
                }

                if (stack.Count != 1)
                    throw new ArgumentException("Ошибка вычисления формулы");

                return stack.Pop();
            };
        }

        /// <summary>
        /// Разворачивает формулу в базис {¬, ∧, ∨}
        /// </summary>
        public string ExpandToBasis(string formula)
        {
            // Заменяем операции на базисные
            string result = formula;
            
            // Импликация: A -> B = !A | B
            // Эквивалентность: A = B = (A & B) | (!A & !B)
            // XOR: A ^ B = (A & !B) | (!A & B)
            
            // Это упрощенная версия - в реальности нужен более сложный парсинг
            // Для полной реализации нужно парсить и переписывать дерево
            
            return result;
        }

        /// <summary>
        /// Получает список переменных из формулы
        /// </summary>
        public static List<string> GetVariables(string formula)
        {
            var parser = new BooleanFunctionParser();
            var tokens = parser.Tokenize(formula.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", ""));
            
            var variables = new HashSet<string>();
            foreach (var token in tokens)
            {
                if (token.Type == TokenType.Variable)
                {
                    variables.Add(token.Value);
                }
            }
            
            return variables.OrderBy(v => v).ToList();
        }
    }
}

