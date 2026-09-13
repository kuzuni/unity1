using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T118 — 장비 교체 «던져내기»(정본 `equip-swap-throwout`). 1회차는 연출을 직접 부른다(`ForgeHost` 의 Grab/Play 두 줄은 T87 lock 뒤):
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
            Assert.IsNotNull(fly.Find("eqsw-shadow"), "공중에 뜬 물건의 드롭섀도");
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
    }
}
