using System.Collections.Generic;
using Forge.Core.Data;
using UnityEngine;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T346 — 정본 팝업 겹 순서 표(<c>Assets/Forge/Resources/PopupZUi.json</c>). 정본은 `z-index` 로 탭바(30) 위·아래를 가르는데
    /// 클론 <see cref="PopupLayer"/> 는 층이 둘(`modals` 탭바 아래 · `modals-over` 위)뿐이라 팝업마다 «정본 z 가 탭바 z 보다 큰가» 만 읽는다.
    /// 호출부는 <c>Popups.Show(Name, null, PopupZUi.AboveTabBar(Name))</c> 한 줄 — 수는 표에만 있다(§1).
    /// (<c>Popups.cs</c>·<c>UiKit.cs</c> 는 T331·T333 lock 이라 이 파일에 따로 둔다.)
    /// </summary>
    public static class PopupZUi
    {
        public const string ResourcePath = "PopupZUi";
        static JsonObject root, modals;

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new System.InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T346)");
            root = MiniJson.ParseObject(ta.text);
            modals = J.Obj(root["modals"]);
        }
        public static void Reset() { root = null; modals = null; }

        static int ZOf(JsonObject o, string what)
        {
            object v = o != null ? o["z"] : null;
            if (!J.IsNum(v)) throw new KeyNotFoundException(ResourcePath + ".json 의 «" + what + "» 에 z 가 없다");
            return J.Int(v);
        }

        /// <summary>정본 `#tabbar` 의 최종 z.</summary>
        public static int TabbarZ { get { Load(); return ZOf(J.Obj(root["tabbar"]), "tabbar"); } }

        public static bool Has(string name) { Load(); return J.Obj(modals[name]) != null; }

        /// <summary>정본 그 팝업의 최종 z(표에 없는 이름은 예외 — 표에 먼저 적는다).</summary>
        public static int Z(string name)
        {
            Load();
            JsonObject o = J.Obj(modals[name]);
            if (o == null) throw new KeyNotFoundException(ResourcePath + ".json 에 팝업 «" + name + "» 이 없다");
            return ZOf(o, name);
        }

        /// <summary>정본이 그 팝업을 탭바 위에 띄우는가(z 가 탭바보다 크다) — <see cref="PopupLayer.Show"/> 의 `aboveTabBar` 인자.</summary>
        public static bool AboveTabBar(string name) { return Z(name) > TabbarZ; }
    }
}
