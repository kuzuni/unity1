using System.Collections.Generic;

namespace Forge.Core.Tech
{
    /// <summary>진행 중인 연구 `{id, endsAt}` — 전체 트리 통틀어 동시 1건. endsAt 은 절대시각 ms(원작 `U.now()` = `Date.now()`).</summary>
    public sealed class TechResearch
    {
        public string Id;
        public double EndsAt;

        public TechResearch() { }
        public TechResearch(string id, double endsAt) { Id = id; EndsAt = endsAt; }
    }

    /// <summary>
    /// 기술 트리의 세이브 조각(원작 `S.tech` · `S.techResearch`). 키 = 노드 id(`타입@단계`) → 레벨.
    /// 세이브 직렬화(T13)는 이 두 칸을 그대로 쓴다 — <see cref="TechTree.Ensure"/> 가 구세이브 키를 이관한다.
    /// </summary>
    public sealed class TechState
    {
        public Dictionary<string, int> Tech = new Dictionary<string, int>();
        public TechResearch Research;

        public int Level(string id) { int v; return Tech.TryGetValue(id, out v) ? v : 0; }
    }

    /// <summary>연구가 쓰는 재화 — 물약(선결제)·젬(건너뛰기). 세이브(T13)가 구현하거나 <see cref="Wallet"/> 을 그대로 든다.</summary>
    public interface IWallet
    {
        double Potions { get; set; }
        double Gems { get; set; }
    }

    public sealed class Wallet : IWallet
    {
        public double Potions { get; set; }
        public double Gems { get; set; }
    }
}
