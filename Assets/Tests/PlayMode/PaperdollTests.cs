using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Forge.Core.Data;
using Forge.Core.Forging;
using Forge.Core.Gear;
using Forge.Game.Gallery;
using Forge.Game.Hero;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T37 — 장비 3D 외형 캡처(`gear-meshes.json`)를 유니티가 예외 0 으로 세우는가 · 정점 수 = 캡처(정본 `Voxel.build`/BoxGeometry/RingGeometry 정점 수 — T4 빌더 대조) ·
    /// 영웅에 입히면 무기 재질·투구(head·helmetMount)·옷 한 벌(spine/shoulder/hip 본)이 붙고 벗기면 걷히는가 · 시트(`ui-screens/gear_*.png`)를 남긴다.
    /// 빨간 로그가 나면 러너가 실패시킨다.
    /// </summary>
    public class PaperdollTests
    {
        static GameData _data;
        static GameData Data
        {
            get { return _data ?? (_data = GameData.LoadDirectory(System.IO.Path.Combine(Application.streamingAssetsPath, "data"))); }
        }
        static GearMeshes _gear;
        static GearMeshes Gear { get { return _gear ?? (_gear = GearMeshes.LoadFromStreamingAssets()); } }

        static int NameCount(string slot)
        {
            int n = 0;
            foreach (var kv in Data.Defs.ItemNames) { string[] arr; if (kv.Value.TryGet(slot, out arr)) n += arr.Length; }
            return n;
        }

        [Test]
        public void 외형_표를_읽는다()
        {
            var g = Gear;
            Assert.AreEqual(Data.Defs.WeaponTypes.Count, g.Weapons.Count, "무기 = WEAPON_TYPES 전부");
            foreach (var kv in Data.Defs.WeaponTypes) Assert.IsNotNull(g.Weapon(kv.Key), "무기 " + kv.Key);
            Assert.AreEqual(NameCount("helmet"), g.Helmets.Count, "투구 = ITEM_NAMES 투구 이름 수");
            Assert.AreEqual(NameCount("armor"), g.Armors.Count, "갑옷 = ITEM_NAMES 갑옷 이름 수");
            CollectionAssert.AreEqual(Data.Defs.Rarities, g.Rarities, "등급 6 순서");
            Assert.AreEqual("head", g.HelmetMountBone); Assert.AreEqual(3, g.HelmetMountPos.Length); Assert.Greater(g.HelmetMountPos[1], 0, "helmetMount y 0.08");
            Assert.Greater(g.Mats.Count, 0); Assert.Greater(g.CellSets.Count, 0);
            Assert.AreEqual(5, g.DecorTiers);
            Assert.AreEqual(0, GearMeshes.AscendTier(0)); Assert.AreEqual(5, GearMeshes.AscendTier(5)); Assert.AreEqual(0, GearMeshes.AscendTier(6)); Assert.AreEqual(1, GearMeshes.AscendTier(7));
            var sword = g.Weapon("sword");
            Assert.AreEqual("medieval", sword.Age); Assert.AreEqual(1, sword.AgeIdx);
            Assert.IsNotNull(g.Helmet("medieval", 0)); Assert.IsNotNull(g.Armor("divine", 4)); Assert.IsNull(g.Helmet(null, 0));
            Assert.AreSame(g.Helmet("medieval", 0), g.Helmet("medieval", 99), "인덱스가 어긋나면 그 시대 첫 것");
        }

        static void CheckBuilt(GearBuilt b, GearGeom geom, int tier, string label)
        {
            Assert.IsNotNull(b.Mesh, label);
            Assert.AreEqual(GearMeshes.ExpectedVerts(geom, tier), b.VertexCount, label + " 정점 수 = 캡처(정본 Voxel.build·Box·Ring 정점 수)");
            Assert.AreEqual(b.Mesh.subMeshCount, b.Materials.Length, label + " 서브메시 = 재질");
            Assert.Greater(b.Materials.Length, 0, label);
            foreach (var m in b.Materials) Assert.IsNotNull(m, label + " 재질");
            var s = b.Mesh.bounds.size;
            Assert.IsTrue(s.x > 0 && s.y > 0 && s.z > 0 && s.x < 5 && s.y < 5 && s.z < 5, label + " 경계 " + s);
        }

        [UnityTest]
        public IEnumerator 무기_전종_등급_전부_세운다()
        {
            var g = Gear;
            int n = 0, verts = 0;
            foreach (var kv in g.Weapons)
            {
                foreach (string r in g.Rarities)
                {
                    int stars = r == "common" ? 5 : 0;              // 데코 5층은 등급 하나에서(전 종) · 나머지는 본체만
                    var row = kv.Value.Rarity(r);
                    var b = g.BuildModel(kv.Value, r, true, stars);
                    CheckBuilt(b, kv.Value.Geoms[row.G], GearMeshes.AscendTier(stars), "무기 " + kv.Key + "/" + r);
                    n++; verts += b.VertexCount;
                    Object.Destroy(b.Mesh);
                }
                if (n % 60 == 0) yield return null;
            }
            Assert.AreEqual(g.Weapons.Count * g.Rarities.Length, n);
            Assert.Greater(verts, 100000, "무기 정점 합");
            // 등급 없음(맨손 club) = common 과 같은 지오메트리
            var club = g.Weapon("club");
            Assert.AreSame(club.Rarity(null), club.Rarity("common"));
            Assert.AreNotEqual(club.Rarity("common").G, club.Rarity("mythic").G, "club 은 신화에서 젬·오브가 붙어 변형이 갈린다");
        }

        [UnityTest]
        public IEnumerator 투구_갑옷_전종_세운다()
        {
            var g = Gear;
            int n = 0;
            foreach (var kv in g.Helmets)
            {
                foreach (string r in g.Rarities)
                {
                    int stars = r == "common" ? 5 : 0;
                    var row = kv.Value.Rarity(r);
                    var b = g.BuildModel(kv.Value, r, true, stars);
                    CheckBuilt(b, kv.Value.Geoms[row.G], GearMeshes.AscendTier(stars), "투구 " + kv.Key + "/" + r);
                    Object.Destroy(b.Mesh); n++;
                }
                if (n % 60 == 0) yield return null;
            }
            var bones = new HashSet<string> { "spine", "shoulderL", "shoulderR", "hipL", "hipR" };
            foreach (var kv in g.Armors)
            {
                foreach (string r in g.Rarities)
                {
                    var row = kv.Value.Rarity(r);
                    var pieces = g.BuildArmor(kv.Value, r);
                    Assert.GreaterOrEqual(pieces.Count, 3, "갑옷 " + kv.Key + " 조각(몸통·바지 2·소매)");
                    int expect = GearMeshes.ExpectedVerts(kv.Value.Geoms[row.G], 0), got = 0;
                    foreach (var pc in pieces)
                    {
                        Assert.IsTrue(bones.Contains(pc.Bone), "갑옷 " + kv.Key + " 본 " + pc.Bone);
                        Assert.IsNotNull(pc.Built.Mesh); got += pc.Built.VertexCount;
                        Assert.AreEqual(pc.Built.Mesh.subMeshCount, pc.Built.Materials.Length);
                        Object.Destroy(pc.Built.Mesh);
                    }
                    Assert.AreEqual(expect, got, "갑옷 " + kv.Key + "/" + r + " 정점 수 = 캡처");
                    n++;
                }
                if (n % 60 == 0) yield return null;
            }
            Assert.AreEqual((g.Helmets.Count + g.Armors.Count) * g.Rarities.Length, n);
        }

        static ForgeItem Item(string slot, string age, int ageIdx, string rarity, int nameIdx, int stars, string wtype = null)
        {
            return new ForgeItem { Slot = slot, Age = age, AgeIdx = ageIdx, Rarity = rarity, NameIdx = nameIdx, Stars = stars, WType = wtype, Level = 1, Main = slot == "weapon" ? "atk" : "hp", Value = 1 };
        }

        [UnityTest]
        public IEnumerator 영웅에_입힌다_무기_투구_옷_그리고_벗긴다()
        {
            var g = Gear;
            var rig = HeroRig.Create(null, "HeroT37");
            rig.ManualStep = true;
            g.Install();
            try
            {
                Assert.AreSame(g, GearMeshes.Installed);
                var state = new GearState();
                state.Set("weapon", Item("weapon", "medieval", 1, "rare", 0, 2, "sword"));
                state.Set("helmet", Item("helmet", "medieval", 1, "epic", 0, 0));
                state.Set("armor", Item("armor", "medieval", 1, "ultimate", 0, 0));
                var look = Paperdoll.Refresh(rig, Data.Defs, state);
                yield return null;
                Assert.AreEqual("sword", look.WtypeId);
                // 무기: 캡처 메시 + 재질 배열(서브메시 수)
                Assert.AreEqual(1, rig.WeaponMount.childCount);
                var wmf = rig.WeaponMount.GetChild(0).GetComponent<MeshFilter>();
                var wmr = rig.WeaponMount.GetChild(0).GetComponent<MeshRenderer>();
                Assert.AreNotSame(HeroWeapon.Stick(), wmf.sharedMesh, "막대가 아니라 캡처 메시");
                var sword = g.Weapon("sword");
                Assert.AreEqual(GearMeshes.ExpectedVerts(sword.Geoms[sword.Rarity("rare").G], 2), wmf.sharedMesh.vertexCount, "rare 검 + 승천 2층");
                Assert.AreEqual(wmf.sharedMesh.subMeshCount, wmr.sharedMaterials.Length, "무기 재질 배열");
                Assert.Greater(wmr.sharedMaterials.Length, 1, "검은 재질이 여럿(날·자루·젬)");
                // 투구: head 본 아래 helmetG · helmetMount(0, 0.08, 0)
                var head = rig.Bone("head");
                Assert.AreEqual(1, GearMeshes.CountNamed(head, GearMeshes.HelmetNode));
                var helm = head.Find(GearMeshes.HelmetNode);
                Assert.AreEqual(new Vector3(0, 0.08f, 0), helm.localPosition);
                Assert.Greater(helm.GetComponent<MeshFilter>().sharedMesh.vertexCount, 0);
                // 옷 한 벌: 관절 본마다 mcCloth
                int cloth = 0;
                foreach (var kv in rig.Bones) cloth += GearMeshes.CountNamed(kv.Value, GearMeshes.ClothNode);
                Assert.GreaterOrEqual(cloth, 3, "옷 조각(몸통·바지 2·소매)");
                Assert.AreEqual(1, GearMeshes.CountNamed(rig.Bone("spine"), GearMeshes.ClothNode), "몸통은 spine 본");
                Assert.AreEqual(1, GearMeshes.CountNamed(rig.Bone("hipL"), GearMeshes.ClothNode), "왼 바지는 hipL 본");
                // 갈아입기: 이전 것이 걷힌다
                state.Set("armor", Item("armor", "divine", 9, "mythic", 4, 0));
                state.Set("helmet", null);
                Paperdoll.Refresh(rig, Data.Defs, state);
                yield return null;
                Assert.AreEqual(0, GearMeshes.CountNamed(head, GearMeshes.HelmetNode), "투구를 벗으면 helmetG 없음");
                int cloth2 = 0;
                foreach (var kv in rig.Bones) cloth2 += GearMeshes.CountNamed(kv.Value, GearMeshes.ClothNode);
                Assert.GreaterOrEqual(cloth2, 3); Assert.AreEqual(1, GearMeshes.CountNamed(rig.Bone("spine"), GearMeshes.ClothNode), "옷은 한 벌만");
                // 맨몸: 무기 = club 캡처(원작 `S.equipment.weapon` 없음 → 'club' · 등급 없음 = common 지오메트리) · 투구·옷 없음
                Paperdoll.Refresh(rig, Data.Defs, new GearState());
                yield return null;
                var club = g.Weapon("club");
                var cmf = rig.WeaponMount.GetChild(0).GetComponent<MeshFilter>();
                Assert.AreNotSame(HeroWeapon.Stick(), cmf.sharedMesh, "club 은 표에 있어 캡처 메시");
                Assert.AreEqual(GearMeshes.ExpectedVerts(club.Geoms[club.Rarity(null).G], 0), cmf.sharedMesh.vertexCount);
                int cloth3 = 0;
                foreach (var kv in rig.Bones) cloth3 += GearMeshes.CountNamed(kv.Value, GearMeshes.ClothNode);
                Assert.AreEqual(0, cloth3, "갑옷을 벗으면 옷 없음");
                // 훅을 빼면 T6 막대로 돌아간다
                GearMeshes.Uninstall();
                Paperdoll.Refresh(rig, Data.Defs, new GearState());
                yield return null;
                Assert.AreSame(HeroWeapon.Stick(), rig.WeaponMount.GetChild(0).GetComponent<MeshFilter>().sharedMesh, "훅 없음 = 막대");
            }
            finally
            {
                GearMeshes.Uninstall();
                Object.Destroy(rig.gameObject);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator 시트를_찍는다_무기_투구_갑옷()
        {
            if (!GallerySheet.GraphicsAvailable) { Assert.Ignore("그래픽 장치 없음(-nographics) — 시트는 못 찍는다"); yield break; }
            var g = Gear;
            var root = new GameObject("GearSheetRoot");
            var rig = GallerySheet.MakeRig(root.transform);
            try
            {
                var weapons = new List<GalleryEntry>();
                foreach (var kv in g.Weapons)
                {
                    var b = g.BuildModel(kv.Value, "common", false, 0);
                    weapons.Add(Entry(root.transform, "wpn " + kv.Key, b));
                }
                yield return null;
                Save(weapons, "gear_weapons", rig);
                foreach (var e in weapons) Object.Destroy(e.Root);
                var helmets = new List<GalleryEntry>();
                foreach (var kv in g.Helmets) helmets.Add(Entry(root.transform, "helm " + kv.Key, g.BuildModel(kv.Value, "common", false, 0)));
                yield return null;
                Save(helmets, "gear_helmets", rig);
                foreach (var e in helmets) Object.Destroy(e.Root);
                g.Install();
                var armors = new List<GalleryEntry>();
                foreach (var kv in g.Armors)
                {
                    var hero = HeroRig.Create(root.transform, "hero " + kv.Key);
                    hero.ManualStep = true;
                    var st = new GearState();
                    st.Set("armor", Item("armor", kv.Value.Age, System.Array.IndexOf(Data.Defs.Ages, kv.Value.Age), "common", kv.Value.NameIdx, 0));
                    Paperdoll.Refresh(hero, Data.Defs, st);
                    armors.Add(new GalleryEntry { Species = new GallerySpecies { Kind = GalleryKind.Pets, Name = kv.Key }, Root = hero.gameObject });
                }
                yield return null;
                Save(armors, "gear_armors", rig);
            }
            finally
            {
                GearMeshes.Uninstall();
                rig.Destroy();
                Object.Destroy(root);
            }
            yield return null;
        }

        static GalleryEntry Entry(Transform parent, string name, GearBuilt b)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = b.Mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = b.Materials;
            return new GalleryEntry { Species = new GallerySpecies { Kind = GalleryKind.Pets, Name = name }, Root = go };
        }

        static void Save(List<GalleryEntry> entries, string file, GallerySheet.Rig rig)
        {
            var sheet = GallerySheet.Compose(entries, GalleryKind.Pets, rig);
            Assert.IsNotNull(sheet, file);
            string path = GallerySheet.Save(sheet, file);
            Assert.IsTrue(System.IO.File.Exists(path), path);
            Object.Destroy(sheet);
        }
    }
}
