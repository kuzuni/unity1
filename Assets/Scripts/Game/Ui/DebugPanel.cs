using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Save;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 디버그 패널(ROUTINE T22 · 원작 ui.js renderDebug · panel-debug · 테스트 전용 · T27 플레이 봇이 쓴다): 스테이지 이동(티어·챕터·스테이지 · 이전/다음) · 재화 +100000 ×7 · 신화 알 +5 · 대장간 Lv+1 · 던전 열쇠 리필.
    /// 탭바에는 없다(T18 카탈로그 결정 — 배포 탭바에서 숨김) — <see cref="MetaHost.OpenDebug"/> 로 연다. 전투 재배치·펫 알·던전 열쇠는 그 시스템(T8·T16·T23)이 훅을 꽂는다.
    /// </summary>
    public static class DebugPanel
    {
        public const string Name = "debug";
        public static readonly string[] Currencies = { "hammers", "coins", "gems", "tickets", "winders", "potions", "eggCurrency" };
        public const double AddAmount = 100000;

        /// <summary>T8 `Combat.setupStage()` · T16 `Pets.addEgg('mythic')` · T8 `Combat.recalcHero()` · T23 던전 열쇠 리필 자리.</summary>
        public static Action StageChanged, AddMythicEggs, HeroChanged, RefillKeys;

        private static int tier, chapter, stage;

        public static void Open(MetaHost h)
        {
            tier = h.S.Difficulty; chapter = h.S.Chapter; stage = h.S.Stage;
            h.Popups.Show(Name);
            Render(h);
        }

        public static void Close(MetaHost h) { h.Popups.Hide(Name); }

        public static void Render(MetaHost h)
        {
            Popup p = h.Popups.Find(Name);
            if (p == null) return;
            RectTransform root = PopupLayer.Clear(p);
            RectTransform sheet = PopupKit.Sheet(root, "sheet", "debug_bg");
            float rem = PopupKit.Rem;
            RectTransform box = UiKit.Box(sheet, "scroll");
            UiKit.Band(box, 0f, UiKit.L("tabbar_top"));
            RectTransform content = PopupKit.ScrollList(box, "list", rem * 0.4f, rem * 0.8f, UiKit.H("sheet_pad_top"), TextAnchor.UpperLeft);
            SaveDefs d = SaveIo.Defs;
            float rowH = UiKit.H("debug_row_h");

            PopupKit.Label(content, "title", TextKind.Title, "디버그 (테스트 전용)", "ink", TextAlignmentOptions.Left);
            PopupKit.Label(content, "h-stage", TextKind.Body, "스테이지 이동  " + h.S.StageName(d), "debug_muted", TextAlignmentOptions.Left);
            RectTransform r1 = PopupKit.Item(content, "row-stage", -1f, rowH);
            PopupKit.Row(r1, 0f, rem * 0.4f);
            string tierName = tier == 0 ? "기본 순환" : d.DifficultyNames[Mathf.Clamp(tier, 0, d.DifficultyNames.Length - 1)];
            PopupKit.Btn(r1, "tier", tierName, "card_bg", "pp_line", () => { tier = (tier + 1) % (d.MaxDifficulty + 1); Render(h); }, rem * 7f, rowH, "ink", TextKind.Sub);
            PopupKit.Btn(r1, "ch-", "−", "card_bg", "pp_line", () => { chapter = Mathf.Max(1, chapter - 1); Render(h); }, rowH, rowH, "ink", TextKind.Sub);
            PopupKit.Label(r1, "ch", TextKind.Body, chapter.ToString(), "ink", TextAlignmentOptions.Center, false, true, rowH).gameObject.GetComponent<LayoutElement>().preferredWidth = rem * 2f;
            PopupKit.Btn(r1, "ch+", "+", "card_bg", "pp_line", () => { chapter = Mathf.Min(d.ChaptersPerCycle, chapter + 1); Render(h); }, rowH, rowH, "ink", TextKind.Sub);
            PopupKit.Label(r1, "dash", TextKind.Body, "-", "debug_muted", TextAlignmentOptions.Center, false, true, rowH).gameObject.GetComponent<LayoutElement>().preferredWidth = rem;
            PopupKit.Btn(r1, "st-", "−", "card_bg", "pp_line", () => { stage = Mathf.Max(1, stage - 1); Render(h); }, rowH, rowH, "ink", TextKind.Sub);
            PopupKit.Label(r1, "st", TextKind.Body, stage.ToString(), "ink", TextAlignmentOptions.Center, false, true, rowH).gameObject.GetComponent<LayoutElement>().preferredWidth = rem * 2f;
            PopupKit.Btn(r1, "st+", "+", "card_bg", "pp_line", () => { stage = Mathf.Min(d.StagesPerChapter, stage + 1); Render(h); }, rowH, rowH, "ink", TextKind.Sub);
            PopupKit.Btn(r1, "go", "이동", "pp_green", "pp_green_dk", () => { SetStage(h, tier, chapter, stage); h.Toast("📍 " + h.S.StageName(d) + "로 이동"); }, rem * 4f, rowH, "stage_ink", TextKind.Sub);
            RectTransform r2 = PopupKit.Item(content, "row-step", -1f, rowH);
            PopupKit.Row(r2, 0f, rem * 0.4f);
            PopupKit.Btn(r2, "prev", "◀ 이전 스테이지", "card_bg", "pp_line", () => Step(h, -1), rem * 9f, rowH, "ink", TextKind.Sub);
            PopupKit.Btn(r2, "next", "다음 스테이지 ▶", "card_bg", "pp_line", () => Step(h, 1), rem * 9f, rowH, "ink", TextKind.Sub);

            PopupKit.Label(content, "h-cur", TextKind.Body, "재화 지급", "debug_muted", TextAlignmentOptions.Left);
            RectTransform cur = null;
            for (int i = 0; i < Currencies.Length; i++)
            {
                if (i % 3 == 0) { cur = PopupKit.Item(content, "row-cur-" + i, -1f, rowH); PopupKit.Row(cur, 0f, rem * 0.4f); }
                string key = Currencies[i];
                PopupKit.Btn(cur, "add-" + key, Label(h, key) + " +" + PopupKit.Fmt(AddAmount), "card_bg", "pp_line", () => AddCurrency(h, key), rem * 8f, rowH, "ink", TextKind.Sub);
            }
            RectTransform r3 = PopupKit.Item(content, "row-egg", -1f, rowH);
            PopupKit.Row(r3, 0f, rem * 0.4f);
            PopupKit.Btn(r3, "eggs", "신화 알 +5", "card_bg", "pp_line", () => { if (AddMythicEggs != null) AddMythicEggs(); h.Toast("🥚 신화 알 +5"); h.Touch(); }, rem * 8f, rowH, "ink", TextKind.Sub);

            PopupKit.Label(content, "h-forge", TextKind.Body, "대장간   현재 Lv." + h.S.ForgeLevel + " / " + d.ForgeMaxLevel, "debug_muted", TextAlignmentOptions.Left);
            RectTransform r4 = PopupKit.Item(content, "row-forge", -1f, rowH);
            PopupKit.Row(r4, 0f, rem * 0.4f);
            PopupKit.Btn(r4, "forge-up", "Lv +1", "pp_green", "pp_green_dk", () => ForgeLevelUp(h), rem * 5f, rowH, "stage_ink", TextKind.Sub);

            PopupKit.Label(content, "h-keys", TextKind.Body, "던전 열쇠", "debug_muted", TextAlignmentOptions.Left);
            RectTransform r5 = PopupKit.Item(content, "row-keys", -1f, rowH);
            PopupKit.Row(r5, 0f, rem * 0.4f);
            PopupKit.Btn(r5, "keys", "모든 열쇠 리필", "pp_green", "pp_green_dk", () => { if (RefillKeys != null) RefillKeys(); h.Toast("🗝 던전 열쇠 리필 완료"); h.Touch(); }, rem * 8f, rowH, "stage_ink", TextKind.Sub);

            PopupKit.SheetBack(sheet, () => Close(h));
        }

        /// <summary>원작 debugCurrencyLabel — 표시 이름은 meta.json `CUR_KR` 한 곳에서.</summary>
        private static string Label(MetaHost h, string key)
        {
            string kr = h.Meta.Quests.CurKr.Get(key, null);
            if (kr == null) { Debug.LogWarning("[debug] 재화 '" + key + "' 의 한글 라벨(kr)이 없다 — meta.json CUR_KR 을 확인할 것"); return key; }
            return kr;
        }

        public static void AddCurrency(MetaHost h, string key)
        {
            h.Wallet.Add(key, AddAmount);
            h.Touch();
            h.Toast(Label(h, key) + " +" + PopupKit.Fmt(AddAmount));
        }

        /// <summary>원작 onDebugStageStep — 사이클 경계를 실제 전진과 같은 규칙으로 넘는다(25-10 다음 = 다음 티어 1-1).</summary>
        public static void Step(MetaHost h, int dir)
        {
            SaveDefs d = SaveIo.Defs;
            int t = h.S.Difficulty, c = h.S.Chapter, s = h.S.Stage + dir;
            if (s < 1)
            {
                if (c > 1) { c--; s = d.StagesPerChapter; }
                else if (t > 0) { t--; c = d.ChaptersPerCycle; s = d.StagesPerChapter; }
                else s = 1;
            }
            if (s > d.StagesPerChapter)
            {
                if (c < d.ChaptersPerCycle) { c++; s = 1; }
                else if (t < d.MaxDifficulty) { t++; c = 1; s = 1; }
                else s = d.StagesPerChapter;
            }
            SetStage(h, t, c, s);
        }

        /// <summary>원작 debugSetStage.</summary>
        public static void SetStage(MetaHost h, int t, int c, int s)
        {
            SaveDefs d = SaveIo.Defs;
            h.S.Difficulty = t; h.S.Chapter = c; h.S.Stage = s;
            if (h.S.CurRank(d) > h.S.BestRank(d)) { h.S.BestDifficulty = t; h.S.BestChapter = c; h.S.BestStage = s; }
            tier = t; chapter = c; stage = s;
            if (StageChanged != null) StageChanged();
            if (Hud.Instance != null) Hud.Instance.SetStage(h.S.StageName(d));
            h.Touch();
        }

        public static void ForgeLevelUp(MetaHost h)
        {
            h.S.ForgeLevel = Mathf.Min(SaveIo.Defs.ForgeMaxLevel, h.S.ForgeLevel + 1);
            if (HeroChanged != null) HeroChanged();
            h.Touch();
            h.Toast("⚒️ 대장간 Lv." + h.S.ForgeLevel);
        }
    }
}
