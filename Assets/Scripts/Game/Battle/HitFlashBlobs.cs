using System;
using System.Collections.Generic;
using UnityEngine;
using Forge.Core.BattleFx;
using Forge.Game.Hero;
using Forge.Game.Voxel;

namespace Forge.Game.Battle
{
    /// <summary>
    /// 영웅 접지 블롭(정본 `heroBlob` · 4735) + 시체 접지 그림자 3개(`corpseBlob` · 13414 — 어깨·골반·무릎). 누우면 발밑 원형은 거의 지우고(0.17 → 0.06) 뼈 자리마다
    /// 타원(1.35×0.78 · 0.5)을 0.3초 스무스스텝으로 켠다 · 기상하면 되돌린다. 리그 좌표에서 중심을 읽어 몸을 따라간다.
    /// </summary>
    public sealed class HeroBlobs
    {
        readonly HeroRig rig;
        readonly FxAnims anims;
        readonly MeshRenderer baseBlob;
        readonly Material baseMat;
        readonly List<MeshRenderer> corpse = new List<MeshRenderer>();
        readonly List<Material> corpseMats = new List<Material>();
        public bool CorpseOn { get; private set; }
        public double BaseOpacity { get { return FxMaterials.GetColor(baseMat).a; } }

        public HeroBlobs(HeroRig rig, FxAnims anims)
        {
            this.rig = rig;
            this.anims = anims;
            baseBlob = BlobShadow.Create(rig.transform, "HeroBlob", FxRules.HeroBlobOpacity);
            baseBlob.transform.localPosition = new Vector3(0, (float)FxRules.HeroBlobY, 0);
            baseBlob.transform.localScale = Vector3.one * (float)FxRules.HeroBlobScale;
            baseMat = baseBlob.sharedMaterial;
        }

        void EnsureCorpse()
        {
            if (corpse.Count > 0) return;
            foreach (string bone in FxRules.CorpseBones)
            {
                var mr = BlobShadow.Create(rig.transform, "CorpseBlob " + bone, 0);
                mr.transform.localPosition = new Vector3(0, (float)FxRules.CorpseBlobY, 0);
                mr.transform.localScale = new Vector3((float)FxRules.CorpseBlobSx, (float)FxRules.CorpseBlobSz, 1);
                mr.gameObject.SetActive(false);
                corpse.Add(mr);
                corpseMats.Add(mr.sharedMaterial);
            }
        }

        /// <summary>`corpseBlob(on)`.</summary>
        public void Corpse(bool on)
        {
            EnsureCorpse();
            CorpseOn = on;
            var from = new double[corpse.Count];
            for (int i = 0; i < corpse.Count; i++) from[i] = FxMaterials.GetColor(corpseMats[i]).a;
            double fromBase = FxMaterials.GetColor(baseMat).a;
            if (on)
            {
                for (int i = 0; i < corpse.Count; i++)
                {
                    Transform bn = rig.Bone(FxRules.CorpseBones[i]);
                    if (bn != null)
                    {
                        Vector3 p = rig.transform.InverseTransformPoint(bn.position);
                        corpse[i].transform.localPosition = new Vector3(p.x, (float)FxRules.CorpseBlobY, p.z);
                    }
                    corpse[i].gameObject.SetActive(true);
                }
            }
            double toOp = on ? FxRules.CorpseBlobOpacity : 0, toBase = on ? FxRules.CorpseBaseOpacity : FxRules.HeroBlobOpacity;
            anims.Add(FxRules.CorpseBlobDur, k =>
            {
                double e = FxRules.SmoothStep01(k);
                for (int i = 0; i < corpse.Count; i++) { var c = FxMaterials.GetColor(corpseMats[i]); c.a = (float)(from[i] + (toOp - from[i]) * e); FxMaterials.SetColor(corpseMats[i], c); }
                var b = FxMaterials.GetColor(baseMat); b.a = (float)(fromBase + (toBase - fromBase) * e); FxMaterials.SetColor(baseMat, b);
            }, () => { if (!on) foreach (var m in corpse) m.gameObject.SetActive(false); });
        }

        public void Destroy()
        {
            if (baseBlob != null) { UnityEngine.Object.Destroy(baseBlob.gameObject); UnityEngine.Object.Destroy(baseMat); }
            for (int i = 0; i < corpse.Count; i++) { if (corpse[i] != null) UnityEngine.Object.Destroy(corpse[i].gameObject); UnityEngine.Object.Destroy(corpseMats[i]); }
            corpse.Clear(); corpseMats.Clear();
        }
    }
}
