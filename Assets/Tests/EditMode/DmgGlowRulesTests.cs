using System;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>T333 10회차 — 데미지 숫자 글로우 표(`DmgGlowUi.json`)와 단계 셈: 정본 `@keyframes dmgcrit`(565·567)·`dmgkill`(578·580)의 값·퍼센트를 표가 그대로 쥐는가.</summary>
    public class DmgGlowRulesTests
    {
        static string TablePath()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            return Path.Combine(root, "Assets", "Forge", "Resources", "DmgGlowUi.json");
        }
        static DmgGlowSpec S() { return DmgGlowSpec.From(MiniJson.ParseObject(File.ReadAllText(TablePath()))); }

        [Test]
        public void 크리와_처치만_글로우가_있고_나머지_등급은_없다()
        {
            var s = S();
            Assert.IsTrue(s.Has("dmg-crit"));
            Assert.IsTrue(s.Has("dmg-kill"));
            // 정본에 글로우 키프레임이 없는 등급 — 지어내지 않는다(일반타 `.float-dmg` 는 바닥 그림자 한 겹뿐이고 그것은 키라인 자 몫이다)
            foreach (string cls in new[] { "dmg-skill", "dmg-hero", "heal", "block", "loot", null, "" })
                Assert.IsFalse(s.Has(cls), (cls ?? "null") + " 에는 정본 글로우 키프레임이 없다");
        }

        [Test]
        public void 표값이_정본_키프레임_그대로다()
        {
            var s = S();
            // style.css 565 @keyframes dmgcrit 0% — 겹 셋 중 0 0 14px rgba(255,214,150,.95)
            var born = s.Get("dmg_crit_born");
            Assert.AreEqual(0, born.DxPx); Assert.AreEqual(0, born.DyPx);
            Assert.AreEqual(14, born.BlurPx, 1e-9); Assert.AreEqual(0.95, born.Alpha, 1e-9);
            Assert.AreEqual("#ffd696", born.Color.ToLowerInvariant(), "rgba(255,214,150) = #ffd696");
            // style.css 510 = 567 9% — 0 0 9px rgba(255,109,0,.7)
            var rest = s.Get("dmg_crit_rest");
            Assert.AreEqual(9, rest.BlurPx, 1e-9); Assert.AreEqual(0.7, rest.Alpha, 1e-9);
            Assert.AreEqual("#ff6d00", rest.Color.ToLowerInvariant(), "rgba(255,109,0) = #ff6d00");
            // style.css 578 dmgkill 0% — 0 0 18px rgba(255,224,150,.98) · 580 7% — 0 0 26px rgba(255,196,90,.95)
            Assert.AreEqual(18, s.Get("dmg_kill_born").BlurPx, 1e-9);
            Assert.AreEqual("#ffe096", s.Get("dmg_kill_born").Color.ToLowerInvariant());
            Assert.AreEqual(26, s.Get("dmg_kill_rest").BlurPx, 1e-9);
            Assert.AreEqual("#ffc45a", s.Get("dmg_kill_rest").Color.ToLowerInvariant());
            // 태어나는 겹이 뒤 겹보다 밝고(알파) 처치가 크리보다 넓다 — 정본이 못 박은 위계(561~563 · 574~576)
            Assert.Greater(s.Get("dmg_crit_born").Alpha, s.Get("dmg_crit_rest").Alpha);
            Assert.Greater(s.Get("dmg_kill_born").BlurPx, s.Get("dmg_crit_born").BlurPx, "처치가 크리보다 한 티어 위다");
        }

        [Test]
        public void 단계는_정본_퍼센트에서_갈리고_마지막_겹이_끝까지_간다()
        {
            var s = S();
            Assert.AreEqual("dmg_crit_born", s.KeyAt("dmg-crit", 0));
            Assert.AreEqual("dmg_crit_born", s.KeyAt("dmg-crit", 0.0899));
            Assert.AreEqual("dmg_crit_rest", s.KeyAt("dmg-crit", 0.09), "정본 9% 키프레임에서 갈린다");
            Assert.AreEqual("dmg_crit_rest", s.KeyAt("dmg-crit", 1));
            Assert.AreEqual("dmg_kill_born", s.KeyAt("dmg-kill", 0.069));
            Assert.AreEqual("dmg_kill_rest", s.KeyAt("dmg-kill", 0.07), "정본 7% 키프레임에서 갈린다");
            Assert.AreEqual(2, s.Phases("dmg-crit").Length);
            Assert.IsNull(s.KeyAt("dmg-skill", 0.5), "글로우가 없는 등급은 겹도 없다");
            Assert.IsNull(s.Phases("heal"));
        }

        [Test]
        public void 어긋난_표는_거부한다()
        {
            const string glows = "\"glows\":{\"a\":{\"dx_px\":0,\"dy_px\":0,\"blur_px\":9,\"color\":\"#ff6d00\",\"alpha\":0.7},\"b\":{\"dx_px\":0,\"dy_px\":0,\"blur_px\":14,\"color\":\"#ffd696\",\"alpha\":0.95}}";
            // 퍼센트가 안 커진다(정본 키프레임 순서가 깨진다)
            Assert.Throws<InvalidOperationException>(() => DmgGlowSpec.From(MiniJson.ParseObject(
                "{" + glows + ",\"classes\":{\"dmg-crit\":[{\"until_f\":0.5,\"key\":\"b\"},{\"until_f\":0.2,\"key\":\"a\"}]}}")));
            // 마지막 단계가 수명 끝까지 안 간다 — 그 뒤 프레임에 겹이 없어진다
            Assert.Throws<InvalidOperationException>(() => DmgGlowSpec.From(MiniJson.ParseObject(
                "{" + glows + ",\"classes\":{\"dmg-crit\":[{\"until_f\":0.09,\"key\":\"b\"},{\"until_f\":0.8,\"key\":\"a\"}]}}")));
            // 표에 없는 겹을 부른다
            Assert.Throws<InvalidOperationException>(() => DmgGlowSpec.From(MiniJson.ParseObject(
                "{" + glows + ",\"classes\":{\"dmg-crit\":[{\"until_f\":1.0,\"key\":\"c\"}]}}")));
            // 단계가 비었다
            Assert.Throws<InvalidOperationException>(() => DmgGlowSpec.From(MiniJson.ParseObject(
                "{" + glows + ",\"classes\":{\"dmg-crit\":[]}}")));
        }
    }
}
