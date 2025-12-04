using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManagement.Shared.Helper.ProjectAppStorage
{
    /// <summary>
    /// مقيِّم تعابير حسابية بسيط يدعم + - * / والأقواس والمتغيرات.
    /// المتغيرات تُمرَّر عبر قاموس vars (غير حساس لحالة الأحرف).
    /// </summary>
    internal static class ExpressionEvaluator
    {
        public static bool TryEval(string expr, IReadOnlyDictionary<string, double> vars, out double result)
        {
            try
            {
                var rpn = ToRpn(expr, vars);
                result = EvalRpn(rpn);
                return true;
            }
            catch
            {
                result = 0;
                return false;
            }
        }

        private static List<string> ToRpn(string expr, IReadOnlyDictionary<string, double> vars)
        {
            var tokens = Tokenize(expr);

            // استبدال المتغيرات بقيمها
            for (int i = 0; i < tokens.Count; i++)
            {
                var t = tokens[i];
                if (IsIdentifier(t))
                {
                    if (!vars.TryGetValue(t, out var val))
                        throw new InvalidOperationException($"Unknown variable '{t}'");
                    tokens[i] = val.ToString(CultureInfo.InvariantCulture);
                }
            }

            var output = new List<string>();
            var ops = new Stack<string>();

            int Prec(string op) => op switch
            {
                "+" or "-" => 1,
                "*" or "/" => 2,
                _ => 0
            };

            foreach (var t in tokens)
            {
                if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                {
                    output.Add(t);
                }
                else if (IsOperator(t))
                {
                    while (ops.Count > 0 && IsOperator(ops.Peek()) && Prec(ops.Peek()) >= Prec(t))
                        output.Add(ops.Pop());
                    ops.Push(t);
                }
                else if (t == "(") ops.Push(t);
                else if (t == ")")
                {
                    while (ops.Count > 0 && ops.Peek() != "(") output.Add(ops.Pop());
                    if (ops.Count == 0 || ops.Pop() != "(") throw new InvalidOperationException("Mismatched parentheses");
                }
                else throw new InvalidOperationException($"Unexpected token '{t}'");
            }

            while (ops.Count > 0)
            {
                var op = ops.Pop();
                if (op is "(" or ")") throw new InvalidOperationException("Mismatched parentheses");
                output.Add(op);
            }
            return output;
        }

        private static double EvalRpn(List<string> rpn)
        {
            var st = new Stack<double>();
            foreach (var t in rpn)
            {
                if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var n)) st.Push(n);
                else
                {
                    if (st.Count < 2) throw new InvalidOperationException("Invalid expression");
                    var b = st.Pop(); var a = st.Pop();
                    st.Push(t switch
                    {
                        "+" => a + b,
                        "-" => a - b,
                        "*" => a * b,
                        "/" => b == 0 ? throw new DivideByZeroException() : a / b,
                        _ => throw new InvalidOperationException($"Unknown operator '{t}'")
                    });
                }
            }
            if (st.Count != 1) throw new InvalidOperationException("Invalid expression");
            return st.Pop();
        }

        private static bool IsOperator(string t) => t is "+" or "-" or "*" or "/";
        private static bool IsIdentifier(string t)
        {
            if (string.IsNullOrEmpty(t)) return false;
            if (!(char.IsLetter(t[0]) || t[0] == '_')) return false;
            for (int i = 1; i < t.Length; i++)
            {
                var ch = t[i];
                if (!(char.IsLetterOrDigit(ch) || ch == '_')) return false;
            }
            return true;
        }

        private static List<string> Tokenize(string s)
        {
            var tokens = new List<string>();
            int i = 0;
            while (i < s.Length)
            {
                char ch = s[i];
                if (char.IsWhiteSpace(ch)) { i++; continue; }

                if (char.IsDigit(ch) || ch == '.' && i + 1 < s.Length && char.IsDigit(s[i + 1]))
                {
                    int start = i; i++;
                    while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.')) i++;
                    tokens.Add(s[start..i]);
                    continue;
                }

                if (char.IsLetter(ch) || ch == '_')
                {
                    int start = i; i++;
                    while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                    tokens.Add(s[start..i].ToLowerInvariant());
                    continue;
                }

                if ("+-*/()".IndexOf(ch) >= 0) { tokens.Add(ch.ToString()); i++; continue; }

                throw new InvalidOperationException($"Unexpected char '{ch}'");
            }
            return tokens;
        }
    }

}
