namespace Forge.Core.Dungeon
{
    /// <summary>
    /// `Dungeons.rewards(id, stage)` 의 반환 객체 — 원작은 던전마다 다른 키만 든 객체(`{hammers, coins}`·`{tickets}`·`{eggCurrency}`·`{potions}`)이고
    /// 지급·문구는 `if (r.hammers)` 참/거짓으로 갈린다. 여기서는 없는 재화 = 0 (수량은 항상 100 이상이라 참/거짓 판정이 같다).
    /// </summary>
    public sealed class DungeonRewards
    {
        public double Hammers, Coins, Tickets, EggCurrency, Potions;
    }
}
