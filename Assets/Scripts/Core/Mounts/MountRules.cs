using Forge.Core.Data;

namespace Forge.Core.Mounts
{
    /// <summary>
    /// 원작 `mounts.js` 상단의 **코드 상수**(표가 아니라 JS 객체 필드라 `data/*.json` 에 없다). 값은 정본과 글자까지 같고
    /// EditMode `MountTests` 가 `mount_vectors.json` 의 `consts`(정본을 실제로 실행해 뽑은 값)와 대조한다 — 여기만 바꾸면 빨개진다.
    /// 원작 주석대로 «보유 상한(INV_CAP)» 과 «장착 상한(MAX_ACTIVE_MOUNTS)» 은 다른 개념이라 합치지 않는다.
    /// </summary>
    public sealed class MountRules
    {
        /// <summary>소환 레벨(등급 확률표) 상한 — `mountSummonRates` 의 마지막 행.</summary>
        public int MaxLevel = 50;
        /// <summary>개체 만렙(승천은 Lv.100 도달부터 · 경험치 커브는 곡선 연장).</summary>
        public int IndivMaxLevel = 100;
        /// <summary>보유(인벤토리) 상한 — 펫·탈것 각각 250.</summary>
        public int InvCap = 250;
        /// <summary>장착 상한 — 1마리(«탈것은 한 개만 장착»).</summary>
        public int MaxActiveMounts = 1;
        /// <summary>폐기된 종 → 새 종(이름만 갈아 끼운다 · 레벨·경험치·별·옵션은 그대로). 순서 = 원작 객체 순서.</summary>
        public OrderedMap<string> LegacySpecies = Legacy();

        static OrderedMap<string> Legacy()
        {
            var m = new OrderedMap<string>();
            m.Add("Brown Leaf", "Pony"); m.Add("Lily Leaf", "Donkey"); m.Add("Oak Leaf", "Alpaca");
            m.Add("Lily Pad", "Clockwork Mouse"); m.Add("Log Raft", "Clockwork Beetle");
            return m;
        }

        public static MountRules Original() { return new MountRules(); }
    }
}
