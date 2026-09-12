using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T56 — 상단바 프로필 카드의 도트 초상(원작 `ui.js renderTopBar` 의 `IconGen.avatar(S.avatarEmoji)`).
    /// 실측(2026-09-12 `screens/screen_main.png`): 프로필 팝업·리그·채팅에는 초상이 그려지는데 HUD 만 빈 흰 타일이었다 —
    /// «오브젝트가 섰는가» 만 보는 테스트로는 안 잡히는 자리라 **스프라이트가 실제로 걸렸는가** 를 단언한다.
    /// </summary>
    public class HudAvatarTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && Hud.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 준비되지 않았다");
            Assert.IsNotNull(Hud.Instance, "HUD 가 서지 않았다");
            yield return null;
        }

        [UnityTest]
        public IEnumerator 부팅하면_상단바에_기본_아바타_초상이_걸린다()
        {
            yield return Boot();

            MetaHost h = MetaHost.Instance;
            Sprite want = UiIcons.Avatar(h.AvatarEmoji);
            Assert.IsNotNull(want, "T31 아틀라스에 기본 아바타(" + h.AvatarEmoji + ") 초상이 있어야 한다");
            Assert.AreSame(want, Hud.Instance.AvatarPortrait,
                "상단바 프로필 카드에 IconGen.avatar(S.avatarEmoji) 초상이 걸려야 한다 (빈 흰 타일이면 원작 renderTopBar 와 다르다)");
        }

        [UnityTest]
        public IEnumerator 아바타를_바꿔_동기화하면_상단바_초상도_따라_바뀐다()
        {
            yield return Boot();

            MetaHost h = MetaHost.Instance;
            string first = h.AvatarEmoji;
            string other = null;
            foreach (string e in h.Meta.Avatars.Pool)
            {
                if (e != first && UiIcons.Avatar(e) != null) { other = e; break; }
            }
            Assert.IsNotNull(other, "아바타 풀에 기본값 말고 초상이 있는 아바타가 하나는 있어야 한다");

            h.S["avatarEmoji"] = other;   // 원작 onPickAvatar 와 같은 경로(Touch → SyncHud)
            h.Touch();
            yield return null;

            Assert.AreSame(UiIcons.Avatar(other), Hud.Instance.AvatarPortrait,
                "원작은 아바타를 고르면 renderTopBar 를 다시 불러 상단바가 따라 바뀐다 (ui.js onPickAvatar)");
        }
    }
}
