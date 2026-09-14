using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Data;
using Forge.Core.Forging;
using Forge.Game;
using Forge.Game.Hero;
using Forge.Game.Gallery;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T122 — 장비 그림 = 실제 3D 장비 썸네일(원작 `Scene3D.itemThumb`). 한 시대 한 부위의 다섯 칸이 **서로 다른 그림**이고(픽셀 해시 5개 서로 다름 · 종전엔 슬롯 실루엣 5종 반복),
    /// 캐시는 같은 키에 같은 스프라이트를 주며, 장신구(캡처 없음)는 null 로 실루엣 폴백을 알린다. 펌프는 한 프레임에 `chunk` 장까지만 굽는다(정본 CHUNK 6).
    /// 자기 파일인 이유: `ForgeUiTests.cs` 는 T87 lock 이 쥐고 있다(T111 GearDetailTests 와 같은 꼴).
    /// </summary>
    public class ItemFacesTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            Assert.IsNotNull(ForgeHost.Instance.Engine, "ForgeHost 가 Ready 인데 엔진이 없다");
            ItemFaces.Reset();
            yield return null;
        }

        [TearDown]
        public void CleanSave()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
        }

        /// <summary>스프라이트 픽셀 해시 + 불투명 픽셀 수(«무언가 그려졌다»).</summary>
        private static long Hash(Sprite sp, out int opaque)
        {
            Color32[] px = ((Texture2D)sp.texture).GetPixels32();
            long h = 17; opaque = 0;
            for (int i = 0; i < px.Length; i++)
            {
                if (px[i].a > 16) opaque++;
                h = h * 31 + px[i].r; h = h * 31 + px[i].g; h = h * 31 + px[i].b; h = h * 31 + px[i].a;
            }
            return h;
        }

        private static void AssertFiveDistinct(GameDefs defs, string slot, string age, string[] wtypes, string label)
        {
            int px = Mathf.RoundToInt(ItemFacesStyle.L("px"));
            var hashes = new HashSet<long>();
            int n = Mathf.Min(5, wtypes != null ? wtypes.Length : 5);
            Assert.GreaterOrEqual(n, 2, label + " 변형이 둘은 있어야 한다");
            for (int i = 0; i < n; i++)
            {
                Sprite sp = ItemFaces.Get(defs, slot, age, 0, wtypes != null ? wtypes[i] : null, i, "common", 0);
                Assert.IsNotNull(sp, label + " " + i + " 썸네일이 null — 캡처(gear-meshes.json)에 없거나 굽기가 죽었다");
                Assert.AreEqual(px, sp.texture.width, label + " 폭 = 표 px");
                int opaque;
                long h = Hash(sp, out opaque);
                Assert.Greater(opaque, px * px / 50, label + " " + i + " 에 그려진 것이 거의 없다(불투명 " + opaque + ")");
                Assert.Less(opaque, px * px * 99 / 100, label + " " + i + " 가 통째로 칠해졌다(배경이 투명이 아니다)");
                Assert.IsTrue(hashes.Add(h), label + " " + i + " 가 앞 칸과 같은 그림이다 — 종전 «실루엣 반복» 그대로다");
            }
        }

        [UnityTest]
        public IEnumerator 한_시대_한_부위의_다섯_칸은_서로_다른_3D_썸네일이고_장신구는_실루엣_폴백이다()
        {
            yield return Boot();
            if (!GallerySheet.GraphicsAvailable) Assert.Ignore("그래픽 장치가 없다 — 썸네일은 CI 의 유니티 잡이 본다");
            PlayLog log = PlayLog.Start("item-faces");
            ForgeHost h = ForgeHost.Instance;
            GameDefs defs = h.Defs;
            Assert.IsTrue(ItemFaces.Available, "장비 메시 표(gear-meshes.json)가 꽂혀야 한다");
            string age = defs.Ages[0];

            AssertFiveDistinct(defs, "weapon", age, h.Engine.WeaponsOfAge(age), "무기 " + age);
            AssertFiveDistinct(defs, "helmet", age, null, "투구 " + age);
            AssertFiveDistinct(defs, "armor", age, null, "갑옷 " + age);

            // 캐시 — 같은 키는 같은 스프라이트
            string wt = h.Engine.WeaponsOfAge(age)[0];
            Sprite a = ItemFaces.Get(defs, "weapon", age, 0, wt, 0, "common", 0);
            Sprite b = ItemFaces.Get(defs, "weapon", age, 0, wt, 0, "common", 0);
            Assert.AreSame(a, b, "캐시");
            // 등급·승천 티어는 키에 든다(정본 키 · 별이 달라도 같은 썸네일이 재사용되지 않게)
            Assert.AreNotEqual(ItemFaces.Key(new ForgeItem { Slot = "weapon", WType = wt, Age = age, Rarity = "common", Stars = 0 }),
                               ItemFaces.Key(new ForgeItem { Slot = "weapon", WType = wt, Age = age, Rarity = "common", Stars = 1 }), "승천 티어가 키에 든다");

            // 장신구 — 정본 makeAccessoryPreview 는 T37 캡처에 없다 → null(호출자가 슬롯 실루엣으로)
            Assert.IsFalse(ItemFaces.Supports("gloves"));
            Assert.IsNull(ItemFaces.Get(defs, "gloves", age, 0, null, 0, "common", 0), "장신구는 캡처가 없어 실루엣 폴백");

            // 무대·리그·모델이 씬에 남지 않는다(굽고 나면 모델은 지운다)
            yield return null;
            Assert.IsNull(GameObject.Find("armor " + age + "/0"), "구운 뒤 갑옷 리그는 지운다");
            log.AssertNoRed();
            log.Dispose();
        }

        [UnityTest]
        public IEnumerator 펌프는_한_프레임에_chunk_장까지만_굽고_새_작업이_시작되면_이전_요청을_버린다()
        {
            yield return Boot();
            if (!GallerySheet.GraphicsAvailable) Assert.Ignore("그래픽 장치가 없다 — 썸네일은 CI 의 유니티 잡이 본다");
            ForgeHost h = ForgeHost.Instance;
            GameDefs defs = h.Defs;
            Assert.IsTrue(ItemFaces.Available);
            int chunk = Mathf.RoundToInt(ItemFaces.Chunk);
            Assert.AreEqual(6, chunk, "정본 CHUNK = 6");
            string age = defs.Ages[0];
            string[] wts = h.Engine.WeaponsOfAge(age);
            int job = ItemFaces.NewJob();
            var got = new List<Sprite>();
            int total = 0;
            foreach (string r in new[] { "rare", "epic" })
                for (int i = 0; i < Mathf.Min(5, wts.Length); i++) { total++; ItemFaces.Request(job, defs, new ForgeItem { Slot = "weapon", WType = wts[i], Age = age, Rarity = r }, sp => got.Add(sp)); }
            Assert.AreEqual(total, ItemFaces.Pending, "요청은 다음 프레임부터 굽는다(동기로 굽지 않는다)");
            int n = ItemFaces.Pump();
            Assert.LessOrEqual(n, chunk, "한 프레임에 chunk 장까지");
            Assert.AreEqual(n, got.Count);
            foreach (Sprite sp in got) Assert.IsNotNull(sp);
            // 새 작업 → 남은 요청은 버려진다(정본: 목록을 다시 그리면 이전 작업은 스스로 멈춘다)
            int job2 = ItemFaces.NewJob();
            Assert.AreEqual(0, ItemFaces.Pending, "새 작업이 시작되면 이전 요청은 버린다");
            Assert.AreEqual(0, ItemFaces.Pump());
            // 옛 작업 번호로 낸 요청은 안 받는다
            ItemFaces.Request(job, defs, new ForgeItem { Slot = "weapon", WType = wts[0], Age = age, Rarity = "legendary" }, sp => got.Add(sp));
            Assert.AreEqual(0, ItemFaces.Pending, "옛 작업 번호의 요청은 버린다");
            // 새 작업 번호로 낸 요청은 펌프(MonoBehaviour)가 프레임마다 굽는다
            int before = got.Count;
            ItemFaces.Request(job2, defs, new ForgeItem { Slot = "weapon", WType = wts[0], Age = age, Rarity = "legendary" }, sp => got.Add(sp));
            for (int f = 0; f < 5 && got.Count == before; f++) yield return null;
            Assert.AreEqual(before + 1, got.Count, "펌프가 프레임마다 굽는다");
            Assert.IsNotNull(got[got.Count - 1]);
        }

        /// <summary>T122 3회차 ⓐ — 정본 `equipCellHTML`(ui.js 3103 `itemImgHTML(it,'cell-img')`)이 그리는 두 자리: 장비 시트 칸(`ForgeSheet.EquipCell`)과 플레이어 정보 칸(`PlayerInfoPopup.EquipCell`).
        /// 캡처 있는 부위는 구운 썸네일(캐시 같은 참조 · 틴트 없음 · contain), 장신구는 슬롯 실루엣 그대로(정본 폴백 순서).</summary>
        [UnityTest]
        public IEnumerator 장비_시트_칸과_플레이어_정보_칸의_장착_장비_그림은_구운_썸네일이고_장신구는_실루엣이다()
        {
            yield return Boot();
            if (!GallerySheet.GraphicsAvailable) Assert.Ignore("그래픽 장치가 없다 — 썸네일은 CI 의 유니티 잡이 본다");
            PlayLog log = PlayLog.Start("item-faces-cells");
            ForgeHost h = ForgeHost.Instance;
            GameDefs d = h.Defs;
            Assert.IsTrue(ItemFaces.Available, "장비 메시 표(gear-meshes.json)가 꽂혀야 한다");
            ForgeItem cur = null;
            for (int i = 0; i < 200 && cur == null; i++) { ForgeItem it = h.Engine.RollItem(); if (ItemFaces.Supports(it.Slot)) cur = it; }
            Assert.IsNotNull(cur, "무기·투구·갑옷 하나는 나와야 한다");
            ForgeItem glove = new ForgeItem { Slot = "gloves", Age = d.Ages[0], AgeIdx = 0, Rarity = "common", Name = "장갑", Level = 1 };
            h.Gear.Set(cur.Slot, cur);
            h.Gear.Set("gloves", glove);
            h.Push();
            ForgeSheet.Render(h);
            yield return null;
            Sprite thumb = ItemFaces.Get(d, cur);
            Assert.IsNotNull(thumb, "장착 장비의 구운 썸네일");
            float frac = ItemFacesStyle.L("img_frac");

            // ⓐ 장비 시트 칸
            RectTransform sheet = UiRoot.Instance.Sheet;
            Image sheetImg = CellImg(sheet, "cell-" + cur.Slot, "장비 시트 " + cur.Slot);
            Assert.AreSame(thumb, sheetImg.sprite, "장비 시트 칸 = 그 장비의 구운 썸네일(캐시 같은 참조)");
            Assert.AreNotSame(UiIcons.Get(ForgeUi.ItemIconKey(d, cur)), sheetImg.sprite, "아틀라스 실루엣이 아니다");
            Assert.AreEqual(Color.white, sheetImg.color, "썸네일은 틴트 없이");
            Assert.IsTrue(sheetImg.preserveAspect, "object-fit: contain");
            Rect cellR = ((RectTransform)sheetImg.transform.parent).rect;
            Assert.AreEqual(cellR.width * frac, sheetImg.rectTransform.rect.width, 0.6f, "썸네일 한 변 = 칸 × img_frac");
            Image sheetGlove = CellImg(sheet, "cell-gloves", "장비 시트 장갑");
            Assert.AreSame(UiIcons.Get(ForgeUi.SlotIconKey("gloves")), sheetGlove.sprite, "장신구 칸은 슬롯 실루엣 그대로(캡처 없음)");

            // ⓑ 플레이어 정보 칸(정본은 같은 equipCellHTML)
            PlayerInfoPopup.Open(MetaHost.Instance);
            yield return null; yield return null;
            Popup p = PopupLayer.Instance.Find(PlayerInfoPopup.Name);
            Assert.IsNotNull(p, "플레이어 정보 팝업");
            Image infoImg = CellImg(p.Root, "slot-" + cur.Slot, "플레이어 정보 " + cur.Slot);
            Assert.AreSame(thumb, infoImg.sprite, "플레이어 정보 칸 = 같은 썸네일(캐시 같은 참조)");
            Assert.AreEqual(Color.white, infoImg.color, "틴트 없이");
            Assert.IsTrue(infoImg.preserveAspect, "contain");
            Image infoGlove = CellImg(p.Root, "slot-gloves", "플레이어 정보 장갑");
            Assert.AreSame(UiIcons.Get(ForgeUi.SlotIconKey("gloves")), infoGlove.sprite, "장신구 칸은 실루엣");
            h.Meta.Popups.HideAll();
            yield return null;
            log.AssertNoRed();
            log.Dispose();
        }

        private static Image CellImg(Transform root, string cellName, string label)
        {
            RectTransform cell = FindIn(root, cellName);
            Assert.IsNotNull(cell, label + " 칸");
            Transform t = cell.Find("img");
            Image img = t != null ? t.GetComponent<Image>() : null;
            Assert.IsNotNull(img, label + " 그림");
            return img;
        }

        private static RectTransform FindIn(Transform root, string name)
        {
            foreach (RectTransform rt in root.GetComponentsInChildren<RectTransform>(true)) if (rt.name == name) return rt;
            return null;
        }

        private static Image TileImg(Transform root, string cardName, string label)
        {
            RectTransform card = FindIn(root, cardName);
            Assert.IsNotNull(card, label + " 카드");
            Transform tile = card.Find("tile");
            Assert.IsNotNull(tile, label + " 타일");
            Image img = tile.Find("img") != null ? tile.Find("img").GetComponent<Image>() : null;
            Assert.IsNotNull(img, label + " 그림");
            return img;
        }

        /// <summary>T122 2회차 — 정본 `itemImgHTML`: 비교 팝업 두 카드(장착됨·새 장비)의 그림은 `Scene3D.itemThumb` 3D 썸네일이고, 캡처가 없는 장신구는 슬롯 실루엣 그대로다.</summary>
        [UnityTest]
        public IEnumerator 비교_팝업_두_카드의_장비_그림은_구운_썸네일이고_장신구는_실루엣이다()
        {
            yield return Boot();
            if (!GallerySheet.GraphicsAvailable) Assert.Ignore("그래픽 장치가 없다 — 썸네일은 CI 의 유니티 잡이 본다");
            PlayLog log = PlayLog.Start("item-faces-card");
            ForgeHost h = ForgeHost.Instance;
            GameDefs d = h.Defs;
            Assert.IsTrue(ItemFaces.Available, "장비 메시 표(gear-meshes.json)가 꽂혀야 한다");
            int px = Mathf.RoundToInt(ItemFacesStyle.L("px"));

            // 캡처가 있는 부위(무기·투구·갑옷)의 장비 둘 — 하나는 장착, 하나는 «새 장비»
            ForgeItem cur = null, fresh = null;
            for (int i = 0; i < 200 && (cur == null || fresh == null); i++)
            {
                ForgeItem it = h.Engine.RollItem();
                if (!ItemFaces.Supports(it.Slot)) continue;
                if (cur == null) cur = it;
                else if (it.Slot == cur.Slot && ItemFaces.Key(it) != ItemFaces.Key(cur)) fresh = it;
            }
            Assert.IsNotNull(cur, "무기·투구·갑옷 하나는 나와야 한다");
            Assert.IsNotNull(fresh, "같은 부위의 다른 장비 하나");
            h.GearSys.Equip(cur);
            yield return null;

            ForgeCraftPopup.Show(h, fresh);
            yield return null;
            yield return null;
            Popup p = h.Meta.Popups.Find(ForgeCraftPopup.Name);
            Assert.IsNotNull(p, "비교 팝업이 열려 있다");
            Image curImg = TileImg(p.Root, "cur", "장착됨");
            Image newImg = TileImg(p.Root, "new", "새 장비");
            Assert.IsNotNull(curImg.sprite, "장착됨 그림");
            Assert.IsNotNull(newImg.sprite, "새 장비 그림");
            Assert.AreSame(ItemFaces.Get(d, cur), curImg.sprite, "장착됨 카드 = 그 장비의 구운 썸네일(캐시 같은 참조)");
            Assert.AreSame(ItemFaces.Get(d, fresh), newImg.sprite, "새 장비 카드 = 그 장비의 구운 썸네일");
            Assert.AreNotSame(curImg.sprite, newImg.sprite, "두 장비의 썸네일은 서로 다르다");
            Assert.AreEqual(px, curImg.sprite.texture.width, "구운 크기(ItemFacesUi px)");
            Assert.AreNotSame(UiIcons.Get(ForgeUi.ItemIconKey(d, cur)), curImg.sprite, "아틀라스 실루엣이 아니다");
            Assert.AreEqual(Color.white, curImg.color, "썸네일은 틴트 없이");
            Assert.IsTrue(curImg.preserveAspect, "object-fit: contain");
            float frac = ItemFacesStyle.L("img_frac");
            Rect tileR = ((RectTransform)curImg.transform.parent).rect;
            Assert.AreEqual(tileR.width * frac, curImg.rectTransform.rect.width, 0.6f, "썸네일 한 변 = 타일 × img_frac(정본 .fl-face img 100%)");
            h.Meta.Popups.Hide(ForgeCraftPopup.Name);
            yield return null;

            // 장신구 — 캡처가 없어 Get 은 null · 타일은 슬롯 실루엣 그대로(정본 플레이스홀더 순서)
            ForgeItem glove = new ForgeItem { Slot = "gloves", Age = d.Ages[0], AgeIdx = 0, Rarity = "common", Name = "장갑", Level = 1 };
            Assert.IsNull(ItemFaces.Get(d, glove), "장신구는 캡처가 없다");
            RectTransform acc = ForgeUi.ItemTile(UiRoot.Instance.App, "t122-acc", 100f, d, glove);
            yield return null;
            Image accImg = acc.Find("img").GetComponent<Image>();
            Assert.AreSame(UiIcons.Get(ForgeUi.SlotIconKey("gloves")), accImg.sprite, "장신구 타일은 슬롯 실루엣");
            Assert.AreEqual(76f, accImg.rectTransform.rect.width, 0.6f, "실루엣 잉크 76%(옛 배치 그대로)");
            UnityEngine.Object.Destroy(acc.gameObject);
            yield return null;
            log.AssertNoRed();
            log.Dispose();
        }

        /// <summary>T122 2회차 — 런 339 실측: PaperdollTests 가 `GearMeshes.Uninstall()` 로 표를 뗀 뒤 도는 촬영이 실루엣만 찍었다(«한 번만 꽂는다» 가 영영 false). 표가 없으면 다시 꽂고, 없던 순간의 null 은 캐시에 남기지 않는다.</summary>
        [UnityTest]
        public IEnumerator 다른_곳이_장비_메시_표를_뗀_뒤에도_다음_Get_은_표를_다시_꽂고_굽는다()
        {
            yield return Boot();
            if (!GallerySheet.GraphicsAvailable) Assert.Ignore("그래픽 장치가 없다 — 썸네일은 CI 의 유니티 잡이 본다");
            ForgeHost h = ForgeHost.Instance;
            GameDefs d = h.Defs;
            Assert.IsTrue(ItemFaces.Available, "장비 메시 표(gear-meshes.json)가 꽂혀야 한다");
            string age = d.Ages[0];
            string wt = h.Engine.WeaponsOfAge(age)[0];
            ForgeItem it = new ForgeItem { Slot = "weapon", WType = wt, Age = age, AgeIdx = 0, Rarity = "common" };

            GearMeshes.Uninstall();
            Assert.IsNull(GearMeshes.Installed, "뗐다");
            Assert.IsTrue(ItemFaces.Available, "표가 없으면 다시 꽂는다(한 번만 시도하지 않는다)");
            Assert.IsNotNull(GearMeshes.Installed, "다시 꽂혔다");
            Sprite sp = ItemFaces.Get(d, it);
            Assert.IsNotNull(sp, "뗀 뒤 첫 Get 도 썸네일을 준다");

            // 캐시에 없는 새 키를 «표가 없는 순간» 에 물어도 null 이 캐시에 남지 않는다
            ForgeItem it2 = new ForgeItem { Slot = "weapon", WType = wt, Age = age, AgeIdx = 0, Rarity = "rare" };
            int before = ItemFaces.CacheCount;
            GearMeshes.Uninstall();
            ItemFaces.Reset();   // 무대·캐시를 비우고 «설치 실패» 기억도 지운다 — 다음 Get 이 다시 꽂는다
            Assert.IsNotNull(ItemFaces.Get(d, it2), "Reset 뒤 첫 Get 이 표를 다시 꽂고 굽는다");
            Assert.AreEqual(1, ItemFaces.CacheCount, "굽힌 것만 캐시에 든다 · 이전 " + before);
            yield return null;
        }
            /// <summary>T122 ⓑ — 정본 2104 플레이스홀더 + 2183 hydrateForgeThumbs(펌프 · rarity common · 티어 없음) · 2244 상세 `idet-icon`(동기) · 1906 buildCraftCard(itemImgHTML):
        /// 목록의 투구·갑옷 다섯 칸이 서로 다른 구운 썸네일이고 장신구 칸은 실루엣 · 상세 아이콘 = 그 정의의 썸네일 · 리빌 카드 = 그 장비의 썸네일.</summary>
        [UnityTest]
        public IEnumerator 목록_상세_리빌_카드의_장비_그림은_구운_썸네일이고_장신구는_실루엣이다()
        {
            yield return Boot();
            if (!GallerySheet.GraphicsAvailable) Assert.Ignore("그래픽 장치가 없다 — 썸네일은 CI 의 유니티 잡이 본다");
            PlayLog log = PlayLog.Start("item-faces-list");
            ForgeHost h = ForgeHost.Instance;
            GameDefs d = h.Defs;
            Assert.IsTrue(ItemFaces.Available, "장비 메시 표(gear-meshes.json)가 꽂혀야 한다");
            string age = d.Ages[0];

            // ① 목록 — 펌프가 다음 프레임부터 몇 장씩 굽는다 · 다 구울 때까지 프레임을 넘긴다
            ForgeInfoPopup.OpenList(h);
            yield return null;
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeInfoPopup.Name), "장비 목록 팝업");
            int frames = 0;
            while (ItemFaces.Pending > 0 && frames < 2000) { frames++; yield return null; }
            Assert.AreEqual(0, ItemFaces.Pending, "펌프가 2000프레임 안에 다 굽는다");
            Transform section = FindIn(h.Meta.Popups.Find(ForgeInfoPopup.Name).Root, "section-" + age);
            Assert.IsNotNull(section, "첫 시대 절");
            var helmetSprites = new System.Collections.Generic.List<Sprite>();
            int silhouettes = 0;
            foreach (Transform cell in FindIn(section, "forge-item-grid"))
            {
                if (!cell.name.StartsWith("cell-")) continue;
                Transform tile = cell.Find("fl-face"); Assert.IsNotNull(tile, cell.name + " 타일");
                Image img = tile.Find("img").GetComponent<Image>();
                Assert.IsNotNull(img.sprite, cell.name + " 그림");
                for (int i = 0; i < 5; i++)
                {
                    Sprite want = ItemFaces.Get(d, "helmet", age, 0, null, i, "common", 0);
                    if (want != null && ReferenceEquals(img.sprite, want)) helmetSprites.Add(img.sprite);
                }
                if (ReferenceEquals(img.sprite, UiIcons.Get(ForgeUi.SlotIconKey("gloves")))) silhouettes++;
            }
            Assert.AreEqual(5, helmetSprites.Count, "첫 시대 투구 다섯 칸이 각각 제 정의의 구운 썸네일을 쥔다(펌프가 갈아 끼웠다)");
            Assert.AreEqual(5, new System.Collections.Generic.HashSet<Sprite>(helmetSprites).Count, "투구 다섯 칸은 서로 다른 그림(정본이 고치려던 «같은 부위 다섯이 같은 그림»)");
            Assert.Greater(silhouettes, 0, "캡처가 없는 장신구(장갑) 칸은 슬롯 실루엣 그대로");

            // ② 상세 — 동기 한 장
            ForgeInfoPopup.OpenDetail(h, age, "helmet", 1, null);
            yield return null;
            Transform head = FindIn(h.Meta.Popups.Find(ForgeInfoPopup.ItemName).Root, "idet-head");
            Image di = head.Find("idet-icon/img").GetComponent<Image>();
            Assert.AreSame(ItemFaces.Get(d, "helmet", age, 0, null, 1, "common", 0), di.sprite, "상세 아이콘 = 그 정의의 썸네일(정본 2244)");
            ForgeInfoPopup.CloseItemDetail(h);
            ForgeInfoPopup.Close(h);
            yield return null;

            // ③ 리빌 카드 — 그 장비의 썸네일(정본 buildCraftCard → itemImgHTML)
            ForgeItem it = null;
            for (int i = 0; i < 200 && it == null; i++) { ForgeItem r = h.Engine.RollItem(); if (ItemFaces.Supports(r.Slot)) it = r; }
            Assert.IsNotNull(it);
            bool done = false;
            ForgeCraftPopup.ShowReveal(h, it, () => { done = true; });
            yield return null;
            Transform reveal = UiRoot.Instance.App.Find("craft-reveal");
            Assert.IsNotNull(reveal, "리빌 카드 오버레이");
            Image ci = reveal.Find("card/img").GetComponent<Image>();
            Assert.AreSame(ItemFaces.Get(d, it), ci.sprite, "리빌 카드 = 그 장비의 구운 썸네일");
            float t = 0f;
            while (!done && t < 3f) { t += Time.unscaledDeltaTime; yield return null; }
            log.AssertNoRed();
            log.Dispose();
        }
    }
}
