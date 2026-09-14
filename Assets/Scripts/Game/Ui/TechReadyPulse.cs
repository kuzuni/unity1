using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T335 ⓒ — 기술 노드 «연구 완료»(정본 `.tech-tree-node.researching.ready`)의 테 색 왕복(`tt-ready` 1.1s ease-in-out infinite alternate ·
    /// `style.css` 4622~4626 · `ui.js` 5414). 바깥 글로우(box-shadow)는 T331 흐림 도우미 뒤(등재문) — 그 전엔 테 색만.
    /// 노드는 1초 틱마다 다시 그려질 수 있어 위상을 **절대 벽시계**(<c>Time.unscaledTimeAsDouble</c>)로 잡는다 — 다시 붙어도 맥동이 이어진다.
    /// </summary>
    public sealed class TechReadyPulse : MonoBehaviour
    {
        Image ring;
        Color from, to;

        /// <summary>테 값(0 = from 색 · 1 = to 색) — 시험이 읽는다.</summary>
        public double T { get; private set; }

        public static TechReadyPulse Begin(Image ring, Color from, Color to)
        {
            TechReadyPulse p = ring.GetComponent<TechReadyPulse>();
            if (p == null) p = ring.gameObject.AddComponent<TechReadyPulse>();
            p.ring = ring; p.from = from; p.to = to;
            p.Apply();
            return p;
        }

        void Update() { Apply(); }

        void Apply()
        {
            if (ring == null) return;
            T = DungeonClearFx.Spec.ReadyT(Time.unscaledTimeAsDouble * 1000.0);
            ring.color = Color.Lerp(from, to, (float)T);
        }
    }
}
