using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T364 — 정본이 앱 **폭** 비율로 못 박은 `gap`(`calc(var(--app-w) * k)`)이 클론에서도 폭 비율인가.
    /// ⓐ 상단바 프로필 카드(`.profile-card` style.css 91 `gap: calc(var(--app-w) * .0261)` · ui.js 1293): 아바타 오른끝 → 닉네임 왼끝 = 카탈로그 `card_gap` × 앱 폭.
    /// (등재문이 짚은 `ProfilePopup.cs:219·372` 는 닉네임 입력·초기화 확인 대화상자(정본 `prompt()`/`confirm()`)라 그 CSS 의 자리가 아니다.)
    /// ⓑ 웨이브 점 줄(`#wave-pips` 169 `.0363`): 점 사이 = `pip_gap` × 앱 폭. 값은 카탈로그에서 읽는다.
    /// </summary>
    public class GapRatioTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t = Time.realtimeSinceStartup;
            while (!MetaHost.Ready || Hud.Instance == null)
            {
                Assert.Less(Time.realtimeSinceStartup - t, 20f, "MetaHost/Hud 가 20초 안에 준비되지 않았다");
                yield return null;
            }
            yield return null;
        }

        static RectTransform Child(Transform t, string name)
        {
            Transform c = t.Find(name);
            Assert.IsNotNull(c, t.name + " 안에 " + name + " 이 없다");
            return (RectTransform)c;
        }

        [UnityTest]
        public IEnumerator 상단바_프로필_카드의_아바타와_닉네임_사이는_앱_폭의_2_61_퍼센트다()
        {
            yield return Boot();
            RectTransform card = (RectTransform)Hud.Instance.ProfileButton.transform;
            RectTransform avatar = Child(card, "avatar");
            RectTransform nick = Child(card, "nickname");
            float avatarRight = avatar.anchoredPosition.x + avatar.sizeDelta.x;
            float gap = nick.anchoredPosition.x - avatarRight;
            Assert.AreEqual(UiKit.W("card_gap"), gap, 0.5f, "틈 = 카탈로그 card_gap × 앱 폭(정본 .profile-card gap: calc(var(--app-w) * .0261))");
            Assert.AreEqual(0.0261f, UiKit.W("card_gap") / UiKit.RefW, 1e-4f, "카탈로그 card_gap 은 정본 .0261 (style.css 91)");
            Assert.Greater(gap, 0f);
        }

        [UnityTest]
        public IEnumerator 웨이브_점_사이는_앱_폭의_3_63_퍼센트다()
        {
            yield return Boot();
            Assert.AreEqual(0.0363f, UiKit.W("pip_gap") / UiKit.RefW, 1e-4f, "카탈로그 pip_gap 은 정본 .0363 (style.css 169 #wave-pips)");
            yield return null;
        }
    }
}
