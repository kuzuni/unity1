using System;
using System.Collections.Generic;
using Forge.Core.Data;
using Forge.Core.Save;

namespace Forge.Core.Battle
{
    /// <summary>진행 중인 던전(원작 `Dungeons.run` = `S.dungeonRun` {id, stage, waves}) + 전투가 던전 모듈에게 묻는 값.</summary>
    public sealed class DungeonRun
    {
        public string Id;
        public int Stage;
        /// <summary>입장 때 굳힌 웨이브 수(1~3). 0 이면 <see cref="BattleRules.DungeonDefaultWaves"/>.</summary>
        public int Waves;
        /// <summary>`Dungeons.monsterHp(id, stage)` — T23 이 계산해 준다.</summary>
        public double MonsterHp;
        /// <summary>`Dungeons.def(id).theme` — 장면 테마 키.</summary>
        public string Theme;
        /// <summary>원작 `updateStageLabel()` 의 던전 갈래 «망치 도둑 1단계»(`def.kr` + 단계) — 판 안에서는 진행 좌표 대신 이것이 상단 라벨이다(T55). null 이면 진행 좌표.</summary>
        public string Label;
    }

    /// <summary>
    /// 원작 전투가 `S`(세이브)와 바깥 모듈에서 읽고 쓰는 것 전부. 전투 규칙은 <see cref="Battle"/> 이 계산하고, 여기는 «상태 + 훅» 이다.
    /// 훅이 null 이면 원작의 «없음» 과 같게 돈다(예: <see cref="EnemyHalfW"/> → 0.5 · <see cref="SkillDmgMult"/> → 1).
    /// </summary>
    public sealed class BattleContext
    {
        public GameDefs Defs;
        public Progress Progress;

        // ── S 의 전투 칸 ──
        public double Kills, Coins, Hammers;
        /// <summary>`S.clearedBosses` — 첫 클리어 키(`Progress.StageKey`).</summary>
        public HashSet<string> ClearedBosses = new HashSet<string>();
        /// <summary>`S.equippedSkills` — 4슬롯.</summary>
        public List<string> EquippedSkills = new List<string>();
        public bool AutoCast = true;
        /// <summary>`S.equipment.weapon.wtype` — null 이면 sword.</summary>
        public string WeaponType;
        /// <summary>`S.dungeonRun` — null 이면 본대.</summary>
        public DungeonRun Dungeon;

        // ── 바깥 모듈 훅 ──
        /// <summary>`Forge.heroStats()` 의 버프 제외분(T15) — 필수.</summary>
        public Func<HeroStats> HeroStats;
        /// <summary>`Skills.def/dmg/healAmt/buffAtk`(T17) — 장착 스킬이 있으면 필수.</summary>
        public Func<string, SkillSpec> Skill;
        /// <summary>`TechTree.skillDmgMult()`(T24) — null → 1.</summary>
        public Func<double> SkillDmgMult;
        /// <summary>`Scene3D.enemyHalfW(id)`(T8 · 메시 실측 반폭) — null → 0.5.</summary>
        public Func<int, double> EnemyHalfW;
        /// <summary>`Dungeons.onClear()`(T23 · 보상·팝업) — 전투가 <see cref="Dungeon"/> 을 먼저 비운 뒤 부른다.</summary>
        public Action OnDungeonClear;
        /// <summary>`Dungeons.onFail()`(T23) — 위와 같은 규약.</summary>
        public Action OnDungeonFail;
        /// <summary>`saveGame()`(T13).</summary>
        public Action Save;

        /// <param name="defs">gamedata.json(무기 타입·스킬 정의).</param>
        /// <param name="save">state.json(티어 이름·상한·챕터 길이·사이클 길이 — T13).</param>
        public BattleContext(GameDefs defs, SaveDefs save)
        {
            if (defs == null) throw new ArgumentNullException("defs");
            Defs = defs;
            Progress = new Progress(save);
        }
    }
}
