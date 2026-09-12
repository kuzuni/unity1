using System;
using System.Collections.Generic;

namespace Forge.Core.BattleFx
{
    /// <summary>정본 `Scene3D.ENEMY_GAIT` 한 줄 — 종별 보행 프로파일(없는 칸은 0 = 원작 undefined).</summary>
    public sealed class GaitProfile
    {
        public double Rate, Bob, BobPow, Roll, Hip, Arm, Lean, Sq, LegRate, TailRate, Pitch, Hover, WingRate, CapTilt, CapAmp, JellyAmp;

        public GaitProfile Clone()
        {
            return new GaitProfile
            {
                Rate = Rate, Bob = Bob, BobPow = BobPow, Roll = Roll, Hip = Hip, Arm = Arm, Lean = Lean, Sq = Sq, LegRate = LegRate, TailRate = TailRate,
                Pitch = Pitch, Hover = Hover, WingRate = WingRate, CapTilt = CapTilt, CapAmp = CapAmp, JellyAmp = JellyAmp,
            };
        }
    }

    /// <summary>
    /// 원작 `web/js/scene3d.js` 의 적 보행·대기 규칙(T8) — `ENEMY_GAIT`(11274~) · `ENEMY_VS`·`BOSS_SCALE`(11286) · `KIND_COLOR`(11258) · `gaitOf`(11294) ·
    /// `hopCurve`(11310) · `gaitSquash`(11331) · `monsterMesh` 의 종 선택(11617) · `update` 루프의 보행/대기 식(18343~18470). 전부 순수 계산이라 Core 에 두고
    /// `tools/battlefx_vectors.js` 가 정본을 실제로 실행한 벡터(`Tests/EditMode/Vectors/t8-battlefx.json`)와 대조한다. 값을 고치려면 정본을 고친다(§1).
    /// </summary>
    public static class EnemyGait
    {
        /// <summary>`monsterMesh` 의 kinds 순서.</summary>
        public static readonly string[] Kinds = { "slime", "golem", "goblin", "bat", "mushroom", "wolf", "imp" };

        /// <summary>`kinds[(e.id + S.chapter * 2) % kinds.length]`.</summary>
        public static string KindOf(int id, int chapter)
        {
            int n = Kinds.Length;
            int k = (int)(((long)id + (long)chapter * 2) % n);
            if (k < 0) k += n;
            return Kinds[k];
        }

        /// <summary>`KIND_COLOR` — 보스 파생색의 씨앗(몸 색은 표가 쥔다).</summary>
        public static readonly Dictionary<string, int> KindColor = new Dictionary<string, int>
        {
            { "slime", 0x74d848 }, { "golem", 0xa9a495 }, { "goblin", 0x4f9440 }, { "bat", 0x6f5a49 }, { "mushroom", 0xc4392b }, { "wolf", 0x9a938a }, { "imp", 0xb0433a },
        };

