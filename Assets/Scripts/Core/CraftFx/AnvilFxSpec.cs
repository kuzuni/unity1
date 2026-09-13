using System;

namespace Forge.Core.CraftFx
{
    /// <summary>
    /// T87 1회차 — 대장간 «두들기기» 연출의 정본 키프레임(원작 `web/css/style.css` 1181~1320행)을 글자 그대로 옮긴 표.
    /// 마스터 클럭은 `--afdur` = <see cref="DurationMs"/>(1500ms · `linear`)이고 타격은 300 / 650 / 1100ms 세 번이다.
    /// 엔진 참조 0 — Game 쪽(`Ui/Anvil*`)이 이 표만 읽어 `RectTransform` 에 바른다.
    ///
    /// 옮기며 지켜야 하는 계약(정본 주석이 못 박은 것):
    ///  · 모루(`anvilbump`)·빌릿(`anvilbillet`)·시트 흔들림(`sheetshake`)은 **같은 클럭**이고 타격·드웰 퍼센트가 같아야 맞물린다.
    ///  · 단조는 비가역이다 — 빌릿은 타격 사이에 **되펴지지 않는다**(scaleY 가 다시 커지지 않는다).
    ///  · 백열(`anvilbillethot`)의 바닥값은 타격마다 **내려간다**(식어 간다) · 식은색(`anvilbilletcool`)은 올라간다.
    ///  · 시트 흔들림 진폭은 정본 probe 상한 <see cref="ShakeMaxPx"/> 를 넘지 않는다.
    /// </summary>
    public static class AnvilFxSpec
    {
        /// <summary>`--afdur` 기본값(ms) — 정본 `animation: … var(--afdur, 1500ms) linear`.</summary>
        public const double DurationMs = 1500;

        /// <summary>타격 시각(ms) — 정본 주석 «타격은 300 / 650 / 1100ms».</summary>
        public static readonly double[] StrikeMs = { 300, 650, 1100 };

        /// <summary>타격 뒤 «망치와 같이 멈추는» 드웰(ms) — 정본 주석 «33/40/50ms 드웰».</summary>
        public static readonly double[] DwellMs = { 33, 40, 50 };

        /// <summary>정본 `probe-anvil-shake.js` 가 지키는 시트 흔들림 상한(px).</summary>
        public const double ShakeMaxPx = 4.5;

        /// <summary>
        /// `anvilbump` 의 축(정본 `.anvil-btn.striking .anvil-svg { transform-box: view-box; transform-origin: 50% 92% }` — 주석 «받침 접지면이 축»).
        /// 값은 **viewBox(132×86) 안의 분수**이고 둘째는 위에서부터다(92% → 유닛 79.12 · 정본이 망치 침하량을 이 축으로 계산해 놨다).
        /// ⚠ 반동은 **모루 그림에만** 건다 — 정본 주석: 버튼(`.anvil-btn`)에 걸면 타격 오버레이가 그 자식이라 «망치가 모루의 반동을 그대로 타고 내려간다»(상대변위 0).
        /// </summary>
        public static readonly double[] BumpOriginFrac = { 0.5, 0.92 };

        /// <summary>
        /// `anvilbillet` 의 축(정본 `.anvil-btn.striking .anv-billet { transform-box: view-box; transform-origin: 55px 21.5px }`) — **viewBox(132×86) 절대 단위**다.
        /// 정본 주석: «압축 축은 빌릿 밑면이다. 가운데를 축으로 잡으면 눌리면서 밑면이 상판을 파고들어 *모루 속으로 가라앉는* 그림이 된다.»
        /// </summary>
        public static readonly double[] BilletOriginVb = { 55.0, 21.5 };

