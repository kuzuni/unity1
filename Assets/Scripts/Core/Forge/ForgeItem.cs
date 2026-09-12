using System.Collections.Generic;
using Forge.Core.Pets;

namespace Forge.Core.Forging
{
    /// <summary>
    /// 제작된 장비 한 개 — 원작 `Forge.rollItem()` 반환 객체 그대로(`{name, slot, age, ageIdx, rarity, level, main, value, subs, wtype, nameIdx, stars}`).
    /// 장착·판매·능력치(`itemValue`·`sellPrice`·`heroStats`)는 T15(`Core/Gear`)가 이 레코드 위에 세운다.
    /// </summary>
    public sealed class ForgeItem
    {
        public string Name;
        public string Slot;
        public string Age;
        public int AgeIdx;
        public string Rarity;
        /// <summary>원작은 Number — 뽑기 레벨(랜덤워크 값)을 그대로 든다.</summary>
        public double Level;
        /// <summary>"atk" | "hp" — 부위의 주스탯(`SLOT_MAIN`).</summary>
        public string Main;
        /// <summary>주스탯 값 = floor(티어 기본치 × 1.01^(레벨−1) × 등급 배율). 승천 별 배율은 T15 `itemValue` 가 곱한다(Big).</summary>
        public double Value;
        /// <summary>서브스탯 — 원작 `U.rollSubs` · 장비·펫·탈것 공용이라 T16 의 <see cref="Substat"/>/<see cref="SubstatRoll"/> 을 그대로 쓴다.</summary>
        public List<Substat> Subs = new List<Substat>();
        /// <summary>무기 종 id(`WEAPON_TYPES` 키) · 무기가 아니면 null.</summary>
        public string WType;
        /// <summary>이름 카탈로그 인덱스(투구·갑옷 = ITEM_NAMES · 장신구 = accNames · 무기 = 그 시대 무기 풀).</summary>
        public int NameIdx;
        /// <summary>제작 시점의 대장간 라인 승천 횟수.</summary>
        public int Stars;
    }
}
