using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core;
using Forge.Core.Battle;
using Forge.Core.Data;
using Forge.Core.Forging;
using Forge.Core.Gear;
using Forge.Core.Pets;

namespace Forge.Tests
{
    /// <summary>
    /// T15 — `Core/Gear` 를 정본 실행 벡터(`Assets/Tests/EditMode/Vectors/t15-gear.json` · `tools/gear_vectors.js` 가 정본 forge.js 를 node vm 에서 돌려 뽑은 것)와 대조한다.
    /// 호스트 값(별 배율 · 기술트리 배율 · 펫/탈것/스킬 보너스 · 버프)은 벡터에 적힌 그대로 <see cref="GearHost"/> 에 꽂는다.
    /// </summary>
    public class GearTests
    {
        static JsonObject _doc;
        static JsonObject Doc
        {
            get
            {
                if (_doc != null) return _doc;
                string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
                string file = Path.Combine(root, "Assets", "Tests", "EditMode", "Vectors", "t15-gear.json");
                if (!File.Exists(file)) throw new FileNotFoundException("T15 벡터가 없다 — node tools/gear_vectors.js: " + file);
                _doc = MiniJson.ParseObject(File.ReadAllText(file));
                return _doc;
            }
        }

        static GameData G { get { return DataDir.Game; } }
        static double StarMultConst { get { return J.Num(J.Obj(Doc["consts"])["STAR_MULT"]); } }
        static Big StarMult(int stars) { return stars <= 0 ? Big.One : Big.Of(StarMultConst).Pow(stars); }

        static void AssertBig(string want, Big got, string at)
        {
            if (want == got.ToString()) return;
            Big w = Big.Parse(want);
            Assert.AreEqual(w.E, got.E, at + " 지수 (want " + want + " got " + got + ")");
            Assert.AreEqual(w.M, got.M, Math.Abs(w.M) * 1e-12 + 1e-15, at + " 가수 (want " + want + " got " + got + ")");
        }

        static GearHost HostOf(JsonObject scene)
        {
            var h = new GearHost { StarMultFn = StarMult };
            if (scene == null) return h;
            var bonus = J.Obj(scene["bonus"]);
            if (bonus != null)
            {
                var pets = J.Obj(bonus["pets"]); var mounts = J.Obj(bonus["mounts"]); var skills = J.Obj(bonus["skills"]);
                h.PetAtk = Big.Of(J.Num(pets["atk"])); h.PetHp = Big.Of(J.Num(pets["hp"])); h.PetSubs = SubsOf(J.Arr(pets["subs"]));
                h.MountAtk = Big.Of(J.Num(mounts["atk"])); h.MountHp = Big.Of(J.Num(mounts["hp"])); h.MountSubs = SubsOf(J.Arr(mounts["subs"]));
                h.SkillAtk = Big.Of(J.Num(skills["atk"])); h.SkillHp = Big.Of(J.Num(skills["hp"]));
            }
            var tech = J.Obj(scene["tech"]);
            if (tech != null)
            {
                h.SellPriceMult = 1 + J.Num(tech["sellPrice"]) / 100;
                h.GearAtkMult = 1 + J.Num(tech["weaponMastery"]) / 100;
                h.GearHpMult = 1 + J.Num(tech["armorMastery"]) / 100;
            }
            var buffs = J.Arr(scene["buffs"]);
            Big sum = Big.Zero;
            if (buffs != null) foreach (var b in buffs) if (b is string) sum = sum.Add(Big.Parse((string)b));
            h.BuffAtkFlat = sum;
            return h;
        }

        static List<Substat> SubsOf(List<object> arr)
        {
            var l = new List<Substat>();
            if (arr != null) foreach (var o in arr) { var s = J.Obj(o); l.Add(new Substat(J.Str(s["key"]), J.Str(s["label"]), J.Num(s["value"]))); }
            return l;
        }

        static GearState StateOf(JsonObject equipment)
        {
            return GearCodec.StateFrom(equipment, G.Defs);
        }

