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
    /// T20 — 소환 시트(스킬·펫 서브탭)가 실제 세이브·Core 위에서 서고, 소환 → 결과 연출 → 상세 → 장착/부화/젬 스킵/업그레이드 팝업이 열리며 콘솔 빨강 0 인가(빨간 로그는 러너가 실패시킨다).
    /// 글자는 전부 UiKit 표식 + 종류 하한 이상(T18 TextSizeGate 규칙을 이 화면들에도 적용). 디스크 세이브는 끈다(SuppressSave).
    /// </summary>
    public class PetUiTests
    {
        static IEnumerator Boot()
        {
            PetSkillHost.SuppressSave = true;
            PetSkillHost.Seed = 20260912;
            SceneManager.LoadScene("SampleScene");
            // 앞 씬의 시트·호스트가 내려가는 두 프레임을 기다린 뒤, «이 씬» 의 시트가 설 때까지(CI 런 46~58: 앞 씬 호스트가 살아 있어 새 씬에 호스트가 안 섰다 · T19 결정 112 와 같은 경쟁)
            yield return null;
            yield return null;
            Scene active = SceneManager.GetActiveScene();
            for (int i = 0; i < 600 && !(SkillPetSheet.Instance != null && SkillPetSheet.Instance.gameObject.scene == active && PetSkillHost.Ready && SkillBar.Instance != null); i++) yield return null;
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 서지 않았다(SaveIo/PetSkillHost 부팅)");
            Assert.AreEqual(active, SkillPetSheet.Instance.gameObject.scene, "앞 씬의 시트가 남아 있다");
            Assert.IsTrue(PetSkillHost.Ready);
            Assert.IsNotNull(PetSkillHost.Instance.Skills, "PetSkillHost 가 Ready 인데 규칙이 없다 — 앞 씬 호스트의 정적 상태가 남았다");
            yield return null;
        }

        static SkillPetSheet Sheet { get { return SkillPetSheet.Instance; } }
        static PetSkillHost Host { get { return PetSkillHost.Instance; } }

        static void OpenSummon()
        {
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
        }

        /// <summary>소환 결과 연출을 탭으로 닫는다 — 첫 탭은 «전부 공개», 둘째 탭은 «닫기». CI 러너에서 첫 프레임이 길면(3D 얼굴 첫 굽기) 연출이 이미 끝나 첫 탭이 곧 닫기라
        /// 둘째 탭의 <c>Current</c> 가 null 이다(런 118 NRE · 결정 기록) → 살아 있을 때만 탭한다.</summary>
        static IEnumerator TapResultClosed()
        {
            for (int k = 0; k < 4 && SkillSummonResultView.Current != null; k++)
            {
                SkillSummonResultView.Current.OnTap();
                yield return null;
            }
            Assert.IsFalse(Sheet.Modal.IsOpen(SkillSummonResultView.ModalName), "소환 결과 연출은 탭 두 번 안에 닫힌다");
        }

        /// <summary>T95 — 장착 오브: 어둠 막(.58 의 지각값 · 오브 전체) → 정중앙 타원(정본 치수 이상) → Lv 는 그 위, 잉크 위끝이 타원 아래끝에 닿는다.</summary>
        static void AssertEquippedOrb(string id)
        {
            RectTransform cell = Sheet.Skills.Cell(id);
            Assert.IsNotNull(cell, "장착 스킬 칸 " + id);
            RectTransform orb = (RectTransform)cell.Find("sk-orb");
            RectTransform dim = (RectTransform)orb.Find("equipped");
            Assert.IsNotNull(dim, "어둠 막");
            Assert.AreEqual(Vector2.zero, dim.anchorMin); Assert.AreEqual(Vector2.one, dim.anchorMax);
            Assert.AreEqual(UiKit.PerceivedDim(PetSkillStyle.C("orb_dim")).a, dim.GetComponent<UnityEngine.UI.Image>().color.a, 1e-4f, "어둠 막 α = 정본 .58 의 지각값");
            RectTransform plate = (RectTransform)orb.Find("sk-eqplate");
            Assert.IsNotNull(plate, "«장착됨» 타원");
            Assert.AreEqual(Vector2.zero, plate.anchoredPosition, "타원은 정중앙");
            Assert.AreEqual(PetSkillStyle.Px("sk_eqplate_w"), plate.rect.width, 0.5f, "타원 폭 = 앱 폭 14.92%");
            Assert.GreaterOrEqual(plate.rect.height, PetSkillStyle.Px("sk_eqplate_h_w") - 0.5f, "타원 높이 ≥ 정본 2.82%W(글자 하한이면 더 크다)");
            RectTransform lv = (RectTransform)orb.Find("sk-lv");
            Assert.Less(orb.Find("ico").GetSiblingIndex(), dim.GetSiblingIndex(), "막은 아이콘 위");
            Assert.Less(dim.GetSiblingIndex(), plate.GetSiblingIndex(), "타원은 막 위");
            Assert.Less(plate.GetSiblingIndex(), lv.GetSiblingIndex(), "Lv 는 타원 위(z 2)");
            float orbH = orb.rect.height, plateBottom = orbH * 0.5f + plate.rect.height * 0.5f;
            float inkTop = -lv.anchoredPosition.y - UiCatalog.Instance.Kind(TextKind.Body).size * PetSkillStyle.L("sk_lv_ink_half_f");
            Assert.GreaterOrEqual(inkTop, plateBottom - 0.5f, "Lv 잉크가 타원과 안 겹친다");
            Assert.GreaterOrEqual(-lv.anchoredPosition.y, orbH * PetSkillStyle.L("sk_lv_center_f") - 0.5f, "Lv 는 원작 72.9% 보다 위로 안 올라간다");
        }

        static void AssertPlainOrb(string id)
        {
            RectTransform orb = (RectTransform)Sheet.Skills.Cell(id).Find("sk-orb");
            Assert.IsNull(orb.Find("equipped"), "비장착 오브엔 막이 없다");
            Assert.IsNull(orb.Find("sk-eqplate"));
        }

        static void AssertTextGate(string where)
        {
            UiCatalog cat = UiCatalog.Instance;
            int seen = 0;
            foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            {
                if (!t.gameObject.activeInHierarchy) continue;
                seen++;
                UiTextKindTag tag = t.GetComponent<UiTextKindTag>();
                Assert.IsNotNull(tag, where + ": " + t.name + " 은 UiKit.Text 를 거치지 않았다");
                Assert.GreaterOrEqual(t.fontSize, cat.Kind(tag.Kind).min, where + ": " + t.name + " 글자 " + t.fontSize + " < 하한");
            }
            Assert.Greater(seen, 0, where + ": 활성 글자가 없다");
        }

        /// <summary>T102 — «장착됨» 리본: 정본 `.sk-ribbon{top:-.2rem}` = 윗변이 면 위 .2rem. 가운데를 거기 두면 반이 면 밖으로 나가 첫 행이 grid-scroll 마스크에 잘린다.</summary>
        static void AssertRibbonInsideTile(int i)
        {
            UnityEngine.UI.Button tile = Sheet.Pets.PetTile(i);
            Assert.IsNotNull(tile, "펫 타일 " + i);
            RectTransform face = null, ribbon = null;
            foreach (RectTransform rt in tile.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name == "tile-face") face = rt;
                if (rt.name == "sk-ribbon") ribbon = rt;
            }
            Assert.IsNotNull(face, "타일 면");
            Assert.IsNotNull(ribbon, "장착됨 리본");
            Assert.AreEqual(1f, ribbon.pivot.y, 1e-3f, "리본 pivot 은 윗변");
            Assert.AreEqual(PetSkillStyle.Rem(0.2f), ribbon.anchoredPosition.y, 0.5f, "리본 윗변 = 면 위 .2rem(정본 top:-.2rem)");
            Vector3[] fc = new Vector3[4], rc = new Vector3[4];
            face.GetWorldCorners(fc); ribbon.GetWorldCorners(rc);
            Assert.Less(rc[0].y, fc[1].y, "리본 아랫변은 면 안쪽(윗변 아래)에 있다");
            Assert.Greater(rc[0].y, fc[0].y, "리본은 면 아래로 안 내려간다");
        }

        static bool NoGraphics()
        {
            return SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
        }

        /// <summary>UI 를 한 장 그려 <paramref name="target"/> 의 화면 사각 안에서 <paramref name="match"/> 픽셀을 센다 — T87 `ForgeUiTests.CountPixels` 와 같은 길(그 파일은 T87 lock 이라 여기 줄인 사본 · T102).</summary>
        static int CountPixels(RectTransform target, System.Func<Color32, bool> match, out int area, out string info)
        {
            UiRoot root = UiRoot.Instance;
            Canvas canvas = root.Canvas;
            RenderMode prevMode = canvas.renderMode;
            Camera prevCam = canvas.worldCamera;
            float prevPlane = canvas.planeDistance;
            RenderTexture prevActive = RenderTexture.active;
            int w = Mathf.Max(64, Screen.width), ht = Mathf.Max(64, Screen.height);
            RenderTexture rt = new RenderTexture(w, ht, 24, RenderTextureFormat.ARGB32);
            GameObject camGo = new GameObject("t102-pixel-cam");
            Camera cam = camGo.AddComponent<Camera>();
            Texture2D shot = null;
            area = 0; info = "";
            try
            {
                int uiLayer = canvas.gameObject.layer;
                if (Camera.main != null) cam.CopyFrom(Camera.main);
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                cam.targetTexture = rt;
                cam.ResetProjectionMatrix();
                cam.cullingMask = 1 << uiLayer;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
                root.Layout();
                Canvas.ForceUpdateCanvases();
                cam.Render();

                RenderTexture.active = rt;
                shot = new Texture2D(w, ht, TextureFormat.RGB24, false);
                shot.ReadPixels(new Rect(0f, 0f, w, ht), 0, 0);
                shot.Apply(false);

                Vector3[] corners = new Vector3[4];
                target.GetWorldCorners(corners);
                Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
                Vector2 b = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
                int x0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, b.x)), 0, w - 1);
                int x1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, b.x)), 0, w - 1);
                int y0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, b.y)), 0, ht - 1);
                int y1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, b.y)), 0, ht - 1);
                area = (x1 - x0 + 1) * (y1 - y0 + 1);
                Assert.Greater(area, 8, target.name + " 칸이 화면에서 너무 작다(" + (x1 - x0 + 1) + "×" + (y1 - y0 + 1) + ")");
                Color32[] px = shot.GetPixels32();
                int hit = 0;
                Color32 brightest = new Color32(0, 0, 0, 255);
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        Color32 c = px[y * w + x];
                        if (match(c)) hit++;
                        if (c.r + c.g + c.b > brightest.r + brightest.g + brightest.b) brightest = c;
                    }
                }
                info = target.name + " 칸 " + area + "픽셀(" + (x1 - x0 + 1) + "×" + (y1 - y0 + 1) + ") 중 맞는 색 " + hit
                       + "개 · 가장 밝은 픽셀 rgb " + brightest.r + "," + brightest.g + "," + brightest.b;
                return hit;
            }
            finally
            {
                RenderTexture.active = prevActive;
                canvas.renderMode = prevMode;
                canvas.worldCamera = prevCam;
                canvas.planeDistance = prevPlane;
                if (root != null) root.Layout();
                Canvas.ForceUpdateCanvases();
                if (shot != null) Object.Destroy(shot);
                Object.Destroy(camGo);
                rt.Release();
            }
        }

        [UnityTest]
        public IEnumerator 소환_시트에_서브탭_셋과_스킬_펫_패널이_선다()
        {
            yield return Boot();
            OpenSummon();
            yield return null;
            Assert.AreEqual(SkillPetSheet.SubSkills, Sheet.ActiveSub, "원작 첫 서브탭 = 스킬");
            Assert.IsTrue(Sheet.SubVisible(SkillPetSheet.SubSkills));
            Assert.IsFalse(Sheet.SubVisible(SkillPetSheet.SubPets));
            StringAssert.StartsWith("스킬 ", Sheet.Skills.Title);
            Assert.IsNotNull(Sheet.Skills.SummonButton);
            Assert.IsNotNull(Sheet.Skills.UpgradeAllButton);
            Assert.IsNotNull(Sheet.Skills.QuickEquipButton);
            Assert.IsNotNull(Sheet.Skills.RatesButton);
            Assert.GreaterOrEqual(Sheet.Skills.GridCells, 1, "새 게임은 표창 난무 1개 보유");
            // T90 — 한글 글꼴 뒤 드러난 넘침 셋: ⓐ 액션 버튼은 글자 폭 + 패딩 이상 ⓑ Lv 라벨은 오브 안(원작 잉크 중심 72.9%) ⓒ 별이 없는 칸은 별 줄을 비워 두지 않는다
            foreach (var pair in new[] { new { B = Sheet.Skills.UpgradeAllButton, L = PetSkillStyle.T("upgrade_all") }, new { B = Sheet.Skills.QuickEquipButton, L = PetSkillStyle.T("quick_equip") } })
            {
                float need = PetSkillKit.TextWidth(TextKind.Button, pair.L) + PetSkillStyle.Px("action_pad_x_rem") * 2f;
                Assert.GreaterOrEqual(pair.B.GetComponent<RectTransform>().rect.width, need - 0.5f, "버튼 «" + pair.L + "» 이 글자보다 좁다(넘침)");
                Assert.GreaterOrEqual(pair.B.GetComponent<RectTransform>().rect.width, PetSkillStyle.Px("action_w") - 0.5f, "원작 고정폭은 하한");
            }
            string firstId = Host.Skills.State.Skills.KeyAt(0);
            RectTransform cell0 = Sheet.Skills.Cell(firstId);
            Assert.IsNotNull(cell0, "첫 스킬 칸");
            RectTransform orb0 = (RectTransform)cell0.Find("sk-orb");
            RectTransform lv0 = (RectTransform)orb0.Find("sk-lv");
            float orbH = orb0.rect.height;
            float lvCenter = -lv0.anchoredPosition.y;                       // 오브 위에서 잰 라벨 중심(아래가 +)
            float inkHalf = UiCatalog.Instance.Kind(TextKind.Body).size * PetSkillStyle.L("sk_lv_ink_half_f");
            float expectCenter = orbH * PetSkillStyle.L("sk_lv_center_f");
            RectTransform plate0 = (RectTransform)orb0.Find("sk-eqplate");
            if (plate0 != null) expectCenter = Mathf.Max(expectCenter, orbH * 0.5f + plate0.rect.height * 0.5f + inkHalf);   // T95 — 장착 오브는 타원 아래
            Assert.AreEqual(expectCenter, lvCenter, 0.5f, "Lv 라벨 중심 = 원작 72.9%(장착 오브는 타원 아래끝 + 잉크 반높이)");
            Assert.LessOrEqual(lvCenter + inkHalf, orbH + 0.5f, "Lv 잉크가 오브 아래로 안 나간다");
            Assert.LessOrEqual(lv0.rect.width, orbH * 1.0f + 0.5f, "Lv 라벨 폭 ≤ 지름(원작 90%)");
            if (Host.Skills.State.Get(firstId).Stars == 0)
            {
                float expect = PetSkillStyle.Px("sk_orb_w") + PetSkillStyle.Px("sk_cell_gap_h") + PetSkillStyle.Px("sk_shard_h");
                Assert.AreEqual(expect, cell0.rect.height, 0.5f, "별 없는 칸 = 오브 + 간격 + 게이지(별 줄 없음)");
                Assert.IsNull(cell0.Find("sk-star"), "별 0 이면 별 줄이 없다");
            }
            AssertTextGate("스킬 패널");

            Sheet.SubButton(SkillPetSheet.SubPets).onClick.Invoke();
            yield return null;
            Assert.AreEqual(SkillPetSheet.SubPets, Sheet.ActiveSub);
            Assert.IsTrue(Sheet.SubVisible(SkillPetSheet.SubPets));
            Assert.IsFalse(Sheet.SubVisible(SkillPetSheet.SubSkills));
            StringAssert.StartsWith("펫 ", Sheet.Pets.Title);
            Assert.IsNotNull(Sheet.Pets.SummonButton);
            Assert.IsNotNull(Sheet.Pets.BackButton, "부화장 ◀");
            AssertTextGate("펫 패널");

            Sheet.SubButton(SkillPetSheet.SubTech).onClick.Invoke();
            yield return null;
            Assert.IsTrue(Sheet.SubVisible(SkillPetSheet.SubTech), "기술 트리 자리(T21)");

            UiRoot.Instance.TabBar.OnTab("summon");
            yield return null;
            Assert.IsNull(UiRoot.Instance.TabBar.ActiveTab, "다시 누르면 홈");
        }

        [UnityTest]
        public IEnumerator 스킬_소환은_결과_연출을_열고_스킵하면_확인이_뜬다()
        {
            yield return Boot();
            OpenSummon();
            Sheet.Switch(SkillPetSheet.SubSkills);
            yield return null;
            while (Host.SummonMult("skill") != 1) Host.CycleSummonMult("skill");
            Host.Tickets = 10000;
            Host.Sync();
            yield return null;
            int before = Host.Skills.State.SummonCount;
            double tickets = Host.Tickets;
            double cost = Host.Skills.TicketCost(1);
            Sheet.Skills.SummonButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual(before + 1, Host.Skills.State.SummonCount, "x1 소환 = 굴림 1");
            Assert.AreEqual(tickets - cost, Host.Tickets, 1e-9, "티켓 선결제");
            Assert.IsTrue(Sheet.Modal.IsOpen(SkillSummonResultView.ModalName), "결과 연출 팝업");
            SkillSummonResultView v = SkillSummonResultView.Current;
            Assert.IsNotNull(v);
            Assert.AreEqual(1, v.CellCount);
            Assert.IsFalse(v.Done);
            v.OnTap();
            yield return null;
            Assert.IsTrue(v.Done, "탭 = 스킵 → 완료");
            Assert.AreEqual(1, v.OnCount);
            Assert.IsTrue(v.OkButton.gameObject.activeInHierarchy, "[확인]");
            Assert.IsNotNull(v.AgainButton, "x1 은 [다시 소환]");
            AssertTextGate("소환 결과");
            v.OkButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(Sheet.Modal.IsOpen(SkillSummonResultView.ModalName));

            // 대량(x25) — 11개부터 묶음 · 스킵 시 전부 켜진다
            while (Host.SummonMult("skill") != 25) Host.CycleSummonMult("skill");
            yield return null;
            Sheet.Skills.SummonButton.onClick.Invoke();
            yield return null;
            v = SkillSummonResultView.Current;
            Assert.IsNotNull(v, "x25 결과");
            Assert.LessOrEqual(v.CellCount, 18, "같은 스킬은 한 셀로 묶인다(스킬 18종)");
            v.OnTap();
            yield return null;
            Assert.AreEqual(v.CellCount, v.OnCount);
            v.OnTap();
            yield return null;
            Assert.IsNull(SkillSummonResultView.Current, "끝난 뒤 탭 = 닫기");
            while (Host.SummonMult("skill") != 1) Host.CycleSummonMult("skill");
        }

        [UnityTest]
        public IEnumerator 스킬_상세에서_장착을_토글하고_확률_팝업이_넘긴다()
        {
            yield return Boot();
            OpenSummon();
            Sheet.Switch(SkillPetSheet.SubSkills);
            yield return null;
            string id = Host.Skills.State.Skills.KeyAt(0);
            Sheet.Skills.OpenSkillDetail(id);
            yield return null;
            Assert.IsTrue(Sheet.Modal.IsOpen(SkillPanel.DetailModal));
            AssertTextGate("스킬 상세");
            bool was = Host.Skills.State.Equipped.Contains(id);
            PetSkillModal.Handle h = Sheet.Modal.Find(SkillPanel.DetailModal);
            var eq = h.Content.Find("btn-equip").GetComponent<UnityEngine.UI.Button>();
            eq.onClick.Invoke();
            yield return null;
            Assert.AreNotEqual(was, Host.Skills.State.Equipped.Contains(id), "장착 토글");
            if (Host.Skills.State.Equipped.Contains(id)) AssertEquippedOrb(id); else AssertPlainOrb(id);
            eq = Sheet.Modal.Find(SkillPanel.DetailModal).Content.Find("btn-equip").GetComponent<UnityEngine.UI.Button>();
            eq.onClick.Invoke();
            yield return null;
            Assert.AreEqual(was, Host.Skills.State.Equipped.Contains(id), "되돌림");
            if (Host.Skills.State.Equipped.Contains(id)) AssertEquippedOrb(id); else AssertPlainOrb(id);
            Sheet.Modal.Find(SkillPanel.DetailModal).XButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(Sheet.Modal.IsOpen(SkillPanel.DetailModal));

            Sheet.Skills.RatesButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(Sheet.Modal.IsOpen(SkillRatesPopup.ModalName));
            int lv = SkillRatesPopup.LevelNow;
            SkillRatesPopup.NextButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual(Mathf.Min(lv + 1, 100), SkillRatesPopup.LevelNow, "▶ 는 레벨 +1");
            AssertTextGate("확률 팝업");
            Sheet.Modal.Close(SkillRatesPopup.ModalName);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 펫_소환_알_부화_젬스킵_상세_업그레이드_팝업()
        {
            yield return Boot();
            OpenSummon();
            Sheet.Switch(SkillPetSheet.SubPets);
            yield return null;
            while (Host.SummonMult("pet") != 1) Host.CycleSummonMult("pet");
            Host.EggCurrency = 100000;
            Host.Gems = 100000;
            Host.Sync();
            yield return null;
            int eggs = Host.Pets.State.Eggs.Count;
            Sheet.Pets.SummonButton.onClick.Invoke();
            yield return null;
            Assert.GreaterOrEqual(Host.Pets.State.Eggs.Count, eggs + 1, "알 +1(보너스 알은 더 될 수 있다)");
            Assert.IsTrue(Sheet.Modal.IsOpen(SkillSummonResultView.ModalName));
            yield return TapResultClosed();

            // 알 상세 → [부화]
            int hatching = Host.Pets.State.Hatching.Count;
            Assert.Less(hatching, Host.Pets.MaxHatchSlots(), "부화장에 자리가 있어야 한다");
            Sheet.Pets.OpenEggDetail(0);
            yield return null;
            Assert.IsTrue(Sheet.Modal.IsOpen(PetPanel.DetailModal));
            AssertTextGate("알 상세");
            Sheet.Modal.Find(PetPanel.DetailModal).Content.Find("btn-hatch").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.AreEqual(hatching + 1, Host.Pets.State.Hatching.Count, "부화 시작");
            Assert.IsFalse(Sheet.Modal.IsOpen(PetPanel.DetailModal), "상세는 닫힌다");
            Assert.IsNotNull(Sheet.Pets.SkipButton(hatching), "부화 칸의 💎 스킵");
            // T102 — 부화 중인 칸의 빛기둥(.hatch-cone)이 **실제로 칠해진다**(런 95·186: 맨 Graphic 정점 메시는 0 픽셀이었다)
            PetHatchCone cone = Sheet.Pets.HatchCone(hatching);
            Assert.IsNotNull(cone, "부화 칸의 빛기둥");
            Assert.IsNotNull(cone.sprite, "빛기둥은 구운 스프라이트다(맨 Graphic 은 이 레포에서 안 칠해진다)");
            Assert.AreEqual(PetSkillStyle.Px("cone_w"), cone.rectTransform.rect.width, 0.5f, "빛기둥 폭 = 정본 12.65%W");
            Assert.AreEqual(PetSkillStyle.Px("cone_h"), cone.rectTransform.rect.height, 0.5f, "빛기둥 높이 = 정본 12.34%H");
            if (!NoGraphics())
            {
                int area; string info;
                // 따뜻한 노랑(빨강·초록이 높고 파랑이 확연히 낮다) — 남색 바탕(#1f2740)·검정 램프와 갈린다
                // 알·타이머·스킵 버튼이 상자 위에 겹치므로 문턱은 1/8 — 그라디언트 위쪽 3/4 만 따뜻해도 사다리꼴 면적이 상자의 1/3 을 넘는다
                int warm = CountPixels(cone.rectTransform, delegate(Color32 c) { return c.r > 100 && c.g > 90 && c.b + 30 < c.r; }, out area, out info);
                Assert.Greater(warm, area / 8, "빛기둥이 화면에 안 칠해졌다 — " + info);
            }

            // 젬 스킵 → 펫 +1
            int pets = Host.Pets.State.Pets.Count;
            Sheet.Pets.SkipButton(hatching).onClick.Invoke();
            yield return null;
            Assert.AreEqual(pets + 1, Host.Pets.State.Pets.Count, "즉시 부화");
            Assert.Greater(Sheet.Modal.ToastCount, 0, "부화 토스트");

            // 펫 상세 → 업그레이드 팝업
            int last = Host.Pets.State.Pets.Count - 1;
            Sheet.Pets.OpenPetDetail(last);
            yield return null;
            Assert.IsTrue(Sheet.Modal.IsOpen(PetPanel.DetailModal));
            AssertTextGate("펫 상세");
            Sheet.Modal.Find(PetPanel.DetailModal).Content.Find("btn-upgrade").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.IsTrue(Sheet.Modal.IsOpen(PetUpgradePopup.ModalName), "업그레이드 팝업");
            Assert.AreEqual(last, PetUpgradePopup.Target);
            AssertTextGate("펫 업그레이드");
            // T79 — 모달이 앱 상자를 덮고 탭바 위에 그려진다(원작 #pet-upgrade-modal z40 > 탭바 z30) · 딤은 정본 .5 의 선형 공간 환산값 · ✕ 는 카드 것 하나(탭바 ✕ 는 딤 아래 원작 그대로)
            PetSkillModal.Handle up = Sheet.Modal.Find(PetUpgradePopup.ModalName);

            // T115 — 넘침 막이를 **이 화면의 버튼 전부**로 넓힌다. T90 이 세운 막이는 스킬 서브시트의 버튼 둘만 재서,
            // 펫 업그레이드의 «업그레이드» 가 버튼 밖으로 삐져나온 채 초록으로 지나갔다(런 224 PNG 실측).
            // 재는 것: 라벨이 실제로 차지하는 폭(글꼴·크기·자간이 다 들어간 preferredWidth) ≤ 버튼 폭.
            int checkedBtns = 0;
            foreach (UnityEngine.UI.Button b in up.Root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                if (!b.gameObject.activeInHierarchy) continue;
                TMPro.TextMeshProUGUI lbl = b.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                if (lbl == null || string.IsNullOrEmpty(lbl.text)) continue;
                lbl.ForceMeshUpdate();
                float w = b.GetComponent<RectTransform>().rect.width;
                Assert.LessOrEqual(lbl.preferredWidth, w + 0.5f,
                    "버튼 «" + lbl.text + "» 의 글자(" + lbl.preferredWidth.ToString("0.0") + ")가 버튼 폭(" + w.ToString("0.0") + ")을 넘는다 — 테두리 밖으로 삐져나온다");
                checkedBtns++;
            }
            Assert.Greater(checkedBtns, 0, "펫 업그레이드 팝업에서 잰 버튼이 하나도 없다 — 막이가 헛돌고 있다");

            UiRoot root = UiRoot.Instance;
            Assert.AreEqual(root.App, up.Root.parent.parent, "모달 층은 앱 상자의 직계 자식");
            Assert.Greater(up.Root.parent.GetSiblingIndex(), root.TabBand.GetSiblingIndex(), "모달 층이 탭바 뒤(위)에 그려진다");
            RectTransform dimRt = (RectTransform)up.Root.Find("dim");
            Assert.IsNotNull(dimRt, "딤 층");
            Assert.AreEqual(Vector2.zero, dimRt.anchorMin); Assert.AreEqual(Vector2.one, dimRt.anchorMax);
            Assert.AreEqual(Vector2.zero, dimRt.sizeDelta, "딤이 앱 상자 전체를 덮는다");
            Assert.AreEqual(UiKit.PerceivedDim(PetSkillStyle.C("modal_dim")).a, dimRt.GetComponent<UnityEngine.UI.Image>().color.a, 1e-4f, "딤 α = 정본 .5 의 지각값");
            int xs = 0;
            foreach (Transform ch in up.Root.GetComponentsInChildren<Transform>(true)) if (ch.name == "x-btn") xs++;
            Assert.AreEqual(1, xs, "✕ 는 카드 것 하나");
            if (Host.Pets.State.Eggs.Count > 0)
            {
                PetUpgradePopup.ToggleMat(true, 0);
                yield return null;
                Assert.AreEqual(1, PetUpgradePopup.SelectedCount, "알 재료 선택");
                double xp = Host.Pets.State.Pets[last].Xp;
                int lv = Host.Pets.State.Pets[last].Level;
                int eggN = Host.Pets.State.Eggs.Count;
                PetUpgradePopup.Confirm();
                yield return null;
                Assert.AreEqual(eggN - 1, Host.Pets.State.Eggs.Count, "재료 알 소모");
                Assert.IsTrue(Host.Pets.State.Pets[last].Xp > xp || Host.Pets.State.Pets[last].Level > lv, "경험치 흡수");
            }
            PetUpgradePopup.Close();
            yield return null;
            Assert.IsFalse(Sheet.Modal.IsOpen(PetUpgradePopup.ModalName));

            // 출전 토글(상세 [제거]/[장착])
            bool active = Host.Pets.State.ActivePets.Contains(last);
            Sheet.Pets.OnTogglePet(last);
            yield return null;
            Assert.AreNotEqual(active, Host.Pets.State.ActivePets.Contains(last));
            if (Host.Pets.State.ActivePets.Contains(last)) AssertRibbonInsideTile(last);
            Sheet.Pets.OnTogglePet(last);
            yield return null;
            Assert.AreEqual(active, Host.Pets.State.ActivePets.Contains(last));

            // 시트를 닫으면 모달 전부 닫힘
            Sheet.Pets.OpenPetDetail(last);
            yield return null;
            UiRoot.Instance.TabBar.OnTab("summon");
            yield return null;
            Assert.AreEqual(0, Sheet.Modal.OpenCount, "시트 닫힘 = 모달 전부 닫힘(원작 closeAllTabSurfaces)");
        }
        [UnityTest]
        public IEnumerator 전투_HUD_스킬_바는_자동_토글과_슬롯_3이고_장착을_따라간다()
        {
            yield return Boot();
            SkillBar sb = SkillBar.Instance;
            Assert.IsNotNull(sb, "스킬 바가 HUD 층에 서지 않았다");
            Assert.AreEqual(Host.Skills.Rules.MaxActive, sb.SlotCount, "원작 MAX_ACTIVE 3 슬롯 고정");
            Assert.IsNotNull(sb.AutoButton);
            string first = Host.Skills.State.Equipped.Count > 0 ? Host.Skills.State.Equipped[0] : null;
            Assert.AreEqual(first, sb.SlotId(0), "첫 슬롯 = 장착 1번");
            // T109 8회차 — 전투 바 슬롯의 Lv 는 **판 없는 흰 글자 + 2px 검정 링**이다:
            // 정본 style.css 603 `.skill-btn .sk-lv` 가 기본 `.sk-lv`(4045 검정 알약)를 덮는다
            // (background:none · border:none · padding:0 · -webkit-text-stroke 2px · bottom .1rem).
            RectTransform lvRt = sb.SlotLv(0);
            Assert.IsNotNull(lvRt, "슬롯 Lv 라벨");
            Assert.IsNull(lvRt.Find("bg"), "판(알약 바탕)이 없다 — 정본 603 이 background 를 끈다");
            TextMeshProUGUI lvTx = lvRt.GetComponent<TextMeshProUGUI>();
            Assert.IsNotNull(lvTx, "Lv 라벨은 글자 자체다");
            Assert.AreEqual(PetSkillStyle.C("white"), lvTx.color, "흰 칠");
            Assert.Greater(lvTx.outlineWidth, 0f, "검정 링이 걸렸다(정본 2px)");
            Assert.AreEqual(PetSkillStyle.Px("sb_lv_bottom_rem"), lvRt.anchoredPosition.y, 0.5f, "bottom = 정본 .1rem");
            Assert.IsNull(sb.SlotLv(Host.Skills.Rules.MaxActive - 1), "빈 슬롯엔 Lv 라벨 없음");
            Assert.IsNull(sb.SlotId(Host.Skills.Rules.MaxActive - 1), "새 게임은 마지막 슬롯이 빈 원");
            bool auto = SaveIo.State.AutoCast;
            sb.AutoButton.onClick.Invoke();
            yield return null;
            Assert.AreNotEqual(auto, SaveIo.State.AutoCast, "자동 토글 = S.autoCast 반전");
            SkillBar.Instance.AutoButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual(auto, SaveIo.State.AutoCast);
            if (first != null)
            {
                Host.Skills.ToggleEquip(first);
                Host.Sync();
                yield return null;
                Assert.AreNotEqual(first, SkillBar.Instance.SlotId(0), "해제하면 슬롯이 따라간다");
                Host.Skills.ToggleEquip(first);
                Host.Sync();
                yield return null;
                Assert.AreEqual(first, SkillBar.Instance.SlotId(0));
                Assert.DoesNotThrow(() => SkillBar.Instance.OnCast(first), "전투가 없거나 쿨 중이면 false 로 끝난다");
            }
            AssertTextGate("스킬 바");
        }

        [UnityTest]
        public IEnumerator 탈것_시트_소환_상세_장착_타기_업그레이드_확률_팝업()
        {
            yield return Boot();
            Assert.IsNotNull(Host.Mounts, "T40 Core 탈것이 호스트에 섰다");
            while (Host.SummonMult("mount") != 1) Host.CycleSummonMult("mount");
            Host.Winders = 100000;
            Host.Sync();
            yield return null;
            // 원작 openMounts 는 어느 탭에서든 전체 모달 — 장비 시트의 탈것 칸이 부른다
            MountSheet.Open();
            yield return null;
            Assert.IsTrue(MountSheet.IsOpen, "탈것 시트(전체 모달)");
            Assert.IsNotNull(MountSheet.SummonButton);
            Assert.IsNotNull(MountSheet.RatesButton);
            Assert.IsNotNull(MountSheet.BackButton);
            AssertTextGate("탈것 시트");

            // 소환 x1 → 개체 +1(기술트리 보너스 마리는 더 될 수 있다) · 결과 연출 → 탭 두 번에 닫힘
            int n = Host.Mounts.Count();
            MountSheet.SummonButton.onClick.Invoke();
            yield return null;
            Assert.GreaterOrEqual(Host.Mounts.Count(), n + 1, "탈것 +1");
            Assert.IsTrue(Sheet.Modal.IsOpen(SkillSummonResultView.ModalName), "소환 결과 연출(탈것 갈래)");
            yield return TapResultClosed();
            Assert.AreEqual(Host.Mounts.Count(), MountSheet.GridCells, "타일 = 보유 개체 수");
            MountSheet.SummonButton.onClick.Invoke();
            yield return null;
            yield return TapResultClosed();
            Assert.GreaterOrEqual(Host.Mounts.Count(), 2, "재료·교체 검사를 하려면 둘 이상");

            // 상세 → 장착 토글(1마리 슬롯: 다른 것을 장착하면 이전 것은 자동 해제)
            MountSheet.OpenDetail(0);
            yield return null;
            Assert.IsTrue(Sheet.Modal.IsOpen(MountSheet.DetailModal), "탈것 상세");
            AssertTextGate("탈것 상세");
            Assert.IsTrue(Host.Mounts.IsActive(0), "첫 소환은 자동 장착(원작 Mounts.summon)");
            MountSheet.OnEquip(1);
            yield return null;
            Assert.IsTrue(Host.Mounts.IsActive(1) && !Host.Mounts.IsActive(0), "장착은 1마리 — 교체");
            Assert.AreEqual(1, Host.Mounts.RiddenIdx(), "탄 것 = 장착 1번");
            MountSheet.OnEquip(1);
            yield return null;
            Assert.IsFalse(Host.Mounts.IsActive(1), "해제");
            MountSheet.OnRide(0);
            yield return null;
            Assert.AreEqual(0, Host.Mounts.RiddenIdx(), "타기 = 그 개체로 교체");
            Assert.IsTrue(Sheet.Modal.IsOpen(MountSheet.ModalName), "시트는 재렌더 뒤에도 열려 있다");

            // 업그레이드 팝업: 재료 = 다른 개체(인덱스) · 확인 → 재료 소모 · 경험치/레벨 상승 · 대상 인덱스 보정
            MountSheet.OpenDetail(1);
            yield return null;
            Sheet.Modal.Find(MountSheet.DetailModal).Content.Find("btn-upgrade").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.IsTrue(Sheet.Modal.IsOpen(MountUpgradePopup.ModalName), "업그레이드 팝업");
            Assert.AreEqual(1, MountUpgradePopup.Target);
            AssertTextGate("탈것 업그레이드");
            Assert.IsNotNull(MountUpgradePopup.Chip(0), "재료 칩 = 다른 개체");
            Assert.IsNull(MountUpgradePopup.Chip(1), "대상 자신은 재료가 아니다");
            MountUpgradePopup.ToggleMat(0);
            yield return null;
            Assert.AreEqual(1, MountUpgradePopup.SelectedCount);
            int before = Host.Mounts.Count();
            string tname = Host.Mounts.Inst(1).Name;
            int lv = Host.Mounts.Inst(1).Level;
            double xp = Host.Mounts.Inst(1).Xp;
            MountUpgradePopup.Confirm();
            yield return null;
            Assert.AreEqual(before - 1, Host.Mounts.Count(), "재료 흡수");
            Assert.AreEqual(0, MountUpgradePopup.Target, "재료(앞 인덱스)가 사라져 대상 인덱스가 당겨진다");
            Assert.AreEqual(tname, Host.Mounts.Inst(0).Name);
            Assert.IsTrue(Host.Mounts.Inst(0).Xp > xp || Host.Mounts.Inst(0).Level > lv, "경험치 흡수");
            MountUpgradePopup.Close();
            yield return null;
            Assert.IsFalse(Sheet.Modal.IsOpen(MountUpgradePopup.ModalName));

            // 확률 팝업(탈것 갈래: 레벨 상한 50 · 게이지 = 오픈 수)
            SkillRatesPopup.Open(Sheet, "mount");
            yield return null;
            Assert.IsTrue(Sheet.Modal.IsOpen(SkillRatesPopup.ModalName));
            Assert.AreEqual("mount", SkillRatesPopup.KindNow);
            Assert.AreEqual(Host.Mounts.Level(), SkillRatesPopup.LevelNow);
            SkillRatesPopup.Step(1);
            yield return null;
            Assert.AreEqual(Host.Mounts.Level() + 1, SkillRatesPopup.LevelNow);
            AssertTextGate("탈것 확률");
            Sheet.Modal.Close(SkillRatesPopup.ModalName);
            yield return null;

            // ◀ = closeMounts
            MountSheet.BackButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(MountSheet.IsOpen, "◀ 로 닫힌다");
            Assert.IsFalse(Sheet.Modal.IsOpen(MountSheet.DetailModal));
        }
    }
}
