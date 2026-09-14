using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T335 ⓐ — 던전 클리어 팝업의 작은 연출 셋을 표(<see cref="DungeonFxSpec"/> · `Resources/DungeonFxUi.json`)대로 도는 러너.
    /// <see cref="Phase.Pop"/>: 카드가 열릴 때 보상 칸이 순서대로 튀어 오른다(정본 `dgc-pop` .38s · 칸마다 .09s 늦게 · backwards).
    /// <see cref="Phase.Leave"/>: [보상 수령] 뒤 카드가 한 박자 뒤 가라앉고(`dgclear-sink` .45s ease-in .12s) 딤이 함께 걷힌다(`.dgclear-out` .55s) —
    /// 끝나면 팝업 뿌리를 스스로 걷는다(정본 `setTimeout → hidden`). 그동안 팝업은 논리적으로 이미 닫혀 있다(<see cref="DungeonClearPopup.IsOpen"/> false).
    /// 시계는 벽시계(<c>Time.unscaledDeltaTime</c> · 다른 연출과 같은 규약 · T135 <see cref="CardPop"/> 꼴).
    /// </summary>
    public sealed class DungeonClearFx : MonoBehaviour
    {
        public const string ResourcePath = "DungeonFxUi";
        public enum Phase { Pop, Leave }

        static DungeonFxSpec spec;
        public static DungeonFxSpec Spec
        {
            get
            {
                if (spec == null)
                {
                    TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
                    if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T335)");
                    spec = DungeonFxSpec.From(MiniJson.ParseObject(ta.text));
                }
                return spec;
            }
        }

        public Phase Mode { get; private set; }
        /// <summary>벽시계 경과(ms) — 시험이 같은 시각의 기대값을 셈한다.</summary>
        public double ElapsedMs { get; private set; }

        RectTransform[] cells;
        CanvasGroup[] cellGroups;
        RectTransform card;
        CanvasGroup cardGroup;
        Image dim;
        Color dim0;
        bool finished;

        /// <summary>카드가 선 직후 — 보상 칸들(형제 순서 = nth-child)에 팝을 건다. 카드에 붙는다.</summary>
        public static DungeonClearFx BeginPop(RectTransform card, IList<RectTransform> cells)
        {
            DungeonClearFx fx = card.gameObject.AddComponent<DungeonClearFx>();
            fx.Mode = Phase.Pop;
            fx.card = card;
            fx.cells = new RectTransform[cells.Count];
            fx.cellGroups = new CanvasGroup[cells.Count];
            for (int i = 0; i < cells.Count; i++)
            {
                fx.cells[i] = cells[i];
                CanvasGroup g = cells[i].GetComponent<CanvasGroup>();
                fx.cellGroups[i] = g != null ? g : cells[i].gameObject.AddComponent<CanvasGroup>();
            }
            fx.Apply();   // 첫 프레임이 «다 큰 칸» 으로 찍히지 않게(정본 backwards)
            return fx;
        }

        /// <summary>[보상 수령] 뒤 — 팝업 뿌리(overlay)에 붙어 카드·딤을 걷고 끝나면 뿌리를 파괴한다. 돌던 팝은 그 자리에서 끝낸다.</summary>
        public static DungeonClearFx BeginLeave(RectTransform overlay, RectTransform card, Image dim)
        {
            if (card != null)
            {
                DungeonClearFx pop = card.GetComponent<DungeonClearFx>();
                if (pop != null) pop.Finish();
            }
            DungeonClearFx fx = overlay.gameObject.AddComponent<DungeonClearFx>();
            fx.Mode = Phase.Leave;
            fx.card = card;
            if (card != null)
            {
                fx.cardGroup = card.GetComponent<CanvasGroup>();
                if (fx.cardGroup == null) fx.cardGroup = card.gameObject.AddComponent<CanvasGroup>();
                fx.cardGroup.blocksRaycasts = false;   // 가라앉는 카드는 더 못 누른다(정본 _dgclearBusy)
            }
            fx.dim = dim;
            if (dim != null) fx.dim0 = dim.color;
            fx.Apply();
            return fx;
        }

        /// <summary>돌고 있는 연출을 전부 **지금** 끝낸다(팝은 원래 모습으로 · 떠나는 뿌리는 걷는다 · 캔버스 갱신) — 촬영·픽셀 자가 찍기 전에 부른다(T128 ⓒ · <see cref="CardPop.SettleAll"/> 과 같은 자리).</summary>
        public static void SettleAll()
        {
            foreach (DungeonClearFx fx in FindObjectsByType<DungeonClearFx>(FindObjectsInactive.Include, FindObjectsSortMode.None)) fx.Finish();
            Canvas.ForceUpdateCanvases();
        }

        void Update()
        {
            if (finished) return;
            ElapsedMs += Time.unscaledDeltaTime * 1000.0;
            Apply();
        }

        void LateUpdate() { if (!finished) Apply(); }

        void Apply()
        {
            if (finished) return;
            DungeonFxSpec s = Spec;
            if (Mode == Phase.Pop)
            {
                if (s.PopDone(ElapsedMs, cells.Length)) { Finish(); return; }
                for (int i = 0; i < cells.Length; i++)
                {
                    if (cells[i] == null) continue;
                    float k = (float)s.PopScaleAt(ElapsedMs, i);
                    cells[i].localScale = new Vector3(k, k, 1f);
                    if (cellGroups[i] != null) cellGroups[i].alpha = (float)s.PopAlphaAt(ElapsedMs, i);
                }
                return;
            }
            if (card != null)
            {
                float k = (float)s.SinkScaleAt(ElapsedMs);
                card.localScale = new Vector3(k, k, 1f);
                if (cardGroup != null) cardGroup.alpha = (float)s.SinkAlphaAt(ElapsedMs);
            }
            if (dim != null)
            {
                Color c = dim0;
                c.a = dim0.a * (float)s.DimFactorAt(ElapsedMs);
                dim.color = c;
            }
            if (s.LeaveDone(ElapsedMs)) Finish();
        }

        /// <summary>팝: 칸을 원래 모습으로 돌리고 러너를 걷는다 · 떠남: 뿌리를 파괴한다.</summary>
        public void Finish()
        {
            if (finished) return;
            finished = true;
            if (Mode == Phase.Pop)
            {
                for (int i = 0; i < cells.Length; i++)
                {
                    if (cells[i] != null) cells[i].localScale = Vector3.one;
                    if (cellGroups[i] != null) { cellGroups[i].alpha = 1f; Destroy(cellGroups[i]); }
                }
                Destroy(this);
                return;
            }
            Destroy(gameObject);
        }
    }
}
