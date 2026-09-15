using UnityEngine;
using Forge.Core.Data;
using Forge.Core.Pets;
using Forge.Game.Voxel;

namespace Forge.Game.Pets
{
    /// <summary>
    /// 출전 펫 한 마리의 시각(T10) — 정본 `makePetMesh`(9436 · `Mobs.build(model, {vivid:0.2})` + `userData.joints`) · `refreshPets`(9762 · 등급 스케일·CREATURE_YAW·대열 자리·위상/속도) ·
    /// `update` 펫 블록(18598 · 따라오기·바운스·sway/yaw/pitch·관절 드라이버). 조형은 T4 <see cref="VoxelMob"/> 가 표(mobs-pets.json) 그대로 세우고 관절은 표가 심어 둔 `joints` 서술을 <see cref="VoxelJoint.Set"/> 로 돌린다.
    /// 수치·식은 전부 Core <see cref="PetSceneRules"/>·<see cref="PetPose"/>·<see cref="PetFormation"/> — 여기는 값을 Transform 에 넣기만 한다(three 좌표 → <see cref="ThreeSpace"/> · 결정 4).
    /// 정본이 펫에 안 거는 것(림 라이트 · 블롭)은 여기도 안 건다. 승천 데코는 정본 9779 대로 `makePetMesh` 뒤 · **스케일 전**에 <see cref="AscendDecor.Apply"/>(T399).
    /// </summary>
    public sealed class PetView
    {
        public readonly string Name, Rarity;
        public readonly int Stars, Index;
        public readonly VoxelMobRig Rig;
        /// <summary>정본 `g`(래퍼 그룹) — 위치·회전은 여기, 스케일은 안의 메시(<see cref="Mesh"/>)에.</summary>
        public readonly Transform G;
        /// <summary>정본 `mesh`(Mobs.build 의 group) — 등급 스케일이 걸린다.</summary>
        public readonly Transform Mesh;
        /// <summary>승천 데코 뿌리(별 0 · 6승천 순환이면 null) — 정본 `deco.userData.ascendDecorRoot`.</summary>
        public readonly AscendDecorRoot Decor;
        public readonly double Scale, SpotX, SpotZ, Phase, Speed;
        public readonly PetMotionSpec Motion;
        public PetPose Pose { get; private set; }
        readonly double[] gRot = { 0, 0, 0 };

        public PetView(Transform parent, int index, string name, string rarity, int stars, MobModel model, GameDefs defs, string[] rarities, double[] spot, double phase, double speed, double creatureYaw)
        {
            Index = index; Name = name; Rarity = rarity; Stars = stars;
            var go = new GameObject("Pet " + index + " " + name);
            go.transform.SetParent(parent, false);
            G = go.transform;
            Rig = VoxelMob.Build(model, 0, PetSceneRules.Vivid, G, "Mesh " + name);
            Mesh = Rig.Root.transform;
            Decor = AscendDecor.Apply(Rig, stars);   // 정본 9779 `applyAscendDecor(mesh, p.stars, 'pet')` — 스케일 전 원본 치수 기준(T399)
            Scale = PetSceneRules.Scale(rarities, rarity);
            Mesh.localScale = Vector3.one * (float)Scale;
            Motion = PetSceneRules.MotionOf(defs, name);
            SpotX = spot[0]; SpotZ = spot[1];
            Phase = phase; Speed = speed;
            gRot[1] = creatureYaw;
            ThreeSpace.Apply(G, gRot);
        }

        /// <summary>한 프레임 — 정본 update 펫 블록. `clock` = 씬 시계(초) · `worldX` = 영웅 월드 x · `walking` = 정본 `this.walking`.</summary>
        public void Step(double clock, double worldX, bool walking, double heroX, double creatureYaw)
        {
            double t = PetPose.Time(clock, Speed, Phase);
            PetPose p = PetPose.Body(Name, Motion, t, heroX, SpotX, SpotZ, worldX, walking, creatureYaw);
            Pose = p;
            G.localPosition = ThreeSpace.Pos(p.X, p.Y, p.Z);
            gRot[0] = p.Rx; gRot[1] = p.Ry; gRot[2] = p.Rz;
            ThreeSpace.Apply(G, gRot);
            var joints = Rig.Joints;
            for (int i = 0; i < joints.Count; i++)
            {
                VoxelJoint j = joints[i];
                j.Set(PetPose.JointAngle(t, Motion.Freq, walking, j.Base, j.Amp, j.Ph, j.F, j.Gain, j.Abs, j.Spin));
            }
        }

        public void Destroy()
        {
            if (G != null) Object.Destroy(G.gameObject);
        }
    }
}
