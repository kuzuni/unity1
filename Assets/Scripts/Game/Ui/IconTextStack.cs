using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T110·T108 — 이모지가 섞인 **두 줄 이상** 문구를 «줄마다 아이콘 + 글자» 로 세로로 세운다(정본 `.btn` 안의 `<br>`:
    /// «판매<br>🪙 +N» · «자동 🔁<br>ON» 꼴). 가로 <see cref="UiKit.IconTextRow"/> 는 줄바꿈을 모른 채 한 줄로 누르므로
    /// 줄바꿈(<c>\n</c>)에서 잘라 줄마다 <see cref="UiKit.IconTextRow"/> 를 세우고 세로 레이아웃으로 쌓는다 — 그래서
    /// 아이콘 규칙(표에 있는 이모지만 · 나머지 글자 그대로 · 아이콘 한 칸 = 글자 크기 정사각)은 가로 줄과 똑같다.
    /// 줄이 하나뿐이면 가로 줄 하나를 감싼 상자라 모양이 <see cref="UiKit.IconTextRow"/> 와 같다.
    /// <para>왜 <c>UiKit.cs</c> 가 아니라 제 파일인가 — 그 파일은 T121 lock(글꼴 굽기) 이다(결정 275). 호출부(T87 lock 의 `Forge*`)가 풀리면 2회차가 잇는다.</para>
    /// </summary>
    public static class IconTextStack
    {
        /// <returns>세로 상자(줄 상자 <c>"line-1"</c>·<c>"line-2"</c>… · 줄 안은 <see cref="UiKit.IconTextRow"/> 와 같이 <c>"msg"</c>·<c>"ico-N"</c>).</returns>
        /// <param name="restKind">T461 — 둘째 줄부터 쓸 종류(정본 `<small>` 줄 = §1 예외 칸 `Micro`). null 이면 모든 줄이 <paramref name="kind"/>.</param>
        public static RectTransform Build(Transform parent, string name, TextKind kind, string msg, string colorKey = null,
                                          TextAlignmentOptions align = TextAlignmentOptions.Center, float lineGap = 0f, TextKind? restKind = null)
        {
            RectTransform box = UiKit.Box(parent, name);
            var lay = box.gameObject.AddComponent<VerticalLayoutGroup>();
            lay.childAlignment = align.ToString().IndexOf("Left", System.StringComparison.Ordinal) >= 0 ? TextAnchor.MiddleLeft
                               : align.ToString().IndexOf("Right", System.StringComparison.Ordinal) >= 0 ? TextAnchor.MiddleRight
                               : TextAnchor.MiddleCenter;
            lay.childControlWidth = true; lay.childControlHeight = true;
            lay.childForceExpandWidth = false; lay.childForceExpandHeight = false;
            lay.spacing = lineGap;
            string[] lines = (msg ?? "").Split('\n');
            for (int i = 0; i < lines.Length; i++)
                UiKit.IconTextRow(box, "line-" + (i + 1), i > 0 && restKind.HasValue ? restKind.Value : kind, lines[i], colorKey, align);
            return box;
        }

        /// <summary>
        /// T108 — 줄 끝에 **아이콘 키로** 아이콘 한 칸을 더한다(정본이 이모지 표가 아니라 `IconGen.img('autoloop')` 처럼 직접 아이콘을 박는 자리).
        /// 칸(<c>"ico-N"</c>)은 정본 `.ico` 의 인라인 상자 — 폭 = 왼 마진 + 한 변 + 오른 마진 · 높이 = 줄 높이(<paramref name="lineHeightPx"/>) 로 두고,
        /// 그림(<c>"img"</c>)은 칸 가운데에 한 변 <paramref name="sizePx"/> 정사각으로 **넘치게** 놓는다 — 정본 `margin: -.32em … -.32em` 이
        /// 아이콘을 줄 높이보다 크게 그리면서 줄상자는 안 키우는 것과 같다. 치수는 카탈로그(`ico_em`·`ico_my_em`·`ico_mr_em`·`auto_loop_ico_*`)에서 호출부가 읽어 넘긴다.
        /// </summary>
        public static Image AppendIcon(RectTransform line, string iconKey, float sizePx, float lineHeightPx, float marginLeftPx = 0f, float marginRightPx = 0f)
        {
            int n = 0;
            for (int i = 0; i < line.childCount; i++) if (line.GetChild(i).name.StartsWith("ico-")) n++;
            RectTransform cell = UiKit.Box(line, "ico-" + (n + 1));
            var le = cell.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = marginLeftPx + sizePx + marginRightPx;
            le.preferredHeight = lineHeightPx;
            le.flexibleWidth = 0f;
            Image img = UiKit.Icon(cell, "img", iconKey);
            UiKit.Anchor(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((marginLeftPx - marginRightPx) * 0.5f, 0f), sizePx, sizePx);
            return img;
        }

        /// <summary>
        /// T108 — 줄마다 크기를 **직접 재어** <see cref="LayoutElement"/> 로 박는다(글자 조각은 <c>GetPreferredValues</c> · 아이콘 칸은 제 <see cref="LayoutElement"/>).
        /// 중첩 레이아웃 그룹의 선호값에 기대지 않는다 — 런 292 PNG 에서 안쪽 <see cref="UiKit.IconTextRow"/> 의 선호 폭이 0 으로 읽혀 줄이 사라진 전례(T138 2회차 · `LootFeed`·`RewardBurst.LabelRow` 와 같은 길).
        /// 굵게·아이콘을 다 더한 **뒤 마지막에** 부른다(굵은 글자는 폭이 다르다).
        /// </summary>
        public static void Fit(RectTransform stack)
        {
            for (int i = 0; i < stack.childCount; i++)
            {
                RectTransform line = stack.GetChild(i) as RectTransform;
                if (line == null || !line.name.StartsWith("line-")) continue;
                float w = 0f, h = 0f;
                for (int k = 0; k < line.childCount; k++)
                {
                    Transform piece = line.GetChild(k);
                    TextMeshProUGUI tm = piece.GetComponent<TextMeshProUGUI>();
                    LayoutElement pl = piece.GetComponent<LayoutElement>();
                    if (tm != null) { Vector2 pv = tm.GetPreferredValues(); w += pv.x; h = Mathf.Max(h, pv.y); }
                    else if (pl != null) { w += pl.preferredWidth; h = Mathf.Max(h, pl.preferredHeight); }
                }
                LayoutElement le = line.GetComponent<LayoutElement>() ?? line.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = w; le.preferredHeight = h; le.flexibleWidth = 0f; le.flexibleHeight = 0f;
            }
        }

        /// <summary>
        /// T110 2회차 — <see cref="PopupKit.Btn"/> 이 세운 한 줄 라벨을 끄고 그 자리에 세로 갈래를 세운다(정본 `.btn` 안 `<br>`/`<small>` 두 줄 라벨:
        /// «판매<small>coin +N</small>» · «건너뛰기<br>gem N» · «레벨 N 업그레이드<br><small>coin N · ⏱ T</small>»). 이모지는 표(`UiText`)로 아이콘이 된다.
        /// 굵게·키라인(<see cref="KeylineUi.BtnFace"/>)은 Btn 의 라벨과 같이 걸고 마지막에 <see cref="Fit"/>. T108 <c>ForgeSheet.AutoBtn</c> 과 같은 꼴.
        /// </summary>
        /// <param name="keylineKey">면 표(<see cref="KeylineUi.BtnFace"/>) 대신 쓸 폭 키 — 정본이 그 버튼만 따로 적은 자리(예 `.fi-card .fi-skip 4px` · T109 14회차). null 이면 면 표.</param>
        public static RectTransform ReplaceLabel(Button b, TextKind kind, string msg, string inkKey, string faceKey, string keylineKey = null)
        {
            RectTransform rt = b.GetComponent<RectTransform>();
            Transform plain = rt.Find("label");
            if (plain != null) plain.gameObject.SetActive(false);
            // T461 — 정본 667 `.btn small { font-size: .7rem }`: 둘째 줄부터는 §1 예외 칸 `Micro` 로 찍고(하한 36 아래로 내려가는 유일한 길 · 결정 633)
            //   크기는 표 `TextSizeUi.json` `btn_small`(.7rem)을 **첫 줄 × (.7 / btn_font_rem .88)** 비율로 준다 — 첫 줄이 하한으로 커진 만큼 같이 따라가야
            //   정본의 «본문:잔글씨» 비가 화면에 남는다(등재 판정 ⓓ · 결정 782). 수는 두 표에서만 온다.
            RectTransform stack = Build(rt, "label-stack", kind, msg, inkKey, TextAlignmentOptions.Center, 0f, TextKind.Micro);
            float smallRatio = TextSizeUi.Rem("btn_small") / UiKit.L("btn_font_rem");
            float smallMin = UiCatalog.Instance.Kind(TextKind.Micro).min;
            float firstFs = UiCatalog.Instance.Kind(kind).size;
            stack.offsetMin = new Vector2(0f, UiKit.H("btn_lip"));
            string kl = keylineKey ?? KeylineUi.BtnFace(faceKey);
            // T352 12회차 — **둘째 줄부터는 정본에서 `<small>` 이고 `.btn small`(667)이 `font-weight: 400` 을 준다.**
            //   이 도우미를 부르는 넷이 전부 정본의 `<button>본문<small>잔글씨</small>` 꼴이다:
            //   `판매<small>🪙 +N</small>`(ui.js 3266 · 비교 팝업·판매 경고) · `건너뛰기<small>💎 N</small>` · `레벨 N 업그레이드<small>…</small>`.
            //   여태는 **모든 줄에 Bold 를 박아** 잔글씨까지 굵었다 — 곧 `.btn small` 은 «남의 lock 뒤» 가 아니라
            //   **이 공용 도우미 한 곳**이 쥐고 있던 자리였다(10·11회차가 «ForgeCraftPopup.cs 뒤» 로 적은 것을 바로잡는다).
            //   ⚠ 링(키라인)은 줄마다 그대로 건다 — 굵기와 다른 축이다(T109).
            //   ⚠ **줄 노드로 센다** — `UiKit.RowTexts` 는 글자를 **평평하게 전부** 주므로(한 줄에 글자가 둘이면 어긋난다)
            //   이 파일이 65행에서 이미 쓰는 `line-` 이름 기준을 그대로 쓴다.
            int li = 0;
            for (int i = 0; i < stack.childCount; i++)
            {
                Transform line = stack.GetChild(i);
                if (line == null || !line.name.StartsWith("line-")) continue;
                foreach (TextMeshProUGUI t in UiKit.RowTexts(line as RectTransform))
                {
                    t.fontStyle = FontStyles.Bold;
                    if (li > 0) TextWeightUi.Regular(t, "btn_small");   // 둘째 줄부터 = 정본 `<small>` = 400
                    if (li > 0) t.fontSize = Mathf.Max(smallMin, firstFs * smallRatio);   // T461 — 둘째 줄부터 = 정본 `.btn small` .7rem(첫 줄 비율)
                    if (!string.IsNullOrEmpty(kl)) PopupKit.Ring(t, kl, "pp_line");
                }
                li++;
            }
            Fit(stack);
            return stack;
        }

        /// <summary>줄 수(정본 `<br>` 개수 + 1).</summary>
        public static int LineCount(RectTransform stack)
        {
            int n = 0;
            for (int i = 0; i < stack.childCount; i++) if (stack.GetChild(i).name.StartsWith("line-")) n++;
            return n;
        }
    }
}
