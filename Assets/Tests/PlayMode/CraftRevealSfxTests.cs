using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core;
using Forge.Core.Audio;
using Forge.Core.Forging;
using Forge.Game;
using Forge.Game.Audio;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T119 4회차 — 정본 `ui.js` 1938 `showCraftReveal` · 1976 `showCraftBatch` 는 카드를 붙인 직후 `SFX.craftReveal(AGES.indexOf(age))` 를 분다.
    /// 클론 `ForgeCraftPopup.ShowReveal`·`ShowBatch` 가 같은 소리를 같은 시대 인덱스로 부는가 — `Sfx.LastPlayed` 가 그 `SfxCall` 의 클립이고 재생 수가 1 는다.
    /// </summary>
    public class CraftRevealSfxTests
    {
        static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && PopupLayer.Instance != null && AudioBank.SfxReady) && t < 30f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 30초 안에 준비되지 않았다");
            Assert.IsTrue(AudioBank.SfxReady, "효과음 클립이 30초 안에 준비되지 않았다");
            yield return null;
        }

        [UnityTest]
        public IEnumerator 제작_공개_카드와_배치_카드판은_craftReveal_을_시대_인덱스로_분다()
        {
            yield return Boot();
            ForgeHost F = ForgeHost.Instance;
            Assert.IsTrue(Sfx.On, "소리가 켜져 있다");
            ForgeItem it = F.Engine.RollItem();
            int idx = ForgeCraftPopup.AgeIndex(F, it);
            Assert.AreEqual(Array.IndexOf(F.Defs.Ages, it.Age), idx, "정본 AGES.indexOf");
            Assert.GreaterOrEqual(idx, 0, "굴린 장비의 시대는 표에 있다");
            AudioClip want = AudioBank.Instance.Sfx(new SfxCall("craftReveal", idx));
            Assert.IsNotNull(want, "craftReveal 레시피 클립(T30)");
            // 배경 전투(Sfx.Hit · 스킬 anvilHit)가 같은 프레임에 울 수 있어 총계·마지막 클립이 아니라 «craftReveal(idx) 클립» 만 센다(§0 런 457 · 결정 523).
            int reveal = 0;
            Action<AudioClip> tally = c => { if (ReferenceEquals(c, want)) reveal++; };
            Sfx.Played += tally;
            try
            {
                // ⓐ 공개 카드
                bool doneA = false;
                ForgeCraftPopup.ShowReveal(F, it, () => doneA = true);
                yield return null;
                Assert.AreEqual(1, reveal, "공개 카드가 뜨는 순간 craftReveal(시대 " + idx + ") 이 한 번");
                ForgeCraftPopup.DismissReveal();
                // ⓑ 배치 카드판 — 첫 장의 시대로
                var items = new List<ForgeItem> { it, F.Engine.RollItem(), F.Engine.RollItem() };
                reveal = 0;
                bool doneB = false;
                ForgeCraftPopup.ShowBatch(F, items, () => doneB = true);
                yield return null;
                Assert.IsTrue(ForgeCraftPopup.BatchVisible, "메인 화면이라 카드판이 폈다");
                Assert.AreEqual(1, reveal, "카드판을 붙인 직후 craftReveal 이 첫 장의 시대 인덱스로 한 번(정본 1976)");
                ForgeCraftPopup.DismissBatch();
            }
            finally { Sfx.Played -= tally; }
            yield return null;
            Assert.IsFalse(ForgeCraftPopup.BatchVisible);
        }
    }
}