        /// <summary>적 공용 복셀 한 변(월드) · 보스 배율 · 3/4 자세 요(yaw) · 영웅 요 · 위치 추종 계수 · 걷기 판정 여유 · 행군 속도(월드/초 · `update` 18556).</summary>
        public const double EnemyVs = 0.05, BossScale = 1.9, EnemyYaw = -0.55, HeroYaw = 0.55, FollowLerp = 12, WalkEps = 0.05, MarchSpeed = 1.7;
        /// <summary>`spawnEnemy` — 반폭 하한 · 지상 적 블롭 배율(footprint × 1.8 · 0.42~0.9) · 비행체 블롭 0.85 · 실그림자 끄는 발자국 반경.</summary>
        public const double HalfWMin = 0.24, BlobK = 1.8, BlobMin = 0.42, BlobMax = 0.9, BlobFly = 0.85, NoSunShadowFootprint = 0.5;
        /// <summary>`footprintRadius` 의 하한·상한.</summary>
        public const double FootprintMin = 0.25, FootprintMax = 4.5;
        /// <summary>HP 바 논리 높이 여유(`topY = maxY·cell + 0.14`) · 바 간격(일반 0.25 · 보스 0.35 · 배율로 나눈다).</summary>
        public const double TopYPad = 0.14, BarGap = 0.25, BarGapBoss = 0.35;
        /// <summary>대기(`update` else 갈래): 홉 박자 3.2 · bobPow 1.35 · 진폭 0.030 · 스쿼시 배율 0.55 · 까딱임 `sin(clk·2.3 + id·1.7)·0.022` · 추종 6 · 감쇠 0.85/0.9.</summary>
        public const double IdleRate = 3.2, IdleBobPow = 1.35, IdleBob = 0.030, IdleSqK = 0.55, SwayRate = 2.3, SwayIdK = 1.7, SwayAmp = 0.022, SwayFollow = 6, IdleDamp = 0.85, IdleDampSlow = 0.9;
        /// <summary>대기 팔꿈치 정착각 −0.22(추종 0.12) · 젤리 대기 진폭 0.06 · 갓 플랩 대기 0.012 · 무릎 복귀 0.15.</summary>
        public const double IdleElbow = -0.22, IdleElbowFollow = 0.12, IdleJellyAmp = 0.06, IdleCapFlap = 0.012, IdleKneeFollow = 0.15;

        static readonly Dictionary<string, GaitProfile> table = new Dictionary<string, GaitProfile>
        {
            { "golem", new GaitProfile { Rate = 4.2, Bob = 0.05, BobPow = 1.9, Roll = 0.055, Hip = 0.34, Arm = 0.40, Lean = 0.02, Sq = 0.055 } },
            { "goblin", new GaitProfile { Rate = 10.5, Bob = 0.05, BobPow = 1.0, Roll = 0.035, Hip = 0.88, Arm = 0.72, Lean = 0.10, Sq = 0.085 } },
            { "imp", new GaitProfile { Rate = 12.0, Bob = 0.062, BobPow = 0.7, Roll = 0.03, Hip = 0.78, Arm = 0.66, Lean = 0.13, Sq = 0.105 } },
            { "wolf", new GaitProfile { Rate = 11, Bob = 0.05, BobPow = 1.0, LegRate = 13, TailRate = 9, Pitch = 0.03, Sq = 0.070 } },
            { "bat", new GaitProfile { Rate = 5, Bob = 0.1, BobPow = 1.0, Hover = 0.12, WingRate = 16, Sq = 0.045 } },
            { "mushroom", new GaitProfile { Rate = 7, Bob = 0.17, BobPow = 1.0, CapTilt = 0.13, CapAmp = 0.035, Sq = 0.115 } },
            { "slime", new GaitProfile { Rate = 6, Bob = 0.12, BobPow = 1.0, JellyAmp = 0.16, Sq = 0 } },
            { "_default", new GaitProfile { Rate = 8, Bob = 0.055, BobPow = 1.0, Roll = 0.05, Hip = 0.8, Arm = 0.65, Lean = 0, Sq = 0.08 } },
        };

        /// <summary>표의 종 프로파일(사본 · 없는 종은 `_default`).</summary>
        public static GaitProfile Table(string kind)
        {
            GaitProfile g;
            return (kind != null && table.TryGetValue(kind, out g) ? g : table["_default"]).Clone();
        }