        [Test]
        public void 상수와_표()
        {
            var c = J.Obj(Doc["consts"]);
            Assert.AreEqual(J.StrArr(c["SLOTS"]), G.Defs.Slots);
            var rm = J.NumMap(c["RARITY_MULT"]);
            foreach (var k in rm.Keys) Assert.AreEqual(rm[k], G.Defs.RarityMult[k], k);
            var sm = J.StrMap(c["SLOT_MAIN"]);
            foreach (var k in sm.Keys) Assert.AreEqual(sm[k], G.Defs.SlotMain[k], k);
            var keys = J.StrArr(c["SUBSTAT_KEYS"]);
            Assert.AreEqual(keys.Length, G.Defs.Substats.Count);
            for (int i = 0; i < keys.Length; i++) Assert.AreEqual(keys[i], G.Defs.Substats[i].Key, "SUBSTATS 순서 " + i);
            Assert.AreEqual(15, GearRules.BaseAtk); Assert.AreEqual(150, GearRules.BaseHp);
        }

        [Test]
        public void 벡터_items_72_가치_위력_판매가_스타일_이름()
        {
            var items = J.Arr(Doc["items"]);
            Assert.AreEqual(72, items.Count);
            ForgeItem prev = null;
            var host = new GearHost { StarMultFn = StarMult };
            var sys = new GearSystem(G, new GearState(), new Wallet(), host);
            foreach (var row0 in items)
            {
                var row = J.Obj(row0);
                var it = GearCodec.ItemFrom(J.Obj(row["item"]));
                string at = "item " + it.Name + "/" + it.Slot + "/" + it.Rarity + " L" + it.Level + " ★" + it.Stars;
                Assert.AreEqual(J.Int(row["stars"]), it.Stars, at + " stars");
                AssertBig(J.Str(row["itemValue"]), sys.ItemValue(it), at + " itemValue");
                AssertBig(J.Str(row["itemPower"]), sys.ItemPower(it), at + " itemPower");
                host.SellPriceMult = 1;
                Assert.AreEqual(J.Num(row["sellPrice"]), sys.SellPrice(it), at + " sellPrice");
                host.SellPriceMult = 1.3;
                Assert.AreEqual(J.Num(row["sellPrice130"]), sys.SellPrice(it), at + " sellPrice ×1.3");
                Assert.AreEqual(J.Bool(row["matchPrev"]), sys.IsMatchingGear(prev, it), at + " matchPrev");
                Assert.AreEqual(J.Str(row["style"]), PaperdollLook.ItemStyleOf(G.Defs, it), at + " style");
                Assert.AreEqual(J.Str(row["nameOf"]), PaperdollLook.ItemNameOf(G.Defs, it), at + " nameOf");
                var noName = GearCodec.ItemFrom(J.Obj(row["item"])); noName.Name = "";
                Assert.AreEqual(J.Str(row["nameOfNoName"]), PaperdollLook.ItemNameOf(G.Defs, noName), at + " nameOf(이름 없이 표에서)");
                prev = it;
            }
            var ms = J.Obj(Doc["matchSame"]);
            var a = GearCodec.ItemFrom(J.Obj(ms["a"])); var b = GearCodec.ItemFrom(J.Obj(ms["b"]));
            Assert.AreEqual(J.Bool(ms["match"]), GearRules.IsMatchingGear(a, b));
            Assert.IsTrue(J.Bool(ms["match"]), "같은 부위·등급·이름이면 레벨이 달라도 같은 장비");
            Assert.AreEqual(J.Bool(ms["selfNull"]), GearRules.IsMatchingGear(null, a));
        }

