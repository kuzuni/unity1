using System;

namespace Forge.Core.CraftFx
{
    /// <summary>
    /// T87 — CSS `@keyframes` 한 줄기를 그대로 담는 트랙(정본 `web/css/style.css`). 엔진 참조 0.
    /// 정본 제작 연출은 전부 `linear` 타이밍이라(예: `animation: anvilbump var(--afdur) linear`)
    /// 키 사이는 **선형 보간**이다 — 브라우저가 하던 일이 여기서는 <see cref="Sample"/> 이다.
    /// 퍼센트는 CSS 에 적힌 값 그대로 두고(0~100), 시간은 `--afdur` 를 곱해 얻는다.
    /// </summary>
    public sealed class CssTrack
    {
        private readonly double[] stops;
        private readonly double[][] values;

        /// <summary>채널 수(예: translateY·scaleX·scaleY = 3 · opacity = 1).</summary>
        public int Channels { get { return values.Length == 0 ? 0 : values[0].Length; } }

        /// <summary>키 개수.</summary>
        public int Count { get { return stops.Length; } }

        /// <summary>키 퍼센트(오름차순 · CSS 에 적힌 값).</summary>
        public double StopAt(int i) { return stops[i]; }

        /// <summary>키 값(채널 배열).</summary>
        public double[] ValueAt(int i) { return values[i]; }

        /// <param name="stops">키 퍼센트 — 오름차순이어야 한다(CSS 가 «0%, 18.667%» 처럼 묶어 적은 것은 풀어서 각각 적는다).</param>
        /// <param name="values">키마다 같은 길이의 채널 배열.</param>
        public CssTrack(double[] stops, double[][] values)
        {
            if (stops == null || values == null) throw new ArgumentNullException("stops");
            if (stops.Length == 0 || stops.Length != values.Length) throw new ArgumentException("키 개수가 안 맞는다");
            for (int i = 1; i < stops.Length; i++)
            {
                if (stops[i] < stops[i - 1]) throw new ArgumentException("키 퍼센트는 오름차순이어야 한다: " + stops[i - 1] + " → " + stops[i]);
            }
            int ch = values[0].Length;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == null || values[i].Length != ch) throw new ArgumentException("채널 수가 키마다 다르다");
            }
            this.stops = stops;
            this.values = values;
        }

        /// <summary>퍼센트(0~100)에서 값을 읽는다 — 밖은 양 끝으로 자른다(CSS 의 `forwards` 와 같은 뜻).</summary>
        public void Sample(double percent, double[] into)
        {
            if (into == null || into.Length < Channels) throw new ArgumentException("into 가 채널 수보다 짧다");
            int n = stops.Length;
            if (percent <= stops[0]) { Copy(values[0], into); return; }
            if (percent >= stops[n - 1]) { Copy(values[n - 1], into); return; }
            int hi = 1;
            while (hi < n && stops[hi] < percent) hi++;
            int lo = hi - 1;
            double span = stops[hi] - stops[lo];
            double t = span <= 0 ? 1 : (percent - stops[lo]) / span;
            double[] a = values[lo], b = values[hi];
            for (int c = 0; c < a.Length; c++) into[c] = a[c] + (b[c] - a[c]) * t;
        }

        /// <summary>
        /// 키 사이에 **이징**이 걸린 트랙(`animation: … cubic-bezier(...)`)을 읽는다 — CSS 는 타이밍 함수를
        /// «구간마다» 다시 적용하므로(키프레임 사이 진행 u 를 이징해 보간) 그 뜻 그대로 푼다.
        /// `linear` 면 <see cref="Sample"/> 와 같다.
        /// </summary>
        public void SampleEased(double percent, CssEase ease, double[] into)
        {
            if (ease == null) { Sample(percent, into); return; }
            if (into == null || into.Length < Channels) throw new ArgumentException("into 가 채널 수보다 짧다");
            int n = stops.Length;
            if (percent <= stops[0]) { Copy(values[0], into); return; }
            if (percent >= stops[n - 1]) { Copy(values[n - 1], into); return; }
            int hi = 1;
            while (hi < n && stops[hi] < percent) hi++;
            int lo = hi - 1;
            double span = stops[hi] - stops[lo];
            double t = span <= 0 ? 1 : (percent - stops[lo]) / span;
            double e = ease.Ease(t);
            double[] a2 = values[lo], b2 = values[hi];
            for (int c = 0; c < a2.Length; c++) into[c] = a2[c] + (b2[c] - a2[c]) * e;
        }

        /// <summary>시간(ms)에서 값을 읽는다 — `--afdur` 를 준다.</summary>
        public void SampleMs(double ms, double durationMs, double[] into)
        {
            if (durationMs <= 0) throw new ArgumentException("durationMs 는 0보다 커야 한다");
            Sample(ms / durationMs * 100.0, into);
        }

        /// <summary>채널 하나짜리 트랙을 바로 읽는다(불투명도 계열).</summary>
        public double Sample1(double percent)
        {
            double[] tmp = new double[Channels];
            Sample(percent, tmp);
            return tmp[0];
        }

        /// <summary>채널 하나짜리 트랙을 시간으로 읽는다.</summary>
        public double Sample1Ms(double ms, double durationMs)
        {
            return Sample1(ms / durationMs * 100.0);
        }

        private static void Copy(double[] from, double[] into)
        {
            for (int i = 0; i < from.Length; i++) into[i] = from[i];
        }
    }
}
