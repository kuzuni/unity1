using System.Collections.Generic;

namespace Forge.Core.Battle
{
    /// <summary>
    /// 전투가 장면(T8)·UI 에 알리는 것 — 원작이 `Scene3D.*`·`UI.*`·`SFX.*` 를 직접 부르던 자리마다 하나씩. 장면은 <see cref="Battle.Events"/> 를 틱마다 비우며 그린다.
    /// 종류는 <see cref="BattleEventKind"/> — 원작 함수 이름 그대로(시뮬레이터 `tools/sim/sim_combat.js` 의 스텁과 같은 이름·인자).
    /// </summary>
    public sealed class BattleEvent
    {
        public int Tick;
        public string Kind;
        public int Id;
        public Big? Value;
        public double Num;
        public bool Flag;
        public string Tag;
        public override string ToString() { return Tick + "|" + Kind + "|" + Id + "|" + (Value.HasValue ? Value.Value.ToString() : "") + "|" + Num + "|" + (Flag ? 1 : 0) + "|" + Tag; }
    }

    /// <summary>이벤트 종류 — 인자 규약은 각 상수 주석.</summary>
    public static class BattleEventKind
    {
        /// <summary>영웅 기상(스테이지 시작 · 사망 뒤).</summary>
        public const string HeroRevive = "heroRevive";
        /// <summary>적 전부 치우기.</summary>
        public const string ClearEnemies = "clearEnemies";
        /// <summary>Tag = 던전 테마 키 또는 `ch:&lt;챕터&gt;`.</summary>
        public const string Theme = "theme";
        /// <summary>Tag = `Progress.StageName()`.</summary>
        public const string StageLabel = "stageLabel";
        /// <summary>Tag = normal | dungeon | boss.</summary>
        public const string Music = "music";
        /// <summary>Num = 웨이브 번호(0 = 리셋).</summary>
        public const string WavePips = "wavePips";
        /// <summary>보스 경고 연출 시작(`BattleRules.BossImpact` 초 뒤 스폰).</summary>
        public const string BossEntrance = "bossEntrance";
        /// <summary>Id = 적 · Value = hp · Num = x · Flag = 보스.</summary>
        public const string Spawn = "spawn";
        /// <summary>Id = 적(`EnemyHitDelay` 뒤 피해).</summary>
        public const string EnemyAttack = "enemyAttack";
        /// <summary>Id = 대상 적(무기 `impact` 뒤 피해).</summary>
        public const string HeroAttack = "heroAttack";
        /// <summary>Tag = fx · Num = 대상 수(회복·버프 0 · 단일 1 · 광역 n).</summary>
        public const string SkillEffect = "skillEffect";
        /// <summary>Tag = 스킬 id.</summary>
        public const string SkillCutin = "skillCutin";
        /// <summary>Tag = 스킬 색.</summary>
        public const string SkillFlash = "skillFlash";
        /// <summary>Id = 적 · Value = 피해 · Flag = 치명 · Tag = "" | skill · Num = 처치타면 1.</summary>
        public const string Hit = "hit";
        /// <summary>Id = 적 · Flag = 보스.</summary>
        public const string Kill = "kill";
        /// <summary>Num = 세기.</summary>
        public const string Shake = "shake";
        /// <summary>Num = 최대 HP 대비 비중 · Value = 피해.</summary>
        public const string HeroHit = "heroHit";
        public const string HeroDown = "heroDown";
        /// <summary>Tag = 배너 문구.</summary>
        public const string DeathFade = "deathFade";
        /// <summary>나를 죽인 적을 암전 동안 녹여 없앤다.</summary>
        public const string DeathWipe = "deathWipe";
        /// <summary>하드컷 커버(뒤따르는 theme·stageLabel·wavePips 가 그 안에서 바뀐다).</summary>
        public const string SceneCut = "sceneCut";
        /// <summary>영웅 머리 위 글자 — Tag = `+회복량` | BLOCK(원작 floatTextAtHero 와 같이 글자만).</summary>
        public const string Float = "float";
        /// <summary>Tag = 드랍 글자(`🪙 +3` · `🔨 +8`). 수량은 `BattleContext.Coins/Hammers` 차분으로(원작 floatLoot 도 글자만 받는다).</summary>
        public const string Loot = "loot";
        /// <summary>Tag = 문구(첫 클리어 · 난이도 상승).</summary>
        public const string Toast = "toast";
        public const string Save = "save";
        public const string DungeonClear = "dungeonClear";
        public const string DungeonFail = "dungeonFail";
    }
}
