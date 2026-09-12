using UnityEngine;
using Forge.Core.Data;
using Forge.Core.Gear;
using Forge.Core.Hero;

namespace Forge.Game.Hero
{
    /// <summary>
    /// 페이퍼돌(T15) — 원작 `Scene3D.refreshHeroEquip` 의 자리: 장착 장비(<see cref="GearState"/>)를 영웅 리그(T6 <see cref="HeroRig"/>)에 입힌다.
    /// 지금 잇는 것: **무기**(종 → `HeroRig.Equip` · 파지·거치 자세는 T6 규약) + 표 값(<see cref="PaperdollLook"/> · 투구 스타일/이름 · 갑옷 색·발광·문장 색).
    /// 무기 8종 형상·투구·갑옷 옷의 3D 외형(정본 `makeWeapon`·`makeHelmet`·`makeArmorExtras`·`dressMcRig` ≈ 1500줄 · 텍스처 의존)은 캡처 이식 작업 T37 이
    /// <see cref="WeaponMeshProvider"/>·<see cref="OnDressed"/> 로 꽂는다 — 그때까지 무기는 T6 의 막대 한 자루다.
    /// </summary>
    public static class Paperdoll
    {
        /// <summary>무기 종 → 메시(T37 `makeWeapon` 캡처). null 이면 막대.</summary>
        public delegate Mesh WeaponMeshFn(string wtypeId, PaperdollLook look);
        public static WeaponMeshFn WeaponMeshProvider;
        /// <summary>투구·갑옷 외형을 입히는 훅(T37 `makeHelmet`·`dressMcRig`) — 무기 장착 뒤 호출된다.</summary>
        public static System.Action<HeroRig, PaperdollLook, bool> OnDressed;

        public static PaperdollLook Last { get; private set; }

        /// <summary>`refreshHeroEquip(withFlash)` — 무기 그룹 교체 + 파지 + 표 값 계산 + 훅. 반환: 이번 표 값.</summary>
        public static PaperdollLook Refresh(HeroRig rig, GameDefs defs, GearState state, bool withFlash = false, PoseAdd ridePose = null, FreeHandReach reach = null)
        {
            var look = PaperdollLook.Of(defs, state);
            Last = look;
            if (rig != null)
            {
                WeaponType def = defs.WeaponTypes.Get(look.WtypeId, null);
                Mesh mesh = WeaponMeshProvider != null ? WeaponMeshProvider(look.WtypeId, look) : null;
                rig.Equip(look.WtypeId, def, mesh, ridePose, reach);
                if (OnDressed != null) OnDressed(rig, look, withFlash);
            }
            return look;
        }

        /// <summary>0xRRGGBB → 유니티 sRGB 색(결정 4) — 갑옷·문장 색을 재질에 줄 때.</summary>
        public static Color ToColor(int hex)
        {
            return new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f, 1f);
        }
    }
}
