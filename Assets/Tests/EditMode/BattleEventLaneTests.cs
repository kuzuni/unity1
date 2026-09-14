using NUnit.Framework;
using Forge.Core.Battle;

namespace Forge.Tests
{
    /// <summary>T138 3회차 — 토스트 레인(<see cref="BattleEvent.Lane"/>)은 정본 `toast(msg, lane)` 의 UI 인자다: 판 단위 대조(정본 sim 벡터 ↔ <see cref="SimExpected.HashLine"/>)에
    /// 들어가면 안 되고(정본 sim 은 UI 인자를 안 센다), Kind·Id·Num·Flag 에 실으면 벡터가 깨진다 — 그래서 따로 둔 필드가 해시 줄 밖에 있는지를 못 박는다.</summary>
    public class BattleEventLaneTests
    {
        [Test]
        public void 레인은_해시_줄에_안_들어간다_그래서_정본_벡터_대조가_안_흔들린다()
        {
            var a = new BattleEvent { Tick = 3, Kind = BattleEventKind.Toast, Tag = "🏆 1-1 첫 클리어! 🪙+60", Lane = "combat" };
            var b = new BattleEvent { Tick = 3, Kind = BattleEventKind.Toast, Tag = "🏆 1-1 첫 클리어! 🪙+60", Lane = null };
            Assert.AreEqual(SimExpected.HashLine(a), SimExpected.HashLine(b), "레인이 다른 두 토스트 이벤트의 해시 줄은 같아야 한다(레인은 UI 인자)");
            Assert.AreEqual(SimExpected.Line(a), SimExpected.Line(b), "디버그 줄(Line)도 레인을 안 적는다 — 정본 sim 로그와 나란히 읽는 줄이다");
            Assert.AreEqual("combat", a.Lane);
            Assert.IsNull(new BattleEvent { Kind = BattleEventKind.Loot, Tag = "🪙 +3" }.Lane, "전리품 이벤트는 레인이 없다(정본 floatLoot 은 레인이 없다)");
        }
    }
}
