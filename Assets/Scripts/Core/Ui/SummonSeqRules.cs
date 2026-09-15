using System;
using System.Collections.Generic;
using Forge.Core.CraftFx;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>
    /// T334 — 소환 결과 연출의 **상태 기계**(정본 `ui.js` `tickSummonResult` 703~731 · `fireSummonHero` 736~758 · `finishSummonResult` 761~765).
    ///
    /// 정본은 매 프레임 «지금까지 떴어야 할 셀» 을 경과 시간으로 다시 판정해 밀린 만큼 따라잡고(한 프레임에 여러 개가 몰려도
    /// 효과음은 최고 등급 하나 · <see cref="Tick"/> 의 반환값), 홀드백이고 마지막 한 칸을 남긴 순간부터 `charging`(정지가 아니라 축적),
    /// 주역 셀이 착지하는 프레임에 `hero`(+ 홀드백이면 `flash` · 아니면 주역 셀 중심 가산 원형 `wipe` — 정본 주석 «x75 신화가 x5 신화보다
    /// 약하다» 의 원인이 이 분기였다), 전부 뜬 시각에서 `SR_TAIL_MS` 뒤 `done`(`charging` 해제). 수치·지연표는 인자로 받는다 —
    /// 표(`SummonFxUi.json` `seq` 절)와 러너는 2회차(T179 lock 뒤). UnityEngine 참조 0.
    /// </summary>
    public sealed class SummonSeqRun
    {
        readonly double[] delays;
        readonly int[] rank;

        /// <summary>셀 수.</summary>
        public int Count { get { return delays.Length; } }
        /// <summary>홀드백(최고 등급 1개를 마지막에 한 박자 늦게) — 정본 `_srHoldback`.</summary>
        public bool Holdback { get; private set; }
        /// <summary>주역(최고 등급) 셀 번호 · 없으면 −1 — 정본 `_srHeroIdx`.</summary>
        public int HeroIdx { get; private set; }
        /// <summary>마지막 아이콘이 뜬 뒤 [확인]까지의 여운(ms) — 정본 `SR_TAIL_MS`.</summary>
        public double TailMs { get; private set; }
        /// <summary>주역 비트(광창·충격파·화면 킥) 길이(ms) — 정본 fireSummonHero 주석 «350ms».</summary>
        public double HeroKickMs { get; private set; }

        /// <summary>지금까지 뜬 셀 수(정본 `_srIdx`).</summary>
        public int Revealed { get; private set; }
        /// <summary>`charging` — 홀드백 대기 구간(소환진·눈금·비네트).</summary>
        public bool Charging { get; private set; }
        /// <summary>주역이 착지한 시각(ms) · 아직이면 −1.</summary>
        public double HeroAtMs { get; private set; }
        /// <summary>`flash` — 뜸들인 단독 등장의 전 화면 섬광(홀드백일 때만).</summary>
        public bool Flash { get; private set; }
        /// <summary>`wipe` — 홀드백이 없는 주역(대량 소환)의 주역 셀 중심 가산 원형 와이프.</summary>
        public bool Wipe { get; private set; }
        /// <summary>전부 뜬 뒤 `done` 이 되는 시각(ms) · 아직이면 −1.</summary>
        public double FinishAtMs { get; private set; }
        /// <summary>`done` — 힌트를 감추고 [확인]을 띄운 상태.</summary>
        public bool Done { get; private set; }

        public bool Hero { get { return HeroAtMs >= 0; } }

        /// <param name="delaysMs">셀마다 뜨는 시각(ms · 오름차순 · 정본 `_srDelays`).</param>
        /// <param name="rarityRank">셀마다 등급 순위(정본 `RARITIES.indexOf` · 클수록 높다).</param>
        public SummonSeqRun(IList<double> delaysMs, IList<int> rarityRank, bool holdback, int heroIdx, double tailMs, double heroKickMs)
        {
            if (delaysMs == null || rarityRank == null || delaysMs.Count != rarityRank.Count) throw new ArgumentException("delays 와 rank 의 길이가 다르다");
            delays = new double[delaysMs.Count]; rank = new int[rarityRank.Count];
            for (int i = 0; i < delays.Length; i++) { delays[i] = delaysMs[i]; rank[i] = rarityRank[i]; }
            Holdback = holdback; HeroIdx = heroIdx; TailMs = tailMs; HeroKickMs = heroKickMs;
            HeroAtMs = -1; FinishAtMs = -1;
        }

        /// <summary>
        /// 정본 `tickSummonResult` 한 프레임. 밀린 셀을 다 띄우고 상태를 옮긴다.
        /// 반환 = 이번 프레임에 뜬 것 중 최고 등급 순위(효과음은 그 하나만) · 아무것도 안 떴으면 −1.
        /// </summary>
        public int Tick(double elapsedMs)
        {
            int loud = -1;
            while (Revealed < Count && delays[Revealed] <= elapsedMs)
            {
                if (rank[Revealed] > loud) loud = rank[Revealed];
                Revealed++;
            }
            if (Done) return loud;
            // 홀드백 대기 구간은 «정지» 가 아니라 «축적» — 마지막 한 칸을 남긴 순간부터.
            if (Holdback && Revealed == Count - 1) Charging = true;
            // 주역 셀이 착지하는 프레임: 그 셀 위치를 원점으로 등급색 한 번.
            if (loud >= 0 && Revealed >= Count && HeroIdx >= 0 && !Hero)
            {
                HeroAtMs = elapsedMs;
                Charging = false;
                if (Holdback) Flash = true; else Wipe = true;
            }
            if (Revealed >= Count && FinishAtMs < 0) FinishAtMs = elapsedMs + TailMs;
            if (FinishAtMs >= 0 && elapsedMs >= FinishAtMs)
            {
                Done = true;
                Charging = false;
            }
            return loud;
        }

        /// <summary>주역 비트가 도는 중인가(착지 뒤 `HeroKickMs` 안).</summary>
        public bool HeroKicking(double elapsedMs) { return Hero && elapsedMs >= HeroAtMs && elapsedMs - HeroAtMs < HeroKickMs; }
    }

    /// <summary>
    /// T334 3회차 ⓑ — 홀드백 대기 구간(`#summon-result-modal.charging`)의 **키프레임 넷**(정본 `style.css` 6815~6879):
    /// 소환진 `srfloorcharge`(.28s linear · 부풀다 마지막 12%에 **수축** · 밝기는 계속 오른다) · 눈금 `srtickup`(`steps(9)` 순차 점등) ·
    /// 중앙 광원 `srhalocharge`(맥동 간격이 132→77ms 로 좁아지는 다섯 산) · 비네트 `srvig`(불투명도 0→1 · 배율 1.10→1) ·
    /// 정착한 조연 셀의 흡기 `srinhale`(슬롯→광원 벡터의 일부만큼 되돌리며 scale 1→.958).
    ///
    /// ⚠ 정본이 세 번 못 박은 것 — **보간은 전부 `linear`** 다(`ease-in` 은 앞 절반이 정지 프레임이 된다).
    ///    가속감은 이징이 아니라 **키프레임 간격**으로만 만든다(뒤로 갈수록 값 폭이 커진다). 표에 `ease` 를 넣지 않는 까닭이다.
    /// ⚠ 비네트는 `background` 가 아니라 `opacity`+`transform` 이다 — 그라디언트를 키프레임으로 만들면 화면에서 계단이 된다(정본 5738~5744).
    ///
    /// 수치는 전부 표(`SummonFxUi.json` 의 `charge` 절)에서 온다(§1) · 시계는 공용 CSS 키프레임 기계(<see cref="RewardBurstSpec.Track"/>) · UnityEngine 참조 0.
    /// </summary>
    public sealed class SummonChargeSpec
    {
        /// <summary>충전 램프 한 번의 길이(ms · 정본 `.28s`).</summary>
        public double ChargeMs;
        /// <summary>눈금 점등 계단 수(정본 `steps(9)`) · 그 처음·끝 불투명도.</summary>
        public double TickSteps, TickA0, TickA1;
        /// <summary>비네트 타원(정본 `radial-gradient(82% 51% at 50% 44%, …)`) — 반지름 비·중심 y·속 투명 반경·바깥 알파.</summary>
        public double VigRxF, VigRyF, VigCyF, VigInnerF, VigOuterA;
        /// <summary>비네트를 굽는 한 변(px).</summary>
        public double VigBakePx;
        public RewardBurstSpec.Track Floor, Halo, Vig, Inhale;

        public static SummonChargeSpec From(JsonObject root)
        {
            JsonObject c = J.Obj(J.Require(root, "charge"));
            var s = new SummonChargeSpec
            {
                ChargeMs = J.Num(J.Require(c, "charge_ms")),
                TickSteps = J.Num(J.Require(c, "tick_steps")),
                TickA0 = J.Num(J.Require(c, "tick_a0")),
                TickA1 = J.Num(J.Require(c, "tick_a1")),
                VigRxF = J.Num(J.Require(c, "vig_rx_f")),
                VigRyF = J.Num(J.Require(c, "vig_ry_f")),
                VigCyF = J.Num(J.Require(c, "vig_cy_f")),
                VigInnerF = J.Num(J.Require(c, "vig_inner_f")),
                VigOuterA = J.Num(J.Require(c, "vig_outer_a")),
                VigBakePx = J.Num(J.Require(c, "vig_bake_px")),
            };
            if (s.ChargeMs <= 0) throw new FormatException("SummonFxUi charge: charge_ms 는 0보다 커야 한다");
            if (s.TickSteps < 1) throw new FormatException("SummonFxUi charge: tick_steps 는 1 이상이어야 한다");
            if (s.VigInnerF < 0 || s.VigInnerF >= 1) throw new FormatException("SummonFxUi charge: vig_inner_f 는 0 이상 1 미만이어야 한다");
            s.Floor = Stops(c, "srfloorcharge");
            s.Halo = Stops(c, "srhalocharge");
            s.Vig = Stops(c, "srvig");
            s.Inhale = Stops(c, "srinhale");
            return s;
        }

        /// <summary>키프레임 배열 하나 — 퍼센트 오름차순 · 이징은 정본대로 전부 linear(표에 `ease` 를 안 쓴다).</summary>
        static RewardBurstSpec.Track Stops(JsonObject c, string name)
        {
            var list = J.List(J.Require(c, name), x => J.Obj(x));
            if (list.Count < 2) throw new FormatException("SummonFxUi charge: " + name + " 키프레임이 둘 미만이다");
            var keys = new RewardBurstSpec.KeyStop[list.Count];
            double prev = -1;
            for (int i = 0; i < list.Count; i++)
            {
                JsonObject o = list[i];
                var k = new RewardBurstSpec.KeyStop { At = J.Num(J.Require(o, "at")) };
                if (k.At < prev) throw new FormatException("SummonFxUi charge: " + name + " 퍼센트는 오름차순이어야 한다");
                prev = k.At;
                foreach (var kv in o)
                {
                    if (kv.Key == "at") continue;
                    if (kv.Key == "ease") throw new FormatException("SummonFxUi charge: " + name + " 에 ease 를 두지 마라 — 정본이 세 번 못 박은 대로 이 구간은 전부 linear 다");
                    if (!(kv.Value is double)) throw new FormatException("SummonFxUi charge: 키프레임 칸 «" + kv.Key + "» 은 수여야 한다");
                    k.Num[kv.Key] = (double)kv.Value;
                }
                keys[i] = k;
            }
            return new RewardBurstSpec.Track { Keys = keys };
        }

        /// <summary>충전 시작에서 <paramref name="ms"/> 뒤의 진행 퍼센트(끝나면 100 에 머문다 — 정본 `forwards`).</summary>
        public double Percent(double ms)
        {
            if (ms <= 0) return 0;
            if (ms >= ChargeMs) return 100;
            return ms / ChargeMs * 100;
        }

        public double FloorAt(double ms, string field) { return Floor.Sample(Percent(ms), field, null); }
        public double HaloAt(double ms, string field) { return Halo.Sample(Percent(ms), field, null); }
        public double VigAt(double ms, string field) { return Vig.Sample(Percent(ms), field, null); }
        public double InhaleAt(double ms, string field) { return Inhale.Sample(Percent(ms), field, null); }

        /// <summary>
        /// 눈금 불투명도 — 정본 `steps(9)`(기본 `end`): 진행을 **아홉 칸으로 끊어** 순차 점등으로 읽히게 한다.
        /// 계단이라 값이 구간 안에서는 안 움직인다 — 그 대신 이 구간의 «안 멈춤» 은 소환진·광원·비네트가 맡는다(정본 주석).
        /// </summary>
        public double TickAlpha(double ms)
        {
            double p = Percent(ms) / 100.0;
            double step = Math.Floor(p * TickSteps) / TickSteps;
            if (step > 1) step = 1;
            return TickA0 + (TickA1 - TickA0) * step;
        }
    }

    /// <summary>
    /// T334 5회차 — 연출이 끝난 뒤(`#summon-result-modal.done`) 셀이 도는 **아이들 호흡**(정본 `srbreath` · style.css 6885~6906).
    ///
    /// 정본이 이 구간을 고쳐 쓴 까닭이 주석에 있다: «예전엔 전 등급이 똑같이 −.16rem / ×1.055 였고 실측상 등급 간 차이는
    /// 광채에서만 나왔다 — 즉 **위계가 구조가 아니라 부산물**이었다. `--idle` 로 호흡 자체를 계단화해 최고 등급이 가장 크게 숨쉰다».
    /// 그래서 진폭은 등급마다 다른 무게(<see cref="Weight"/>)를 탄다. 셀마다 `i × .21s` 씩 늦게 시작해 물결이 된다.
    /// UnityEngine 참조 0.
    /// </summary>
    public sealed class SummonIdleSpec
    {
        /// <summary>한 번 숨쉬는 길이(ms · 정본 2.6s) · 셀 사이 지연(ms · 정본 .21s).</summary>
        public double IdleMs, DelayStepMs;
        /// <summary>정점에서 뜨는 거리(rem · 정본 −.16rem — 음수 = 위로) · 배율 증가분(정본 .055).</summary>
        public double TyRem, ScaleF;
        /// <summary>등급 계단(정본 `--idle`).</summary>
        public double[] Tier;

        public static SummonIdleSpec From(JsonObject root)
        {
            JsonObject o = J.Obj(J.Require(root, "idle"));
            double[] w = J.NumArr(J.Require(o, "weight"));
            if (w == null || w.Length < 2) throw new FormatException("SummonFxUi idle: weight 는 등급 계단(둘 이상)이다");
            for (int i = 1; i < w.Length; i++) if (w[i] < w[i - 1]) throw new FormatException("SummonFxUi idle: weight 는 등급이 오를수록 커져야 한다(위계가 구조여야 한다 — 정본 주석)");
            var s = new SummonIdleSpec
            {
                IdleMs = J.Num(J.Require(o, "idle_ms")),
                DelayStepMs = J.Num(J.Require(o, "delay_step_ms")),
                TyRem = J.Num(J.Require(o, "ty_rem")),
                ScaleF = J.Num(J.Require(o, "scale_f")),
                Tier = w,
            };
            if (s.IdleMs <= 0) throw new FormatException("SummonFxUi idle: idle_ms 는 0보다 커야 한다");
            if (s.TyRem > 0) throw new FormatException("SummonFxUi idle: ty_rem 은 위로 뜨는 값(음수)이다");
            return s;
        }

        /// <summary>그 등급의 무게 — 표 밖 등급은 양 끝으로 자른다.</summary>
        public double Weight(int tier) { return Tier[tier < 0 ? 0 : tier >= Tier.Length ? Tier.Length - 1 : tier]; }

        /// <summary>
        /// 셀 <paramref name="index"/>(등급 <paramref name="tier"/>)의 지금 호흡 — 0(제자리) ↔ 1(정점) 사이의 진행.
        /// 정본은 `ease-in-out` 무한 왕복이라 코사인 한 번으로 같아진다.
        /// </summary>
        public double Phase(double elapsedMs, int index, out double tyRem, out double scaleAdd)
        {
            double t = elapsedMs - index * DelayStepMs;
            double u = t <= 0 ? 0 : (t % IdleMs) / IdleMs;
            double k = 0.5 - 0.5 * Math.Cos(u * Math.PI * 2.0);
            tyRem = TyRem * k; scaleAdd = ScaleF * k;
            return k;
        }

        /// <summary>그 셀의 지금 뜬 거리(rem)와 배율 증가분 — 등급 무게를 태운 값.</summary>
        public void At(double elapsedMs, int index, int tier, out double tyRem, out double scaleAdd)
        {
            double w = Weight(tier);
            Phase(elapsedMs, index, out tyRem, out scaleAdd);
            tyRem *= w; scaleAdd *= w;
        }
    }

    /// <summary>
    /// T334 16회차 — **등급 챕터 펄스**(정본 `.sr-tierpulse` · style.css 6402~6416 · `fillSummonTierBreaks` ui.js 690~700).
    ///
    /// 대량 판(&gt;10셀)의 등급 경계 정지(`SR_TIER_PAUSE_MS`)를 채우는 예고다. 정본이 링(`.sr-tierflash`) 말고
    /// **전화면 펄스**를 따로 둔 까닭을 주석이 실측으로 적었다: «링만으로는 화면 평균 휘도가 안 움직인다 —
    /// 면적이 작은 층은 아무리 밝아도 중반 진폭 계측에 안 잡힌다. 챕터가 바뀌는 순간 화면 전체가
    /// 그 등급색으로 한 번 달아올랐다 식는다».
    ///
    /// ⚠ 세기 `--pk` 는 **셋째 수**다 — CSS 등급 계단(`tier.glow`)도, 재점화의 `0.16 + tier × 0.13` 도 아닌
    ///   `fillSummonTierBreaks` 의 `0.15 + tier × 0.04` 다. 같은 이름의 «등급 세기» 가 겹마다 다르다.
    /// ⚠ 시각은 경계 셀의 등장보다 <see cref="LeadMs"/> **앞**이되 충전 끝보다 앞서지 않는다
    ///   (정본 «예고 → 그 등급 등장» 의 인과).
    /// UnityEngine 참조 0.
    /// </summary>
    public sealed class SummonTierBreakSpec
    {
        /// <summary>경계 셀 등장보다 얼마나 앞서 켜는가(ms · 정본 160).</summary>
        public double LeadMs;
        /// <summary>한 번 달아올랐다 식는 길이(ms · 정본 .54s).</summary>
        public double PulseMs;
        /// <summary>판을 화면 밖으로 얼마나 물리는가(비율 · 정본 `inset: -2%`).</summary>
        public double InsetF;
        /// <summary>바탕 타원 그라디언트의 반지름·중심(비율 · 정본 `120% 90% at 50% 42%`).</summary>
        public double Rx, Ry, Cx, Cy;
        /// <summary>정지점(가운데 하이라이트색 · 등급색 · 투명).</summary>
        public double StopLite, StopRc, StopOut;
        /// <summary>정점 세기(정본 `--pk` = <see cref="PkBase"/> + 등급 × <see cref="PkStep"/>).</summary>
        public double PkBase, PkStep;
        public RewardBurstSpec.Track Pulse;

        public static SummonTierBreakSpec From(JsonObject root)
        {
            JsonObject o = J.Obj(J.Require(root, "tierbreak"));
            var s = new SummonTierBreakSpec
            {
                LeadMs = J.Num(J.Require(o, "break_lead_ms")),
                PulseMs = J.Num(J.Require(o, "pulse_ms")),
                InsetF = J.Num(J.Require(o, "inset_f")),
                Rx = J.Num(J.Require(o, "pulse_rx")),
                Ry = J.Num(J.Require(o, "pulse_ry")),
                Cx = J.Num(J.Require(o, "pulse_cx")),
                Cy = J.Num(J.Require(o, "pulse_cy")),
                StopLite = J.Num(J.Require(o, "stop_lite")),
                StopRc = J.Num(J.Require(o, "stop_rc")),
                StopOut = J.Num(J.Require(o, "stop_out")),
                PkBase = J.Num(J.Require(o, "pk_base")),
                PkStep = J.Num(J.Require(o, "pk_step")),
            };
            if (s.PulseMs <= 0) throw new FormatException("SummonFxUi tierbreak: pulse_ms 는 0보다 커야 한다");
            if (s.LeadMs < 0) throw new FormatException("SummonFxUi tierbreak: 예고는 앞서는 것이다 — break_lead_ms 는 0 이상이다");
            if (s.Rx <= 0 || s.Ry <= 0) throw new FormatException("SummonFxUi tierbreak: 타원 반지름은 0보다 커야 한다");
            if (s.InsetF > 0) throw new FormatException("SummonFxUi tierbreak: inset_f 는 화면 밖으로 무는 값(0 이하)이다");
            if (!(s.StopLite < s.StopRc && s.StopRc < s.StopOut)) throw new FormatException("SummonFxUi tierbreak: 정지점은 stop_lite < stop_rc < stop_out 이어야 한다");
            if (s.PkBase < 0 || s.PkStep < 0) throw new FormatException("SummonFxUi tierbreak: 세기는 음수가 아니다");

            double[] e = J.NumArr(J.Require(o, "pulse_ease"));
            if (e == null || e.Length != 4) throw new FormatException("SummonFxUi tierbreak: pulse_ease 는 cubic-bezier 넷이다");
            CssEase ease = new CssEase(e[0], e[1], e[2], e[3]);
            var list = J.List(J.Require(o, "srtierpulse"), x => J.Obj(x));
            if (list.Count < 2) throw new FormatException("SummonFxUi tierbreak: srtierpulse 키프레임이 둘 미만이다");
            var keys = new RewardBurstSpec.KeyStop[list.Count];
            double prev = -1;
            for (int i = 0; i < list.Count; i++)
            {
                JsonObject k = list[i];
                var ks = new RewardBurstSpec.KeyStop { At = J.Num(J.Require(k, "at")), Ease = ease };
                if (ks.At < prev) throw new FormatException("SummonFxUi tierbreak: 퍼센트는 오름차순이어야 한다");
                prev = ks.At;
                ks.Num["f"] = J.Num(J.Require(k, "f"));
                keys[i] = ks;
            }
            // 펄스는 **달아올랐다 식는다** — 양 끝이 0 이 아니면 화면이 등급색으로 물든 채 굳는다.
            if (keys[0].Num["f"] != 0 || keys[keys.Length - 1].Num["f"] != 0)
                throw new FormatException("SummonFxUi tierbreak: srtierpulse 는 0 에서 시작해 0 으로 식어야 한다");
            s.Pulse = new RewardBurstSpec.Track { Keys = keys };
            ReadFlash(s, o);
            return s;
        }

        /// <summary>그 등급의 정점 세기(정본 `--pk`) — 0~1 로 자른다.</summary>
        public double Pk(int tier)
        {
            double v = PkBase + (tier < 0 ? 0 : tier) * PkStep;
            return v < 0 ? 0 : v > 1 ? 1 : v;
        }

        /// <summary>
        /// 등급 경계가 **켜지는 시각**(ms · 모달이 열린 때부터) — 경계 셀의 등장보다 <see cref="LeadMs"/> 앞이되
        /// 충전이 끝나기 전으로는 당기지 않는다(정본 `Math.max(SR_CHARGE_MS, d - 160)`).
        /// </summary>
        public double BreakAt(double cellDelayMs, double chargeMs)
        {
            double t = cellDelayMs - LeadMs;
            return t < chargeMs ? chargeMs : t;
        }

        /// <summary>켜진 뒤 <paramref name="ms"/> 지난 펄스의 불투명도.</summary>
        public double AlphaAt(double ms, int tier)
        {
            double p = ms <= 0 ? 0 : ms >= PulseMs ? 100 : ms / PulseMs * 100;
            return Pulse.Sample(p, "f", null) * Pk(tier);
        }

        /// <summary>아직 달아올라 있는가.</summary>
        public bool Pulsing(double ms) { return ms >= 0 && ms < PulseMs; }

        // ── 17회차: 챕터 **링**(정본 `.sr-tierflash`)과 그 심지(`::after`) ──────────────────

        /// <summary>링이 퍼지는 길이(ms · 정본 .56s).</summary>
        public double FlashMs;
        /// <summary>링 판 크기 — `min(w_rem rem, w_vw_f × 앱 폭)`(정본 `min(11rem, 46vw)`).</summary>
        public double FlashWRem, FlashWVwF;
        /// <summary>테 굵기·번짐이 **다른 판을 몇 장 구워 갈아 끼우는가**(정본의 연속 변화를 계단으로 근사한다 · 결정 663 과 같은 길).</summary>
        public int FlashSteps;
        /// <summary>링 둘레의 번짐(rem · 정본 `box-shadow 0 0 .9rem`).</summary>
        public double RingGlowRem;
        /// <summary>심지의 안쪽 여백(비율 · 정본 `inset: 18%`)과 정지점·번짐.</summary>
        public double WickInsetF, WickStopLite, WickStopRc, WickStopOut, WickBlurPx;
        public RewardBurstSpec.Track FlashAlpha, FlashGeom, Wick;

        static RewardBurstSpec.Track Ramp(JsonObject o, string key, CssEase ease, string[] fields, string who)
        {
            var list = J.List(J.Require(o, key), x => J.Obj(x));
            if (list.Count < 2) throw new FormatException("SummonFxUi " + who + ": " + key + " 키프레임이 둘 미만이다");
            var keys = new RewardBurstSpec.KeyStop[list.Count];
            double prev = -1;
            for (int i = 0; i < list.Count; i++)
            {
                JsonObject k = list[i];
                var ks = new RewardBurstSpec.KeyStop { At = J.Num(J.Require(k, "at")), Ease = ease };
                if (ks.At < prev) throw new FormatException("SummonFxUi " + who + ": " + key + " 퍼센트는 오름차순이어야 한다");
                prev = ks.At;
                for (int f = 0; f < fields.Length; f++) ks.Num[fields[f]] = J.Num(J.Require(k, fields[f]));
                keys[i] = ks;
            }
            return new RewardBurstSpec.Track { Keys = keys };
        }

        static void ReadFlash(SummonTierBreakSpec s, JsonObject o)
        {
            s.FlashMs = J.Num(J.Require(o, "flash_ms"));
            if (s.FlashMs <= 0) throw new FormatException("SummonFxUi tierbreak: flash_ms 는 0보다 커야 한다");
            s.FlashWRem = J.Num(J.Require(o, "flash_w_rem"));
            s.FlashWVwF = J.Num(J.Require(o, "flash_w_vw_f"));
            if (s.FlashWRem <= 0 || s.FlashWVwF <= 0) throw new FormatException("SummonFxUi tierbreak: 링 크기는 0보다 커야 한다");
            s.FlashSteps = (int)J.Num(J.Require(o, "flash_steps"));
            if (s.FlashSteps < 2) throw new FormatException("SummonFxUi tierbreak: flash_steps 가 2보다 작으면 «굵기가 변한다» 가 안 보인다");
            s.RingGlowRem = J.Num(J.Require(o, "ring_glow_rem"));
            s.WickInsetF = J.Num(J.Require(o, "wick_inset_f"));
            if (s.WickInsetF < 0 || s.WickInsetF >= 0.5) throw new FormatException("SummonFxUi tierbreak: wick_inset_f 는 0~0.5 안이다");
            s.WickStopLite = J.Num(J.Require(o, "wick_stop_lite"));
            s.WickStopRc = J.Num(J.Require(o, "wick_stop_rc"));
            s.WickStopOut = J.Num(J.Require(o, "wick_stop_out"));
            if (!(s.WickStopLite < s.WickStopRc && s.WickStopRc < s.WickStopOut)) throw new FormatException("SummonFxUi tierbreak: 심지 정지점은 lite < rc < out 이어야 한다");
            s.WickBlurPx = J.Num(J.Require(o, "wick_blur_px"));

            double[] fe = J.NumArr(J.Require(o, "flash_ease"));
            if (fe == null || fe.Length != 4) throw new FormatException("SummonFxUi tierbreak: flash_ease 는 cubic-bezier 넷이다");
            CssEase fease = new CssEase(fe[0], fe[1], fe[2], fe[3]);
            s.FlashAlpha = Ramp(o, "srtierflash_a", fease, new[] { "f" }, "tierbreak");
            s.FlashGeom = Ramp(o, "srtierflash_g", fease, new[] { "scale", "border_rem", "blur_px" }, "tierbreak");
            s.Wick = Ramp(o, "srtierwick", fease, new[] { "f" }, "tierbreak");

            // 링은 **떠올랐다 사라진다** — 양 끝이 0 이 아니면 결과 화면에 등급색 테가 남는다.
            if (s.FlashAlpha.Keys[0].Num["f"] != 0 || s.FlashAlpha.Keys[s.FlashAlpha.Keys.Length - 1].Num["f"] != 0)
                throw new FormatException("SummonFxUi tierbreak: srtierflash_a 는 0 에서 시작해 0 으로 사라져야 한다");
            if (s.Wick.Keys[0].Num["f"] != 0 || s.Wick.Keys[s.Wick.Keys.Length - 1].Num["f"] != 0)
                throw new FormatException("SummonFxUi tierbreak: srtierwick 은 0 에서 시작해 0 으로 사라져야 한다");
            // 정본이 못 박은 압력파 문법: **퍼질수록 얇아지고 번진다**. 굵기가 커지면 «그래픽 스탬프» 가 된다.
            var g0 = s.FlashGeom.Keys[0];
            var g1 = s.FlashGeom.Keys[s.FlashGeom.Keys.Length - 1];
            if (g1.Num["scale"] <= g0.Num["scale"]) throw new FormatException("SummonFxUi tierbreak: 링은 퍼져야 한다(배율이 커진다)");
            if (g1.Num["border_rem"] >= g0.Num["border_rem"]) throw new FormatException("SummonFxUi tierbreak: 링은 퍼질수록 **얇아져야** 한다(정본 «하드엣지 고정 굵기는 그래픽 스탬프다»)");
            if (g1.Num["blur_px"] <= g0.Num["blur_px"]) throw new FormatException("SummonFxUi tierbreak: 링은 퍼질수록 **번져야** 한다");
        }

        /// <summary>켜진 뒤 <paramref name="ms"/> 지난 링의 불투명도·배율·테 굵기(rem)·번짐(px).</summary>
        public void FlashAt(double ms, out double alpha, out double scale, out double borderRem, out double blurPx)
        {
            double p = ms <= 0 ? 0 : ms >= FlashMs ? 100 : ms / FlashMs * 100;
            alpha = FlashAlpha.Sample(p, "f", null);
            scale = FlashGeom.Sample(p, "scale", null);
            borderRem = FlashGeom.Sample(p, "border_rem", null);
            blurPx = FlashGeom.Sample(p, "blur_px", null);
        }

        /// <summary>켜진 뒤 <paramref name="ms"/> 지난 심지의 불투명도.</summary>
        public double WickAt(double ms)
        {
            double p = ms <= 0 ? 0 : ms >= FlashMs ? 100 : ms / FlashMs * 100;
            return Wick.Sample(p, "f", null);
        }

        /// <summary>
        /// 그 시각에 쓸 **구운 판의 번호**(0 ~ <see cref="FlashSteps"/>−1) — 테 굵기·번짐은 판마다 다르고 단계 경계에서 갈아 끼운다.
        /// 굽는 쪽이 그 번호의 «대표 시각» 을 <see cref="StepMid"/> 로 받아 그때의 굵기로 굽는다.
        /// </summary>
        public int StepOf(double ms)
        {
            double p = ms <= 0 ? 0 : ms >= FlashMs ? 1 : ms / FlashMs;
            int k = (int)(p * FlashSteps);
            return k < 0 ? 0 : k >= FlashSteps ? FlashSteps - 1 : k;
        }

        /// <summary>그 단계의 대표 시각(ms · 단계 한가운데).</summary>
        public double StepMid(int step) { return FlashMs * (step + 0.5) / FlashSteps; }
    }

    /// <summary>
    /// T334 15회차 — **비행 잔상**(정본 `.sr-ghost` · style.css 6448~6474).
    ///
    /// 정본 주석: «팝이 아래에서 올라오는데 궤적이 없으면 «순간이동 후 튕김» 으로 보인다» ·
    /// «잔상은 «아래에서 솟은 자국» 이 아니라 **비행 경로에 끌리는 꼬리** 다 — 셀이 광원 쪽에서 날아오므로
    /// 잔상은 그 뒤쪽(광원 쪽)에 남아 따라붙는다. 셀 안에 있어서 셀의 이동이 이미 곱해진 상태라,
    /// 여기서는 «뒤처진 만큼» 만 더 민다».
    ///
    /// ⚠ **제 길이가 없다** — `animation: srghost var(--pop)` 이라 그 셀의 팝 길이(등급 계단)를 그대로 쓴다.
    ///   그래서 <see cref="At"/> 가 길이를 인자로 받는다.
    /// ⚠ 치우침은 `--dx/--dy` 에 곱하는 비율이고 그 벡터는 **전체가 아니라 `SR_EJECT` 를 곱한 것**이다(`layout.eject_f`).
    /// ⚠ 꼬리 마디의 76% 를 줄이지 말 것 — 정본이 «비행 창(0→76%)에 맞춰 다시 찍었다» 고 적어 뒀다
    ///   (예전엔 55% 에 사그라들어 늘어난 비행의 뒷부분에 꼬리가 없었다).
    /// UnityEngine 참조 0.
    /// </summary>
    public sealed class SummonGhostSpec
    {
        /// <summary>바탕 방사 그라디언트의 정지점(가운데 하이라이트색 · 등급색 · 투명).</summary>
        public double StopLite, StopRc, StopOut;
        /// <summary>정본 `filter: blur(6px)` — 굽는 쪽이 이미 매끈한 감쇠라 참고값으로만 둔다.</summary>
        public double BlurPx;
        /// <summary>등급 계단(정본 `--glow` · `tier.glow`).</summary>
        public double[] TierGlow;
        public RewardBurstSpec.Track Ghost;

        public static SummonGhostSpec From(JsonObject root)
        {
            double[] g = J.NumArr(J.Require(J.Obj(J.Require(root, "tier")), "glow"));
            if (g == null || g.Length < 2) throw new FormatException("SummonFxUi tier: glow 는 등급 계단(둘 이상)이다");

            JsonObject o = J.Obj(J.Require(root, "ghost"));
            var s = new SummonGhostSpec
            {
                StopLite = J.Num(J.Require(o, "stop_lite")),
                StopRc = J.Num(J.Require(o, "stop_rc")),
                StopOut = J.Num(J.Require(o, "stop_out")),
                BlurPx = J.Num(J.Require(o, "blur_px")),
                TierGlow = g,
            };
            if (!(s.StopLite < s.StopRc && s.StopRc < s.StopOut)) throw new FormatException("SummonFxUi ghost: 정지점은 stop_lite < stop_rc < stop_out 이어야 한다");
            if (s.StopOut > 1) throw new FormatException("SummonFxUi ghost: stop_out 은 판 반지름의 비율(1 이하)이다");

            double[] e = J.NumArr(J.Require(o, "ghost_ease"));
            if (e == null || e.Length != 4) throw new FormatException("SummonFxUi ghost: ghost_ease 는 cubic-bezier 넷이다");
            CssEase ease = new CssEase(e[0], e[1], e[2], e[3]);
            var list = J.List(J.Require(o, "srghost"), x => J.Obj(x));
            if (list.Count < 2) throw new FormatException("SummonFxUi ghost: srghost 키프레임이 둘 미만이다");
            var keys = new RewardBurstSpec.KeyStop[list.Count];
            double prev = -1;
            for (int i = 0; i < list.Count; i++)
            {
                JsonObject k = list[i];
                var ks = new RewardBurstSpec.KeyStop { At = J.Num(J.Require(k, "at")), Ease = ease };
                if (ks.At < prev) throw new FormatException("SummonFxUi ghost: 퍼센트는 오름차순이어야 한다");
                prev = ks.At;
                ks.Num["back_f"] = J.Num(J.Require(k, "back_f"));
                ks.Num["scale"] = J.Num(J.Require(k, "scale"));
                ks.Num["a_base"] = J.Num(J.Require(k, "a_base"));
                ks.Num["a_glow"] = J.Num(J.Require(k, "a_glow"));
                keys[i] = ks;
            }
            // 꼬리는 **본체를 따라잡고 사라진다** — 마지막 키가 제자리·알파 0 이 아니면 결과 화면에 흐린 원이 남는다.
            var last = keys[keys.Length - 1];
            if (last.Num["back_f"] != 0 || last.Num["a_base"] != 0 || last.Num["a_glow"] != 0 || last.Num["scale"] != 1)
                throw new FormatException("SummonFxUi ghost: srghost 의 마지막 키는 제자리(치우침 0 · 배율 1)에서 알파 0 이어야 한다");
            // 꼬리는 **광원 쪽으로만** 뒤처진다 — 비율이 음수면 진행 방향 앞에 서서 «앞서 나간 잔상» 이 된다.
            // 그리고 뒤처진 정도는 줄어들기만 한다(따라붙는다).
            double p0 = keys[0].Num["back_f"];
            if (p0 <= 0) throw new FormatException("SummonFxUi ghost: 첫 키의 치우침은 0보다 커야 한다(꼬리가 광원 쪽에 남는다)");
            for (int i = 1; i < keys.Length; i++)
                if (keys[i].Num["back_f"] > keys[i - 1].Num["back_f"]) throw new FormatException("SummonFxUi ghost: 꼬리는 따라붙기만 한다 — 치우침이 커지면 안 된다");
            s.Ghost = new RewardBurstSpec.Track { Keys = keys };
            return s;
        }

        /// <summary>그 등급의 세기(정본 `--glow` 계단).</summary>
        public double Glow(int tier) { return TierGlow[tier < 0 ? 0 : tier >= TierGlow.Length ? TierGlow.Length - 1 : tier]; }

        /// <summary>
        /// 셀이 뜬 뒤 <paramref name="ms"/> 지난 잔상의 치우침 비율·배율·불투명도.
        /// <paramref name="popMs"/> 는 **그 셀의 팝 길이**다(정본 `var(--pop)`).
        /// <paramref name="backF"/> 는 «슬롯 → 광원» 사출 벡터(`--dx/--dy`)에 곱할 비율.
        /// </summary>
        public void At(double ms, double popMs, int tier, out double backF, out double scale, out double alpha)
        {
            double p = popMs <= 0 || ms >= popMs ? 100 : ms <= 0 ? 0 : ms / popMs * 100;
            backF = Ghost.Sample(p, "back_f", null);
            scale = Ghost.Sample(p, "scale", null);
            alpha = Ghost.Sample(p, "a_base", null) + Glow(tier) * Ghost.Sample(p, "a_glow", null);
        }

        /// <summary>아직 꼬리가 남아 있는가.</summary>
        public bool Trailing(double ms, double popMs) { return ms >= 0 && ms < popMs; }
    }

    /// <summary>
    /// T334 12회차 — **착지 스파크**(정본 `.sr-spark` · style.css 6463~6484).
    ///
    /// 정본 주석: «링 하나로는 «내려앉았다» 만 말하고 «부딪혔다» 를 말하지 못한다 —
    /// `box-shadow` 8방향 복제를 `transform: scale` 로 바깥으로 날린다(오프셋도 함께 확대된다)».
    ///
    /// ⚠ 알파는 0→62→100 **두 구간**으로 이징하는데 배율은 0%·100% 에만 적혀 **한 구간**이다
    ///   (CSS 는 이징을 키프레임 «구간마다» 건다) — 그래서 트랙을 둘로 나눈다. 한 트랙에 넣으면 배율이 62% 에서 꺾인다.
    /// ⚠ 세기 `--glow` 는 **CSS 등급 계단**(`tier.glow` · 6327~6332)이다 — 재점화(`relight`)가 쓰는
    ///   `0.16 + tier × 0.13` 과 **다른 수**다(그쪽은 `fillSummonRelights` 가 인라인으로 심는다).
    /// UnityEngine 참조 0.
    /// </summary>
    public sealed class SummonSparkSpec
    {
        /// <summary>한 번 튀는 길이(ms · 정본 .42s).</summary>
        public double Ms;
        /// <summary>심 원의 지름(rem · 정본 .22rem) · 축 복제 거리(rem) · 대각 복제 거리(rem).</summary>
        public double DotRem, AxisRem, DiagRem;
        /// <summary>복제의 퍼짐(rem · 음수 — 복제 반지름이 그만큼 줄어든다). 위→오른쪽→아래→왼쪽 · 대각은 그 사이.</summary>
        public double[] AxisSpreadRem, DiagSpreadRem;
        /// <summary>굽는 판 한 변(rem) — 축 복제가 안 잘리는 가장 작은 상자.</summary>
        public double BoxRem;
        /// <summary>등급 계단(정본 `.sr-cell` 의 `--glow`).</summary>
        public double[] TierGlow;
        /// <summary>알파·배율 트랙 — 칸은 `calc(base + glow × --glow)` 그대로 둘이다.</summary>
        public RewardBurstSpec.Track Alpha, Scale;

        public static SummonSparkSpec From(JsonObject root)
        {
            double[] g = J.NumArr(J.Require(J.Obj(J.Require(root, "tier")), "glow"));
            if (g == null || g.Length < 2) throw new FormatException("SummonFxUi tier: glow 는 등급 계단(둘 이상)이다");
            for (int i = 1; i < g.Length; i++) if (g[i] < g[i - 1]) throw new FormatException("SummonFxUi tier: glow 는 등급이 오를수록 커져야 한다");

            JsonObject o = J.Obj(J.Require(root, "spark"));
            var s = new SummonSparkSpec
            {
                Ms = J.Num(J.Require(o, "spark_ms")),
                DotRem = J.Num(J.Require(o, "dot_rem")),
                AxisRem = J.Num(J.Require(o, "axis_rem")),
                DiagRem = J.Num(J.Require(o, "diag_rem")),
                AxisSpreadRem = J.NumArr(J.Require(o, "axis_spread_rem")),
                DiagSpreadRem = J.NumArr(J.Require(o, "diag_spread_rem")),
                BoxRem = J.Num(J.Require(o, "box_rem")),
                TierGlow = g,
            };
            if (s.Ms <= 0) throw new FormatException("SummonFxUi spark: spark_ms 는 0보다 커야 한다");
            if (s.DotRem <= 0 || s.AxisRem <= 0 || s.DiagRem <= 0) throw new FormatException("SummonFxUi spark: 심·거리는 0보다 커야 한다");
            if (s.AxisSpreadRem == null || s.AxisSpreadRem.Length != 4 || s.DiagSpreadRem == null || s.DiagSpreadRem.Length != 4)
                throw new FormatException("SummonFxUi spark: 퍼짐은 축 넷·대각 넷이다(정본 box-shadow 여덟 겹)");
            // 퍼짐이 심 반지름보다 더 깎으면 그 복제가 사라진다 — 정본은 여덟이 다 보인다.
            double r = s.DotRem * 0.5;
            for (int i = 0; i < 4; i++)
            {
                if (r + s.AxisSpreadRem[i] <= 0 || r + s.DiagSpreadRem[i] <= 0)
                    throw new FormatException("SummonFxUi spark: 퍼짐이 심 반지름을 다 깎았다 — 그 복제는 화면에서 사라진다");
            }
            // 축 복제가 판 밖으로 나가면 잘린 채 커진다(scale 은 잘린 판을 늘릴 뿐이다).
            if (s.BoxRem < (s.AxisRem + r) * 2) throw new FormatException("SummonFxUi spark: box_rem 이 축 복제를 못 담는다");

            s.Alpha = Pair(o, "srspark_a", J.NumArr(J.Require(o, "spark_ease")));
            s.Scale = Pair(o, "srspark_s", J.NumArr(J.Require(o, "spark_ease")));
            // 스파크는 **꺼진다** — 마지막 키가 0 이 아니면 결과 화면에 흰 점 아홉이 남는다.
            var last = s.Alpha.Keys[s.Alpha.Keys.Length - 1];
            if (last.Num["base"] != 0 || last.Num["glow"] != 0)
                throw new FormatException("SummonFxUi spark: srspark_a 의 마지막 키는 알파 0 이어야 한다");
            return s;
        }

        /// <summary>`calc(base + glow × --glow)` 꼴 트랙 하나를 읽는다.</summary>
        static RewardBurstSpec.Track Pair(JsonObject o, string key, double[] e)
        {
            if (e == null || e.Length != 4) throw new FormatException("SummonFxUi spark: spark_ease 는 cubic-bezier 넷이다");
            CssEase ease = new CssEase(e[0], e[1], e[2], e[3]);
            var list = J.List(J.Require(o, key), x => J.Obj(x));
            if (list.Count < 2) throw new FormatException("SummonFxUi spark: " + key + " 키프레임이 둘 미만이다");
            var keys = new RewardBurstSpec.KeyStop[list.Count];
            double prev = -1;
            for (int i = 0; i < list.Count; i++)
            {
                JsonObject k = list[i];
                var ks = new RewardBurstSpec.KeyStop { At = J.Num(J.Require(k, "at")), Ease = ease };
                if (ks.At < prev) throw new FormatException("SummonFxUi spark: " + key + " 퍼센트는 오름차순이어야 한다");
                prev = ks.At;
                ks.Num["base"] = J.Num(J.Require(k, "base"));
                ks.Num["glow"] = J.Num(J.Require(k, "glow"));
                keys[i] = ks;
            }
            return new RewardBurstSpec.Track { Keys = keys };
        }

        /// <summary>그 등급의 세기(정본 `--glow` 계단) — 표 밖 등급은 양 끝으로 자른다.</summary>
        public double Glow(int tier) { return TierGlow[tier < 0 ? 0 : tier >= TierGlow.Length ? TierGlow.Length - 1 : tier]; }

        /// <summary>복제 여덟의 자리(rem · +y 는 **위** · UGUI 쪽이 그대로 쓴다)와 반지름(rem). 0번은 심.</summary>
        public void Dot(int i, out double x, out double y, out double rRem, out bool rarity)
        {
            double r0 = DotRem * 0.5;
            if (i <= 0) { x = 0; y = 0; rRem = r0; rarity = false; return; }
            int k = (i - 1) / 2, odd = (i - 1) % 2;   // 정본 차례: 위 · 오른위 · 오른 · 오른아래 · 아래 · 왼아래 · 왼 · 왼위
            if (odd == 0)
            {
                double[] ax = { 0, 1, 0, -1 }, ay = { 1, 0, -1, 0 };   // CSS 의 «0 -1.5rem»(위)을 +y 위로 뒤집었다
                x = ax[k] * AxisRem; y = ay[k] * AxisRem; rRem = r0 + AxisSpreadRem[k]; rarity = true;
            }
            else
            {
                double[] dx = { 1, 1, -1, -1 }, dy = { 1, -1, -1, 1 };
                x = dx[k] * DiagRem; y = dy[k] * DiagRem; rRem = r0 + DiagSpreadRem[k]; rarity = false;
            }
        }

        /// <summary>셀이 뜬 뒤 <paramref name="ms"/> 지난 스파크의 불투명도·배율.</summary>
        public void At(double ms, int tier, out double alpha, out double scale)
        {
            double p = ms <= 0 ? 0 : ms >= Ms ? 100 : ms / Ms * 100;
            double g = Glow(tier);
            alpha = Alpha.Sample(p, "base", null) + g * Alpha.Sample(p, "glow", null);
            scale = Scale.Sample(p, "base", null) + g * Scale.Sample(p, "glow", null);
        }

        /// <summary>아직 튀는 중인가.</summary>
        public bool Sparking(double ms) { return ms >= 0 && ms < Ms; }
    }

    /// <summary>
    /// T334 11회차 — **셀별 광원 재점화**(정본 `.sr-relight` · style.css 6364~6389 · `UI.fillSummonRelights` ui.js 638~649).
    ///
    /// 정본이 이 겹을 넣은 까닭이 주석에 실측으로 적혀 있다: 광원 ±20px 평균 휘도가 2~4번 셀이 사출되는 900ms 내내
    /// **45.1~55.2** 로 시작 프레임 baseline 53.3 보다도 낮았다 — 인과(빛 → 아이템)가 첫 셀과 주역에만 걸려 있어
    /// 나머지는 «꺼진 광원에서 튀어나오는 물체» 로 읽혔다. 그래서 셀이 뜰 때마다 **광원 자리**에 그 셀 등급색으로
    /// 짧은 플래시를 한 번 켠다.
    ///
    /// ⚠ 세기(`--glow`)는 CSS 의 등급 계단(6327~6332)이 아니라 `fillSummonRelights` 가 심는 `0.16 + tier × 0.13` 이다.
    /// ⚠ 알파는 표에 **비율**(0 → 1 → 0)로 있고 정점값 `a_base + a_glow × glow` 를 곱한다 — 정점이 상수라 보간 결과는 정본과 같다.
    /// 수치는 표(`SummonFxUi.json` `relight` 절)가 쥔다. UnityEngine 참조 0.
    /// </summary>
    public sealed class SummonRelightSpec
    {
        /// <summary>한 번 켜졌다 꺼지는 길이(ms · 정본 .34s).</summary>
        public double Ms;
        /// <summary>판 크기 — `min(w_rem rem, w_vw_f × 앱 폭)`(정본 `min(9rem, 38vw)`).</summary>
        public double WRem, WVwF;
        /// <summary>바탕 방사 그라디언트의 정지점(가운데 하이라이트색 · 등급색 · 투명).</summary>
        public double StopLite, StopRc, StopOut;
        /// <summary>정본 `filter: blur(3px)` — 굽는 쪽이 이미 매끈한 감쇠라 참고값으로만 둔다.</summary>
        public double BlurPx;
        /// <summary>세기 계단(정본 `fillSummonRelights` 의 `0.16 + tier × 0.13`).</summary>
        public double GlowBase, GlowStep;
        /// <summary>정점 알파 = <see cref="ABase"/> + <see cref="AGlow"/> × glow(정본 `calc(.55 + .42 * var(--glow))`).</summary>
        public double ABase, AGlow;
        public RewardBurstSpec.Track Relight;

        public static SummonRelightSpec From(JsonObject root)
        {
            JsonObject o = J.Obj(J.Require(root, "relight"));
            var s = new SummonRelightSpec
            {
                Ms = J.Num(J.Require(o, "relight_ms")),
                WRem = J.Num(J.Require(o, "w_rem")),
                WVwF = J.Num(J.Require(o, "w_vw_f")),
                StopLite = J.Num(J.Require(o, "stop_lite")),
                StopRc = J.Num(J.Require(o, "stop_rc")),
                StopOut = J.Num(J.Require(o, "stop_out")),
                BlurPx = J.Num(J.Require(o, "blur_px")),
                GlowBase = J.Num(J.Require(o, "glow_base")),
                GlowStep = J.Num(J.Require(o, "glow_step")),
                ABase = J.Num(J.Require(o, "a_base")),
                AGlow = J.Num(J.Require(o, "a_glow")),
            };
            if (s.Ms <= 0) throw new FormatException("SummonFxUi relight: relight_ms 는 0보다 커야 한다");
            if (s.WRem <= 0 || s.WVwF <= 0) throw new FormatException("SummonFxUi relight: 판 크기는 0보다 커야 한다");
            // 그라디언트 정지점은 가운데에서 바깥으로 간다 — 뒤집히면 심지가 테두리에 서고 광원이 «도넛» 이 된다.
            if (!(s.StopLite < s.StopRc && s.StopRc < s.StopOut)) throw new FormatException("SummonFxUi relight: 정지점은 stop_lite < stop_rc < stop_out 이어야 한다");
            if (s.StopOut > 1) throw new FormatException("SummonFxUi relight: stop_out 은 판 반지름의 비율(1 이하)이다");
            // 최고 등급에서도 가산 판의 정점 알파는 1을 넘지 않는다(정본 .55 + .42 = .97).
            if (s.ABase < 0 || s.AGlow < 0 || s.ABase + s.AGlow > 1) throw new FormatException("SummonFxUi relight: a_base + a_glow 는 0~1 이어야 한다");

            double[] e = J.NumArr(J.Require(o, "relight_ease"));
            if (e == null || e.Length != 4) throw new FormatException("SummonFxUi relight: relight_ease 는 cubic-bezier 넷이다");
            CssEase ease = new CssEase(e[0], e[1], e[2], e[3]);
            var list = J.List(J.Require(o, "srrelight"), x => J.Obj(x));
            if (list.Count < 2) throw new FormatException("SummonFxUi relight: srrelight 키프레임이 둘 미만이다");
            var keys = new RewardBurstSpec.KeyStop[list.Count];
            double prev = -1;
            for (int i = 0; i < list.Count; i++)
            {
                JsonObject k = list[i];
                var ks = new RewardBurstSpec.KeyStop { At = J.Num(J.Require(k, "at")), Ease = ease };
                if (ks.At < prev) throw new FormatException("SummonFxUi relight: 퍼센트는 오름차순이어야 한다");
                prev = ks.At;
                ks.Num["alpha_f"] = J.Num(J.Require(k, "alpha_f"));
                ks.Num["scale"] = J.Num(J.Require(k, "scale"));
                keys[i] = ks;
            }
            // 재점화는 **켜졌다 꺼진다** — 양 끝이 0 이 아니면 결과 화면 가운데에 등급색 얼룩이 남는다.
            if (keys[0].Num["alpha_f"] != 0 || keys[keys.Length - 1].Num["alpha_f"] != 0)
                throw new FormatException("SummonFxUi relight: srrelight 는 알파 0 에서 시작해 0 으로 꺼져야 한다");
            s.Relight = new RewardBurstSpec.Track { Keys = keys };
            return s;
        }

        /// <summary>그 등급의 세기(정본 `--glow`) — 0~1 로 자른다.</summary>
        public double Glow(int tier)
        {
            double g = GlowBase + (tier < 0 ? 0 : tier) * GlowStep;
            return g < 0 ? 0 : g > 1 ? 1 : g;
        }

        /// <summary>그 등급 플래시의 정점 알파(정본 `calc(.55 + .42 * var(--glow))`).</summary>
        public double PeakAlpha(int tier) { return ABase + AGlow * Glow(tier); }

        /// <summary>셀이 뜬 뒤 <paramref name="ms"/> 지난 재점화 플래시의 불투명도·배율. 끝나면 꺼진 채로 남는다.</summary>
        public void At(double ms, int tier, out double alpha, out double scale)
        {
            double p = ms <= 0 ? 0 : ms >= Ms ? 100 : ms / Ms * 100;
            alpha = Relight.Sample(p, "alpha_f", null) * PeakAlpha(tier);
            scale = Relight.Sample(p, "scale", null);
        }

        /// <summary>아직 켜져 있는가.</summary>
        public bool Lit(double ms) { return ms >= 0 && ms < Ms; }
    }

    /// <summary>
    /// T334 6회차 — 주역 착지의 **화면 킥**(정본 `srshakehit` · style.css 5675~5684).
    ///
    /// 정본 주석이 두 가지를 못 박았다: ⓐ «최고 등급 착지 — 앞의 것보다 짧고 세게» ⓑ «화면 킥은 **홀드백 여부와 무관하게**
    /// 주역 비트(`.hero`)에 건다» — 즉 섬광(`flash`)이든 와이프(`wipe`)든 판은 똑같이 흔들린다.
    ///
    /// ⚠ 치우침은 `translate3d` 의 **퍼센트**다 — 판 제 크기 기준이지 화면 기준이 아니다. CSS 의 +y 는 아래라 거는 쪽이 뒤집는다.
    /// ⚠ `.hero` 자체에는 **시간 제한이 없다**(`fireSummonHero` 는 클래스만 붙이고 `done` 까지 둔다) — 길이는 이 애니메이션이 쥔다.
    ///    결정 534 가 «주역 비트 길이 미정» 으로 남겨 둔 것의 답이 이것이다: 그런 값은 정본에 없다.
    /// UnityEngine 참조 0.
    /// </summary>
    public sealed class SummonHeroSpec
    {
        /// <summary>한 번 흔드는 길이(ms · 정본 .44s) · 조연이 물러났다 돌아오는 길이(ms · 정본 .68s).</summary>
        public double ShakeMs, RecedeMs, HeroPopMs, BeamMs;
        /// <summary>주역 등장의 0% 치우침 기본값(rem · 슬롯→광원 벡터가 없을 때 아래로).</summary>
        public double HeroPopDy0Rem;
        public RewardBurstSpec.Track Shake, Recede, HeroPop, Beam;
        /// <summary>등급별 하이라이트 목표 휘도(정본 `SR_HILITE_LUMA`).</summary>
        public double[] HiliteLuma;

        public static SummonHeroSpec From(JsonObject root)
        {
            JsonObject o = J.Obj(J.Require(root, "hero"));
            var s = new SummonHeroSpec { ShakeMs = J.Num(J.Require(o, "shake_ms")) };
            if (s.ShakeMs <= 0) throw new FormatException("SummonFxUi hero: shake_ms 는 0보다 커야 한다");
            double[] e = J.NumArr(J.Require(o, "shake_ease"));
            if (e == null || e.Length != 4) throw new FormatException("SummonFxUi hero: shake_ease 는 cubic-bezier 넷이다");
            CssEase ease = new CssEase(e[0], e[1], e[2], e[3]);

            var list = J.List(J.Require(o, "srshakehit"), x => J.Obj(x));
            if (list.Count < 2) throw new FormatException("SummonFxUi hero: srshakehit 키프레임이 둘 미만이다");
            var keys = new RewardBurstSpec.KeyStop[list.Count];
            double prev = -1;
            for (int i = 0; i < list.Count; i++)
            {
                JsonObject k = list[i];
                var ks = new RewardBurstSpec.KeyStop { At = J.Num(J.Require(k, "at")), Ease = ease };
                if (ks.At < prev) throw new FormatException("SummonFxUi hero: 퍼센트는 오름차순이어야 한다");
                prev = ks.At;
                ks.Num["tx_pct"] = J.Num(J.Require(k, "tx_pct"));
                ks.Num["ty_pct"] = J.Num(J.Require(k, "ty_pct"));
                ks.Num["scale"] = J.Num(J.Require(k, "scale"));
                keys[i] = ks;
            }
            // 흔들기는 **제자리에서 시작해 제자리로 돌아온다** — 양 끝이 0 이 아니면 판이 튄 채로 남는다.
            if (keys[0].Num["tx_pct"] != 0 || keys[0].Num["ty_pct"] != 0) throw new FormatException("SummonFxUi hero: 첫 키는 제자리여야 한다");
            var last = keys[keys.Length - 1];
            if (last.Num["tx_pct"] != 0 || last.Num["ty_pct"] != 0 || last.Num["scale"] != 1) throw new FormatException("SummonFxUi hero: 마지막 키는 제자리로 돌아와야 한다");
            s.Shake = new RewardBurstSpec.Track { Keys = keys };

            // 조연 물러남 — 정본 주석의 «나머지를 물리고(후퇴)». 18~58% 가 평지라 물러난 채로 머문다.
            s.RecedeMs = J.Num(J.Require(o, "recede_ms"));
            if (s.RecedeMs <= 0) throw new FormatException("SummonFxUi hero: recede_ms 는 0보다 커야 한다");
            double[] re = J.NumArr(J.Require(o, "recede_ease"));
            if (re == null || re.Length != 4) throw new FormatException("SummonFxUi hero: recede_ease 는 cubic-bezier 넷이다");
            CssEase rease = new CssEase(re[0], re[1], re[2], re[3]);
            var rlist = J.List(J.Require(o, "srrecede"), x => J.Obj(x));
            if (rlist.Count < 2) throw new FormatException("SummonFxUi hero: srrecede 키프레임이 둘 미만이다");
            var rkeys = new RewardBurstSpec.KeyStop[rlist.Count];
            prev = -1;
            for (int i = 0; i < rlist.Count; i++)
            {
                JsonObject k = rlist[i];
                var ks = new RewardBurstSpec.KeyStop { At = J.Num(J.Require(k, "at")), Ease = rease };
                if (ks.At < prev) throw new FormatException("SummonFxUi hero: srrecede 퍼센트는 오름차순이어야 한다");
                prev = ks.At;
                ks.Num["scale"] = J.Num(J.Require(k, "scale"));
                ks.Num["sat"] = J.Num(J.Require(k, "sat"));
                ks.Num["bright"] = J.Num(J.Require(k, "bright"));
                rkeys[i] = ks;
            }
            // 물러난 조연은 **반드시 제자리로 돌아온다** — 안 그러면 결과 화면이 어두운 채로 굳는다.
            var rl = rkeys[rkeys.Length - 1];
            if (rl.Num["scale"] != 1 || rl.Num["sat"] != 1 || rl.Num["bright"] != 1)
                throw new FormatException("SummonFxUi hero: srrecede 의 마지막 키는 제자리(1/1/1)로 돌아와야 한다");
            s.Recede = new RewardBurstSpec.Track { Keys = rkeys };

            // 주역 등장 — 정본 주석: «더 길고 더 크게 넘치고, 끝에서 원래 크기로 안 돌아온다(무대에 남는다)».
            s.HeroPopMs = J.Num(J.Require(o, "heropop_ms"));
            if (s.HeroPopMs <= 0) throw new FormatException("SummonFxUi hero: heropop_ms 는 0보다 커야 한다");
            s.HeroPopDy0Rem = J.Num(J.Require(o, "heropop_dy0_rem"));
            double[] he = J.NumArr(J.Require(o, "heropop_ease"));
            if (he == null || he.Length != 4) throw new FormatException("SummonFxUi hero: heropop_ease 는 cubic-bezier 넷이다");
            CssEase hease = new CssEase(he[0], he[1], he[2], he[3]);
            var hlist = J.List(J.Require(o, "srheropop"), x => J.Obj(x));
            if (hlist.Count < 2) throw new FormatException("SummonFxUi hero: srheropop 키프레임이 둘 미만이다");
            var hkeys = new RewardBurstSpec.KeyStop[hlist.Count];
            prev = -1;
            for (int i = 0; i < hlist.Count; i++)
            {
                JsonObject k = hlist[i];
                var ks = new RewardBurstSpec.KeyStop { At = J.Num(J.Require(k, "at")), Ease = hease };
                if (ks.At < prev) throw new FormatException("SummonFxUi hero: srheropop 퍼센트는 오름차순이어야 한다");
                prev = ks.At;
                ks.Num["back_f"] = J.Num(J.Require(k, "back_f"));
                ks.Num["ty_rem"] = J.Num(J.Require(k, "ty_rem"));
                ks.Num["scale"] = J.Num(J.Require(k, "scale"));
                ks.Num["alpha"] = J.Num(J.Require(k, "alpha"));
                hkeys[i] = ks;
            }
            // ⚠ 정본이 실측으로 못 박은 자리: 정착 배율은 **1.0** 이다 — 1보다 크면 셀 폭을 넘는 이름판이 옆 셀 이름과 겹친다.
            //   주역의 «큰 몸집» 은 등급 계단이 이미 맡는다. 표에서 그 규칙을 지킨다.
            var hl = hkeys[hkeys.Length - 1];
            if (hl.Num["scale"] != 1) throw new FormatException("SummonFxUi hero: srheropop 의 정착 배율은 1.0 이어야 한다(정본 6729 주석 — 이름판이 옆 셀과 겹친다)");
            if (hl.Num["back_f"] != 0 || hl.Num["ty_rem"] != 0) throw new FormatException("SummonFxUi hero: srheropop 은 제자리에 정착해야 한다");
            s.HeroPop = new RewardBurstSpec.Track { Keys = hkeys };

            // 광창 — 정본 `srbeam`. 십자 광선이 돌며 커지다 사라진다.
            s.BeamMs = J.Num(J.Require(o, "beam_ms"));
            if (s.BeamMs <= 0) throw new FormatException("SummonFxUi hero: beam_ms 는 0보다 커야 한다");
            double[] be = J.NumArr(J.Require(o, "beam_ease"));
            if (be == null || be.Length != 4) throw new FormatException("SummonFxUi hero: beam_ease 는 cubic-bezier 넷이다");
            CssEase bease = new CssEase(be[0], be[1], be[2], be[3]);
            var blist = J.List(J.Require(o, "srbeam"), x => J.Obj(x));
            if (blist.Count < 2) throw new FormatException("SummonFxUi hero: srbeam 키프레임이 둘 미만이다");
            var bkeys = new RewardBurstSpec.KeyStop[blist.Count];
            prev = -1;
            for (int i = 0; i < blist.Count; i++)
            {
                JsonObject k = blist[i];
                var ks = new RewardBurstSpec.KeyStop { At = J.Num(J.Require(k, "at")), Ease = bease };
                if (ks.At < prev) throw new FormatException("SummonFxUi hero: srbeam 퍼센트는 오름차순이어야 한다");
                prev = ks.At;
                ks.Num["alpha"] = J.Num(J.Require(k, "alpha"));
                ks.Num["scale"] = J.Num(J.Require(k, "scale"));
                ks.Num["rot_deg"] = J.Num(J.Require(k, "rot_deg"));
                bkeys[i] = ks;
            }
            // 광창은 **떠올랐다 사라진다** — 양 끝이 0 이 아니면 결과 화면에 빛기둥이 남는다.
            if (bkeys[0].Num["alpha"] != 0 || bkeys[bkeys.Length - 1].Num["alpha"] != 0)
                throw new FormatException("SummonFxUi hero: srbeam 은 알파 0 에서 시작해 0 으로 사라져야 한다");
            s.Beam = new RewardBurstSpec.Track { Keys = bkeys };

            double[] hl2 = J.NumArr(J.Require(o, "hilite_luma"));
            if (hl2 == null || hl2.Length < 2) throw new FormatException("SummonFxUi hero: hilite_luma 는 등급 계단(둘 이상)이다");
            for (int i = 1; i < hl2.Length; i++) if (hl2[i] < hl2[i - 1]) throw new FormatException("SummonFxUi hero: hilite_luma 는 등급이 오를수록 밝아져야 한다(정본 «등급이 올라갈수록 하이라이트가 밝아지게»)");
            s.HiliteLuma = hl2;
            return s;
        }

        /// <summary>착지에서 <paramref name="ms"/> 뒤 판의 치우침(판 크기의 비율 · +y 는 **아래**)과 배율. 끝나면 제자리.</summary>
        public void At(double ms, out double txF, out double tyF, out double scale)
        {
            double p = ms <= 0 ? 0 : ms >= ShakeMs ? 100 : ms / ShakeMs * 100;
            txF = Shake.Sample(p, "tx_pct", null) / 100.0;
            tyF = Shake.Sample(p, "ty_pct", null) / 100.0;
            scale = Shake.Sample(p, "scale", null);
        }

        /// <summary>아직 흔드는 중인가.</summary>
        public bool Kicking(double ms) { return ms >= 0 && ms < ShakeMs; }

        /// <summary>착지에서 <paramref name="ms"/> 뒤 **조연** 셀의 배율·채도·밝기. 끝나면 제자리(1/1/1).</summary>
        public void RecedeAt(double ms, out double scale, out double sat, out double bright)
        {
            double p = ms <= 0 ? 0 : ms >= RecedeMs ? 100 : ms / RecedeMs * 100;
            scale = Recede.Sample(p, "scale", null);
            sat = Recede.Sample(p, "sat", null);
            bright = Recede.Sample(p, "bright", null);
        }

        /// <summary>조연이 아직 물러나 있는가.</summary>
        public bool Receding(double ms) { return ms >= 0 && ms < RecedeMs; }

        /// <summary>
        /// 착지에서 <paramref name="ms"/> 뒤 **주역** 셀의 자리·배율·불투명도.
        /// <paramref name="backF"/> 는 «슬롯 → 광원» 벡터에 곱할 비율(정본 `--dx/--dy`) · <paramref name="tyRem"/> 은 그 위에 더하는 세로 치우침(rem · +가 아래).
        /// </summary>
        public void HeroPopAt(double ms, out double backF, out double tyRem, out double scale, out double alpha)
        {
            double p = ms <= 0 ? 0 : ms >= HeroPopMs ? 100 : ms / HeroPopMs * 100;
            backF = HeroPop.Sample(p, "back_f", null);
            tyRem = HeroPop.Sample(p, "ty_rem", null);
            scale = HeroPop.Sample(p, "scale", null);
            alpha = HeroPop.Sample(p, "alpha", null);
        }

        /// <summary>주역이 아직 등장 중인가.</summary>
        public bool HeroPopping(double ms) { return ms >= 0 && ms < HeroPopMs; }

        /// <summary>착지에서 <paramref name="ms"/> 뒤 광창의 불투명도·배율·회전(도).</summary>
        public void BeamAt(double ms, out double alpha, out double scale, out double rotDeg)
        {
            double p = ms <= 0 ? 0 : ms >= BeamMs ? 100 : ms / BeamMs * 100;
            alpha = Beam.Sample(p, "alpha", null);
            scale = Beam.Sample(p, "scale", null);
            rotDeg = Beam.Sample(p, "rot_deg", null);
        }

        /// <summary>광창이 아직 도는 중인가.</summary>
        public bool Beaming(double ms) { return ms >= 0 && ms < BeamMs; }

        /// <summary>
        /// 등급 하이라이트(정본 `srHilite`) — 그 색을 **흰 쪽으로 얼마나 당길지**(0~1).
        ///
        /// 정본 주석: «등급이 올라갈수록 하이라이트가 밝아지게 **목표 휘도로 역산**한다 … 색상(hue)은 몸통 40% 스톱이 지킨다».
        /// 그래서 색을 등급마다 새로 고르지 않고 **원래 색에서 목표 휘도까지 당기는 양**만 등급으로 가른다.
        /// </summary>
        public double HiliteAmount(double r255, double g255, double b255, int tier)
        {
            double luma = r255 * 0.299 + g255 * 0.587 + b255 * 0.114;
            double want = HiliteLuma[tier < 0 ? 0 : tier >= HiliteLuma.Length ? HiliteLuma.Length - 1 : tier];
            double amt = (want - luma) / Math.Max(1, 255 - luma);
            return amt < 0 ? 0 : amt > 1 ? 1 : amt;
        }
    }
}
