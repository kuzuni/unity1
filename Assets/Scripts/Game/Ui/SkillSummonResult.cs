using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
using Forge.Core.Mounts;
using Forge.Core.Pets;
using Forge.Game.Gallery;
using Forge.Core.Skills;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 소환 결과 연출 팝업(원작 ui.js openSummonResult · summonEntries · groupSummonEntries · tickSummonResult · fireSummonHero · finishSummonResult · onSummonResultTap · summonSoloInfo · buildSummonReflection · 스킬·펫·탈것 공용).
    /// 원작 타임라인 그대로: ① 빛 모임(SR_CHARGE 240ms) → ② 등급 오름차순으로 셀 팝(≤10 = 125ms 간격 · &gt;10 = 행 웨이브 300/40ms + 등급 경계 200ms 정지) →
    /// ③ 최고 등급(전설↑ · 단독)은 150ms 홀드백 뒤 주역 비트 + 화면 섬광 → ④ 150ms 여운 뒤 [확인] · 등급 집계 칩 · x1 은 요약+[다시 소환]. 탭 = 스킵 · 끝난 뒤 탭 = 닫기.
    /// 11개부터 같은 항목을 한 셀로 묶는다(×N · 조각 +N · NEW). 그림 = 슬롯의 그림(스킬 sk_* · 알 등급색 egg · summon-result-image-unify).
    /// 광채·소환진·먼지는 CSS 다층 그라데이션이라 정점 색 원판·섬광·바닥 타원으로 줄였다(결정 기록 · 주인 눈 확인). 천개·광선·별·바닥 반사는 T179 가 <see cref="SummonFx"/> 로 세운다(무대판). 효과음은 <see cref="PetSkillHost"/> 훅(T30).
    /// </summary>
    public sealed class SkillSummonResultView : MonoBehaviour
    {
        public const string ModalName = "summon-result";

        public sealed class Entry
        {
            public string Key, IconKey, IconTint, Rarity, Name, Sub, Extra;
            public bool IsNew, Bonus;
            public int Qty = 1, NewQty;
            public string FaceName;
            public GalleryKind FaceKind = GalleryKind.Pets;
        }

        sealed class Cell
        {
            public RectTransform Root;
            public CanvasGroup Group;
            public RectTransform OrbWrap;
            public Entry Entry;
            public float BaseScale;
            public float Pop;
            public bool On;
            public float OnAt;
            public bool Heroic;
            /// <summary>T334 3회차 ⓑ — 흡기 전 제자리(anchoredPosition)와 «슬롯 → 광원» 벡터(정본 `--dx/--dy`).</summary>
            public Vector2 Home, ToLight;
            /// <summary>T459 ⓨ — 고등급 광채 원판(맥동은 이 원판의 배율).</summary>
            public Image Glow;
            /// <summary>T459 ⓩ — 구슬 본체와 그 위를 훑는 스페큘러 띠(done 뒤 처음 필요할 때 세운다).</summary>
            public Image Orb, Sweep;
            public float SweepD;
            /// <summary>T334 5회차 — 아이들 호흡 전 구체 래퍼의 제자리.</summary>
            public Vector2 OrbHome;
            /// <summary>T334 7회차 — 물러남 전 이 셀 그림들의 제 색(한 번만 담는다).</summary>
            public Graphic[] Tint;
            public Color[] TintHome;
            /// <summary>T334 12회차 — 착지 스파크(정본 `.sr-spark`) — 구체 래퍼 한가운데에 겹친다.</summary>
            public Image Spark;
            /// <summary>T334 15회차 — 비행 잔상(정본 `.sr-ghost`) — 구체 **뒤**(z 0)에 깔리는 흐린 복제.</summary>
            public Image Ghost;
            /// <summary>T448 — 동급(peer) 착지 링(정본 `.sr-cell.peer.on::after`) — 셀 **뒤**(z −1) 폭 100% 정사각 · 자기 착지에 한 번 돈다.</summary>
            public Image PeerRing;
        }

        public static SkillSummonResultView Current { get; private set; }

        SkillPetSheet sheet;
        PetSkillModal.Handle handle;
        readonly List<Cell> cells = new List<Cell>();
        bool ejectSet;   // T455 — «슬롯 → 광원» 벡터를 잰 적이 있는가(창이 아니라 상태로 가른다).
        readonly List<float> delays = new List<float>();
        /// <summary>T334 18회차 — 끝난 뒤의 잔잔한 고리(정본 `.sr-idle` · `.done` 에서만 돈다).</summary>
        readonly List<Image> idleRings = new List<Image>();
        /// <summary>T334 19회차 — 예고 충격파 한 쌍(정본 `.sr-shock` · `.sr-shock.echo`)과 그 단계 판.</summary>
        Image shockMain, shockEcho;
        Sprite[] shockMainSteps, shockEchoSteps;
        Color shockLine;
        float shockHalf;
        /// <summary>T334 21회차 — 빛 모임(정본 `.sr-charge` · z 60 맨 위) · 등급 예고 세기.</summary>
        Image chargeBurst;
        float preK;
        /// <summary>T334 22회차 — 수렴 빛줄기(정본 `.sr-streaks i`): 회전 홀더 · 막대 · 그 스포크의 시작 거리·지연.</summary>
        sealed class Streak { public RectTransform Holder, Bar; public Image Img; public float RRem, DelayMs, LenPx; }
        readonly List<Streak> streaks = new List<Streak>();
        /// <summary>T334 23회차 — 깊이 평면 셋(정본 `.sr-motes` z 1 · `.sr-dust` z 35 · `.sr-near` z 42)의 점들.</summary>
        sealed class Mote { public RectTransform Rt; public Image Img; public Vector2 Home; public int I; public SummonParticleSpec.Layer L; }
        readonly List<Mote> motes = new List<Mote>();
        /// <summary>T334 19회차 — 굴림 에너지(정본 `--sr-e`) — 본파의 최종 반경이 이것에 물린다.</summary>
        float srEnergy;
        /// <summary>T334 16회차 — 등급 챕터 경계(정본 `_srTierBreaks`): 켜지는 시각(ms)과 그 등급.</summary>
        struct TierBreak { public float At; public int Tier; public Color Rc, Lite; public Image Pulse, Ring, Wick; public Sprite[] Steps; public float Half; }
        readonly List<TierBreak> tierBreaks = new List<TierBreak>();
        List<Entry> entries;
        List<Entry> rollList;
        int rolls;
        float start;
        int idx;
        bool done, holdback, heroFired;
        /// <summary>T334 2회차 — 정본 `tickSummonResult` 의 상태 기계(Core · UnityEngine 0). 시각·판정은 전부 이것이 쥔다.</summary>
        SummonSeqRun seq;
        SummonFx fx;   // T179 연출 겹(무대판에서만)
        public SummonFx Fx { get { return fx; } }

        // ── T334 2회차 — 정본 다섯 상태를 그대로 연다(3회차가 이 위에 겹을 얹는다) ──
        /// <summary>`charging` — 홀드백에서 마지막 한 칸을 남긴 구간(정지가 아니라 축적).</summary>
        public bool Charging { get { return seq != null && seq.Charging; } }
        /// <summary>`hero` — 주역(최고 등급) 셀이 착지했다.</summary>
        public bool Hero { get { return seq != null && seq.Hero; } }
        /// <summary>`flash` — 뜸들인 단독 등장의 전 화면 섬광(홀드백일 때만).</summary>
        public bool Flash { get { return seq != null && seq.Flash; } }
        /// <summary>`wipe` — 홀드백이 없는 주역의 셀 중심 가산 원형 와이프(대량 소환).</summary>
        public bool Wipe { get { return seq != null && seq.Wipe; } }
        // 주역 비트(`HeroKicking`)는 아직 안 연다 — 그 길이가 3회차에 정해진다(위 Open 의 주석).
        int heroIdx = -1;
        string best;
        Action repeat;
        Image flash;
        /// <summary>T334 3회차 — 주역 와이프(정본 `.sr-wipe` · 홀드백이 **없는** 주역에서만). 가산 혼합이라 씬을 안 죽인다.</summary>
        Image wipe;
        float wipeAt = -1f;
        /// <summary>T334 6회차 — 화면 킥(정본 `srshakehit`)이 시작한 시각 · 흔들 판과 그 제자리.</summary>
        float kickAt = -1f;
        /// <summary>T459 ⓧ — 열린 시각(입장 셰이크의 시계 · 끝나면 −1).</summary>
        float enterAt = -1f;
        /// <summary>T334 9회차 — 주역 충격파 링(정본 `.sr-cell.heroic::after`)과 그 최대 배율(정본 `--ringmax` · 배치마다 다르다).</summary>
        Image heroRing;
        float ringMax = 3.2f;
        /// <summary>T334 10회차 — 주역 광창(정본 `.sr-beam`) · 클론에 없던 겹이다.</summary>
        Image heroBeam;
        /// <summary>T334 11회차 — 셀마다 하나씩 광원 자리에서 켜지는 재점화 플래시(정본 `.sr-relight`).</summary>
        readonly List<Image> relights = new List<Image>();
        /// <summary>주역이 착지한 벽시계 시각(물러남·킥이 같이 쓴다).</summary>
        float heroAtWall = -1f;
        RectTransform wrap;
        Vector2 wrapHome;
        float flashAt = -1f;
        /// <summary>T334 3회차 ⓑ — 충전 구간이 움직이는 것들: 소환진·중앙 광원·비네트(정본 `.sr-floor`·`.sr-halo`·`.sr-wrap::before`).</summary>
        Image floorImg, haloImg, vigImg;
        /// <summary>T454 ⓒ — 배경 `bg-a`(정본 `--bg-pre-a` → `.done` 의 `--bg-a`): 예고값에서 승격값으로 .5s ease-out(표 TransitionUi `sr_bg_done`).</summary>
        Image bgAImg;
        Color bgAPre, bgADone;
        double bgPromoteMs = -1;
        /// <summary>등급 위치 0~1(정본 pk · 자가 본다).</summary>
        public double Pk { get; private set; }
        /// <summary>배경 `bg-a` 의 예고값·승격값·지금 색(자가 본다).</summary>
        public Color BgAPre { get { return bgAPre; } }
        public Color BgADone { get { return bgADone; } }
        public Color BgAColor { get { return bgAImg != null ? bgAImg.color : Color.clear; } }
        /// <summary>done 뒤 승격 전이가 도는 중인가.</summary>
        public bool BgPromoting { get { return bgPromoteMs >= 0; } }
        /// <summary>
        /// T385 2회차 ⑶ — 구체 스페큘러를 **아이콘 위에** 한 겹 더(정본 `.sr-ico::after` 6539~6547 · 표 <c>OrbIconUi.json</c>).
        ///
        /// ⚠ 상자를 **구체 기준**으로 잡는다. 정본은 «아이콘 한 변의 220%» 인데 정본 아이콘이 구체의 1/2.4 라
        /// 그 상자가 곧 **구체의 91.7%** 다. 클론 아이콘은 구체의 0.60~0.62 라 같은 220% 를 아이콘에 곱하면
        /// 겹이 구체의 1.32배로 부풀어 밝은 점이 구체 19% 가 아니라 7.7% 에 온다(1회차 셈이 잡은 11%p 어긋남 ·
        /// <see cref="OrbIconRules.ToOrbFrac"/>). 그래서 «표의 상자 × 정본 아이콘 비율» 을 구체 한 변에 곱한다.
        ///
        /// 합성은 정본이 `mix-blend-mode: screen` 인데 UGUI 엔 그 합성이 없다 — 가산(<see cref="CraftFxPoly.Screen"/>)이
        /// 그 근사고, 바탕이 어두울수록 둘이 가깝다(<see cref="OrbIconRules.ScreenBlend"/> 가 그 어긋남을 잰다).
        /// </summary>
        static void OrbIconSpec(RectTransform wrap, float cw)
        {
            float side = cw * (float)(OrbIconUi.Box.W * OrbIconUi.IconFracOfOrb);
            RectTransform b = UiKit.Box(wrap, "sr-ico-spec");
            UiKit.Anchor(b, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, side, side);
            Image im = b.gameObject.AddComponent<Image>();
            im.raycastTarget = false;
            im.preserveAspect = false;
            im.sprite = SummonFx.BakeOrbIconSpec("sr-ico-spec");
            Material m = CraftFxPoly.Screen();
            if (m != null) im.material = m;
            im.color = new Color(1f, 1f, 1f, OrbIconUi.Alpha);   // 정본 `opacity: .35` — 표값은 여기 한 번만 나온다
        }

        /// <summary>
        /// T332 17회차 ⑴ — 정본 6521 `.sr-ico { filter: drop-shadow(0 3px 5px rgba(0,0,0,.55)) }`(표 <c>DropShadowUi.json</c> `sr_ico`).
        ///
        /// 정본 6509~6518 주석이 처방 세 겹을 적어 뒀고(⑴ 접지 그림자 ⑵ 4% 배럴 스케일 ⑶ 구체 스페큘러 한 겹) **이것이 첫 겹**이다 —
        /// «글리프가 구체 표면 위에 «놓인» 두께를 만든다». ⑵⑶ 은 T385 2회차가 이미 걸었다(<see cref="OrbIconSpec"/>).
        ///
        /// 정본은 필터를 **감싸개**(`.sr-ico`)에 걸어 그 안의 셋(스킬 `.ico` · 알 `.ico.sr-egg` · 탈것 `.mt-face` 썸네일)이 다 같은 그림자를 진다.
        /// 클론엔 감싸개가 없으므로 **그 그림 자체**에 건다 — 같은 자리 · 같은 실루엣이다.
        /// 탈것 썸네일이 이미 지고 있는 `.mt-face.has-thumb > img`(7604 · <see cref="ForgeUi.ThumbShadow"/>)와는 **선택자가 달라 정본에서도 함께** 걸린다.
        ///
        /// ⚠ 배율을 그림자에도 얹는다: CSS 는 `filter` 를 구운 **뒤** `transform: scale(1.04)` 가 그림자까지 함께 키우는데,
        /// 클론 그림자는 아이콘의 **형제**라 아이콘의 `localScale` 이 안 따라온다.
        /// </summary>
        /// <remarks>이모지 폴백(정본 6538 `.mt-face:not(.has-thumb)`)은 글자라 실루엣이 없어 못 건다 — 3D 썸네일이 아직 안 구워진 순간의 자리다.</remarks>
        static void SrIcoShadow(Image ico)
        {
            if (ico == null) return;
            Image sh = DropShadow.Apply(ico, "sr_ico");
            if (sh != null) sh.rectTransform.localScale = ico.rectTransform.localScale;
        }

        /// <summary>T334 4회차 — 소환진의 룬 눈금 띠(정본 `.sr-floor::after`). 충전 중엔 `steps(9)` 로 점등하고 그 밖에는 느리게 호흡한다.</summary>
        Image tickImg;
        Color tickBase;
        Color floorBase, haloBase;
        Vector3 floorHome;
        float chargeAt = -1f;
        RectTransform foot;
        GameObject hint, ok, chips, solo;
        /// <summary>T458 2회차 — [확인] 버튼 팝(정본 7188 `.done .sr-ok { animation: srpop .32s … both }`) — 켜진 시각 · 제자리 · 알파 그룹.</summary>
        RectTransform okRect; CanvasGroup okGroup; Vector2 okHome; float okAt = -1f;
        string kind;

        public bool Done { get { return done; } }
        public int CellCount { get { return cells.Count; } }
        public int OnCount { get { int n = 0; foreach (Cell c in cells) if (c.On) n++; return n; } }
        // ── T334 3회차 ⓑ — 충전 겹을 자가 수로 잰다(정지 프레임이 없다는 것은 «움직였다» 로만 증명된다) ──
        /// <summary>비네트(`.sr-wrap::before`)의 지금 불투명도.</summary>
        public float VigAlpha { get { return vigImg != null ? vigImg.color.a : -1f; } }
        /// <summary>소환진(`.sr-floor`)의 지금 배율(제자리 대비).</summary>
        public float FloorScale { get { return floorImg != null && floorHome.x != 0f ? floorImg.rectTransform.localScale.x / floorHome.x : -1f; } }
        /// <summary>
        /// 소환진(`.sr-floor`)의 지금 밝기 — 세 성분의 **합**이다.
        ///
        /// ⚠ 최댓값으로 재면 안 된다(런 512 가 가르친 것): 등급색이 `#ff1c1c`(ultimate)처럼 **한 성분이 이미 1.0** 이면
        ///    CSS `filter: brightness()` 도 거기서 잘려 그 성분은 안 움직인다 — 오르는 것은 나머지 두 성분이고,
        ///    화면에서는 «붉은 판이 흰 쪽으로 씻긴다» 로 보인다. 합은 그 두 갈래를 다 담는다.
        /// </summary>
        public float FloorBright { get { if (floorImg == null) return -1f; Color c = floorImg.color; return c.r + c.g + c.b; } }
        /// <summary>주역 충격파 링 — 자가 본다.</summary>
        public Image HeroRing { get { return heroRing; } }
        /// <summary>그 링의 최대 배율(정본 `--ringmax` · 배치마다 다르다).</summary>
        public float RingMax { get { return ringMax; } }
        /// <summary>T448 — 동급(peer) 착지 링들 — 자가 본다(동급 셀 하나에 하나).</summary>
        public List<Image> PeerRings { get { var l = new List<Image>(); foreach (Cell c in cells) if (c.PeerRing != null) l.Add(c.PeerRing); return l; } }
        /// <summary>그 링의 최대 배율(정본 6702 `.sr-cell.peer { --ringmax: 1.5 }`).</summary>
        public float RingMaxPeer { get { return SummonFxStyle.H("ringmax_peer"); } }

        /// <summary>룬 눈금 띠(정본 `.sr-floor::after`)의 지금 불투명도 — 없으면 −1.</summary>
        public float TickAlpha { get { return tickImg != null ? tickImg.color.a : -1f; } }
        /// <summary>룬 눈금 띠가 구운 판을 쥐고 있는가(원판을 늘려 쓰면 눈금 굵기가 각도마다 달라진다).</summary>
        public bool TickBaked { get { return tickImg != null && tickImg.sprite != null; } }

        /// <summary>소환진의 지금 색 — 등급색으로 물들었는지 자가 본다(정본은 `.done` 에서만 물든다).</summary>
        public Color FloorColor { get { return floorImg != null ? floorImg.color : Color.clear; } }

        /// <summary>중앙 광원(`.sr-halo`)의 지금 불투명도.</summary>
        public float HaloAlpha { get { return haloImg != null ? haloImg.color.a : -1f; } }
        /// <summary>셀 하나가 제자리에서 광원 쪽으로 빨려든 거리(흡기 · 정본 `srinhale`).</summary>
        public float CellPulledIn(int i) { return i < 0 || i >= cells.Count ? -1f : (cells[i].Root.anchoredPosition - cells[i].Home).magnitude; }

        public Button OkButton { get; private set; }
        public Button AgainButton { get; private set; }

        static GameDefs Defs { get { return PetSkillHost.Instance.Data.Defs; } }

        // ===== 진입(원작 openSummonResult · summonEntries) =====

        public static SkillSummonResultView Open(SkillPetSheet sheet, string kind, SkillSummonResult r, Action repeat)
        {
            var list = new List<Entry>();
            foreach (SkillSummonResult.Item it in r.Results)
                list.Add(new Entry { Key = "sk:" + it.Def.Id, IconKey = "sk_" + it.Def.Id, Rarity = it.Def.Rarity, Name = it.Def.Name, IsNew = it.IsNew });
            return Open(sheet, kind, list, r.BestRarity, repeat);
        }

        public static SkillSummonResultView Open(SkillPetSheet sheet, string kind, SummonResult r, Action repeat)
        {
            var list = new List<Entry>();
            foreach (SummonResult.Item it in r.Results)
            {
                string kr = Defs.RarityKr.Get(it.Rarity, it.Rarity);
                list.Add(new Entry
                {
                    Key = "eg:" + it.Rarity + (it.Extra ? ":x" : ""), IconKey = "egg", IconTint = PetSkillStyle.RarityHex(Defs, it.Rarity), Rarity = it.Rarity,
                    Name = it.Extra ? PetSkillStyle.T("sr_egg_bonus", kr) : PetSkillStyle.T("sr_egg", kr), IsNew = false, Bonus = it.Extra,
                });
            }
            return Open(sheet, kind, list, r.BestRarity, repeat);
        }

        /// <summary>원작 summonEntries('mount') — 키 `mt:이름` · 3D 얼굴(`mountFace`) · MOUNT_KR 이름 · 신규/중복.</summary>
        public static SkillSummonResultView Open(SkillPetSheet sheet, string kind, MountSummonResult r, Action repeat)
        {
            var list = new List<Entry>();
            foreach (MountSummonResult.Item it in r.Results)
                list.Add(new Entry { Key = "mt:" + it.Name, IconKey = "winder", Rarity = it.Rarity, Name = Defs.MountKr.Get(it.Name, it.Name), IsNew = it.IsNew, FaceName = it.Name, FaceKind = GalleryKind.Mounts });
            return Open(sheet, kind, list, r.BestRarity, repeat);
        }

        public static SkillSummonResultView Open(SkillPetSheet sheet, string kind, List<Entry> rollsList, string bestRarity, Action repeat)
        {
            if (rollsList == null || rollsList.Count == 0) return null;
            if (Current != null) Current.Close();
            PetSkillModal.Handle h = sheet.Modal.OpenFull(ModalName, () => { if (Current != null) Current.OnTap(); });
            var v = h.Root.gameObject.AddComponent<SkillSummonResultView>();
            Current = v;
            v.sheet = sheet;
            v.handle = h;
            v.kind = kind;
            v.repeat = repeat;
            v.Build(rollsList);
            return v;
        }

        // ===== groupSummonEntries =====
        static List<Entry> Group(string kind, List<Entry> rollsList, bool merge)
        {
            string dup = kind == "skill" ? PetSkillStyle.T("sr_dup_skill") : kind == "mount" ? PetSkillStyle.T("sr_dup_mount") : null;
            var list = new List<Entry>();
            if (merge)
            {
                var map = new Dictionary<string, Entry>();
                foreach (Entry e in rollsList)
                {
                    Entry g;
                    if (map.TryGetValue(e.Key, out g)) { g.Qty++; if (e.IsNew) g.NewQty++; }
                    else
                    {
                        g = new Entry { Key = e.Key, IconKey = e.IconKey, IconTint = e.IconTint, Rarity = e.Rarity, Name = e.Name, Bonus = e.Bonus, Qty = 1, NewQty = e.IsNew ? 1 : 0, FaceName = e.FaceName, FaceKind = e.FaceKind };
                        map[e.Key] = g;
                        list.Add(g);
                    }
                }
            }
            else
                foreach (Entry e in rollsList)
                    list.Add(new Entry { Key = e.Key, IconKey = e.IconKey, IconTint = e.IconTint, Rarity = e.Rarity, Name = e.Name, Bonus = e.Bonus, Qty = 1, NewQty = e.IsNew ? 1 : 0, FaceName = e.FaceName, FaceKind = e.FaceKind });
            foreach (Entry g in list)
            {
                g.IsNew = g.NewQty > 0;
                g.Sub = Defs.RarityKr.Get(g.Rarity, g.Rarity);
                g.Extra = dup != null && g.NewQty == 0 ? PetSkillStyle.T("sr_dup", dup, g.Qty) : string.Empty;
            }
            // 등급 오름차순(안정) — 마지막이 최고 등급
            var stable = new List<Entry>(list);
            for (int i = 1; i < stable.Count; i++)
            {
                Entry x = stable[i];
                int j = i - 1;
                while (j >= 0 && RarityIdx(stable[j].Rarity) > RarityIdx(x.Rarity)) { stable[j + 1] = stable[j]; j--; }
                stable[j + 1] = x;
            }
            return stable;
        }

        static int RarityIdx(string r) { return Array.IndexOf(Defs.Rarities, r); }
        /// <summary>T342 ⓔ — 정본 `.sr-cell[data-tier=N] .sr-orb` 의 filter(표 FilterUi `summon_orb_N`)를 색 한 칸에 건다.
        /// 스프라이트를 굽는 `UiFilter.ApplyColor` 가 아니라 Core 셈 `FilterRules.Apply` 를 바로 부른다 — 이 자리는 틴트 색이 곧 구체 색이라 그것을 걸러야 정본과 같다. 알파는 그대로.</summary>
        public static Color OrbFilter(Color c, int tier)
        {
            FilterSpec f = UiFilter.Table.Get("summon_orb_" + Mathf.Clamp(tier, 0, 5));
            double r = c.r, g = c.g, b = c.b;
            FilterRules.Apply(f, ref r, ref g, ref b);
            return new Color((float)r, (float)g, (float)b, c.a);
        }
        /// <summary>순위 → 등급 이름(T334 2회차 — 상태 기계가 돌려주는 «이번 프레임 최고 순위» 를 소리 이름으로).</summary>
        static string RarityOf(int rank) { return rank >= 0 && rank < Defs.Rarities.Length ? Defs.Rarities[rank] : null; }
        static bool Hi(string r) { return r == "legendary" || r == "ultimate" || r == "mythic"; }

        /// <summary>원작 srCols — 마지막 행 충전율이 가장 높은 열 수.</summary>
        public static int Cols(int n)
        {
            int[] cand = n >= 6 && n <= 10 ? new[] { 3, 4, 5 } : new[] { 5, 4, 6 };
            int bestC = cand[0];
            float bestFill = -1f;
            foreach (int c in cand)
            {
                float fill = (n % c == 0 ? c : n % c) / (float)c;
                if (fill > bestFill + 1e-9f) { bestFill = fill; bestC = c; }
            }
            return bestC;
        }

        // ===== 그리기 =====
        void Build(List<Entry> rollsList)
        {
            rolls = rollsList.Count;
            rollList = rollsList;
            int mergeFrom = Mathf.RoundToInt(PetSkillStyle.L("sr_merge_from"));
            entries = Group(kind, rollsList, rolls >= mergeFrom);
            int n = entries.Count;
            best = entries[n - 1].Rarity;
            holdback = Hi(best) && entries[n - 1].Qty == 1;
            heroIdx = Hi(best) ? n - 1 : -1;
            bool heroRow = heroIdx > 0 && n <= 10;
            int cols = Cols(n);
            bool one = n == 1, dense = n > 24, mid = !dense && n > 10;
            bool stage = n <= 10;
            float W = UiKit.RefW, Hh = UiKit.RefH;
            RectTransform c = handle.Content;
            UiKit.Fill(c);
            wrap = c; wrapHome = c.anchoredPosition;   // 정본 `.sr-wrap` — 주역 착지에 이 판이 흔들린다

            // ---- 배경(남색 방사 → 검정) ----
            Image bg = UiKit.Panel(c, "bg", "pp_line");
            bg.color = PetSkillStyle.C("sr_bg_d");
            Image glowB = PetSkillKit.Disc(c, "bg-b", PetSkillStyle.C("sr_bg_c"));
            glowB.preserveAspect = false;
            UiKit.Anchor(glowB.rectTransform, new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.5f), Vector2.zero, W * 2.4f, Hh * 1.5f);
            // T454 ⓒ — 정본은 열 때 `--bg-pre-a` = 등급색을 `.24 × PRE_BG × pk` 만 섞은 **예고값**(ui.js 536~538 · 일반 판은 0 = 기본색)이고,
            //   `.done` 에서 `--bg-a` = `.24`(507) 로 **승격**한다(7150 `.5s ease-out`). 전엔 처음부터 승격값이라 «색으로 미리 알려주기» 가 0 이었다. 값은 표 · 셈은 Core.
            Pk = SummonBgRules.Pk(RarityIdx(best), Defs.Rarities.Length);
            float mixF = PetSkillStyle.L("sr_bg_a_mix_f");
            Color bgABase = PetSkillStyle.C("sr_bg_a"), bestCol = PetSkillStyle.Rarity(Defs, best);
            bgAPre = Color.Lerp(bgABase, bestCol, (float)SummonBgRules.PreMixF(mixF, PetSkillStyle.L("sr_bg_pre_f"), Pk));
            bgADone = Color.Lerp(bgABase, bestCol, mixF);
            Image glowA = PetSkillKit.Disc(c, "bg-a", bgAPre);
            bgAImg = glowA;
            glowA.preserveAspect = false;
            UiKit.Anchor(glowA.rectTransform, new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.5f), Vector2.zero, W * 1.5f, Hh * 0.9f);
            Image halo = PetSkillKit.Disc(c, "halo", PetSkillStyle.Rarity(Defs, best));
            halo.color = new Color(halo.color.r, halo.color.g, halo.color.b, PetSkillStyle.L("sr_halo_a"));   // T454 4회차 — 박힌 .18 을 표로(값 그대로 · 정본 맞춤은 이 축 밖)
            halo.preserveAspect = false;
            UiKit.Anchor(halo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, PetSkillStyle.Rem(26f), PetSkillStyle.Rem(16f));
            haloImg = halo; haloBase = halo.color;
            // 비네트(정본 `.sr-wrap::before` z 2) — 고정 그라디언트 한 장, 움직이는 것은 불투명도·배율뿐.
            RectTransform vigRt = UiKit.Box(c, "sr-vig");
            UiKit.Fill(vigRt);
            vigImg = vigRt.gameObject.AddComponent<Image>();
            vigImg.sprite = SummonFx.BakeVig("sr-vig");
            vigImg.preserveAspect = false;
            vigImg.raycastTarget = false;
            vigImg.color = new Color(1f, 1f, 1f, 0f);

            // ---- 머리 ----
            float padT = PetSkillStyle.Px("sr_pad_top_rem"), padX = PetSkillStyle.Px("sr_pad_x_rem"), padB = PetSkillStyle.Px("sr_pad_bottom_rem");
            float title = UiCatalog.Instance.Kind(TextKind.Title).size;
            float headH = title * 1.4f + PetSkillStyle.Px("sr_title_pad_y_rem") * 2f;
            float headY = padT + Hh * PetSkillStyle.L("sr_head_top_f");
            RectTransform head = UiKit.Box(c, "sr-head");
            UiKit.Place(head, 0f, headY, W, headH);
            string tt = PetSkillStyle.T("sr_title_x", PetSkillStyle.T(kind == "pet" ? "sr_title_pet" : kind == "mount" ? "sr_title_mount" : "sr_title_skill"), rolls);
            float tw = PetSkillKit.TextWidth(TextKind.Title, tt) + title * 1.2f + PetSkillStyle.Px("sr_title_pad_x_rem") * 2f;
            RectTransform band = UiKit.Box(head, "sr-title");
            UiKit.Anchor(band, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, tw, headH);
            // T178 27회차 — 정본 **6207** `.sr-title { background: linear-gradient(90deg, rgba(255,255,255,0),
            //   rgba(12,20,52,.85) 14%, rgba(12,20,52,.85) 86%, rgba(255,255,255,0)) }` — 띠는 단색이 아니라
            //   **양끝이 알파 0 으로 사라지는** 가로 겹이다(정본 주석의 뜻: 공중에서 뚝 끊기지 않게). 클론은 한 색이라 양끝이 각졌다.
            //   새 조각을 얹지 않고 **그 판의 그림을 바꾼다** — 정본도 배경 하나고, «bg» 를 찾는 다른 자들이 그대로 산다.
            Image bandBg = UiKit.Panel(band, "bg", "pp_line");
            bandBg.sprite = SurfaceArt.Bake("sr_title_band", headH > 0f ? tw / headH : 1f, tw);
            bandBg.color = Color.white;   // 구운 그림 위에 색을 또 곱하지 않는다
            // 정본 **6220** `.sr-title::before/::after` — 띠 위·아래 **금색 헤어라인 1 CSS px**, 좌우로 사라진다(같은 값 · 자리만 다르다).
            float hair = Mathf.Max(1f, KeylineUi.CssPx);
            foreach (bool top in new[] { true, false })
            {
                RectTransform hr = UiKit.Box(band, top ? "hair-top" : "hair-bot");
                hr.anchorMin = new Vector2(0f, top ? 1f : 0f); hr.anchorMax = new Vector2(1f, top ? 1f : 0f);
                hr.pivot = new Vector2(0f, top ? 1f : 0f);
                hr.offsetMin = new Vector2(0f, top ? -hair : 0f); hr.offsetMax = new Vector2(0f, top ? 0f : hair);
                Image hi = hr.gameObject.AddComponent<Image>();
                hi.raycastTarget = false;
                hi.sprite = SurfaceArt.Bake("sr_title_hair", hair > 0f ? tw / hair : 1f, tw);
                hi.color = Color.white;
            }
            Image tico = UiKit.Icon(band, "ico", kind == "pet" ? "egg" : kind == "mount" ? "winder" : "ticket");
            UiKit.Place(tico.rectTransform, PetSkillStyle.Px("sr_title_pad_x_rem"), (headH - title * 1.05f) * 0.5f, title * 1.05f, title * 1.05f);
            TextMeshProUGUI tx = PetSkillKit.Text(band, "t", TextKind.Title, tt, PetSkillStyle.C("white"), TextAlignmentOptions.Left);
            UiKit.TextShadow(tx, "sr_title");   // T333 15회차 — 정본 6207 `.sr-title` 두 겹 중 검정 낙하 겹(0 .2rem .55rem .85 · 결정 738) · 파랑 후광 겹은 언더레이 한 겹으론 못 낸다
            LetterSpacing.Apply(tx, "sr_title_ls_em");   // T168 3회차 — 정본 style.css 6207 `.sr-title { letter-spacing: .07em }`
            UiKit.Place(tx.rectTransform, PetSkillStyle.Px("sr_title_pad_x_rem") + title * 1.2f, 0f, tw, headH);

            // ---- 발(집계 · 힌트 · 확인) ----
            float footH = Mathf.Max(PetSkillStyle.Px("sr_foot_min_rem"), PetSkillStyle.Px("sr_ok_h_rem") + UiCatalog.Instance.Kind(TextKind.Sub).size * 1.6f + PetSkillStyle.Px("sr_foot_gap_rem") * 2f);
            foot = UiKit.Box(c, "sr-foot");
            UiKit.Place(foot, padX, Hh - padB - footH, W - padX * 2f, footH);
            BuildFoot(footH, W - padX * 2f);

            // ---- 몸(그리드) ----
            float bodyY = headY + headH;
            float bodyH = Hh - padB - footH - bodyY;
            RectTransform body = UiKit.Box(c, "sr-body");
            UiKit.Place(body, padX, bodyY, W - padX * 2f, bodyH);
            float gw = W - padX * 2f;
            float gapX = PetSkillStyle.Px("sr_grid_gap_x_rem"), gapY = PetSkillStyle.Px("sr_grid_gap_y_rem");
            float cellW;
            if (one) cellW = Mathf.Min(PetSkillStyle.Px("sr_cell_one_max_rem"), W * 0.58f);
            else if (dense) cellW = (gw - PetSkillStyle.Px("sr_dense_gap_rem")) / PetSkillStyle.L("sr_dense_cols");
            else if (mid) cellW = Mathf.Min(PetSkillStyle.Px("sr_cell_mid_max_rem"), (gw - (cols - 1) * gapX - PetSkillStyle.Rem(1.2f)) / cols);
            else if (stage) cellW = Mathf.Min(PetSkillStyle.Px("sr_cell_stage_max_rem"), (gw - (cols - 1) * gapX - PetSkillStyle.Rem(0.4f)) / cols);
            else cellW = Mathf.Min(PetSkillStyle.Px("sr_cell_max_rem"), (gw - (cols - 1) * gapX - PetSkillStyle.Rem(0.4f)) / cols);
            if (dense) cols = Mathf.RoundToInt(PetSkillStyle.L("sr_dense_cols"));
            float sub = UiCatalog.Instance.Kind(TextKind.Sub).size;
            // T391 5회차 — 정본 7084 `.sr-grid.one .sr-name { font-size: 1.25rem }`(= 기준 캔버스 45.5px): x1 소환의 이름만 한 단 크다.
            //   클론은 여태 모든 셀에 `Sub` 36 을 줘서 그 자리가 **−21%** 였다(`check_text_kinds` 의 마지막 KNOWN).
            //   판 높이도 같은 글자 크기로 낸다 — `Sub` 로 잰 판(52.7px)에 44px 글자를 넣으면 TMP 가 줄을 통째로 버린다(런 528 함정).
            //   ⚠ 종류를 도우미로 감싸지 않는다 — `check_text_kinds` 는 **그 호출 문장 안의 `TextKind.X` 리터럴**을 읽는다(T365 15회차와 같은 갈래).
            float nameFs = UiCatalog.Instance.Kind(one ? TextKind.Button : TextKind.Sub).size;
            float nameH = dense ? 0f : TextClamp.BoxHeight(UiFont.Primary, nameFs, "sr_name", LineHeight.Table.Get("sr_name_lh"));   // T423 2회차 — 판 높이는 «클램프가 재는 높이» 다(표 `sr_name_h_em` × 코드에 박힌 0.62 를 걷었다)
            float subH = dense ? 0f : sub * 1.35f;
            float cellH = cellW + (dense ? 0f : PetSkillStyle.Px("sr_name_mt_rem") + nameH + PetSkillStyle.Px("sr_sub_mt_rem") + subH);
            float heroOrb = heroRow ? Mathf.Min(PetSkillStyle.Px("sr_hero_orb_rem"), W * 0.36f) : mid ? Mathf.Min(PetSkillStyle.Px("sr_hero_mid_orb_rem"), W * 0.3f) : dense ? Mathf.Min(PetSkillStyle.Px("sr_hero_dense_orb_rem"), W * 0.24f) : cellW;
            bool heroOwnRow = heroIdx >= 0 && (heroRow || mid || dense);
            int normal = heroOwnRow ? n - 1 : n;
            int rows = (normal + cols - 1) / cols;
            float gridH = rows * cellH + Mathf.Max(0, rows - 1) * gapY;
            float heroH = heroOwnRow ? heroOrb + PetSkillStyle.Px("sr_hero_top_rem") + PetSkillStyle.Px("sr_name_mt_rem") + sub * 1.6f + PetSkillStyle.Px("sr_sub_mt_rem") + sub * 1.5f + gapY : 0f;
            float totalH = gridH + heroH;
            ScrollRect scroll = null;
            RectTransform grid;
            if (totalH > bodyH)
            {
                grid = PetSkillKit.Scroll(body, "sr-grid", out scroll);
                UiKit.Fill((RectTransform)grid.parent);
                grid.sizeDelta = new Vector2(0f, totalH + PetSkillStyle.Rem(1f));
            }
            else
            {
                grid = UiKit.Box(body, "sr-grid");
                UiKit.Place(grid, 0f, (bodyH - totalH) * 0.5f, gw, totalH);
                if (stage)
                {
                    // 소환진(바닥 타원) — 그리드 아래
                    // 정본 `.sr-floor`(style.css 5809~5812)의 기본 바탕은 **푸른색**이고, 등급색으로 물드는 것은
                    // `#summon-result-modal.done` 한 줄(7182~7185)에서다 — ui.js 548 주석이 까닭을 댄다:
                    // «원색 그대로는 배경에 묻혀 소환진이 사라진다». 처음부터 등급 원색으로 칠하면 `charging` 의
                    // 밝기 램프도 화면에서 안 읽힌다(궁극의 #ff1c1c 는 빨강이 이미 1.0 이라 CSS 도 거기서 자른다).
                    Image floor = PetSkillKit.Disc(body, "sr-floor", SummonFxStyle.C("floor_fill"));
                    floor.preserveAspect = false;
                    // T449 — 정본 5771 `.stage.one .sr-floor { width: 64%; aspect-ratio: 2.6/1 }` · 5800 `.stage:not(.one) { 88%; 2.5/1 }`:
                    //   값은 같았지만 코드 두 곳에 박혀 있었다(§1) → 캐노피(SummonFx.cs 118~119)와 같은 꼴로 표 SummonFxUi.json 에서 읽는다.
                    string fk = one ? "floor_one_" : "floor_";
                    float fw = gw * SummonFxStyle.L(fk + "w_f"), fh = fw / SummonFxStyle.L(fk + "aspect");
                    UiKit.Anchor(floor.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -totalH * 0.5f + PetSkillStyle.Rem(1.4f)), fw, fh);
                    floor.transform.SetAsFirstSibling();
                    floorImg = floor; floorBase = floor.color; floorHome = floor.rectTransform.localScale;
                    // 룬 눈금 띠 — 소환진 위에 같은 상자로 얹는다(정본은 `::after` 라 같은 자리·같은 크기다).
                    RectTransform tickRt = UiKit.Box(floor.rectTransform, "sr-floor-ticks");
                    UiKit.Fill(tickRt);
                    tickImg = tickRt.gameObject.AddComponent<Image>();
                    tickImg.sprite = SummonFx.BakeFloorTicks("sr-floor-ticks", fw, fh);
                    tickImg.preserveAspect = false;
                    tickImg.raycastTarget = false;
                    tickBase = SummonFxStyle.C("floor_line");
                    tickImg.color = tickBase;
                    // T179 — 연출 겹(정본 sr-canopy 아치+빛발+스필 · sr-rays · sr-stars · ui.js 491~494: canopy = stage · compact = herorow) — 그리드 위 밴드·배경·별
                    fx = SummonFx.Build(body, floor.rectTransform, (bodyH - totalH) * 0.5f, gw, one, heroRow);
                }
            }
            for (int i = 0; i < n; i++)
            {
                Entry e = entries[i];
                bool heroic = i == heroIdx;
                bool ownRow = heroic && heroOwnRow;
                int k = ownRow ? 0 : i;
                float x, y, cw;
                if (ownRow)
                {
                    cw = heroOrb;
                    x = (gw - cw) * 0.5f;
                    y = gridH + gapY + PetSkillStyle.Px("sr_hero_top_rem");
                }
                else
                {
                    int rowN = Mathf.Min(cols, normal - (k / cols) * cols);
                    float rowW = rowN * cellW + (rowN - 1) * gapX;
                    cw = cellW;
                    x = (gw - rowW) * 0.5f + (k % cols) * (cellW + gapX);
                    y = (k / cols) * (cellH + gapY);
                }
                bool peer = heroIdx >= 0 && !heroic && e.Rarity == best;
                cells.Add(BuildCell(grid, e, i, x, y, cw, heroic, peer, dense, one));
            }
            // T179 3회차 — 바닥 반사의 원본(정본 buildSummonReflection: done 에서 이 그리드를 복제) · 셀별 구체 배율(--sz · heroic 확대는 복제본에서 뗀다)
            if (fx != null)
            {
                var orbSz = new List<float>(cells.Count);
                foreach (Cell cc in cells) orbSz.Add(cc.BaseScale);
                fx.SetReflectSource(grid, orbSz);
            }

            // ---- 셀별 광원 재점화(정본 `.sr-relights`/`.sr-relight` 6364~6389) ----
            // 정본 실측이 이 겹의 근거다: 2~4번 셀이 사출되는 900ms 내내 광원 ±20px 평균 휘도가 45.1~55.2 로
            // **시작 프레임 baseline 53.3 보다도 낮았다** — 빛 → 아이템의 인과가 첫 셀과 주역에만 걸려 있었다.
            // 그래서 셀이 뜰 때마다 광원 자리(= `.sr-wrap` 중심)에 그 셀 등급색으로 짧은 플래시를 한 번 켠다.
            // ⚠ 정본 주석: 이 층을 **셀 안에 넣으면 안 된다** — 셀의 배율·비행 이동·`opacity:0` 이 전부 곱해져
            //    광원에 서지도 제 밝기로 켜지지도 않는다. 그래서 여기서도 셀이 아니라 판(`body`)에 붙이고
            //    자리는 `wrap` 의 한가운데를 그대로 받는다(정본 «광원 좌표가 그냥 가운데»).
            // ⚠ 사다리(정본 z): 소환진 10 · 천개 20 · **재점화 26** · 충격파 30 · 격자 40 — 그래서 격자 **바로 아래**에 끼운다.
            //    (`SummonFx.Build` 의 겹들이 뒤에 붙으므로 여기서 자리를 정해야 한다.)
            if (grid != null && cells.Count > 0)
            {
                SummonRelightSpec rsp = SummonFxStyle.Relight;
                float rw = Mathf.Min(PetSkillStyle.Rem((float)rsp.WRem), (float)rsp.WVwF * W);
                RectTransform gp = (RectTransform)grid.parent;   // 넘치는 판은 그리드가 스크롤 뷰포트 안에 있다
                RectTransform host = UiKit.Box(gp, "sr-relights");
                UiKit.Fill(host);
                host.SetSiblingIndex(grid.GetSiblingIndex());   // 언제나 그리드 **바로 아래**
                for (int i = 0; i < cells.Count; i++)
                {
                    Entry e = cells[i].Entry;
                    Color rc = PetSkillStyle.Rarity(Defs, e.Rarity);
                    int tier = RarityIdx(e.Rarity);
                    double amt = SummonFxStyle.Hero.HiliteAmount(rc.r * 255.0, rc.g * 255.0, rc.b * 255.0, tier);
                    Color lite = Shade(rc, (float)amt);   // 정본 `--rc-lite` = srHilite(rc, tier)
                    RectTransform rt = UiKit.Box(host, "sr-relight");
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(rw, rw);
                    rt.anchoredPosition = Vector2.zero;
                    rt.position = wrap.TransformPoint(wrap.rect.center);   // 광원 = wrap 한가운데
                    Image ri = rt.gameObject.AddComponent<Image>();
                    ri.raycastTarget = false;
                    ri.preserveAspect = false;
                    ri.sprite = SummonFx.BakeRelight(
                        "sr-relight-" + ColorUtility.ToHtmlStringRGB(rc) + "-" + ColorUtility.ToHtmlStringRGB(lite), rc, lite);
                    Material rmat = CraftFxPoly.Screen();   // ⚠ 가산이라야 이웃을 «밝힌다»(정본 주석 · 알파 오버레이는 탁하게 죽인다)
                    if (rmat != null) ri.material = rmat;
                    ri.color = new Color(1f, 1f, 1f, 0f);
                    ri.rectTransform.localScale = Vector3.one * (float)SummonFxStyle.Relight.Relight.Sample(0, "scale", null);
                    relights.Add(ri);
                }
            }

            // ---- 빛 모임(정본 `.sr-charge` 6082~6115 · z 60 맨 위) ----
            // 정본 주석: «정점 배율 = 등급 예고(--pre-sc) × 굴림 에너지(1 + .42 × --sr-e). **두 축이 독립이라 곱한다** —
            //   신화가 하나 뜬 x1 과 일반만 나온 x75 가 **서로 다른 이유로** 커진다».
            // 굽는 판은 **한 장**이다 — 정지점이 굴림 에너지에 물리지만 그 수는 판마다 상수다(결정 691).
            // 이 블록은 충격파 뒤에 와야 한다(`preK`·`srEnergy` 를 거기서 잰다).

            // ---- 예고 충격파 한 쌍(정본 `.sr-shock` + `.echo` 6119~6146 · z 30) ----
            // 정본 주석 셋이 이 겹을 못 박았다: «빛이 터지는 정점(240ms)에 나가야 한다 — 0ms 에 터지면 아무것도
            //   없는 화면에서 링만 먼저 퍼진다»(지연) · «z 30 — 그리드(40)보다 **아래**다 … 압력파는 피사체 뒤에서
            //   퍼져야 피사체가 앞에 선 것으로 읽힌다» · «압력파가 하나면 «링 애니메이션», 둘이면 «터진 것» 으로
            //   읽힌다 — 부모 transform 과 곱해지지 않게 **형제 요소**로 둔다(::after 로 두면 배율이 중첩된다)».
            // 색은 «등급 예고»(`--pre-line`/`--pre-glow`)라 최고 등급으로 k 를 만들어 섞는다 — 등급색 원본이 아니라
            // 중립색에서 그쪽으로 당긴 것이다(정본 «알려 주는 건 «얼마나 센 게 온다» 뿐이고 «무엇» 은 끝까지 숨는다»).
            {
                SummonShockSpec sk = SummonFxStyle.Shock;
                SummonPreludeSpec pr = SummonFxStyle.Prelude;
                int bt = RarityIdx(best);
                float pk = (float)pr.K(bt);
                Color bc = PetSkillStyle.Rarity(Defs, best);
                Color preLine = Color.Lerp(SummonFxStyle.C("pre_line_base"), Shade(bc, (float)pr.LineShade), pk);
                // ⚠ 정본이 이름으로 경고한 자리 — «**셀 수가 아니라 굴림 수**로 잰다. 대량 소환은 같은 항목을
                //   한 셀로 묶으므로 x25 와 x75 가 똑같이 3셀이 될 수 있다. 75개를 뽑았다는 사실이 빛에 담겨야 한다».
                //   클론에서 굴림 수는 묶기 전의 개수 = 셀마다 `Qty` 의 합이다.
                int rolls = 0;
                for (int q = 0; q < entries.Count; q++) rolls += entries[q].Qty < 1 ? 1 : entries[q].Qty;
                srEnergy = (float)pr.Energy(rolls);
                float sw = PetSkillStyle.Rem((float)sk.WRem);
                shockMainSteps = new Sprite[sk.Steps];
                shockEchoSteps = new Sprite[sk.Steps];
                shockLine = preLine;
                shockHalf = sw * 0.5f;
                // 단계 판은 **쓸 때** 굽는다(챕터 링과 같은 까닭 — Open 한 프레임에 몰면 뒤 겹의 짧은 구간이 프레임 사이로 빠진다).
                shockMain = ShockPlate(c, "sr-shock", sw, ShockStep(false, 0));
                shockEcho = ShockPlate(c, "sr-shock-echo", sw, ShockStep(true, 0));

                // 빛 모임 — 판 높이와 같은 정사각(정본 `height: 100%; aspect-ratio: 1`) · 맨 위 형제(z 60).
                preK = pk;
                Color preMid = Color.Lerp(SummonFxStyle.C("pre_mid_base"), bc, pk);
                RectTransform cb = UiKit.Box(c, "sr-charge");
                cb.anchorMin = cb.anchorMax = new Vector2(0.5f, 0.5f);
                cb.pivot = new Vector2(0.5f, 0.5f);
                cb.sizeDelta = new Vector2(Hh, Hh);
                cb.anchoredPosition = Vector2.zero;
                Image cbi = cb.gameObject.AddComponent<Image>();
                cbi.raycastTarget = false;
                cbi.preserveAspect = false;
                cbi.sprite = SummonFx.BakeChargeBurst(
                    "sr-charge-" + ColorUtility.ToHtmlStringRGB(preMid) + "-" + srEnergy.ToString("0.00"), preMid, srEnergy);
                Material cm = CraftFxPoly.Screen();
                if (cm != null) cbi.material = cm;
                cbi.color = new Color(1f, 1f, 1f, 0f);
                chargeBurst = cbi;

                // ---- 수렴 빛줄기(정본 `.sr-streaks i` 5710~5726 · z 60) ----
                // 바깥에서 광원으로 **모여드는** 막대 9~24 개. 개수·각·거리·길이·지연이 전부 굴림 수와 번호에서
                // 결정론으로 나온다(정본 `summonStreaks`). 굽는 판은 **한 장**이고 스포크는 그것을 나눠 쓴다(결정 691).
                // ⚠ 정본 `transform-origin: 50% 100%` + `rotate(a) translateY(-r)` — **원점이 막대의 바깥 끝**이고
                //   막대는 거기서 **안쪽으로** 자란다. 그래서 홀더를 각만큼 돌리고, 막대는 피벗을 위끝(0.5,1)에 두어
                //   아래(= 광원 쪽)로 뻗게 한다. 안팎을 뒤집으면 «퍼지는 빛» 이 된다.
                SummonStreakSpec stk = SummonFxStyle.Streaks;
                Color preGlow = Color.Lerp(SummonFxStyle.C("pre_glow_base"), bc, pk);
                preGlow.a = (float)pr.GlowAlpha(bt);
                Sprite bar = SummonFx.BakeStreak(
                    "sr-streak-" + ColorUtility.ToHtmlStringRGB(preLine) + "-" + ColorUtility.ToHtmlStringRGB(preGlow),
                    preLine, preGlow, (float)(stk.GlowRem / (stk.Width(pk) * 0.5 + stk.GlowRem)));
                int sn = stk.Count(rolls);
                float barW = PetSkillStyle.Rem((float)stk.Width(pk));
                for (int i = 0; i < sn; i++)
                {
                    double ang, rr2, len, dly;
                    stk.Spoke(i, sn, out ang, out rr2, out len, out dly);
                    RectTransform spk = UiKit.Box(c, "sr-streak-" + i);
                    spk.anchorMin = spk.anchorMax = new Vector2(0.5f, 0.5f);
                    spk.pivot = new Vector2(0.5f, 0.5f);
                    spk.sizeDelta = Vector2.zero;
                    spk.anchoredPosition = Vector2.zero;
                    spk.localRotation = Quaternion.Euler(0f, 0f, -(float)ang);   // CSS 의 +각은 시계 방향
                    RectTransform bx = UiKit.Box(spk, "bar");
                    bx.anchorMin = bx.anchorMax = new Vector2(0.5f, 0.5f);
                    bx.pivot = new Vector2(0.5f, 1f);                              // 위끝 = 바깥 끝 = 정본 원점
                    bx.sizeDelta = new Vector2(barW, PetSkillStyle.Rem((float)len));
                    Image bi = bx.gameObject.AddComponent<Image>();
                    bi.raycastTarget = false;
                    bi.preserveAspect = false;
                    bi.sprite = bar;
                    Material bm = CraftFxPoly.Screen();
                    if (bm != null) bi.material = bm;
                    bi.color = new Color(1f, 1f, 1f, 0f);
                    streaks.Add(new Streak { Holder = spk, Bar = bx, Img = bi, RRem = (float)rr2, DelayMs = (float)dly, LenPx = PetSkillStyle.Rem((float)len) });
                }
            }

            // ---- 깊이 평면 셋(정본 `.sr-motes` 5992 z 1 · `.sr-dust` 6014 z 35 · `.sr-near` 6028 z 42) ----
            // 정본 주석: «구체 **앞을** 가로지르는 요소가 NEW 배지·이름판뿐이라 화면이 한 겹으로 납작하다 …
            //   깊이는 피사체 뒤가 아니라 **앞**에 무언가가 지날 때 생긴다» ·
            //   «⚠ 근평면을 45 위로 올리지 말 것 — 흐린 광점이 글자 위를 지나면 라벨 가독성이 그대로 무너진다» ·
            //   «배치는 난수 대신 **고정 수열**이다(소환할 때마다 튀지 않게)».
            // 개체 18 + 48 + 14 = 80 · 굽는 판은 겹마다 한 장씩 **셋**(결정 691 로 미리 셌다).
            {
                SummonParticleSpec ps = SummonFxStyle.Particles;
                Sprite mSp = SummonFx.BakeParticle("sr-mote", PetSkillStyle.C("white"), SummonFxStyle.C("mote_mid"), 0.42f, 0.72f, false);
                Sprite dSp = SummonFx.BakeParticle("sr-dust", SummonFxStyle.C("dust_fill"), SummonFxStyle.C("dust_fill"), 1f, 1f, true);
                Color nearCore = PetSkillStyle.C("white"); nearCore.a = 0.95f;
                Sprite nSp = SummonFx.BakeParticle("sr-near", nearCore, SummonFxStyle.C("near_mid"), 0.45f, 0.78f, false);
                // 배경(맨 뒤) · 중간(격자 앞) · 근평면(격자 앞이되 머리·발 뒤) — 사다리는 형제 차례로 옮긴다.
                MoteLayer(c, "sr-motes", ps.Motes, mSp, W, Hh, 0);
                MoteLayer(c, "sr-dust", ps.Dust, dSp, W, Hh, -1);
                MoteLayer(c, "sr-near", ps.Near, nSp, W, Hh, -1);
            }

            // ---- 끝난 뒤의 잔잔한 고리(정본 `.sr-idle` 6934~6948 · `.done` 에서만 보인다) ----
            // 챕터 링과 달리 **테 굵기가 안 변하므로** 한 장을 구워 배율로 날린다(17회차의 단계 갈아 끼우기가 필요 없다).
            {
                SummonIdleRingSpec ir = SummonFxStyle.IdleRing;
                float iw = PetSkillStyle.Rem((float)ir.WRem), ih = iw * 0.5f;
                Color ic = SummonFxStyle.C("idle_ring");
                Sprite isp = SummonFx.BakeTierRing("sr-idlering", ic,
                    PetSkillStyle.Rem((float)ir.BorderRem) / ih, 0f, PetSkillStyle.Rem((float)ir.GlowRem) / ih, 0f);
                for (int k = 0; k < ir.Count; k++)
                {
                    RectTransform rt = UiKit.Box(c, "sr-idle-" + k);
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(iw, iw);
                    rt.anchoredPosition = Vector2.zero;
                    Image im = rt.gameObject.AddComponent<Image>();
                    im.raycastTarget = false;
                    im.preserveAspect = false;
                    im.sprite = isp;
                    Material imat = CraftFxPoly.Screen();
                    if (imat != null) im.material = imat;
                    im.color = new Color(1f, 1f, 1f, 0f);
                    idleRings.Add(im);
                }
            }

            // ---- 섬광 ----
            flash = UiKit.Panel(c, "sr-flash", "pp_line");
            // T419 ⓐ — 정본 6148 `.sr-flash { mix-blend-mode: screen }`: 이 판은 덮는 것이 아니라 **밝힌다**.
            //   흰색일 때는 screen ≡ 보통 알파라 안 드러나지만, 홀드백 착지에서 `flash.color` 에 **등급색**을 칠하는 순간
            //   갈린다(정본은 그 색으로 화면을 밝히고 클론은 그 색 막을 덮었다 — 이웃 셀이 탁해진다).
            Material fmat = CraftFxPoly.Screen();
            if (fmat != null) flash.material = fmat;
            flash.color = new Color(1f, 1f, 1f, 0f);
            flash.raycastTarget = false;

            // ---- 주역 충격파 링(정본 `.sr-cell.heroic::after` 6754~6764) ----
            // 셀 **뒤**(z-index −1)에 폭 100% 정사각으로, 셀 위 가운데에 깔린다. 가산 혼합(screen)이라 와이프와 같은 재질.
            // 최대 배율은 배치마다 다르다(정본 `--ringmax`): 주역 단독 행 2.4 · 민 무대 격자 1.8 · 그 밖 3.2.
            if (heroIdx >= 0 && heroIdx < cells.Count && cells[heroIdx].Root != null)
            {
                RectTransform hc = cells[heroIdx].Root;
                float cw = hc.rect.width;
                RectTransform rr = UiKit.Box(hc, "sr-heroring");
                rr.anchorMin = new Vector2(0.5f, 1f); rr.anchorMax = new Vector2(0.5f, 1f);
                rr.pivot = new Vector2(0.5f, 1f);
                rr.sizeDelta = new Vector2(cw, cw);
                rr.anchoredPosition = Vector2.zero;
                rr.SetAsFirstSibling();
                heroRing = rr.gameObject.AddComponent<Image>();
                heroRing.raycastTarget = false;
                Color rcr = PetSkillStyle.Rarity(Defs, best);
                heroRing.sprite = SummonFx.BakeHeroRing("sr-heroring-" + ColorUtility.ToHtmlStringRGB(rcr), rcr);
                Material rm = CraftFxPoly.Screen();
                if (rm != null) heroRing.material = rm;
                heroRing.color = new Color(1f, 1f, 1f, 0f);
                // 광창(정본 `.sr-beam`) — 셀 가운데에 폭 100% 정사각. 링과 달리 셀 **앞**에 뜬다(정본은 `::after` 가 아니라 제 요소다).
                RectTransform bm = UiKit.Box(hc, "sr-beam");
                bm.anchorMin = bm.anchorMax = new Vector2(0.5f, 0.5f);
                bm.pivot = new Vector2(0.5f, 0.5f);
                bm.sizeDelta = new Vector2(cw, cw);
                bm.anchoredPosition = Vector2.zero;
                heroBeam = bm.gameObject.AddComponent<Image>();
                heroBeam.raycastTarget = false;
                int tier = RarityIdx(best);
                double amt = SummonFxStyle.Hero.HiliteAmount(rcr.r * 255.0, rcr.g * 255.0, rcr.b * 255.0, tier);
                Color lite = Shade(rcr, (float)amt);   // 정본 srHilite — 목표 휘도까지 흰 쪽으로 당긴다
                heroBeam.sprite = SummonFx.BakeBeam("sr-beam-" + ColorUtility.ToHtmlStringRGB(lite), lite);
                Material bmat = CraftFxPoly.Screen();
                if (bmat != null) heroBeam.material = bmat;
                heroBeam.color = new Color(1f, 1f, 1f, 0f);

                ringMax = heroRow ? SummonFxStyle.H("ringmax_herorow")
                    : (!mid && !dense ? SummonFxStyle.H("ringmax_stage") : SummonFxStyle.H("ringmax_default"));
            }

            // ---- 주역 와이프(정본 `.sr-wipe` 6184~6198) ----
            // 정본이 이 겹을 따로 둔 까닭이 주석에 있다: 홀드백이 없는 대량 소환에 **전 화면 섬광을 쓰면 안 된다**
            // — «x75 는 위쪽 20셀이 같이 하얗게 떠 등급 구분이 무너진다». 그래서 주역 셀 중심에서 번지는
            // **가산 원형 와이프**라 정점 순간에도 반경이 아직 작다. 혼합은 T173 이 세운 `CraftFxPoly.Screen()`.
            if (heroIdx >= 0)
            {
                RectTransform wr = UiKit.Box(c, "sr-wipe");
                UiKit.Fill(wr);
                wipe = wr.gameObject.AddComponent<Image>();
                wipe.raycastTarget = false;
                wipe.type = Image.Type.Simple;
                Color rc = PetSkillStyle.Rarity(Defs, best);
                wipe.sprite = SummonFx.BakeWipe("sr-wipe-" + ColorUtility.ToHtmlStringRGB(rc), rc);
                Material sm = CraftFxPoly.Screen();
                if (sm != null) wipe.material = sm;   // 못 찾으면 보통 알파로 그린다(연출이 사라지는 것보다 낫다 · T173 꼴)
                wipe.color = new Color(1f, 1f, 1f, 0f);
                wr.SetAsLastSibling();
            }

            // ---- 시각표(원작 tickSummonResult 의 지연) ----
            float charge = PetSkillStyle.L("sr_charge_ms"), slow = PetSkillStyle.L("sr_slow_step_ms"), rowMs = PetSkillStyle.L("sr_row_ms"), stag = PetSkillStyle.L("sr_row_stag_ms"), pause = PetSkillStyle.L("sr_tier_pause_ms"), hold = PetSkillStyle.L("sr_holdback_ms");
            if (n <= 10)
                for (int i = 0; i < n; i++) delays.Add(charge + i * slow + (holdback && i == n - 1 ? hold : 0f));
            else
            {
                float acc = 0f;
                for (int i = 0; i < n; i++)
                {
                    bool boundary = i > 0 && RarityIdx(entries[i].Rarity) > RarityIdx(entries[i - 1].Rarity);
                    if (boundary) acc += pause;
                    float dNow = charge + Mathf.Floor(i / (float)cols) * rowMs + (i % cols) * stag + acc + (holdback && i == n - 1 ? hold : 0f);
                    if (boundary)
                    {
                        // 정본 `_srTierBreaks` — «플래시는 첫 셀보다 반 박자 앞 · «예고 → 그 등급 등장» 의 인과가 서게».
                        SummonTierBreakSpec tb = SummonFxStyle.TierBreak;
                        int bt = RarityIdx(entries[i].Rarity);
                        Color brc = PetSkillStyle.Rarity(Defs, entries[i].Rarity);
                        double bamt = SummonFxStyle.Hero.HiliteAmount(brc.r * 255.0, brc.g * 255.0, brc.b * 255.0, bt);
                        tierBreaks.Add(new TierBreak
                        {
                            At = (float)tb.BreakAt(dNow, charge),
                            Tier = bt,
                            Rc = brc,
                            Lite = Shade(brc, (float)bamt),
                        });
                    }
                    delays.Add(dNow);
                }
            }
            // ---- 등급 챕터 펄스(정본 `.sr-tierbreaks`/`.sr-tierpulse` 6391~6416) ----
            // 대량 판(>10셀)의 등급 경계 정지를 채우는 예고다. 정본이 링 말고 **전화면 펄스**를 따로 둔 까닭이
            // 주석에 실측으로 있다: «링만으로는 화면 평균 휘도가 안 움직인다 — 면적이 작은 층은 아무리 밝아도
            // 중반 진폭 계측에 안 잡힌다. 챕터가 바뀌는 순간 화면 전체가 그 등급색으로 한 번 달아올랐다 식는다».
            // ⚠ 이 판은 **화면 크기**라야 뜻이 산다 — 그래서 재점화(격자 바로 아래)와 달리 `wrap` 직속으로 두고
            //    `body` **앞** 형제에 끼운다(사다리에서 소환진보다 뒤로 내려가지만, 가산 한 겹이라 그 차이보다
            //    «화면 전체» 가 훨씬 크다 · 정본 z 27).
            if (tierBreaks.Count > 0)
            {
                SummonTierBreakSpec tb = SummonFxStyle.TierBreak;
                RectTransform tbHost = UiKit.Box(c, "sr-tierbreaks");
                UiKit.Fill(tbHost);
                tbHost.SetSiblingIndex(body.GetSiblingIndex());
                float grow = -(float)tb.InsetF;   // 정본 `inset: -2%` — 화면 밖으로 문다
                for (int i = 0; i < tierBreaks.Count; i++)
                {
                    TierBreak b = tierBreaks[i];
                    RectTransform pr = UiKit.Box(tbHost, "sr-tierpulse");
                    UiKit.Fill(pr);
                    pr.localScale = Vector3.one * (1f + grow * 2f);
                    Image pi = pr.gameObject.AddComponent<Image>();
                    pi.raycastTarget = false;
                    pi.preserveAspect = false;
                    pi.sprite = SummonFx.BakeTierPulse(
                        "sr-tierpulse-" + ColorUtility.ToHtmlStringRGB(b.Rc) + "-" + ColorUtility.ToHtmlStringRGB(b.Lite), b.Rc, b.Lite);
                    Material pm = CraftFxPoly.Screen();
                    if (pm != null) pi.material = pm;
                    pi.color = new Color(1f, 1f, 1f, 0f);
                    b.Pulse = pi;

                    // 링(정본 `.sr-tierflash`) — 광원 한가운데에 선다. 테 굵기·번짐이 시간에 따라 줄고 번지므로
                    // 단계마다 다른 판을 미리 굽고 갈아 끼운다(정본 «퍼질수록 얇아지고 번진다 · 하드엣지 고정 굵기는 그래픽 스탬프다»).
                    float rw = Mathf.Min(PetSkillStyle.Rem((float)tb.FlashWRem), (float)tb.FlashWVwF * W);
                    RectTransform rr2 = UiKit.Box(tbHost, "sr-tierflash");
                    rr2.anchorMin = rr2.anchorMax = new Vector2(0.5f, 0.5f);
                    rr2.pivot = new Vector2(0.5f, 0.5f);
                    rr2.sizeDelta = new Vector2(rw, rw);
                    rr2.anchoredPosition = Vector2.zero;
                    rr2.position = wrap.TransformPoint(wrap.rect.center);   // 광원 = wrap 한가운데(재점화와 같은 계약)
                    Image ri2 = rr2.gameObject.AddComponent<Image>();
                    ri2.raycastTarget = false;
                    ri2.preserveAspect = false;
                    b.Steps = new Sprite[tb.FlashSteps];
                    b.Half = rw * 0.5f;
                    // ⚑ 19회차 판정에서 나온 자리 — 단계 판을 **여기서 다 굽지 않는다**. Open 한 프레임에 굽는 판이
                    //   스물이 넘으면 그 프레임이 통째로 길어져(§1 60fps) 뒤 겹의 짧은 구간(챕터 펄스 .54s)이
                    //   **프레임 사이로 빠진다** — 런 828 의 «첫 챕터가 안 달아올랐다» 가 그것이었다.
                    //   쓰는 순간 굽고 이름으로 캐시한다(같은 등급·단계는 한 번만 구워진다).
                    ri2.sprite = TierStep(tierBreaks.Count, 0, b);
                    Material rm2 = CraftFxPoly.Screen();
                    if (rm2 != null) ri2.material = rm2;
                    ri2.color = new Color(1f, 1f, 1f, 0f);
                    b.Ring = ri2;

                    // 심지(정본 `::after`) — 링 안쪽 여백만큼 들어가 앉고 부모 배율을 그대로 받는다.
                    RectTransform wk = UiKit.Box(rr2, "sr-tierwick");
                    float ins = (float)tb.WickInsetF;
                    UiKit.Anchor(wk, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, rw * (1f - ins * 2f), rw * (1f - ins * 2f));
                    Image wi = wk.gameObject.AddComponent<Image>();
                    wi.raycastTarget = false;
                    wi.preserveAspect = false;
                    wi.sprite = SummonFx.BakeTierWick("sr-tierwick-" + ColorUtility.ToHtmlStringRGB(b.Rc) + "-" + ColorUtility.ToHtmlStringRGB(b.Lite), b.Rc, b.Lite);
                    Material wm = CraftFxPoly.Screen();
                    if (wm != null) wi.material = wm;
                    wi.color = new Color(1f, 1f, 1f, 0f);
                    b.Wick = wi;

                    tierBreaks[i] = b;
                }
            }

            var sc = PetSkillHost.SfxSummonCharge;
            if (sc != null) sc(best);
            start = Time.unscaledTime;
            enterAt = start;   // T459 ⓧ — 정본 `.sr-wrap srshake` 는 열림에서 .21s 뒤 .25s 흔든다
            idx = 0;
            done = false;
            // T334 2회차 — 시각·판정을 Core 상태 기계에 넘긴다(정본 tickSummonResult 그대로).
            // 등급 순위는 정본 `RARITIES.indexOf` 자리 — 이 화면이 이미 쓰는 RarityIdx 다.
            List<int> rank = new List<int>(cells.Count);
            for (int i = 0; i < cells.Count; i++) rank.Add(RarityIdx(cells[i].Entry.Rarity));
            List<double> ds = new List<double>(delays.Count);
            for (int i = 0; i < delays.Count; i++) ds.Add(delays[i]);
            // ⚠ 주역 비트 길이는 **0 으로 둔다**(3회차 몫). 정본에 «350ms» 같은 수는 **없다** — 그 비트는 CSS 가
            //   쥐고 있고 후보가 셋이다: `srshakehit .44s`(5676 · 판 흔들기) · `srheropop .52s`(6727 · 주역 셀 팝) ·
            //   `srrecede .68s`(6720 · 나머지 셀 물러남). 어느 것이 «비트» 인지는 그 겹을 실제로 얹는 회차가
            //   화면을 보고 고른다 — 지금 하나를 골라 표에 박으면 근거 없는 수가 굳는다.
            seq = new SummonSeqRun(ds, rank, holdback, heroIdx, PetSkillStyle.L("sr_tail_ms"), 0.0);
        }

        Cell BuildCell(RectTransform grid, Entry e, int i, float x, float y, float cw, bool heroic, bool peer, bool dense, bool one)
        {
            int tier = RarityIdx(e.Rarity);
            Color rc = PetSkillStyle.Rarity(Defs, e.Rarity);
            float sub = UiCatalog.Instance.Kind(TextKind.Sub).size;
            float nameFs = UiCatalog.Instance.Kind(one ? TextKind.Button : TextKind.Sub).size;   // T391 5회차 — 위 `Open` 과 같은 셈(정본 7084 `.sr-grid.one .sr-name` 1.25rem)
            float nameH = dense ? 0f : TextClamp.BoxHeight(UiFont.Primary, nameFs, "sr_name", LineHeight.Table.Get("sr_name_lh"));   // T423 2회차 — 판 높이는 «클램프가 재는 높이» 다(표 `sr_name_h_em` × 코드에 박힌 0.62 를 걷었다)
            float subH = dense ? 0f : sub * 1.35f;
            float ch = cw + (dense ? 0f : PetSkillStyle.Px("sr_name_mt_rem") + nameH + PetSkillStyle.Px("sr_sub_mt_rem") + subH);
            RectTransform cell = UiKit.Box(grid, "sr-cell-" + i);
            UiKit.Place(cell, x, y, cw, ch);
            var cg = cell.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            var c = new Cell { Root = cell, Group = cg, Entry = e, Heroic = heroic, Home = cell.anchoredPosition };
            c.BaseScale = heroic ? 1f : PetSkillStyle.L("sr_sz_" + Mathf.Clamp(tier, 0, 5)) * (peer ? PetSkillStyle.L("sr_peer_sz") : 1f);
            c.Pop = PetSkillStyle.L("sr_pop_" + Mathf.Clamp(tier, 0, 5));
            // orbwrap
            RectTransform wrap = UiKit.Box(cell, "sr-orbwrap");
            UiKit.Place(wrap, 0f, 0f, cw, cw);
            wrap.pivot = new Vector2(0.5f, 0.5f);
            wrap.anchoredPosition = new Vector2(cw * 0.5f, -cw * 0.5f);
            c.OrbWrap = wrap;
            c.OrbHome = wrap.anchoredPosition;
            // T448 — 동급(peer) 착지 링(정본 6710~6716 `.sr-cell.peer.on::after`): 주역 충격파의 축소판이되 정지점·흰 알파·길이·배율 넷이 다르고,
            //   거는 때가 다르다 — 주역 링은 `.hero` 비트, 이 링은 **자기 착지**(.on). 셀 뒤(z −1) 폭 100% 정사각 · 가산(screen) 재질 · 알파 0 으로 두고 착지 시계가 돌린다.
            if (peer)
            {
                RectTransform pr = UiKit.Box(cell, "sr-peerring");
                pr.anchorMin = new Vector2(0.5f, 1f); pr.anchorMax = new Vector2(0.5f, 1f);
                pr.pivot = new Vector2(0.5f, 1f);
                pr.sizeDelta = new Vector2(cw, cw);
                pr.anchoredPosition = Vector2.zero;
                pr.SetAsFirstSibling();
                Image ring = pr.gameObject.AddComponent<Image>();
                ring.raycastTarget = false;
                ring.sprite = SummonFx.BakePeerRing("sr-peerring-" + ColorUtility.ToHtmlStringRGB(rc), rc);
                Material pm = CraftFxPoly.Screen();
                if (pm != null) ring.material = pm;
                ring.color = new Color(1f, 1f, 1f, 0f);
                c.PeerRing = ring;
            }
            // 광채(고등급) · 그림자 · 구체 · 하이라이트
            if (Hi(e.Rarity) || peer)
            {
                Image glow = PetSkillKit.Disc(wrap, "glow", rc);
                glow.color = new Color(rc.r, rc.g, rc.b, 0.35f + 0.1f * tier);
                float gs = cw * (1.25f + 0.1f * tier);
                UiKit.Anchor(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, gs, gs);
                if (Hi(e.Rarity)) c.Glow = glow;   // T459 ⓨ — 정본 6674 `.sr-cell.hi.on` 만 맥동한다(동급 조연 peer 는 아니다)
            }
            Image shadow = PetSkillKit.Disc(wrap, "shadow", PetSkillStyle.C("black"));
            shadow.color = new Color(0f, 0f, 0f, 0.5f);
            shadow.preserveAspect = false;
            UiKit.Anchor(shadow.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, -cw * 0.02f), cw * 0.74f, cw * 0.13f);
            // T342 7회차 ⓔ — 정본 6656~6665 `.sr-cell[data-tier="N"] .sr-orb { filter: saturate(·) brightness(·) }`(표 FilterUi `summon_orb_N`):
            //   구체 **본체**(림·면·하이라이트 = 정본 `.sr-orb` 의 배경 세 겹)의 색에 건다. 래퍼의 광채(`.sr-orbwrap` box-shadow)·그림자는 밖이다.
            //   종전 «tier ≤ 1 이면 검정 30% 섞기» 는 이 filter 의 근사였다 — 걷는다(정본 주석 6641~6655: 휘도 역전 억제 · tier 0 만 한 단 더).
            Image deep = PetSkillKit.Disc(wrap, "sr-orb-deep", OrbFilter(Color.Lerp(rc, PetSkillStyle.C("sr_orb_deep"), 0.62f), tier));
            UiKit.Fill(deep.rectTransform);
            Image orb = PetSkillKit.Disc(wrap, "sr-orb", OrbFilter(rc, tier));
            c.Orb = orb;   // T459 ⓩ
            orb.rectTransform.offsetMin = new Vector2(cw * 0.04f, cw * 0.09f);
            orb.rectTransform.offsetMax = new Vector2(-cw * 0.09f, -cw * 0.04f);
            Image hi = PetSkillKit.Disc(wrap, "sr-hilite", OrbFilter(PetSkillStyle.C("sr_hilite"), tier));
            UiKit.Anchor(hi.rectTransform, new Vector2(0.36f, 0.81f), new Vector2(0.5f, 0.5f), Vector2.zero, cw * 0.28f, cw * 0.2f);
            // 아이콘(슬롯의 그림)
            float isz = cw * (one ? 0.62f : 0.6f);
            if (!string.IsNullOrEmpty(e.FaceName))
            {
                RectTransform pf = PetSkillKit.PetFace(wrap, Defs, e.FaceName, isz, e.FaceKind);
                UiKit.Anchor(pf, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, isz, isz);
                pf.localScale = Vector3.one * OrbIconUi.BarrelScale;
                SrIcoShadow(pf.GetComponent<Image>());
            }
            else
            {
                Image ico = UiKit.Icon(wrap, "sr-ico", e.IconKey, e.IconTint);
                UiKit.Anchor(ico.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, isz, isz);
                // T385 2회차 ⑵ — 정본 6522 `.sr-ico { transform: scale(1.04) }`: «구면에 얹힌 것은 가운데가 미세하게 부푼다».
                ico.rectTransform.localScale = Vector3.one * OrbIconUi.BarrelScale;
                SrIcoShadow(ico);
            }
            OrbIconSpec(wrap, cw);
            if (!dense)
            {
                float bh = sub * 1.2f;
                if (e.Qty > 1)
                {
                    string q = PetSkillStyle.T("sr_qty", e.Qty);
                    float bw = PetSkillKit.TextWidth(TextKind.Sub, q) + PetSkillStyle.Rem(0.6f);
                    RectTransform qb = PetSkillKit.Framed(wrap, "sr-qty", PetSkillStyle.C("sr_badge_bg"), PetSkillStyle.Px("sr_qty_r_rem"), PetSkillStyle.L("line1_px"));
                    ((Image)qb.Find("line").GetComponent<Image>()).color = rc;
                    UiKit.Anchor(qb, new Vector2(0.95f, 0.06f), new Vector2(1f, 0f), Vector2.zero, bw, bh);
                    TextMeshProUGUI qt = PetSkillKit.Text(qb, "t", TextKind.Sub, q, PetSkillStyle.C("white"));
                    Shrink(qb, qt, PetSkillStyle.Rem(0.6f), bh);
                    UiKit.Fill(qt.rectTransform);
                    // T331 32회차 — 정본 7008 `.sr-qty { box-shadow: 0 .1rem .28rem rgba(0,0,0,.6) }`(겹이 하나다).
                    //   **줄인 뒤**에 건다 — `Shrink` 가 폭을 바꾸므로 구운 판도 그 폭이라야 한다.
                    UiShadow.Drop(qb, "srqty_drop", PetSkillStyle.Px("sr_qty_r_rem"), qb.sizeDelta.x, bh);
                }
                if (!string.IsNullOrEmpty(e.Extra))
                {
                    float bw = PetSkillKit.TextWidth(TextKind.Sub, e.Extra) + PetSkillStyle.Rem(0.6f);
                    RectTransform db = PetSkillKit.Framed(wrap, "sr-dup", PetSkillStyle.C("sr_badge_bg"), PetSkillStyle.Px("sr_qty_r_rem"), PetSkillStyle.L("line1_px"));
                    ((Image)db.Find("line").GetComponent<Image>()).color = PetSkillStyle.C("sr_hilite");
                    UiKit.Anchor(db, new Vector2(0.06f, 0.93f), new Vector2(0f, 1f), Vector2.zero, bw, bh);
                    TextMeshProUGUI dt = PetSkillKit.Text(db, "t", TextKind.Sub, e.Extra, PetSkillStyle.C("sr_dup_ink"));   // T396 17회차 — 정본 7009 `.sr-dup { color: #dfeaff }`(전엔 white)
                    WrapUi.Apply(dt, "sr_dup");   // T361 배선 — 이 파일이 내 lock 뒤라 3회차가 못 걸었다(결정 661)
                    Shrink(db, dt, PetSkillStyle.Rem(0.6f), bh);
                    UiKit.Fill(dt.rectTransform);
                    // T331 32회차 — 정본 7029 `.sr-dup` 는 수량 배지와 **값이 같지만 선택자가 달라** 키를 나눴다.
                    UiShadow.Drop(db, "srdup_drop", PetSkillStyle.Px("sr_qty_r_rem"), db.sizeDelta.x, bh);
                }
                if (e.IsNew)
                {
                    string nw = PetSkillStyle.T("sr_new");
                    float bw = PetSkillKit.TextWidth(TextKind.Sub, nw) + PetSkillStyle.Rem(0.52f);
                    RectTransform nb = PetSkillKit.Framed(wrap, "sr-new", PetSkillStyle.C("sr_new"), PetSkillStyle.Px("sr_new_r_rem"), PetSkillKit.Line2);
                    UiKit.Anchor(nb, new Vector2(0.93f, 0.92f), new Vector2(1f, 1f), Vector2.zero, bw, bh);
                    TextMeshProUGUI nt = PetSkillKit.Text(nb, "t", TextKind.Sub, nw, PetSkillStyle.C("white"));
                    LetterSpacing.Apply(nt, "sr_new_ls_em");   // T168 3회차 — 정본 6980 `.sr-new`
                    Shrink(nb, nt, PetSkillStyle.Rem(0.52f), bh);   // 자간까지 먹인 **뒤** 잰다
                    UiKit.Fill(nt.rectTransform);
                    // T331 32회차 — 정본 6985 `.sr-new { box-shadow: 0 .12rem .3rem rgba(0,0,0,.6) }`.
                    UiShadow.Drop(nb, "srnew_drop", PetSkillStyle.Px("sr_new_r_rem"), nb.sizeDelta.x, bh);
                }
                // 이름판 · 등급 칩
                float ny = cw + PetSkillStyle.Px("sr_name_mt_rem");
                float nw2 = cw * PetSkillStyle.L("sr_name_w_f");
                RectTransform nameBox = UiKit.Box(cell, "sr-name");
                UiKit.Place(nameBox, (cw - nw2) * 0.5f, ny, nw2, nameH);
                PetSkillKit.Fill(nameBox, "bg", PetSkillStyle.C("sr_name_bg"), PetSkillStyle.Px("sr_name_r_rem"));
                TextMeshProUGUI nt2 = PetSkillKit.Text(nameBox, "t", one ? TextKind.Button : TextKind.Sub, e.Name, PetSkillStyle.C("white"));   // T391 5회차 — 정본 7084 x1 소환만 1.25rem(45.5) → Button 44
                UiKit.TextShadow(nt2, one ? "sr_name_one" : "sr_name");   // T333 15회차 — 정본 7032 `.sr-name`(0 .1rem .3rem .95) · 7084 `.sr-grid.one .sr-name`(0 .12rem .4rem .95) — 같은 글에 x1 이면 뒤 규칙
                WrapUi.Apply(nt2, "sr_name");   // 정본 `.sr-name` 은 **접는다**(두 줄까지 · style.css 7032~7036)
                // T351 5회차 — 정본 7033 `line-height: 1.18`. T354 표(`LineHeightUi.json` `sr_name_lh`)가 이 수를 쥐고 있는데
                //   부르는 곳이 없어 클론은 글꼴 기본 줄높이(1.448em)로 그렸다 — 두 줄이 그만큼 더 벌어진다.
                LineHeight.Apply(nt2, "sr_name_lh");
                // T351 5회차 — 정본 7048 `.sr-name > span { -webkit-line-clamp: 2; overflow: hidden }` 의 마지막 자리.
                //   여태 TMP 기본(줄바꿈 + 넘침)이라 긴 이름이 이름판 **밖으로 흘러** 아래 등급 칩·옆 셀을 덮었다.
                //   `Ellipsis` 는 상자에 든 마지막 줄 끝에 …(U+2026)을 달고 나머지를 버린다 — 줄 수는 곧 상자 높이다.
                TextClamp.Apply(nt2, "sr_name");
                KeepAll.Apply(nt2, "sr_name");   // T466 — 정본 7040 `.sr-name { word-break: keep-all }`: 이름을 어절에서만 꺾는다(두 줄 클램프는 그대로)
                UiKit.Fill(nt2.rectTransform);
                float sy = ny + nameH + PetSkillStyle.Px("sr_sub_mt_rem");
                float rkW = PetSkillKit.TextWidth(TextKind.Sub, e.Sub) + PetSkillStyle.Px("sr_rk_pad_x_rem") * 2f;
                RectTransform rk = UiKit.Box(cell, "sr-sub");
                UiKit.Place(rk, (cw - rkW) * 0.5f, sy, rkW, subH);
                // T331 32회차 — 정본 7075 `.sr-sub .sr-rk` 의 **둘째 겹** `0 .08rem .22rem rgba(0,0,0,.5)`
                //   (첫 겹 `0 0 0 1px` 은 번짐만 있는 테두리라 T109 축이 본다).
                UiShadow.Drop(rk, "srrk_drop", PetSkillStyle.Px("sr_rk_r_rem"), rkW, subH);
                PetSkillKit.Fill(rk, "bg", rc, PetSkillStyle.Px("sr_rk_r_rem"));
                TextMeshProUGUI rt = PetSkillKit.Text(rk, "t", TextKind.Sub, e.Sub, ChipInk(rc));
                LetterSpacing.Apply(rt, "sr_sub_ls_em");   // T168 3회차 — 정본 7059 `.sr-sub`
                LineHeight.Apply(rt, "sr_sub_lh");   // T354 25회차 — 정본 7067 `.sr-sub { line-height: 1.25 }`(한 줄 배지 · 표를 읽는 자리)
                WrapUi.Apply(rt, "sr_sub");   // 정본 nowrap
                WrapUi.Apply(rt, "sr_sub_sr_rk");   // T361 9회차 — 정본 7072 `.sr-sub .sr-rk { nowrap }`: 클론엔 그 span 이 따로 없고 이 글 `t` 가 곧 그것이다(칩 상자 `sr-sub` 는 제 글자가 없다) — 두 규칙이 같은 글에 내린다
                UiKit.Fill(rt.rectTransform);
            }
            // ---- 비행 잔상(정본 `.sr-ghost` 6448~6474 · z 0) ----
            // 정본 주석: «팝이 아래에서 올라오는데 궤적이 없으면 «순간이동 후 튕김» 으로 보인다» ·
            //   «잔상은 «아래에서 솟은 자국» 이 아니라 **비행 경로에 끌리는 꼬리** 다 — 셀이 광원 쪽에서 날아오므로
            //    잔상은 그 뒤쪽(광원 쪽)에 남아 따라붙는다. 셀 안에 있어서 셀의 이동이 이미 곱해진 상태라,
            //    여기서는 «뒤처진 만큼» 만 더 민다» — 그래서 이 판은 셀 **안**(구체 래퍼)에 산다(스파크·재점화와 다른 자리다).
            // 상자는 `inset: 0`(래퍼와 같은 칸) · 구체 **뒤**라 맨 앞 형제로 넣는다.
            {
                RectTransform gh = UiKit.Box(wrap, "sr-ghost");
                UiKit.Fill(gh);
                gh.SetAsFirstSibling();
                Image gi = gh.gameObject.AddComponent<Image>();
                gi.raycastTarget = false;
                gi.preserveAspect = false;
                double gamt = SummonFxStyle.Hero.HiliteAmount(rc.r * 255.0, rc.g * 255.0, rc.b * 255.0, tier);
                Color glite = Shade(rc, (float)gamt);   // 정본 `--rc-lite`
                gi.sprite = SummonFx.BakeGhost(
                    "sr-ghost-" + ColorUtility.ToHtmlStringRGB(rc) + "-" + ColorUtility.ToHtmlStringRGB(glite), rc, glite);
                gi.color = new Color(1f, 1f, 1f, 0f);
                c.Ghost = gi;
            }
            // ---- 착지 스파크(정본 `.sr-spark` 6463~6484 · z 2) ----
            // 정본 주석: «링 하나로는 «내려앉았다» 만 말하고 «부딪혔다» 를 말하지 못한다 —
            //   box-shadow 8방향 복제를 transform: scale 로 바깥으로 날린다(오프셋도 함께 확대된다)».
            // 그래서 여기서도 복제 여덟 + 심 하나를 **한 장에** 굽고, 날리기는 판의 배율이 한다.
            // 가산(screen)이라야 구체 위에서 «튄 빛» 으로 읽힌다 — 알파로 덮으면 구체에 흰 점을 찍은 것이 된다.
            {
                SummonSparkSpec ssp = SummonFxStyle.Spark;
                float sz = PetSkillStyle.Rem((float)ssp.BoxRem);
                RectTransform spk = UiKit.Box(wrap, "sr-spark");
                spk.anchorMin = spk.anchorMax = new Vector2(0.5f, 0.5f);
                spk.pivot = new Vector2(0.5f, 0.5f);
                spk.sizeDelta = new Vector2(sz, sz);
                spk.anchoredPosition = Vector2.zero;
                Image si = spk.gameObject.AddComponent<Image>();
                si.raycastTarget = false;
                si.preserveAspect = false;
                si.sprite = SummonFx.BakeSpark("sr-spark-" + ColorUtility.ToHtmlStringRGB(rc), rc);
                Material sm = CraftFxPoly.Screen();
                if (sm != null) si.material = sm;
                si.color = new Color(1f, 1f, 1f, 0f);
                spk.localScale = Vector3.one * (float)(ssp.Scale.Sample(0, "base", null) + ssp.Glow(tier) * ssp.Scale.Sample(0, "glow", null));
                c.Spark = si;
            }
            cell.localScale = Vector3.one * 0.35f;
            return c;
        }

        /// <summary>
        /// 배지 알약을 **제 글자에 맞춰 줄인다**(정본은 `position: absolute` + `padding` 이라 폭이 잉크에 딱 맞는 shrink-to-fit 이다).
        ///
        /// ⚑ 13회차 판정(런 754 `screen_t179-summon` 8배 확대)에서 나온 자리다: «NEW» 가 «NE / W» 로 **접혀 알약 밖으로 넘쳤다**.
        /// 까닭은 알약 폭을 <see cref="PetSkillKit.TextWidth"/>(ASCII 를 한 자 .58em 로 어림하는 자)로 잡는데
        /// 대문자 N·E·W 의 실제 폭이 그보다 훨씬 넓고(자간 .04em 도 안 센다), T361 3회차가 줄바꿈 기본값을
        /// 정본대로 «접는다» 로 뒤집으면서 그 어림이 **넘침에서 접힘으로** 바뀐 것이다(결정 661 이 예고한 자리).
        /// 어림을 고치는 대신(그 자는 이 파일 밖이고 다른 자리까지 한꺼번에 움직인다) **정본의 shrink-to-fit 을 그대로 옮긴다** —
        /// 글자를 세운 뒤(자간까지 먹인 뒤) 제 선호 폭을 재어 알약을 그 폭으로 다시 앉힌다. 상자가 잉크보다 넓으니 접힐 일이 없다.
        /// </summary>
        static void Shrink(RectTransform pill, TextMeshProUGUI t, float padX, float h)
        {
            Vector2 pv = t.GetPreferredValues();
            float w = pv.x + padX;
            if (w <= pill.sizeDelta.x) return;   // 어림이 이미 넉넉하면 그대로 둔다(자리가 안 움직인다)
            pill.sizeDelta = new Vector2(w, h);
        }

        /// <summary>원작 chipFill — 등급색 필 위에 검정/흰색 중 대비 큰 쪽.</summary>
        static Color ChipInk(Color bg)
        {
            float lum = 0.2126f * bg.r + 0.7152f * bg.g + 0.0722f * bg.b;
            return lum > 0.5f ? PetSkillStyle.C("ink") : PetSkillStyle.C("white");
        }

        void BuildFoot(float footH, float fw)
        {
            float sub = UiCatalog.Instance.Kind(TextKind.Sub).size;
            float gap = PetSkillStyle.Px("sr_foot_gap_rem");
            float okW = PetSkillStyle.Px("sr_ok_w_rem"), okH = PetSkillStyle.Px("sr_ok_h_rem");
            // 집계 칩(원작 summonSummary · done 에서만)
            chips = UiKit.Box(foot, "sr-sum").gameObject;
            var cnt = new Dictionary<string, int>();
            foreach (Entry e in rollList) { int v; cnt.TryGetValue(e.Rarity, out v); cnt[e.Rarity] = v + 1; }
            float chipH = sub * 1.5f;
            var list = new List<KeyValuePair<string, int>>();
            foreach (string r in Defs.Rarities) if (cnt.ContainsKey(r)) list.Add(new KeyValuePair<string, int>(r, cnt[r]));
            float x = 0f, total = 0f;
            var widths = new List<float>();
            foreach (var kv in list)
            {
                float w = PetSkillKit.TextWidth(TextKind.Sub, PetSkillStyle.T("sr_chip", Defs.RarityKr.Get(kv.Key, kv.Key), kv.Value)) + PetSkillStyle.Px("sr_chip_pad_x_rem") * 2f;
                widths.Add(w);
                total += w + PetSkillStyle.Px("sr_chip_gap_rem");
            }
            x = (fw - total) * 0.5f;
            RectTransform chipRow = (RectTransform)chips.transform;
            UiKit.Place(chipRow, 0f, footH - okH - gap - chipH - gap, fw, chipH);
            for (int i = 0; i < list.Count; i++)
            {
                Color rc = PetSkillStyle.Rarity(Defs, list[i].Key);
                RectTransform chip = UiKit.Box(chipRow, "sr-chip-" + list[i].Key);
                UiKit.Place(chip, x, 0f, widths[i], chipH);
                // T331 32회차 — 정본 7128 `.sr-chip` 의 **둘째 겹** `0 .1rem .26rem rgba(0,0,0,.55)`(첫 겹은 1px 테두리).
                //   등급 4·5 의 `[data-tier]`(7131·7132)는 같은 두 겹에 **발광만** 더한 것이라 그늘은 이 키 하나다.
                UiShadow.Drop(chip, "srchip_drop", PetSkillStyle.Px("sr_chip_r_rem"), widths[i], chipH);
                PetSkillKit.Fill(chip, "bg", rc, PetSkillStyle.Px("sr_chip_r_rem"));
                TextMeshProUGUI t = PetSkillKit.Text(chip, "t", TextKind.Sub, PetSkillStyle.T("sr_chip", Defs.RarityKr.Get(list[i].Key, list[i].Key), list[i].Value), ChipInk(rc));
                LineHeight.Apply(t, "sr_chip_lh");   // T354 25회차 — 정본 7127 `.sr-chip { line-height: 1.3 }`(한 줄 칩)
                UiKit.Fill(t.rectTransform);
                x += widths[i] + PetSkillStyle.Px("sr_chip_gap_rem");
            }
            chips.SetActive(false);
            // 힌트
            string ht = PetSkillStyle.T("sr_hint");
            float hw = PetSkillKit.TextWidth(TextKind.Sub, ht) + PetSkillStyle.Px("sr_hint_pad_x_rem") * 2f;
            RectTransform hintBox = PetSkillKit.Framed(foot, "sr-hint", new Color(0f, 0f, 0f, 0f), PetSkillStyle.Px("sr_hint_r_rem"), PetSkillStyle.L("line1_px"));
            ((Image)hintBox.Find("line").GetComponent<Image>()).color = new Color(1f, 1f, 1f, 0.22f);
            ((Image)hintBox.Find("face").GetComponent<Image>()).color = PetSkillStyle.C("sr_bg_d");
            UiKit.Anchor(hintBox, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, (okH - sub * 1.6f) * 0.5f), hw, sub * 1.6f);
            TextMeshProUGUI hint2 = PetSkillKit.Text(hintBox, "t", TextKind.Sub, ht, PetSkillStyle.C("sr_hint"), TextAlignmentOptions.Center, false);
            UiKit.Fill(hint2.rectTransform);
            hint = hintBox.gameObject;
            // 확인(금색 · done 에서만)
            OkButton = UiKit.Button(foot, "sr-ok", () => Close());
            RectTransform okr = OkButton.GetComponent<RectTransform>();
            UiKit.Anchor(okr, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, okW, okH);
            RectTransform okSkin = PetSkillKit.Framed(okr, "skin", PetSkillStyle.C("sr_ok_ink"), PetSkillStyle.Px("sr_ok_r_rem"), PetSkillKit.Line2);
            UiKit.Fill(okSkin);
            Image okFace = PetSkillKit.Fill(okSkin, "top", PetSkillStyle.C("sr_ok"), PetSkillStyle.Px("sr_ok_r_rem") - PetSkillKit.Line2);
            okFace.rectTransform.offsetMin = new Vector2(PetSkillKit.Line2, PetSkillKit.Line2 + PetSkillStyle.Px("sr_ok_inset_rem"));
            okFace.rectTransform.offsetMax = new Vector2(-PetSkillKit.Line2, -PetSkillKit.Line2);
            TextMeshProUGUI okt = PetSkillKit.Text(okr, "t", TextKind.Button, PetSkillStyle.T("sr_ok"), PetSkillStyle.C("sr_ok_ink"));
            LetterSpacing.Apply(okt, "sr_ok_ls_em");   // T168 3회차 — 정본 7139 `.sr-ok`
            UiKit.Fill(okt.rectTransform);
            ok = okr.gameObject;
            okRect = okr; okHome = okr.anchoredPosition;
            // ⚠ `??` 금지 — 에디터의 GetComponent 는 빠진 컴포넌트에 «가짜 null»(네이티브 0 의 관리 객체)을 돌려줘 `??` 가 그것을
            //   산 것으로 보고 AddComponent 를 건너뛴다(런 1143 실측 — 유니티가 겹쳐 쓴 `==` 로만 가른다).
            okGroup = okr.GetComponent<CanvasGroup>();
            if (okGroup == null) okGroup = okr.gameObject.AddComponent<CanvasGroup>();
            ok.SetActive(false);
            // x1 요약(원작 summonSoloInfo · done 에서만)
            if (entries.Count == 1)
            {
                Entry e = entries[0];
                string line, own;
                if (kind == "skill")
                {
                    string id = e.Key.Substring(3);
                    SkillEntry st = PetSkillHost.Instance.Skills.State.Get(id);
                    line = PetSkillStyle.T(e.IsNew ? "sr_solo_skill_new" : "sr_solo_skill_dup");
                    own = PetSkillStyle.T("sr_solo_skill_own", st != null ? st.Level : 1, st != null ? st.Dupes : 0);
                }
                else
                {
                    int eggs = 0;
                    foreach (Egg g in PetSkillHost.Instance.Pets.State.Eggs) if (g.Rarity == e.Rarity) eggs++;
                    line = PetSkillStyle.T("sr_solo_egg");
                    own = PetSkillStyle.T("sr_solo_egg_own", Defs.RarityKr.Get(e.Rarity, e.Rarity), eggs);
                }
                float sg = PetSkillStyle.Px("sr_solo_gap_rem");
                float aw = PetSkillStyle.Px("sr_again_w_rem"), ah = PetSkillStyle.Px("sr_again_h_rem");
                float soloH = sub * 1.3f + sg + sub * 1.5f + sg + ah;
                RectTransform sb = UiKit.Box(foot, "sr-solo");
                UiKit.Place(sb, 0f, footH - okH - gap - soloH, fw, soloH);
                // ⚑ 13회차 · §1 «실제 화면을 본다» — 이 줄은 **아이콘 길을 거쳐야 한다.**
                //   런 743 `screen_t179-summon` 을 열어 보니 «✨ 신규 스킬 획득!» 의 머리가 **두부(□)** 였다.
                //   문구는 표(`sr_solo_skill_new`)에서 오고 정본 `TOAST_ICON` 이 «✨ → sparkle» 로 쥐고 있는데
                //   여기서 글자 그대로 세우고 있었다 — 주인 글꼴에도 이모지 폴백에도 U+2728 이 없으니 □ 다.
                //   `check_text_glyphs` 가 rc 0 인 까닭은 그 자가 «TOAST_ICON 에 있으면 아이콘으로 치환된다» 로 빼기 때문이고,
                //   자기 머리 주석이 바로 그 함정을 경고한다(«표에 있다» 가 «그 자리가 아이콘을 거친다» 는 뜻이 아니다).
                RectTransform lt = UiKit.IconTextRow(sb, "line", TextKind.Sub, line, "white");
                foreach (TextMeshProUGUI lt_t in lt.GetComponentsInChildren<TextMeshProUGUI>(true)) { lt_t.color = PetSkillStyle.C("sr_solo_line_ink"); UiKit.TextShadow(lt_t, "sr_solo_line"); }   // T396 17회차 — 정본 5783 `.sr-solo-line { color: #e8eeff }`(전엔 white · IconTextRow 는 카탈로그 키만 받아 뒤에서 칠한다)   // T333 15회차 — 정본 5784 `.sr-solo-line { text-shadow: 0 1px 3px rgba(0,0,0,.7) }` · 아이콘 조각 사이 글 조각마다
                UiKit.Place(lt, 0f, 0f, fw, sub * 1.3f);
                float ow = PetSkillKit.TextWidth(TextKind.Sub, own) + PetSkillStyle.Rem(1.4f);
                RectTransform ob = PetSkillKit.Framed(sb, "own", PetSkillStyle.C("sr_solo_own"), PetSkillStyle.Px("sr_solo_own_r_rem"), PetSkillStyle.L("line1_px"));
                ((Image)ob.Find("line").GetComponent<Image>()).color = new Color(0.47f, 0.55f, 0.78f, 0.35f);
                UiKit.Place(ob, (fw - ow) * 0.5f, sub * 1.3f + sg, ow, sub * 1.5f);
                TextMeshProUGUI ot = PetSkillKit.Text(ob, "t", TextKind.Sub, own, new Color(0.84f, 0.88f, 0.97f, 0.85f), TextAlignmentOptions.Center, false);
                UiKit.Fill(ot.rectTransform);
                AgainButton = UiKit.Button(sb, "sr-again", () => { Action a = repeat; Close(); if (a != null) a(); });
                RectTransform ar = AgainButton.GetComponent<RectTransform>();
                UiKit.Place(ar, (fw - aw) * 0.5f, sub * 1.3f + sg + sub * 1.5f + sg, aw, ah);
                // T331 32회차 — 정본 5796 `.sr-again` 의 **둘째 겹** `0 .25rem .6rem rgba(0,0,0,.5)`
                //   (첫 겹 `inset 0 .1rem 0 rgba(255,255,255,.14)` 은 안쪽 림라이트다).
                UiShadow.Drop(ar, "sragain_drop", PetSkillStyle.Px("sr_again_r_rem"), aw, ah);
                RectTransform askin = PetSkillKit.Framed(ar, "skin", PetSkillStyle.C("sr_again"), PetSkillStyle.Px("sr_again_r_rem"), PetSkillStyle.L("line1_px"));
                UiKit.Fill(askin);
                ((Image)askin.Find("line").GetComponent<Image>()).color = new Color(0.59f, 0.67f, 0.92f, 0.55f);
                TextMeshProUGUI at = PetSkillKit.Text(ar, "t", TextKind.Sub, PetSkillStyle.T("sr_again"), PetSkillStyle.C("sr_again_ink"));
                LetterSpacing.Apply(at, "sr_again_ls_em");   // T168 3회차 — 정본 5791 `.sr-again`
                UiKit.Fill(at.rectTransform);
                solo = sb.gameObject;
                solo.SetActive(false);
            }
        }

        // ===== 시계(원작 tickSummonResult · rAF) =====
        void Update()
        {
            if (bgPromoteMs >= 0)
            {
                bgPromoteMs += Time.unscaledDeltaTime * 1000.0;
                ApplyBgPromote();
                if (TransitionRules.Done(TransitionUi.Table.Get("sr_bg_done"), bgPromoteMs)) SettleBg();
            }
            if (!done && seq != null)
            {
                float elapsed = (Time.unscaledTime - start) * 1000f;
                int loudRank = seq.Tick(elapsed);          // 밀린 셀을 다 띄우고 상태를 옮긴다(정본 tickSummonResult)
                while (idx < seq.Revealed && idx < cells.Count) { TurnOn(cells[idx]); idx++; }
                if (loudRank >= 0)
                {
                    // 효과음은 이번 프레임 **최고 등급 하나**(정본 주석 — 한 프레임에 여럿이 몰려도 한 번).
                    string loud = RarityOf(loudRank);
                    var sr = PetSkillHost.SfxSummonReveal;
                    if (sr != null && loud != null) sr(loud);
                }
                if (seq.Hero && !heroFired) FireHero();
                if (seq.Done) Finish();
            }
            // §0-6 보탬(런 1173 · 결정 792) — «슬롯 → 광원» 벡터는 **셀을 움직이기 전에** 잰다. T455 가 «창이 아니라 첫 틱에» 로 옮겼지만 그 자리가
            //   `AnimateCharge`(AnimateCells **뒤**)라, 느린 러너에서 첫 틱과 첫 공개가 한 틱에 몰리면 켜진 셀의 0% 자리가 벡터 0 의 대체값
            //   (`--dy` .5rem)으로 한 프레임 찍히고 같은 틱 끝에야 벡터가 선다(자 `셀은_광원_자리에서…` 런 1173: x 134 ↔ 361). 화면으론 한 프레임의
            //   «아래서» ↔ «광원에서» 차이다. 정본은 `setSummonEjectPaths` 가 **셀 등장 전**에 심는다 — 그 차례를 여기서 지킨다.
            if (!ejectSet && haloImg != null && cells.Count > 0) { ejectSet = true; MeasureEject(); }
            AnimateCells();
            AnimateTicks();
            AnimateCharge();
            AnimateFlash();
            AnimateWipe();
            AnimateEnterShake();
            AnimateKick();
            AnimateHiPulses();
            AnimateOrbSweeps();
            AnimateOk();
            AnimateHeroRing();
            AnimatePeerRings();
            AnimateBeam();
            AnimateRelights();
            AnimateSparks();
            AnimateGhosts();
            AnimateTierBreaks();
            AnimateIdleRings();
            AnimateShock();
            AnimateChargeBurst();
            AnimateStreaks();
            AnimateMotes();
        }

        void TurnOn(Cell c)
        {
            if (c.On) return;
            c.On = true;
            c.OnAt = Time.unscaledTime;
        }

        /// <summary>
        /// T458 2회차 — [확인] 버튼은 done 에 셀과 **같은 `srpop`** 을 탄다(정본 7188 · .32s `both`). 셀과 달리 `--dx/--dy`·`--over` 가 없어
        /// 정본 기본값 그대로다 — 아래 `.5rem` 에서 올라오고 넘침은 `× 1`. 끝나면 마지막 키에 그대로 선다(`both`).
        /// </summary>
        void AnimateOk()
        {
            if (okAt < 0f || okRect == null) return;
            SummonPopSpec pp = SummonFxStyle.Pop;
            float ms = (Time.unscaledTime - okAt) * 1000f;
            double flyF, sc, a;
            pp.At(ms, pp.OkMs, 1, out flyF, out sc, out a);
            okRect.localScale = Vector3.one * (float)sc;
            okRect.anchoredPosition = okHome + new Vector2(0f, -(float)(pp.Dy0Rem * flyF) * PetSkillStyle.RemPx);
            if (okGroup != null) okGroup.alpha = (float)a;
            if (ms >= pp.OkMs) okAt = -1f;   // 정착 — 다음 프레임부터 손대지 않는다
        }

        /// <summary>
        /// 셀 팝(정본 `srpop` · style.css 6286 · 6296~6321) · 끝난 뒤에는 숨쉬기(srbreath).
        /// T458 2회차 — 종전엔 «.35 → 1 EaseOutBack 한 곡선» 이었다. 정본은 **광원에서 날아와**(0% 는 `--dx/--dy` 자리 · 클론은
        ///   T455 의 `ToLight`) 30% 까지 가속 · 56% 까지 등속 비행 · 76% 까지 감속 · 76% 에서 `1 + .1 × --over`(등급 계단) 만큼
        ///   넘쳤다가 100% 에 1 로 선다 — 구간마다 이징이 다르고 첫 프레임 알파가 .34 다. 수치·곡선은 전부 표(`pop` 절 + `tier.over`)가 쥔다.
        /// </summary>
        void AnimateCells()
        {
            float tt = Time.unscaledTime;
            SummonPopSpec pp = cells.Count > 0 ? SummonFxStyle.Pop : null;
            for (int i = 0; i < cells.Count; i++)
            {
                Cell c = cells[i];
                if (!c.On) continue;
                float popMs = c.Pop * 1000f;
                float ms = (tt - c.OnAt) * 1000f;
                float t = popMs > 0f ? ms / popMs : 1f;
                double flyF, popScale, popAlpha;
                pp.At(ms, popMs, pp.Over(RarityIdx(c.Entry.Rarity)), out flyF, out popScale, out popAlpha);
                float s = (float)popScale;
                c.Group.alpha = (float)popAlpha;
                // 0% 는 광원 자리 — 벡터가 아직 없으면(정본 `var(--dy, .5rem)`) 아래서 .5rem 올라온다.
                Vector2 fly = c.ToLight * (float)flyF;
                if (fly == Vector2.zero && flyF > 0) fly = new Vector2(0f, -(float)pp.Dy0Rem * PetSkillStyle.RemPx);
                // T334 5회차 — 아이들 호흡은 **등급에 가중**된다(정본 `--idle` 계단 · style.css 6327~6332).
                //   정본 주석: «예전엔 전 등급이 똑같이 −.16rem / ×1.055 였고 실측상 등급 간 차이는 광채에서만 나왔다 —
                //   즉 위계가 구조가 아니라 부산물이었다». 수치는 표(`SummonFxUi.json` 의 `idle` 절)가 쥔다.
                float b = 1f, ty = 0f;
                if (done && t >= 1f && doneAt >= 0f)
                {
                    SummonIdleSpec sp = SummonFxStyle.Idle;
                    double tyRem, add;
                    sp.At((tt - doneAt) * 1000f, i, RarityIdx(c.Entry.Rarity), out tyRem, out add);
                    b = 1f + (float)add;
                    ty = (float)tyRem * PetSkillStyle.RemPx;
                }
                // T334 7회차 — 주역이 착지하면 **조연은 물러난다**(정본 `srrecede` · 절 머리 주석: «나머지를 물리고(후퇴)
                //   광창 → 충격파 → 화면 킥을 몰아 «다른 사건» 으로 만든다»). 배율은 셀에, 채도·밝기는 그 그림들에 건다.
                float rs = 1f;
                if (heroFired && heroAtWall >= 0f && !c.Heroic) rs = Recede(c, (tt - heroAtWall) * 1000f);
                if (c.Heroic && heroFired && heroAtWall >= 0f)
                {
                    // T334 8회차 — 주역은 착지에서 제 등장(정본 `srheropop`)을 새로 탄다: 정본이 `animation` 단축 속성을
                    //   통째로 갈아 끼우므로 앞의 팝은 그 순간 사라진다. «더 길고 더 크게 넘치고, 끝에서 원래 크기로
                    //   안 돌아온다(무대에 남는다)».
                    SummonHeroSpec hp = SummonFxStyle.Hero;
                    double backF, tyRem, sc, op;
                    hp.HeroPopAt((tt - heroAtWall) * 1000f, out backF, out tyRem, out sc, out op);
                    c.Group.alpha = (float)op;
                    c.Root.localScale = Vector3.one * (float)sc;
                    Vector2 back = c.ToLight * (float)backF;
                    if (back == Vector2.zero && backF > 0f) back = new Vector2(0f, -(float)hp.HeroPopDy0Rem * PetSkillStyle.RemPx);
                    c.Root.anchoredPosition = c.Home + back + new Vector2(0f, -(float)tyRem * PetSkillStyle.RemPx);
                }
                else
                {
                    c.Root.localScale = Vector3.one * s * rs;
                    c.Root.anchoredPosition = c.Home + fly;   // 흡기(AnimateCharge · 뒤에 돈다)는 이 위에 제 자리를 덮어쓴다
                }
                // ⚠ 정본 6729 주석이 실측으로 못 박았다: «정착 스케일을 1보다 크게 두면 셀 폭을 넘는 이름판이 옆 셀
                //   이름과 겹친다 … 주역의 «큰 몸집» 은 등급 계단이 이미 맡는다». 그래서 여기 배수(옛 1.18)를 뺐다.
                c.OrbWrap.localScale = Vector3.one * c.BaseScale * b;
                c.OrbWrap.anchoredPosition = c.OrbHome + new Vector2(0f, -ty);   // 표의 ty_rem 은 CSS 부호(음수 = 위로)
            }
        }


        /// <summary>
        /// T334 3회차 ⓑ — 홀드백 대기 구간(정본 `#summon-result-modal.charging` · style.css 6815~6879).
        /// 정본 주석이 못 박은 것: 이 구간은 **정지가 아니라 축적**이고, 예전엔 480ms 가 통째로 정지 프레임이라
        /// «긴장» 이 아니라 «렌더 멈춤» 으로 읽혔다. 그래서 다섯이 한꺼번에 움직인다 —
        /// 소환진이 부풀며 밝아지다 **마지막 12%에 수축**(그 반동으로 주역이 터진다) · 눈금이 `steps(9)` 로 순차 점등 ·
        /// 중앙 광원이 점점 좁은 간격으로 뛰고 · 비네트가 조여들고 · 정착한 조연 셀이 광원 쪽으로 빨려든다(사출의 역재생).
        ///
        /// 값·곡선은 전부 <see cref="SummonChargeSpec"/>(Core · 표 `charge` 절)이 쥔다 — 여기는 그것을 화면에 거는 손일 뿐이다.
        /// 구간이 끝나면(주역 착지) 정본이 클래스를 떼는 것과 같게 **제자리로 되돌린다** — 그 순간은 섬광/와이프가 덮는다.
        /// </summary>
        void AnimateCharge()
        {
            bool on = seq != null && seq.Charging;
            // T455 — **«슬롯 → 광원» 벡터는 충전 창 안에서만 재면 안 된다.** 종전엔 아래 `if (chargeAt < 0f)` 안에서 한 번 쟀는데,
            //   그 블록은 `Charging` 이 참인 **프레임이 실제로 들어왔을 때만** 돈다. 러너·저사양 기기처럼 프레임이 길어 그 창(수백 ms)에
            //   한 프레임도 안 들어오면 `ToLight` 가 **영영 0 으로 남고**, 그 벡터를 쓰는 세 자리(주역 등장 `srheropop` ·
            //   조연 흡기 `srinhale` · 잔상 `srghost`)가 전부 «광원 쪽에서 밀려 나온» 방향을 잃는다 — 자가 아니라 **화면이 조용히 틀어진다**.
            //   (실측: 런 1102 `SummonChargeTests.사출_경로는…` 이 «사출 벡터가 안 재졌다(경과 6.03초)» 로 빨갰다. 같은 파일에
            //    같은 사유의 «환경» 접음이 이미 둘 있다 — 그 둘은 «못 쟀다» 지만 이것은 «안 세워졌다» 라 갈래가 다르다.)
            //   ⇒ 창과 무관하게 **첫 틱에 한 번** 잰다. 정상 흐름에서는 그 첫 틱이 곧 첫 충전 틱이라 값도 시점도 그대로다.
            if (!ejectSet && haloImg != null && cells.Count > 0)
            {
                ejectSet = true;
                MeasureEject();
            }
            if (!on)
            {
                if (chargeAt < 0f) return;
                chargeAt = -1f;
                if (floorImg != null) { floorImg.color = floorBase; floorImg.rectTransform.localScale = floorHome; }
                if (haloImg != null) haloImg.color = haloBase;
                if (vigImg != null) { vigImg.color = new Color(1f, 1f, 1f, 0f); vigImg.rectTransform.localScale = Vector3.one; }
                for (int i = 0; i < cells.Count; i++) cells[i].Root.anchoredPosition = cells[i].Home;
                return;
            }
            if (chargeAt < 0f)
            {
                chargeAt = Time.unscaledTime;
                if (!ejectSet) { ejectSet = true; MeasureEject(); }   // T455 — 위에서 못 쟀으면(후광이 아직 없던 첫 틱) 여기서
            }
            SummonChargeSpec sp = SummonFxStyle.Charge;
            float ms = (Time.unscaledTime - chargeAt) * 1000f;
            if (floorImg != null)
            {
                float sc = (float)sp.FloorAt(ms, "scale"), br = (float)sp.FloorAt(ms, "bright"), sa = (float)sp.FloorAt(ms, "sat");
                floorImg.rectTransform.localScale = floorHome * sc;
                // ⚠ 눈금(`srtickup`)은 **아직 안 건다**: 정본에서 그것은 `.sr-floor::after` 의 룬 띠인데 클론의 소환진은
                //    민 원판 한 장이고 룬 눈금은 천개 아치에 구워져 있다(`BakeArch`). 없는 겹에 얹으면 «소환진이 통째로
                //    짙어진다» 가 되어 정본과 다른 그림이 된다 — 수치(`TickAlpha`)는 표·Core 에 세워 두고 겹은 다음 회차에 낸다.
                floorImg.color = Brighten(floorBase, br, sa, floorBase.a);
            }
            if (haloImg != null)
            {
                float a = (float)sp.HaloAt(ms, "alpha"), sc = (float)sp.HaloAt(ms, "scale"), br = (float)sp.HaloAt(ms, "bright");
                haloImg.rectTransform.localScale = Vector3.one * sc;
                haloImg.color = Brighten(haloBase, br, 1f, a);
            }
            if (vigImg != null)
            {
                vigImg.color = new Color(1f, 1f, 1f, (float)sp.VigAt(ms, "alpha"));
                vigImg.rectTransform.localScale = Vector3.one * (float)sp.VigAt(ms, "scale");
            }
            float back = (float)sp.InhaleAt(ms, "back_f"), isc = (float)sp.InhaleAt(ms, "scale");
            for (int i = 0; i < cells.Count; i++)
            {
                Cell c = cells[i];
                if (!c.On || c.Heroic) continue;          // 정본 `.sr-cell.on:not(.heroic)`
                c.Root.anchoredPosition = c.Home + c.ToLight * back;
                c.Root.localScale *= isc;                 // AnimateCells 가 이미 판 팝을 얹었다 — 그 위에 흡기를 곱한다
            }
        }

        /// <summary>CSS `filter: brightness(b) saturate(s)` 를 틴트 한 색으로 — 알파는 따로 준다(합성 단계라 곱하기가 맞다).</summary>
        static Color Brighten(Color c, float bright, float sat, float alpha)
        {
            float g = c.r * 0.2126f + c.g * 0.7152f + c.b * 0.0722f;
            return new Color(
                Mathf.Clamp01((g + (c.r - g) * sat) * bright),
                Mathf.Clamp01((g + (c.g - g) * sat) * bright),
                Mathf.Clamp01((g + (c.b - g) * sat) * bright),
                Mathf.Clamp01(alpha));
        }


        /// <summary>
        /// 룬 눈금 띠 — 충전 구간에서는 `srtickup`(steps(9) · .45 → 1), 그 밖에는 `srfloorbreath`(4.2s · .5 ↔ .9).
        /// 정본이 «움직임은 호흡뿐» 이라 못 박은 자리라 돌리지도 넓히지도 않는다.
        /// </summary>
        void AnimateTicks()
        {
            if (tickImg == null) return;
            float a;
            if (seq != null && seq.Charging && chargeAt >= 0f)
            {
                a = (float)SummonFxStyle.Charge.TickAlpha((Time.unscaledTime - chargeAt) * 1000f);
            }
            else
            {
                float per = SummonFxStyle.L("floor_breath_s"), lo = SummonFxStyle.L("floor_breath_a_lo"), hi = SummonFxStyle.L("floor_breath_a_hi");
                float t = per <= 0f ? 0f : Mathf.Repeat(Time.unscaledTime - start, per) / per;   // start 는 이미 초다(ms 로 나누지 않는다)
                float k = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 2f);      // ease-in-out 왕복(0 → 1 → 0)
                a = Mathf.Lerp(lo, hi, k);
            }
            tickImg.color = new Color(tickBase.r, tickBase.g, tickBase.b, tickBase.a * a);
        }

        /// <summary>정본 `srShade(hex, amt)` — 양수면 흰 쪽으로 amt 만큼 띄운다(`c + (255-c)*amt`).</summary>
        static Color Shade(Color c, float amt)
        {
            return new Color(c.r + (1f - c.r) * amt, c.g + (1f - c.g) * amt, c.b + (1f - c.b) * amt, c.a);
        }


        /// <summary>
        /// 주역 착지의 화면 킥(정본 `srshakehit` · 5675~5684) — 판을 제 크기의 비율만큼 흔든다.
        /// 치우침이 퍼센트라 화면 크기가 달라져도 같은 세기로 읽힌다. 끝나면 제자리(정본 `both` 필의 마지막 키가 «없음» 이다).
        /// </summary>
        void AnimateKick()
        {
            if (wrap == null || kickAt < 0f) return;
            SummonHeroSpec sp = SummonFxStyle.Hero;
            double ms = (Time.unscaledTime - kickAt) * 1000f;
            double tx, ty, sc;
            sp.At(ms, out tx, out ty, out sc);
            Rect r = wrap.rect;
            wrap.anchoredPosition = wrapHome + new Vector2((float)(tx * r.width), -(float)(ty * r.height));   // CSS 의 +y 는 아래
            wrap.localScale = Vector3.one * (float)sc;
            if (!sp.Kicking(ms)) kickAt = -1f;      // 다 흔들었으면 손을 뗀다(아이들이 이 판을 다시 안 잡는다)
        }

        /// <summary>화면 킥이 도는 중인가 — 자가 본다.</summary>
        public bool Kicking { get { return kickAt >= 0f; } }

        /// <summary>
        /// T459 ⓧ — 열릴 때의 판 셰이크(정본 `.sr-wrap { animation: srshake .25s … both }` 5657 + `animation-delay: .21s` 6148).
        /// 킥(`srshakehit`)이 판을 잡고 있으면 물러난다 — 정본에서도 `.hero .sr-wrap` 의 단축이 이 애니메이션을 갈아 끼운다.
        /// </summary>
        void AnimateEnterShake()
        {
            if (wrap == null || enterAt < 0f) return;
            SummonEnterSpec sp = SummonFxStyle.Enter;
            double ms = (Time.unscaledTime - enterAt) * 1000f;
            if (kickAt >= 0f) { enterAt = -1f; return; }
            if (sp.Done(ms))
            {
                wrap.anchoredPosition = wrapHome;
                wrap.localScale = Vector3.one;
                enterAt = -1f;
                return;
            }
            if (!sp.Shaking(ms)) return;
            double tx, ty, sc;
            sp.At(ms, out tx, out ty, out sc);
            Rect r = wrap.rect;
            wrap.anchoredPosition = wrapHome + new Vector2((float)(tx * r.width), -(float)(ty * r.height));   // CSS 의 +y 는 아래
            wrap.localScale = Vector3.one * (float)sc;
        }

        /// <summary>
        /// T459 ⓩ — done 뒤 구슬 표면 스페큘러 스윕(정본 6912~6926 `#summon-result-modal.done .sr-cell.on .sr-orb::after` · `srsweep` 3.6s · 지연 i×.29s).
        /// 정본은 `border-radius` 가 배경을 잘라 주므로 `background-position` 만 흘린다 — 클론은 구슬 원판에 스텐실 마스크(`Mask`)를 걸고 그 안에서 띠를 민다.
        /// 띠는 처음 필요한 프레임에 한 번 세운다(구슬의 실제 폭이 서야 굽는 크기가 정해진다).
        /// </summary>
        void AnimateOrbSweeps()
        {
            if (!done || doneAt < 0f) return;
            SummonOrbSweepSpec sp = null;
            double ms = (Time.unscaledTime - doneAt) * 1000f;
            for (int i = 0; i < cells.Count; i++)
            {
                Cell c = cells[i];
                if (c.Orb == null || !c.On) continue;
                if (sp == null) sp = SummonFxStyle.OrbSweep;
                if (c.Sweep == null)
                {
                    float d = c.Orb.rectTransform.rect.width;
                    if (d < 4f) continue;                              // 레이아웃이 아직 안 섰다 — 다음 프레임에
                    int di = Mathf.RoundToInt(d);
                    Mask mask = c.Orb.GetComponent<Mask>();
                    if (mask == null) { mask = c.Orb.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = true; }
                    RectTransform rt = UiKit.Box(c.Orb.rectTransform, "sr-sweep");
                    UiKit.Anchor(rt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, d * (float)sp.SizeF, d);
                    Image img = rt.gameObject.AddComponent<Image>();
                    img.raycastTarget = false;
                    img.sprite = SummonFx.BakeOrbSweep("sr-orbsweep-" + di, di);
                    img.color = new Color(1f, 1f, 1f, 0f);
                    c.Sweep = img; c.SweepD = d;
                }
                double a, off;
                sp.At(ms, i, out a, out off);
                c.Sweep.color = new Color(1f, 1f, 1f, (float)a);
                c.Sweep.rectTransform.anchoredPosition = new Vector2((float)(off * c.SweepD), 0f);
            }
        }

        /// <summary>T459 — 흔들리는 판(정본 `.sr-wrap` = `handle.Content`) · 자가 본다.</summary>
        public RectTransform Wrap { get { return wrap; } }
        /// <summary>입장 셰이크가 도는 중인가 — 자가 본다.</summary>
        public bool EnterShaking { get { return enterAt >= 0f && SummonFxStyle.Enter.Shaking((Time.unscaledTime - enterAt) * 1000f); } }

        /// <summary>
        /// T459 ⓨ — 고등급 셀 광채의 무한 맥동(정본 6674 `.sr-cell.hi.on .sr-orbwrap { animation: srpulse 1.6s ease-in-out infinite; animation-delay: calc(.45s + var(--i) * .17s) }`).
        /// 정본은 box-shadow 두 겹의 번짐이 뛴다 — 클론은 광채 원판의 배율을 두 겹 비율의 평균으로 뛰게 한다(본체 밝기는 안 건드린다 · 정본 주석).
        /// </summary>
        void AnimateHiPulses()
        {
            SummonHiPulseSpec sp = null;
            for (int i = 0; i < cells.Count; i++)
            {
                Cell c = cells[i];
                if (c.Glow == null || !c.On) continue;
                if (sp == null) sp = SummonFxStyle.HiPulse;
                double ms = (Time.unscaledTime - c.OnAt) * 1000f;
                c.Glow.rectTransform.localScale = Vector3.one * (float)sp.ScaleAt(ms, i);
            }
        }


        /// <summary>
        /// 조연 셀 하나의 물러남(정본 `srrecede`) — 배율을 돌려주고 채도·밝기는 그 셀의 그림들에 건다.
        /// 제 색은 **처음 한 번만** 담는다(매 프레임 담으면 어두워진 색이 새 «제 색» 이 되어 회차마다 더 어두워진다).
        /// </summary>
        float Recede(Cell c, double ms)
        {
            SummonHeroSpec sp = SummonFxStyle.Hero;
            double sc, sat, br;
            sp.RecedeAt(ms, out sc, out sat, out br);
            if (c.Tint == null)
            {
                c.Tint = c.Root.GetComponentsInChildren<Graphic>(true);
                c.TintHome = new Color[c.Tint.Length];
                for (int i = 0; i < c.Tint.Length; i++) c.TintHome[i] = c.Tint[i].color;
            }
            for (int i = 0; i < c.Tint.Length; i++)
            {
                if (c.Tint[i] == null) continue;
                Color h = c.TintHome[i];
                c.Tint[i].color = Brighten(h, (float)br, (float)sat, h.a);
            }
            return (float)sc;
        }


        /// <summary>
        /// 주역 충격파 링(정본 `srheroring` .62s) — 알파 .95 → 0, 배율 .5 → `--ringmax`.
        /// 정본이 «일반 착지 링과 **별개 레이어**» 라 적어 둔 겹이라 셀의 다른 연출과 따로 돈다.
        /// </summary>
        void AnimateHeroRing()
        {
            if (heroRing == null || heroAtWall < 0f) return;
            float ms = (Time.unscaledTime - heroAtWall) * 1000f;
            float dur = SummonFxStyle.H("heroring_ms");
            float u = dur <= 0f ? 1f : Mathf.Clamp01(ms / dur);
            float k = (float)SummonFxStyle.HeroRingEase.Ease(u);
            float a0 = SummonFxStyle.H("heroring_a0"), s0 = SummonFxStyle.H("heroring_scale0");
            heroRing.color = new Color(1f, 1f, 1f, Mathf.Lerp(a0, 0f, k));
            heroRing.rectTransform.localScale = Vector3.one * Mathf.Lerp(s0, ringMax, k);
        }

        /// <summary>
        /// T448 — 동급(peer) 착지 링(정본 `.sr-cell.peer.on::after` · `srheroring .58s`) — 같은 키프레임(알파 .95 → 0 · 배율 .5 → `--ringmax` 1.5)을
        /// 셀마다 **제 착지 시각**(`OnAt`)에서 센다. 주역 링과 달리 `.hero` 비트를 안 기다린다(정본 6706 주석).
        /// </summary>
        void AnimatePeerRings()
        {
            float dur = -1f, a0 = 0f, s0 = 0f, smax = 1f;
            for (int i = 0; i < cells.Count; i++)
            {
                Cell c = cells[i];
                if (c.PeerRing == null || !c.On) continue;
                if (dur < 0f) { dur = SummonFxStyle.H("peerring_ms"); a0 = SummonFxStyle.H("heroring_a0"); s0 = SummonFxStyle.H("heroring_scale0"); smax = SummonFxStyle.H("ringmax_peer"); }
                float ms = (Time.unscaledTime - c.OnAt) * 1000f;
                float u = dur <= 0f ? 1f : Mathf.Clamp01(ms / dur);
                float k = (float)SummonFxStyle.HeroRingEase.Ease(u);
                c.PeerRing.color = new Color(1f, 1f, 1f, Mathf.Lerp(a0, 0f, k));
                c.PeerRing.rectTransform.localScale = Vector3.one * Mathf.Lerp(s0, smax, k);
            }
        }


        /// <summary>
        /// 주역 광창(정본 `srbeam` .52s) — 알파 0 → .95(24%) → 0 · 배율 .16 → 1.05 → 2.1 · 회전 −9° → 0° → 9°.
        /// 떠올랐다 **사라진다**(표가 그것을 강제한다) — 안 그러면 결과 화면에 빛기둥이 남는다.
        /// </summary>
        void AnimateBeam()
        {
            if (heroBeam == null || heroAtWall < 0f) return;
            SummonHeroSpec sp = SummonFxStyle.Hero;
            double a, sc, rot;
            sp.BeamAt((Time.unscaledTime - heroAtWall) * 1000f, out a, out sc, out rot);
            heroBeam.color = new Color(1f, 1f, 1f, (float)a);
            heroBeam.rectTransform.localScale = Vector3.one * (float)sc;
            heroBeam.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -(float)rot);   // CSS 의 +각은 시계 방향
        }

        /// <summary>
        /// 셀별 광원 재점화(정본 `srrelight` .34s) — 셀이 뜬 그 순간부터 광원 자리에서 켜졌다 꺼진다.
        /// 정본은 인라인 `animation-delay` 로 시각을 주지만 그 지연은 곧 «그 셀이 뜨는 시각» 이라, 여기서는
        /// 셀의 점등 시각(<c>OnAt</c>)에서 잰다 — 탭 건너뛰기로 셀이 한꺼번에 떠도 인과가 안 끊긴다.
        /// </summary>
        void AnimateRelights()
        {
            if (relights.Count == 0) return;
            SummonRelightSpec sp = SummonFxStyle.Relight;
            float tt = Time.unscaledTime;
            for (int i = 0; i < relights.Count && i < cells.Count; i++)
            {
                Image ri = relights[i];
                if (ri == null) continue;
                Cell c = cells[i];
                if (!c.On) continue;
                double a, sc;
                sp.At((tt - c.OnAt) * 1000f, RarityIdx(c.Entry.Rarity), out a, out sc);
                ri.color = new Color(1f, 1f, 1f, (float)a);
                ri.rectTransform.localScale = Vector3.one * (float)sc;
            }
        }

        /// <summary>
        /// 착지 스파크(정본 `srspark` .42s) — 셀이 뜬 그 순간부터 심에서 터져 바깥으로 날아가며 꺼진다.
        /// 알파는 0→62→100 **두 구간**이고 배율은 한 구간이다(표가 트랙을 둘로 나눠 쥔다).
        /// </summary>
        void AnimateSparks()
        {
            SummonSparkSpec sp = null;
            float tt = Time.unscaledTime;
            for (int i = 0; i < cells.Count; i++)
            {
                Cell c = cells[i];
                if (c.Spark == null || !c.On) continue;
                if (sp == null) sp = SummonFxStyle.Spark;
                double a, sc;
                sp.At((tt - c.OnAt) * 1000f, RarityIdx(c.Entry.Rarity), out a, out sc);
                c.Spark.color = new Color(1f, 1f, 1f, (float)a);
                c.Spark.rectTransform.localScale = Vector3.one * (float)sc;
            }
        }

        /// <summary>
        /// 비행 잔상(정본 `srghost` · 길이는 그 셀의 `var(--pop)`) — 셀이 날아온 **뒤쪽**(광원 쪽)에 남아 따라붙는다.
        ///
        /// 잔상은 셀 안에 있어 셀의 이동이 이미 곱해져 있다 — 그래서 여기서는 «뒤처진 만큼» 만 더 민다(정본 주석 그대로).
        /// 미는 방향은 «슬롯 → 광원» 사출 벡터(`--dx/--dy` · `SR_EJECT` 를 이미 곱한 값)다.
        /// </summary>
        void AnimateGhosts()
        {
            SummonGhostSpec sp = null;
            float tt = Time.unscaledTime;
            for (int i = 0; i < cells.Count; i++)
            {
                Cell c = cells[i];
                if (c.Ghost == null || !c.On) continue;
                if (sp == null) sp = SummonFxStyle.Ghost;
                double back, sc, a;
                sp.At((tt - c.OnAt) * 1000f, c.Pop * 1000f, RarityIdx(c.Entry.Rarity), out back, out sc, out a);
                c.Ghost.color = new Color(1f, 1f, 1f, (float)a);
                c.Ghost.rectTransform.localScale = Vector3.one * (float)sc;
                c.Ghost.rectTransform.anchoredPosition = c.ToLight * (float)back;
            }
        }

        /// <summary>
        /// 등급 챕터 펄스(정본 `srtierpulse` .54s) — 경계마다 화면 전체가 그 등급색으로 한 번 달아올랐다 식는다.
        /// 시각은 모달이 열린 때부터의 절대 시각(경계 셀보다 반 박자 앞)이라 `start` 에서 잰다.
        /// </summary>
        void AnimateTierBreaks()
        {
            if (tierBreaks.Count == 0) return;
            SummonTierBreakSpec sp = SummonFxStyle.TierBreak;
            float ms = (Time.unscaledTime - start) * 1000f;
            for (int i = 0; i < tierBreaks.Count; i++)
            {
                TierBreak b = tierBreaks[i];
                if (b.Pulse == null) continue;
                float e = ms - b.At;
                b.Pulse.color = new Color(1f, 1f, 1f, (float)sp.AlphaAt(e, b.Tier));
                if (b.Ring != null)
                {
                    double a, sc, br, bl;
                    sp.FlashAt(e, out a, out sc, out br, out bl);
                    b.Ring.color = new Color(1f, 1f, 1f, (float)a);
                    b.Ring.rectTransform.localScale = Vector3.one * (float)sc;
                    // 테 굵기·번짐은 구운 판이 쥔다 — 단계가 바뀔 때만 갈아 끼운다(프레임마다 굽지 않는다).
                    Sprite want = TierStep(i, sp.StepOf(e), b);
                    if (b.Ring.sprite != want) b.Ring.sprite = want;
                }
                if (b.Wick != null) b.Wick.color = new Color(1f, 1f, 1f, (float)sp.WickAt(e));
            }
        }

        /// <summary>
        /// 끝난 뒤의 잔잔한 고리(정본 `sridlering` 2.4s 무한) — `done` 이 된 뒤에만 돈다.
        /// 둘째 고리는 절반 늦게 시작해 물결이 끊기지 않는다(정본 `animation-delay: 1.2s`).
        /// </summary>
        void AnimateIdleRings()
        {
            if (idleRings.Count == 0) return;
            if (!done || doneAt < 0f)
            {
                for (int i = 0; i < idleRings.Count; i++)
                    if (idleRings[i] != null && idleRings[i].color.a != 0f) idleRings[i].color = new Color(1f, 1f, 1f, 0f);
                return;
            }
            SummonIdleRingSpec sp = SummonFxStyle.IdleRing;
            float ms = (Time.unscaledTime - doneAt) * 1000f;
            for (int i = 0; i < idleRings.Count; i++)
            {
                Image im = idleRings[i];
                if (im == null) continue;
                double a, sc;
                sp.At(ms, i, out a, out sc);
                im.color = new Color(1f, 1f, 1f, (float)a);
                im.rectTransform.localScale = Vector3.one * (float)sc;
            }
        }

        /// <summary>
        /// 챕터 링의 <paramref name="k"/> 번 단계 판 — **처음 쓸 때** 굽는다(이름으로 캐시되므로 같은 등급·단계는 한 번뿐).
        /// Open 한 프레임에 스무 장 넘게 구우면 그 프레임이 길어져 짧은 구간이 프레임 사이로 빠진다(런 828).
        /// </summary>
        static Sprite TierStep(int idx, int k, TierBreak b)
        {
            if (b.Steps[k] != null) return b.Steps[k];
            SummonTierBreakSpec tb = SummonFxStyle.TierBreak;
            double ka, ksc, kb, kblur;
            tb.FlashAt(tb.StepMid(k), out ka, out ksc, out kb, out kblur);
            // 판 반지름에 대한 비율로 바꾼다 — 배율은 거는 쪽이 따로 곱한다.
            b.Steps[k] = SummonFx.BakeTierRing(
                "sr-tierflash-" + ColorUtility.ToHtmlStringRGB(b.Rc) + "-" + k, b.Rc,
                PetSkillStyle.Rem((float)kb) / b.Half, (float)kblur / b.Half,
                PetSkillStyle.Rem((float)tb.RingGlowRem) / b.Half, 0f);
            return b.Steps[k];
        }

        /// <summary>충격파의 <paramref name="k"/> 번 단계 판 — 처음 쓸 때 굽는다(이름으로 캐시).</summary>
        Sprite ShockStep(bool echo, int k)
        {
            Sprite[] arr = echo ? shockEchoSteps : shockMainSteps;
            if (arr[k] != null) return arr[k];
            SummonShockSpec sk = SummonFxStyle.Shock;
            double a, sc, br, bl;
            if (echo) sk.EchoAt(sk.EchoStepMid(k), out a, out sc, out br, out bl);
            else sk.MainAt(sk.MainStepMid(k), srEnergy, out a, out sc, out br, out bl);
            arr[k] = SummonFx.BakeTierRing(
                (echo ? "sr-shockecho-" : "sr-shock-") + ColorUtility.ToHtmlStringRGB(shockLine) + "-" + k, shockLine,
                PetSkillStyle.Rem((float)br) / shockHalf, (float)bl / shockHalf,
                PetSkillStyle.Rem((float)(echo ? sk.EchoGlowRem : sk.GlowRem)) / shockHalf,
                echo ? 0f : PetSkillStyle.Rem((float)sk.InsetGlowRem) / shockHalf);   // 잔파엔 안쪽 광채가 없다
            return arr[k];
        }

        /// <summary>충격파 판 한 장 — 광원 한가운데에 선다(둘은 **형제**다 · 정본 «::after 로 두면 배율이 중첩된다»).</summary>
        static Image ShockPlate(RectTransform parent, string name, float w, Sprite sp)
        {
            RectTransform rt = UiKit.Box(parent, name);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, w);
            rt.anchoredPosition = Vector2.zero;
            Image im = rt.gameObject.AddComponent<Image>();
            im.raycastTarget = false;
            im.preserveAspect = false;
            im.sprite = sp;
            Material m = CraftFxPoly.Screen();
            if (m != null) im.material = m;
            im.color = new Color(1f, 1f, 1f, 0f);
            return im;
        }

        /// <summary>
        /// 예고 충격파 한 쌍(정본 `srshock` .46s @.23s · `srshockecho` .39s @.30s) —
        /// 모달이 열린 때부터 재고, 제 지연 전에는 꺼져 있다. 테 굵기·번짐은 구운 판이 쥔다(단계 경계에서만 갈아 끼운다).
        /// </summary>
        void AnimateShock()
        {
            if (shockMain == null) return;
            SummonShockSpec sp = SummonFxStyle.Shock;
            float ms = (Time.unscaledTime - start) * 1000f;
            double a, sc, br, bl;
            sp.MainAt(ms, srEnergy, out a, out sc, out br, out bl);
            shockMain.color = new Color(1f, 1f, 1f, (float)a);
            shockMain.rectTransform.localScale = Vector3.one * (float)sc;
            Sprite w1 = ShockStep(false, sp.MainStepOf(ms));
            if (shockMain.sprite != w1) shockMain.sprite = w1;
            if (shockEcho != null)
            {
                sp.EchoAt(ms, out a, out sc, out br, out bl);
                shockEcho.color = new Color(1f, 1f, 1f, (float)a);
                shockEcho.rectTransform.localScale = Vector3.one * (float)sc;
                Sprite w2 = ShockStep(true, sp.EchoStepOf(ms));
                if (shockEcho.sprite != w2) shockEcho.sprite = w2;
            }
        }

        /// <summary>
        /// 빛 모임(정본 `srcharge` .24s → `srchargeout` .21s @.24s) — 모달이 열린 때부터 재고, 끝나면 꺼진 채로 남는다.
        /// 배율은 등급 예고와 굴림 에너지의 **곱**이다(정본 «두 축이 독립이라 곱한다»).
        /// </summary>
        void AnimateChargeBurst()
        {
            if (chargeBurst == null) return;
            SummonChargeBurstSpec sp = SummonFxStyle.ChargeBurst;
            double a, sc;
            sp.At((Time.unscaledTime - start) * 1000f, preK, srEnergy, out a, out sc);
            chargeBurst.color = new Color(1f, 1f, 1f, (float)a);
            chargeBurst.rectTransform.localScale = Vector3.one * (float)sc;
        }

        /// <summary>
        /// 수렴 빛줄기(정본 `srstreak` .18s · 스포크마다 0~59ms 지연) — 바깥 끝이 광원으로 다가오는 동안 길어진다.
        /// 정본 주석대로 «셀 등장까지 전부 사라져야» 하므로 길이 + 최대 지연이 충전 길이 안이다.
        /// </summary>
        void AnimateStreaks()
        {
            if (streaks.Count == 0) return;
            SummonStreakSpec sp = SummonFxStyle.Streaks;
            float ms = (Time.unscaledTime - start) * 1000f;
            for (int i = 0; i < streaks.Count; i++)
            {
                Streak st = streaks[i];
                if (st.Img == null) continue;
                double a, dist, sy;
                sp.At(ms, st.DelayMs, st.RRem, out a, out dist, out sy);
                st.Img.color = new Color(1f, 1f, 1f, (float)a);
                st.Bar.anchoredPosition = new Vector2(0f, PetSkillStyle.Rem((float)dist));
                st.Bar.localScale = new Vector3(1f, (float)sy, 1f);
            }
        }

        /// <summary>깊이 평면 한 겹을 세운다 — 자리·크기는 번호에서 결정론으로 나오고 판은 겹이 한 장을 나눠 쓴다.</summary>
        void MoteLayer(RectTransform parent, string name, SummonParticleSpec.Layer L, Sprite sp, float w, float h, int sib)
        {
            RectTransform host = UiKit.Box(parent, name);
            UiKit.Fill(host);
            if (sib == 0) host.SetAsFirstSibling();
            for (int i = 0; i < L.N; i++)
            {
                float px = (float)L.XOf(i) * w - w * 0.5f;
                float py = L.FromBottom
                    ? -h * 0.5f + PetSkillStyle.Rem((float)L.YRem)      // 정본 `bottom: -2rem`
                    : h * 0.5f - (float)L.YOf(i) * h;                    // CSS 의 top% 는 위에서 아래로
                float sz = (float)L.SizePx(i) * PetSkillStyle.RemPx / 16f;   // 정본은 px — 기준 캔버스로 옮긴다
                RectTransform rt = UiKit.Box(host, "p" + i);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(sz, sz);
                Image im = rt.gameObject.AddComponent<Image>();
                im.raycastTarget = false;
                im.preserveAspect = false;
                im.sprite = sp;
                Material m = CraftFxPoly.Screen();
                if (m != null) im.material = m;
                im.color = new Color(1f, 1f, 1f, 0f);
                var mo = new Mote { Rt = rt, Img = im, Home = new Vector2(px, py), I = i, L = L };
                rt.anchoredPosition = mo.Home;
                motes.Add(mo);
            }
        }

        /// <summary>
        /// 깊이 평면 셋(정본 `srmote`·`srdust`·`srnear` · 겹마다 제 주기로 무한 되풀이) —
        /// 겹마다 **속도와 방향이 달라야** 시차가 생긴다(정본 «같은 방향이면 두 겹이 한 겹으로 붙어 보인다»).
        /// </summary>
        void AnimateMotes()
        {
            if (motes.Count == 0) return;
            float ms = (Time.unscaledTime - start) * 1000f;
            for (int i = 0; i < motes.Count; i++)
            {
                Mote mo = motes[i];
                if (mo.Img == null) continue;
                double a, tx, ty, sc;
                mo.L.At(mo.I, ms, out a, out tx, out ty, out sc);
                mo.Img.color = new Color(1f, 1f, 1f, (float)a);
                // CSS 의 +y 는 아래 — 여기서 한 번 뒤집는다.
                mo.Rt.anchoredPosition = mo.Home + new Vector2(PetSkillStyle.Rem((float)tx), -PetSkillStyle.Rem((float)ty));
                mo.Rt.localScale = Vector3.one * (float)sc;
            }
        }

        /// <summary>깊이 평면의 점 — 자가 본다.</summary>
        public int MoteCount { get { return motes.Count; } }
        public Image MoteOf(int i) { return i >= 0 && i < motes.Count ? motes[i].Img : null; }
        public Vector2 MoteAt(int i) { return i >= 0 && i < motes.Count ? motes[i].Rt.anchoredPosition : Vector2.zero; }
        /// <summary>그 점의 «집»(움직이기 전 자리) — 자가 본다(움직임을 재려면 기준이 있어야 한다).</summary>
        public Vector2 MoteHomeOf(int i) { return i >= 0 && i < motes.Count ? motes[i].Home : Vector2.zero; }
        /// <summary>모달이 열린 뒤 흐른 시각(ms) — 자가 본다(연출은 전부 이 하나로 돌아간다).</summary>
        public float ElapsedMs { get { return (Time.unscaledTime - start) * 1000f; } }

        /// <summary>수렴 빛줄기 — 자가 본다.</summary>
        public int StreakCount { get { return streaks.Count; } }
        public Image StreakOf(int i) { return i >= 0 && i < streaks.Count ? streaks[i].Img : null; }
        /// <summary>그 빛줄기의 바깥 끝이 광원에서 떨어진 거리(px) — 자가 본다.</summary>
        public float StreakDist(int i) { return i >= 0 && i < streaks.Count ? streaks[i].Bar.anchoredPosition.y : -1f; }

        /// <summary>빛 모임 판 · 등급 예고 세기 — 자가 본다.</summary>
        public Image ChargeBurst { get { return chargeBurst; } }
        public float PreK { get { return preK; } }

        /// <summary>예고 충격파 본파·잔파 — 자가 본다.</summary>
        public Image ShockMain { get { return shockMain; } }
        public Image ShockEcho { get { return shockEcho; } }

        /// <summary>굴림 에너지(정본 `--sr-e`) — 자가 본다.</summary>
        public float SrEnergy { get { return srEnergy; } }

        /// <summary>끝난 뒤의 잔잔한 고리 — 자가 본다.</summary>
        public Image IdleRingOf(int i) { return i >= 0 && i < idleRings.Count ? idleRings[i] : null; }

        /// <summary>그 고리 수 — 자가 본다.</summary>
        public int IdleRingCount { get { return idleRings.Count; } }

        /// <summary>등급 챕터 펄스 수 — 자가 본다.</summary>
        public int TierBreakCount { get { return tierBreaks.Count; } }

        /// <summary>그 경계의 챕터 링 — 자가 본다.</summary>
        public Image TierRingOf(int i) { return i >= 0 && i < tierBreaks.Count ? tierBreaks[i].Ring : null; }

        /// <summary>그 경계의 심지 — 자가 본다.</summary>
        public Image TierWickOf(int i) { return i >= 0 && i < tierBreaks.Count ? tierBreaks[i].Wick : null; }

        /// <summary>그 경계의 펄스 판 — 자가 본다.</summary>
        public Image TierPulseOf(int i) { return i >= 0 && i < tierBreaks.Count ? tierBreaks[i].Pulse : null; }

        /// <summary>그 경계가 켜지는 시각(ms · 모달이 열린 때부터) — 자가 본다.</summary>
        public float TierBreakAt(int i) { return i >= 0 && i < tierBreaks.Count ? tierBreaks[i].At : -1f; }

        /// <summary>그 셀의 비행 잔상 — 자가 본다.</summary>
        public Image GhostOf(int i) { return i >= 0 && i < cells.Count ? cells[i].Ghost : null; }

        /// <summary>그 셀의 착지 스파크 — 자가 본다.</summary>
        public Image SparkOf(int i) { return i >= 0 && i < cells.Count ? cells[i].Spark : null; }

        /// <summary>그 셀의 «슬롯 → 광원» 사출 벡터(정본 `--dx/--dy` · 전체 벡터의 `SR_EJECT` 만큼) — 자가 본다.</summary>
        /// <summary>
        /// T455 — «슬롯 → 광원» 벡터를 한 번 잰다(정본 `--dx/--dy` 는 `setSummonEjectPaths` 가 심어 둔다 — 클론엔 없어 여기서 잰다).
        /// 종전엔 이 셈이 «충전 창의 첫 프레임» 안에 있어서, 프레임이 길어 그 창에 한 번도 안 들어오면 벡터가 **0 으로 남았다**(T455).
        /// 이제 부르는 쪽이 «첫 틱에 한 번» 을 맡고 이 함수는 재기만 한다 — 시점이 창이 아니라 **상태**(`ejectSet`)로 정해진다.
        /// </summary>
        void MeasureEject()
        {
                // «슬롯 → 광원» 벡터(정본 `--dx/--dy` 는 setSummonEjectPaths 가 심어 둔다 — 클론엔 없어 여기서 잰다).
                // ⚑ 15회차 — **62%만 되짚는다**(정본 `SR_EJECT = 0.62` · ui.js 604). 3회차가 재는 길은 옮겼지만
                //   그 한 줄을 빠뜨려 클론은 벡터를 **100%** 되짚고 있었다. 정본 주석이 그것을 이름으로 금지한다:
                //   «⚠️ 벡터를 100% 되짚으면 전 셀이 한 점에서 겹쳐 나와 5개가 한 덩어리로 보인다 —
                //    일부(EJECT)만 되짚어 «광원 쪽에서 밀려 나온» 인상만 남긴다».
                //   이 벡터를 쓰는 자리 셋(주역 등장 `srheropop` · 조연 흡기 `srinhale` · 잔상 `srghost`)이 전부 62% 벡터다.
                float eject = SummonFxStyle.L("eject_f");
                for (int i = 0; i < cells.Count; i++)
                {
                    Cell c = cells[i];
                    c.Home = c.Root.anchoredPosition;
                    // 두 자리 다 **같은 부모의 지역 좌표**로 재야 한다 — anchoredPosition 과 localPosition 은 상수만큼
                    // 어긋나 있어서(앵커·피벗) 차이(벡터)는 같지만 한쪽을 다른 쪽에서 빼면 그 상수가 섞여 들어간다.
                    c.ToLight = haloImg != null
                        ? ((Vector2)c.Root.parent.InverseTransformPoint(haloImg.rectTransform.position) - (Vector2)c.Root.localPosition) * eject
                        : Vector2.zero;
                }
                    }

        public Vector2 EjectOf(int i) { return i >= 0 && i < cells.Count ? cells[i].ToLight : Vector2.zero; }
        /// <summary>T458 — i 번째 셀의 뿌리 · 제자리(슬롯) · 켜졌는가(자가 본다).</summary>
        public RectTransform CellRootOf(int i) { return i >= 0 && i < cells.Count ? cells[i].Root : null; }
        public Vector2 HomeOf(int i) { return i >= 0 && i < cells.Count ? cells[i].Home : Vector2.zero; }
        public bool CellOn(int i) { return i >= 0 && i < cells.Count && cells[i].On; }
        /// <summary>T459 ⓨ — i 번째 셀의 광채 원판(고등급이 아니면 null).</summary>
        public Image GlowOf(int i) { return i >= 0 && i < cells.Count ? cells[i].Glow : null; }
        /// <summary>T459 ⓩ — i 번째 구슬의 스페큘러 띠(done 뒤 첫 프레임에 선다 · 그 전엔 null).</summary>
        public Image SweepOf(int i) { return i >= 0 && i < cells.Count ? cells[i].Sweep : null; }

        /// <summary>셀별 재점화 플래시 — 자가 본다.</summary>
        public Image RelightOf(int i) { return i >= 0 && i < relights.Count ? relights[i] : null; }

        /// <summary>재점화 플래시 수 — 자가 본다.</summary>
        public int RelightCount { get { return relights.Count; } }

        /// <summary>주역 광창 — 자가 본다.</summary>
        public Image HeroBeam { get { return heroBeam; } }

        void FireHero()
        {
            if (heroFired) return;
            heroFired = true;
            // 정본 fireSummonHero 748~757: 홀드백이면 `.flash`(전 화면), **아니면 `.wipe`** — 그 갈래를 그대로.
            // 예전 클론은 둘 다 전 화면 섬광이고 홀드백이 아닐 때만 옅게 했다(0.45) — 정본이 «쓰면 안 된다» 고
            // 못 박은 자리다(x75 는 위쪽 20셀이 같이 하얗게 뜬다).
            if (holdback || wipe == null)
            {
                flash.color = PetSkillStyle.Rarity(Defs, best);
                flashAt = Time.unscaledTime;
            }
            else
            {
                // 가운데를 주역 셀로 옮긴다(정본 `--fx`/`--fy`). 스프라이트는 가운데로 구웠으니 자리만 잡는다.
                RectTransform wr = (RectTransform)wipe.transform;
                if (heroIdx >= 0 && heroIdx < cells.Count && cells[heroIdx].Root != null)
                    wr.pivot = HeroPivot(wr, cells[heroIdx].Root);
                wipeAt = Time.unscaledTime;
            }
            // 정본 5675: 화면 킥은 **홀드백 여부와 무관하게** `.hero` 에 건다 — 섬광이든 와이프든 판은 똑같이 흔들린다.
            kickAt = Time.unscaledTime;
            heroAtWall = kickAt;
            var g = PetSkillHost.SfxGacha;
            if (g != null) g(best);
        }

        /// <summary>주역 셀의 가운데를 와이프 상자 안의 피벗(0~1)으로 — 정본 `transform-origin: var(--fx) var(--fy)`.</summary>
        static Vector2 HeroPivot(RectTransform box, RectTransform cell)
        {
            Vector2 c = (Vector2)box.InverseTransformPoint(cell.TransformPoint(cell.rect.center));
            Rect r = box.rect;
            if (r.width <= 0f || r.height <= 0f) return new Vector2(0.5f, 0.5f);
            return new Vector2(Mathf.Clamp01((c.x - r.xMin) / r.width), Mathf.Clamp01((c.y - r.yMin) / r.height));
        }

        /// <summary>정본 `@keyframes srwipe`(6194~6198): 불투명도 0 → .98(15%% ≈ 70ms 정점) → 0 · 배율 .55 → 1.12 → 2.2.</summary>
        void AnimateWipe()
        {
            if (wipe == null) return;
            if (wipeAt < 0f) return;
            float ms = SummonFxStyle.L("wipe_ms");
            float t = ms <= 0f ? 1f : (Time.unscaledTime - wipeAt) * 1000f / ms;
            float pk = SummonFxStyle.L("wipe_peak_f"), pa = SummonFxStyle.L("wipe_peak_a");
            float s0 = SummonFxStyle.L("wipe_scale0_f"), sp = SummonFxStyle.L("wipe_scale_peak_f"), s1 = SummonFxStyle.L("wipe_scale1_f");
            float a, sc;
            if (t <= pk) { float k = pk <= 0f ? 1f : t / pk; a = Mathf.Lerp(0f, pa, k); sc = Mathf.Lerp(s0, sp, k); }
            else { float k = Mathf.Clamp01((t - pk) / Mathf.Max(1e-4f, 1f - pk)); a = Mathf.Lerp(pa, 0f, k); sc = Mathf.Lerp(sp, s1, k); }
            if (t >= 1f) { a = 0f; sc = s1; wipeAt = -1f; }
            Color c = wipe.color; wipe.color = new Color(c.r, c.g, c.b, a);
            wipe.transform.localScale = new Vector3(sc, sc, 1f);
        }

        void AnimateFlash()
        {
            if (flashAt < 0f || flash == null) return;
            float t = (Time.unscaledTime - flashAt) / PetSkillStyle.L("sr_flash_sec");
            float a = holdback ? Mathf.Clamp01(1f - t) * 0.85f : Mathf.Clamp01(1f - t) * 0.45f;
            Color c = flash.color;
            flash.color = new Color(c.r, c.g, c.b, a);
            if (t >= 1f) flashAt = -1f;
        }

        float doneAt = -1f;

        void Finish()
        {
            done = true;
            if (bgAImg != null) { bgPromoteMs = 0; ApplyBgPromote(); }   // T454 ⓒ — 정본 `.done` 의 배경 승격(.5s ease-out)
            doneAt = Time.unscaledTime;
            if (fx != null) fx.SetDone();   // T179 — 정본 #summon-result-modal.done: 별 켜기 · 광선 done 마스크
            // 소환진이 등급색으로 물드는 것은 **이 순간뿐**이다(정본 ui.js 551~552 · style.css 7182~7185) — 알파도 .2 → .26.
            if (floorImg != null)
            {
                Color rc = PetSkillStyle.Rarity(Defs, best);
                floorImg.color = new Color(rc.r, rc.g, rc.b, SummonFxStyle.L("floor_done_a"));
                floorBase = floorImg.color;
                if (tickImg != null)
                {
                    // 정본 ui.js 548~551: 선은 **등급색 원본이 아니라 밝게 띄운 파생색**이다 —
                    // «배경까지 그 등급색으로 물든 뒤라 원색 그대로는 배경에 묻혀 소환진이 사라진다».
                    Color lr = Shade(rc, SummonFxStyle.L("floor_line_hi_shade_f"));
                    tickBase = new Color(lr.r, lr.g, lr.b, SummonFxStyle.L("floor_line_hi_a"));
                    tickImg.color = tickBase;
                }
            }
            if (hint != null) hint.SetActive(false);
            if (ok != null) { ok.SetActive(true); okAt = doneAt; }
            if (chips != null && rolls > 1) chips.SetActive(true);
            if (solo != null) solo.SetActive(true);
        }

        void ApplyBgPromote()
        {
            if (bgAImg == null || bgPromoteMs < 0) return;
            double p = TransitionRules.Progress(TransitionUi.Table.Get("sr_bg_done"), bgPromoteMs);
            bgAImg.color = Color.Lerp(bgAPre, bgADone, (float)p);
        }

        /// <summary>T454 ⓒ — 배경 승격을 지금 끝낸다(정지 촬영·자). done 전이면 아무것도 안 한다.</summary>
        public void SettleBg()
        {
            if (bgPromoteMs < 0) return;
            bgPromoteMs = -1;
            if (bgAImg != null) bgAImg.color = bgADone;
        }

        /// <summary>열려 있는 소환 결과의 배경 승격을 끝낸다 — 촬영 직전(`UiShotsTests`)에 부른다.</summary>
        public static void SettleBgAll() { if (Current != null) Current.SettleBg(); }

        /// <summary>오버레이 탭(원작 onSummonResultTap): 연출 중이면 스킵(전부 즉시) · 끝났으면 닫기.</summary>
        public void OnTap()
        {
            if (done) { Close(); return; }
            foreach (Cell c in cells) TurnOn(c);
            idx = cells.Count;
            if (heroIdx >= 0) FireHero();
            Finish();
        }

        public void Close()
        {
            if (Current == this) Current = null;
            if (sheet != null) sheet.Modal.Close(handle);
        }

        void OnDestroy() { if (Current == this) Current = null; }
    }
}
