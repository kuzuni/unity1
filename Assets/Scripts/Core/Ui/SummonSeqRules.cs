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
        public double ShakeMs, RecedeMs, HeroPopMs;
        /// <summary>주역 등장의 0% 치우침 기본값(rem · 슬롯→광원 벡터가 없을 때 아래로).</summary>
        public double HeroPopDy0Rem;
        public RewardBurstSpec.Track Shake, Recede, HeroPop;

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
    }
}
