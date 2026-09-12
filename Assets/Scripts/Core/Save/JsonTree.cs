using System;
using System.Collections.Generic;
using Forge.Core.Data;

namespace Forge.Core.Save
{
    /// <summary>
    /// 세이브 트리(MiniJson 값 트리)를 원작 JS 의 눈으로 다루는 도우미(T13). 원작 state.js 는 `typeof` · `Array.isArray` ·
    /// 참/거짓 판정(`!S.version`) · `Number.isInteger` 로 세이브를 검사한다 — 그 판정을 글자 그대로 옮긴다.
    /// </summary>
    public static class JsonTree
    {
        /// <summary>원작 `kind(v)`: null → "null" · 배열 → "array" · 그 밖은 JS typeof(number/string/boolean/object).</summary>
        public static string Kind(object v)
        {
            if (v == null) return "null";
            if (v is List<object>) return "array";
            if (v is double) return "number";
            if (v is string) return "string";
            if (v is bool) return "boolean";
            return "object";
        }

        /// <summary>JS 참/거짓: null·false·0·NaN·"" 가 거짓. 객체·배열은 언제나 참.</summary>
        public static bool Truthy(object v)
        {
            if (v == null) return false;
            if (v is bool) return (bool)v;
            if (v is double) { double d = (double)v; return d != 0 && !double.IsNaN(d); }
            if (v is string) return ((string)v).Length > 0;
            return true;
        }

        /// <summary>JS `Number.isInteger(v)` — 수이면서 유한하고 소수부가 없다.</summary>
        public static bool IsInteger(object v)
        {
            if (!(v is double)) return false;
            double d = (double)v;
            return !double.IsNaN(d) && !double.IsInfinity(d) && Math.Floor(d) == d;
        }

        /// <summary>JS `U.clamp(v, a, b)` = `Math.min(b, Math.max(a, v))`.</summary>
        public static double Clamp(double v, double a, double b) { return Math.Min(b, Math.Max(a, v)); }

        /// <summary>깊은 복제(객체 키 순서 유지). 원시값은 그대로.</summary>
        public static object Clone(object v)
        {
            var o = v as JsonObject;
            if (o != null)
            {
                var c = new JsonObject();
                foreach (var kv in o) c[kv.Key] = Clone(kv.Value);
                return c;
            }
            var a = v as List<object>;
            if (a != null)
            {
                var c = new List<object>(a.Count);
                for (int i = 0; i < a.Count; i++) c.Add(Clone(a[i]));
                return c;
            }
            return v;
        }
    }
}
