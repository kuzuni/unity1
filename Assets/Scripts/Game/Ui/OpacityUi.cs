using System.Collections.Generic;
using Forge.Core.Data;
using UnityEngine;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T359 — 정적 `opacity` 표(<c>Assets/Forge/Resources/OpacityUi.json</c>). 정본이 «반투명하게 그린다» 는 자리의 알파를 키로 읽고
    /// <see cref="Apply"/> 가 그 상자에 <see cref="CanvasGroup"/> 알파로 건다(정본 opacity = 자손까지 한 겹 · CanvasGroup 과 같은 뜻).
    /// 수치는 표에만 있다(§1). catalog.json 에 이미 있는 넷은 그대로 <c>UiKit.L</c> 로 읽는다.
    /// </summary>
    public static class OpacityUi
    {
        public const string ResourcePath = "OpacityUi";
        static JsonObject root, alpha;

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new System.InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T359)");
            root = MiniJson.ParseObject(ta.text);
            alpha = J.Obj(root["alpha"]);
        }
        public static void Reset() { root = null; alpha = null; }

        public static bool Has(string key) { Load(); return J.Obj(alpha[key]) != null; }

        /// <summary>표의 알파(0~1) — 없는 키는 예외(표에 먼저 적는다).</summary>
        public static float A(string key)
        {
            Load();
            JsonObject o = J.Obj(alpha[key]);
            if (o == null) throw new KeyNotFoundException(ResourcePath + ".json 에 알파 «" + key + "» 이 없다");
            object v = o["a"];
            if (!J.IsNum(v)) throw new KeyNotFoundException(ResourcePath + ".json «" + key + "» 에 a 가 없다");
            float a = (float)J.Num(v);
            if (a < 0f || a > 1f) throw new System.FormatException(ResourcePath + ".json «" + key + "» a 는 0~1 이어야 한다");
            return a;
        }

        /// <summary>곁 표의 rem 값(`asc_arrow` 같은 절) — CSS rem 그대로(px 는 호출자가 <c>PopupKit.Rem</c> 을 곱한다).</summary>
        public static float Rem(string section, string field)
        {
            Load();
            JsonObject o = J.Obj(root[section]);
            object v = o != null ? o[field] : null;
            if (!J.IsNum(v)) throw new KeyNotFoundException(ResourcePath + ".json 의 «" + section + "» 에 «" + field + "» 이 없다");
            return (float)J.Num(v);
        }

        /// <summary>그 알파 자리의 곁수치(`fade_ms` 처럼 알파 말고 같이 적힌 값) — 없으면 예외.</summary>
        public static double Num(string key, string field)
        {
            Load();
            JsonObject o = J.Obj(alpha[key]);
            object v = o != null ? o[field] : null;
            if (!J.IsNum(v)) throw new KeyNotFoundException(ResourcePath + ".json «" + key + "» 에 «" + field + "» 이 없다");
            return J.Num(v);
        }

        /// <summary>그 알파 자리의 글자 값(`box` 처럼 이름) — 없으면 예외.</summary>
        public static string Text(string key, string field)
        {
            Load();
            JsonObject o = J.Obj(alpha[key]);
            string v = o != null ? J.Str(o[field]) : null;
            if (string.IsNullOrEmpty(v)) throw new KeyNotFoundException(ResourcePath + ".json «" + key + "» 에 «" + field + "» 이 없다");
            return v;
        }

        /// <summary>그 알파 자리의 타이밍 함수(`ease` = cubic-bezier 넷) — 안 적혀 있으면 linear.</summary>
        public static Forge.Core.CraftFx.CssEase Ease(string key)
        {
            Load();
            JsonObject o = J.Obj(alpha[key]);
            List<object> e = o != null ? J.Arr(o["ease"]) : null;
            if (e == null || e.Count < 4) return Forge.Core.CraftFx.CssEase.Linear;
            return new Forge.Core.CraftFx.CssEase(J.Num(e[0]), J.Num(e[1]), J.Num(e[2]), J.Num(e[3]));
        }

        /// <summary>그 상자(와 자손)에 표의 알파를 건다 — 이미 CanvasGroup 이 있으면 그것의 알파를 바꾼다.</summary>
        public static CanvasGroup Apply(GameObject go, string key)
        {
            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.alpha = A(key);
            return cg;
        }
    }
}
