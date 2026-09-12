using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Forge.Core.Data;
using Forge.Game.Gallery;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T5 — JSON 의 모든 종(펫 25 · 탈것 29 · 적 7 · 스킬 오브젝트 20 · 소품 표본 23)을 세워 **예외 0 · 파츠 수 = 정본 · 경계 상자 = 정본 ±1칸** 을 잰다.
    /// 정본 값은 `tools/gallery_vectors.js` 가 실물 three r128 로 `Mobs.build` + `Box3.setFromObject` 를 돌려 뽑은 `Vectors/t5-bounds.json`(three 좌표 · z 는 부호 반전해 비교).
    /// 빨간 로그가 나면 러너가 실패시킨다. 그래픽 장치가 있으면 `ui-screens/mobs_*.png` 시트도 남긴다.
    /// </summary>
    public class MobGalleryTests
    {
        static JsonObject vectors;

        static JsonObject Vectors()
        {
            if (vectors != null) return vectors;
            string file = Path.Combine(Application.dataPath, "Tests", "PlayMode", "Vectors", "t5-bounds.json");
            vectors = MiniJson.ParseObject(File.ReadAllText(file));
            return vectors;
        }

        static double[] Vec3(object v)
        {
            var a = J.Arr(v);
            return new[] { J.Num(a[0]), J.Num(a[1]), J.Num(a[2]) };
        }

        static void AssertMatchesVector(GalleryEntry e, JsonObject v)
        {
            double cell = J.Num(v["cell"]);
            Assert.AreEqual(cell, e.Species.Cell, 1e-6, e.Species.Name + " 칸 크기(시트 규약)");
            Assert.AreEqual(J.Int(v["parts"]), e.PartCount, e.Species.Name + " 파츠 수 = 정본 메시 수");
            Assert.AreEqual(e.Rig.Plan.Parts.Count, e.PartCount, e.Species.Name + " 파츠 수 = 계획 파츠 수");
            double[] mn = Vec3(v["min"]), mx = Vec3(v["max"]);
            Bounds b = e.LocalBounds;
            float tol = (float)cell;
            Assert.AreEqual(mx[1] - mn[1], b.size.y, tol, e.Species.Name + " 높이 = 정본 ±1칸");
            Assert.AreEqual(mx[0] - mn[0], b.size.x, tol, e.Species.Name + " 폭 = 정본 ±1칸");
            Assert.AreEqual(mx[2] - mn[2], b.size.z, tol, e.Species.Name + " 깊이 = 정본 ±1칸");
            Assert.AreEqual((mx[1] + mn[1]) * 0.5, b.center.y, tol, e.Species.Name + " 중심 y");
            Assert.AreEqual((mx[0] + mn[0]) * 0.5, b.center.x, tol, e.Species.Name + " 중심 x");
            Assert.AreEqual(-(mx[2] + mn[2]) * 0.5, b.center.z, tol, e.Species.Name + " 중심 z(z 반전)");
        }

        static IEnumerator CheckTable(GalleryKind kind, int expectedCount)
        {
            GameData data = GalleryData.Load();
            var root = new GameObject("Gallery " + kind);
            List<GalleryEntry> entries = MobGallery.BuildAll(data, kind, root.transform);
            yield return null;
            Assert.AreEqual(expectedCount, entries.Count, kind + " 종 수(ROUTINE §1 조형 계약)");
            JsonObject table = J.Obj(Vectors()[MobGallery.TableKey(kind)]);
            Assert.IsNotNull(table, "t5-bounds.json 에 " + kind + " 표가 없다");
            Assert.AreEqual(entries.Count, table.Count, kind + " 정본 벡터 종 수");
            foreach (var e in entries)
            {
                Assert.IsNotNull(e.Root, e.Species.Name);
                Assert.Greater(e.PartCount, 0, e.Species.Name + " 파츠 0");
                JsonObject v = J.Obj(table[e.Species.Name]);
                Assert.IsNotNull(v, "정본 벡터에 없는 종: " + e.Species.Name);
                AssertMatchesVector(e, v);
            }
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest] public IEnumerator 펫_25종이_예외_0으로_서고_파츠수_경계상자가_정본과_같다() { yield return CheckTable(GalleryKind.Pets, 25); }
        [UnityTest] public IEnumerator 탈것_29종이_예외_0으로_서고_파츠수_경계상자가_정본과_같다() { yield return CheckTable(GalleryKind.Mounts, 29); }
        [UnityTest] public IEnumerator 적_7종이_예외_0으로_서고_파츠수_경계상자가_정본과_같다() { yield return CheckTable(GalleryKind.Enemies, 7); }
        [UnityTest] public IEnumerator 스킬_오브젝트_20종이_예외_0으로_서고_파츠수_경계상자가_정본과_같다() { yield return CheckTable(GalleryKind.SkillFx, 20); }

        [UnityTest]
        public IEnumerator 소품_표본_23개가_예외_0으로_서고_파츠수가_표본과_같다()
        {
            GameData data = GalleryData.Load();
            var root = new GameObject("Gallery Props");
            List<GalleryEntry> entries = MobGallery.BuildAll(data, GalleryKind.Props, root.transform);
            yield return null;
            Assert.AreEqual(data.Props.Samples.Count, entries.Count);
            Assert.AreEqual(23, entries.Count, "T2 표본 수");
            foreach (var e in entries)
            {
                int expected = 0;
                foreach (var p in e.Species.Prop.Parts) if (p != null && p.V != null && p.V.Count > 0) expected++;
                Assert.AreEqual(expected, e.PartCount, e.Species.Name + " 파츠 수");
                Bounds b = e.LocalBounds;
                Assert.Greater(b.size.y, 0f, e.Species.Name + " 높이 0");
                Assert.GreaterOrEqual(b.min.y, -(float)e.Species.Cell, e.Species.Name + " 바닥이 지면 아래로 내려갔다(vxProp: y = size × 0.5)");
            }
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 시트_다섯_장을_ui_screens_에_남긴다()
        {
            if (!GallerySheet.GraphicsAvailable)
            {
                Debug.LogWarning("그래픽 장치가 없다(-nographics) — 시트 촬영을 건너뛴다");
                yield break;
            }
            GameData data = GalleryData.Load();
            var root = new GameObject("Gallery Sheets");
            GallerySheet.Rig rig = GallerySheet.MakeRig(root.transform);
            foreach (var kind in MobGallery.AllKinds)
            {
                var block = new GameObject(kind.ToString());
                block.transform.SetParent(root.transform, false);
                List<GalleryEntry> entries = MobGallery.BuildAll(data, kind, block.transform);
                yield return null;
                Texture2D sheet = GallerySheet.Compose(entries, kind, rig);
                Assert.IsNotNull(sheet, kind + " 시트");
                string file = GallerySheet.Save(sheet, MobGallery.FileName(kind));
                Assert.IsTrue(File.Exists(file), file);
                Assert.Greater(new FileInfo(file).Length, 1000, file + " 가 비었다");
                Object.Destroy(sheet);
                Object.Destroy(block);
                yield return null;
            }
            rig.Destroy();
            Object.Destroy(root);
            yield return null;
        }
    }
}
