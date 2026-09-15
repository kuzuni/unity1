using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T378 — «표값 × 박힌 상수» 자리를 걷은 뒤 그 자리가 **표값 그대로** 서는지(자 `tools/check_table_scale.py` 는 코드 글자를 보고,
    /// 이 자는 실제 RectTransform 을 본다). 자리를 걷을 때마다 칸을 하나씩 더한다.
    /// </summary>
    public class TableScaleSitesTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t = Time.realtimeSinceStartup;
            while (!(MetaHost.Ready && PopupLayer.Instance != null && UiRoot.Instance != null))
            {
                Assert.Less(Time.realtimeSinceStartup - t, 20f, "MetaHost/PopupLayer 가 20초 안에 준비되지 않았다");
                yield return null;
            }
            yield return null;
        }

        /// <summary>3회차 — 프로필 연필 버튼: 정본 3008 `.profile-edit-btn { width: 1.5rem; height: 1.5rem }` = 표 `profile_edit`(0.0284H) 그대로(전엔 ×1.3).</summary>
        [UnityTest]
        public IEnumerator 프로필_연필_버튼은_표_profile_edit_그대로_1_5rem_정사각이다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            ProfilePopup.Open(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(ProfilePopup.Name);
            Assert.IsNotNull(p, "프로필 팝업");
            Button avEdit = null;
            foreach (Button b in p.Root.GetComponentsInChildren<Button>(true)) if (b.name == "avatar-edit") avEdit = b;
            Assert.IsNotNull(avEdit, "연필 버튼(avatar-edit)");
            Rect r = avEdit.GetComponent<RectTransform>().rect;
            float expect = UiKit.H("profile_edit");
            Assert.AreEqual(expect, r.width, 0.5f, "폭 = 표 profile_edit(곱 없이)");
            Assert.AreEqual(expect, r.height, 0.5f, "높이 = 표 profile_edit(곱 없이)");
            Assert.AreEqual(PopupKit.Rem * 1.5f, r.width, 0.5f, "정본 1.5rem — 표 0.0284 가 그 값이다(1.5 × rem_h)");
            Assert.Less(r.width, UiKit.H("profile_edit") * 1.3f - 1f, "옛 ×1.3 크기가 아니다");
            PopupLayer.Instance.Hide(ProfilePopup.Name);
            yield return null;
        }
    }
}
