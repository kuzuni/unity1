using System.Collections.Generic;
using UnityEngine;
using Forge.Core.Data;

namespace Forge.Game.Ui
{
    /// <summary>T381 — 탈것 칸의 «탄 탈것» 갈래가 쓰는 수(표 <c>Assets/Forge/Resources/MountCellUi.json</c> · 정본 `ui.js` 1526~1535 · `UI.MOUNT_COUNT_STYLE`).</summary>
    public static class MountCellUi
    {
        public const string ResourcePath = "MountCellUi";

        static JsonObject root;

        static JsonObject Table()
        {
            if (root != null) return root;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new KeyNotFoundException("Resources/" + ResourcePath + ".json 이 없다");
            root = J.Obj(MiniJson.Parse(ta.text));
            if (root == null || root.Count == 0) throw new KeyNotFoundException(ResourcePath + ".json 을 못 읽었다");
            return root;
        }

        /// <summary>표의 수(비율·rem·px).</summary>
        public static float F(string key) { return (float)J.Num(J.Require(Table(), key)); }

        /// <summary>표의 색 — `[R,G,B,A]`(채널 0~255 · 알파 0~1).</summary>
        public static Color C(string key)
        {
            double[] a = J.NumArr(J.Require(Table(), key));
            if (a == null || a.Length < 4) throw new KeyNotFoundException(ResourcePath + ".json 의 «" + key + "» 는 [R,G,B,A] 여야 한다");
            return new Color((float)(a[0] / 255.0), (float)(a[1] / 255.0), (float)(a[2] / 255.0), (float)a[3]);
        }
    }
}