        /// <summary>`anvilbump` — 모루(`.anvil-svg`). 채널 = translateY(px) · scaleX · scaleY. 원점 50% 92%(받침 접지면).</summary>
        public static readonly CssTrack Bump = new CssTrack(
            new double[] { 0, 18.667, 20.0, 22.2, 23.333, 26.0, 42.0, 43.333, 46.0, 47.333, 50.0, 72.0, 73.333, 76.667, 78.333, 82.667, 87.333, 92.667, 100 },
            new double[][]
            {
                new double[] { 0, 1, 1 },            // 0%
                new double[] { 0, 1, 1 },            // 18.667%
                new double[] { 0.5, 1.006, 0.994 },  // 20.0% — 1타(300ms)
                new double[] { 0.5, 1.006, 0.994 },  // 22.2% — 드웰 33ms
                new double[] { -0.3, 0.998, 1.005 }, // 23.333% — 되튐
                new double[] { 0, 1, 1 },            // 26.0%
                new double[] { 0, 1, 1 },            // 42.0%
                new double[] { 0.8, 1.01, 0.99 },    // 43.333% — 2타(650ms)
                new double[] { 0.8, 1.01, 0.99 },    // 46.0% — 드웰 40ms
                new double[] { -0.4, 0.997, 1.008 }, // 47.333%
                new double[] { 0, 1, 1 },            // 50.0%
                new double[] { 0, 1, 1 },            // 72.0%
                new double[] { 1.1, 1.016, 0.984 },  // 73.333% — 3타(1100ms · 가장 깊다)
                new double[] { 1.1, 1.016, 0.984 },  // 76.667% — 드웰 50ms(최장)
                new double[] { -0.6, 0.995, 1.012 }, // 78.333%
                new double[] { 0.25, 1.004, 0.996 }, // 82.667% — 잔진동
                new double[] { -0.12, 0.999, 1.002 },// 87.333%
                new double[] { 0.05, 1.001, 0.999 }, // 92.667%
                new double[] { 0, 1, 1 },            // 100%
            });

        /// <summary>`anvilbillet` — 달군 쇳덩이(`.anv-billet` · 원점 55px 21.5px). 채널 = scaleX · scaleY. `forwards`(끝값을 붙잡는다).</summary>
        public static readonly CssTrack Billet = new CssTrack(
            new double[] { 0, 18.667, 20.0, 22.2, 23.333, 26.0, 42.0, 43.333, 46.0, 47.333, 50.0, 72.0, 73.333, 76.667, 78.333, 100 },
            new double[][]
            {
                new double[] { 1, 1 },
                new double[] { 1, 1 },
                new double[] { 1.12, 0.82 },  // 1타 — 스냅
                new double[] { 1.12, 0.82 },  // 드웰
                new double[] { 1.13, 0.84 },  // 되돌림 최소
                new double[] { 1.13, 0.84 },
                new double[] { 1.13, 0.84 },
                new double[] { 1.28, 0.66 },  // 2타
                new double[] { 1.28, 0.66 },
                new double[] { 1.29, 0.68 },
                new double[] { 1.29, 0.68 },
                new double[] { 1.29, 0.68 },
                new double[] { 1.54, 0.44 },  // 3타 — 가장 납작
                new double[] { 1.54, 0.44 },
                new double[] { 1.52, 0.46 },  // 되돌리지 않는다
                new double[] { 1.52, 0.46 },
            });

        /// <summary>`anvilbillethot` — 백열 겹(`.ab-hot`). 채널 = opacity. 타격에 타고 사이에 식는다.</summary>
        public static readonly CssTrack BilletHot = new CssTrack(
            new double[] { 0, 18.667, 20.0, 26.0, 42.0, 43.333, 49.333, 72.0, 73.333, 83.333, 100 },
            new double[][]
            {
                new double[] { 0.12 }, new double[] { 0.12 },
                new double[] { 0.9 },   // 1타 피크
                new double[] { 0.34 },
                new double[] { 0.18 },
                new double[] { 0.92 },  // 2타 피크
                new double[] { 0.28 },
                new double[] { 0.12 },
                new double[] { 1.0 },   // 3타 피크
                new double[] { 0.3 },
                new double[] { 0.03 },  // 끝 — 식었다
            });

