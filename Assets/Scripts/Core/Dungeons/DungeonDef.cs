using System;

namespace Forge.Core.Dungeon
{
    /// <summary>`DEFS[i].theme` — 던전 장면 색(0xRRGGBB)·바이옴·천체. T9(세계)가 `Scene3D.setTheme` 자리에 받는다.</summary>
    public sealed class DungeonTheme
    {
        public int Sky, Fog, Ground;
        public string Biome, Celestial;
    }

    /// <summary>원작 `Dungeons.DEFS` 한 줄. `Kr` 은 원본 표기 그대로(«망치 도둑»·«침략» — 정본 주석 2026-08-19 `dungeon-name-hammer`).</summary>
    public sealed class DungeonDef
    {
        public string Id, Name, Kr, Icon;
        /// <summary>해금 좌표 `"c-s"`(기본 순환 · 티어 0 기준) — `unlocked` 는 `bestRank() >= c·100 + s`.</summary>
        public string Unlock;
        /// <summary>보상 설명 문구(목록·상세 팝업).</summary>
        public string Reward;
        public DungeonTheme Theme;

        /// <summary>`Number(unlock.split('-')[0])`.</summary>
        public int UnlockChapter { get { return int.Parse(Unlock.Substring(0, Unlock.IndexOf('-'))); } }
        /// <summary>`Number(unlock.split('-')[1])`.</summary>
        public int UnlockStage { get { return int.Parse(Unlock.Substring(Unlock.IndexOf('-') + 1)); } }
    }

    /// <summary>던전 4종 표 — 원작 `Dungeons.DEFS` 순서·값 그대로(코드 표 · 결정 기록 참조).</summary>
    public static class DungeonDefs
    {
        public static readonly DungeonDef[] All =
        {
            new DungeonDef { Id = "hammer",   Name = "Hammer Thief", Kr = "망치 도둑", Icon = "🔨", Unlock = "2-10", Reward = "해머 · 코인",
                Theme = new DungeonTheme { Sky = 0x5d4037, Fog = 0x795548, Ground = 0x4e342e, Biome = "rock",   Celestial = "none" } },
            new DungeonDef { Id = "ghost",    Name = "Ghost Town",   Kr = "유령 마을", Icon = "👻", Unlock = "2-8",  Reward = "스킬 티켓",
                Theme = new DungeonTheme { Sky = 0x37474f, Fog = 0x546e7a, Ground = 0x455a64, Biome = "rock",   Celestial = "moon" } },
            new DungeonDef { Id = "invasion", Name = "Invasion",     Kr = "침략",      Icon = "🥚", Unlock = "3-1",  Reward = "깨진 알 (펫 소환용)",
                Theme = new DungeonTheme { Sky = 0x4a148c, Fog = 0x6a1b9a, Ground = 0x38006b, Biome = "magic",  Celestial = "moon" } },
            new DungeonDef { Id = "zombie",   Name = "Zombie Rush",  Kr = "좀비 러시", Icon = "🧟", Unlock = "4-1",  Reward = "물약 (기술 재화)",
                Theme = new DungeonTheme { Sky = 0x1b5e20, Fog = 0x2e7d32, Ground = 0x1b3a1e, Biome = "forest", Celestial = "none" } },
        };

        /// <summary>`Dungeons.def(id)` — 없으면 null(원작 `find` 의 undefined).</summary>
        public static DungeonDef Find(string id)
        {
            for (int i = 0; i < All.Length; i++) if (All[i].Id == id) return All[i];
            return null;
        }
    }
}
