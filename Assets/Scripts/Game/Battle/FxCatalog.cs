using System;
using System.Collections.Generic;
using UnityEngine;

namespace Forge.Game.Battle
{
    /// <summary>
    /// 히트 파티클 카탈로그(T8) — 용도 키 → 주인 에셋 `JMO Assets/Cartoon FX Remaster` 프리팹(ROUTINE §1 «에셋은 주인 에셋만» · `docs/assets-map.md`).
    /// `Assets/Forge/Resources/FxCatalog.asset` 이 GUID 로 엮는다(T18 UiCatalog 와 같은 길 · Resources 밖 프리팹을 런타임에 읽는 유일한 길).
    /// 키 이름은 원작 연출 자리(hitEnemy 플레어 · killEnemy 버스트 · bossEntrance 착지 · heroHit · heroRevive)다.
    /// </summary>
    public sealed class FxCatalog : ScriptableObject
    {
        public const string ResourcePath = "FxCatalog";

        [Serializable]
        public sealed class Entry
        {
            public string key;
            public GameObject prefab;
        }

        public List<Entry> entries = new List<Entry>();

        static FxCatalog instance;
        static bool warned;

        public static FxCatalog Instance
        {
            get
            {
                if (instance == null) instance = Resources.Load<FxCatalog>(ResourcePath);
                return instance;
            }
        }

        public GameObject Prefab(string key)
        {
            for (int i = 0; i < entries.Count; i++) if (entries[i].key == key) return entries[i].prefab;
            return null;
        }

        /// <summary>프리팹을 위치에 세운다(프리팹의 CFXR_Effect 가 끝나면 스스로 지운다 · 보험으로 <paramref name="life"/> 초 뒤 Destroy). 카탈로그가 없으면 조용히 null.</summary>
        public static GameObject Play(string key, Vector3 pos, float scale = 1f, float life = 4f)
        {
            var cat = Instance;
            if (cat == null)
            {
                if (!warned) { warned = true; Debug.LogWarning("[FxCatalog] Resources/" + ResourcePath + " 이 없다 — 히트 파티클을 건너뛴다"); }
                return null;
            }
            var prefab = cat.Prefab(key);
            if (prefab == null) return null;
            var go = Instantiate(prefab, pos, Quaternion.identity);
            go.name = "fx " + key;
            if (scale != 1f) go.transform.localScale = prefab.transform.localScale * scale;
            Destroy(go, life);
            return go;
        }
    }
}
