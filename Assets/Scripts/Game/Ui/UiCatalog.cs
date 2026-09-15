using System;
using System.Collections.Generic;
using UnityEngine;

namespace Forge.Game.Ui
{
    /// <summary>
    /// UI 카탈로그(ROUTINE T18). 정본은 <c>Assets/Forge/catalog.json</c>(용도 키 → GUI PRO Kit 경로 · 글자 종류 크기·하한 · 색 · 배치 비율)이고,
    /// 이 ScriptableObject 에셋(<c>Assets/Forge/Resources/UiCatalog.asset</c>)은 <c>tools/gen_ui_catalog.py</c> 가 그 JSON 에서 만든다 —
    /// 스프라이트·글꼴은 GUID 참조라 Resources 밖의 킷 조각을 런타임에 쓸 수 있다. 손으로 고치지 않는다.
    /// 코드에는 글자 크기·색·배치 숫자를 박지 않는다(§1) — 전부 여기서 읽는다.
    /// </summary>
    public sealed class UiCatalog : ScriptableObject
    {
        public const string ResourcePath = "UiCatalog";

        [Serializable] public sealed class SpriteEntry { public string key; public Sprite sprite; }
        [Serializable] public sealed class TextKindEntry { public string kind; public float size; public float min; }
        [Serializable] public sealed class ColorEntry { public string key; public string hex; }
        [Serializable] public sealed class LayoutEntry { public string key; public float value; }
        [Serializable] public sealed class TabEntry { public string key; public string label; public string icon; public string kind; }
        [Serializable] public sealed class BootEntry { public string nickname; public string stage; public int waves; }
        [Serializable] public sealed class RefSize { public float w; public float h; }

        /// <summary>catalog.json 의 JsonUtility 상(사전은 못 받으므로 항목 목록이다).</summary>
        [Serializable]
        public sealed class Data
        {
            public RefSize reference;
            public string[] fallbackOsFonts;
            public List<TextKindEntry> textKinds;
            public List<ColorEntry> colors;
            public List<LayoutEntry> layout;
            public List<TabEntry> tabs;
            public BootEntry boot;
        }

        [Tooltip("Assets/Forge/catalog.json")]
        public TextAsset catalog;
        [Tooltip("주인 글꼴 Assets/Fonts/NotoSans-Regular.ttf")]
        public Font font;
        [Tooltip("T106 — 이모지 폴백 글꼴 Assets/Fonts/NotoEmoji-Forge.ttf(정본이 글자로 쓰는 이모지 여덟 · 단색 서브셋). 없으면 폴백 없이 선다.")]
        public Font emojiFont;
        public List<SpriteEntry> sprites = new List<SpriteEntry>();

        private Data data;
        private Dictionary<string, Sprite> spriteMap;
        private Dictionary<string, Color> colorMap;
        private Dictionary<string, float> layoutMap;
        private Dictionary<string, TextKindEntry> kindMap;

        private static UiCatalog instance;

        /// <summary>Resources 에서 한 번 읽어 파싱한 카탈로그. 없으면 예외 — 조용히 빈 화면을 그리지 않는다.</summary>
        public static UiCatalog Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<UiCatalog>(ResourcePath);
                    if (instance == null)
                        throw new InvalidOperationException("Resources/" + ResourcePath + ".asset 이 없다 — python3 tools/gen_ui_catalog.py 로 만든다 (T18)");
                    instance.Parse();
                }
                return instance;
            }
        }

        /// <summary>테스트가 다시 읽게 할 때.</summary>
        public static void Reset() { instance = null; }

        private void Parse()
        {
            if (catalog == null) throw new InvalidOperationException("UiCatalog.catalog(TextAsset) 이 비었다");
            data = JsonUtility.FromJson<Data>(catalog.text);
            if (data == null || data.reference == null) throw new InvalidOperationException("catalog.json 을 읽지 못했다");

            spriteMap = new Dictionary<string, Sprite>();
            foreach (SpriteEntry e in sprites) spriteMap[e.key] = e.sprite;

            colorMap = new Dictionary<string, Color>();
            foreach (ColorEntry e in data.colors)
            {
                Color c;
                if (!ColorUtility.TryParseHtmlString(e.hex, out c)) throw new InvalidOperationException("색 «" + e.key + "» 의 값 «" + e.hex + "» 을 못 읽는다");
                colorMap[e.key] = c;
            }

            layoutMap = new Dictionary<string, float>();
            foreach (LayoutEntry e in data.layout) layoutMap[e.key] = e.value;

            kindMap = new Dictionary<string, TextKindEntry>();
            foreach (TextKindEntry e in data.textKinds) kindMap[e.kind] = e;
        }

        public float RefW { get { return data.reference.w; } }
        public float RefH { get { return data.reference.h; } }
        public IReadOnlyList<string> FallbackOsFonts { get { return data.fallbackOsFonts ?? new string[0]; } }
        public IReadOnlyList<TabEntry> Tabs { get { return data.tabs; } }
        public BootEntry Boot { get { return data.boot; } }

        public Sprite SpriteOf(string key)
        {
            Sprite s;
            if (!spriteMap.TryGetValue(key, out s) || s == null) throw new KeyNotFoundException("카탈로그에 스프라이트 «" + key + "» 이 없다 (Assets/Forge/catalog.json)");
            return s;
        }

        public Color ColorOf(string key)
        {
            Color c;
            if (!colorMap.TryGetValue(key, out c)) throw new KeyNotFoundException("카탈로그에 색 «" + key + "» 이 없다");
            return c;
        }

        /// <summary>그 배치 키가 표에 있는가 — «있으면 쓰고 없으면 기본값» 갈래가 예외 없이 묻는 자리(T401 2회차).</summary>
        public bool HasLayout(string key) { return key != null && layoutMap.ContainsKey(key); }

        /// <summary>배치 비율(앱 폭·높이에 대한 분수 · 또는 기준 px). 키가 없으면 예외.</summary>
        public float Layout(string key)
        {
            float v;
            if (!layoutMap.TryGetValue(key, out v)) throw new KeyNotFoundException("카탈로그에 배치 값 «" + key + "» 이 없다");
            return v;
        }

        public TextKindEntry Kind(TextKind kind)
        {
            TextKindEntry e;
            if (!kindMap.TryGetValue(kind.ToString(), out e)) throw new KeyNotFoundException("카탈로그에 글자 종류 «" + kind + "» 이 없다");
            return e;
        }
    }
}
