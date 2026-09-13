using UnityEngine;
using UnityEngine.UI;
using Forge.Core.CraftFx;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T87 27회차 — 제작 **결과 카드** 연출 러너. 표는 Core <see cref="CraftCardSpec"/>(정본 `style.css` 1060~1156)가 쥐고
    /// 여기는 그 값을 `RectTransform`·`CanvasGroup`·`Image` 에 바르기만 한다(눈대중 0 · 수치 0줄).
    ///
    /// 세 갈래를 한 러너가 몬다 — 정본이 세 자리 다 «튀어올랐다 내려앉는다» 한 몸짓이기 때문이다:
    ///  · <see cref="Mode.Reveal"/> = `crpop` + `crring`(제작 결과 · 560ms · 끝에서 **머문다**)
    ///  · <see cref="Mode.AutoDrop"/> = `adcpop`(자동 제련 탈락 · 620ms · 끝에서 **빨려 들어간다**)
    ///  · <see cref="Mode.Batch"/> = `cbpop`(격자 **전체** 340ms) + `cbfade`(딤 180ms)
    ///
    /// 축은 카드 한가운데(CSS `transform-origin` 기본값)라 카드 피벗을 (.5,.5) 로 두고 자리를 «가운데» 로 계산한다 —
    /// `UiKit.Place`(피벗 좌상단)를 그대로 쓰면 커지는 카드가 오른쪽 아래로 흘러내린다(T87 함정 ⓑ 와 같은 갈래).
    /// 광택(`crsheen`)은 구운 그라디언트 띠가 필요해 28회차로 남겼다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CraftCardFx : MonoBehaviour
    {
        public enum Mode { Reveal, AutoDrop, Batch }

        private Mode mode;
        private RectTransform card;      // Reveal·AutoDrop = 카드 · Batch = 격자
        private CanvasGroup cardCg;
        private Image ring;              // Reveal 만
        private Graphic dim;             // Batch 만
        private float ringBase, ringRadius;
        private Color ringColor;
        private Vector2 anchor;          // 기준점(모루 위 한 점 · y 는 «위에서 아래로»)
        private float cardSize;
        private double ms;
        private bool running;

        private readonly double[] buf = new double[3];

        /// <summary>연출이 도는 중인가.</summary>
        public bool Running { get { return running; } }

        /// <summary>시작부터 흐른 시간(ms) — 테스트가 <see cref="SampleTo"/> 로 직접 민다.</summary>
        public double ElapsedMs { get { return ms; } }

        /// <summary>이 갈래의 전체 길이(ms).</summary>
        public double DurationMs
        {
            get
            {
                if (mode == Mode.Reveal) return CraftCardSpec.RevealMs;
                if (mode == Mode.AutoDrop) return CraftCardSpec.AutoDropMs;
                return CraftCardSpec.BatchPopMs;
            }
        }

        /// <summary>
        /// 카드 하나(리빌·탈락)를 정본 키프레임대로 몬다.
        /// </summary>
        /// <param name="cardRt">카드 상자 — 이 함수가 앵커·피벗을 가운데로 바꿔 놓는다.</param>
        /// <param name="anchorDown">정본 `left`/`top` 기준점(y 는 위에서 아래로 잰 값).</param>
        /// <param name="size">카드 한 변.</param>
        /// <param name="ringImg">시대색 링(리빌만 · null 이면 안 그린다).</param>
        /// <param name="ringTint">링 색(시대색) — 불투명도는 표가 준다.</param>
        public static CraftCardFx Play(RectTransform cardRt, Mode kind, Vector2 anchorDown, float size, Image ringImg, Color ringTint)
        {
            CraftCardFx fx = cardRt.gameObject.GetComponent<CraftCardFx>();
            if (fx == null) fx = cardRt.gameObject.AddComponent<CraftCardFx>();
            fx.mode = kind;
            fx.card = cardRt;
            fx.cardCg = Group(cardRt);
            fx.ring = ringImg;
            fx.ringColor = ringTint;
            fx.anchor = anchorDown;
            fx.cardSize = size;
            fx.ringBase = size;
            fx.ringRadius = size * 0.16f;   // ItemTile 의 라운드와 같게
            cardRt.anchorMin = cardRt.anchorMax = new Vector2(0f, 1f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(size, size);
            fx.ms = 0;
            fx.running = true;
            fx.Apply();
            return fx;
        }

        /// <summary>카드판 — 격자 **전체**가 하나의 `cbpop` 을 타고 딤이 `cbfade` 로 깔린다(정본 «카드마다 지연 금지»).</summary>
        public static CraftCardFx PlayBatch(RectTransform grid, Graphic dimGraphic)
        {
            CraftCardFx fx = grid.gameObject.GetComponent<CraftCardFx>();
            if (fx == null) fx = grid.gameObject.AddComponent<CraftCardFx>();
            fx.mode = Mode.Batch;
            fx.card = grid;
            fx.cardCg = Group(grid);
            fx.dim = dimGraphic;
            fx.ms = 0;
            fx.running = true;
            fx.Apply();
            return fx;
        }

        private static CanvasGroup Group(RectTransform rt)
        {
            CanvasGroup cg = rt.gameObject.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
            return cg;
        }

        private void Update()
        {
            if (!running) return;
            SampleTo(ms + Time.unscaledDeltaTime * 1000.0);
        }

        /// <summary>시각을 그 자리로 옮기고 바른다(테스트가 프레임을 기다리지 않고 민다).</summary>
        public void SampleTo(double elapsedMs)
        {
            ms = elapsedMs;
            Apply();
            // `forwards` 라 끝값을 물고 멈춘다 — 지우는 것은 부른 쪽(ForgeCraftPopup 의 Delay)이다.
            if (ms >= DurationMs) running = false;
        }

        private void Apply()
        {
            if (card == null) return;
            double pct = ms / DurationMs * 100.0;
            if (mode == Mode.Batch)
            {
                CraftCardSpec.BatchPop.SampleEased(pct, CraftCardSpec.Bounce, buf);
                if (cardCg != null) cardCg.alpha = (float)buf[0];
                card.localScale = new Vector3((float)buf[1], (float)buf[1], 1f);
                if (dim != null)
                {
                    double dp = CraftCardSpec.BatchFadeMs <= 0 ? 100 : ms / CraftCardSpec.BatchFadeMs * 100.0;
                    Color c = dim.color;
                    c.a = DimBase * (float)CraftCardSpec.BatchFade.Sample1(dp);
                    dim.color = c;
                }
                return;
            }

            CssTrack track = mode == Mode.Reveal ? CraftCardSpec.Pop : CraftCardSpec.AutoDrop;
            CssEase ease = mode == Mode.Reveal ? CraftCardSpec.Bounce : CraftCardSpec.EaseOut;
            track.SampleEased(pct, ease, buf);
            if (cardCg != null) cardCg.alpha = (float)buf[0];
            float cy = (float)CraftCardSpec.CenterYDown(anchor.y, buf[1], cardSize);
            card.anchoredPosition = new Vector2(anchor.x, -cy);
            card.localScale = new Vector3((float)buf[2], (float)buf[2], 1f);

            if (ring != null && mode == Mode.Reveal)
            {
                double[] rv = new double[2];
                CraftCardSpec.Ring.SampleEased(pct, CraftCardSpec.EaseOut, rv);
                float spread = (float)rv[0] * PopupKit.Rem;
                float w = ringBase + spread * 2f;
                RectTransform rt = ring.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(w, w);
                Color c = ringColor;
                c.a = (float)rv[1];
                ring.color = c;
            }
        }

        /// <summary>정본 `.craft-batch { background: rgba(0,0,0,.42) }` — `cbfade` 가 이 값을 0 에서 끌어올린다.</summary>
        public const float DimBase = 0.42f;
    }
}
