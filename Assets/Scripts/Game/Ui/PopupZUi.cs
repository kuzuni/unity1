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

        /// <summary>정본 `#toasts` 의 z(1925 — 30). 열 모달(40)보다 작아 그 모달이 열려 있으면 토스트가 **아래**다(T33 88회차).</summary>
        public static int ToastsZ { get { Load(); return ZOf(J.Obj(root["toasts"]), "toasts"); } }

        /// <summary>
        /// T33 88회차 — 토스트 레인이 **탭바 위 층(`modals-over`)보다 위**인가. 클론 층은 둘뿐이라(탭바 아래·위)
        /// 그 둘 사이에 토스트를 끼울 자리를 이 한 수로 정한다: 정본 토스트 z(30)가 **그 층의 z 보다 크면** 위, 아니면 아래다.
        /// 위 층의 z 는 «탭바보다 큰 팝업» 들의 값이고 정본에선 전부 40 이다 — 표에서 **가장 작은** 그 값으로 잰다(새 팝업이 들어와도 맞다).
        /// </summary>
        public static bool ToastsAboveOverLayer
        {
            get
            {
                Load();
                int lowestAbove = int.MaxValue;
                foreach (KeyValuePair<string, object> kv in modals)
                {
                    if (kv.Key.StartsWith("_")) continue;
                    JsonObject o = J.Obj(kv.Value);
                    if (o == null) continue;
                    int z = ZOf(o, kv.Key);
                    if (z > TabbarZ && z < lowestAbove) lowestAbove = z;
                }
                return lowestAbove == int.MaxValue || ToastsZ > lowestAbove;
            }
        }

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
