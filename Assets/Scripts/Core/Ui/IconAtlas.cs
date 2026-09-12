using System;
using System.Collections.Generic;
using System.Text;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>아틀라스 한 장(PNG). <see cref="H"/> 는 PNG 높이 — 유니티 스프라이트 rect 의 y 는 아래에서 세므로 <see cref="IconAtlas.BottomUpY"/> 로 뒤집는다.</summary>
    public sealed class IconAtlasSheet
    {
        public readonly string File;
        public readonly int W;
        public readonly int H;
        public IconAtlasSheet(string file, int w, int h) { File = file; W = w; H = h; }
        /// <summary>확장자를 뗀 이름(Resources.Load 의 경로 조각).</summary>
        public string Name { get { int i = File.LastIndexOf('.'); return i < 0 ? File : File.Substring(0, i); } }
    }

    /// <summary>아이콘 한 칸. 좌표는 PNG 좌상단 기준(정본 캔버스 좌표) · <see cref="Size"/> 는 정본 굽기 해상도(S · 표시 크기 계산에는 안 쓴다).</summary>
    public sealed class IconAtlasEntry
    {
        public readonly string Key;
        public readonly string Name;
        public readonly string Tint;
        public readonly int Atlas;
        public readonly int X;
        public readonly int Y;
        public readonly int W;
        public readonly int H;
        public readonly int Size;
        public IconAtlasEntry(string key, string name, string tint, int atlas, int x, int y, int w, int h, int size)
        {
            Key = key; Name = name; Tint = tint; Atlas = atlas; X = x; Y = y; W = w; H = h; Size = size;
        }
    }

    /// <summary>
    /// 원작 아이콘 아틀라스 표(ROUTINE T31). <c>Assets/Forge/Icons/Resources/Icons/atlas.json</c> 을 <c>tools/export_icons.js</c> 가 정본
    /// <c>icongen.js</c>·<c>avatars.js</c> 를 headless Chromium 으로 그려 낸다 — 키 이름은 정본 <c>IconGen.draw</c> 의 문자열 그대로이고
    /// tint 변형은 정본 캐시 키와 같은 <c>name|#rrggbb</c>, 아바타는 정본 <c>avatarKey</c> 와 같은 <c>avatar_&lt;코드포인트16진&gt;</c> 다.
    /// 순수 C#(UnityEngine 0) — 그림을 붙이는 쪽은 Game 의 <c>UiIcons</c>.
    /// </summary>
    public sealed class IconAtlas
    {
        public const string JsonFile = "atlas.json";
        public const string ResourceDir = "Icons";

        public readonly int Pad;
        public readonly IReadOnlyList<IconAtlasSheet> Sheets;
        public readonly IReadOnlyList<IconAtlasEntry> Entries;
        private readonly Dictionary<string, IconAtlasEntry> map;

        private IconAtlas(int pad, List<IconAtlasSheet> sheets, List<IconAtlasEntry> entries)
        {
            Pad = pad; Sheets = sheets; Entries = entries;
            map = new Dictionary<string, IconAtlasEntry>(StringComparer.Ordinal);
            foreach (IconAtlasEntry e in entries) map[e.Key] = e;
        }

        /// <summary>atlas.json → 표. 겹친 키 · 장 밖 rect · 빈 rect 는 <see cref="FormatException"/> — 조용히 빈 아이콘을 그리지 않는다.</summary>
        public static IconAtlas Parse(string json)
        {
            JsonObject root = MiniJson.ParseObject(json);
            int pad = J.Int(root["pad"]);
            var sheets = new List<IconAtlasSheet>();
            foreach (object o in J.Arr(J.Require(root, "atlases")))
            {
                JsonObject s = J.Obj(o);
                string file = J.Str(s["file"]);
                int w = J.Int(s["w"]), h = J.Int(s["h"]);
                if (string.IsNullOrEmpty(file) || w <= 0 || h <= 0) throw new FormatException("atlas.json 의 장이 이상하다: " + file + " " + w + "x" + h);
                sheets.Add(new IconAtlasSheet(file, w, h));
            }
            if (sheets.Count == 0) throw new FormatException("atlas.json 에 장이 없다");
            var entries = new List<IconAtlasEntry>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (object o in J.Arr(J.Require(root, "icons")))
            {
                JsonObject it = J.Obj(o);
                string key = J.Str(it["key"]), name = J.Str(it["name"]), tint = J.Str(it["tint"]);
                int a = J.Int(it["atlas"], -1), x = J.Int(it["x"]), y = J.Int(it["y"]), w = J.Int(it["w"]), h = J.Int(it["h"]), size = J.Int(it["size"]);
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(name)) throw new FormatException("atlas.json 에 키 없는 아이콘이 있다");
                if (key != Key(name, tint)) throw new FormatException("atlas.json 키 «" + key + "» 가 name|tint 규약과 다르다");
                if (!seen.Add(key)) throw new FormatException("atlas.json 키가 겹친다: " + key);
                if (a < 0 || a >= sheets.Count) throw new FormatException("atlas.json «" + key + "» 의 장 번호가 밖이다: " + a);
                if (w <= 0 || h <= 0 || x < 0 || y < 0 || x + w > sheets[a].W || y + h > sheets[a].H) throw new FormatException("atlas.json «" + key + "» 의 rect 가 장 밖이다: " + x + "," + y + " " + w + "x" + h);
                entries.Add(new IconAtlasEntry(key, name, tint, a, x, y, w, h, size));
            }
            if (entries.Count == 0) throw new FormatException("atlas.json 에 아이콘이 없다");
            return new IconAtlas(pad, sheets, entries);
        }

        /// <summary>정본 <c>IconGen.url()</c> 의 캐시 키와 같은 꼴: tint 가 있으면 <c>name|#rrggbb</c>(소문자), 없으면 <c>name</c>.</summary>
        public static string Key(string name, string tint)
        {
            return string.IsNullOrEmpty(tint) ? name : name + "|" + tint.ToLowerInvariant();
        }

        /// <summary>정본 <c>avatars.js</c> 의 <c>avatarKey</c>: <c>'avatar_' + [...emoji].map(c =&gt; c.codePointAt(0).toString(16)).join('_')</c> — ZWJ 결합 문자도 코드포인트마다 한 조각.</summary>
        public static string AvatarKey(string emoji)
        {
            if (string.IsNullOrEmpty(emoji)) throw new ArgumentException("아바타 이모지가 비었다");
            var sb = new StringBuilder("avatar_");
            bool first = true;
            for (int i = 0; i < emoji.Length;)
            {
                int cp = char.ConvertToUtf32(emoji, i);
                i += char.IsSurrogatePair(emoji, i) ? 2 : 1;
                if (!first) sb.Append('_');
                first = false;
                sb.Append(cp.ToString("x"));
            }
            return sb.ToString();
        }

        public bool TryGet(string key, out IconAtlasEntry entry) { return map.TryGetValue(key, out entry); }
        public bool Has(string name, string tint = null) { return map.ContainsKey(Key(name, tint)); }
        /// <summary>없으면 null — 호출자가 «원작에 없는 키» 를 스스로 가른다(정본 <c>url()</c> 도 없는 이름에 빈 문자열을 준다).</summary>
        public IconAtlasEntry Find(string name, string tint = null)
        {
            IconAtlasEntry e;
            return map.TryGetValue(Key(name, tint), out e) ? e : null;
        }

        /// <summary>유니티 스프라이트 rect 의 y(아래에서 위로) — PNG 좌상단 y 를 장 높이로 뒤집는다.</summary>
        public int BottomUpY(IconAtlasEntry e) { return Sheets[e.Atlas].H - e.Y - e.H; }
    }
}
