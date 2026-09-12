using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>
    /// T31 — 원작 아이콘 아틀라스 표(<c>Assets/Forge/Icons/Resources/Icons/atlas.json</c> · <c>tools/export_icons.js</c> 가 정본을 Chromium 으로 그려 낸 것)가
    /// 규약대로 서는가. 순수 C#(dotnet 하니스에서도 돈다) — 그림을 실제로 붙이는 검사는 PlayMode <c>UiIconsTests</c>.
    /// </summary>
    public class IconAtlasTests
    {
        static string _json;
        static string Json
        {
            get
            {
                if (_json != null) return _json;
                string dataDir = DataDir.Path; // <root>/Assets/StreamingAssets/data
                string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(dataDir)));
                string file = Path.Combine(root, "Assets", "Forge", "Icons", "Resources", IconAtlas.ResourceDir, IconAtlas.JsonFile);
                if (!File.Exists(file)) throw new FileNotFoundException("아이콘 아틀라스가 없다 — tools/check_icons_sync.sh .wwwww-src --sync: " + file);
                _json = File.ReadAllText(file);
                return _json;
            }
        }
        static IconAtlas _atlas;
        static IconAtlas Atlas { get { return _atlas ?? (_atlas = IconAtlas.Parse(Json)); } }

        [Test]
        public void 아틀라스가_서고_키_160_이상_겹침_0()
        {
            IconAtlas a = Atlas;
            Assert.GreaterOrEqual(a.Entries.Count, 160, "정본 IconGen.draw 아이콘 136 + 아바타 24 (+tint 변형)");
            Assert.GreaterOrEqual(a.Sheets.Count, 1);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (IconAtlasEntry e in a.Entries) Assert.IsTrue(seen.Add(e.Key), "키가 겹친다: " + e.Key);
            int avatars = 0;
            foreach (IconAtlasEntry e in a.Entries) if (e.Key.StartsWith("avatar_", StringComparison.Ordinal)) avatars++;
            Assert.AreEqual(24, avatars, "아바타 24종(정본 AVATAR_POOL)");
        }

        [Test]
        public void rect_는_장_안쪽이고_유니티_y_는_아래에서_센다()
        {
            IconAtlas a = Atlas;
            foreach (IconAtlasEntry e in a.Entries)
            {
                IconAtlasSheet s = a.Sheets[e.Atlas];
                Assert.LessOrEqual(s.W, 2048, "유니티 maxTextureSize 2048(gen_meta .png 메타)");
                Assert.LessOrEqual(s.H, 2048);
                Assert.IsTrue(e.X >= 0 && e.Y >= 0 && e.X + e.W <= s.W && e.Y + e.H <= s.H, e.Key + " 가 장 밖");
                int by = a.BottomUpY(e);
                Assert.IsTrue(by >= 0 && by + e.H <= s.H, e.Key + " 뒤집은 y 가 장 밖");
                Assert.AreEqual(s.H, by + e.H + e.Y, "위에서 센 y + 아래에서 센 y + 높이 = 장 높이");
            }
        }

        [Test]
        public void 원작_키_이름이_그대로다()
        {
            IconAtlas a = Atlas;
            foreach (string k in new[] { "coin", "gem", "hammer", "xmark", "star", "check", "tab_pvp", "tab_dungeon", "tab_summon", "tab_quest", "tab_shop", "tab_debug",
                "sk_powerStrike", "sk_apocalypse", "slot_weapon", "wpn_sword", "dg_hammer", "shop_gems1", "age_primitive", "age_divine", "leagueEmblem", "gender_m", "gender_f", "tri_left", "tri_right" })
                Assert.IsTrue(a.Has(k), "정본 IconGen 키가 없다: " + k);
            // tint 변형 = 원작 UI 가 실제로 부르는 조합(등급색 알 6 · 자동 제련 체크 · 파란 삼각형 2 · 기술 발바닥)
            foreach (string k in new[] { "egg|#e0e0e0", "egg|#1cafff", "egg|#1cff41", "egg|#f8ff1c", "egg|#ff1c1c", "egg|#aa1cff", "check|#23c552", "tri_left|#005dff", "tri_right|#005dff", "paw|#e8544a" })
            {
                IconAtlasEntry e;
                Assert.IsTrue(a.TryGet(k, out e), "tint 변형이 없다: " + k);
                Assert.AreEqual(k.Split('|')[0], e.Name);
                Assert.AreEqual(k.Split('|')[1], e.Tint);
            }
            Assert.IsNotNull(a.Find("egg", "#1CAFFF"), "tint 는 대소문자를 안 가린다(정본은 소문자 리터럴)");
            Assert.IsNull(a.Find("egg", "#123456"), "원작이 안 부르는 tint 는 없다");
            Assert.IsNull(a.Find("no_such_icon"), "없는 키는 null(정본 url() 의 빈 문자열)");
        }

        [Test]
        public void 크기가_정본_굽기_규약이다()
        {
            IconAtlas a = Atlas;
            IconAtlasEntry coin = a.Find("coin"), xmark = a.Find("xmark"), egg = a.Find("egg"), av = a.Find("avatar_1f6e1_fe0f"), dg = a.Find("dg_hammer");
            Assert.AreEqual(160, coin.W, "블록화 BLOCK.CELLS 20 × PX 8");
            Assert.AreEqual(160, coin.H);
            Assert.AreEqual(320, xmark.W, "BLOCK_CELLS.xmark 40 × 8");
            Assert.AreEqual(128, egg.W, "알은 BLOCK_SKIP — 굽기 해상도 S=128 그대로");
            Assert.AreEqual(128, egg.Size);
            Assert.AreEqual(160, av.W, "아바타 도트는 SIZES.avatar_ 160 (40칸×4)");
            Assert.AreEqual(160, av.Size);
            Assert.AreEqual(200, dg.Size, "던전 배너 SIZES.dg_ 200");
            Assert.Greater(dg.W, dg.H * 3, "던전 배너 ASPECT 3.45");
        }

        [Test]
        public void 아바타_키는_원작_avatarKey_와_같다()
        {
            Assert.AreEqual("avatar_1f6e1_fe0f", IconAtlas.AvatarKey("🛡️"), "DEFAULT_AVATAR 🛡️ = U+1F6E1 U+FE0F");
            Assert.AreEqual("avatar_1f9d1_200d_1f680", IconAtlas.AvatarKey("🧑‍🚀"), "ZWJ 결합은 코드포인트마다 한 조각");
            Assert.AreEqual("avatar_1f383", IconAtlas.AvatarKey("🎃"));
            Assert.IsTrue(Atlas.Has(IconAtlas.AvatarKey("🛡️")), "기본 아바타가 아틀라스에 있다");
            Assert.IsTrue(Atlas.Has(IconAtlas.AvatarKey("🧑‍🌾")), "농부(ZWJ) 아바타가 아틀라스에 있다");
            Assert.Throws<ArgumentException>(() => IconAtlas.AvatarKey(""));
        }

        [Test]
        public void Key_는_원작_캐시_키_꼴이다()
        {
            Assert.AreEqual("coin", IconAtlas.Key("coin", null));
            Assert.AreEqual("coin", IconAtlas.Key("coin", ""));
            Assert.AreEqual("egg|#1cafff", IconAtlas.Key("egg", "#1CAFFF"));
        }

        [Test]
        public void 깨진_표는_예외다()
        {
            const string sheet = "\"pad\":2,\"atlases\":[{\"file\":\"atlas-0.png\",\"w\":64,\"h\":64}],";
            Assert.Throws<FormatException>(() => IconAtlas.Parse("{" + sheet + "\"icons\":[{\"key\":\"a\",\"name\":\"a\",\"tint\":null,\"atlas\":0,\"x\":0,\"y\":0,\"w\":8,\"h\":8,\"size\":8},{\"key\":\"a\",\"name\":\"a\",\"tint\":null,\"atlas\":0,\"x\":8,\"y\":0,\"w\":8,\"h\":8,\"size\":8}]}"), "겹친 키");
            Assert.Throws<FormatException>(() => IconAtlas.Parse("{" + sheet + "\"icons\":[{\"key\":\"a\",\"name\":\"a\",\"tint\":null,\"atlas\":0,\"x\":60,\"y\":0,\"w\":8,\"h\":8,\"size\":8}]}"), "장 밖 rect");
            Assert.Throws<FormatException>(() => IconAtlas.Parse("{" + sheet + "\"icons\":[{\"key\":\"a\",\"name\":\"a\",\"tint\":null,\"atlas\":1,\"x\":0,\"y\":0,\"w\":8,\"h\":8,\"size\":8}]}"), "없는 장");
            Assert.Throws<FormatException>(() => IconAtlas.Parse("{" + sheet + "\"icons\":[{\"key\":\"a|#ff0000\",\"name\":\"a\",\"tint\":null,\"atlas\":0,\"x\":0,\"y\":0,\"w\":8,\"h\":8,\"size\":8}]}"), "키 ≠ name|tint");
            Assert.Throws<FormatException>(() => IconAtlas.Parse("{" + sheet + "\"icons\":[]}"), "아이콘 0");
            IconAtlas ok = IconAtlas.Parse("{" + sheet + "\"icons\":[{\"key\":\"a\",\"name\":\"a\",\"tint\":null,\"atlas\":0,\"x\":0,\"y\":48,\"w\":8,\"h\":16,\"size\":8}]}");
            Assert.AreEqual(0, ok.BottomUpY(ok.Find("a")), "PNG 맨 아래 칸은 유니티 y 0");
        }
    }
}
