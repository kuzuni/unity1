using System;
using System.Collections.Generic;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>
    /// T361 — 정본 `white-space` 의 **규약**. 자리마다의 값은 표(`Resources/WrapUi.json`)가 쥐고, 여기에는 낱말과 기본값만 있다.
    /// UnityEngine 참조 0.
    ///
    /// 정본은 CSS 기본값(normal = 접는다)을 그대로 두고 **40 자리에만** `nowrap` 을 못 박았다(`.sr-name` 하나는 `normal` 을 명시 — 두 줄 허용).
    /// 그러니 «표에 없는 글자 = 접는다» 가 정본의 뜻이고, 클론 공장이 그 반대(NoWrap 기본)였던 것이 T361 의 병이다.
    /// </summary>
    public static class WrapRules
    {
        /// <summary>T466 — 정본 `word-break` 낱말(keep-all). 표 `keep_all` 의 값은 이것뿐이다.</summary>
        public const string KeepAllWord = "keep-all";

        public const string NoWrap = "nowrap";
        public const string Normal = "normal";

        /// <summary>정본 기본값 — 표에 안 적힌 글자는 접는다.</summary>
        public const bool DefaultWraps = true;

        /// <summary>표 낱말 → 접는가. 표에 없는 낱말은 거부한다(정본에 없는 값을 조용히 «접는다» 로 읽지 않기 위해).</summary>
        public static bool Wraps(string mode)
        {
            if (mode == Normal) return true;
            if (mode == NoWrap) return false;
            throw new FormatException("white-space 낱말이 정본에 없다: «" + mode + "»(nowrap|normal)");
        }

        /// <summary>선택자 → 표 키(영숫자 밖은 `_` · 잇단 `_` 는 하나 · 소문자). `tools/check_wrap.py` 와 같은 규칙.</summary>
        public static string KeyOf(string selector)
        {
            if (string.IsNullOrEmpty(selector)) throw new FormatException("선택자가 비었다");
            var sb = new System.Text.StringBuilder(selector.Length);
            bool us = false;
            foreach (char c in selector)
            {
                bool ok = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
                if (ok) { sb.Append(char.ToLowerInvariant(c)); us = false; }
                else if (!us) { sb.Append('_'); us = true; }
            }
            string k = sb.ToString().Trim('_');
            if (k.Length == 0) throw new FormatException("선택자에서 키를 못 만든다: «" + selector + "»");
            return k;
        }
    }

    /// <summary>`WrapUi.json` `sites` — 자리 키 → 낱말(nowrap|normal). `_` 로 시작하는 칸은 설명이다.
    /// `clone_nowrap`(선택) — **정본은 접는 자리지만 클론에서만 안 접어야 하는 예외**(행 높이가 고정이고 글자 하한이 정본보다 커
    /// 정본 폭의 한 줄 글이 접혀 아래 줄을 덮는 자리 · T361 6회차 · 결정 673). `sites` 와 따로 세고(`Count` 는 정본 41 그대로) 자리마다 `_클론` 에 까닭을 적는다.</summary>
    public sealed class WrapTable
    {
        readonly Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Dictionary<string, string> clone = new Dictionary<string, string>(StringComparer.Ordinal);
        /// <summary>T466 — 정본 `word-break: keep-all` 자리(`keep_all`) · 값은 낱말 그대로(keep-all).</summary>
        readonly Dictionary<string, string> keep = new Dictionary<string, string>(StringComparer.Ordinal);

        public int Count { get { return map.Count; } }
        /// <summary>클론 쪽 예외 자리 수(`clone_nowrap`).</summary>
        public int CloneCount { get { return clone.Count; } }
        public IEnumerable<string> Keys { get { return map.Keys; } }
        public bool Has(string key) { return map.ContainsKey(key) || clone.ContainsKey(key); }
        public bool IsCloneException(string key) { return clone.ContainsKey(key); }
        /// <summary>T466 — 그 자리가 정본 `word-break: keep-all` 인가(어절에서만 꺾는다). 표에 없으면 false(= 정본 기본 normal · 음절마다 꺾는다).</summary>
        public bool KeepsAll(string key) { string v; return keep.TryGetValue(key, out v) && v == WrapRules.KeepAllWord; }
        /// <summary>`keep_all` 자리 수.</summary>
        public int KeepAllCount { get { return keep.Count; } }

        /// <summary>표 낱말 그대로(nowrap|normal).</summary>
        public string Mode(string key)
        {
            string v;
            if (!map.TryGetValue(key, out v) && !clone.TryGetValue(key, out v)) throw new FormatException("WrapUi 에 없는 자리다: " + key);
            return v;
        }

        /// <summary>그 자리가 접는가 — 정본 표(`sites`) → 클론 예외(`clone_nowrap`) → 정본 기본(<see cref="WrapRules.DefaultWraps"/>).</summary>
        public bool Wraps(string key)
        {
            string v;
            if (map.TryGetValue(key, out v)) return WrapRules.Wraps(v);
            if (clone.TryGetValue(key, out v)) return WrapRules.Wraps(v);
            return WrapRules.DefaultWraps;
        }

        public int CountOf(string mode)
        {
            int n = 0;
            foreach (var kv in map) if (kv.Value == mode) n++;
            return n;
        }

        public static WrapTable From(JsonObject root)
        {
            if (root == null) throw new FormatException("WrapUi 의 최상위가 «상자» 가 아니다");
            object so;
            if (!root.TryGet("sites", out so)) throw new FormatException("WrapUi 에 sites 칸이 없다");
            JsonObject sites = J.Obj(so);
            if (sites == null) throw new FormatException("WrapUi 의 sites 가 «상자» 가 아니다");
            var t = new WrapTable();
            foreach (var kv in sites)
            {
                string k = kv.Key;
                if (k.Length > 0 && k[0] == '_') continue;
                string v = J.Str(kv.Value);
                if (v == null) throw new FormatException("WrapUi «" + k + "» 가 낱말이 아니다");
                WrapRules.Wraps(v);   // 낱말 검사(nowrap|normal 밖이면 던진다)
                if (k != WrapRules.KeyOf(k)) throw new FormatException("WrapUi 키가 규칙(영숫자·`_`·소문자) 밖이다: " + k);
                t.map[k] = v;
            }
            if (t.map.Count == 0) throw new FormatException("WrapUi 에 자리가 하나도 없다");
            object ko;
            if (root.TryGet("keep_all", out ko))
            {
                JsonObject ka = J.Obj(ko);
                if (ka == null) throw new FormatException("WrapUi 의 keep_all 이 «상자» 가 아니다");
                foreach (var kv in ka)
                {
                    string k = kv.Key;
                    if (k.Length > 0 && k[0] == '_') continue;
                    string v = J.Str(kv.Value);
                    if (v != WrapRules.KeepAllWord) throw new FormatException("WrapUi keep_all «" + k + "» 의 낱말이 keep-all 이 아니다: " + v);
                    if (k != WrapRules.KeyOf(k)) throw new FormatException("WrapUi keep_all 키가 규칙 밖이다: " + k);
                    t.keep[k] = v;
                }
            }
            object co;
            if (root.TryGet("clone_nowrap", out co))
            {
                JsonObject cl = J.Obj(co);
                if (cl == null) throw new FormatException("WrapUi 의 clone_nowrap 이 «상자» 가 아니다");
                foreach (var kv in cl)
                {
                    string k = kv.Key;
                    if (k.Length > 0 && k[0] == '_') continue;
                    string v = J.Str(kv.Value);
                    if (v != WrapRules.NoWrap) throw new FormatException("WrapUi clone_nowrap «" + k + "» 는 nowrap 만 된다(정본이 접는 자리를 클론이 안 접는 예외다)");
                    if (k != WrapRules.KeyOf(k)) throw new FormatException("WrapUi 키가 규칙(영숫자·`_`·소문자) 밖이다: " + k);
                    if (t.map.ContainsKey(k)) throw new FormatException("WrapUi clone_nowrap «" + k + "» 는 이미 정본 표(sites)에 있다");
                    t.clone[k] = v;
                }
            }
            return t;
        }
    }
}
