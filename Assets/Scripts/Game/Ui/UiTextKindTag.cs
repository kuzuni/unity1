using UnityEngine;

namespace Forge.Game.Ui
{
    /// <summary>UiKit 이 만든 글자에 붙는 표식 — 어떤 종류(TextKind)로 만들었는지. TextSizeGateTests 가 «모든 활성 글자가 종류 하한 이상» 을 이것으로 잰다.</summary>
    public sealed class UiTextKindTag : MonoBehaviour
    {
        public TextKind Kind;
    }
}
