using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Audio;
using Forge.Game.Audio;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T30 — 부팅 씬에서 소리 은행이 서고(오디오 파일 0 · 백그라운드 렌더), 효과음 24종 전부 `Play` 되고, 음악 4모드가 코드 경계 규칙으로 갈아타며 콘솔 빨강이 0 인가.
    /// 빨간 로그는 러너가 실패시킨다.
    /// </summary>
    public class AudioSmokeTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
        }

        private static IEnumerator WaitUntil(System.Func<bool> cond, float seconds, string what)
        {
            float t0 = Time.realtimeSinceStartup;
            while (!cond())
            {
                if (Time.realtimeSinceStartup - t0 > seconds) Assert.Fail(what + " — " + seconds + "초 안에 안 됐다");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator 효과음_24종_전부_재생_음악_4모드_전환_콘솔_빨강_0()
        {
            yield return Boot();

            AudioBank bank = AudioBank.Instance;
            Assert.IsNotNull(bank, "AudioBank 가 Bootstrap 아래에 서지 않았다");
            Assert.IsNotNull(Sfx.Instance);
            Assert.IsNotNull(Music.Instance);

            yield return WaitUntil(() => AudioBank.Loaded, 30f, "sfx.json·gamedata.json 로드");
            Assert.AreEqual(4, AudioBank.Table.Modes.Count);
            Assert.AreEqual(6, AudioBank.Rarities.Length);

            yield return WaitUntil(() => AudioBank.SfxReady, 120f, "효과음 기본 변형 렌더");
            Assert.GreaterOrEqual(bank.SfxClipCount, SfxRecipes.DefaultCalls(AudioBank.Rarities).Count, "기본 변형 테이크 0 이 전부 클립");

            int before = Sfx.PlayedCount;
            foreach (string name in SfxRecipes.Names)
            {
                bool ok = Sfx.Play(new SfxCall(name, 0, 0, "common"));
                Assert.IsTrue(ok, name + " 재생");
                Assert.IsNotNull(Sfx.LastPlayed);
                Assert.Greater(Sfx.LastPlayed.length, 0f, name + " 길이");
                Assert.AreEqual(1, Sfx.LastPlayed.channels);
            }
            Assert.AreEqual(before + SfxRecipes.Names.Length, Sfx.PlayedCount, "24종 전부 났다");
            Sfx.Hit(true); Sfx.AnvilHit(true); Sfx.StormStrike(3); Sfx.SlashArc(2, 5); Sfx.ArrowShot(7, 9);
            Sfx.CraftReveal(9); Sfx.Gacha("mythic"); Sfx.SummonCharge("legendary"); Sfx.SummonReveal("ultimate");
            Assert.AreEqual(before + SfxRecipes.Names.Length + 9, Sfx.PlayedCount, "표면 메서드(인자 클램프 · 기본 변형 밖은 같은 이름의 구워진 변형으로)도 난다");
            string lazyKey = new SfxCall("slashArc", 2, 5).Key;
            Assert.IsFalse(AudioBank.AllReady, "기본 변형 밖 호출은 정확한 변형을 백그라운드에 요청한다");
            yield return new WaitForSecondsRealtime(0.3f);

            Music music = Music.Instance;
            music.StartMusic();
            yield return WaitUntil(() => music.IsPlaying, 120f, "normal 루프 렌더 후 재생");
            Assert.AreEqual("normal", music.MusicMode);
            Assert.IsTrue(music.Running);
            AudioClip loop = bank.MusicClip("normal");
            float expectSec = (float)(AudioBank.Table.LoopSteps * AudioBank.Table.Mode("normal").StepDur);
            Assert.AreEqual(expectSec, loop.length, 0.01f, "8마디 루프 길이");

            yield return WaitUntil(() => AudioBank.AllReady, 180f, "음악 4모드 · 테이크 2 · 뒤늦게 요청한 변형 렌더");
            Assert.IsTrue(bank.HasSfx(lazyKey), "요청해 둔 slashArc(2, 5) 가 구워졌다");
            foreach (string mode in new[] { "boss", "dungeon", "shop" })
            {
                music.SetMusicMode(mode);
                Assert.AreEqual(mode, music.CombatMode);
                music.SwitchNow();
                Assert.AreEqual(mode, music.MusicMode, mode + " 로 코드 경계 전환");
                Assert.IsTrue(music.IsPlaying);
                yield return null;
            }
            Music.ShopOpen = () => true;
            music.SetMusicMode("normal");
            music.SwitchNow();
            Assert.AreEqual("shop", music.MusicMode, "상점 오버레이 > Combat 모드");
            Music.ShopOpen = null;
            music.SwitchNow();
            Assert.AreEqual("normal", music.MusicMode, "오버레이가 벗겨지면 Combat 모드로 복귀");
            music.SetMusicMode("nope");
            Assert.AreEqual("normal", music.CombatMode, "모르는 모드는 normal");

            bool on = music.ToggleMusic();
            Assert.IsFalse(on);
            Assert.IsFalse(music.Running, "토글 끔 → 멈춤");
            on = music.ToggleMusic();
            Assert.IsTrue(on);
            Assert.IsTrue(music.Running, "토글 켬 → 다시");
            music.StopMusic();
            Assert.IsFalse(music.IsPlaying);
        }
    }
}
