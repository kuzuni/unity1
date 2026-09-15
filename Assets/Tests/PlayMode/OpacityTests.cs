using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Data;
using Forge.Core.Forging;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T359 — 정본 정적 `opacity` 표(`OpacityUi.json`)가 승천 팝업 세 자리에 걸리는가: 행 진행 글자 .85 · 준비된 행의 화살 .8(준비 안 된 행엔 없다) · 초점 효과 글줄 .9.
    /// 값은 표에서 읽어 견준다. 자기 파일인 이유: `DungeonUiTests` 는 T24·T25 절의 자리다.
    /// </summary>
    public class OpacityTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t = Time.realtimeSinceStartup;
            while (!DungeonUiHost.Ready)
            {
                Assert.Less(Time.realtimeSinceStartup - t, 20f, "DungeonUiHost 가 20초 안에 Ready 되지 않았다");
                yield return null;
            }
            yield return null;
        }

        static void Collect(Transform t, string name, List<Transform> outList)
        {
            if (t.name == name) outList.Add(t);
            for (int i = 0; i < t.childCount; i++) Collect(t.GetChild(i), name, outList);
        }

        static float Alpha(Transform t)
        {
            CanvasGroup cg = t.GetComponent<CanvasGroup>();
            return cg != null ? cg.alpha : 1f;
        }

        [Test]
        public void 표는_정본_아홉_자리를_정본_값으로_쥔다()
        {
            OpacityUi.Reset();
            Assert.AreEqual(0.8f, OpacityUi.A("asc_row_ready_arrow"), 1e-6f);
            Assert.AreEqual(0.85f, OpacityUi.A("asc_prog"), 1e-6f);
            Assert.AreEqual(0.9f, OpacityUi.A("asc_focus_eff"), 1e-6f);
            Assert.AreEqual(0.5f, OpacityUi.A("pet_tile_mat_locked"), 1e-6f);
            Assert.AreEqual(0.45f, OpacityUi.A("btn_disabled"), 1e-6f);
            Assert.AreEqual(0.7f, OpacityUi.A("cmp_card_empty"), 1e-6f);
            Assert.AreEqual(0.85f, OpacityUi.A("idet_icon_tn_dim"), 1e-6f);
            // T359 3회차 — `ob_zzz` 는 표에서 걷었다: 정본 232 `.ob-zzz i` 는 `opacity: 0` + 애니메이션이고 `.9` 는
            //   242~247 `@media (prefers-reduced-motion: reduce)` 안의 갈래다(클론은 이미 매 프레임 애니메이션을 돈다).
            Assert.Throws<KeyNotFoundException>(() => OpacityUi.A("ob_zzz"), "정적값이 아니라 걷은 키다");
            Assert.AreEqual(0.1f, OpacityUi.A("toasts_rw_dim"), 1e-6f);
            Assert.AreEqual(0.7f, OpacityUi.Rem("asc_arrow", "font_rem"), 1e-6f);
            Assert.AreEqual(0.1f, OpacityUi.Rem("asc_arrow", "ml_rem"), 1e-6f);
            Assert.Throws<KeyNotFoundException>(() => OpacityUi.A("없는_자리"));
        }

        [UnityTest]
        public IEnumerator 승천_팝업의_진행_글자_화살_효과_글줄이_표_알파로_선다()
        {
            yield return Boot();
            AscendPopup.Open();
            yield return null;
            Assert.IsTrue(AscendPopup.IsOpen);
            RectTransform root = AscendPopup.Root;
            Assert.IsNotNull(root);

            var progs = new List<Transform>(); Collect(root, "prog", progs);
            Assert.AreEqual(AscendPopup.RowCount, progs.Count, "행마다 진행 글자 하나");
            foreach (Transform p in progs) Assert.AreEqual(OpacityUi.A("asc_prog"), Alpha(p), 1e-4f, "정본 .asc-prog opacity .85");

            var arrows = new List<Transform>(); Collect(root, "arrow", arrows);
            var hits = new List<Transform>(); Collect(root, "hit", hits);
            int readyRows = 0;
            foreach (Transform h in hits) if (h.parent != null && h.parent.name.StartsWith("row-")) readyRows++;
            Assert.AreEqual(readyRows, arrows.Count, "화살(::after)은 준비된(.ready) 행에만 있다");
            foreach (Transform a in arrows)
            {
                Assert.AreEqual(OpacityUi.A("asc_row_ready_arrow"), Alpha(a), 1e-4f, "정본 .asc-row.ready::after opacity .8");
                TextMeshProUGUI t = a.GetComponent<TextMeshProUGUI>();
                Assert.IsNotNull(t); Assert.AreEqual("▶", t.text);
                Assert.AreEqual(OpacityUi.Rem("asc_arrow", "font_rem") * PopupKit.Rem, t.fontSize, 0.01f, "정본 .7rem");
            }
            var cnts = new List<Transform>(); Collect(root, "cnt", cnts);
            foreach (Transform c in cnts)
            {
                TextMeshProUGUI t = c.GetComponent<TextMeshProUGUI>();
                if (t != null && c.parent != null && c.parent.name.StartsWith("row-")) Assert.IsFalse(t.text.Contains("▶"), "화살은 cnt 글자에 붙이지 않는다(알파가 다르다)");
            }

            AscendPopup.Open("forge");
            yield return null;
            root = AscendPopup.Root;
            var effs = new List<Transform>(); Collect(root, "eff", effs);
            Assert.AreEqual(1, effs.Count, "초점 카드의 효과 글줄 하나");
            Assert.AreEqual(OpacityUi.A("asc_focus_eff"), Alpha(effs[0]), 1e-4f, "정본 .asc-focus-eff opacity .9");
            AscendPopup.Close();
            yield return null;
            Assert.IsFalse(AscendPopup.IsOpen);
        }

        /// <summary>
        /// T359 5회차 — 정본 `style.css` **673** `.btn.disabled { opacity: .45 }`. 공용 버튼 공장(<see cref="PopupKit.Btn"/>)이
        /// 그 `.45` 를 숫자로 박고 있었다(§1). 정본 `opacity` 는 **그 상자 한 겹 전체**(글자·테까지)라 CanvasGroup 한 장이 같은 뜻이다.
        /// 켜진 버튼에는 안 걸리는 것까지 같이 본다 — 정본은 `.disabled` 에만 준다.
        /// </summary>
        [UnityTest]
        public IEnumerator 비활성_버튼만_정본_알파_한_겹을_쓴다()
        {
            yield return Boot();
            RectTransform host = UiKit.Box(UiRoot.Instance.App, "t359-btn-host");
            UiKit.Place(host, 0f, 0f, UiKit.RefW, UiKit.RefH);
            float w = UiKit.RefW * 0.4f, h = UiKit.RefH * 0.05f;

            Button off = PopupKit.Btn(host, "btn-off", "비활성", "pp_paper", "pp_line", null, w, h, "stage_ink", TextKind.Button, true);
            Assert.IsNotNull(off, "비활성 버튼");
            Assert.IsFalse(off.interactable, "비활성은 눌리지 않는다");
            CanvasGroup cg = off.GetComponent<CanvasGroup>();
            Assert.IsNotNull(cg, "정본 .btn.disabled 의 opacity 는 버튼 한 겹에 걸린다");
            Assert.AreEqual(OpacityUi.A("btn_disabled"), cg.alpha, 1e-4f, "정본 673 opacity .45 · 실측 " + cg.alpha);
            Assert.AreEqual(0.45f, OpacityUi.A("btn_disabled"), 1e-6f, "표값이 정본 그대로");

            Button on = PopupKit.Btn(host, "btn-on", "켜짐", "pp_paper", "pp_line", null, w, h);
            Assert.IsTrue(on.interactable, "켜진 버튼");
            CanvasGroup cg2 = on.GetComponent<CanvasGroup>();
            Assert.IsTrue(cg2 == null || Mathf.Approximately(cg2.alpha, 1f), "켜진 버튼은 안 흐려진다(정본은 .disabled 에만)");

            Object.Destroy(host.gameObject);
            yield return null;
        }

        /// <summary>
        /// T359 4회차 — 정본 `style.css` **1830** `.cmp-card.empty { opacity: .7 }`. 정본 `opacity` 는 **그 상자 한 겹 전체**(테·글자까지)라
        /// CanvasGroup 한 장이 같은 뜻이다. 클론은 그 .7 을 얼굴 이미지의 알파에 숫자로 박아 두어 **글자는 안 흐려졌다** — 그것이 이 자가 막는 자리다.
        /// 값은 표에서 읽고, 채워진 카드에는 **안 걸리는 것**까지 같이 본다(정본은 `.empty` 에만 건다).
        /// </summary>
        [UnityTest]
        public IEnumerator 비교_카드의_빈_슬롯만_정본_알파_한_겹을_쓴다()
        {
            yield return Boot();
            float t0 = Time.realtimeSinceStartup;
            while (!ForgeHost.Ready)
            {
                Assert.Less(Time.realtimeSinceStartup - t0, 20f, "ForgeHost 가 20초 안에 준비되지 않았다");
                yield return null;
            }
            GameDefs d = ForgeHost.Instance != null ? ForgeHost.Instance.Defs : null;
            Assert.IsNotNull(d, "GameDefs");
            RectTransform host = UiKit.Box(UiRoot.Instance.App, "t359-cmp-host");
            UiKit.Place(host, 0f, 0f, UiKit.RefW, UiKit.RefH);

            float w = UiKit.RefW * 0.4f;
            RectTransform empty = ForgeUi.ItemCard(host, "cmp-empty", w, null, null, null, false, d, null);
            Assert.IsNotNull(empty, "빈 슬롯 카드");
            CanvasGroup cg = empty.GetComponent<CanvasGroup>();
            Assert.IsNotNull(cg, "정본 .cmp-card.empty 의 opacity 는 카드 한 겹에 걸린다 — CanvasGroup 이 없다(얼굴 알파에 박힌 채다)");
            Assert.AreEqual(OpacityUi.A("cmp_card_empty"), cg.alpha, 1e-4f, "정본 .cmp-card.empty opacity .7 · 실측 " + cg.alpha);

            // 글자도 같이 흐려져야 한다 — 종전 꼴(얼굴 알파만 .7)에서는 이 줄이 통과하지 못한다.
            Transform label = empty.Find("empty");
            Assert.IsNotNull(label, "«빈 슬롯» 글자");
            Assert.AreSame(cg, label.GetComponentInParent<CanvasGroup>(), "글자가 그 한 겹 안에 든다");
            // `PopupKit.Outlined` 은 «상자 face / 안에 line + face(Image)» 로 두 겹이다 — 바깥 `face` 는 민 Box 라 Image 가 없다(런 701 이 여기서 빨갰다).
            Transform faceBox = empty.Find("face");
            Assert.IsNotNull(faceBox, "얼굴 상자");
            Image face = faceBox.Find("face") != null ? faceBox.Find("face").GetComponent<Image>() : null;
            Assert.IsNotNull(face, "얼굴 이미지(PopupKit.Outlined 의 안쪽 face)");
            Assert.AreEqual(1f, face.color.a, 1e-4f, "얼굴 알파에 숫자를 다시 박지 않는다(한 겹은 CanvasGroup 이 쥔다)");

            // 채워진 카드에는 안 건다(정본은 `.empty` 에만). 아이템은 **엔진이 굴린 진짜**를 쓴다 —
            //   손으로 지은 레코드는 `Name`·`Main`·`Subs` 가 비어 채워진 갈래가 그리다 넘어진다(4회차 런 701 이 그렇게 빨갰다).
            ForgeItem it = ForgeHost.Instance.Engine.RollItem();
            Assert.IsNotNull(it, "엔진이 아이템을 굴린다");
            RectTransform full = ForgeUi.ItemCard(host, "cmp-full", w, it, null, null, false, d, ForgeHost.Instance.GearSys.ItemValue);
            Assert.IsNotNull(full, "채워진 카드");
            CanvasGroup cg2 = full.GetComponent<CanvasGroup>();
            Assert.IsTrue(cg2 == null || Mathf.Approximately(cg2.alpha, 1f), "채워진 카드는 안 흐려진다(정본 .cmp-card.empty 에만 건다)");
            Object.Destroy(host.gameObject);
            yield return null;
        }

        /// <summary>
        /// T359 2회차 — 정본 `#toasts.rw-dim`: 수령 연출이 도는 동안 **떠 있던 토스트가 물러난다**(.1) 그리고 연출이 끝나면 돌아온다(1).
        /// 시각은 벽시계라 «몇 프레임 뒤» 로 재지 않고 **닿는가/돌아오는가** 를 넉넉한 기한 안에서 본다(T360 이 가르친 자리).
        /// 상자 이름도 표에서 읽는다 — 그릇을 세우는 `Popups.cs` 가 이름을 바꾸면 이 자가 먼저 깨진다.
        /// </summary>
        [UnityTest]
        public IEnumerator 수령_연출_동안_토스트가_물러났다_돌아온다()
        {
            yield return Boot();
            string boxName = OpacityUi.Text("toasts_rw_dim", "box");
            Transform lane = UiRoot.Instance.App.Find(boxName);
            Assert.IsNotNull(lane, "정본 #toasts 레인(" + boxName + ")이 App 아래에 있어야 한다 — 이름이 바뀌면 표(OpacityUi.json 의 box)도 같이 고쳐라");

            float dim = OpacityUi.A("toasts_rw_dim");
            Assert.AreEqual(0.1f, dim, 1e-4f, "정본 style.css 7564 #toasts.rw-dim { opacity: .1 }");
            Assert.AreEqual(1f, Alpha(lane), 1e-3f, "연출 전에는 물러나 있지 않다");

            int icons = RewardBurst.Play(RewardBurst.Rewards("coins", 12), UiRoot.Instance.Sheet);
            Assert.Greater(icons, 0, "아이콘이 떠야 연출이 도는 것이다");

            float t0 = Time.realtimeSinceStartup;
            while (Alpha(lane) > dim + 0.01f)
            {
                Assert.Less(Time.realtimeSinceStartup - t0, 5f, "3초 안에 토스트가 .1 로 물러나야 한다(지금 " + Alpha(lane) + ")");
                yield return null;
            }
            Assert.AreEqual(dim, Alpha(lane), 0.02f, "물러난 값은 표의 .1");

            float t1 = Time.realtimeSinceStartup;
            // 끝값은 코루틴이 «정확히 1» 로 닫는다 — 0.99 에서 끊고 1 을 기대하면 한 프레임 일찍 나가 깨진다(런 594 가 그랬다).
            while (Alpha(lane) < 0.9999f)
            {
                Assert.Less(Time.realtimeSinceStartup - t1, 15f, "연출이 끝나면 토스트가 돌아와야 한다(지금 " + Alpha(lane) + ")");
                yield return null;
            }
            Assert.AreEqual(1f, Alpha(lane), 1e-3f, "돌아온 값은 1");
            Debug.Log("[T359] 토스트 물러남 왕복 " + (Time.realtimeSinceStartup - t0).ToString("0.00") + "초 · 아이콘 " + icons);
        }
    }
}