        [Test]
        public void 벡터_damaged_반쪽_항목은_지급을_건너뛴다()
        {
            var d = J.Obj(Doc["damaged"]);
            var wallet = new Wallet { Coins = 777 };
            var sys = new GearSystem(G, new GearState(), wallet, new GearHost { StarMultFn = StarMult });
            int refused = 0;
            sys.SellRefused += (it, p) => refused++;
            var half = new ForgeItem { Slot = "ring", Level = double.NaN };
            Assert.AreEqual("NaN", J.Str(d["sellPrice"]));
            Assert.IsTrue(double.IsNaN(sys.SellPrice(half)), "레벨·등급 없는 반쪽은 NaN");
            Assert.AreEqual(J.Num(d["sell"]), sys.Sell(half));
            Assert.AreEqual(J.Num(d["coinsAfter"]), wallet.Coins, "코인 불변");
            Assert.AreEqual(J.Int(d["errors"]), refused, "console.error 한 번 = SellRefused 한 번");
            AssertBig(J.Str(d["itemValueNull"]), sys.ItemValue(null), "itemValue(null)");
            AssertBig(J.Str(d["itemPowerNull"]), sys.ItemPower(null), "itemPower(null)");
            Assert.IsTrue(double.IsNaN(sys.SellPrice(new ForgeItem { Slot = "ring", Level = 5 })), "등급 없음 → NaN");
            Assert.AreEqual("NaN", J.Str(d["sellPriceNoRarity"]));
        }

        [Test]
        public void 벡터_equip_장착_판매_autoResolve_흐름()
        {
            var flow = J.Obj(Doc["equip"]);
            var pool = new List<ForgeItem>();
            foreach (var o in J.Arr(flow["pool"])) pool.Add(GearCodec.ItemFrom(J.Obj(o)));
            Assert.AreEqual(14, pool.Count);
            var quests = new List<string>(); var refresh = new List<bool>(); int recalc = 0;
            var host = new GearHost
            {
                StarMultFn = StarMult,
                OnQuestBump = (k, n) => quests.Add(k), OnRefreshHeroEquip = f => refresh.Add(f), OnRecalcHero = () => recalc++,
            };
            var wallet = new Wallet { Coins = 100 };
            var state = new GearState();
            var sys = new GearSystem(G, state, wallet, host);
            foreach (var s0 in J.Arr(flow["steps"]))
            {
                var s = J.Obj(s0);
                int i = J.Int(s["i"]);
                string op = J.Str(s["op"]);
                string at = op + " #" + i;
                if (op == "equip")
                {
                    ForgeItem prev = sys.Equip(pool[i]);
                    object wantPrev = s["prev"];
                    if (wantPrev == null) Assert.IsNull(prev, at + " prev 없음");
                    else Assert.AreSame(pool[J.Int(wantPrev)], prev, at + " prev = 같은 개체");
                    Assert.AreEqual(J.StrArr(s["quests"]), quests.ToArray(), at + " quests");
                    var wr = J.Arr(s["refresh"]);
                    Assert.AreEqual(wr.Count, refresh.Count, at + " refresh 횟수");
                    for (int k = 0; k < wr.Count; k++) Assert.AreEqual((bool)wr[k], refresh[k], at + " refresh withFlash");
                    Assert.AreEqual(J.Int(s["recalc"]), recalc, at + " recalc");
                    var eq = J.Obj(s["equipment"]);
                    foreach (var kv in eq)
                    {
                        ForgeItem got = state.Get(kv.Key);
                        if (kv.Value == null) Assert.IsNull(got, at + " " + kv.Key + " 비어야");
                        else Assert.AreSame(pool[J.Int(kv.Value)], got, at + " " + kv.Key);
                    }
                }
                else if (op == "sell")
                {
                    host.SellPriceMult = 1.15;
                    Assert.AreEqual(J.Num(s["price"]), sys.Sell(pool[i]), at + " price");
                    Assert.AreEqual(J.Num(s["coins"]), wallet.Coins, at + " coins");
                    Assert.AreEqual(J.Int(s["quests"]), quests.Count, at + " quests 수");
                    Assert.AreEqual("sellGear", quests[quests.Count - 1]);
                }
                else
                {
                    var r = sys.AutoResolve(pool[i]);
                    Assert.AreEqual(J.Bool(s["equipped"]), r.Equipped, at + " equipped(항상 false)");
                    Assert.IsFalse(r.Equipped);
                    Assert.AreEqual(J.Num(s["gained"]), r.Gained, at + " gained");
                    Assert.AreEqual(J.Num(s["coins"]), wallet.Coins, at + " coins");
                    Assert.AreEqual(J.Int(s["quests"]), quests.Count, at + " quests 수");
                }
            }
        }

