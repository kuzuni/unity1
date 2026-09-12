using System.Collections.Generic;
using Forge.Core.Data;
using Forge.Core.Forging;
using Forge.Core.Gear;
using Forge.Core.Save;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 대장간 조각 ↔ 세이브 트리(원작 `S` 의 forgeLevel·forgeUpgradeEndsAt·rollLevel·autoForge·autoForgeOn·pendingCraft·autoMatchQueue·autoMatchHeld·autoBatch·lineAscend.forge · 재화 넷).
    /// T13 <see cref="SaveState"/> 가 통짜 <see cref="JsonObject"/> 라 여기서 강타입 <see cref="ForgeState"/>·<see cref="Wallet"/> 으로 오가며, 장비 항목은 T15 <see cref="GearCodec"/> 를 그대로 쓴다.
    /// </summary>
    public static class ForgeSave
    {
        public const string KeyRollLevel = "rollLevel", KeyAutoForge = "autoForge", KeyPending = "pendingCraft", KeyQueue = "autoMatchQueue", KeyHeld = "autoMatchHeld", KeyBatch = "autoBatch", KeyLineAscend = "lineAscend";
        /// <summary>대기품에 얹히는 «[장착] 에 밀려 내려온 옛 장비» 표식(원작 `item._swapped`).</summary>
        public const string KeySwapped = "_swapped";

        // ---- 읽기 ----

        public static void ReadForge(SaveState s, ForgeState f)
        {
            f.ForgeLevel = s.ForgeLevel < 1 ? 1 : s.ForgeLevel;
            f.UpgradeEndsAt = s.ForgeUpgradeEndsAt;
            f.AscendCount = AscendCount(s, "forge");
            f.RollLevel = new Dictionary<string, double>();
            JsonObject rl = s.Obj(KeyRollLevel);
            if (rl != null) foreach (var kv in rl) if (J.IsNum(kv.Value)) f.RollLevel[kv.Key] = J.Num(kv.Value);
            f.AutoForge = ReadAutoForge(s.Obj(KeyAutoForge));
        }

        public static void ReadWallet(SaveState s, Wallet w)
        {
            w.Coins = s.Coins; w.Gems = s.Gems; w.Hammers = s.Hammers; w.TotalCrafts = s.TotalCrafts;
        }

        public static int AscendCount(SaveState s, string line)
        {
            JsonObject la = s.Obj(KeyLineAscend);
            return la == null ? 0 : J.Int(la[line]);
        }

        public static AutoForgeConfig ReadAutoForge(JsonObject o)
        {
            if (o == null) return null;
            var cfg = new AutoForgeConfig
            {
                KeepAges = new List<string>(J.StrArr(o["keepAges"]) ?? new string[0]),
                FilterOn = J.Bool(o["filterOn"]),
                FilterSubs = new List<string>(J.StrArr(o["filterSubs"]) ?? new string[0]),
                HammersPerBatch = J.Num(o["hammersPerBatch"], 10),
                StopOnTarget = J.Bool(o["stopOnTarget"])
            };
            if (o.Has("continueOnTarget")) cfg.LegacyContinueOnTarget = J.Bool(o["continueOnTarget"]);
            return cfg;
        }

        /// <summary>원작 `isForgeShaped` — 판매·필터·썸네일이 읽는 칸이 다 있는 «제작물의 최소 형태» 인가(손상 세이브 방어).</summary>
        public static bool IsForgeShaped(ForgeItem it, GameDefs defs)
        {
            return it != null && System.Array.IndexOf(defs.Slots, it.Slot) >= 0 && System.Array.IndexOf(defs.Ages, it.Age) >= 0
                && System.Array.IndexOf(defs.Rarities, it.Rarity) >= 0 && !double.IsNaN(it.Level) && !double.IsInfinity(it.Level)
                && !double.IsNaN(it.Value) && !double.IsInfinity(it.Value) && it.Subs != null;
        }

        public static ForgeItem ReadItem(object v, out bool swapped)
        {
            swapped = false;
            JsonObject o = J.Obj(v);
            if (o == null) return null;
            swapped = J.Bool(o[KeySwapped]);
            return GearCodec.ItemFrom(o);
        }

        public static List<ForgeItem> ReadItems(List<object> arr)
        {
            var list = new List<ForgeItem>();
            if (arr == null) return list;
            for (int i = 0; i < arr.Count; i++)
            {
                bool sw;
                ForgeItem it = ReadItem(arr[i], out sw);
                if (it != null) list.Add(it);
            }
            return list;
        }

        // ---- 쓰기 ----

        public static void WriteForge(SaveState s, ForgeState f)
        {
            s.ForgeLevel = f.ForgeLevel;
            s.ForgeUpgradeEndsAt = f.UpgradeEndsAt;
            var rl = new JsonObject();
            if (f.RollLevel != null) foreach (var kv in f.RollLevel) rl[kv.Key] = kv.Value;
            s[KeyRollLevel] = rl;
            if (f.AutoForge != null) s[KeyAutoForge] = WriteAutoForge(f.AutoForge);
        }

        public static void WriteWallet(SaveState s, Wallet w)
        {
            s.Coins = w.Coins; s.Gems = w.Gems; s.Hammers = w.Hammers; s.TotalCrafts = w.TotalCrafts;
        }

        public static JsonObject WriteAutoForge(AutoForgeConfig cfg)
        {
            var o = new JsonObject();
            o["keepAges"] = StrList(cfg.KeepAges);
            o["filterOn"] = cfg.FilterOn;
            o["filterSubs"] = StrList(cfg.FilterSubs);
            o["hammersPerBatch"] = cfg.HammersPerBatch;
            o["stopOnTarget"] = cfg.StopOnTarget;
            return o;
        }

        public static object WriteItem(ForgeItem it, bool swapped)
        {
            if (it == null) return null;
            JsonObject o = GearCodec.ItemTo(it);
            if (swapped) o[KeySwapped] = true;
            return o;
        }

        public static List<object> WriteItems(List<ForgeItem> items)
        {
            var arr = new List<object>();
            if (items != null) for (int i = 0; i < items.Count; i++) arr.Add(WriteItem(items[i], false));
            return arr;
        }

        static List<object> StrList(List<string> l)
        {
            var arr = new List<object>();
            if (l != null) for (int i = 0; i < l.Count; i++) arr.Add(l[i]);
            return arr;
        }
    }
}
