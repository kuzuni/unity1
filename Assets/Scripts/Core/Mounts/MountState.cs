using System.Collections.Generic;
using Forge.Core.Pets;

namespace Forge.Core.Mounts
{
    /// <summary>
    /// 탈것 개체 — 원작 `S.mounts[i]` 의 `{name, rarity, level, xp, stars, subs}`. 같은 종이 여러 개체로 들어온다(옛 `dupes` 적립 폐기 ·
    /// 2026-08-19 지시). 장착·재료 지정은 이름이 아니라 **인덱스**다. 옵션(<see cref="Substat"/>)은 장비·펫과 공용 체계.
    /// </summary>
    public sealed class Mount
    {
        public string Name;
        public string Rarity;
        public int Level = 1;
        public double Xp;
        public int Stars;
        public List<Substat> Subs = new List<Substat>();
    }

    /// <summary>
    /// 탈것 계통이 쥐는 세이브 상태 — 원작 `S` 의 `mounts`·`activeMounts`·`mountOpens`. 태엽(`S.winders`)은 지갑이라 <see cref="IMountHost"/> 가 든다.
    /// 장착 목록은 배열 그대로 두되(세이브 호환 · `ridden()` 경로가 배열 전제) 길이는 <see cref="MountRules.MaxActiveMounts"/>(1)로 못박는다.
    /// </summary>
    public sealed class MountState
    {
        public List<Mount> Mounts = new List<Mount>();
        /// <summary>장착 중인 <see cref="Mounts"/> 인덱스 — 영웅이 타는 것은 첫 번째.</summary>
        public List<int> ActiveMounts = new List<int>();
        /// <summary>누적 소환 수 → 소환 레벨.</summary>
        public int MountOpens;
    }
}
