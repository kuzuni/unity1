using System;
using System.Collections.Generic;
using Forge.Core.CraftFx;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>
    /// 부팅 로딩 오버레이(T142)의 **수치와 단계 표** — 정본 `web/index.html` 21~55행의 인라인 CSS 와
    /// `web/js/main.js` 28~110행 `boot()` 를 옮긴다. 값은 전부 `Resources/BootLoadingUi.json` 에서 온다(§1).
    ///
    /// 왜 인라인 CSS 인가(정본 주석 그대로): «이 오버레이는 뒤따르는 무거운 스크립트 평가가 시작되기 전에
    /// 그려져야 의미가 있어서 외부 CSS 클래스에 의존하지 않는다». 유니티 쪽 대응은 «첫 씬이 서기 전에
    /// 뜨는 화면» 이고, 그 배선은 2회차가 `Bootstrap` 에 단다 — 이 절은 UnityEngine 참조 0 이다.
    /// </summary>
    public sealed class BootLoadingSpec
    {
        /// <summary>부팅 한 단계 — 진행률(%)과 그때 갈아 끼우는 글자(비면 글자를 안 건드린다 · 정본 `blSet`).</summary>
        public struct Stage
        {
            public double Pct;
            public string Label;
        }

        public Stage[] Stages;

        // 자리·크기(px — 로딩창은 앱 폭 비율이 아니라 뷰포트 가운데 고정 크기다)
        public double BoxGapPx, ForgeWPx, ForgeHPx;
        public double AnvilWPx, AnvilHPx, AnvilBottomPx, AnvilFootWPx, AnvilFootHPx;
        public double HammerWPx, HammerHPx, HammerLeftF, HammerBottomPx, HammerPivotXF, HammerPivotYF;
        public double HammerHeadWPx, HammerHeadHPx, HammerHeadTopPx;
        public double HammerHaftWPx, HammerHaftHPx, HammerHaftLeftPx, HammerHaftTopPx;
        public double SparkDPx, SparkLeftF, SparkBottomPx;
        public double TitlePx, TitleTrackEm, TrackWPx, TrackHPx, TrackRPx, StagePx;

        // 시각
        public double SwingMs, SparkMs, SparkDelay2Ms, SparkDelay3Ms, FillMs, FadeMs, RemoveMs;

        /// <summary>정본 CSS px 1 = 기준 캔버스 px 몇인가(정본 앱 폭 499 ↔ 카탈로그 reference 1080).</summary>
        /// <summary>모루 자르개(정본 `clip-path: polygon(...)`) — 상자 안 비율 점들 · y 는 **위에서부터**(CSS 그대로).</summary>
        public double[][] AnvilClip;

        /// <summary>덮개의 정렬 순서(정본 인라인 CSS `z-index: 200`) — 이 덮개는 `#app` 밖이라 앱 캔버스 위에 제 캔버스로 선다.</summary>
        public int ZIndex;

        public double CssPx;

        // 불티 셋의 방향(정본은 조각마다 CSS 변수 --dx/--dy)
        public double[] SparkDxPx, SparkDyPx;

        public RewardBurstSpec.Track Swing, Spark;
        public OrderedMap<string> Colors;
        public double TrackAlpha;

        public static BootLoadingSpec From(JsonObject root)
        {
            var L = J.Obj(J.Require(root, "layout"));
            var T = J.Obj(J.Require(root, "times"));
            var C = J.Obj(J.Require(root, "colors"));
            Func<JsonObject, string, double> n = (o, k) => J.Num(J.Require(o, k));   // 시각·비율은 환산 안 한다

            var s = new BootLoadingSpec();
            s.Stages = ReadStages(J.Require(root, "stages"));
            // 정본 CSS px → 기준 캔버스 px. 안 곱하면 글자가 §1 하한 아래로 내려가 씬 전체의 글자 자가 빨개진다(런 331).
            s.CssPx = J.Num(J.Require(root, "css_px"));
            if (s.CssPx <= 0) throw new FormatException("BootLoadingUi css_px 는 0 보다 커야 한다");
            Func<JsonObject, string, double> raw = (o, k) => J.Num(J.Require(o, k));

            Func<string, double> px = k => raw(L, k) * s.CssPx;
            s.BoxGapPx = px("box_gap_px"); s.ForgeWPx = px("forge_w_px"); s.ForgeHPx = px("forge_h_px");
            s.AnvilWPx = px("anvil_w_px"); s.AnvilHPx = px("anvil_h_px"); s.AnvilBottomPx = px("anvil_bottom_px");
            s.AnvilFootWPx = px("anvil_foot_w_px"); s.AnvilFootHPx = px("anvil_foot_h_px");
            s.HammerWPx = px("hammer_w_px"); s.HammerHPx = px("hammer_h_px");
            s.HammerLeftF = raw(L, "hammer_left_f"); s.HammerBottomPx = px("hammer_bottom_px");
            s.HammerPivotXF = raw(L, "hammer_pivot_x_f"); s.HammerPivotYF = raw(L, "hammer_pivot_y_f");
            s.HammerHeadWPx = px("hammer_head_w_px"); s.HammerHeadHPx = px("hammer_head_h_px"); s.HammerHeadTopPx = px("hammer_head_top_px");
            s.HammerHaftWPx = px("hammer_haft_w_px"); s.HammerHaftHPx = px("hammer_haft_h_px");
            s.HammerHaftLeftPx = px("hammer_haft_left_px"); s.HammerHaftTopPx = px("hammer_haft_top_px");
            s.SparkDPx = px("spark_d_px"); s.SparkLeftF = raw(L, "spark_left_f"); s.SparkBottomPx = px("spark_bottom_px");
            s.TitlePx = px("title_px"); s.TitleTrackEm = raw(L, "title_track_em");
            s.TrackWPx = px("track_w_px"); s.TrackHPx = px("track_h_px"); s.TrackRPx = px("track_r_px");
            s.StagePx = px("stage_px");

            s.SwingMs = n(T, "swing_ms"); s.SparkMs = n(T, "spark_ms");
            s.SparkDelay2Ms = n(T, "spark_delay2_ms"); s.SparkDelay3Ms = n(T, "spark_delay3_ms");
            s.FillMs = n(T, "fill_ms"); s.FadeMs = n(T, "fade_ms"); s.RemoveMs = n(T, "remove_ms");

            var M = J.Obj(J.Require(root, "spark_move"));
            s.SparkDxPx = new[] { n(M, "dx1_px") * s.CssPx, n(M, "dx2_px") * s.CssPx, n(M, "dx3_px") * s.CssPx };
            s.SparkDyPx = new[] { n(M, "dy1_px") * s.CssPx, n(M, "dy2_px") * s.CssPx, n(M, "dy3_px") * s.CssPx };

            s.TrackAlpha = n(C, "track_a");
            s.Colors = new OrderedMap<string>();
            foreach (var kv in C)
            {
                string v = kv.Value as string;
                if (v != null && kv.Key != "_") s.Colors.Add(kv.Key, v);
            }

            s.ZIndex = (int)J.Num(J.Require(root, "z_index"));
            s.AnvilClip = ReadPoly(J.Require(J.Obj(J.Require(root, "anvil_clip")), "xy"));

            s.Swing = Stops(J.Require(root, "swing"), "swing");
            s.Spark = Stops(J.Require(root, "spark"), "spark");
            return s;
        }

        static double[][] ReadPoly(object arr)
        {
            var list = J.Arr(arr);
            if (list.Count < 3) throw new FormatException("자르개 폴리곤은 점이 셋 이상이어야 한다(BootLoadingUi anvil_clip)");
            var pts = new double[list.Count][];
            for (int i = 0; i < list.Count; i++)
            {
                var xy = J.Arr(list[i]);
                if (xy.Count != 2) throw new FormatException("자르개 점은 [x, y] 둘이어야 한다(BootLoadingUi anvil_clip)");
                pts[i] = new[] { J.Num(xy[0]), J.Num(xy[1]) };
            }
            return pts;
        }

        /// <summary>점 (<paramref name="x"/>, <paramref name="y"/>) 이 폴리곤 안인가 — 홀짝 규칙(CSS `clip-path: polygon` 과 같다).
        /// 좌표는 상자 안 비율이고 y 는 위에서부터다.</summary>
        public static bool InPoly(double[][] poly, double x, double y)
        {
            if (poly == null) return true;
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                double xi = poly[i][0], yi = poly[i][1], xj = poly[j][0], yj = poly[j][1];
                if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) inside = !inside;
            }
            return inside;
        }

        /// <summary>한 칸(가운데 <paramref name="cx"/>·<paramref name="cy"/> · 크기 <paramref name="w"/>×<paramref name="h"/>)이
        /// 폴리곤에 얼마나 덮이는가(0~1) — <paramref name="sub"/>×<paramref name="sub"/> 잔표본. 자른 모서리가 계단이 되지 않게 하는 셈이다.</summary>
        public static double Coverage(double[][] poly, double cx, double cy, double w, double h, int sub)
        {
            if (poly == null) return 1.0;
            if (sub < 1) sub = 1;
            int hit = 0;
            for (int i = 0; i < sub; i++)
                for (int k = 0; k < sub; k++)
                {
                    double x = cx + ((i + 0.5) / sub - 0.5) * w;
                    double y = cy + ((k + 0.5) / sub - 0.5) * h;
                    if (InPoly(poly, x, y)) hit++;
                }
            return hit / (double)(sub * sub);
        }

        static Stage[] ReadStages(object arr)
        {
            var list = J.List(arr, x => J.Obj(x));
            var outp = new Stage[list.Count];
            double prev = -1;
            for (int i = 0; i < list.Count; i++)
            {
                double pct = J.Num(J.Require(list[i], "pct"));
                if (pct < prev) throw new FormatException("BootLoadingUi 단계 퍼센트는 오름차순이어야 한다");
                prev = pct;
                object lab;
                outp[i] = new Stage { Pct = pct, Label = list[i].TryGet("label", out lab) ? J.Str(lab) : string.Empty };
            }
            if (outp.Length < 2) throw new FormatException("BootLoadingUi 단계가 둘 미만이다");
            if (outp[outp.Length - 1].Pct != 100) throw new FormatException("마지막 단계는 100% 여야 한다");
            return outp;
        }

        /// <summary>키프레임 배열 → Track. `_` 칸은 주석이라 건너뛴다(다른 표와 달리 이 표는 줄마다 출처를 적는다).</summary>
        static RewardBurstSpec.Track Stops(object arr, string what)
        {
            var list = J.List(arr, x => J.Obj(x));
            var keys = new RewardBurstSpec.KeyStop[list.Count];
            double prev = -1;
            for (int i = 0; i < list.Count; i++)
            {
                var o = list[i];
                var k = new RewardBurstSpec.KeyStop { At = J.Num(J.Require(o, "at")) };
                if (k.At < prev) throw new FormatException(what + " 키프레임 퍼센트는 오름차순이어야 한다");
                prev = k.At;
                foreach (var kv in o)
                {
                    if (kv.Key == "at" || kv.Key == "_") continue;
                    if (kv.Key == "ease") { k.Ease = RewardBurstSpec.EaseOf(kv.Value); continue; }
                    if (kv.Value is double) k.Num[kv.Key] = (double)kv.Value;
                    else throw new FormatException(what + " 키프레임 칸 «" + kv.Key + "» 은 수여야 한다");
                }
                keys[i] = k;
            }
            if (keys.Length < 2) throw new FormatException(what + " 키프레임이 둘 미만이다");
            return new RewardBurstSpec.Track { Keys = keys };
        }

        /// <summary>망치 각도(도) — 주기 안의 시각(ms)으로. 정본 `bl-swing` 은 무한 반복이라 주기로 접는다.</summary>
        public double SwingDeg(double ms)
        {
            return Swing.Sample(Wrap(ms, SwingMs), "rot_deg", null);
        }

        /// <summary>불티 하나의 (불투명도, x, y) — <paramref name="i"/> 는 0·1·2(정본 s1·s2·s3 · 각자 지연과 방향).</summary>
        public void SparkAt(double ms, int i, out double opacity, out double dxPx, out double dyPx)
        {
            if (i < 0 || i >= SparkDxPx.Length) throw new ArgumentOutOfRangeException("i");
            double delay = i == 1 ? SparkDelay2Ms : (i == 2 ? SparkDelay3Ms : 0);
            double p = Wrap(ms - delay, SparkMs);
            opacity = Spark.Sample(p, "opacity", null);
            double k = Spark.Sample(p, "k", null);
            dxPx = SparkDxPx[i] * k;
            dyPx = SparkDyPx[i] * k;
        }

        /// <summary>진행률(%) → 채움 막대 폭(px).</summary>
        public double FillWidthPx(double pct)
        {
            double c = pct < 0 ? 0 : (pct > 100 ? 100 : pct);
            return TrackWPx * c / 100.0;
        }

        /// <summary>그 시각에 보여야 할 단계 — 아직 첫 단계 전이면 -1.</summary>
        public int StageAt(double pct)
        {
            int at = -1;
            for (int i = 0; i < Stages.Length; i++) if (pct >= Stages[i].Pct) at = i;
            return at;
        }

        /// <summary>어디까지 준비됐는가 → 그 순간 보여야 할 진행률(%). 정본 `boot()` 의 단계 여섯을 **클론이 실제로
        /// 그 일을 끝낸 신호**에 하나씩 붙인다 — 순서대로 하나라도 아직이면 그 앞 단계에서 멎는다.
        ///
        /// 왜 «신호» 인가: 정본은 한 함수 안에서 순서대로 내려가며 `blSet` 을 부르지만, 클론은 그 여섯 가지 일을
        /// **서로 다른 MonoBehaviour 가 제 차례에** 한다(`SaveIo`·`UiRoot`·`BattleScene`·`ForgeHost`·`MetaHost`).
        /// 그러니 «지금 몇 %인가» 는 부름 순서가 아니라 **무엇이 섰는가** 로 답해야 한다.
        /// 여섯이 다 서면 마지막 칸(100%)을 준다 — 그때 화면을 치운다.</summary>
        public double PctFromReady(bool save, bool ui, bool scene, bool battle, bool forge, bool meta)
        {
            bool[] step = { save, ui, scene, battle, forge, meta };
            int n = 0;
            while (n < step.Length && step[n]) n++;
            if (n == 0) return 0;
            int i = n - 1;
            if (i >= Stages.Length) i = Stages.Length - 1;
            // 여섯을 다 지났으면 마지막 칸(100%)
            if (n >= step.Length && Stages.Length > step.Length) return Stages[Stages.Length - 1].Pct;
            return Stages[i].Pct;
        }

        /// <summary>주기로 접는다(음수도 접힌다 — 지연이 걸린 불티가 0ms 에 제 창 뒤쪽에서 시작한다).</summary>
        static double Wrap(double ms, double periodMs)
        {
            if (periodMs <= 0) return 0;
            double t = ms % periodMs;
            if (t < 0) t += periodMs;
            return t / periodMs * 100.0;
        }
    }
}
