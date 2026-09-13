using System;

namespace Forge.Core.CraftFx
{
    /// <summary>
    /// T87 — 대장간 «망치» 오버레이(정본 `web/css/style.css` 1326~1460 · `.anvil-fx`)의 키프레임을 글자 그대로 옮긴 표. 엔진 참조 0.
    /// 모루(<see cref="AnvilFxSpec"/>)와 **같은 마스터 클럭**(`--afdur` 1500ms)이고 타격 시각도 같다(`ui.js ANVIL_HITS` = 300 / 650 / 1100ms) —
    /// 정본 주석: «퍼센트와 링·섬광·잔열·그림자·코어의 절대 지연이 한 마스터 클럭이다. 한꺼번에 옮길 것.»
    ///
    /// 옮기며 지켜야 하는 계약(정본 주석이 못 박은 것):
    ///  · **타격 자리는 세 번 다 다르다**(<see cref="HitDx"/>) — «대장장이는 소재를 옮긴다. 같은 자리를 세 번 찍으면 프레임이 복사본으로 보인다.»
    ///  · 접촉 프레임 직전 한 키는 **스미어**(머리를 뒤로 늘린다 · scaleY 1.16 / 1.18 / 1.22) — 60fps 에서 최종 접근이 프레임당 6px 이라 눈이 궤적을 못 잇는다.
    ///  · 접촉 뒤 **드웰**(33 / 40 / 50ms)은 모루·빌릿과 같은 퍼센트에 있어야 한 프레임에 맞물린다.
    ///  · 되튐은 타격보다 **느리다**(0.6~0.72배) — 빠르면 «튕겼다» 가 아니라 «손으로 잡아챘다» 로 읽힌다.
    ///  · 3타의 되튐·퇴장은 <see cref="Exit"/>(1170ms 부터 180ms)이 맡고, 그 0% 자세는 <see cref="Swing"/> 의 100% 와 **같아야** 경계에서 안 튄다.
    /// </summary>
    public static class AutoForgeFxSpec
    {
        /// <summary>`ui.js ANVIL_HITS` — 타격 시각(ms). <see cref="AnvilFxSpec.StrikeMs"/> 와 같은 값이어야 한다(같은 클럭).</summary>
        public static readonly double[] HitMs = { 300, 650, 1100 };

        /// <summary>`ui.js ANVIL_HIT_X` — 타격점 x(viewBox 132×86 단위 · 모루 상판 윗면의 중심).</summary>
        public const double HitX = 55;

        /// <summary>`ui.js ANVIL_HIT_Y` — 타격점 y. 상판(14)이 아니라 **빌릿 윗면**이다(쇳덩이를 얹으며 접점이 3유닛 올라갔다).</summary>
        public const double HitY = 11;

        /// <summary>`ui.js ANVIL_HIT_DX` — 타격마다 옮겨 가는 x(정본: «대장장이는 소재를 옮긴다»). CSS 는 `--dx0/1/2` 로 받는다.</summary>
        public static readonly double[] HitDx = { -4.8, 0.2, 5.4 };

        /// <summary>망치가 접촉하는 키의 퍼센트(스미어 → 접촉 → 드웰 끝) — 모루·빌릿과 같은 자리다.</summary>
        public static readonly double[] SmearStop = { 18.933, 42.067, 71.733 };
        /// <summary>접촉 퍼센트(300 / 650 / 1100ms).</summary>
        public static readonly double[] ContactStop = { 20.0, 43.333, 73.333 };
        /// <summary>드웰이 끝나는 퍼센트(33 / 40 / 50ms 뒤).</summary>
        public static readonly double[] DwellStop = { 22.2, 46.0, 76.667 };