        [Test]
        public void 벡터_stats_allSubsBag_heroStats_페이퍼돌_표값()
        {
            var scenes = J.Arr(Doc["stats"]);
            Assert.AreEqual(7, scenes.Count);
            foreach (var s0 in scenes)
            {
                var s = J.Obj(s0);
                string at = "장면 «" + J.Str(s["name"]) + "»";
                var state = StateOf(J.Obj(s["equipment"]));
                var sys = new GearSystem(G, state, new Wallet(), HostOf(s));
                // allSubsBag
                var wantBag = J.NumMap(s["bag"]);
                SubsBag bag;
                HeroStats st = sys.HeroStats(out bag);
                Assert.AreEqual(new List<string>(wantBag.Keys).ToArray(), new List<string>(bag.Keys).ToArray(), at + " bag 키 순서");
                foreach (var k in wantBag.Keys) Assert.AreEqual(wantBag[k], bag[k], 1e-9, at + " bag." + k);
                var bag2 = sys.AllSubsBag();
                foreach (var k in wantBag.Keys) Assert.AreEqual(wantBag[k], bag2[k], 1e-9, at + " allSubsBag." + k);
                // heroStats
                var w = J.Obj(s["stats"]);
                AssertBig(J.Str(w["atk"]), st.Atk, at + " atk");
                AssertBig(J.Str(w["hp"]), st.Hp, at + " hp");
                Assert.AreEqual(J.Num(w["critCh"]), st.CritCh, 1e-9, at + " critCh");
                Assert.AreEqual(J.Num(w["critDmg"]), st.CritDmg, 1e-9, at + " critDmg");
                Assert.AreEqual(J.Num(w["attacksPerSec"]), st.AttacksPerSec, 1e-9, at + " attacksPerSec");
                Assert.AreEqual(J.Num(w["dblAtk"]), st.DblAtk, 1e-9, at + " dblAtk");
                Assert.AreEqual(J.Num(w["block"]), st.Block, 1e-9, at + " block");
                Assert.AreEqual(J.Num(w["hpRegen"]), st.HpRegen, 1e-9, at + " hpRegen");
                Assert.AreEqual(J.Num(w["lifesteal"]), st.Lifesteal, 1e-9, at + " lifesteal");
                Assert.AreEqual(J.Num(w["meleeDmg"]), st.MeleeDmg, 1e-9, at + " meleeDmg");
                Assert.AreEqual(J.Num(w["rangedDmg"]), st.RangedDmg, 1e-9, at + " rangedDmg");
                Assert.AreEqual(J.Num(w["skillDmg"]), st.SkillDmg, 1e-9, at + " skillDmg");
                Assert.AreEqual(J.Num(w["skillCd"]), st.SkillCd, 1e-9, at + " skillCd");
                // 페이퍼돌 표값
                var lk = J.Obj(s["look"]);
                var look = PaperdollLook.Of(G.Defs, state);
                Assert.AreEqual(J.Str(lk["wtypeId"]), look.WtypeId, at + " wtypeId");
                Assert.AreEqual(J.Str(lk["helmetStyle"]), look.HelmetStyle, at + " helmetStyle");
                Assert.AreEqual(J.Str(lk["helmetName"]), look.HelmetName, at + " helmetName");
                Assert.AreEqual(J.Int(lk["armorColor"]), look.ArmorColor, at + " armorColor");
                Assert.AreEqual(J.Int(lk["armorEmissive"]), look.ArmorEmissive, at + " armorEmissive");
                Assert.AreEqual(J.Num(lk["armorEmissiveIntensity"]), look.ArmorEmissiveIntensity, at + " armorEmissiveIntensity");
                Assert.AreEqual(J.Int(lk["emblemColor"]), look.EmblemColor, at + " emblemColor");
                Assert.AreEqual(J.Int(lk["emblemEmissive"]), look.EmblemEmissive, at + " emblemEmissive");
                Assert.AreEqual(J.Num(lk["emblemEmissiveIntensity"]), look.EmblemEmissiveIntensity, at + " emblemEmissiveIntensity");
                Assert.AreEqual(J.Str(lk["armorStyle"]), look.ArmorStyle, at + " armorStyle");
                Assert.AreEqual(J.Str(lk["armorName"]), look.ArmorName, at + " armorName");
            }
        }

