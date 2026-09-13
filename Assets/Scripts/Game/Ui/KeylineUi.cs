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

        private static JsonObject em, px;

        private static void Load()
        {
            if (em != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new KeyNotFoundException("Resources/" + ResourcePath + ".json 이 없다");
            JsonObject root = J.Obj(MiniJson.Parse(ta.text));
            em = J.Obj(root["em"]);
            px = J.Obj(root["px"]);
            if (em == null || px == null) throw new KeyNotFoundException(ResourcePath + ".json 에 «em»·«px» 절이 없다");
        }

        /// <summary>정본이 px 로 적은 자리 — 기준 캔버스 px 그대로.</summary>
        public static float Px(string key)
        {
            Load();
            object v = px[key];
            if (!J.IsNum(v)) throw new KeyNotFoundException(ResourcePath + ".json 의 «px» 에 «" + key + "» 이 없다");
            return (float)J.Num(v);
        }

        /// <summary>정본이 em 으로 적은 자리 — 그 글자 크기에 곱한다(정본 `em` 이 글자 크기 기준인 것과 같다).</summary>
        public static float Em(string key, float fontSizePx)
        {
            Load();
            object v = em[key];
            if (!J.IsNum(v)) throw new KeyNotFoundException(ResourcePath + ".json 의 «em» 에 «" + key + "» 이 없다");
            return (float)J.Num(v) * fontSizePx;
        }
    }
}
