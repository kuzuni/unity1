using System.IO;
using NUnit.Framework;
using Forge.Core.Data;

namespace Forge.Tests
{
    /// <summary>
    /// T377 — 정본이 선택자에만 리터럴로 못박은 면 색의 표(`Resources/PinnedColorUi.json`)가 읽히고,
    /// 그 값이 **전역 토큰과 다르다**(같으면 이 표가 있을 까닭이 없다 — 토큰을 쓰면 된다).
    /// 정본 리터럴과 같은지는 `tools/check_pinned_colors.py` 가 정본 CSS 를 열어 본다(여기서는 정본이 없다).
    /// </summary>
    public class PinnedColorTests
    {
        static string Root() { return Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path))); }
        static JsonObject Pinned() { return J.Obj(MiniJson.ParseObject(File.ReadAllText(Path.Combine(Root(), "Assets", "Forge", "Resources", "PinnedColorUi.json")))["colors"]); }

        static string CatalogHex(string key)
        {
            var cat = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Root(), "Assets", "Forge", "catalog.json")));
            foreach (object o in J.Arr(cat["colors"]))
            {
                JsonObject e = J.Obj(o);
                if (e != null && J.Str(e["key"]) == key) return J.Str(e["hex"]).ToLowerInvariant();
            }
            return null;
        }

        [Test]
        public void 표가_읽히고_세_키가_hex_다()
        {
            JsonObject c = Pinned();
            foreach (string k in new[] { "chat_back_face", "sell_btn_face", "sell_btn_lip" })
            {
                string hex = J.Str(c[k]);
                Assert.IsNotNull(hex, k);
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(hex, "^#[0-9a-fA-F]{6}$"), k + " = " + hex);
            }
        }

        [Test]
        public void 못박은_색은_전역_토큰과_다르다()
        {
            JsonObject c = Pinned();
            string red = CatalogHex("pp_red"), redDk = CatalogHex("pp_red_dk");
            Assert.IsNotNull(red, "catalog pp_red"); Assert.IsNotNull(redDk, "catalog pp_red_dk");
            Assert.AreNotEqual(red, J.Str(c["chat_back_face"]).ToLowerInvariant(), "정본 8692 — 토큰을 옮기지 않고 자리에만 리터럴을 준다");
            Assert.AreNotEqual(red, J.Str(c["sell_btn_face"]).ToLowerInvariant());
            Assert.AreNotEqual(redDk, J.Str(c["sell_btn_lip"]).ToLowerInvariant());
            Assert.AreEqual("#ff1017", J.Str(c["chat_back_face"]).ToLowerInvariant(), "정본 3283");
            Assert.AreEqual("#ff1017", J.Str(c["sell_btn_face"]).ToLowerInvariant(), "정본 8686");
            Assert.AreEqual("#4e0507", J.Str(c["sell_btn_lip"]).ToLowerInvariant(), "정본 8686 턱");
        }
    }
}
