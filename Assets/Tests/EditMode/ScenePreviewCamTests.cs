using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.World;

namespace Forge.Tests
{
    /// <summary>
    /// T105 — 플레이어 정보 팝업 미니 씬의 카메라 리그는 정본 `Scene3D.previewBuild()` 의 것(fov 42 · (0.1,1.95,4.3) → (0.05,0.92,0) · 영웅 리그 (0,0,0.4))이다.
    /// 추출기(`tools/export_data.js` · PREVIEW_CAM)가 scene.json 에 넣고 <see cref="SceneDefs"/> 가 강타입으로 세운다 — 코드에 박힌 수가 없어야 한다(§1).
    /// </summary>
    public class ScenePreviewCamTests
    {
        static string Json() { return File.ReadAllText(Path.Combine(DataDir.Path, SceneDefs.File)); }

        [Test]
        public void 정본_previewBuild_카메라_리그가_표에서_선다()
        {
            var d = SceneDefs.Parse(Json());
            Assert.IsTrue(d.HasPreviewCam, "PREVIEW_CAM");
            Assert.AreEqual(42.0, d.PvFov, 1e-9, "fov");
            Assert.AreEqual(0.1, d.PvNear, 1e-9, "near");
            Assert.AreEqual(120.0, d.PvFar, 1e-9, "far");
            CollectionAssert.AreEqual(new[] { 0.1, 1.95, 4.3 }, d.PvPos, "position.set");
            CollectionAssert.AreEqual(new[] { 0.05, 0.92, 0.0 }, d.PvLook, "lookAt");
            CollectionAssert.AreEqual(new[] { 0.0, 0.0, 0.4 }, d.PvHero, "rig.group.position");
        }

        [Test]
        public void 카메라는_영웅_앞_위에서_가슴을_보고_본편보다_바짝_붙는다()
        {
            var d = SceneDefs.Parse(Json());
            Assert.Greater(d.PvPos[2], d.PvHero[2], "영웅보다 +z(three · 관객 쪽)");
            Assert.Greater(d.PvPos[1], d.PvLook[1], "위에서 아래로 본다");
            Assert.Less(d.PvFov, d.CamFov, "본편 리그(CAM_FOV)보다 좁은 화각 — «멀면 초록 판에 점 하나»");
            double dz = d.PvPos[2] - d.PvHero[2];
            Assert.Less(dz, d.CamPos[2], "본편 카메라(CAM_POS z)보다 가깝다");
        }

        [Test]
        public void 옛_표에_키가_없으면_물러난다()
        {
            var o = MiniJson.ParseObject(Json());
            Assert.IsTrue(o.Remove("PREVIEW_CAM"));
            var d = SceneDefs.From(o);
            Assert.IsFalse(d.HasPreviewCam);
            Assert.IsNull(d.PvPos);
            Assert.AreEqual(0.0, d.PvFov, 1e-12);
        }
    }
}
