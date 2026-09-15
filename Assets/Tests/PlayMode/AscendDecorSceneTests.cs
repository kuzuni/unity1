using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Pets;
using Forge.Game.Battle;
using Forge.Game.Gallery;
using Forge.Game.Mounts;
using Forge.Game.Pets;
using Forge.Game.Ui;
using Forge.Game.Voxel;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T399 — 승천 데코가 정본의 세 자리(전투 펫 9779 · 전투 탈것 10820/10982 · 얼굴 썸네일 9236/9242)에 실제로 서는가:
    /// 별 3 펫은 데코 층 3(마커 1·2·3) · 별 0 은 없음 · 별 6 은 순환이라 없음 · 탈것 별 5 는 층 5 · 얼굴 캐시 키에 `:a<tier>` · 데코는 스케일 전 원본 위(몸 메시 뿌리의 자식).
    /// </summary>
    public class AscendDecorSceneTests
    {
        static IEnumerator Boot()
        {
            BattleScene.AutoBoot = true; PetParty.AutoBoot = true; MountRider.AutoBoot = true;
            if (BattleScene.Instance != null) Object.Destroy(BattleScene.Instance.gameObject);
            if (PetParty.Instance != null) Object.Destroy(PetParty.Instance.gameObject);
            if (MountRider.Instance != null) Object.Destroy(MountRider.Instance.gameObject);
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t0 = Time.realtimeSinceStartup;
            while ((MountRider.Instance == null || !MountRider.Instance.Ready || PetParty.Instance == null || !PetParty.Instance.Ready) && Time.realtimeSinceStartup - t0 < 30f) yield return null;
            Assert.IsTrue(PetParty.Instance != null && PetParty.Instance.Ready, "PetParty 가 30초 안에 준비되지 않았다");
            Assert.IsTrue(MountRider.Instance != null && MountRider.Instance.Ready, "MountRider 가 30초 안에 준비되지 않았다");
        }

        static int[] Layers(AscendDecorRoot d)
        {
            var set = new SortedSet<int>();
            if (d != null) foreach (AscendDecorTag t in d.GetComponentsInChildren<AscendDecorTag>(true)) set.Add(t.Layer);
            var arr = new int[set.Count]; set.CopyTo(arr); return arr;
        }

        [UnityTest]
        public IEnumerator 전투_펫은_별_수대로_데코_층이_서고_별_0과_6승천은_없다()
        {
            yield return Boot();
            PetParty pp = PetParty.Instance;
            BattleScene.Instance.ManualStep = true;
            pp.Set(new List<PetSlot> { new PetSlot("Cat", "common", 3), new PetSlot("Dog", "epic", 0), new PetSlot("Bear", "mythic", 6) });
            Assert.AreEqual(3, pp.Pets.Count);
            PetView cat = pp.Pets[0], dog = pp.Pets[1], bear = pp.Pets[2];
            Assert.IsNotNull(cat.Decor, "별 3 펫은 데코 뿌리가 있다");
            Assert.AreEqual(3, cat.Decor.Tier);
            Assert.AreEqual(new[] { 1, 2, 3 }, Layers(cat.Decor), "층 마커 1·2·3(누적 단조)");
            Assert.AreEqual(3, cat.Decor.LayerCount);
            Assert.AreEqual(cat.Mesh, cat.Decor.transform.parent, "데코는 스케일이 걸리는 몸 메시 뿌리의 자식(정본 root.add(deco))");
            Assert.AreSame(cat.Decor, AscendDecor.Of(cat.Mesh));
            Assert.AreEqual(1, cat.Decor.transform.Find("band").GetComponent<AscendDecorTag>().Layer);
            Assert.AreEqual(8, cat.Decor.GetComponentsInChildren<AscendDecorTag>(true).Length, "밴드 1 + 가시 6 + 룬 1 = 8 조각");
            Assert.IsNull(dog.Decor, "별 0 은 데코 없음"); Assert.IsNull(AscendDecor.Of(dog.Mesh));
            Assert.IsNull(bear.Decor, "6승천은 0승천 디자인으로 순환 — 데코 없음");
            // 데코 조각이 몸 안쪽이 아니라 몸 둘레에 있다 — 밴드 칸의 로컬 x 범위가 몸 폭보다 넓다(r×1.05).
            // 경계 상자는 **얹던 순간(정지 자세)** 의 것(뿌리에 적어 둔다) — PetParty.Refresh 가 세운 직후 Step 으로 관절을 돌리므로
            // 지금 다시 재면 몇 mm 다르다(런 782 · 0.1939 ↔ 0.1961). 정본도 부른 순간의 bbox 를 쓰고 그 뒤 안 옮긴다.
            double[] min = cat.Decor.BoundsMin, max = cat.Decor.BoundsMax;
            Assert.IsNotNull(min); Assert.IsNotNull(max);
            double r = System.Math.Max(max[0] - min[0], max[2] - min[2]) * 0.5;
            Assert.AreEqual(r, cat.Decor.R, 1e-9, "r = max(sx, sz) / 2");
            Assert.Greater(r, 0, "몸 경계 상자");
            Transform band = cat.Decor.transform.Find("band");
            Bounds bb = band.GetComponent<MeshFilter>().sharedMesh.bounds;
            Assert.Greater(bb.extents.x, r * 0.9f, "밴드 반지름 ≈ r×1.05 — 몸 둘레를 두른다");
            Assert.AreEqual(min[1] + (max[1] - min[1]) * 0.55, cat.Decor.BandY, 1e-9, "bandY = min.y + h×.55");
            Assert.AreEqual((float)cat.Decor.BandY, band.localPosition.y, 1e-5f, "밴드는 bandY 에 선다");
            // 정지 자세로 돌려 다시 재면 얹던 순간과 같은 상자다(관절만 돌았을 뿐 몸은 그대로)
            foreach (var j in cat.Rig.Joints) j.Set(j.Base);
            var mfs = new List<MeshFilter>(cat.Rig.Meshes);
            double[] min2, max2;
            Assert.IsTrue(AscendDecor.LocalBoundsThree(cat.Mesh, mfs, out min2, out max2));
            Assert.AreEqual(max[1] - min[1], max2[1] - min2[1], 0.02, "정지 자세 높이 ≈ 얹던 순간의 높이");
        }

        [UnityTest]
        public IEnumerator 탄_탈것과_무리는_별대로_층이_서고_별_5는_왕관까지다()
        {
            yield return Boot();
            MountRider mr = MountRider.Instance;
            BattleScene.Instance.ManualStep = true;
            mr.Set(new MountSlot("Pony", "epic", 5), new List<MountSlot> { new MountSlot("Mini Dragon", "legendary", 2), new MountSlot("Bike", "rare", 0) });
            Assert.IsTrue(mr.Riding); Assert.IsNotNull(mr.Ridden);
            Assert.IsNotNull(mr.Ridden.Decor, "별 5 탈것");
            Assert.AreEqual(new[] { 1, 2, 3, 4, 5 }, Layers(mr.Ridden.Decor));
            Assert.AreEqual(mr.Ridden.Mesh, mr.Ridden.Decor.transform.parent, "배율이 걸리는 메시 뿌리의 자식(스케일 전 원본 치수)");
            Assert.IsNotNull(mr.Ridden.Decor.transform.Find("crown-ring"), "왕관 링"); Assert.IsNotNull(mr.Ridden.Decor.transform.Find("crown-horn4"), "뿔 5");
            Assert.IsNotNull(mr.Ridden.Decor.transform.Find("rune"), "룬 링");
            Assert.AreEqual(2, mr.Followers.Count);
            Assert.AreEqual(new[] { 1, 2 }, Layers(mr.Followers[0].Decor), "무리 별 2 = 밴드 + 가시");
            Assert.IsNull(mr.Followers[1].Decor, "무리 별 0");
        }

        [UnityTest]
        public IEnumerator 얼굴_썸네일은_별마다_다른_키로_굽고_데코를_얹은_채_찍는다()
        {
            yield return Boot();
            Assert.AreEqual("Pets:Cat:a0", PetFaces.Key("Cat", GalleryKind.Pets, 0));
            Assert.AreEqual("Pets:Cat:a3", PetFaces.Key("Cat", GalleryKind.Pets, 3));
            Assert.AreEqual("Pets:Cat:a0", PetFaces.Key("Cat", GalleryKind.Pets, 6), "6승천 = a0(순환)");
            Assert.AreEqual("Mounts:Pony:a5", PetFaces.Key("Pony", GalleryKind.Mounts, 5));
            if (!PetFaces.Available) { Debug.Log("[AscendDecorSceneTests] 그래픽 장치 없음 — 굽기는 못 본다(키만 쟀다)"); yield break; }
            Sprite a0 = PetFaces.Get("Cat", GalleryKind.Pets, 0), a3 = PetFaces.Get("Cat", GalleryKind.Pets, 3);
            Assert.IsNotNull(a0); Assert.IsNotNull(a3);
            Assert.AreNotSame(a0, a3, "별이 다르면 다른 스프라이트");
            Assert.AreEqual("petface:Pets:Cat:a3", a3.name);
            Assert.AreSame(a3, PetFaces.Get("Cat", GalleryKind.Pets, 3), "같은 키는 캐시");
            // 두 그림이 실제로 다르다(데코 화소)
            var t0 = a0.texture.GetPixels32(); var t3 = a3.texture.GetPixels32();
            int diff = 0;
            for (int i = 0; i < t0.Length; i++) if (t0[i].r != t3[i].r || t0[i].g != t3[i].g || t0[i].b != t3[i].b || t0[i].a != t3[i].a) diff++;
            Assert.Greater(diff, 0, "별 3 얼굴은 별 0 얼굴과 화소가 다르다(데코가 찍혔다)");
        }
    }
}
