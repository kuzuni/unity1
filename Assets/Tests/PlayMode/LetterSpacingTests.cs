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
    /// T168 2회차 — 정본이 벌려 놓은 글자가 클론에서도 벌어지는가.
    ///
    /// 자리 대조 자체는 `tools/check_letter_spacing.py` 가 본다(정본 선언 ↔ 표 키). 여기서는 **실물**을 잰다:
    /// ⓐ 환산이 한 군데뿐인가(em ↔ TMP 1/100 em) ⓑ 보스 경고·사망 배너의 글자가 표대로인가
    /// ⓒ 자간이 마지막 글자 뒤에도 붙어 가운데가 쏠리는 것을 **되민** 자리(정본 `text-indent`·`padding-left`)가 실제로 되밀렸는가.
    /// </summary>
    public class LetterSpacingTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(UiRoot.Instance != null && UiRoot.Instance.App != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(UiRoot.Instance, "UiRoot 가 20초 안에 안 섰다");
            yield return null;
        }

        [UnityTest]
        public IEnumerator 표의_em_이_TMP_단위로_한_번만_환산된다()
        {
            yield return Boot();

            Assert.AreEqual(0.55f, LetterSpacing.Em("bw_sub_ls_em"), 1e-5f, "정본 style.css 411 `.bw-sub { letter-spacing: .55em }`");
            Assert.AreEqual(0.14f, LetterSpacing.Em("bw_track_ls_em"), 1e-5f, "정본 402 `.bw-track span`");
            Assert.AreEqual(0.32f, LetterSpacing.Em("death_title_ls_em"), 1e-5f, "정본 scene3d.js 17417 인라인 `.32em`");
            Assert.AreEqual(0.06f, LetterSpacing.Em("death_sub_ls_em"), 1e-5f, "정본 scene3d.js 17423 인라인 `.06em`");
            // TMP 는 1/100 em — 이 환산이 호출부로 흩어지면 «×100 을 빠뜨린 자리» 가 조용히 생긴다(자간이 100분의 1 = 사실상 0).
            Assert.AreEqual(55f, LetterSpacing.Tmp("bw_sub_ls_em"), 1e-4f, "TMP characterSpacing 은 1/100 em");
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => LetterSpacing.Em("없는_키_ls_em"), "표에 없는 키는 조용히 0 이 아니라 던진다");
        }

        /// <summary>3회차에 붙인 자리들의 값이 정본 그대로인가(부호까지 — 음수는 «좁힌다» 는 뜻이다).</summary>
        [UnityTest]
        public IEnumerator 표가_정본_자간을_부호까지_그대로_쥔다()
        {
            yield return Boot();

            Assert.AreEqual(0.07f, LetterSpacing.Em("sr_title_ls_em"), 1e-5f, "style.css 6207 .sr-title");
            Assert.AreEqual(0.02f, LetterSpacing.Em("sr_sub_ls_em"), 1e-5f, "7059 .sr-sub");
            Assert.AreEqual(0.04f, LetterSpacing.Em("sr_new_ls_em"), 1e-5f, "6980 .sr-new");
            Assert.AreEqual(0.04f, LetterSpacing.Em("sr_again_ls_em"), 1e-5f, "5791 .sr-again");
            Assert.AreEqual(0.06f, LetterSpacing.Em("sr_ok_ls_em"), 1e-5f, "7139 .sr-ok");
            Assert.AreEqual(0.06f, LetterSpacing.Em("dgclear_title_ls_em"), 1e-5f, "5375 .dgclear-title");
            // 음수 둘 — 부호를 잃으면 «넓힌다» 가 되어 정본과 반대로 간다
            Assert.AreEqual(-0.01f, LetterSpacing.Em("substat_row_ls_em"), 1e-5f, "3729 .substat-row 는 **음수**(좁힌다)");
            Assert.AreEqual(-0.06f, LetterSpacing.Em("chat_placeholder_ls_em"), 1e-5f, "3460 입력칸 안내 글자도 음수");
            Assert.Less(LetterSpacing.Tmp("chat_placeholder_ls_em"), 0f, "TMP 단위로 바뀌어도 음수 그대로");
        }

        [UnityTest]
        public IEnumerator 보스_경고와_사망_배너의_글자가_표대로_벌어진다()
        {
            yield return Boot();
            BattleOverlay o = BattleOverlay.Ensure();
            Assert.IsNotNull(o);

            o.BossWarning(2.0);
            yield return null;
            TextMeshProUGUI sub = Find(o.Layer, "bw-sub");
            TextMeshProUGUI mq = Find(o.Layer, "bw-track");
            Assert.IsNotNull(sub, "보스 경고 한글 부제");
            Assert.AreEqual(LetterSpacing.Tmp("bw_sub_ls_em"), sub.characterSpacing, 1e-3f, "정본 .55em — 한 글자씩 벌어지는 줄이다");
            Assert.Greater(sub.margin.x, 0f, "정본 `text-indent: .55em` — 마지막 글자 뒤 여백을 되민다");
            Assert.AreEqual(LetterSpacing.Em("bw_sub_indent_em") * sub.fontSize, sub.margin.x, 1e-2f, "되민 몫 = 표의 em × 글자 크기");
            if (mq != null) Assert.AreEqual(LetterSpacing.Tmp("bw_track_ls_em"), mq.characterSpacing, 1e-3f, "정본 .14em(영문 마퀴)");

            Assert.IsTrue(o.DeathFade("회복 후 다시 도전합니다"));
            yield return null;
            TextMeshProUGUI title = Find(o.Layer, "title");
            TextMeshProUGUI dsub = Find(o.Layer, "sub");
            Assert.IsNotNull(title, "사망 배너 제목");
            Assert.AreEqual(LetterSpacing.Tmp("death_title_ls_em"), title.characterSpacing, 1e-3f, "정본 scene3d.js 17417 `.32em`(여태 코드에 박힌 32f 였다)");
            Assert.Greater(title.margin.x, 0f, "정본 같은 줄의 `padding-left: .32em`");
            if (dsub != null) Assert.AreEqual(LetterSpacing.Tmp("death_sub_ls_em"), dsub.characterSpacing, 1e-3f, "정본 17423 `.06em`");
        }

        /// <summary>T168 4회차 — 장비 칸 Lv 배지(정본 style.css 936~946 `.equip-cell .cell-lv { letter-spacing: .05em }`)가 표대로 벌어진다.
        /// 배지 공장(`ForgeUi.LvBadge`)을 직접 세워 잰다 — 어느 시트를 열든 같은 공장이라 실물과 같다.</summary>
        [UnityTest]
        public IEnumerator 장비_칸_Lv_배지의_글자가_표대로_벌어진다()
        {
            yield return Boot();
            RectTransform tile = UiKit.Box(UiRoot.Instance.App, "t168-tile");
            try
            {
                TextMeshProUGUI lv = ForgeUi.LvBadge(tile, 12, 100f);
                yield return null;
                Assert.IsNotNull(lv, "Lv 배지");
                Assert.Greater(LetterSpacing.Em("equip_cell_lv_ls_em"), 0f, "정본 .05em — 양수(벌린다)");
                Assert.AreEqual(0.05f, LetterSpacing.Em("equip_cell_lv_ls_em"), 1e-4f, "표가 정본 값 그대로");
                Assert.AreEqual(LetterSpacing.Tmp("equip_cell_lv_ls_em"), lv.characterSpacing, 1e-3f, "배지 글자가 표대로 벌어진다(TMP 1/100 em 환산은 한 군데)");
            }
            finally { Object.Destroy(tile.gameObject); }
        }

        /// <summary>그 이름의 가지에서 첫 TMP 글자를 찾는다(그 자신이 글자면 그것).</summary>
        private static TextMeshProUGUI Find(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name)
            {
                TextMeshProUGUI self = root.GetComponent<TextMeshProUGUI>();
                return self != null ? self : root.GetComponentInChildren<TextMeshProUGUI>(true);
            }
            for (int i = 0; i < root.childCount; i++)
            {
                TextMeshProUGUI hit = Find(root.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }
    }
}
