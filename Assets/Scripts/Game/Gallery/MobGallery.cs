using System;
using System.Collections.Generic;
using UnityEngine;
using Forge.Core.Data;
using Forge.Core.Voxel;
using Forge.Game.Voxel;

namespace Forge.Game.Gallery
{
    /// <summary>도감 표 다섯 — 원작 시트(`web/tools/shot-mobs.js` pets|mounts|enemies|skillfx) + 소품 표본(T2 `mobs-props.json` samples).</summary>
    public enum GalleryKind { Pets, Mounts, Enemies, SkillFx, Props }

    /// <summary>도감의 한 칸 — 종(또는 소품 표본) 하나.</summary>
    public sealed class GallerySpecies
    {
        public GalleryKind Kind;
        public string Name;
        /// <summary>몹 표의 종(소품이면 null).</summary>
        public MobModel Model;
        /// <summary>세울 때 쓴 칸 크기(세계 단위).</summary>
        public double Cell;
        /// <summary>소품 표본(몹이면 null).</summary>
        public PropSample Prop;
    }

    /// <summary>세운 결과 — 종 + 루트 + 파츠 수.</summary>
    public sealed class GalleryEntry
    {
        public GallerySpecies Species;
        public GameObject Root;
        /// <summary>몹이면 조립 결과(소품이면 null).</summary>
        public VoxelMobRig Rig;
        public int PartCount;

        /// <summary>루트 기준 경계 상자(렌더러 AABB 합집합 · 루트 위치를 뺀 값).</summary>
        public Bounds LocalBounds { get { return MobGallery.LocalBounds(Root); } }
    }

    /// <summary>
    /// 몹 도감(T5) — JSON 의 모든 종을 <see cref="VoxelMob"/> 로 세워 격자에 놓는다. 원작 시트 `web/tools/mobsheet.html renderMob` 과 같은 규약:
    /// 탈것은 `cell = 0.44 / model.seat`(안장 계약 · `MOUNT_FORMS.quad.saddle`) · 나머지는 표의 `cell` · vivid 0 · 4열.
    /// 숫자는 종 표(JSON)와 정본 시트의 것만이다 — 여기서 종 좌표를 고치지 않는다(§1 조형 계약).
    /// </summary>
    public static class MobGallery
    {
        /// <summary>원작 `mobsheet.html`: `Mobs.build(model, model.seat ? { cell: 0.44 / model.seat } : {})`.</summary>
        public const double MountSaddleCell = 0.44;
        /// <summary>원작 시트 열 수(`SHEET.*.cols`).</summary>
        public const int Columns = 4;

        public static readonly GalleryKind[] AllKinds = { GalleryKind.Pets, GalleryKind.Mounts, GalleryKind.Enemies, GalleryKind.SkillFx, GalleryKind.Props };

        /// <summary>시트 파일 이름(ROUTINE T5: `ui-screens/mobs_pets.png` …).</summary>
        public static string FileName(GalleryKind kind)
        {
            switch (kind)
            {
                case GalleryKind.Pets: return "mobs_pets";
                case GalleryKind.Mounts: return "mobs_mounts";
                case GalleryKind.Enemies: return "mobs_enemies";
                case GalleryKind.SkillFx: return "mobs_skillfx";
                default: return "mobs_props";
            }
        }

        /// <summary>정본 벡터·JSON 의 표 키(pets · mounts · enemies · skillfx · props).</summary>
        public static string TableKey(GalleryKind kind)
        {
            switch (kind)
            {
                case GalleryKind.Pets: return "pets";
                case GalleryKind.Mounts: return "mounts";
                case GalleryKind.Enemies: return "enemies";
                case GalleryKind.SkillFx: return "skillfx";
                default: return "props";
            }
        }

        public static MobTable TableOf(GameData data, GalleryKind kind)
        {
            switch (kind)
            {
                case GalleryKind.Pets: return data.Pets;
                case GalleryKind.Mounts: return data.Mounts;
                case GalleryKind.Enemies: return data.Enemies;
                case GalleryKind.SkillFx: return data.SkillFx;
                default: return null;
            }
        }

