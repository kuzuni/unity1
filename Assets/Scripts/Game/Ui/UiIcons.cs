using System;
using System.Collections.Generic;
using Forge.Core.Ui;
using UnityEngine;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 원작 아이콘(ROUTINE T31) — <c>Resources/Icons/atlas.json</c> + <c>atlas-N.png</c>(<c>tools/export_icons.js</c> 가 정본 <c>icongen.js</c>·<c>avatars.js</c> 를
    /// headless Chromium 으로 그려 낸 것)를 스프라이트로 잘라 준다. 키는 정본 문자열 그대로(<c>IconGen.img('coin')</c> → <see cref="Get"/>("coin")).
    /// 없는 키는 null — 정본 <c>url()</c> 이 빈 문자열을 주는 것과 같은 뜻. 아틀라스 자체가 없으면 예외(조용히 빈 화면을 그리지 않는다).
    /// </summary>
    public static class UiIcons
    {
        private const float PixelsPerUnit = 100f;

        private static IconAtlas atlas;
        private static Texture2D[] sheets;
        private static Dictionary<string, Sprite> cache;

        public static IconAtlas Atlas { get { Load(); return atlas; } }
        public static int SheetCount { get { Load(); return sheets.Length; } }

        private static void Load()
        {
            if (atlas != null) return;
            string dir = IconAtlas.ResourceDir;
            TextAsset ta = Resources.Load<TextAsset>(dir + "/atlas");
            if (ta == null) throw new InvalidOperationException("Resources/" + dir + "/atlas.json 이 없다 — tools/check_icons_sync.sh .wwwww-src --sync 로 만든다 (T31)");
            IconAtlas parsed = IconAtlas.Parse(ta.text);
            var tex = new Texture2D[parsed.Sheets.Count];
            for (int i = 0; i < tex.Length; i++)
            {
                tex[i] = Resources.Load<Texture2D>(dir + "/" + parsed.Sheets[i].Name);
                if (tex[i] == null) throw new InvalidOperationException("Resources/" + dir + "/" + parsed.Sheets[i].File + " 이 없다 — tools/check_icons_sync.sh .wwwww-src --sync (T31)");
                if (tex[i].width != parsed.Sheets[i].W || tex[i].height != parsed.Sheets[i].H)
                    throw new InvalidOperationException(parsed.Sheets[i].File + " 크기 " + tex[i].width + "x" + tex[i].height + " 가 atlas.json(" + parsed.Sheets[i].W + "x" + parsed.Sheets[i].H + ")과 다르다 — 둘을 같이 다시 뽑는다");
            }
            sheets = tex;
            cache = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            atlas = parsed;
        }

        /// <summary>원작 키(+tint) → 스프라이트. 없으면 null.</summary>
        public static Sprite Get(string name, string tint = null)
        {
            if (string.IsNullOrEmpty(name)) return null;
            Load();
            string key = IconAtlas.Key(name, tint);
            Sprite sp;
            if (cache.TryGetValue(key, out sp)) return sp;
            IconAtlasEntry e;
            if (!atlas.TryGet(key, out e)) return null;
            Rect rect = new Rect(e.X, atlas.BottomUpY(e), e.W, e.H);
            sp = Sprite.Create(sheets[e.Atlas], rect, new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sp.name = "ico:" + key;
            cache[key] = sp;
            return sp;
        }

        public static bool Has(string name, string tint = null) { return Atlas.Has(name, tint); }

        /// <summary>정본 <c>IconGen.skill(id)</c> = <c>sk_&lt;id&gt;</c>.</summary>
        public static Sprite Skill(string id) { return Get("sk_" + id); }
        /// <summary>정본 <c>IconGen.tab(name)</c> = <c>tab_&lt;name&gt;</c>.</summary>
        public static Sprite Tab(string name) { return Get("tab_" + name); }
        /// <summary>정본 <c>IconGen.avatar(emoji)</c> — 이모지를 코드포인트 키로(<see cref="IconAtlas.AvatarKey"/>). 없으면 null(원작은 이모지 글자로 폴백한다).</summary>
        public static Sprite Avatar(string emoji) { return string.IsNullOrEmpty(emoji) ? null : Get(IconAtlas.AvatarKey(emoji)); }

        public static IEnumerable<string> Keys
        {
            get { foreach (IconAtlasEntry e in Atlas.Entries) yield return e.Key; }
        }
    }
}
