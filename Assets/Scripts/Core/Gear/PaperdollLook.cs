using System;
using Forge.Core.Data;
using Forge.Core.Forging;

namespace Forge.Core.Gear
{
    /// <summary>
    /// 페이퍼돌이 읽는 값(원작 `scene3d.js refreshHeroEquip` 의 표 계산 부분 · `gamedata.js itemStyleOf/itemNameOf`) — 무기 종 · 투구 스타일/이름 · 갑옷 색/발광/문장 색/스타일.
    /// 3D 외형 자체(`makeWeapon`·`makeHelmet`·`makeArmorExtras`·`dressMcRig`)는 캡처 이식 작업(T37)이 잇는다 — 여기는 표 값만.
    /// </summary>
    public sealed class PaperdollLook
    {
        /// <summary>`S.equipment.weapon ? (w.wtype || 'sword') : 'club'`.</summary>
        public string WtypeId;
        public ForgeItem Weapon, Helmet, Armor;
        public string HelmetStyle, HelmetName;
        /// <summary>갑옷 재질 색 = `AGE_COLORS[age]` · 없으면 0xb0bec5.</summary>
        public int ArmorColor;
        /// <summary>궁극 이상(등급 인덱스 ≥ 4)만 등급색 발광 0.18.</summary>
        public int ArmorEmissive;
        public double ArmorEmissiveIntensity;
        /// <summary>문장 색 = 등급색(없으면 0x78909c) · 발광 0.6(없으면 0).</summary>
        public int EmblemColor, EmblemEmissive;
        public double EmblemEmissiveIntensity;
        public string ArmorStyle, ArmorName;

        public const int NoArmorColor = 0xb0bec5, NoEmblemColor = 0x78909c;
        public const double ArmorGlow = 0.18, EmblemGlow = 0.6;
        public const int GlowRarityIndex = 4;

        public static PaperdollLook Of(GameDefs defs, GearState state)
        {
            var look = new PaperdollLook();
            ForgeItem w = state != null ? state.Get("weapon") : null;
            ForgeItem h = state != null ? state.Get("helmet") : null;
            ForgeItem a = state != null ? state.Get("armor") : null;
            look.Weapon = w; look.Helmet = h; look.Armor = a;
            look.WtypeId = w != null ? (w.WType ?? GearRules.DefaultWtype) : GearRules.NoWeaponWtype;
            if (h != null) { look.HelmetStyle = ItemStyleOf(defs, h); look.HelmetName = ItemNameOf(defs, h); }
            look.ArmorColor = a != null ? defs.AgeColors.Get(a.Age ?? "", 0) : NoArmorColor;
            int aIdx = a != null ? Array.IndexOf(defs.Rarities, a.Rarity) : 0;
            bool glow = aIdx >= GlowRarityIndex;
            look.ArmorEmissive = glow ? defs.RarityHex.Get(a.Rarity, 0) : 0;
            look.ArmorEmissiveIntensity = glow ? ArmorGlow : 0;
            int ec = a != null ? defs.RarityHex.Get(a.Rarity ?? "", 0) : NoEmblemColor;
            look.EmblemColor = ec;
            look.EmblemEmissive = a != null ? ec : 0;
            look.EmblemEmissiveIntensity = a != null ? EmblemGlow : 0;
            look.ArmorStyle = a != null ? ItemStyleOf(defs, a) : "plate";
            look.ArmorName = a != null ? ItemNameOf(defs, a) : null;
            return look;
        }

        /// <summary>`itemStyleOf(item)` — 투구 `HELMET_STYLES[age][nameIdx]`(없으면 plume) · 갑옷 `ARMOR_STYLES`(없으면 plate) · 그 밖 null.</summary>
        public static string ItemStyleOf(GameDefs defs, ForgeItem item)
        {
            if (item == null) return null;
            OrderedMap<string[]> table = item.Slot == "helmet" ? defs.HelmetStyles : item.Slot == "armor" ? defs.ArmorStyles : null;
            if (table == null) return null;
            string[] arr = item.Age != null ? table.Get(item.Age, null) : null;
            string s = arr != null && item.NameIdx >= 0 && item.NameIdx < arr.Length ? arr[item.NameIdx] : null;
            return !string.IsNullOrEmpty(s) ? s : (item.Slot == "helmet" ? "plume" : "plate");
        }

        /// <summary>`itemNameOf(item)` — 든 이름 우선 · 무기는 종 한글명 · 투구/갑옷은 `ITEM_NAMES[age][slot][nameIdx]` · 장신구는 `accNames(age, slot)[nameIdx]` · 없으면 "".</summary>
        public static string ItemNameOf(GameDefs defs, ForgeItem item)
        {
            if (item == null) return "";
            if (!string.IsNullOrEmpty(item.Name)) return item.Name;
            if (item.Slot == "weapon")
            {
                WeaponType wt = item.WType != null ? defs.WeaponTypes.Get(item.WType, null) : null;
                return wt != null && wt.Kr != null ? wt.Kr : "";
            }
            string[] arr;
            if (item.Slot == "helmet" || item.Slot == "armor")
            {
                OrderedMap<string[]> bySlot = item.Age != null ? defs.ItemNames.Get(item.Age, null) : null;
                arr = bySlot != null ? bySlot.Get(item.Slot, null) : null;
            }
            else arr = AccNames(defs, item.Age, item.Slot);
            return arr != null && item.NameIdx >= 0 && item.NameIdx < arr.Length && arr[item.NameIdx] != null ? arr[item.NameIdx] : "";
        }

        /// <summary>원작 `accNames(age, slot)`(T14 `ForgeEngine.AccNames` 와 같은 식 — 인스턴스 없이 쓰려고 여기에도 둔다).</summary>
        public static string[] AccNames(GameDefs defs, string age, string slot)
        {
            OrderedMap<string[]> byAge = age != null ? defs.AccNamesByAge.Get(age, null) : null;
            if (byAge != null && slot != null && byAge.Has(slot)) return byAge[slot];
            if (slot != null && defs.AccNames.Has(slot)) return defs.AccNames[slot];
            return new[] { slot != null ? defs.SlotKr.Get(slot, slot) : "" };
        }
    }
}