        /// <summary>`anvilbilletglow` — 상판 글로(`.ab-glow`). 채널 = opacity.</summary>
        public static readonly CssTrack BilletGlow = new CssTrack(
            new double[] { 0, 18.667, 20.0, 26.0, 42.0, 43.333, 49.333, 72.0, 73.333, 83.333, 100 },
            new double[][]
            {
                new double[] { 0.5 }, new double[] { 0.5 },
                new double[] { 0.88 },
                new double[] { 0.6 },
                new double[] { 0.56 },
                new double[] { 0.95 },
                new double[] { 0.64 },
                new double[] { 0.6 },
                new double[] { 0.92 },
                new double[] { 0.7 },
                new double[] { 0.56 },
            });

        /// <summary>`anvilbilletcool` — 식은 쇠색(`.ab-cool`). 채널 = opacity. 끝에서 가장 진하다(완성품).</summary>
        public static readonly CssTrack BilletCool = new CssTrack(
            new double[] { 0, 20.0, 26.0, 43.333, 49.333, 73.333, 83.333, 100 },
            new double[][]
            {
                new double[] { 0 }, new double[] { 0 },
                new double[] { 0.14 },
                new double[] { 0.12 },
                new double[] { 0.32 },
                new double[] { 0.28 },
                new double[] { 0.58 },
                new double[] { 0.82 },
            });

        /// <summary>`sheetshake` — 모루가 든 시트(`#equip-sheet`)가 같이 흔들린다. 채널 = translateX(px) · translateY(px).</summary>
        public static readonly CssTrack SheetShake = new CssTrack(
            new double[] { 0, 18.667, 20.0, 21.667, 23.333, 25.333, 42.0, 43.333, 45.0, 46.667, 48.667, 72.0, 73.333, 75.0, 76.667, 79.333, 100 },
            new double[][]
            {
                new double[] { 0, 0 }, new double[] { 0, 0 },
                new double[] { 0.6, 2.1 },     // 1타 — 아래로 꽂힌다
                new double[] { -0.55, -1.15 }, // 되튐
                new double[] { 0.25, 0.4 },
                new double[] { 0, 0 },
                new double[] { 0, 0 },
                new double[] { 0.95, 2.9 },    // 2타
                new double[] { -0.6, -1.25 },
                new double[] { 0.3, 0.5 },
                new double[] { 0, 0 },
                new double[] { 0, 0 },
                new double[] { 1.3, 4.0 },     // 3타 — 가장 깊다
                new double[] { -0.95, -2.1 },
                new double[] { 0.5, 1.0 },
                new double[] { 0, 0 },
                new double[] { 0, 0 },
            });

        /// <summary>
        /// 정본 CSS 에 **적힌 대로의** 타격 키 퍼센트(20.0 / 43.333 / 73.333). 브라우저는 이 반올림된 값으로 애니메이션하므로
        /// 표를 읽을 때는 이 값을 쓴다 — <see cref="StrikePercent"/>(시각 ÷ 클럭)와는 소수 넷째 자리에서 갈린다.
        /// </summary>
        public static readonly double[] StrikeStop = { 20.0, 43.333, 73.333 };

        /// <summary>정본 CSS 에 적힌 대로의 드웰 끝 퍼센트(22.2 / 46.0 / 76.667).</summary>
        public static readonly double[] DwellStop = { 22.2, 46.0, 76.667 };

        /// <summary>타격 n(0~2)의 퍼센트 — 정본이 «타격 시각 ÷ 클럭» 으로 적어 둔 20.0 / 43.333 / 73.333 이다(CSS 는 셋째 자리에서 반올림해 적었다).</summary>
        public static double StrikePercent(int i)
        {
            if (i < 0 || i >= StrikeMs.Length) throw new ArgumentOutOfRangeException("i");
            return StrikeMs[i] / DurationMs * 100.0;
        }

        /// <summary>타격 n 의 드웰이 끝나는 퍼센트(= 타격 + 드웰).</summary>
        public static double DwellEndPercent(int i)
        {
            if (i < 0 || i >= StrikeMs.Length) throw new ArgumentOutOfRangeException("i");
            return (StrikeMs[i] + DwellMs[i]) / DurationMs * 100.0;
        }
    }
}
