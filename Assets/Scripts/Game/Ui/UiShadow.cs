using System;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T331 2회차 — 정본 `box-shadow` 의 **바깥 그림자**를 화면에 거는 자리. 수치는 `Resources/ShadowUi.json`(표) ·
    /// 셈·부호는 <see cref="ShadowTable"/>(Core) 가 쥔다.
    ///
    /// 정본의 «딱딱한 턱»(`0 .25rem 0 rgba(0,0,0,.3)` 꼴 · 흐림 0 · 번짐 0)은 원작 UI 가 «종이 카드가 한 겹 떠 있다» 를
    /// 만드는 방식이다. 흐림이 없으니 **같은 모양을 한 겹 뒤에 깔고 내리기만** 하면 화면이 정본과 같아진다 — 구울 것이 없다.
    /// UGUI 에서 «뒤» 는 곧 **먼저 그려지는 것**이라, 그늘을 대상 상자의 **첫 자식**으로 꽂는다(부모가 먼저, 자식이 뒤에
    /// 그려지므로 형제 중 첫째가 가장 아래다). 레이아웃은 건드리지 않는다 — 그늘은 상자를 꽉 채우고 치우침만 받는다.
    ///
    /// 흐린 그림자(`blur > 0`)는 여기서 거절한다 — 굽는 길(스프라이트)이 먼저 있어야 하고 그것이 3회차다.
    /// 조용히 딱딱하게 그리면 «섰다» 고 세어져 자가 거짓으로 초록이 된다.
    ///
    /// 도우미를 `UiKit` 에 안 넣고 새 파일로 낸 까닭: `UiKit.cs` 는 남의 산 lock 이다(T106·T178·T335) — T342 가 낸 길과 같다.
    /// </summary>
    public static class UiShadow
    {
        public const string ResourcePath = "ShadowUi";
        /// <summary>그늘 겹의 이름 — 자·테스트가 이 이름으로 찾는다.</summary>
        public const string LayerName = "shadow";

        static ShadowTable table;

        public static ShadowTable Table
        {
            get
            {
                if (table == null)
                {
                    TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
                    if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 을 못 읽었다(.meta 가 없으면 유니티가 안 싣는다)");
                    table = ShadowTable.From(MiniJson.ParseObject(ta.text));
                }
                return table;
            }
        }

        /// <summary>테스트가 표를 다시 읽게 한다.</summary>
        public static void Reset() { table = null; }

        /// <summary>
        /// 상자 <paramref name="box"/> 뒤에 정본 자리 <paramref name="key"/> 의 턱을 깐다.
        /// </summary>
        /// <param name="radiusPx">상자의 둥근 모서리(기준 px) — 그늘도 같은 모양이라야 테두리에서 안 비어져 나온다.</param>
        /// <returns>깐 겹(같은 상자에 두 번 부르면 앞서 깐 것을 고쳐 준다).</returns>
        public static Image Drop(RectTransform box, string key, float radiusPx)
        {
            if (box == null) throw new ArgumentNullException("box");
            ShadowSpec s = Table.Get(key);
            if (!s.IsHard) throw new NotSupportedException("«" + key + "» 은 흐린 그림자다(blur " + s.BlurRem + "rem) — 굽는 길이 선 뒤에 건다(T331 3회차). 딱딱하게 대신 그리면 자가 거짓으로 초록이 된다.");

            Transform had = box.Find(LayerName);
            Image img = had != null ? had.GetComponent<Image>() : UiKit.Rounded(box, LayerName, "pp_line", radiusPx);
            img.rectTransform.SetAsFirstSibling();
            UiKit.Fill(img.rectTransform);
            img.raycastTarget = false;
            img.color = new Color((float)s.R, (float)s.G, (float)s.B, (float)s.A);

            double x, y;
            Table.OffsetPx(key, PetSkillStyle.RemPx, out x, out y);
            img.rectTransform.anchoredPosition = new Vector2((float)x, (float)y);
            return img;
        }
    }
}
