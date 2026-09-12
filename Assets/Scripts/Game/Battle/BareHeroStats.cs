using Forge.Core;
using Forge.Core.Battle;

namespace Forge.Game.Battle
{
    /// <summary>
    /// 맨몸 영웅 스탯 — 원작 `forge.js heroStats()`(329~369) 의 기본치: atk 15 · hp 150 · 치명 5% · 치명 피해 100% · 공속 1.1 · 나머지 0(장비·펫·탈것·스킬 패시브·기술트리 없음).
    /// T15(장비 8부위 `Core/Gear`)가 `heroStats` 전체를 옮기면 <see cref="BattleScene"/> 은 그것을 `BattleContext.HeroStats` 로 받는다 — 여기는 그때까지의 자리표(값은 정본 코드 상수 · 결정 기록).
    /// </summary>
    public static class BareHeroStats
    {
        public const double Atk = 15, Hp = 150, CritCh = 5, CritDmg = 100, AttacksPerSec = 1.1;

        public static HeroStats Make()
        {
            return new HeroStats { Atk = Big.Of(Atk), Hp = Big.Of(Hp), CritCh = CritCh, CritDmg = CritDmg, AttacksPerSec = AttacksPerSec };
        }
    }
}
