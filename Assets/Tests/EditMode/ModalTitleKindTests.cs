using System.IO;
using NUnit.Framework;
using Forge.Core.Data;

namespace Forge.Tests.EditMode
{
    /// <summary>
    /// T404 ⓐ — 모달 제목 단 `Title2` 가 카탈로그 `textKinds` 에 정본 크기로 있다: 정본 모달 제목 여덟이 1.12~1.2rem(40.8~43.7px · 1rem = 36.4)이고
    /// 다섯이 1.15rem(41.9)이라 42. 클론 `Title` 60 은 정본 어느 제목에서도 안 나오는 수였다(+37~47%). Head 48 은 그 위 창(T391)이라 겹치지 않는다.
    /// </summary>
    public class ModalTitleKindTests
    {
        static JsonObject Catalog_()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            return MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "Assets", "Forge", "catalog.json")));
        }
        static JsonObject Kind_(string kind)
        {
            foreach (JsonObject e in J.List(J.Require(Catalog_(), "textKinds"), x => J.Obj(x))) if (J.Str(e["kind"]) == kind) return e;
            Assert.Fail("catalog.json textKinds 에 " + kind + " 이 없다");
            return null;
        }

        [Test]
        public void Title2_는_정본_1_15rem_모달_제목_크기다()
        {
            JsonObject k = Kind_("Title2");
            double size = J.Num(J.Require(k, "size")), min = J.Num(J.Require(k, "min"));
            Assert.AreEqual(42.0, size, 1e-9, "정본 1.15rem × 36.4 = 41.9 → 42");
            Assert.AreEqual(size, min, 1e-9, "하한이 아니라 정본 크기다 — 크기 = 하한");
            double remPx = 16.0 / 844.0 * 1920.0;   // 1rem = 앱높이/844×16 · 기준 캔버스 1920
            Assert.AreEqual(1.15 * remPx, size, 1.0, "1.15rem 에서 1px 안");
            Assert.Less(size, J.Num(J.Require(Kind_("Head"), "size")), "Head(48 · 1.22~1.5rem 창) 아래");
            Assert.Less(size, J.Num(J.Require(Kind_("Button"), "size")), "Button 44 아래 — 제목이 버튼 글자보다 작은 것이 정본이다");
            Assert.GreaterOrEqual(size, J.Num(J.Require(Kind_("Body"), "size")), "Body 40 이상");
        }
    }
}