        /// <summary>`gaitOf(kind, isBoss)` — 보스는 박자 1/√1.9 · bob ×1.15 · bobPow +0.35 · roll ×1.2 · hip/arm ×0.9 · sq ×0.82.</summary>
        public static GaitProfile GaitOf(string kind, bool isBoss)
        {
            GaitProfile g = Table(kind);
            if (!isBoss) return g;
            double k = 1 / Math.Sqrt(BossScale);
            GaitProfile o = g.Clone();
            if (g.Rate != 0) o.Rate = g.Rate * k;
            if (g.LegRate != 0) o.LegRate = g.LegRate * k;
            if (g.TailRate != 0) o.TailRate = g.TailRate * k;
            if (g.WingRate != 0) o.WingRate = g.WingRate * k;
            o.Bob = g.Bob * 1.15;
            o.BobPow = g.BobPow + 0.35;
            if (g.Roll != 0) o.Roll = g.Roll * 1.2;
            if (g.Hip != 0) o.Hip = g.Hip * 0.9;
            if (g.Arm != 0) o.Arm = g.Arm * 0.9;
            if (g.Sq != 0) o.Sq = g.Sq * 0.82;
            return o;
        }

        /// <summary>`hopCurve(u, bobPow)` — 1930s 카툰 홉(접지 → 이륙 스냅 → 정점 체공 → 가속 낙하) · u 는 |sin| 한 주기를 0~1 로 편 값.</summary>
        public static double HopCurve(double u, double bobPow)
        {
            u = u - Math.Floor(u);
            double gc = Math.Min(0.34, Math.Max(0.05, 0.11 * (bobPow != 0 ? bobPow : 1)));
            if (u < gc) return 0;
            double v = (u - gc) / (1 - gc);
            if (v < 0.34) { double t = v / 0.34; return 1 - Math.Pow(1 - t, 4); }
            if (v < 0.62) return 1 - 0.06 * (1 - Math.Cos((v - 0.34) / 0.28 * Math.PI * 2)) * 0.5;
            double f = (v - 0.62) / 0.38;
            return Math.Max(0, 1 - f * f);
        }

        /// <summary>`gaitSquash(u, bobPow, amp)` — 같은 주기의 스쿼시&스트레치(+ 늘어남 · − 눌림).</summary>
        public static double GaitSquash(double u, double bobPow, double amp)
        {
            if (amp == 0) return 0;
            u = u - Math.Floor(u);
            double gc = Math.Min(0.34, Math.Max(0.05, 0.11 * (bobPow != 0 ? bobPow : 1)));
            if (u < gc)
            {
                double g = u / gc;
                return -amp * (1 - 0.30 * g);
            }
            double v = (u - gc) / (1 - gc);
            if (v < 0.22) return amp * 0.85 * (1 - v / 0.22);
            if (v < 0.60) return 0;
            double f = (v - 0.60) / 0.40;
            return amp * 0.75 * f * f;
        }

        /// <summary>홉 위상 `(clk·rate + id) / π`.</summary>
        public static double HopU(double clk, double rate, int id) { return (clk * rate + id) / Math.PI; }

