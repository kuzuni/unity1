using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Data;
using Forge.Core.Dungeon;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 던전 목록 시트(ROUTINE T21 · 원작 ui.js openDungeons/closeDungeons · index.html #dungeon-modal.sheet · 샷 042251).
    /// 탭바가 보이는 «탭 화면»(흰 전체화면 + 가로 배너 4 + 왼쪽 아래 ◀) — 탭바의 던전 탭이 빨간 ✕ 가 되고(T22 `TabBar.SetPopupX`) 그것을 누르면 `Switch(null)` → `Switched` 로 닫힌다.
    /// 배너 = 그림(IconGen dg_*) 위에 왼쪽 위 보상 아이콘+이름 · (잠김이면 자물쇠+해금 조건) · 오른쪽 열쇠 n/2 + [열기].
    /// </summary>
    public sealed class DungeonSheet : MonoBehaviour
    {
        public static DungeonSheet Instance { get; private set; }

        static readonly Dictionary<string, string[]> RewardIcon = new Dictionary<string, string[]>
        {
            { "hammer", new[] { "hammer", "coin" } }, { "ghost", new[] { "ticket" } }, { "invasion", new[] { "eggCracked" } }, { "zombie", new[] { "potion" } },
        };
        static readonly Dictionary<string, string> KeyIcon = new Dictionary<string, string>
        {
            { "hammer", "key" }, { "ghost", "key_green" }, { "invasion", "key_orange" }, { "zombie", "key_red" },
        };
        /// <summary>원작 DG_ICON — 던전 얼굴(상세 헤더·클리어 팝업).</summary>
        public static readonly Dictionary<string, string> FaceIcon = new Dictionary<string, string>
        {
            { "hammer", "hammer" }, { "ghost", "ghost" }, { "invasion", "egg" }, { "zombie", "zombie" },
        };
        public static string SceneIcon(string id) { return "dg_" + id; }

        DungeonUiHost host;
        RectTransform body;
        readonly List<RectTransform> banners = new List<RectTransform>();
        readonly Dictionary<string, Button> openButtons = new Dictionary<string, Button>();

        public bool IsOpen { get { return gameObject.activeSelf; } }
        public int BannerCount { get { return banners.Count; } }
        public Button OpenButton(string id) { Button b; return openButtons.TryGetValue(id, out b) ? b : null; }

        /// <summary>접착층이 Ready 된 뒤 한 번 — 시트를 만들고 탭바에 건다(원작 onTabClick('dungeon') → openDungeons).</summary>
        public static DungeonSheet Install(DungeonUiHost h)
        {
            if (Instance != null) return Instance;
            UiRoot root = UiRoot.Instance;
            if (root == null) return null;
            RectTransform rt = UiKit.Box(root.PanelHost, "sheet-dungeon");
            DungeonSheet s = rt.gameObject.AddComponent<DungeonSheet>();
            s.host = h;
            UiKit.Panel(rt, "bg", "pp_paper");
            UiKit.Line(rt, "line", "pp_line", UiKit.L("line3_px"), true);
            UiKit.Button(rt, "hit", null);
            s.body = UiKit.Box(rt, "body");
            DungeonPopups.BackButton(rt, s.Close);
            rt.gameObject.SetActive(false);
            root.TabBar.OpenRequested += s.OnTabRequest;
            root.TabBar.Switched += s.OnTabSwitched;
            h.DungeonEvent += s.OnDungeonEvent;
            return s;
        }

        private void Awake() { Instance = this; }
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (host != null) host.DungeonEvent -= OnDungeonEvent;
        }

        void OnTabRequest(string key)
        {
            if (key == "dungeon") Open();
            else Close();
        }

        /// <summary>탭 전환(원작 switchTab) — 시트 탭이 열리든 ✕ 로 홈에 가든 던전 시트는 닫힌다(TabBar.Switch 가 popup ✕ 를 지운 뒤 알린다).</summary>
        void OnTabSwitched(string tab) { Close(); }

        void OnDungeonEvent(DungeonEvent e)
        {
            switch (e.Kind)
            {
                case DungeonEventKind.Toast: DungeonPopups.Toast(e.Text); break;
                case DungeonEventKind.OpenDungeons: if (IsOpen) Rebuild(); break;
                case DungeonEventKind.RenderDungeonDetail: DungeonDetailPopup.Refresh(); break;
                // T134 3회차 — 정본 dungeons.js 180 `UI.rewardBurst(r)`(from 없음 · 소탕은 팝업 없이 이펙트만) · ⚡ 소탕 토스트는 Core Dungeons.Sweep 이 따로 낸다 · 종전 T22 대체 토스트는 걷었다
                case DungeonEventKind.RewardBurst: RewardBurst.Play(RewardBurst.Rewards(e.Rewards), null); host.RenderTopBar(); break;
                case DungeonEventKind.RenderTopBar: host.RenderTopBar(); break;
                case DungeonEventKind.ShowDungeonClear: DungeonClearPopup.Show(DungeonDefs.Find(e.Id), e.Stage, e.Rewards); break;
                case DungeonEventKind.SetupStage: UpdateStageLabel(); break;
            }
        }

        /// <summary>«+해머 302 · +코인 27.1k» 한 줄 — T22 가 rewardBurst 대신 쓰던 토스트 문구(T134 3회차부터 소탕 자리는 진짜 연출을 부른다 · 이 줄은 남겨 둔다).</summary>
        public static string RewardLine(DungeonRewards r)
        {
            var parts = new List<string>();
            if (r == null) return "";
            if (r.Hammers > 0) parts.Add("해머 +" + NumFmt.Fmt(r.Hammers));
            if (r.Coins > 0) parts.Add("코인 +" + NumFmt.Fmt(r.Coins));
            if (r.Tickets > 0) parts.Add("티켓 +" + NumFmt.Fmt(r.Tickets));
            if (r.EggCurrency > 0) parts.Add("깨진 알 +" + NumFmt.Fmt(r.EggCurrency));
            if (r.Potions > 0) parts.Add("물약 +" + NumFmt.Fmt(r.Potions));
            return string.Join(" · ", parts.ToArray());
        }

        /// <summary>원작 updateStageLabel 의 던전 갈래 — 진행 중이면 «던전 이름 N단계», 아니면 본대 라벨은 전투(T8)가 쥔다.</summary>
        public void UpdateStageLabel()
        {
            if (Hud.Instance == null || host.Dungeons == null) return;
            if (host.Dungeons.InRun)
            {
                var run = host.Dungeons.Run;
                DungeonDef d = DungeonDefs.Find(J.Str(run["id"]));
                if (d != null) Hud.Instance.SetStage(d.Kr + " " + J.Int(run["stage"]) + "단계");
            }
        }

        public void Open()
        {
            if (!DungeonUiHost.Ready) return;
            Rebuild();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            UiRoot.Instance.TabBar.SetPopupX("dungeon");
        }

        public void Close()
        {
            DungeonDetailPopup.Close();
            if (!gameObject.activeSelf) return;
            gameObject.SetActive(false);
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.XTab == "dungeon") tb.SetPopupX(null);
        }

        /// <summary>목록을 다시 그린다(원작 openDungeons 본문 — 열쇠 수·잠금이 바뀌면 다시).</summary>
        public void Rebuild()
        {
            for (int i = body.childCount - 1; i >= 0; i--) Destroy(body.GetChild(i).gameObject);
            banners.Clear();
            openButtons.Clear();
            host.Dungeons.Ensure();

            float W = UiKit.RefW;
            float padTop = DungeonPopups.RemL("sheet_pad_top_rem");
            float y = padTop;
            float titleH = DungeonPopups.LineH(TextKind.Title);
            TextMeshProUGUI title = DungeonPopups.Bold(body, "title", TextKind.Title, "던전", "pp_ink");
            UiKit.Place(title.rectTransform, 0f, y, W, titleH);
            y += titleH + DungeonPopups.RemL("sheet_gap_rem");

            float subW = W * UiKit.L("sheet_sub_maxw");
            float subH = DungeonPopups.LineH(TextKind.Sub) * 2f;
            TextMeshProUGUI sub = DungeonPopups.Para(body, "sub", TextKind.Sub, "던전 열쇠는 매일 09:00에 보충됩니다. 열쇠는 던전을 완료할 때만 소모됩니다", "pp_ink", TextAlignmentOptions.Center);
            sub.fontStyle = FontStyles.Bold;
            LineHeight.Apply(sub, "sheet_sub_lh");   // T354 5회차 — 정본 3854 `.sheet-sub { line-height: 1.4 }`(퀘스트·상점의 같은 자리는 T331·T333 lock 뒤)
            UiKit.Place(sub.rectTransform, (W - subW) * 0.5f, y, subW, subH);
            y += subH + DungeonPopups.RemL("sheet_sub_mb_rem");

            float bw = W * UiKit.L("dg_banner_w");
            float bh = bw / UiKit.L("dg_banner_aspect");
            float gap = DungeonPopups.RemL("dg_banner_gap_rem");
            for (int i = 0; i < DungeonDefs.All.Length; i++)
            {
                DungeonDef d = DungeonDefs.All[i];
                RectTransform b = Banner(d, bw, bh);
                UiKit.Place(b, (W - bw) * 0.5f, y, bw, bh);
                // 정본 `.modal-card.sheet .dg-banner`(style.css 3875) `0 .25rem 0 rgba(0,0,0,.3)` — 배너가 시트 위에 한 겹 떠 있다.
                // 자리를 잡은 **뒤**에 부른다(그늘이 상자 크기를 읽는다) · 반지름은 배너 테와 같은 표값.
                UiShadow.Drop(b, "dgbanner_lip", DungeonPopups.RemL("dg_banner_radius_rem"));
                banners.Add(b);
                y += bh + gap;
            }
        }

        RectTransform Banner(DungeonDef d, float bw, float bh)
        {
            bool ok = host.Dungeons.Unlocked(d.Id);
            double keys = host.Dungeons.Keys(d.Id);
            float line3 = UiKit.L("line3_px");
            float radius = DungeonPopups.RemL("dg_banner_radius_rem");
            RectTransform rt = UiKit.Box(body, "dg-" + d.Id);
            RectTransform face = DungeonPopups.Bordered(rt, "frame", "app_bg", radius, line3);
            // T178 2회차 — 정본 style.css 1952 `.dg-banner { background: linear-gradient(120deg, var(--bg,#444c56), #161b22) }`:
            // 배너 제 바탕이 비스듬한 겹이다(일러스트가 없는 던전은 이것만 보인다). 여태 단색 한 장이었다.
            SurfaceArt.Fill(face, "bg-grad", "dg_banner", bw, bh);
            Image scene = UiKit.Icon(face, "scene", SceneIcon(d.Id));
            scene.preserveAspect = false;
            // 정본 1982 `.dg-banner::before` — **왼쪽 제목 자리 스크림**(일러스트 위에 얹혀 흰 제목을 살린다).
            // 정본 주석이 값의 내력까지 적어 뒀다(«R8 .36→.22 · 도달 62%→42%»). 클론엔 이 겹이 아예 없었다.
            SurfaceArt.Fill(face, "scrim", "dg_banner_scrim", bw, bh);
            if (!ok)
            {
                CanvasGroup cg = rt.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = UiKit.L("dg_locked_alpha");
            }

            float padX = DungeonPopups.RemL("dg_banner_pad_x_rem");
            float padY = DungeonPopups.RemL("dg_banner_pad_y_rem");
            float nameH = DungeonPopups.LineH(TextKind.Body);
            float ico = nameH * UiKit.L("dg_rw_icon_em") / 1.25f;
            float x = padX;
            string[] rw;
            if (RewardIcon.TryGetValue(d.Id, out rw))
                for (int i = 0; i < rw.Length; i++)
                {
                    Image img = UiKit.Icon(rt, "rw-" + rw[i], rw[i]);
                    UiKit.Place(img.rectTransform, x, padY + (nameH - ico) * 0.5f, ico, ico);
                    // T332 15회차 — 정본 1993~1998 `.dg-rw .dg-rw-ico { filter: drop-shadow(0 .10em .16em rgba(0,0,0,.55)) }`.
                    //   정본 주석(1988~1991)이 구실을 적어 뒀다: 흰 원형 판을 걷고 아이콘을 슬롯 가득 키운 대신 «판독은 아이콘 키라인 + 드롭섀도가 보증한다».
                    //   길이가 `em` 이라 표는 **상자 비율**(`_f`)로 적는다 — 슬롯 `.dg-rw` 가 1.62em 이고 아이콘이 그 100% 라 1em = 상자/1.62 다.
                    DropShadow.Apply(img, "dg_rw_ico");
                    x += ico * 0.62f;
                }
            x += ico * 0.6f;
            TextMeshProUGUI name = DungeonPopups.Bold(rt, "name", TextKind.Body, d.Kr, "white", TextAlignmentOptions.Left);
            UiKit.Outline(name, "pp_line", UiKit.L("dg_name_outline"));
            UiKit.Place(name.rectTransform, x, padY, bw * 0.55f, nameH);

            if (!ok)
            {
                float lh = DungeonPopups.LineH(TextKind.Sub);
                float lk = lh * 0.9f;
                Image lockImg = UiKit.Icon(rt, "lock", "lock");
                UiKit.Place(lockImg.rectTransform, padX, padY + nameH + (lh - lk) * 0.5f, lk, lk);
                TextMeshProUGUI lockT = DungeonPopups.Bold(rt, "lock-text", TextKind.Sub, d.Unlock + " 도달 시 해금", "dg_lock", TextAlignmentOptions.Left);
                UiKit.Place(lockT.rectTransform, padX + lk * 1.15f, padY + nameH, bw * 0.6f, lh);
            }

            float btnW = DungeonPopups.RemL("dg_btn_w_rem"), btnH = DungeonPopups.RemL("dg_btn_h_rem");
            float keysH = DungeonPopups.LineH(TextKind.Sub);
            float rightGap = DungeonPopups.RemL("dg_right_gap_rem");
            float colH = (ok ? keysH + rightGap : 0f) + btnH;
            float colTop = (bh - colH) * 0.5f;
            float colRight = bw - padX;
            if (ok)
            {
                float kIco = keysH * 1.65f / 1.25f;
                string kText = NumFmt.Fmt(keys) + "/" + DungeonRules.MaxKeys;
                TextMeshProUGUI kt = DungeonPopups.Bold(rt, "keys", TextKind.Sub, kText, "dg_keys", TextAlignmentOptions.Right);
                UiKit.Outline(kt, "pp_line", UiKit.L("dg_name_outline"));
                float ktW = btnW * 0.6f;
                UiKit.Place(kt.rectTransform, colRight - ktW, colTop, ktW, keysH);
                string kIcon; KeyIcon.TryGetValue(d.Id, out kIcon);
                Image ki = UiKit.Icon(rt, "key", kIcon ?? "key");
                UiKit.Place(ki.rectTransform, colRight - ktW - kIco * 1.05f, colTop + (keysH - kIco) * 0.5f, kIco, kIco);
            }
            string id = d.Id;
            Button open = DungeonPopups.Pill(rt, "open", "열기", ok ? DungeonPopups.Skin.Blue : DungeonPopups.Skin.Gray, TextKind.Button,
                () => DungeonDetailPopup.Open(id), DungeonPopups.RemL("btn_radius_rem"), ok);
            UiKit.Place(DungeonPopups.Root(open), colRight - btnW, colTop + (ok ? keysH + rightGap : 0f), btnW, btnH);
            openButtons[d.Id] = open;
            return rt;
        }
    }
}
