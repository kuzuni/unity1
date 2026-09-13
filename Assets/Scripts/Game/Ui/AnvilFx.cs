using UnityEngine;
using UnityEngine.UI;
using Forge.Core.CraftFx;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T87 2회차 — 두들기는 동안 모루와 시트를 정본 키프레임(<see cref="AnvilFxSpec"/>)대로 흔든다.
    /// 정본은 CSS 애니메이션(`.anvil-btn.striking .anvil-svg` → `anvilbump` · `#equip-sheet.shaking` → `sheetshake`)이고
    /// 클럭은 `ui.js` 의 `ANVIL_FX_MS`(1500ms)를 `--afdur` 로 내려보낸 것이다 — 여기서는 그 표를 프레임마다 읽어 바른다.
    ///
    /// 규칙 둘:
    ///  · **자기가 만든 것만 되돌린다** — 시작할 때 모루·시트의 원래 자리(`anchoredPosition`·`localScale`)를 적어 두고 끝에 그대로 돌려놓는다.
    ///  · 한 번에 하나 — 다시 두들기면 그 자리에서 0% 로 되감는다(정본도 클래스를 지웠다 다시 단다).
    /// </summary>
    public sealed class AnvilFx : MonoBehaviour
    {
        private RectTransform anvil, sheet, billet, hammer;
        private CanvasGroup hammerGroup;
        private Graphic hot, cool, glow;
        private float unit;
        private Vector2 hammerHome;
        private Image[] rings, shadows, cores;
        private Vector2 anvilHome, sheetHome;
        private Vector3 anvilScaleHome, billetScaleHome;
        private bool running;
        private double ms;

        private readonly double[] bump = new double[3];
        private readonly double[] shake = new double[2];
        private readonly double[] billetV = new double[2];
        private readonly double[] swing = new double[4];
        private readonly double[] exit = new double[4];

        /// <summary>지금 돌고 있는가(테스트·중복 시작 방지).</summary>
        public bool Running { get { return running; } }

        /// <summary>연출 시작부터 흐른 시간(ms) — PlayMode 가 표와 대조할 때 쓴다.</summary>
        public double ElapsedMs { get { return ms; } }

        /// <summary>UI 층에 붙은 러너를 찾거나 만든다.</summary>
        public static AnvilFx Ensure(RectTransform host)
        {
            if (host == null) return null;
            AnvilFx fx = host.GetComponent<AnvilFx>();
            if (fx == null) fx = host.gameObject.AddComponent<AnvilFx>();
            return fx;
        }

        /// <summary>
        /// 두들기기 시작 — `anvilRt` 는 **모루 그림 칸**(정본 `.anvil-svg` · 버튼이 아니다: 버튼에 걸면 타격 오버레이가 반동을 같이 타 상대변위가 0 이 된다 · 정본 주석),
        /// `sheetRt` 는 그 모루가 든 시트(둘 다 없어도 죽지 않는다).
        /// </summary>
        public void Play(RectTransform anvilRt, RectTransform sheetRt, RectTransform billetRt, Graphic hotLayer, Graphic coolLayer, Graphic glowLayer, RectTransform hammerRt, CanvasGroup hammerCg, Image[] ringLayers, Image[] shadowLayers, Image[] coreLayers, float vbUnit)
        {
            Stop();
            anvil = anvilRt;
            sheet = sheetRt;
            Take(billetRt, hotLayer, coolLayer, glowLayer, hammerRt, hammerCg, ringLayers, shadowLayers, coreLayers, vbUnit);
            if (anvil != null)
            {
                anvilHome = anvil.anchoredPosition;
                anvilScaleHome = anvil.localScale;
            }
            if (sheet != null) sheetHome = sheet.anchoredPosition;
            ms = 0;
            running = true;
            Apply();
        }

        /// <summary>쇳덩이 묶음과 불투명도 겹을 받아 «쉬는 자세» 를 적어 둔다(되돌릴 때 그 자리로).</summary>
        private void Take(RectTransform billetRt, Graphic hotLayer, Graphic coolLayer, Graphic glowLayer, RectTransform hammerRt, CanvasGroup hammerCg, Image[] ringLayers, Image[] shadowLayers, Image[] coreLayers, float vbUnit)
        {
            billet = billetRt;
            hot = hotLayer;
            cool = coolLayer;
            glow = glowLayer;
            hammer = hammerRt;
            hammerGroup = hammerCg;
            rings = ringLayers;
            shadows = shadowLayers;
            cores = coreLayers;
            unit = vbUnit;
            if (billet != null) billetScaleHome = billet.localScale;
            if (hammer != null) hammerHome = hammer.anchoredPosition;
        }

        /// <summary>
        /// 두들기는 도중 시트가 다시 그려졌을 때(세이브 → `Rerender`) **흐른 시간을 지키며** 새 칸에 다시 문다.
        /// 정본은 DOM 을 갈아도 CSS 애니메이션이 그 자리에서 이어지지 않지만, 클론은 시트를 통째로 다시 그리므로
        /// 다시 물지 않으면 남은 구간이 통째로 사라진다(런 157 실측: 모루가 파괴돼 연출이 없던 일이 됐다).
        /// </summary>
        public void Rebind(RectTransform anvilRt, RectTransform sheetRt, RectTransform billetRt, Graphic hotLayer, Graphic coolLayer, Graphic glowLayer, RectTransform hammerRt, CanvasGroup hammerCg, Image[] ringLayers, Image[] shadowLayers, Image[] coreLayers, float vbUnit)
        {
            if (!running) return;
            Restore();
            anvil = anvilRt;
            sheet = sheetRt;
            Take(billetRt, hotLayer, coolLayer, glowLayer, hammerRt, hammerCg, ringLayers, shadowLayers, coreLayers, vbUnit);
            if (anvil != null)
            {
                anvilHome = anvil.anchoredPosition;
                anvilScaleHome = anvil.localScale;
            }
            if (sheet != null) sheetHome = sheet.anchoredPosition;
            Apply();
        }

        /// <summary>연출을 걷고 제자리로 — 취소(`CancelAnvilStrike`)와 정상 종료가 같은 길을 쓴다.</summary>
        public void Stop()
        {
            if (running) Restore();
            running = false;
            anvil = null;
            sheet = null;
            billet = null;
            hot = null;
            cool = null;
            glow = null;
            hammer = null;
            hammerGroup = null;
            rings = null;
            shadows = null;
            cores = null;
        }

        /// <summary>자기가 만든 것만 되돌린다 — 모루·시트 자리와 쇳덩이 자세·겹 불투명도(정지 상태 = 트랙 0%).</summary>
        private void Restore()
        {
            if (anvil != null)
            {
                anvil.anchoredPosition = anvilHome;
                anvil.localScale = anvilScaleHome;
            }
            if (sheet != null) sheet.anchoredPosition = sheetHome;
            if (billet != null) billet.localScale = billetScaleHome;
            ForgeSheet.SetOpacity(hot, (float)AnvilFxSpec.BilletHot.Sample1(0));
            ForgeSheet.SetOpacity(cool, (float)AnvilFxSpec.BilletCool.Sample1(0));
            ForgeSheet.SetOpacity(glow, (float)AnvilFxSpec.BilletGlow.Sample1(0));
        }

        /// <summary>정본 클럭은 «게임 시간» 이 아니라 벽시계다(CSS 애니메이션) — `unscaledDeltaTime` 으로 돈다.</summary>
        private void Update()
        {
            if (!running) return;
            ms += Time.unscaledDeltaTime * 1000.0;
            if (ms >= AnvilFxSpec.DurationMs) { Stop(); return; }
            Apply();
        }

        /// <summary>지금 시각의 표 값을 바른다 — 테스트가 시간을 직접 주고 부를 수 있다.</summary>
        public void SampleTo(double elapsedMs)
        {
            ms = elapsedMs;
            if (running) Apply();
        }

        /// <summary>
        /// 정본 키프레임의 **절대 CSS px** → 기준 캔버스 px(카탈로그 `anvil_fx_px`). rem 값과 달리 이 px 들은 원작에서 앱 크기를 안 따라가므로
        /// 촬영 배율(앱 폭 499px → 1080px)로 환산해야 «같은 깊이로 보인다» — 그대로 쓰면 정본의 절반이고, 정본 주석이 이미
        /// «진폭이 지각 한계 미만이었다» 고 적어 둔 자리다.
        /// </summary>
        public static float PxScale { get { return UiKit.L("anvil_fx_px"); } }

        private void Apply()
        {
            double pct = ms / AnvilFxSpec.DurationMs * 100.0;
            float px = PxScale;
            if (anvil != null)
            {
                AnvilFxSpec.Bump.Sample(pct, bump);
                // CSS 의 translateY 는 아래가 +, 유니티 UI 는 위가 + 다. 스케일 축은 그림 칸의 피벗(= 정본 transform-origin 50% 92%)이다.
                anvil.anchoredPosition = new Vector2(anvilHome.x, anvilHome.y - (float)bump[0] * px);
                anvil.localScale = new Vector3(anvilScaleHome.x * (float)bump[1], anvilScaleHome.y * (float)bump[2], anvilScaleHome.z);
            }
            if (sheet != null)
            {
                AnvilFxSpec.SheetShake.Sample(pct, shake);
                sheet.anchoredPosition = new Vector2(sheetHome.x + (float)shake[0] * px, sheetHome.y - (float)shake[1] * px);
            }
            if (billet != null)
            {
                // 하중은 강철 모루가 아니라 **달군 쇠**가 먹는다(정본 주석) — 축은 쇳덩이 밑면(피벗)이다.
                AnvilFxSpec.Billet.Sample(pct, billetV);
                billet.localScale = new Vector3(billetScaleHome.x * (float)billetV[0], billetScaleHome.y * (float)billetV[1], billetScaleHome.z);
            }
            ForgeSheet.SetOpacity(hot, (float)AnvilFxSpec.BilletHot.Sample1(pct));
            ForgeSheet.SetOpacity(cool, (float)AnvilFxSpec.BilletCool.Sample1(pct));
            ForgeSheet.SetOpacity(glow, (float)AnvilFxSpec.BilletGlow.Sample1(pct));
            ApplyHammer();
            ApplyRings();
            ApplyBurst(shadows, AutoForgeFxSpec.Shadow, AutoForgeFxSpec.ShadowEase, AutoForgeFxSpec.ShadowDurMs, AutoForgeFxSpec.ShadowLeadMs, AutoForgeFxSpec.ShadowScale);
            ApplyBurst(cores, AutoForgeFxSpec.Core, AutoForgeFxSpec.CoreEase, AutoForgeFxSpec.CoreDurMs, AutoForgeFxSpec.StarLeadMs, AutoForgeFxSpec.CoreScale);
        }

        /// <summary>타격 순간의 겹(그림자·코어 …) 셋 — 제 창에서만 배율·불투명도를 바르고 밖에서는 투명하게 둔다.</summary>
        private void ApplyBurst(Image[] layers, CssTrack track, CssEase ease, double durMs, double leadMs, double[] perStrike)
        {
            if (layers == null || track == null) return;
            double[] v = new double[2];
            for (int i = 0; i < layers.Length; i++)
            {
                Image img = layers[i];
                if (img == null) continue;
                Color c = img.color;
                if (AutoForgeFxSpec.SampleBurst(track, ease, durMs, leadMs, perStrike, i, ms, v))
                {
                    img.rectTransform.localScale = new Vector3((float)v[0], (float)v[0], 1f);
                    c.a = Mathf.Clamp01((float)v[1]);
                }
                else
                {
                    img.rectTransform.localScale = Vector3.one;
                    c.a = 0f;
                }
                img.color = c;
            }
        }

        /// <summary>
        /// 타격 링 — 타격마다 제 창(접촉 8ms 앞 + 200ms)에서만 퍼지며 꺼진다(`afring` · cubic-bezier). 창 밖에서는 투명이다.
        /// ⚠ 정본은 `non-scaling-stroke` 라 퍼져도 테두리 굵기가 그대로지만 여기서는 스프라이트를 키우므로 같이 굵어진다(결정 231 · 그 구간은 이미 흐려지는 중이다).
        /// </summary>
        private void ApplyRings()
        {
            if (rings == null) return;
            double[] v = new double[2];
            for (int i = 0; i < rings.Length; i++)
            {
                Image img = rings[i];
                if (img == null) continue;
                Color c = img.color;
                if (AutoForgeFxSpec.SampleRing(i, ms, v))
                {
                    img.rectTransform.localScale = new Vector3((float)v[0], (float)v[0], 1f);
                    c.a = Mathf.Clamp01((float)v[1]);
                }
                else
                {
                    img.rectTransform.localScale = Vector3.one;
                    c.a = 0f;
                }
                img.color = c;
            }
        }

        /// <summary>
        /// 망치 — `afswing`(0~1170ms · linear)과 그 뒤를 이어받는 `afexit`(180ms · cubic-bezier). translate 는 **viewBox 단위**라 <see cref="unit"/> 를 곱한다.
        /// CSS 의 +y 는 아래, +각은 시계 방향이라 유니티에서는 둘 다 부호가 뒤집힌다. 스케일(스미어)은 회전보다 먼저 걸리는데 유니티 Transform 도 같은 순서다.
        /// </summary>
        private void ApplyHammer()
        {
            if (hammer == null) return;
            double x, y, rot, scaleY, alpha;
            if (AutoForgeFxSpec.SampleExit(ms, exit))
            {
                x = exit[0]; y = exit[1]; rot = exit[2]; scaleY = 1.0; alpha = exit[3];
            }
            else
            {
                AutoForgeFxSpec.SampleSwing(ms, swing);
                x = swing[0]; y = swing[1]; rot = swing[2]; scaleY = swing[3];
                alpha = AutoForgeFxSpec.SwingOpacity.Sample1(ms / AnvilFxSpec.DurationMs * 100.0);
            }
            hammer.anchoredPosition = new Vector2(hammerHome.x + (float)x * unit, hammerHome.y - (float)y * unit);
            hammer.localRotation = Quaternion.Euler(0f, 0f, -(float)rot);
            hammer.localScale = new Vector3(1f, (float)scaleY, 1f);
            if (hammerGroup != null) hammerGroup.alpha = Mathf.Clamp01((float)alpha);
        }

        private void OnDisable()
        {
            Stop();
        }
    }
}
