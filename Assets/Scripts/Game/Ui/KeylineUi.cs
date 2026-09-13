using System.Collections.Generic;
using UnityEngine;
using Forge.Core.Data;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T109 ⓑ — 정본 `style.css` 의 `-webkit-text-stroke` 폭표(<c>Assets/Forge/Resources/KeylineUi.json</c>).
    /// 정본은 자리마다 `2px` 처럼 절대 폭을 주기도 하고 `.11em` 처럼 글자 크기 비율로 주기도 한다 — 둘을 갈라 담고
    /// 여기서 «기준 캔버스 px» 하나로 환산해 <see cref="UiKit.OutlinePx"/>(T104 SDF 환산)에 넘긴다.
    /// 값을 `catalog.json` 이 아니라 곁 표에 두는 것은 그 파일이 T87 lock 이기 때문이다(T65·T111 과 같은 길).
    /// </summary>
    public static class KeylineUi
    {
        public const string ResourcePath = "KeylineUi";

        private static JsonObject em, px, btnFace;
        private static float cssPx;

        private static void Load()
        {
            if (em != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new KeyNotFoundException("Resources/" + ResourcePath + ".json 이 없다");
            JsonObject root = J.Obj(MiniJson.Parse(ta.text));
            em = J.Obj(root["em"]);
            px = J.Obj(root["px"]);
            btnFace = J.Obj(root["btn_face"]);   // T109 7회차 — 없으면 null(버튼 라벨은 민글자)
            if (em == null || px == null) throw new KeyNotFoundException(ResourcePath + ".json 에 «em»·«px» 절이 없다");
            object c = root["css_px"];
            if (!J.IsNum(c) || J.Num(c) <= 0) throw new KeyNotFoundException(ResourcePath + ".json 에 «css_px»(정본 CSS px → 캔버스 px 배율) 이 없다");
            cssPx = (float)J.Num(c);
        }

        /// <summary>정본 CSS px 1 = 캔버스 px 몇인가(표의 <c>css_px</c> · 정본 앱 폭 499 ↔ 앱 상자 1080 = 2.164 · T87 결정 222).</summary>
        public static float CssPx { get { Load(); return cssPx; } }

        /// <summary>정본이 px 로 적은 자리 — **캔버스 px 로 환산해**(×<see cref="CssPx"/>) 돌려준다(T104 2회차 · 그 전엔 CSS px 그대로 넘겨 절반 굵기였다 · 2ae9903).</summary>
        public static float Px(string key)
        {
            Load();
            object v = px[key];
            if (!J.IsNum(v)) throw new KeyNotFoundException(ResourcePath + ".json 의 «px» 에 «" + key + "» 이 없다");
            return (float)J.Num(v) * cssPx;
        }

        /// <summary>정본이 em 으로 적은 자리 — 그 글자 크기(캔버스 px)에 곱한다. 표 값이 <c>{em, min_px}</c> 꼴이면 정본 `max(Npx, .Mem)` 대로 CSS px 바닥을 캔버스로 환산해 큰 쪽.</summary>
        public static float Em(string key, float fontSizePx)
        {
            Load();
            object v = em[key];
            if (J.IsNum(v)) return (float)J.Num(v) * fontSizePx;
            JsonObject o = J.Obj(v);
            if (o == null || !J.IsNum(o["em"])) throw new KeyNotFoundException(ResourcePath + ".json 의 «em» 에 «" + key + "» 이 없다");
            float w = (float)J.Num(o["em"]) * fontSizePx;
            float floor = (float)J.Num(o["min_px"], 0) * cssPx;
            return w < floor ? floor : w;
        }

        /// <summary>
        /// T109 7회차 — 공용 <see cref="PopupKit.Btn"/> 의 면 색 키(<c>pp_blue</c> …)가 정본 버튼 클래스(`.btn.primary/on/equip/danger/sell`)를 대신한다 —
        /// 표 <c>btn_face</c> 가 그 면에 거는 키라인 폭표 키를 돌려준다. 표에 없는 면(회색 · 디버그)은 정본에 규칙이 없어 <c>null</c>(민글자).
        /// </summary>
        public static string BtnFace(string faceKey)
        {
            Load();
            if (btnFace == null || string.IsNullOrEmpty(faceKey)) return null;
            object v = btnFace[faceKey];
            string k = v as string;
            return string.IsNullOrEmpty(k) ? null : k;
        }

        /// <summary>키 하나로 — «px» 절에 있으면 <see cref="Px"/>, «em» 절에 있으면 <see cref="Em"/>(둘 다면 px). 호출부가 어느 절인지 몰라도 되게.</summary>
        public static float Stroke(string key, float fontSizePx)
        {
            Load();
            if (J.IsNum(px[key])) return Px(key);
            object v = em[key];
            if (J.IsNum(v) || J.Obj(v) != null) return Em(key, fontSizePx);
            throw new KeyNotFoundException(ResourcePath + ".json 의 «px»·«em» 어디에도 «" + key + "» 이 없다");
        }
    }
}
