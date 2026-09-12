namespace Forge.Core.Pets
{
    /// <summary>
    /// 원작 `pets.js` 상단의 **코드 상수**(표가 아니라 JS 객체 필드라 `data/*.json` 에 없다). 값은 정본과 글자까지 같고
    /// EditMode `PetTests` 가 `pet_vectors.json` 의 `consts`(정본을 실제로 실행해 뽑은 값)와 대조한다 — 여기만 바꾸면 빨개진다.
    /// 원작 주석대로 셋(MAX_ACTIVE · AUTO_ACTIVE · POWER_DIV)은 값이 같아도 **다른 개념**이라 하나로 합치지 않는다.
    /// </summary>
    public sealed class PetRules
    {
        /// <summary>부화장 기본 슬롯.</summary>
        public int BaseHatchSlots = 3;
        /// <summary>젬 구매로 늘릴 수 있는 슬롯 상한.</summary>
        public int MaxHatchSlotsCap = 5;
        /// <summary>슬롯 1칸 젬 단가(◆400 · 회당 누적 증가).</summary>
        public double SlotGemCost = 400;
        /// <summary>보유(인벤토리) 상한.</summary>
        public int InvCap = 250;
        /// <summary>동시에 출전(장착)할 수 있는 최대 마리 수.</summary>
        public int MaxActive = 3;
        /// <summary>부화로 새 펫이 나왔을 때 자동 출전시키는 최대 마리 수.</summary>
        public int AutoActive = 3;
        /// <summary>밸런스 등가 나눗수 — 펫 1마리 = 같은 등급 장비 8부위 합의 1/3.</summary>
        public double PowerDiv = 3;
        /// <summary>알 보관 상한(최대 소환 배수 x75 가 빈 보관함에서 한 번에 들어가는 크기).</summary>
        public int EggCap = 100;
        /// <summary>펫 만렙(승천은 Lv.100 부터).</summary>
        public int MaxLevel = 100;
        /// <summary>알 소환 1회 비용(🥚100).</summary>
        public double SummonEggCost = 100;

        public static PetRules Original() { return new PetRules(); }
    }
}