        [Test]
        public void 벡터_subs_없는_반쪽_장비가_allSubsBag_을_끊지_않는다()
        {
            var state = new GearState();
            state.Set("ring", new ForgeItem { Slot = "ring", Main = "atk", Value = 5, Level = 1, Rarity = "common", Stars = 0, Subs = null });
            var sys = new GearSystem(G, state, new Wallet(), new GearHost { StarMultFn = StarMult });
            var want = J.NumMap(Doc["noSubsBag"]);
            var bag = sys.AllSubsBag();
            Assert.AreEqual(want.Count, bag.Count);
            foreach (var k in want.Keys) Assert.AreEqual(want[k], bag[k], 1e-12, k);
            AssertBig(J.Str(Doc["noSubsAtk"]), sys.HeroStats().Atk, "atk = 15 + 5");
        }

        [Test]
        public void 코덱_왕복과_상태_직렬화()
        {
            var items = J.Arr(Doc["items"]);
            for (int i = 0; i < 12; i++)
            {
                var src = J.Obj(J.Obj(items[i])["item"]);
                var back = GearCodec.ItemTo(GearCodec.ItemFrom(src));
                Assert.AreEqual(MiniJson.Serialize(src), MiniJson.Serialize(back), "item " + i + " 왕복");
            }
            var state = StateOf(J.Obj(J.Obj(J.Arr(Doc["stats"])[1])["equipment"]));
            Assert.AreEqual(8, state.Equipment.Count, "8부위 전부");
            var json = GearCodec.StateTo(state, G.Defs);
            Assert.AreEqual(G.Defs.Slots, new List<string>(json.Keys).ToArray(), "SLOTS 순서로 직렬화");
            var again = GearCodec.StateFrom(json, G.Defs);
            foreach (var slot in G.Defs.Slots) Assert.AreEqual(MiniJson.Serialize(GearCodec.ItemTo(state.Get(slot))), MiniJson.Serialize(GearCodec.ItemTo(again.Get(slot))), slot);
            var empty = GearCodec.StateFrom(null, G.Defs);
            Assert.AreEqual(0, empty.Equipment.Count);
            Assert.IsNull(GearCodec.ItemFrom(null));
        }

        [Test]
        public void 무기_없으면_club_있으면_wtype_없으면_sword()
        {
            var st = new GearState();
            Assert.AreEqual("club", PaperdollLook.Of(G.Defs, st).WtypeId);
            st.Set("weapon", new ForgeItem { Slot = "weapon", WType = null });
            Assert.AreEqual("sword", PaperdollLook.Of(G.Defs, st).WtypeId);
            st.Set("weapon", new ForgeItem { Slot = "weapon", WType = "bow" });
            Assert.AreEqual("bow", PaperdollLook.Of(G.Defs, st).WtypeId);
            Assert.AreEqual(PaperdollLook.NoArmorColor, PaperdollLook.Of(G.Defs, st).ArmorColor);
            Assert.AreEqual("plate", PaperdollLook.Of(G.Defs, st).ArmorStyle);
        }
    }
}
