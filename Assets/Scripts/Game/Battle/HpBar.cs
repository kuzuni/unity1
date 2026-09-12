using System;
using UnityEngine;
using UnityEngine.Rendering;
using Forge.Core.BattleFx;
using Forge.Game.Hero;
using Forge.Game.Voxel;
using R = Forge.Core.BattleFx.HitRules.HpBar;

namespace Forge.Game.Battle
{
    /// <summary>
    /// 머리 위 HP 바(T8) — 원작 `monsterMesh` 의 hpBg/hpGhost/hpFg/pip(11743~) · `driveHpBar`(12928) · `hitHpBar`(12912) · `killEnemy` 의 드레인(13160~).
    /// 몸의 변형을 상속하지 않게 **씬 직속**으로 두고 주인이 매 프레임 위치만 준다. 적 = 적대 빨강 고정 · 영웅 = 초록/노랑/빨강 램프. 수치는 전부 <see cref="HitRules.HpBar"/>.
    /// </summary>
    public sealed class HpBar
    {
        public readonly Transform Root;
        public readonly bool Foe;
        readonly Transform bg, ghost, fg, pipOuter, pipInner;
        readonly Material bgMat, ghostMat, fgMat, pipOuterMat, pipInnerMat;
        readonly float barBase;
        // driveHpBar 상태
        double hpFlash, ghostV = -1, ghostHold, barPunch, barShake;
        int ghostColor = R.GhostInit;
        public double ShakeX, ShakeY;
        // 처치 드레인
        bool dying; double dieT; double g0; float s0;
        public bool Dying { get { return dying; } }
        public bool Done { get; private set; }

        static Mesh quad;
        static Mesh Quad() { return quad ?? (quad = HeroMeshes.Quad(1, 1, 0xffffff)); }

        /// <param name="barY">바의 로컬 높이(three) — 적은 topY + 간격/배율 · 영웅은 2.18.</param>
        /// <param name="scale">그룹 배율(보스 1.9).</param>
        public HpBar(Transform parent, string name, bool foe, double barY, double scale)
        {
            Foe = foe;
            Root = new GameObject(name).transform;
            Root.SetParent(parent, false);
            barBase = (float)scale;
            Root.localScale = Vector3.one * barBase;
            bg = Part("bg", R.BgW, R.BgH, 0, barY, 0, R.BgColor, R.BgOpacity, out bgMat);
            ghost = Part("ghost", R.FgW, R.FgH, 0, barY, R.GhostZ, R.GhostInit, 0.999, out ghostMat);   // 0.999 = 처치 페이드가 알파를 쓰게 투명 큐 재질로
            fg = Part("fg", R.FgW, R.FgH, 0, barY, R.FgZ, foe ? R.FoeFill : R.HeroFill, 0.999, out fgMat);
            double pipTop = barY - R.BgH / 2 + R.PipYTop;
            pipOuter = Tri("pipOuter", R.PipOuterW, R.PipOuterH, pipTop, 0.011, R.BgColor, R.BgOpacity, out pipOuterMat);
            pipInner = Tri("pipInner", R.PipInnerW, R.PipInnerH, pipTop - R.PipInnerDy, 0.013, foe ? R.FoePip : R.HeroPip, 0.999, out pipInnerMat);
        }

        Transform Part(string name, double w, double h, double x, double y, double z, int hex, double opacity, out Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Root, false);
            go.transform.localPosition = ThreeSpace.Pos(x, y, z);
            go.transform.localScale = new Vector3((float)w, (float)h, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = Quad();
            var mr = go.AddComponent<MeshRenderer>();
            mat = FxMaterials.Instance(hex, opacity);
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go.transform;
        }

        /// <summary>`makeOwnerPip` 의 아래 향한 이등변 삼각(−z 를 본다).</summary>
        Transform Tri(string name, double w, double h, double yTop, double z, int hex, double opacity, out Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Root, false);
            go.transform.localPosition = ThreeSpace.Pos(0, 0, z);
            var mesh = new Mesh();
            mesh.name = "pip";
            mesh.SetVertices(new[] { new Vector3(-(float)w, (float)yTop, 0), new Vector3((float)w, (float)yTop, 0), new Vector3(0, (float)(yTop - h), 0) });
            mesh.SetNormals(new[] { Vector3.back, Vector3.back, Vector3.back });
            mesh.SetColors(new[] { Color.white, Color.white, Color.white });
            mesh.SetTriangles(new[] { 0, 2, 1 }, 0);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mat = FxMaterials.Instance(hex, opacity);
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go.transform;
        }

        static void SetFill(Transform t, double v)
        {
            var s = t.localScale; s.x = (float)(Math.Max(0.001, v) * R.FgW); t.localScale = s;
            var p = t.localPosition; p.x = (float)R.FillX(v); t.localPosition = p;
        }

        public void SetPosition(Vector3 worldPos)
        {
            Root.position = worldPos + new Vector3((float)ShakeX, (float)ShakeY, 0);
        }