        /// <summary>
        /// `afswing` — 망치(`.af-hammer`). 채널 = translateX(기본값 · <see cref="SwingDx"/> 를 더해야 정본 `calc()` 가 된다) · translateY · rotate(도) · scaleY(스미어).
        /// 정본은 `linear` 다(3타 동기화가 마스터 클럭에 묶여 있어 이징 대신 **키 간격에 이징을 인코딩**했다 — 주석 그대로).
        /// </summary>
        public static readonly CssTrack Swing = new CssTrack(
            new double[] { 0, 1.667, 6.333, 10.0, 13.667, 17.0, 18.933, 20.0, 22.2, 25.333, 28.667, 33.333, 37.0, 40.333, 42.067, 43.333, 46.0, 49.133, 54.0, 60.0, 65.0, 69.667, 71.733, 73.333, 76.667, 78.0, 100 },
            new double[][]
            {
                new double[] { 12, -44, -50, 1 },        // 0% — 위가 아니라 옆에서 들어온다
                new double[] { 7, -38, -42, 1 },         // 25ms — 여기서 이미 보인다
                new double[] { 4.6, -31, -34, 1 },       // 95ms
                new double[] { 3.6, -27, -30, 1 },       // 150ms — 1타 윈드업 정점
                new double[] { 2.6, -21, -23, 1 },       // 205ms — 앤티시페이션(55ms)
                new double[] { 1.2, -8, -2, 1 },         // 255ms — 45ms에 21° 저속
                new double[] { 0.31, 0.12, 10.6, 1.16 }, // 284ms — 스미어(+dx0)
                new double[] { -0.07, 3.08, 19, 1 },     // 300ms — 1타 접촉(+dx0)
                new double[] { -0.07, 2.58, 19, 1 },     // 33ms 드웰(+dx0)
                new double[] { 0.8, -6, 4, 1 },          // 되튐 = 타격의 0.72배
                new double[] { 4.2, -30, -33, 1 },       // 430ms
                new double[] { 3.4, -38, -42, 1 },       // 500ms — 2타 정점
                new double[] { 2.9, -27, -29, 1 },       // 555ms 체공(55ms)
                new double[] { 1.6, -14, -9, 1 },
                new double[] { 0.17, 1.28, 10.6, 1.18 }, // 스미어(+dx1)
                new double[] { -0.11, 5.40, 19, 1 },     // 650ms — 2타 접촉(0.8유닛 관통 · +dx1)
                new double[] { -0.11, 4.60, 19, 1 },     // 40ms 드웰(+dx1)
                new double[] { 1.2, -7, 3, 1 },
                new double[] { 4.4, -42, -48, 1 },       // 810ms
                new double[] { 5, -54, -60, 1 },         // 900ms — 화면 최고점
                new double[] { 3.8, -36, -38, 1 },       // 975ms — 가장 길게 체공(75ms)
                new double[] { 2.6, -20, -20, 1 },
                new double[] { 0.32, 2.51, 8, 1.22 },    // 스미어(가장 길게 늘어난다 · +dx2)
                new double[] { -0.18, 8.35, 20, 1 },     // 1100ms — 3타 접촉(1.2유닛 관통 · +dx2)
                new double[] { -0.18, 7.15, 20, 1 },     // 50ms 드웰(최장 · +dx2)
                new double[] { 1.2, -1, 8, 1 },          // 1170ms — afexit 이 넘겨받는 자세
                new double[] { 1.2, -1, 8, 1 },          // 100% — 같은 자세로 붙잡아 둔다
            });

        /// <summary>
        /// `afswing` 의 `calc(… + var(--dxN))` 몫 — 키마다 더해지는 x(<see cref="HitDx"/>). CSS 는 키에서 calc 을 풀고 그 값들을 보간하므로
        /// «기본값 트랙 + 이 트랙» 을 각각 보간해 더한 것과 같다(선형 보간이라 합이 보존된다). 옮기는 것은 **스미어·접촉·드웰뿐**이다(정본 주석).
        /// </summary>
        public static readonly CssTrack SwingDx = new CssTrack(
            new double[] { 0, 1.667, 6.333, 10.0, 13.667, 17.0, 18.933, 20.0, 22.2, 25.333, 28.667, 33.333, 37.0, 40.333, 42.067, 43.333, 46.0, 49.133, 54.0, 60.0, 65.0, 69.667, 71.733, 73.333, 76.667, 78.0, 100 },
            new double[][]
            {
                new double[] { 0 }, new double[] { 0 }, new double[] { 0 }, new double[] { 0 }, new double[] { 0 }, new double[] { 0 },
                new double[] { -4.8 }, new double[] { -4.8 }, new double[] { -4.8 },
                new double[] { 0 }, new double[] { 0 }, new double[] { 0 }, new double[] { 0 }, new double[] { 0 },
                new double[] { 0.2 }, new double[] { 0.2 }, new double[] { 0.2 },
                new double[] { 0 }, new double[] { 0 }, new double[] { 0 }, new double[] { 0 }, new double[] { 0 },
                new double[] { 5.4 }, new double[] { 5.4 }, new double[] { 5.4 },
                new double[] { 0 }, new double[] { 0 },
            });

