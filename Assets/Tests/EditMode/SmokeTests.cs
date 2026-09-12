using NUnit.Framework;
using Forge.Core;

namespace Forge.Tests
{
    /// <summary>하니스 자리표 — dotnet(`tools/dotnet`)과 유니티 EditMode 둘 다에서 도는 순수 C# 테스트.</summary>
    public class SmokeTests
    {
        [Test]
        public void 코어_어셈블리가_선다()
        {
            Assert.AreEqual(3, CoreInfo.Add(1, 2));
            Assert.AreEqual("kuzuni/wwwww web/", CoreInfo.Source);
        }
    }
}
