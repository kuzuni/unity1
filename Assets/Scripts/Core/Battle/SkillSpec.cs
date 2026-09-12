using Forge.Core.Data;

namespace Forge.Core.Battle
{
    /// <summary>
    /// 전투가 스킬 하나에 대해 아는 전부 — 정의(`SKILL_DEFS` · `GameDefs.Skill`) + 값(원작 `Skills.dmg/healAmt/buffAtk` · 레벨·승천이 걸린 Big).
    /// 값 계산은 T17 몫(`BattleContext.Skill` 이 준다) — 전투는 «해석된 값» 만 받는다.
    /// </summary>
    public sealed class SkillSpec
    {
        public string Id, Type, Fx, Color;
        public double Cd;
        /// <summary>버프·회복 지속(초). 없으면 0(회복은 `dur || 1`).</summary>
        public double Dur;
        /// <summary>착탄 시각(초). 없으면 null → 광역 0.25 · 단일 0.2.</summary>
        public double? ImpactAt;
        /// <summary>`Skills.dmg(id)` — 광역·단일 고정 피해.</summary>
        public Big Dmg;
        /// <summary>`Skills.healAmt(id)` — 지속시간 동안 «총» 회복량.</summary>
        public Big HealAmt;
        /// <summary>`Skills.buffAtk(id)` — 지속시간 동안 더해지는 «고정» 공격력.</summary>
        public Big BuffAtk;

        public static SkillSpec From(SkillDef d, Big dmg, Big healAmt, Big buffAtk)
        {
            return new SkillSpec
            {
                Id = d.Id, Type = d.Type, Fx = d.Fx, Color = d.Color, Cd = d.Cd, Dur = d.Dur ?? 0, ImpactAt = d.ImpactAt,
                Dmg = dmg, HealAmt = healAmt, BuffAtk = buffAtk,
            };
        }
    }
}