        /// <summary>시트 규약의 칸 크기 — 탈것(seat 가 있는 종)은 안장 계약, 나머지는 표 값(없으면 <see cref="MobBuilder.DefaultCell"/>).</summary>
        public static double CellOf(MobModel model)
        {
            if (model.Seat.HasValue && model.Seat.Value > 0) return MountSaddleCell / model.Seat.Value;
            return model.Cell > 0 ? model.Cell : MobBuilder.DefaultCell;
        }

        /// <summary>표의 종 목록(정본 삽입 순서).</summary>
        public static List<GallerySpecies> SpeciesOf(GameData data, GalleryKind kind)
        {
            var list = new List<GallerySpecies>();
            if (kind == GalleryKind.Props)
            {
                foreach (var kv in data.Props.Samples)
                    list.Add(new GallerySpecies { Kind = kind, Name = kv.Key, Prop = kv.Value, Cell = kv.Value.U });
                return list;
            }
            var table = TableOf(data, kind);
            foreach (var kv in table.Models)
                list.Add(new GallerySpecies { Kind = kind, Name = kv.Key, Model = kv.Value, Cell = CellOf(kv.Value) });
            return list;
        }

        /// <summary>종 하나를 세운다(예외는 그대로 던진다 — 도감의 목적이 «예외 0» 을 재는 것이다).</summary>
        public static GalleryEntry Build(GallerySpecies s, Transform parent)
        {
            var e = new GalleryEntry { Species = s };
            if (s.Prop != null)
            {
                e.Root = GalleryProps.Build(s.Prop, parent, s.Name);
            }
            else
            {
                e.Rig = VoxelMob.Build(s.Model, s.Cell, 0, parent, s.Name);
                e.Root = e.Rig.Root;
            }
            e.PartCount = PartCount(e.Root);
            return e;
        }

        /// <summary>표 하나를 전부 세워 4열 격자에 놓는다(간격 = 그 표에서 가장 큰 바닥 치수 × 1.5 + 0.3).</summary>
        public static List<GalleryEntry> BuildAll(GameData data, GalleryKind kind, Transform parent)
        {
            var species = SpeciesOf(data, kind);
            var entries = new List<GalleryEntry>(species.Count);
            float footprint = 0f;
            for (int i = 0; i < species.Count; i++)
            {
                var e = Build(species[i], parent);
                entries.Add(e);
                Bounds b = e.LocalBounds;
                footprint = Mathf.Max(footprint, b.size.x, b.size.z);
            }
            float pitch = footprint * 1.5f + 0.3f;
            for (int i = 0; i < entries.Count; i++)
            {
                int col = i % Columns, row = i / Columns;
                entries[i].Root.transform.localPosition = new Vector3(col * pitch, 0f, -row * pitch);
            }
            return entries;
        }

        /// <summary>MeshFilter 수 = 파츠 수(정본 `group.traverse(isMesh)`).</summary>
        public static int PartCount(GameObject root)
        {
            return root.GetComponentsInChildren<MeshFilter>(true).Length;
        }

        /// <summary>렌더러 월드 AABB 합집합 — three `Box3.setFromObject` 와 같은 정의(파츠 로컬 상자를 월드 행렬로 옮긴 상자의 합).</summary>
        public static Bounds WorldBounds(GameObject root)
        {
            var rs = root.GetComponentsInChildren<Renderer>(false);
            if (rs.Length == 0) return new Bounds(root.transform.position, Vector3.zero);
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        /// <summary>루트 위치를 뺀 경계 상자(격자에 놓인 뒤에도 정본 벡터와 바로 비교할 수 있게).</summary>
        public static Bounds LocalBounds(GameObject root)
        {
            Bounds b = WorldBounds(root);
            b.center -= root.transform.position;
            return b;
        }
    }
}
