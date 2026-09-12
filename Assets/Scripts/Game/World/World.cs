using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Forge.Core.Data;
using Forge.Core.Voxel;
using Forge.Core.World;
using Forge.Game.Voxel;

namespace Forge.Game.Map
{
    /// <summary>
    /// 맵·바이옴(ROUTINE T9 · 정본 `scene3d.js` 의 `buildTerrain`·`voxelGroundGeo`·`setTheme`·`update` 스크롤 — 배포 상태 `SIMPLE_BG: true` 그대로).
    /// 배경 제거 모드에서 세계는 **복셀 지면 타일(정점색 = 노면 포석·연석·흙 결·셀 지터) + 안개·단색 배경 + 광원 3(태양·반구광·림) + 챕터 테마 파생색** 이고
    /// 소품·스캐터·능선·구름·안개 블롭·하늘 돔은 정본이 통째로 끈다(`buildProps` 조기 return · `buildTerrain`/`buildSky` 분기). 그래서 여기도 그리지 않는다(T35 가 `SIMPLE_BG=false` 경로).
    /// 수치·색은 `scene.json`(정본 상수표) + <see cref="WorldRules"/>(setTheme 인라인 규칙) + `gamedata.json` CHAPTER_THEMES 에서만. 파생 계산은 Core <see cref="WorldGrade"/>.
    /// 씬은 안 만진다 — 부팅 씬의 <see cref="Bootstrap"/> 아래에 스스로 선다(T18 UiRoot·T13 SaveIo 와 같은 길).
    /// 네임스페이스가 폴더(World)와 다른 이유: `Forge.Game.World.World` 는 다른 `Forge.Game.*` 안에서 `World` 가 네임스페이스로 잡혀 못 쓴다(결정 기록).
    /// </summary>
    [DefaultExecutionOrder(-800)]
    public sealed class World : MonoBehaviour
    {
        public const string DataFolder = "data";
        public const string SunName = "Sun";
        public const string RimName = "Rim";

        public static World Instance { get; private set; }
        public static event Action OnReady;

        public bool Ready { get; private set; }
        public SceneDefs Defs { get; private set; }
        public IReadOnlyList<ChapterTheme> Themes { get; private set; }
        public int ThemeIndex { get; private set; }
        public ThemeLook Look { get; private set; }
        public GameObject Ground { get; private set; }
        public Mesh GroundMesh { get; private set; }
        public Material GroundMaterial { get; private set; }
        /// <summary>영웅의 월드 x(정본 `worldX` · 행군하면 +1.7/s).</summary>
        public double WorldX { get; private set; }
        /// <summary>지면 타일의 x(정본 `ground.position.x` · 주기 30 으로 순환).</summary>
        public double GroundX { get; private set; }

