using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Data;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T157 — 부팅 끝자락의 두 호출(`RestorePendingCraft` · `StartAutoSeq`)이 정본 `main.js` 119~123 처럼 격리돼 있는가:
    /// ⓐ 격리 자체 — 던져도 경고 한 줄만 남기고 false(부팅은 계속) ⓑ 손상 대기품 세이브로 부팅해도 자동 제련 시퀀스가 선다(안쪽 가드 + 바깥 격리 «둘 다»).
    /// </summary>
    public class BootGuardTests
    {
        static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            yield return null;
        }

        static void DeleteSave() { try { if (File.Exists(SaveIo.SavePath)) File.Delete(SaveIo.SavePath); } catch (Exception) { } }

        [UnityTest]
        public IEnumerator 격리는_던져도_경고_한_줄만_남기고_부팅을_잇는다()
        {
            DeleteSave();
            yield return Boot();
            int before = ForgeHost.BootGuardTrips;
            LogAssert.Expect(LogType.Warning, new Regex("Junk\\(\\) 실패 — 나머지 부팅은 계속한다"));
            bool ok = ForgeHost.BootGuard("Junk", () => { throw new InvalidOperationException("boom"); });
            Assert.IsFalse(ok, "던진 단계는 false");
            Assert.AreEqual(before + 1, ForgeHost.BootGuardTrips, "삼킨 횟수 +1");
            int ran = 0;
            Assert.IsTrue(ForgeHost.BootGuard("Fine", () => { ran++; }), "안 던지면 true");
            Assert.AreEqual(1, ran);
            Assert.AreEqual(before + 1, ForgeHost.BootGuardTrips, "안 던진 단계는 안 센다");
            yield return null;
        }

        [UnityTest]
        public IEnumerator 손상_대기품_세이브로_부팅해도_자동_제련_시퀀스가_선다()
        {
            DeleteSave();
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            // 세이브에 «켜진 자동 제련 + 해금 + 망치» 와 최소 형태가 아닌 대기품(시대·희귀도·subs 없음)을 심고 파일로 쓴다
            h.S.BestChapter = 3; h.S.BestStage = 1;
            h.S.Hammers = 50;
            h.S.AutoForgeOn = true;
            JsonObject junk = new JsonObject();
            junk["name"] = "junk"; junk["slot"] = h.Defs.Slots[0]; junk["level"] = 1.0;
            h.S.Root[ForgeSave.KeyPending] = junk;
            File.WriteAllText(SaveIo.SavePath, MiniJson.Serialize(h.S.Root));

            // 다시 부팅 — 안쪽 가드가 손상 대기품을 버리며 LogError 를 내지만(정본도 console.error) 바깥 격리 덕에 StartAutoSeq 까지 간다
            LogAssert.Expect(LogType.Error, new Regex("restorePendingCraft: 제작물의 최소 형태가 아닌 대기품을 버렸다"));
            yield return Boot();
            h = ForgeHost.Instance;
            Assert.IsTrue(SaveIo.LoadedFromDisk, "심어 둔 세이브를 읽었다");
            Assert.IsNull(h.Pending, "손상 대기품은 버려졌다");
            Assert.IsTrue(h.AutoOn, "자동 제련 켜짐이 세이브에서 왔다");
            Assert.IsTrue(h.AutoForgeUnlocked, "해금 상태");
            Assert.IsTrue(h.AutoSeqRunning, "정본 main.js 123: 자동 제련 시퀀스가 이어서 선다 — 앞 줄이 던져도 여기까지 온다");
            StringAssert.Contains("손상된 제작 대기품", h.Meta.Popups.LastToast ?? "", "정본 토스트");

            h.StopAutoSeq();
            yield return null;
            DeleteSave();
        }
    }
}
