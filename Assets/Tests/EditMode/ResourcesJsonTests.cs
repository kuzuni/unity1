using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;

namespace Forge.Tests
{
    /// <summary>
    /// T156 5회차 — `Assets/Forge/Resources/*.json`(곁 표들)을 **클론의 파서로** 읽어 본다.
    ///
    /// 왜 필요한가: 그 표들은 유니티에서 `Resources.Load` + <see cref="MiniJson"/> 로 읽히는데, 워커의 §3 게이트에는
    /// 그 길을 지나는 자가 하나도 없었다 — `dotnet test` 는 표를 안 읽고, `gen_ui_catalog --check` 는 `catalog.json`
    /// 하나만 본다. 그래서 표 하나가 **MiniJson 으로만 못 읽히는 꼴**이면 로컬 게이트가 전부 초록인 채로 push 되고,
    /// 그 표를 쓰는 화면이 서는 순간 **남의 PlayMode 자 넷까지 예외로 죽는다**(런 429 실측 · T156 `RibbonUi.json`).
    /// 파이썬 `json` 은 읽히는데 MiniJson 은 못 읽는 자리가 있다(주석·후행 쉼표·제어문자 규칙이 다르다) —
    /// 그러니 **쓰는 쪽 파서로** 재야 한다.
    /// </summary>
    public class ResourcesJsonTests
    {
        static string ResourcesDir()
        {
            foreach (string start in new[] { Directory.GetCurrentDirectory(), AppDomain.CurrentDomain.BaseDirectory })
            {
                string d = start;
                for (int i = 0; i < 10 && !string.IsNullOrEmpty(d); i++)
                {
                    string cand = Path.Combine(d, "Assets", "Forge", "Resources");
                    if (Directory.Exists(cand)) return cand;
                    d = Path.GetDirectoryName(d);
                }
            }
            throw new DirectoryNotFoundException("Assets/Forge/Resources 를 못 찾았다 — cwd=" + Directory.GetCurrentDirectory());
        }

        [Test]
        public void 곁_표는_전부_MiniJson_으로_읽힌다()
        {
            string dir = ResourcesDir();
            string[] files = Directory.GetFiles(dir, "*.json");
            Assert.Greater(files.Length, 0, "Resources 에 표가 하나도 없다 — 경로를 잘못 찾았다(" + dir + ")");
            List<string> bad = new List<string>();
            foreach (string f in files)
            {
                string text = File.ReadAllText(f);
                try
                {
                    object o = MiniJson.Parse(text);
                    if (J.Obj(o) == null) bad.Add(Path.GetFileName(f) + ": 최상위가 «상자»가 아니다");
                }
                catch (Exception e) { bad.Add(Path.GetFileName(f) + ": " + e.Message); }
            }
            Assert.AreEqual(0, bad.Count, "MiniJson 이 못 읽는 곁 표가 있다 — 그 표를 쓰는 화면이 서는 순간 예외다:\n  · "
                            + string.Join("\n  · ", bad.ToArray()));
        }
    }
}
