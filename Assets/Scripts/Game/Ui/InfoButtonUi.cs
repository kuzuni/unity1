using System.Collections.Generic;
using Forge.Core.Data;
using UnityEngine;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T365 13회차 — 정본 정보 버튼 버튼의 «얼굴» 표(<c>Assets/Forge/Resources/InfoButtonUi.json</c>).
    /// 정본은 같은 버튼을 화면마다 다르게 못 박는다: 장비 시트 3634 는 `--pp-line`(#000) 면, 대장간 정보 5059 는 #17181a 면 —
    /// 둘 다 <b>테 없음 · 흰 소문자 i</b> 다. 기본 규칙 971(흰 면 + ol1 고리)은 실물에 한 번도 안 서므로 갈래를 두지 않는다.
    /// 부르는 쪽 파일이 대개 남의 lock 이라 <b>오브젝트 이름</b>(= 정본 선택자)으로 가른다(T342 1회차 `hatch-cone` 과 같은 길).
    /// </summary>
    public static class InfoButtonUi
    {
        public const string ResourcePath = "InfoButtonUi";
        static JsonObject root, faces;

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new System.InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T365)");
            root = MiniJson.ParseObject(ta.text);
            faces = J.Obj(root["faces"]);
        }

        public static void Reset() { root = null; faces = null; }

        static JsonObject One(string name)
        {
            Load();
            JsonObject o = name == null ? null : J.Obj(faces[name]);
            if (o == null) throw new KeyNotFoundException(ResourcePath + ".json 에 정보 버튼 얼굴 «" + name + "» 이 없다 — 정본 줄과 함께 표에 먼저 적는다");
            return o;
        }

        /// <summary>그 자리 면의 카탈로그 색 키.</summary>
        public static string FaceKey(string name) { return Need(One(name), "face", name); }

        /// <summary>그 자리 글자의 카탈로그 색 키.</summary>
        public static string InkKey(string name) { return Need(One(name), "ink", name); }

        static string Need(JsonObject o, string field, string name)
        {
            string s = J.Str(o[field]);
            if (s == null) throw new KeyNotFoundException(ResourcePath + ".json «" + name + "» 에 " + field + " 가 없다");
            return s;
        }
    }
}
