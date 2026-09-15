using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Forging;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T118 — 장비 교체 «던져내기»(정본 `equip-swap-throwout`). 앞 둘은 연출을 직접 부르고, 셋째(2회차)는 `ForgeHost.DoResolveCraft` 의 실장착으로 돈다:
    /// 붙잡은 칸이 있고 · 옛 타일 복제가 바깥쪽으로 날아 정수 바퀴 돌고 · 소리 셋이 정본 순서(던짐 0 → 딸깍 130 → 착지 522ms) · 칸은 그동안 빈 소켓이었다가 되살아나고 · 수명이 끝나면 층이 빈다 · 팝업 카드가 열려 있으면 착지 자리가 카드 밖.
    /// </summary>
    public class EquipSwapTests
    {
        static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            yield return null;
        }

        static IEnumerator WaitMs(double ms) { double t = 0; while (t < ms) { t += Time.unscaledDeltaTime * 1000.0; yield return null; } }

        static int Enabled(RectTransform cell)
        {
            int n = 0;
            foreach (Graphic g in cell.GetComponentsInChildren<Graphic>(true)) if (g.transform != cell && g.enabled) n++;
            return n;
        }

        [UnityTest]
        public IEnumerator 옛_타일이_바깥쪽으로_정수_바퀴_돌며_눕고_소리_셋이_정본_순서이며_칸은_빈_소켓이었다가_되살아난다()
        {
            yield return Boot();
            UiRoot root = UiRoot.Instance;
            string slot = ForgeHost.Instance.Defs.Slots[0];
            RectTransform cell = EquipSwapFx.CellOf(slot);
            Assert.IsNotNull(cell, "장비 시트의 첫 칸(equip-grid/cell-" + slot + ")");
            int shown = Enabled(cell);
            Assert.Greater(shown, 0, "칸에 보이는 그래픽이 있다");
            EquipSwapGrabbed gr = EquipSwapFx.Grab(slot);
            Assert.IsNotNull(gr, "grabEquipSwapFx — 칸을 붙잡았다");
            Assert.AreSame(cell, gr.Cell);
            Assert.Greater(gr.G.W, 0); Assert.Greater(gr.G.H, 0);
            Assert.AreEqual(root.App.rect.width, gr.G.HostW, 0.5, "앱 상자 폭");
            Assert.IsFalse(gr.G.HasBlock, "메인 화면 — 열린 팝업 카드 없음");
            Assert.Greater(gr.G.FloorY, gr.G.Cy, "바닥(장비 시트 아랫단)은 칸 아래");
            EquipSwapSpec s = EquipSwapFx.Spec;
            Assert.IsTrue(EquipSwapFx.Play(gr), "playEquipSwapFx");
            EquipSwapFx fx = EquipSwapFx.Instance;
            Assert.IsNotNull(fx);
            Transform modals = root.App.Find("modals");
            Assert.IsNotNull(modals);
            Assert.AreEqual(modals.GetSiblingIndex() + 1, fx.Layer.GetSiblingIndex(), "층은 비교 팝업(modals) 바로 위(정본 z 21)");
            EquipSwapPlan p = fx.LastPlan;
            Assert.AreEqual(gr.G.Cx < gr.G.HostW / 2 ? -1 : 1, p.Dir, "바깥쪽으로");
            double turns = (Mathf.Abs((float)p.Spin) - p.Tilt) / 360.0;
            Assert.IsTrue(Mathf.Abs((float)(turns - 1)) < 1e-6 || Mathf.Abs((float)(turns - 2)) < 1e-6, "정수 바퀴 + 기울기: " + p.Spin);
            Assert.IsTrue(p.Tilt >= s.TiltMinDeg && p.Tilt < s.TiltMaxDeg);
            Assert.IsTrue(p.LandX >= p.Reach - 1e-6 && p.LandX <= gr.G.HostW - p.Reach + 1e-6, "착지 x 는 반경 안");
            Assert.IsTrue(p.Lands, "카드가 없으니 바닥에 눕는다");
            Assert.IsTrue(p.Dx * p.Dir >= -1e-6, "dx 는 나가는 방향(가장자리 칸은 0 까지 짧아진다)");
            yield return null;
            Assert.AreEqual(1, fx.Flying, "복제 타일 하나가 날고 있다");
            Assert.AreEqual(1, fx.LastSounds.Count); Assert.AreEqual("equipToss", fx.LastSounds[0], "던질 때 equipToss");
            Assert.Less(Enabled(cell), shown, "칸은 빈 소켓(내용물 감춤)");
            RectTransform fly = fx.Layer.Find("eqsw-fly") as RectTransform;
            Assert.IsNotNull(fly, "복제 타일이 층에 섰다");
            // T331 5회차부터 이 그늘은 손으로 깐 판이 아니라 **정본 번짐을 구운 판**이다(정본 `.eqsw-fly-box` 7291~7292).
            // 겹 이름도 그때 `UiShadow` 의 공용 이름으로 바뀌었다 — 그래서 여기서 그 이름으로 찾는다.
            Transform sh = UiShadow.Find(fly, "eqswfly_drop");
            Assert.IsNotNull(sh, "공중에 뜬 물건의 드롭섀도");
            Assert.AreEqual(0, sh.GetSiblingIndex(), "그늘은 첫 형제라야 복제 타일 **뒤**에 그려진다");
            UnityEngine.UI.Image shi = sh.GetComponent<UnityEngine.UI.Image>();
            Assert.IsNotNull(shi, "그늘은 그림 한 장이다");
            Assert.IsNotNull(shi.sprite, "번진 판이라야 한다 — T117 이 «번짐은 못 내고 판만» 이라 적어 두었던 자리다");
            Assert.IsNull(fly.GetComponent<Button>(), "복제는 버튼이 아니다");
            // 딸깍(130ms) → 착지(522ms): 소리 순서 던짐 → 딸깍 → 착지
            yield return WaitMs(EquipSwapRules.LandMs(s) + 120);
            Assert.AreEqual(3, fx.LastSounds.Count, "소리 셋: " + string.Join(",", fx.LastSounds.ToArray()));
            Assert.AreEqual("equipSnap", fx.LastSounds[1], "딸깍은 130ms");
            Assert.AreEqual("equipDrop", fx.LastSounds[2], "착지음은 522ms");
            Assert.AreEqual(1, fx.Dusts, "착지 자리에 먼지 하나");
            Assert.IsNotNull(fx.Layer.Find("eqsw-dust"));
            Assert.AreEqual(1, fx.PlayCount);
            // 수명 끝: 타일 900+160 · 먼지 522+520 · 딸깍 130+340 → 층이 빈다 · 칸이 되살아난다
            yield return WaitMs(EquipSwapRules.LandMs(s) + s.DustRemoveMs + 400);
            Assert.AreEqual(0, fx.Flying, "타일은 걷혔다"); Assert.AreEqual(0, fx.Dusts, "먼지도"); Assert.AreEqual(0, fx.Snaps, "딸깍도");
            Assert.AreEqual(0, fx.Layer.childCount, "층은 비었다(정본 el.remove)");
            Assert.AreEqual(shown, Enabled(cell), "칸의 내용물이 되살아났다");
            // null 이면 조용히 생략
            Assert.IsFalse(EquipSwapFx.Play(null));
            Assert.IsNull(EquipSwapFx.Grab("no-such-slot"), "없는 칸은 null");
        }

        [UnityTest]
        public IEnumerator 팝업_카드가_열려_있으면_착지_자리가_카드_밖이거나_화면_밖으로_떨어진다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            string slot = ForgeHost.Instance.Defs.Slots[0];
            PlayerInfoPopup.Open(h);
            yield return null;
            EquipSwapGrabbed gr = EquipSwapFx.Grab(slot);
            Assert.IsNotNull(gr);
            Assert.IsTrue(gr.G.HasBlock, "열린 팝업 카드(.modal-card)의 가로 범위를 붙잡았다");
            Assert.Greater(gr.G.BlockR, gr.G.BlockL);
            Assert.IsTrue(EquipSwapFx.Play(gr));
            EquipSwapPlan p = EquipSwapFx.Instance.LastPlan;
            bool clear = p.LandX + p.Reach <= gr.G.BlockL + 1e-6 || p.LandX - p.Reach >= gr.G.BlockR - 1e-6;
            Assert.IsTrue(!p.Lands || clear, "눕는 자리는 카드 밖(옆 빈 띠) — 아니면 화면 밖으로");
            yield return WaitMs(EquipSwapRules.LandMs(s: EquipSwapFx.Spec) + 120);
            if (!p.Lands) { Assert.AreEqual(0, EquipSwapFx.Instance.Dusts, "화면 밖으로 떨어지면 먼지·착지음 없음"); Assert.IsFalse(EquipSwapFx.Instance.LastSounds.Contains("equipDrop")); }
            else Assert.IsTrue(EquipSwapFx.Instance.LastSounds.Contains("equipDrop"));
            PlayerInfoPopup.Close(h);
            yield return WaitMs(EquipSwapFx.Spec.FlyMs + 300);
            Assert.AreEqual(0, EquipSwapFx.Instance.Layer.childCount);
        }
            static IEnumerator WaitCraftPopup(ForgeHost h)
        {
            float t = 0f;
            while (!h.Meta.Popups.IsOpen(ForgeCraftPopup.Name) && t < 8f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "망치질 + 리빌 뒤 비교 팝업이 떠야 한다");
            yield return null;
        }

        /// <summary>T118 2회차 — 실장착: 빈 부위 첫 장착은 연출 0(«교체» 가 아니다) · 그 부위를 채운 뒤 같은 부위를 장착하면 옛 타일이 날고 소리 셋이 정본 순서 · 비교 팝업이 열린 채라 착지는 카드 밖.</summary>
        [UnityTest]
        public IEnumerator 실장착_빈_부위_첫_장착은_연출_0_이고_교체는_옛_타일이_날며_딸깍은_연출_안에서만_운다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            h.S.Hammers = 20; h.Pull();
            h.OnCraft();
            yield return WaitCraftPopup(h);
            ForgeItem item = h.Pending;
            Assert.IsNotNull(item);
            string slot = item.Slot;
            // 그 부위가 비어 있으면 같은 부위의 장비를 하나 굴려 먼저 끼운다(Core 굴림 · 해머 소모 없음) — 교체가 «반드시» 나게
            if (h.Gear.Get(slot) == null)
            {
                // ⓐ 먼저 «빈 부위 첫 장착 = 연출 0» 을 본다: 이 대기품을 그대로 끼운다
                h.ResolveCraft("equip");
                yield return null;
                Assert.AreSame(item, h.Gear.Get(slot), "빈 부위에 끼워졌다");
                Assert.IsTrue(EquipSwapFx.Instance == null || EquipSwapFx.Instance.PlayCount == 0, "빈 부위 첫 장착은 교체가 아니라 연출 0(정본 3898 주석)");
                Assert.IsFalse(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "빈 부위면 팝업이 닫힌다");
                // ⓑ 같은 부위가 나올 때까지 굴린 장비를 대기품으로 세운다(Craft 는 부위가 랜덤이라 굴림으로 부위를 맞춘다)
                ForgeItem again = null;
                for (int i = 0; i < 400 && again == null; i++) { ForgeItem r = h.Engine.RollItem(); if (r != null && r.Slot == slot) again = r; }
                Assert.IsNotNull(again, "400번 안에 같은 부위(" + slot + ")가 굴려져야 한다");
                h.SetPendingCraft(again);
                h.ShowCraftModal(again);
                yield return null;
                item = again;
            }
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "비교 팝업이 열린 채 [장착]");
            ForgeItem prev = h.Gear.Get(slot);
            Assert.IsNotNull(prev, "이제 그 부위에 옛 장비가 있다");
            int before = EquipSwapFx.Instance != null ? EquipSwapFx.Instance.PlayCount : 0;
            h.ResolveCraft("equip");
            yield return null;
            Assert.AreSame(item, h.Gear.Get(slot), "새 장비가 끼워졌다");
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "옛 장비가 있으면 두 카드가 맞바뀐 채 열려 있다");
            EquipSwapFx fx = EquipSwapFx.Instance;
            Assert.IsNotNull(fx, "교체면 연출 층이 선다");
            Assert.AreEqual(before + 1, fx.PlayCount, "교체 한 번 = 연출 한 번");
            Assert.IsTrue(fx.LastGrab.HasBlock, "비교 팝업 카드가 열린 채 스왑 — 착지는 카드 옆 빈 띠(정본 주 경로)");
            Assert.AreEqual(1, fx.Flying, "옛 타일 복제가 날고 있다");
            // 런 292 실측: 러너에서 장착 프레임(팝업·시트 재렌더)이 130ms 를 넘겨 이 자리에 딸깍이 이미 울려 있었다(Count 2) — 개수가 아니라 **순서**를 본다
            Assert.GreaterOrEqual(fx.LastSounds.Count, 1);
            Assert.AreEqual("equipToss", fx.LastSounds[0], "던질 때 equipToss 가 첫 소리");
            if (fx.LastSounds.Count > 1) Assert.AreEqual("equipSnap", fx.LastSounds[1], "둘째는 딸깍");
            Assert.LessOrEqual(fx.LastSounds.Count, 2, "착지음(522ms)은 아직 아니다");
            RectTransform live = EquipSwapFx.CellOf(slot);
            Assert.IsNotNull(live, "다시 그려진 칸");
            yield return null;   // Rehollow — 다시 그려진 새 칸이 빈 소켓
            int hidden = 0;
            foreach (Graphic g in live.GetComponentsInChildren<Graphic>(true)) if (g.transform != live && !g.enabled) hidden++;
            bool snapDone = fx.Snaps == 0 && fx.LastSounds.Contains("equipSnap");   // 느린 러너에서 딸깍(130+340ms)이 벌써 끝났으면 칸은 이미 되살아났다
            if (!snapDone) Assert.Greater(hidden, 0, "새 칸은 딸깍 전까지 빈 소켓(내용물 감춤)");
            EquipSwapSpec s = EquipSwapFx.Spec;
            yield return WaitMs(EquipSwapRules.LandMs(s) + 120);
            Assert.AreEqual("equipSnap", fx.LastSounds[1], "딸깍은 연출 안 130ms — ForgeHost 가 바로 울리던 줄은 뺐다(결정 296)");
            EquipSwapPlan p = fx.LastPlan;
            bool clear = p.LandX + p.Reach <= fx.LastGrab.BlockL + 1e-6 || p.LandX - p.Reach >= fx.LastGrab.BlockR - 1e-6;
            Assert.IsTrue(!p.Lands || clear, "눕는 자리는 카드 밖");
            if (p.Lands) Assert.AreEqual("equipDrop", fx.LastSounds[2], "착지음 522ms");
            yield return WaitMs(EquipSwapRules.LandMs(s) + s.DustRemoveMs + 400);
            Assert.AreEqual(0, fx.Layer.childCount, "층은 비었다");
            int hiddenAfter = 0;
            foreach (Graphic g in live.GetComponentsInChildren<Graphic>(true)) if (g.transform != live && !g.enabled) hiddenAfter++;
            Assert.AreEqual(0, hiddenAfter, "칸의 내용물이 되살아났다");
        }
    }
}