        /// <summary>`afswing` 의 불투명도 — 0% 에 0, 25ms(1.667%)에 1, 그 뒤로 계속 1(정본은 그 사이 키에 적지 않아 브라우저가 이어 간다).</summary>
        public static readonly CssTrack SwingOpacity = new CssTrack(
            new double[] { 0, 1.667, 100 },
            new double[][] { new double[] { 0 }, new double[] { 1 }, new double[] { 1 } });

        /// <summary>`afexit` 이 시작하는 시각(ms) — `animation-delay: 1170ms`.</summary>
        public const double ExitStartMs = 1170;
        /// <summary>`afexit` 길이(ms) — `.18s`. 1170 + 180 = 1350ms 로 오버레이 수명(1500ms) 안에서 끝난다.</summary>
        public const double ExitDurMs = 180;
        /// <summary>`afexit` 의 타이밍 함수 — `cubic-bezier(.2,.62,.5,1)`.</summary>
        public static readonly CssEase ExitEase = new CssEase(0.2, 0.62, 0.5, 1);

        /// <summary>
        /// `afexit` — 3타의 되튐 + 퇴장(망치가 왼쪽 위로 빠진다). 채널 = translateX · translateY · rotate(도) · opacity.
        /// 0% 자세는 <see cref="Swing"/> 의 100% 와 같다(경계에서 안 튄다). 앞 33%(59ms)에 45° 를 뽑고 나머지 27° 를 121ms 에 흘린다.
        /// </summary>
        public static readonly CssTrack Exit = new CssTrack(
            new double[] { 0, 33, 100 },
            new double[][]
            {
                new double[] { 1.2, -1, 8, 1 },
                new double[] { 3.4, -28, -37, 1 },
                new double[] { 8, -74, -64, 0 },
            });

        /// <summary>
        /// `ui.js SINK_X/SINK_Y` — 타격마다 «접점이 눌려 내려간» 양(viewBox 단위). 모루 침하 + 빌릿 압축의 합이라
        /// 링·불티·그림자가 **망치가 실제로 닿는 자리**에 놓인다(정본 주석: 정지 y=11 대비 6.33 / 9.25 / 14.35 유닛 중 오버슛을 뺀 값이 이 표다).
        /// </summary>
        public static readonly double[] SinkX = { -0.07, -0.11, -0.18 };
        /// <summary>같은 표의 세로 몫.</summary>
        public static readonly double[] SinkY = { 2.71, 4.81, 7.49 };

        /// <summary>타격 n 의 접점 x(viewBox) — `ui.js` 의 `hx(h) = cx + SINK_X[h] + ANVIL_HIT_DX[h]`.</summary>
        public static double HitCenterX(int i)
        {
            if (i < 0 || i >= HitMs.Length) throw new ArgumentOutOfRangeException("i");
            return HitX + SinkX[i] + HitDx[i];
        }

        /// <summary>타격 n 의 접점 y(viewBox) — `hy(h) = cy + SINK_Y[h]`.</summary>
        public static double HitCenterY(int i)
        {
            if (i < 0 || i >= HitMs.Length) throw new ArgumentOutOfRangeException("i");
            return HitY + SinkY[i];
        }

        /// <summary>`afring` 길이(ms) — `.2s`.</summary>
        public const double RingDurMs = 200;
        /// <summary>`afring` 지연 보정(ms) — `calc(var(--hN) - 8ms)`: 60fps 에서 접촉 프레임에 링의 최대 광량이 놓이게 8ms 앞에 켠다.</summary>
        public const double RingLeadMs = 8;
        /// <summary>`afring` 의 타이밍 함수 — `cubic-bezier(.1,.82,.28,1)`.</summary>
        public static readonly CssEase RingEase = new CssEase(0.1, 0.82, 0.28, 1);
        /// <summary>타격마다의 링 최대 배율(`--afr` 1.5 / 1.9 / 2.7) — 3타가 가장 크게 퍼진다.</summary>
        public static readonly double[] RingScale = { 1.5, 1.9, 2.7 };

        /// <summary>`afring` — 채널 = 배율(0~1 진행에서 1 → <see cref="RingScale"/> 로 퍼진다) · opacity.</summary>
        public static readonly CssTrack Ring = new CssTrack(
            new double[] { 0, 100 },
            new double[][] { new double[] { 0, 1 }, new double[] { 1, 0 } });

        // ── 타격 순간의 겹 셋(접지 그림자 · 4갈래 섬광 · 순백 코어) ─────────────────────────────
        // 정본이 셋에 같은 교훈을 적어 뒀다: «접촉 프레임에 빛이 없었다 — 작게 출발해 뒤에 커지면
        // 소리는 제때 나는데 빛만 메아리로 온다»(`af-ring` 이 먼저 밟은 함정). 그래서 셋 다 접촉 프레임에서 거의 최대로 시작한다.

