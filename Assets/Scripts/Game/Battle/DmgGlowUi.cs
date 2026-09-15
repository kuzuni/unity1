using System;
using UnityEngine;
using TMPro;
using Forge.Core.Data;
using Forge.Core.Ui;
using Forge.Game.Ui;

namespace Forge.Game.Battle
{
    /// <summary>
    /// T333 10회차 — 데미지 숫자 글로우 표(<c>Assets/Forge/Resources/DmgGlowUi.json</c>) 로더 + **공유 재질**에 언더레이를 굽는 자리.
    /// <para>
    /// <see cref="UiKit.TextShadow(TextMeshProUGUI, string)"/> 와 식(<see cref="UnderlaySdf"/>)·단위(<see cref="KeylineUi.CssPx"/>)는 같지만 대상이 다르다 —
    /// 저쪽은 글자 하나의 <c>fontMaterial</c>(인스턴스)이고, 데미지 숫자는 초당 수십 개라 인스턴스를 만들면 안 된다(T50). 그래서 여기서는
    /// <see cref="DamageNumbers"/> 가 캐시하는 **공유 재질**을 받아 그 위에 굽고, 시간에 따른 겹 갈아 끼움은 재질 교체로 낸다.
    /// </para>
    /// ⚠ 남은 몫: `UiKit.cs`(T331·T361 산 lock) 가 열리면 <c>TextShadow(Material, …)</c> 갈래를 그쪽에 두고 이 Apply 는 그것을 부르게 합치는 것이 맞다.
    /// </summary>
    public static class DmgGlowUi
    {
        public const string ResourcePath = "DmgGlowUi";
        static DmgGlowSpec spec;

        public static DmgGlowSpec Spec
        {
            get
            {
                if (spec == null)
                {
                    TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
                    if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T333 10회차)");
                    spec = DmgGlowSpec.From(MiniJson.ParseObject(ta.text));
                }
                return spec;
            }
        }

        /// <summary>테스트용 — 표를 다시 읽게 한다.</summary>
        public static void Reset() { spec = null; }

        /// <summary>그 데미지 종류에 정본 글로우가 있나(`dmg-crit`·`dmg-kill`).</summary>
        public static bool Has(string cls) { return Spec.Has(cls); }

        /// <summary>수명 비율(0~1)에 서는 겹 키 — 종류에 글로우가 없으면 null.</summary>
        public static string KeyAt(string cls, double u) { return Spec.KeyAt(cls, u); }

        /// <summary>겹 색(#RRGGBB + alpha).</summary>
        public static Color C(string key)
        {
            DmgGlowSpec.Glow g = Spec.Get(key);
            Color c;
            if (g.Color == null || !ColorUtility.TryParseHtmlString(g.Color, out c))
                throw new FormatException(ResourcePath + ".json «" + key + "» 의 색 «" + g.Color + "» 을 못 읽는다");
            c.a = (float)g.Alpha;
            return c;
        }

        /// <summary>글로우를 끈다 — 정본에 겹이 없는 종류(일반타·영웅 피해 …). 복제 원본이 이미 언더레이를 쥐고 있던 자리를 지운다.</summary>
        public static void Off(Material m)
        {
            if (m == null) return;
            if (m.IsKeywordEnabled("UNDERLAY_ON")) m.DisableKeyword("UNDERLAY_ON");
        }

        /// <summary>
        /// 공유 재질에 글로우 한 겹을 굽는다(표 px → 캔버스 px → 언더레이 값). 글자 크기가 단위를 정하므로 호출자가 그 크기의 재질마다 따로 굽는다.
        /// 재질에 언더레이가 없으면 아무것도 안 하고 빈 값을 돌린다.
        /// </summary>
        public static UnderlaySdf Apply(Material m, TMP_FontAsset font, float fontSize, string key)
        {
            if (m == null || !m.HasProperty("_UnderlayColor"))
            {
                Debug.LogWarning("[DmgGlowUi] 재질에 언더레이가 없다 — 글로우 «" + key + "» 를 건너뛴다");
                return new UnderlaySdf();
            }
            float g = m.HasProperty("_GradientScale") ? m.GetFloat("_GradientScale") : 0f;
            float rc = m.HasProperty("_ScaleRatioC") ? m.GetFloat("_ScaleRatioC") : 0f;
            float ps = font != null ? (float)font.faceInfo.pointSize : 0f;
            if (g <= 0f) g = 10f;                 // UiKit.TextShadow 와 같은 TMP 기본값 갈래
            if (ps <= 0f) ps = 90f;
            if (rc <= 0f) rc = 1f;
            DmgGlowSpec.Glow gl = Spec.Get(key);
            float css = KeylineUi.CssPx;
            UnderlaySdf u = UnderlaySdf.FromPx(gl.DxPx * css, gl.DyPx * css, gl.BlurPx * css, fontSize, g, rc, ps);
            if (u.Clipped)
                Debug.LogWarning("[DmgGlowUi] 글로우 «" + key + "» 가 이 글자(" + fontSize + "px)의 SDF 여백(단위 " + u.UnitPx.ToString("0.0") + "px)을 넘어 잘렸다");
            m.EnableKeyword("UNDERLAY_ON");
            m.SetColor("_UnderlayColor", C(key));
            m.SetFloat("_UnderlayOffsetX", (float)u.OffsetX01);
            m.SetFloat("_UnderlayOffsetY", (float)u.OffsetY01);
            m.SetFloat("_UnderlayDilate", 0f);
            m.SetFloat("_UnderlaySoftness", (float)u.Softness01);
            return u;
        }
    }
}
