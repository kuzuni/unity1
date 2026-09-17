using System;
using UnityEngine;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Game.Battle
{
    /// <summary>T460 — 피해 숫자 아크 표(<c>Assets/Forge/Resources/DmgArcUi.json</c>) 로더. 값은 <see cref="DmgArcSpec"/>(Core)이 쥔다 — <see cref="DmgGlowUi"/> 와 같은 꼴.</summary>
    public static class DmgArcUi
    {
        public const string ResourcePath = "DmgArcUi";
        static DmgArcSpec spec;

        public static DmgArcSpec Spec
        {
            get
            {
                if (spec == null)
                {
                    TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
                    if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T460)");
                    spec = DmgArcSpec.From(MiniJson.ParseObject(ta.text));
                }
                return spec;
            }
        }

        /// <summary>테스트용 — 표를 다시 읽게 한다.</summary>
        public static void Reset() { spec = null; }
    }
}