        // ── 보행 관절 식(`update` 이족/사족/박쥐 갈래 그대로) ──
        /// <summary>이족 고관절 `sin(lp)·hip + sin(2lp)·0.12`.</summary>
        public static double BipedHip(double lp, double hip) { return Math.Sin(lp) * hip + Math.Sin(2 * lp) * 0.12; }
        /// <summary>이족 무릎 `−0.15 − max(0, cos(lp − 0.35))^1.4 · 1.15`.</summary>
        public static double BipedKnee(double lp) { return -0.15 - Math.Pow(Math.Max(0, Math.Cos(lp - 0.35)), 1.4) * 1.15; }
        /// <summary>이족 어깨 `sin(ap)·arm` · 팔꿈치 `−0.7 − max(0, sin(ap))·0.6`.</summary>
        public static double BipedShoulder(double ap, double arm) { return Math.Sin(ap) * arm; }
        public static double BipedElbow(double ap) { return -0.7 - Math.Max(0, Math.Sin(ap)) * 0.6; }
        /// <summary>관절 없는 리그 폴백 팔 `sin(ph)·0.55`.</summary>
        public static double FallbackArm(double ph) { return Math.Sin(ph) * 0.55; }
        /// <summary>늑대 로터리 갤럽 위상 오프셋(FL·FR·BL·BR).</summary>
        public static readonly double[] WolfLegPhase = { 0, 1.1, 3.25, 4.35 };
        /// <summary>늑대 다리 `sin(lp)·0.85` · 무릎 `rx0 + (앞 ? −1 : 1)·max(0, sin(lp + 1.3))·0.85` · 꼬리 `sin(clk·tailRate + id)·0.25`.</summary>
        public static double WolfLeg(double lp) { return Math.Sin(lp) * 0.85; }
        public static double WolfKnee(double rx0, bool front, double lp) { return rx0 + (front ? -1 : 1) * Math.Max(0, Math.Sin(lp + 1.3)) * 0.85; }
        public static double WolfTail(double clk, double tailRate, int id) { return Math.Sin(clk * tailRate + id) * 0.25; }
        /// <summary>박쥐 고도 `hover + sin(clk·rate + id)·bob` · 날개 `s·(0.3 + sin(clk·wingRate + id)·0.55)` · 스쿼시 `−sin(clk·wingRate + id)·sq·0.6`.</summary>
        public static double FlyY(GaitProfile g, double clk, int id) { return g.Hover + Math.Sin(clk * g.Rate + id) * g.Bob; }
        public static double FlyWing(double s, double clk, double wingRate, int id) { return s * (0.3 + Math.Sin(clk * wingRate + id) * 0.55); }
        public static double FlySq(double clk, double wingRate, double sq, int id) { return -Math.Sin(clk * wingRate + id) * sq * 0.6; }
        /// <summary>버섯 갓 기울기 `sin(clk·rate + id)·capTilt`.</summary>
        public static double CapTilt(double clk, double rate, double capTilt, int id) { return Math.Sin(clk * rate + id) * capTilt; }
        /// <summary>젤리 웨이브(`driveJelly`): 높이 t(0~1) 의 (세로배율, 가로배율) — `sq = 1 − |sin(ph − t·lag)|` · lag 1.15 · ph = clk·6 + id.</summary>
        public const double JellyLag = 1.15, JellyRate = 6, CapFlapLag = 1.6, CapFlapRate = 7, CapTopT = 0.62;
        public static void JellyAt(double ph, double t, double amp, out double sy, out double sr)
        {
            double sq = 1 - Math.Abs(Math.Sin(ph - t * JellyLag));
            sy = 1 - sq * amp;
            sr = 1 + sq * amp * 0.55;
        }
        /// <summary>갓 테두리 플랩(`driveCapFlap`): `sin(ph − t·lag)·amp·t²` · ph = clk·7 + id.</summary>
        public static double CapFlapAt(double ph, double t, double amp) { return Math.Sin(ph - t * CapFlapLag) * amp * t * t; }

        /// <summary>몸 스케일 합성(`update`): 히트 스쿼시 a(가로 압축) × 보행/공격 스쿼시 s(세로).</summary>
        public static void BodyScale(double b, double a, double s, out double sx, out double sy, out double sz)
        {
            sx = b * (1 - a) * (1 - s * 0.5);
            sy = b * (1 + a * 0.92) * (1 + s);
            sz = b * (1 + a * 0.14) * (1 - s * 0.5);
        }

        /// <summary>지상 블롭 축소 `max(0.55, 1 − y·0.35)` · 비행 블롭 `1 + y·1.7` / 불투명도 `clamp(1 − y·2.2, 0.30, 1)`.</summary>
        public static double GroundBlobScale(double y) { return Math.Max(0.55, 1 - y * 0.35); }
        public static double FlyBlobScale(double y) { return 1 + y * 1.7; }
        public static double FlyBlobOpacity(double y) { return Math.Max(0.30, Math.Min(1, 1 - y * 2.2)); }
        /// <summary>블롭 불투명도(`ensureBlobRes`): 지상 적 0.26 · 비행 0.45 · 영웅 0.22.</summary>
        public const double BlobOpacityFoe = 0.26, BlobOpacityFly = 0.45, BlobOpacity = 0.22;
    }
}
