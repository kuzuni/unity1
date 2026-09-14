using System;
using UnityEngine;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T135 ⓑ — 모달을 **처음** 열 때 카드가 튀는 연출(정본 `style.css` 1758 `.modal.opening .modal-card { animation: cardpop .25s ease-out }`).
    /// <see cref="PopupLayer.Show"/> 가 새 팝업의 뿌리(딤)에 이것을 하나 붙인다. 카드(<c>"card"</c> · 정본 `.modal-card`)는 호출부가 뒤에 세우므로
    /// 매 프레임 이름으로 찾아 scale·α 를 표(<see cref="CardPopSpec"/>)대로 건다 — 카드가 재렌더로 다시 서도(같은 이름) 같은 시계를 잇는다.
    /// 재호출(<see cref="PopupLayer.Show"/> 가 이미 열린 팝업을 돌려줄 때)은 새로 안 붙는다 = 정본 «재호출은 opening 을 다시 안 붙인다»(ui.js 1156).
    /// 시계는 벽시계(<c>Time.unscaledDeltaTime</c> · 다른 연출과 같은 규약). 끝나면 카드를 원래 모습으로 돌려놓고 스스로 사라진다.
    /// 시트(<c>"card"</c> 가 없는 팝업)는 정본에서도 `.modal-card` 가 아니라 안 튄다.
    /// </summary>
    public sealed class CardPop : MonoBehaviour
    {
        public const string ResourcePath = "CardPopUi";
        public const string CardName = "card";

        static CardPopSpec spec;
        public static CardPopSpec Spec
        {
            get
            {
                if (spec == null)
                {
                    TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
                    if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T135 카드 팝)");
                    spec = CardPopSpec.From(MiniJson.ParseObject(ta.text));
                }
                return spec;
            }
        }

        /// <summary>새 팝업 뿌리에 팝을 건다(이미 걸려 있으면 그대로 · 시험이 붙는 순간을 잡을 수 있게 돌려준다).</summary>
        public static CardPop Begin(RectTransform popupRoot)
        {
            CardPop c = popupRoot.GetComponent<CardPop>();
            return c != null ? c : popupRoot.gameObject.AddComponent<CardPop>();
        }

        /// <summary>
        /// 돌고 있는 팝을 전부 **지금** 끝낸다(카드를 원래 모습으로 · 러너 제거 · 캔버스 갱신). 촬영·픽셀 자가 찍기 전에 부른다 —
        /// 정지 촬영이 팝 도중(반투명·축소)을 찍으면 그림이 정본과 어긋난다(런 341 · T28 38회차 · T128 ⓒ). 게임 흐름에서는 안 부른다.
        /// </summary>
        public static void SettleAll()
        {
            foreach (CardPop c in FindObjectsByType<CardPop>(FindObjectsInactive.Include, FindObjectsSortMode.None)) c.Finish();
            Canvas.ForceUpdateCanvases();
        }

        bool finished;

        void Finish()
        {
            if (finished) return;
            finished = true;
            Transform t = transform.Find(CardName);
            if (Card == null && t != null) Card = t as RectTransform;
            Restore();
            Card = null;
            Destroy(this);
        }

        /// <summary>벽시계 경과(ms) — 시험이 같은 시각의 기대값을 셈한다.</summary>
        public double ElapsedMs { get; private set; }
        /// <summary>지금 값을 건 카드(없으면 null).</summary>
        public RectTransform Card { get; private set; }
        CanvasGroup mine;

        void Update()
        {
            if (finished) return;
            ElapsedMs += Time.unscaledDeltaTime * 1000.0;
            Apply();
        }

        // 카드는 Show 뒤 같은 프레임에 호출부가 세운다 — 렌더 직전에 한 번 더 걸어 첫 프레임이 «다 큰 카드» 로 찍히지 않게.
        void LateUpdate() { if (!finished) Apply(); }

        void Apply()
        {
            if (finished) return;
            CardPopSpec s = Spec;
            Transform t = transform.Find(CardName);
            RectTransform card = t as RectTransform;
            if (card != Card)
            {
                Restore();
                Card = card;
            }
            if (Card == null) { if (s.Done(ElapsedMs)) Destroy(this); return; }
            if (s.Done(ElapsedMs)) { Finish(); return; }
            if (mine == null)
            {
                mine = Card.GetComponent<CanvasGroup>();
                if (mine == null) mine = Card.gameObject.AddComponent<CanvasGroup>();
            }
            float k = (float)s.ScaleAt(ElapsedMs);
            Card.localScale = new Vector3(k, k, 1f);
            mine.alpha = (float)s.AlphaAt(ElapsedMs);
        }

        /// <summary>카드를 원래 모습으로(scale 1 · α 1) — 내가 더한 CanvasGroup 은 걷는다.</summary>
        void Restore()
        {
            if (Card != null) Card.localScale = Vector3.one;
            if (mine != null) { mine.alpha = 1f; Destroy(mine); }
            mine = null;
        }

        void OnDestroy() { Restore(); }
    }
}
