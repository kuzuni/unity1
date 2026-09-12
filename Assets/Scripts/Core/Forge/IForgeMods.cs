namespace Forge.Core.Forging
{
    /// <summary>
    /// 기술트리(T24)가 대장간에 주는 배율 — 원작 `TechTree.forgeCostMult/forgeTimeMult/freeForgeChance/gearMaxLevelBonus`.
    /// 대장간은 «곱해진 결과» 만 받는다(노드 %→배율 식은 기술트리 몫). 없으면 <see cref="ForgeMods.None"/>(전부 1 · 0).
    /// </summary>
    public interface IForgeMods
    {
        /// <summary>업그레이드 비용 배율(원작 max(0.1, 1 − pct/100)).</summary>
        double ForgeCostMult { get; }
        /// <summary>업그레이드 시간 배율(원작 1 / (1 + pct/100)).</summary>
        double ForgeTimeMult { get; }
        /// <summary>무료 제련 확률 [0,1](원작 pct/100).</summary>
        double FreeForgeChance { get; }
        /// <summary>뽑기 레벨 캡 보너스(레벨 수 · % 아님).</summary>
        double GearMaxLevelBonus { get; }
    }

    /// <summary>기술트리 없음(모든 노드 0) — 또는 테스트가 값을 직접 넣는 그릇.</summary>
    public sealed class ForgeMods : IForgeMods
    {
        public static readonly ForgeMods None = new ForgeMods();
        public double ForgeCostMult { get; set; } = 1;
        public double ForgeTimeMult { get; set; } = 1;
        public double FreeForgeChance { get; set; } = 0;
        public double GearMaxLevelBonus { get; set; } = 0;
    }
}