        /// <summary>`af-shadow` — 머리 밑 접지 그림자(`#2a0d04`). 없으면 «머리가 상판에 얹혔는지 앞에 떠 있는지» 가 안 읽힌다.</summary>
        public const double ShadowDurMs = 130;
        /// <summary>그림자는 접촉 **23ms 앞**에 켠다(다가올 때 작고 옅게 → 접촉 프레임에 가장 진하고 넓게).</summary>
        public const double ShadowLeadMs = 23;
        /// <summary>`cubic-bezier(.2,.7,.35,1)`.</summary>
        public static readonly CssEase ShadowEase = new CssEase(0.2, 0.7, 0.35, 1);
        /// <summary>타격마다의 그림자 배율(`--afds` 1 / 1.12 / 1.3).</summary>
        public static readonly double[] ShadowScale = { 1.0, 1.12, 1.3 };
        /// <summary>`afshadow` — 채널 = 배율 곱(위 배율에 곱한다) · opacity.</summary>
        public static readonly CssTrack Shadow = new CssTrack(
            new double[] { 0, 36, 100 },
            new double[][] { new double[] { 0.5, 0.1 }, new double[] { 1.0, 0.42 }, new double[] { 1.25, 0.0 } });

        /// <summary>`af-star` — 4갈래 섬광(`screen` 합성). 원형 광량만으로는 상판 얼룩과 구별이 안 된다 — 축이 있어야 «어디를 때렸는지» 가 읽힌다.</summary>
        public const double StarDurMs = 75;
        /// <summary>섬광·코어는 접촉 **7ms 앞**.</summary>
        public const double StarLeadMs = 7;
        /// <summary>`cubic-bezier(.1,.75,.3,1)`.</summary>
        public static readonly CssEase StarEase = new CssEase(0.1, 0.75, 0.3, 1);
        /// <summary>타격마다의 섬광 배율(`--afss` 1 / 1.3 / 1.75).</summary>
        public static readonly double[] StarScale = { 1.0, 1.3, 1.75 };
        /// <summary>`afstar` — 채널 = 배율 곱 · opacity.</summary>
        public static readonly CssTrack Star = new CssTrack(
            new double[] { 0, 30, 100 },
            new double[][] { new double[] { 0.82, 1.0 }, new double[] { 1.0, 0.96 }, new double[] { 1.35, 0.0 } });

        /// <summary>`af-core` — 순백 코어(`#ffffff` · `screen`). 네이티브 92px 에서 «때렸다» 를 파는 마지막 수단(수명 3프레임).</summary>
        public const double CoreDurMs = 70;
        /// <summary>`cubic-bezier` 가 아니라 `linear` 다.</summary>
        public static readonly CssEase CoreEase = CssEase.Linear;
        /// <summary>타격마다의 코어 배율(`--afcs` 1.12 / 1.35 / 1.72).</summary>
        public static readonly double[] CoreScale = { 1.12, 1.35, 1.72 };
        /// <summary>`afcore` — 채널 = 배율 곱 · opacity.</summary>
        public static readonly CssTrack Core = new CssTrack(
            new double[] { 0, 45, 100 },
            new double[][] { new double[] { 0.88, 1.0 }, new double[] { 1.0, 1.0 }, new double[] { 1.3, 0.0 } });

        /// <summary>`af-flash` — 타격 섬광 웅덩이(`screen` 합성 · 방사 그라디언트). 수명 100ms · 접촉 **6ms 앞**.</summary>
        public const double FlashDurMs = 100;
        /// <summary>접촉 앞당김(ms).</summary>
        public const double FlashLeadMs = 6;
        /// <summary>타격마다의 플래시 배율(`--affs` 1.16 / 1.38 / 1.82).</summary>
        public static readonly double[] FlashScale = { 1.16, 1.38, 1.82 };
        /// <summary>`afflash` — 채널 = 배율 곱 · opacity. `linear` 다(정본 «같은 이유로 .55 → .86 출발»).</summary>
        public static readonly CssTrack Flash = new CssTrack(
            new double[] { 0, 22, 100 },
            new double[][] { new double[] { 0.86, 1.0 }, new double[] { 1.0, 1.0 }, new double[] { 1.9, 0.0 } });

