using System.Collections.Generic;
using UnityEngine;

namespace Forge.Game.Gallery
{
    /// <summary>
    /// 빈 씬에 붙이면 표 다섯을 전부 세우는 도감 컴포넌트(T5) — 주인이 에디터에서 원작 시트와 나란히 본다.
    /// 표마다 한 블록(4열 격자) · 블록은 z 뒤쪽으로 이어 붙는다. 촬영은 PlayMode `MobGalleryTests` 가 <see cref="GallerySheet"/> 로 한다.
    /// </summary>
    public sealed class MobGalleryScene : MonoBehaviour
    {
        [Tooltip("표 블록 사이 간격(세계 단위)")]
        public float BlockGap = 1.5f;

        public readonly Dictionary<GalleryKind, List<GalleryEntry>> Built = new Dictionary<GalleryKind, List<GalleryEntry>>();

        private void Start()
        {
            BuildAll();
        }

        public void BuildAll()
        {
            var data = GalleryData.Load();
            float z = 0f;
            foreach (var kind in MobGallery.AllKinds)
            {
                var block = new GameObject(kind.ToString());
                block.transform.SetParent(transform, false);
                block.transform.localPosition = new Vector3(0f, 0f, z);
                var entries = MobGallery.BuildAll(data, kind, block.transform);
                Built[kind] = entries;
                float depth = 0f;
                for (int i = 0; i < entries.Count; i++)
                    depth = Mathf.Max(depth, -entries[i].Root.transform.localPosition.z + entries[i].LocalBounds.size.z);
                z -= depth + BlockGap;
            }
        }
    }
}
