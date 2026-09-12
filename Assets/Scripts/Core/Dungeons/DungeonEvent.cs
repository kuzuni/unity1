namespace Forge.Core.Dungeon
{
    /// <summary>원작 `dungeons.js` 가 부르는 화면·전투 함수 이름 그대로(결정 28ⓑ 와 같은 규약 — 이벤트 줄이 대조 단위).</summary>
    public enum DungeonEventKind
    {
        /// <summary>`UI.toast(text)`.</summary>
        Toast,
        /// <summary>`UI.openDungeons()` — 09:00 리셋이 일어난 순간(원작은 목록 팝업이 열려 있을 때만 다시 그린다 · 열림 여부는 UI 가 안다).</summary>
        OpenDungeons,
        /// <summary>`UI.renderDungeonDetail()` — 위와 같은 순간(상세 팝업).</summary>
        RenderDungeonDetail,
        /// <summary>`UI.rewardBurst(r)` — 소탕 수령 연출(클리어는 팝업 [보상 수령] 시점에 UI 가 스스로).</summary>
        RewardBurst,
        /// <summary>`UI.renderTopBar()`.</summary>
        RenderTopBar,
        /// <summary>`UI.renderPets()` — 침략 알 화폐 보상이 펫 패널에 즉시 보이도록.</summary>
        RenderPets,
        /// <summary>`UI.showDungeonClear(def, stage, r)` — 클리어 보상 팝업.</summary>
        ShowDungeonClear,
        /// <summary>`Combat.hero.hp = maxHp; Combat.setupStage()` — 입장·복원이 전투를 다시 세우는 순간(Battle 이 붙어 있으면 이미 실행된 뒤 알린다).</summary>
        SetupStage,
    }

    public sealed class DungeonEvent
    {
        public DungeonEventKind Kind;
        /// <summary>토스트 글자.</summary>
        public string Text;
        /// <summary>던전 id(ShowDungeonClear).</summary>
        public string Id;
        /// <summary>단계(ShowDungeonClear).</summary>
        public int Stage;
        /// <summary>보상(RewardBurst · ShowDungeonClear).</summary>
        public DungeonRewards Rewards;
    }
}