        /// <summary>
        /// `af-heat` — 타격 직후 상판에 남아 서서히 식는 **잔열**. 수명 260ms · 접촉 3ms 앞.
        /// 정본 주석: 플래시(0.1s)·섬광(0.075s)·링(0.2s)이 250ms 안에 다 사라져 «타격 사이 구간의 상판이 완전히 식은 그림» 이었다 —
        /// 잔열이 다음 타격까지 **다리를 놓는다**. 그리고 이징을 걷어 `linear` 로 둬야 실제로 이어진다(이징판은 200~320ms 에 98% 식었다).
        /// </summary>
        public const double HeatDurMs = 260;
        /// <summary>접촉 앞당김(ms).</summary>
        public const double HeatLeadMs = 3;
        /// <summary>타격마다의 잔열 배율(`--afhs` 1 / 1.15 / 1.45).</summary>
        public static readonly double[] HeatScale = { 1.0, 1.15, 1.45 };
        /// <summary>`afheat` — 채널 = 배율 곱 · opacity. 뒤가 긴 키프레임(12% / 55%)이라 다음 타격까지 밝기가 남는다.</summary>
        public static readonly CssTrack Heat = new CssTrack(
            new double[] { 0, 12, 55, 100 },
            new double[][] { new double[] { 0.55, 0.95 }, new double[] { 1.0, 0.82 }, new double[] { 1.08, 0.42 }, new double[] { 1.18, 0.0 } });

        /// <summary>
        /// 타격 겹 하나를 읽는다 — 창은 «타격 시각 − <paramref name="leadMs"/>» 부터 <paramref name="durMs"/> 동안이고
        /// `into` = [배율, opacity](배율은 타격마다의 <paramref name="perStrike"/> 에 트랙 값을 곱한 것 · CSS 의 `scale(calc(var(--afXs) * k))` 와 같은 뜻).
        /// 창 밖이면 false(그릴 것이 없다).
        /// </summary>
        public static bool SampleBurst(CssTrack track, CssEase ease, double durMs, double leadMs, double[] perStrike, int i, double ms, double[] into)
        {
            if (track == null || perStrike == null) throw new ArgumentNullException("track");
            if (i < 0 || i >= HitMs.Length) throw new ArgumentOutOfRangeException("i");
            if (into == null || into.Length < 2) throw new ArgumentException("into 는 2칸이어야 한다");
            double t0 = HitMs[i] - leadMs;
            if (ms < t0 || ms > t0 + durMs) return false;
            track.SampleEased((ms - t0) / durMs * 100.0, ease, into);
            into[0] *= perStrike[i];
            return true;
        }

        /// <summary>타격 n(0~2)의 링이 켜지는 시각(ms).</summary>
        public static double RingStartMs(int i)
        {
            if (i < 0 || i >= HitMs.Length) throw new ArgumentOutOfRangeException("i");
            return HitMs[i] - RingLeadMs;
        }

        /// <summary>망치의 지금 자세 — `into` = [x, y, rotate(도), scaleY]. x 는 기본값 + `--dxN` 몫이다.</summary>
        public static void SampleSwing(double ms, double[] into)
        {
            if (into == null || into.Length < 4) throw new ArgumentException("into 는 4칸이어야 한다");
            double pct = ms / AnvilFxSpec.DurationMs * 100.0;
            Swing.Sample(pct, into);
            into[0] += SwingDx.Sample1(pct);
        }

        /// <summary>퇴장(1170~1350ms)의 지금 자세 — `into` = [x, y, rotate(도), opacity]. 그 전이면 <see cref="Swing"/> 이 그린다(false).</summary>
        public static bool SampleExit(double ms, double[] into)
        {
            if (into == null || into.Length < 4) throw new ArgumentException("into 는 4칸이어야 한다");
            if (ms < ExitStartMs) return false;
            double pct = Math.Min(100.0, (ms - ExitStartMs) / ExitDurMs * 100.0);
            Exit.SampleEased(pct, ExitEase, into);
            return true;
        }

        /// <summary>타격 n 의 링 — 지금 배율과 불투명도(`into` = [scale, opacity]). 아직 안 켜졌거나 끝났으면 false.</summary>
        public static bool SampleRing(int i, double ms, double[] into)
        {
            if (into == null || into.Length < 2) throw new ArgumentException("into 는 2칸이어야 한다");
            double t0 = RingStartMs(i);
            if (ms < t0 || ms > t0 + RingDurMs) return false;
            double pct = (ms - t0) / RingDurMs * 100.0;
            Ring.SampleEased(pct, RingEase, into);
            into[0] = 1.0 + (RingScale[i] - 1.0) * into[0];
            return true;
        }
    }
}