        /// <summary>`hitHpBar(o, sev)`.</summary>
        public void Hit(double sev)
        {
            hpFlash = 1;
            ghostHold = R.GhostHold(sev);
            ghostColor = R.GhostColorOnHit(Foe, sev);
            barShake = R.ShakeAdd(barShake, sev);
            barPunch = 1;
        }

        /// <summary>`driveHpBar(o, ratio, dt)` — 앞바 즉시 · 잔상 지연 추격 · 트랙 흰 플래시 · 세로 펀치 · 흔들림.</summary>
        public void Drive(double ratio, float dt)
        {
            if (dying) { DriveDying(dt); return; }
            SetFill(fg, ratio);
            hpFlash = Math.Max(0, hpFlash - dt * R.FlashDecay);
            FxMaterials.SetColor(fgMat, R.FillColor(Foe, ratio));
            Color bgC = VoxelMaterials.ToColor(R.BgColor);
            bgC = Color.Lerp(bgC, Color.white, (float)(hpFlash * R.FlashLerp));
            bgC.a = (float)(R.BgOpacity + hpFlash * R.FlashOpacity);
            FxMaterials.SetColor(bgMat, bgC);
            if (ghostV < 0 || ratio > ghostV) ghostV = ratio;
            if (ghostV > ratio)
            {
                if (ghostHold > 0) ghostHold -= dt;
                else ghostV = R.GhostChase(ghostV, ratio, dt);
            }
            SetFill(ghost, ghostV);
            FxMaterials.SetColor(ghostMat, ghostColor);
            ghost.gameObject.SetActive(ghostV > ratio + R.GhostEps);
            barPunch = Math.Max(0, barPunch - dt / R.PunchDur);
            var s = Root.localScale; s.y = (float)(barBase * (1 + R.PunchK * barPunch)); Root.localScale = s;
            barShake = Math.Max(0, barShake - dt * R.ShakeDecay);
            double sh = barShake;
            ShakeX = sh > R.ShakeEps ? (UnityEngine.Random.value * 2 - 1) * sh * R.ShakeX : 0;
            ShakeY = sh > R.ShakeEps ? (UnityEngine.Random.value * 2 - 1) * sh * R.ShakeY : 0;
        }

        /// <summary>`killEnemy` 의 바 드레인: 앞바 스냅 0 → 잔상 0.06초 홀드 → 0.13초 소진 → 0.12초 커지며 페이드 → 숨김.</summary>
        public void BeginDying()
        {
            if (dying) return;
            dying = true; dieT = 0;
            double v0 = fg.localScale.x / R.FgW;
            g0 = Math.Max(ghost.localScale.x / R.FgW, v0);
            s0 = Root.localScale.x;
            SetFill(fg, 0);
            ghost.gameObject.SetActive(true);
            SetFill(ghost, g0);
            ShakeX = ShakeY = 0;
        }

        void DriveDying(float dt)
        {
            dieT += dt;
            if (dieT <= R.KillT)
            {
                double t = dieT;
                double fl = Clamp01(1 - t / R.KillFlash);
                Color bgC = Color.Lerp(VoxelMaterials.ToColor(R.BgColor), Color.white, (float)fl);
                bgC.a = (float)(R.BgOpacity + fl * R.FlashOpacity);
                FxMaterials.SetColor(bgMat, bgC);
                var s = Root.localScale; s.y = (float)(barBase * (1 + R.KillPunch * Clamp01(1 - t / R.KillPunchT))); Root.localScale = s;
                double gk = Clamp01((t - R.KillHold) / R.KillDrain);
                double gv = Math.Max(0.001, g0 * (1 - gk));
                SetFill(ghost, gv);
                ghost.gameObject.SetActive(gk < 1);
                return;
            }
            double k = Clamp01((dieT - R.KillT) / R.KillFadeT);
            Root.localScale = Vector3.one * (float)(s0 * (1 + R.KillFadeGrow * k));
            float a = (float)(1 - k);
            Fade(bgMat, a); Fade(fgMat, a); Fade(ghostMat, a); Fade(pipOuterMat, a * (float)R.BgOpacity); Fade(pipInnerMat, a);
            if (k >= 1) { Root.gameObject.SetActive(false); Done = true; }
        }

        static void Fade(Material m, float a)
        {
            var c = FxMaterials.GetColor(m); c.a = a; FxMaterials.SetColor(m, c);
        }

        static double Clamp01(double v) { return v < 0 ? 0 : v > 1 ? 1 : v; }

        public void SetVisible(bool on) { Root.gameObject.SetActive(on); }

        public void Destroy()
        {
            if (Root != null) UnityEngine.Object.Destroy(Root.gameObject);
            foreach (var m in new[] { bgMat, ghostMat, fgMat, pipOuterMat, pipInnerMat }) if (m != null) UnityEngine.Object.Destroy(m);
        }
    }
}
