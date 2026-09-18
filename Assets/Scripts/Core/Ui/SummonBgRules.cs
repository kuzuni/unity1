namespace Forge.Core.Ui
{
    /// <summary>T454 ⓒ — 소환 결과 배경의 «등급 예고» 셈(정본 `ui.js` 505~538 · UnityEngine 0).
    /// 열 때 배경은 최고 등급색을 **승격값의 PRE_BG 만큼 · 등급 위치(pk)만큼** 만 섞어 «얼마나 센 게 오나» 만 알려 주고, `.done` 에서 100% 로 올라간다.</summary>
    public static class SummonBgRules
    {
        /// <summary>등급 위치 0(최하)~1(최상) — `RARITIES.indexOf(best) / (RARITIES.length - 1)`. 등급이 하나뿐이면 0.</summary>
        public static double Pk(int rarityIndex, int rarityCount)
        {
            if (rarityCount <= 1 || rarityIndex <= 0) return 0;
            double pk = rarityIndex / (double)(rarityCount - 1);
            return pk > 1 ? 1 : pk;
        }

        /// <summary>예고 배합 비율 — `mixF × preF × pk`(ui.js 537 `.24 * PRE_BG * pk`). 일반 판(pk 0)은 0 이라 기본색 그대로.</summary>
        public static double PreMixF(double mixF, double preF, double pk) { return mixF * preF * pk; }
    }
}
