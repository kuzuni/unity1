using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Forge.Core.Ui;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T353 — 아이콘 시트는 정본 `.ico { image-rendering: pixelated }`(style.css 7215·7225) 대로 **최근접**으로 줄어야 한다.
    /// 정본 IconGen 은 칸 단위 색으로 그리고 화면에선 `.ico { width: 1.45em }` 로 축소되는데, pixelated 가 그 축소를 최근접으로 강제해
    /// 칸 경계가 딱딱하게 남는다(정본 주석 «안 걸면 칸 단위로 끊긴 색이 다시 그라디언트가 된다»). 클론 시트는 128px 칸을 40~50px 로 줄이므로
    /// `filterMode` 가 Point 가 아니면 정본과 달리 부드럽게 섞인다. 값은 `tools/gen_meta.py` 의 시트 규칙이 `.meta` 에 쓴다 — 여기서는 그 결과를 잰다.
    /// </summary>
    public class IconSheetFilterTests
    {
        [UnityTest]
        public IEnumerator 아이콘_시트_전부가_최근접_축소이고_밉맵이_없다()
        {
            yield return null;
            IconAtlas atlas = UiIcons.Atlas;
            Assert.IsNotNull(atlas);
            Assert.Greater(atlas.Entries.Count, 0, "아틀라스 항목이 없다");
            var seen = new Dictionary<Texture2D, string>();
            foreach (IconAtlasEntry e in atlas.Entries)
            {
                Sprite sp = UiIcons.Get(e.Key);
                Assert.IsNotNull(sp, e.Key + " 스프라이트가 없다");
                Texture2D tex = sp.texture;
                Assert.IsNotNull(tex, e.Key + " 의 시트가 없다");
                if (!seen.ContainsKey(tex)) seen[tex] = e.Key;
            }
            Assert.AreEqual(UiIcons.SheetCount, seen.Count, "항목이 가리키는 시트 수가 atlas.json 의 장 수와 다르다");
            foreach (KeyValuePair<Texture2D, string> kv in seen)
            {
                Assert.AreEqual(FilterMode.Point, kv.Key.filterMode,
                    kv.Key.name + "(예: " + kv.Value + ") 의 filterMode 가 Point 가 아니다 — 정본 image-rendering: pixelated · tools/gen_meta.py 의 시트 규칙(T353)");
                Assert.AreEqual(1, kv.Key.mipmapCount, kv.Key.name + " 에 밉맵이 있다 — 시트는 enableMipMap 0 이어야 최근접 축소가 칸을 지킨다");
            }
        }
    }
}
