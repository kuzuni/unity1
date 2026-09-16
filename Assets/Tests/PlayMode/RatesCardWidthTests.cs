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
    /// T410 — 정본 `style.css` 4575 `.rates-card { width: 74.35% }` 는 **흰 면**(원본 shot-042521 실측 371/499 · 검정 테는 그 바깥)이다.
    /// 클론은 그 분수를 테까지 품은 바깥 상자에 먹여 흰 면이 좌우 테 두께씩(합 1.39%p) 좁았다(런 827). 여기서는 카드 «face» 의 폭이 표 `rates_w_f` × 앱 폭이고
    /// 카드 가운데가 앱 가운데(50%W)에 그대로 있는지 잰다. 눈 확인은 `screen_summon-rates.png` 의 흰 면 74.35%W ± .2%p.
    /// </summary>
    public class RatesCardWidthTests
    {
        static IEnumerator Boot()
        {
            PetSkillHost.SuppressSave = true;
            PetSkillHost.Seed = 20260912;
            SceneManager.LoadScene("SampleScene");
            yield return null; yield return null;
            Scene active = SceneManager.GetActiveScene();
            for (int i = 0; i < 600 && !(SkillPetSheet.Instance != null && SkillPetSheet.Instance.gameObject.scene == active && PetSkillHost.Ready && SkillBar.Instance != null); i++) yield return null;
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 서지 않았다");
            Assert.IsTrue(PetSkillHost.Ready);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 확률_카드의_흰_면은_정본_74_35퍼센트W_이고_테는_그_바깥이다()
        {
            yield return Boot();
            SkillPetSheet sheet = SkillPetSheet.Instance;
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
            yield return null;
            SkillRatesPopup.Open(sheet, "skill");
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.IsTrue(sheet.Modal.IsOpen(SkillRatesPopup.ModalName), "확률 팝업이 열렸다");
            PetSkillModal.Handle m = sheet.Modal.Find(SkillRatesPopup.ModalName);
            Assert.IsNotNull(m, "모달 손잡이");
            RectTransform face = (RectTransform)m.Card.Find("face");
            Assert.IsNotNull(face, "카드 흰 면(face)");
            float want = PetSkillStyle.L("rates_w_f") * UiKit.RefW;
            float line = PetSkillKit.Line3;
            Assert.AreEqual(want, face.rect.width, 1f, "흰 면 폭 = rates_w_f × 앱 폭(정본 4575 · 원본 371/499) — 테는 그 바깥");
            Assert.AreEqual(want + line * 2f, m.Card.rect.width, 1f, "바깥 상자 = 흰 면 + 테 두 겹(line3)");
            Assert.AreEqual(want, m.Content.rect.width, 1f, "내용 상자는 흰 면에 맞춘다(x 셈은 흰 면 기준)");
            // 가운데(50%W)는 안 움직인다 — 카드와 앱 상자의 가로 가운데를 월드 좌표로 견준다
            Vector3[] cc = new Vector3[4], ac = new Vector3[4];
            m.Card.GetWorldCorners(cc); UiRoot.Instance.App.GetWorldCorners(ac);
            float cardMid = (cc[0].x + cc[3].x) * 0.5f, appMid = (ac[0].x + ac[3].x) * 0.5f, appW = ac[3].x - ac[0].x;
            Assert.AreEqual(appMid, cardMid, appW * 0.002f, "카드 가운데 = 앱 가운데(50%W · ±0.2%W)");
            // 테는 바깥 — face 의 왼끝이 카드 왼끝에서 line 만큼 안이다
            Vector3[] fc = new Vector3[4]; face.GetWorldCorners(fc);
            float scale = appW / UiKit.RefW;
            Assert.AreEqual(line * scale, fc[0].x - cc[0].x, 0.5f * scale + 0.01f, "흰 면 왼끝 = 카드 왼끝 + 테 두께");
            sheet.Modal.Close(SkillRatesPopup.ModalName);
            yield return null;
        }
    }
}
