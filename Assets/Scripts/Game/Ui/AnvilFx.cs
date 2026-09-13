using UnityEngine;
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
        private RectTransform anvil, sheet;
        private Vector2 anvilHome, sheetHome;
        private Vector3 anvilScaleHome;
        private bool running;
        private double ms;

        private readonly double[] bump = new double[3];
        private readonly double[] shake = new double[2];

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

        /// <summary>두들기기 시작 — `anvil` 은 모루 그림 칸, `sheet` 는 그 모루가 든 시트(둘 다 없어도 죽지 않는다).</summary>
        public void Play(RectTransform anvilRt, RectTransform sheetRt)
        {
            Stop();
            anvil = anvilRt;
            sheet = sheetRt;
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

        /// <summary>연출을 걷고 제자리로 — 취소(`CancelAnvilStrike`)와 정상 종료가 같은 길을 쓴다.</summary>
        public void Stop()
        {
            if (running)
            {
                if (anvil != null)
                {
                    anvil.anchoredPosition = anvilHome;
                    anvil.localScale = anvilScaleHome;
                }
                if (sheet != null) sheet.anchoredPosition = sheetHome;
            }
            running = false;
            anvil = null;
            sheet = null;
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

        private void Apply()
        {
            double pct = ms / AnvilFxSpec.DurationMs * 100.0;
            if (anvil != null)
            {
                AnvilFxSpec.Bump.Sample(pct, bump);
                // CSS 의 translateY 는 아래가 +, 유니티 UI 는 위가 + 다.
                anvil.anchoredPosition = new Vector2(anvilHome.x, anvilHome.y - (float)bump[0]);
                anvil.localScale = new Vector3(anvilScaleHome.x * (float)bump[1], anvilScaleHome.y * (float)bump[2], anvilScaleHome.z);
            }
            if (sheet != null)
            {
                AnvilFxSpec.SheetShake.Sample(pct, shake);
                sheet.anchoredPosition = new Vector2(sheetHome.x + (float)shake[0], sheetHome.y - (float)shake[1]);
            }
        }

        private void OnDisable()
        {
            Stop();
        }
    }
}
