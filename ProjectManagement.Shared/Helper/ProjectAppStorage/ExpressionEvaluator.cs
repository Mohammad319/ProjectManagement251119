using System;
using System.Collections.Generic;
using System.Globalization;

namespace ProjectManagement.Shared.Helper.ProjectAppStorage
{
    /// <summary>
    /// مقيِّم تعابير حسابية بسيط يدعم:
    /// + - * / والأقواس والمتغيرات + unary +/- (مثل: -2، -(a+b))
    /// المتغيرات تُمرَّر عبر قاموس vars (غير حساس لحالة الأحرف).
    /// </summary>
    internal static class ExpressionEvaluator
    {
        public static bool TryEval(string expr, IReadOnlyDictionary<string, decimal> vars, out decimal result)
        {
            try
            {
                var rpn = ToRpn(expr, vars);
                result = EvalRpn(rpn);
                return true;
            }
            catch
            {
                result = 0m;
                return false;
            }
        }

        private static List<string> ToRpn(string expr, IReadOnlyDictionary<string, decimal> vars)
        {
            var tokens = Tokenize(expr);
            tokens = NormalizeUnary(tokens);

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
                "u+" or "u-" => 3,
                "*" or "/" => 2,
                "+" or "-" => 1,
                _ => 0
            };

            bool IsRightAssoc(string op) => op is "u+" or "u-";

            foreach (var t in tokens)
            {
                if (decimal.TryParse(t, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _))
                {
                    output.Add(t);
                }
                else if (IsOperator(t))
                {
                    while (ops.Count > 0 && IsOperator(ops.Peek()))
                    {
                        var top = ops.Peek();
                        var cond = IsRightAssoc(t) ? Prec(top) > Prec(t) : Prec(top) >= Prec(t);
                        if (!cond) break;
                        output.Add(ops.Pop());
                    }
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

        private static decimal EvalRpn(List<string> rpn)
        {
            var st = new Stack<decimal>();
            foreach (var t in rpn)
            {
                if (decimal.TryParse(t, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var n))
                {
                    st.Push(n);
                    continue;
                }

                if (t is "u+" or "u-")
                {
                    if (st.Count < 1) throw new InvalidOperationException("Invalid expression");
                    var a = st.Pop();
                    st.Push(t == "u-" ? -a : a);
                    continue;
                }

                if (st.Count < 2) throw new InvalidOperationException("Invalid expression");
                var b = st.Pop();
                var a2 = st.Pop();

                st.Push(t switch
                {
                    "+" => a2 + b,
                    "-" => a2 - b,
                    "*" => a2 * b,
                    "/" => b == 0m ? throw new DivideByZeroException() : a2 / b,
                    _ => throw new InvalidOperationException($"Unknown operator '{t}'")
                });
            }

            if (st.Count != 1) throw new InvalidOperationException("Invalid expression");
            return st.Pop();
        }

        private static bool IsOperator(string t) => t is "+" or "-" or "*" or "/" or "u+" or "u-";

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

                // number ('.' كفاصلة عشرية)
                if (char.IsDigit(ch) || (ch == '.' && i + 1 < s.Length && char.IsDigit(s[i + 1])))
                {
                    int start = i; i++;
                    while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.')) i++;
                    tokens.Add(s[start..i]);
                    continue;
                }

                // identifier
                if (char.IsLetter(ch) || ch == '_')
                {
                    int start = i; i++;
                    while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                    tokens.Add(s[start..i].ToLowerInvariant());
                    continue;
                }

                if ("+-*/()".Contains(ch)) { tokens.Add(ch.ToString()); i++; continue; }

                throw new InvalidOperationException($"Unexpected char '{ch}'");
            }

            return tokens;
        }

        private static List<string> NormalizeUnary(List<string> tokens)
        {
            var result = new List<string>();
            for (int i = 0; i < tokens.Count; i++)
            {
                var t = tokens[i];

                bool isUnaryCandidate =
                    (t == "+" || t == "-") &&
                    (result.Count == 0 || result[^1] == "(" || IsOperator(result[^1]));

                if (!isUnaryCandidate)
                {
                    result.Add(t);
                    continue;
                }

                // لو التالي رقم => دمج الإشارة مع الرقم
                if (i + 1 < tokens.Count &&
                    decimal.TryParse(tokens[i + 1], NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                {
                    result.Add((t == "-" ? "-" : "") + tokens[i + 1]);
                    i++;
                    continue;
                }

                // غير ذلك => unary operator
                result.Add(t == "-" ? "u-" : "u+");
            }

            return result;
        }
    }
}