using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace Forge.Core
{
    /// <summary>
    /// JS `Number` 의 문자열 규약을 C# 에서 그대로 낸다 — 원작 `bignum.js` 의 `toString`(`${m}e${e}`)·`toFixed`·`String(n)`
    /// 이 세이브·표시 문자열을 만들기 때문에 «같은 입력 → 같은 글자» 는 여기서 결정된다.
    /// 런타임(dotnet · Mono)의 서식기에 기대지 않고 정확한 값으로 계산한다(toFixed 는 BigInteger · 최단 표기는 되읽기 탐색).
    /// </summary>
    public static class JsNum
    {
        /// <summary>ECMAScript Number::toString(10). NaN → "NaN" · ±Infinity · -0 → "0" · 1e21 이상·1e-7 미만은 지수 표기.</summary>
        public static string ToString(double d)
        {
            if (double.IsNaN(d)) return "NaN";
            if (double.IsPositiveInfinity(d)) return "Infinity";
            if (double.IsNegativeInfinity(d)) return "-Infinity";
            if (d == 0) return "0";
            string sign = d < 0 ? "-" : "";
            if (d < 0) d = -d;
            string digits; int n;
            ShortestDigits(d, out digits, out n);
            int k = digits.Length;
            var sb = new StringBuilder(sign);
            if (k <= n && n <= 21)
            {
                sb.Append(digits).Append('0', n - k);
            }
            else if (0 < n && n <= 21)
            {
                sb.Append(digits, 0, n).Append('.').Append(digits, n, k - n);
            }
            else if (-6 < n && n <= 0)
            {
                sb.Append("0.").Append('0', -n).Append(digits);
            }
            else
            {
                int e = n - 1;
                sb.Append(digits[0]);
                if (k > 1) sb.Append('.').Append(digits, 1, k - 1);
                sb.Append('e').Append(e >= 0 ? "+" : "-").Append(Math.Abs(e).ToString(CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 되읽으면 같은 double 이 되는 가장 짧은 10진 유효숫자(digits · 앞뒤 0 없음)와 소수점 위치 n(값 = 0.digits × 10^n).
        /// 정밀도 1~17 자리를 차례로 «정확히 반올림한 E 표기» 로 찍어 되읽어 본다.
        /// </summary>
        static void ShortestDigits(double d, out string digits, out int n)
        {
            for (int p = 1; p <= 17; p++)
            {
                string s = d.ToString("E" + (p - 1), CultureInfo.InvariantCulture);
                double back;
                if (p < 17 && !(double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out back) && back == d)) continue;
                int ePos = s.IndexOf('E');
                string mant = s.Substring(0, ePos).Replace(".", "");
                int exp = int.Parse(s.Substring(ePos + 1), CultureInfo.InvariantCulture);
                mant = mant.TrimEnd('0');
                if (mant.Length == 0) mant = "0";
                digits = mant;
                n = exp + 1;
                return;
            }
            digits = "0"; n = 1;
        }

        /// <summary>ECMAScript Number.prototype.toFixed — 정확한 값으로 반올림(동률은 큰 쪽) · |x| ≥ 1e21 은 ToString.</summary>
        public static string ToFixed(double x, int fractionDigits)
        {
            if (fractionDigits < 0 || fractionDigits > 100) throw new ArgumentOutOfRangeException("fractionDigits");
            if (double.IsNaN(x)) return "NaN";
            if (double.IsInfinity(x) || Math.Abs(x) >= 1e21) return ToString(x);
            string sign = "";
            if (x < 0) { sign = "-"; x = -x; }
            BigInteger mant; int e2;
            Decompose(x, out mant, out e2);
            BigInteger num = mant * BigInteger.Pow(10, fractionDigits);
            BigInteger n;
            if (e2 >= 0) n = num << e2;
            else
            {
                int sh = -e2;
                n = (num + (BigInteger.One << (sh - 1))) >> sh;
            }
            string s = n.ToString(CultureInfo.InvariantCulture);
            if (fractionDigits == 0) return sign + s;
            if (s.Length <= fractionDigits) s = new string('0', fractionDigits - s.Length + 1) + s;
            return sign + s.Substring(0, s.Length - fractionDigits) + "." + s.Substring(s.Length - fractionDigits);
        }

        /// <summary>x = mant × 2^e2 (정확).</summary>
        static void Decompose(double x, out BigInteger mant, out int e2)
        {
            long bits = BitConverter.DoubleToInt64Bits(x);
            int exp = (int)((bits >> 52) & 0x7FF);
            long frac = bits & 0xFFFFFFFFFFFFFL;
            if (exp == 0) { mant = frac; e2 = -1074; }
            else { mant = frac | (1L << 52); e2 = exp - 1075; }
            if (mant.IsZero) e2 = 0;
        }

        /// <summary>ECMAScript Math.round — 소수부 .5 는 +∞ 쪽(C# Math.Round 의 은행가 반올림이 아니다).</summary>
        public static double Round(double x)
        {
            if (double.IsNaN(x) || double.IsInfinity(x)) return x;
            double f = Math.Floor(x);
            return (x - f) >= 0.5 ? f + 1 : f;
        }

        static readonly Regex FloatHead = new Regex(@"^[+-]?(Infinity|(\d+\.?\d*|\.\d+)([eE][+-]?\d+)?)", RegexOptions.CultureInvariant);

        /// <summary>ECMAScript parseFloat — 앞쪽의 가장 긴 유효 접두를 읽고 나머지는 버린다 · 못 읽으면 NaN.</summary>
        public static double ParseFloat(string s)
        {
            if (s == null) return double.NaN;
            s = s.TrimStart(' ', '\t', '\n', '\r', '\f', '\v', ' ', '﻿');
            var m = FloatHead.Match(s);
            if (!m.Success) return double.NaN;
            string t = m.Value;
            if (t.EndsWith("Infinity", StringComparison.Ordinal)) return t[0] == '-' ? double.NegativeInfinity : double.PositiveInfinity;
            double d;
            if (!double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return double.NaN;
            return d;
        }
    }

    /// <summary>
    /// 원작 `bignum.js` 의 `Big` — 가수 m ∈ [1,10)(또는 0 · 음수는 m 이 음수) × 10^e. 값 = m × 10^e.
    /// 불변 값 타입(원작도 연산마다 새 Big 을 돌려준다). 정규화(`norm`)·덧셈 컷오프 17·가수 12자리 스냅·MAX_E 1e308 전부 원작 그대로.
    /// </summary>
    public readonly struct Big : IEquatable<Big>, IComparable<Big>
    {
        public const double MaxE = 1e308;
        const int AddCutoff = 17;

        public readonly double M;
        public readonly double E;

        public Big(double m, double e)
        {
            Norm(ref m, ref e);
            M = m; E = e;
        }

        static void Norm(ref double m, ref double e)
        {
            if (double.IsInfinity(m) || double.IsNaN(m) || m == 0)
            {
                if (m == 0 || double.IsNaN(m)) { m = 0; e = 0; }
                else { m = m > 0 ? 1 : -1; e = MaxE; }
                return;
            }
            double abs = Math.Abs(m);
            if (abs < 1 || abs >= 10)
            {
                double shift = Math.Floor(Math.Log10(abs));
                m = m / Math.Pow(10, shift);
                e = e + shift;
            }
            m = JsNum.Round(m * 1e12) / 1e12;
            if (Math.Abs(m) >= 10) { m /= 10; e++; }
            else if (m != 0 && Math.Abs(m) < 1) { m *= 10; e--; }
            if (m == 0) { e = 0; return; }
            if (double.IsInfinity(e) || double.IsNaN(e)) e = e > 0 ? MaxE : -MaxE;
            if (e > MaxE) e = MaxE;
            if (e < -MaxE) { m = 0; e = 0; }
        }

        // ---- 생성 ----
        public static Big Of(double v)
        {
            if (double.IsNaN(v)) return Zero;
            if (double.IsInfinity(v)) return v > 0 ? new Big(1, MaxE) : new Big(-1, MaxE);
            return new Big(v, 0);
        }

        public static Big Of(string s) { return Parse(s); }
        public static implicit operator Big(double v) { return Of(v); }

        /// <summary>"1.234e567" / "12345" / "1.5" — 세이브에서 되읽는 경로(원작 `fromString`). 못 읽으면 0.</summary>
        public static Big Parse(string s)
        {
            if (s == null) return Zero;
            s = s.Trim();
            if (s.Length == 0) return Zero;
            int i = s.IndexOf('e');
            if (i < 0) i = s.IndexOf('E');
            if (i >= 0)
            {
                double m = JsNum.ParseFloat(s.Substring(0, i)), e = JsNum.ParseFloat(s.Substring(i + 1));
                if (double.IsNaN(m) || double.IsNaN(e)) return Zero;
                return new Big(m, e);
            }
            double n = JsNum.ParseFloat(s);
            return double.IsNaN(n) ? Zero : new Big(n, 0);
        }

        public static Big Zero { get { return new Big(0, 0); } }
        public static Big One { get { return new Big(1, 0); } }

        /// <summary>10^x — 지수만 다루므로 x 가 얼마든 상수 시간.</summary>
        public static Big Pow10(double x) { return new Big(Math.Pow(10, x - Math.Floor(x)), Math.Floor(x)); }

        // ---- 사칙연산 ----
        public Big Add(Big o)
        {
            if (M == 0) return new Big(o.M, o.E);
            if (o.M == 0) return new Big(M, E);
            Big big, small;
            if (E >= o.E) { big = this; small = o; } else { big = o; small = this; }
            double de = big.E - small.E;
            if (de > AddCutoff) return new Big(big.M, big.E);
            return new Big(big.M + small.M / Math.Pow(10, de), big.E);
        }
        public Big Sub(Big o) { return Add(new Big(-o.M, o.E)); }
        public Big Mul(Big o) { if (M == 0 || o.M == 0) return Zero; return new Big(M * o.M, E + o.E); }
        /// <summary>0 으로 나누기는 0 (원작: 게임 로직상 무한대가 더 위험).</summary>
        public Big Div(Big o)
        {
            if (o.M == 0) return Zero;
            if (M == 0) return Zero;
            return new Big(M / o.M, E - o.E);
        }
        public Big Neg() { return new Big(-M, E); }
        public Big Abs() { return new Big(Math.Abs(M), E); }

        /// <summary>실수 지수 허용(1.01^99). 음수 밑은 정수 지수만 부호를 살린다.</summary>
        public Big Pow(double n)
        {
            if (M == 0) return n == 0 ? One : Zero;
            if (n == 0) return One;
            if (M < 0)
            {
                double sign = Math.Abs(n % 2) == 1 ? -1 : 1;
                Big r = Abs().Pow(n);
                return sign < 0 ? r.Neg() : r;
            }
            return Pow10(Log10() * n);
        }
        public double Log10() { return M == 0 ? double.NegativeInfinity : Math.Log10(Math.Abs(M)) + E; }
        public Big Sqrt() { return M < 0 ? Zero : Pow10(Log10() / 2); }

        public static Big operator +(Big a, Big b) { return a.Add(b); }
        public static Big operator -(Big a, Big b) { return a.Sub(b); }
        public static Big operator *(Big a, Big b) { return a.Mul(b); }
        public static Big operator /(Big a, Big b) { return a.Div(b); }
        public static Big operator -(Big a) { return a.Neg(); }

        // ---- 비교 ----
        public int Cmp(Big o)
        {
            if (M == 0 && o.M == 0) return 0;
            if (M == 0) return o.M > 0 ? -1 : 1;
            if (o.M == 0) return M > 0 ? 1 : -1;
            bool s1 = M > 0, s2 = o.M > 0;
            if (s1 != s2) return s1 ? 1 : -1;
            if (E != o.E) return (E > o.E) == s1 ? 1 : -1;
            return M == o.M ? 0 : (M > o.M ? 1 : -1);
        }
        public int CompareTo(Big o) { return Cmp(o); }
        public bool Gt(Big o) { return Cmp(o) > 0; }
        public bool Gte(Big o) { return Cmp(o) >= 0; }
        public bool Lt(Big o) { return Cmp(o) < 0; }
        public bool Lte(Big o) { return Cmp(o) <= 0; }
        public bool Eq(Big o) { return Cmp(o) == 0; }
        public bool IsZero { get { return M == 0; } }
        public bool IsPos { get { return M > 0; } }
        public bool IsNeg { get { return M < 0; } }
        public Big Min(Big o) { return Lte(o) ? this : o; }
        public Big Max(Big o) { return Gte(o) ? this : o; }
        public Big Clamp(Big a, Big b) { return Max(a).Min(b); }

        public static bool operator >(Big a, Big b) { return a.Cmp(b) > 0; }
        public static bool operator <(Big a, Big b) { return a.Cmp(b) < 0; }
        public static bool operator >=(Big a, Big b) { return a.Cmp(b) >= 0; }
        public static bool operator <=(Big a, Big b) { return a.Cmp(b) <= 0; }

        // ---- 변환 ----
        /// <summary>Number 로. 범위를 넘으면 ±Infinity 가 아니라 유한 최대값(원작: 좌표·게이지가 NaN 으로 오염되지 않게).</summary>
        public double ToNumber()
        {
            if (M == 0) return 0;
            if (E > 308) return M > 0 ? double.MaxValue : -double.MaxValue;
            if (E < -308) return 0;
            return M * Math.Pow(10, E);
        }
        public double RatioTo(Big o)
        {
            if (o.M == 0) return 0;
            return Div(o).ToNumber();
        }
        public Big Floor()
        {
            if (E >= 15) return new Big(M, E);
            return Of(Math.Floor(ToNumber()));
        }

        /// <summary>세이브 직렬화 — 원작 `toString`/`toJSON`: "0" 또는 `${m}e${e}` (JS 수 표기).</summary>
        public override string ToString() { return M == 0 ? "0" : JsNum.ToString(M) + "e" + JsNum.ToString(E); }

        public bool Equals(Big o) { return M == o.M && E == o.E; }
        public override bool Equals(object obj) { return obj is Big && Equals((Big)obj); }
        public override int GetHashCode() { return M.GetHashCode() * 397 ^ E.GetHashCode(); }
    }

    /// <summary>
    /// 원작 표시 포맷 — `bignum.js` 의 `fmtBig` 와 `util.js` 의 `U.fmt`·`U.fmtDec`·`U.pctTrim`·`U.fmtTime`.
    /// 접미사는 소문자 k/m/b/t → aa/ab/…(10^15부터 3자리마다) → zz 초과는 e 표기 → 1e21 이상 지수는 ∞. 꼬리 0 제거·1000k→1m 재정규화 그대로.
    /// </summary>
    public static class NumFmt
    {
        static readonly string[] Units = { "k", "m", "b", "t" };
        const string Alpha = "abcdefghijklmnopqrstuvwxyz";

        /// <summary>tier: 1=k, 2=m, 3=b, 4=t, 5=aa, 6=ab, … · zz 초과(또는 0 이하)는 null.</summary>
        public static string UnitFor(int tier)
        {
            if (tier < 1) return null;
            if (tier <= Units.Length) return Units[tier - 1];
            int i = tier - Units.Length - 1;
            if (i >= Alpha.Length * Alpha.Length) return null;
            return Alpha[i / Alpha.Length].ToString() + Alpha[i % Alpha.Length];
        }

        /// <summary>꼬리 0 제거 — 소수점이 없는 문자열은 건드리지 않는다(782 → 78 이 되지 않게).</summary>
        public static string TrimZeros(string s)
        {
            if (s.IndexOf('.') < 0) return s;
            s = s.TrimEnd('0');
            if (s.EndsWith(".", StringComparison.Ordinal)) s = s.Substring(0, s.Length - 1);
            return s;
        }

        static int Digits(double v, int decimals) { return decimals >= 0 ? decimals : (v >= 100 ? 0 : v >= 10 ? 1 : 2); }

        /// <summary>반올림한 가수가 1000 이 되면 단위를 한 칸 올린다(`1000k` 가 아니라 `1m`). shown 은 부호 없는 가수 · decimals &lt; 0 = 미지정.</summary>
        static void Renorm(double shown, ref int tier, int decimals, out string body)
        {
            body = JsNum.ToFixed(shown, Digits(shown, decimals));
            if (JsNum.ParseFloat(body) >= 1000 && UnitFor(tier + 1) != null)
            {
                tier += 1;
                shown /= 1000;
                body = JsNum.ToFixed(shown, Digits(shown, decimals));
            }
        }

        /// <summary>원작 `fmtBig(v, decimals)` — decimals &lt; 0 이면 미지정(꼬리 0 제거) · 지정하면 자릿수 보존.</summary>
        public static string FmtBig(Big b, int decimals = -1)
        {
            if (b.M == 0) return "0";
            bool neg = b.IsNeg;
            Big a = b.Abs();
            if (a.E < 3)
            {
                double n = a.ToNumber();
                string s = decimals < 0 ? JsNum.ToString(Math.Floor(n)) : JsNum.ToFixed(n, decimals);
                return neg ? "-" + s : s;
            }
            double tierD = Math.Floor(a.E / 3);
            int tier = tierD > int.MaxValue ? int.MaxValue : (int)tierD;
            string unit = UnitFor(tier);
            if (unit == null)
            {
                if (a.E >= 1e21) return (neg ? "-" : "") + "∞";
                return (neg ? "-" : "") + JsNum.ToFixed(a.M, 2) + "e" + JsNum.ToFixed(a.E, 0);
            }
            double shown = a.M * Math.Pow(10, a.E - tier * 3);
            string body;
            Renorm(shown, ref tier, decimals, out body);
            if (decimals < 0) body = TrimZeros(body);
            return (neg ? "-" : "") + body + UnitFor(tier);
        }

        /// <summary>원작 `U.fmt(Big)` — 세이브 문자열("1.5e300")도 이 경로다.</summary>
        public static string Fmt(Big b) { return FmtBig(b); }
        public static string Fmt(string saved) { return FmtBig(Big.Of(saved)); }

        /// <summary>원작 `U.fmt(number)`: 999 / 1.23k / 4.56aa. 1e15 이상은 Big 경로.</summary>
        public static string Fmt(double n)
        {
            if (double.IsNaN(n)) return "0";
            n = Math.Floor(n);
            double abs = Math.Abs(n);
            if (abs < 1000) return JsNum.ToString(n);
            if (!(abs < 1e15)) return FmtBig(Big.Of(n));
            int u = -1; double v = abs;
            while (v >= 1000 && u < Units.Length - 1) { v /= 1000; u++; }
            int tier = u + 1; string body;
            Renorm(v, ref tier, -1, out body);
            return (n < 0 ? "-" : "") + TrimZeros(body) + UnitFor(tier);
        }

        /// <summary>원작 `U.fmtDec(Big)` — 소수 둘째 자리 보존.</summary>
        public static string FmtDec(Big b) { return FmtBig(b, 2); }
        public static string FmtDec(string saved) { return FmtBig(Big.Of(saved), 2); }

        /// <summary>원작 `U.fmtDec(number)`: 1.13 / 8.87k / 149.05 — 꼬리 0 을 남긴다.</summary>
        public static string FmtDec(double n)
        {
            if (double.IsNaN(n)) return "0";
            double abs = Math.Abs(n);
            if (abs < 1000) return JsNum.ToFixed(n, 2);
            if (!(abs < 1e15)) return FmtBig(Big.Of(n), 2);
            int u = -1; double v = abs;
            while (v >= 1000 && u < Units.Length - 1) { v /= 1000; u++; }
            int tier = u + 1; string body;
            Renorm(v, ref tier, 2, out body);
            return (n < 0 ? "-" : "") + body + UnitFor(tier);
        }

        /// <summary>원작 `U.pctTrim`: 소수 둘째 자리 반올림 · 꼬리 0 제거 · "12.5%" / "0%".</summary>
        public static string PctTrim(double p)
        {
            if (double.IsNaN(p)) p = 0;
            double v = JsNum.ParseFloat(JsNum.ToFixed(p, 2));
            if (double.IsNaN(v) || v == 0) v = 0;
            return JsNum.ToString(v) + "%";
        }

        /// <summary>원작 `U.fmtTime(sec)`: `4일 2시` · `2시 10분` · `13분 5초` · `7초` (앞 단위는 '시간' 이 아니라 '시').</summary>
        public static string FmtTime(double sec)
        {
            sec = Math.Max(0, Math.Ceiling(sec));
            double d = Math.Floor(sec / 86400), h = Math.Floor(sec % 86400 / 3600),
                   m = Math.Floor(sec % 3600 / 60), s = sec % 60;
            if (d > 0) return JsNum.ToString(d) + "일 " + JsNum.ToString(h) + "시";
            if (h > 0) return JsNum.ToString(h) + "시 " + JsNum.ToString(m) + "분";
            if (m > 0) return JsNum.ToString(m) + "분 " + JsNum.ToString(s) + "초";
            return JsNum.ToString(s) + "초";
        }
    }
}
