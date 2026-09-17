using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T439 — 정본 `style.css` 4647 `.rates-i { font-weight: 900; font-style: italic; font-family: Georgia, serif }` ·
    /// `ui.js` 4384 `&lt;button class="rates-i"&gt;i&lt;/button&gt;` — 검정 원판 위의 «i» 는 **기울어진** 글자다(정보 아이콘의 관용 꼴).
    /// 클론은 굵기만 있고 기울임이 0이었다. 여기서는 그 글자의 `fontStyle` 에 **Italic 비트가 서고 Bold 가 안 지워졌는지**를 잰다.
    /// 세리프(Georgia)는 이 레포에 글꼴이 없어 못 낸다(§1 · T53) — 그래서 «기울임» 까지가 이 자리의 몫이고 자도 그것만 묻는다.
    /// </summary>
    public class RatesInfoItalicTests
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
        public IEnumerator 확률_정보_i_는_정본처럼_기울어지고_굵기는_그대로다()
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

            // T414 — 앱 뿌리에서 이름으로 찾지 않는다. 이 팝업의 내용 상자 아래에서만 찾는다.
            Transform ib = m.Content.Find("rates-i");
            Assert.IsNotNull(ib, "«i» 버튼(rates-i)");
            TextMeshProUGUI it = ib.Find("t") != null ? ib.Find("t").GetComponent<TextMeshProUGUI>() : null;
            Assert.IsNotNull(it, "«i» 버튼의 글자(t)");
            Assert.AreEqual("i", it.text, "정본 ui.js 4384 가 넣는 글자는 «i» 하나다");

            Assert.AreNotEqual(0, (int)(it.fontStyle & FontStyles.Italic),
                "정본 4647 `font-style: italic` — 기울임 비트가 서 있다");
            Assert.AreNotEqual(0, (int)(it.fontStyle & FontStyles.Bold),
                "정본 4647 `font-weight: 900` — 기울임을 더하면서 굵기를 지우지 않았다(`|=` 라야 한다)");
            yield return null;
        }
    }
}
