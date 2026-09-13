using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Data;
using Forge.Core.Forging;
using Forge.Game;
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
    }
}
