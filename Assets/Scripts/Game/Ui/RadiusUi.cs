using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T345 — 정본 `border-radius` 를 그 자리에 준다. 값은 `Assets/Forge/Resources/RadiusUi.json` 이 쥐고(§1 — 수치를 코드에 안 박는다),
    /// 환산(rem → px · app-w 비율 → px)은 Core <see cref="RadiusRules.Px"/> 한 군데다.
    ///
    /// 왜 곁 표인가: 반지름 키를 쥔 표(`PetSkillUi`·`catalog`·`LootFeedUi`…)는 자리마다 다르고 그 파일들은 남의 lock 일 때가 잦다 —
    /// T168(`LetterSpacingUi`)·T178(`SurfaceUi`)과 같은 길이다. `tools/check_border_radius.py` 가 «정본 줄 ↔ 표값 ↔ 호출» 을 견준다.
    /// </summary>
    public static class RadiusUi
    {
        public const string ResourcePath = "RadiusUi";

        static RadiusTable table;

        static RadiusTable Table
        {
            get
            {
                if (table != null) return table;
                TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
                if (ta == null) throw new KeyNotFoundException("Resources/" + ResourcePath + ".json 이 없다");
                table = RadiusTable.From(J.Obj(MiniJson.Parse(ta.text)));
                return table;
            }
        }

        /// <summary>1rem 이 몇 캔버스 px 인가 — 정본 `:root` 글꼴(높이 기준)과 같은 셈(catalog `rem_h` × 앱 높이).</summary>
        public static float PxPerRem { get { return UiKit.L("rem_h") * UiKit.RefH; } }

        /// <summary>표의 반지름(캔버스 px). 키 꼬리(`_r_rem`·`_r_w`·`_r_px`)가 단위를 정한다.</summary>
        public static float Px(string key)
        {
            return (float)RadiusRules.Px(Table.Get(key), key, PxPerRem, UiKit.RefW, KeylineUi.CssPx);
        }

        /// <summary>정본이 «각진»(0) 자리인가 — <see cref="UiShapes.RoundedMultiplier"/> 는 0 을 «배율 1 = 반지름 24px» 로 읽으므로 0 은 여기서 갈라 민판을 쓴다.</summary>
        public static bool IsSquare(string key) { return Px(key) <= 0.01f; }

        /// <summary><see cref="UiKit.Rounded"/> 와 같되 반지름을 표에서 읽는다. 표값 0(정본 `border-radius: 0`)이면 각진 <see cref="UiKit.Panel"/>.</summary>
        public static Image Rounded(Transform parent, string name, string colorKey, string key)
        {
            return IsSquare(key) ? UiKit.Panel(parent, name, colorKey) : UiKit.Rounded(parent, name, colorKey, Px(key));
        }

        /// <summary><see cref="PopupKit.Outlined"/> 와 같되 반지름을 표에서 읽는다 — 테(line) + 안쪽 면(face). 표값 0 이면 각진 테와 면.</summary>
        public static Image Outlined(Transform parent, string name, string faceKey, string key, float line, string lineKey = "pp_line")
        {
            if (!IsSquare(key)) return PopupKit.Outlined(parent, name, faceKey, Px(key), line, lineKey);
            RectTransform rt = UiKit.Box(parent, name);
            UiKit.Panel(rt, "line", lineKey);
            Image face = UiKit.Panel(rt, "face", faceKey);
            PopupKit.Inset(face.rectTransform, line);
            return face;
        }
    }
}