        private Light sun, rim;
        private Camera cam;
        private Volume volume;
        private float[] baseMult;
        private bool[] road;
        private Color[] colors;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Instance != null) return;
            foreach (Bootstrap b in Resources.FindObjectsOfTypeAll<Bootstrap>())
            {
                if (!b.gameObject.scene.isLoaded) continue;
                Create(b.transform);
                return;
            }
        }

        public static World Create(Transform parent)
        {
            if (Instance != null) return Instance;
            var go = new GameObject("World");
            go.transform.SetParent(parent, false);
            World w = go.AddComponent<World>();
            w.StartCoroutine(w.Boot());
            return w;
        }

        private void Awake() { Instance = this; ThemeIndex = -1; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private IEnumerator Boot()
        {
            string sceneJson = null, gameJson = null;
            yield return ReadText(SceneDefs.File, s => sceneJson = s);
            yield return ReadText(GameData.GameDataFile, s => gameJson = s);
            if (sceneJson == null || gameJson == null)
            {
                Debug.LogError("[World] StreamingAssets/data/" + SceneDefs.File + " 또는 " + GameData.GameDataFile + " 를 못 읽었다 — 세계를 세우지 않는다");
                yield break;
            }
            Defs = SceneDefs.Parse(sceneJson);
            Themes = ParseThemes(gameJson);
            FindSceneRefs();
            BuildGround();
            SetTheme(0);
            Ready = true;
            Action h = OnReady;
            if (h != null) h();
        }

        /// <summary>`gamedata.json` 의 CHAPTER_THEMES 만(GameData 전체를 세우지 않는다 — 그건 SaveIo 의 몫).</summary>
        public static List<ChapterTheme> ParseThemes(string gamedataJson)
        {
            JsonObject o = MiniJson.ParseObject(gamedataJson);
            return J.List(J.Require(o, "CHAPTER_THEMES"), v =>
            {
                var t = J.Obj(v);
                return new ChapterTheme { Sky = J.Int(t["sky"]), Fog = J.Int(t["fog"]), Ground = J.Int(t["ground"]), Biome = J.Str(t["biome"]), Celestial = J.Str(t["celestial"]), Raw = t };
            });
        }

        /// <summary>에디터·PC 는 파일로, Android(apk 안)·WebGL(URL)은 UnityWebRequest 로 — `streamingAssetsPath` 규약(SaveIo 와 같다).</summary>
        private static IEnumerator ReadText(string name, Action<string> done)
        {
            string p = Path.Combine(Application.streamingAssetsPath, DataFolder, name);
            if (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.WebGLPlayer)
            {
                try { done(File.ReadAllText(p)); } catch (Exception e) { Debug.LogError("[World] " + p + " 읽기 실패: " + e.Message); done(null); }
                yield break;
            }
            using (var req = UnityWebRequest.Get(p))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success) done(req.downloadHandler.text);
                else { Debug.LogError("[World] " + p + " 읽기 실패: " + req.error); done(null); }
            }
        }

        private void FindSceneRefs()
        {
            foreach (Light l in Resources.FindObjectsOfTypeAll<Light>())
            {
                if (!l.gameObject.scene.isLoaded) continue;
                if (l.name == SunName) sun = l;
                else if (l.name == RimName) rim = l;
            }
            cam = Camera.main;
            foreach (Volume v in Resources.FindObjectsOfTypeAll<Volume>())
            {
                if (!v.gameObject.scene.isLoaded) continue;
                volume = v;
                break;
            }
        }

        /// <summary>정본 `voxelGroundGeo()` → Mesh. three(오른손) 좌표를 z 반전(결정 4)하고 삼각형 감김을 뒤집는다. 정점색은 재질색에 곱해지는 계수라 선형 색공간이면 선형으로.</summary>
        private void BuildGround()
        {
            GroundMesh gm = GroundGrid.Build(Defs);
            int n = gm.VertexCount;
            var verts = new Vector3[n];
            var norms = new Vector3[n];
            var uvs = new Vector2[n];
            baseMult = new float[n * 3];
            road = new bool[n];
            colors = new Color[n];
            for (int i = 0; i < n; i++)
            {
                int p = i * 3;
                verts[i] = new Vector3((float)gm.Positions[p], (float)gm.Positions[p + 1], -(float)gm.Positions[p + 2]);
                norms[i] = new Vector3((float)gm.Normals[p], (float)gm.Normals[p + 1], -(float)gm.Normals[p + 2]);
                uvs[i] = new Vector2((float)gm.Uvs[i * 2], (float)gm.Uvs[i * 2 + 1]);
                baseMult[p] = (float)gm.Colors[p]; baseMult[p + 1] = (float)gm.Colors[p + 1]; baseMult[p + 2] = (float)gm.Colors[p + 2];
                road[i] = gm.Road[i];
            }
            var tris = new int[n];
            for (int i = 0; i + 2 < n; i += 3) { tris[i] = i; tris[i + 1] = i + 2; tris[i + 2] = i + 1; }

            var mesh = new Mesh();
            mesh.name = "ground (voxelGroundGeo)";
            if (n > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            GroundMesh = mesh;

            Ground = new GameObject("ground");
            Ground.transform.SetParent(transform, false);
            Ground.AddComponent<MeshFilter>().sharedMesh = mesh;
            GroundMaterial = new Material(VoxelMaterials.Get(MobBuilder.MatKey(null), null));
            GroundMaterial.name = "terrainMat";
            var mr = Ground.AddComponent<MeshRenderer>();
            mr.sharedMaterial = GroundMaterial;
            mr.receiveShadows = true;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // 포석 줄눈 데칼(T34 · 정본 buildTerrain 의 pathMesh · ground 의 자식이라 타일 순환을 따라간다)
            GroundTextures.AttachCobble(Ground.transform, Defs);
            GroundX = 0;
            WorldX = 0;
        }

        private static Color ToColor(Col c) { return new Color((float)c.R, (float)c.G, (float)c.B, 1f); }

        /// <summary>챕터 테마를 입힌다(정본 `setTheme(CHAPTER_THEMES[i])` 중 SIMPLE_BG 에서 보이는 것 전부).</summary>
        public void SetTheme(int index)
        {
            if (Themes == null || Themes.Count == 0) throw new InvalidOperationException("세계가 아직 안 섰다(Ready 전)");
            index = ((index % Themes.Count) + Themes.Count) % Themes.Count;
            ThemeLook L = WorldGrade.Compute(Defs, Themes[index]);
            ThemeIndex = index;
            Look = L;

            // 안개 + 단색 배경 (SIMPLE_BG: scene.background = fogC · 지면 far 모서리가 같은 색에 녹는다)
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = ToColor(L.FogColor);
            RenderSettings.fogStartDistance = (float)L.FogNear;
            RenderSettings.fogEndDistance = (float)L.FogFar;
            if (cam != null) cam.backgroundColor = ToColor(L.Background);

            // 반구광(hemi.color = 하늘 · groundColor = gC −0.1L · intensity) → 트라이라이트 앰비언트(T1 과 같은 환산: 색 × 세기 · 적도 = 둘의 평균)
            Color skyA = ToColor(L.HemiColor) * (float)L.HemiIntensity;
            Color gndA = ToColor(L.HemiGround) * (float)L.HemiIntensity;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = skyA;
            RenderSettings.ambientGroundColor = gndA;
            RenderSettings.ambientEquatorColor = (skyA + gndA) * 0.5f;

            // 태양: 정본은 위치 벡터로 방향을 준다(원점을 본다). z 부호 반전.
            if (sun != null)
            {
                sun.color = ToColor(L.SunColor);
                sun.intensity = (float)L.SunIntensity;
                Vector3 p = ThreeSpace.Pos(L.SunPos[0], L.SunPos[1], L.SunPos[2]);
                sun.transform.position = p;
                sun.transform.rotation = Quaternion.LookRotation(-p.normalized, Vector3.up);
            }
            if (rim != null)
            {
                rim.color = ToColor(L.RimColor);
                rim.intensity = (float)L.RimIntensity;
            }

            // 톤맵 노출 (renderer.toneMappingExposure → URP ColorAdjustments.postExposure EV = log2)
            if (volume != null && volume.profile != null)
            {
                ColorAdjustments ca;
                if (volume.profile.TryGet(out ca))
                {
                    ca.postExposure.overrideState = true;
                    ca.postExposure.value = (float)Math.Log(L.Exposure, 2);
                }
            }

            // 지면: 재질색 = 흙 보정색 · 노면 회랑(|z|<2.25)은 셰이더 uRoad 대신 정점색에 배율을 곱한다(같은 곱셈 · 결정 기록)
            GroundMaterial.SetColor("_BaseColor", ToColor(L.TerrainColor));
            // 바이옴 지면 소재(알베도 12×6 · 노멀 0.7) + 용암 발광 균열(T34 · 정본 groundTexFor / setTheme emissiveMap 갈래)
            GroundTextures.Apply(GroundMaterial, Defs, L);
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            float rr = (float)L.Road[0], rg = (float)L.Road[1], rb = (float)L.Road[2];
            for (int i = 0; i < colors.Length; i++)
            {
                int p = i * 3;
                var c = road[i]
                    ? new Color(baseMult[p] * rr, baseMult[p + 1] * rg, baseMult[p + 2] * rb, 1f)
                    : new Color(baseMult[p], baseMult[p + 1], baseMult[p + 2], 1f);
                colors[i] = linear ? c.linear : c;
            }
            GroundMesh.SetColors(colors);
        }

        /// <summary>정본 `update`: 영웅 월드 x 가 지면 타일보다 15 넘게 앞서면 타일을 30 밀어 순환(높이 함수 주기 30 이라 이음 무결).</summary>
        public void SetWorldX(double worldX)
        {
            WorldX = worldX;
            while (WorldX - GroundX > WorldRules.ScrollAhead) GroundX += WorldRules.TilePeriod;
            if (Ground != null) Ground.transform.localPosition = new Vector3((float)GroundX, 0f, 0f);
        }

        /// <summary>정본 `heightAt(x, z)`(three 좌표 · SIMPLE_BG 면 0) — 물건을 놓을 때 이것을 쓴다.</summary>
        public double HeightAt(double x, double z) { return GroundGrid.HeightAt(Defs, x, z); }
    }
}
