using System;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using Forge.Game.Gallery;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T174 — 하니스 스텁(`tools/dotnet/Stubs/*.cs`)이 **실물에 없는 서명**을 갖는 것을 막는 자의 «진실» 쪽.
    ///
    /// 왜 여기여야 하나: `tools/dotnet` 은 URP·TMP 를 스텁으로 물고 컴파일만 본다(T48 설계). 그래서 스텁에
    /// 없는 서명을 적어 넣으면 **로컬은 초록인데 유니티가 죽는다** — 그것도 빨간 테스트가 아니라
    /// «Scripts have compiler errors → 결과 XML 0개» 로(§1 «빨간 테스트보다 나쁘다» · 런 409·410·412).
    /// 진짜 TMP·URP 어셈블리를 쥐고 있는 자리는 **여기(유니티 안)** 뿐이라, 이 자가 그 표면을 글자로 찍어
    /// `ui-screens/stub-sigs.txt` 로 남기고(CI 가 `screens` 로 올린다) `tools/check_stub_sigs.py` 가
    /// 그것을 받아 **오프라인으로** 견준다. 그래야 다음 워커가 밀기 **전에** 빨개진다.
    ///
    /// ⚠ 이 자는 아무것도 판정하지 않는다(단언은 «목록을 읽었나 · 하나라도 찾았나» 둘뿐). 판정은 파이썬 자가 한다 —
    ///   여기서 판정하면 그 빨강이 또 «유니티 런 한 번» 뒤에야 보인다.
    /// </summary>
    public class StubSigsTests
    {
        private const string WantPath = "tools/stub-sigs.want.txt";
        private const string OutName = "stub-sigs.txt";

        /// <summary>.NET 이름 → C# 키워드. 스텁은 `int`·`bool` 로 적으니 양쪽을 같은 자로 줄인다.</summary>
        private static string Short(Type t)
        {
            if (t == null) return "?";
            if (t.IsByRef) t = t.GetElementType();
            if (t.IsArray) return Short(t.GetElementType()) + "[]";
            if (t.IsGenericType)
            {
                string bare = t.Name;
                int tick = bare.IndexOf('`');
                if (tick > 0) bare = bare.Substring(0, tick);
                var args = t.GetGenericArguments();
                var sb = new StringBuilder(bare.ToLowerInvariant()).Append('<');
                for (int i = 0; i < args.Length; i++) { if (i > 0) sb.Append(','); sb.Append(Short(args[i])); }
                return sb.Append('>').ToString();
            }
            switch (t.FullName)
            {
                case "System.Int32": return "int";
                case "System.UInt32": return "uint";
                case "System.Int64": return "long";
                case "System.UInt64": return "ulong";
                case "System.Int16": return "short";
                case "System.UInt16": return "ushort";
                case "System.Byte": return "byte";
                case "System.SByte": return "sbyte";
                case "System.Boolean": return "bool";
                case "System.Single": return "float";
                case "System.Double": return "double";
                case "System.Char": return "char";
                case "System.String": return "string";
                case "System.Object": return "object";
                case "System.Void": return "void";
                case "System.Decimal": return "decimal";
            }
            return t.Name.ToLowerInvariant();
        }

        private static Type Find(string full)
        {
            Type t = Type.GetType(full, false);
            if (t != null) return t;
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { t = a.GetType(full, false); } catch (Exception) { t = null; }
                if (t != null) return t;
            }
            return null;
        }

        [Test]
        public void 스텁이_흉내_내는_타입들의_진짜_공개_표면을_글자로_남긴다()
        {
            string root = Directory.GetCurrentDirectory();
            string want = Path.Combine(root, WantPath);
            Assert.IsTrue(File.Exists(want),
                          "물어볼 목록이 없다: " + WantPath + " — `python3 tools/check_stub_sigs.py --emit` 이 쓴다(T174)");

            var sb = new StringBuilder();
            sb.Append("# T174 — 진짜 유니티가 찍은 공개 표면. `tools/check_stub_sigs.py` 가 이것을 받아 스텁과 견준다.\n");
            sb.Append("# fmt 2\n");
            sb.Append("# 한 줄: «T\\t<타입>[\\tnotfound]» · «M\\t<메서드>\\t<줄인 매개변수>» · «P\\t<속성·필드>»\n");
            // ⚠ fmt 2 부터 **선택 매개변수 뒤에 «=» 를 붙인다**(`char,bool=,bool=`). 그게 없으면 스텁이
            //   짧게 적힌 것(`HasCharacter(char)`)이 «실물에 없다» 로 잘못 잡힌다 — 실물이
            //   `HasCharacter(char, bool = false, bool = false)` 라 그 호출은 멀쩡히 컴파일되기 때문이다(첫 판 실측).
            sb.Append("# 유니티 ").Append(Application.unityVersion).Append('\n');

            int types = 0, found = 0, members = 0;
            foreach (string raw in File.ReadAllLines(want))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                string[] parts = line.Split('\t');
                if (parts.Length < 2) continue;
                string full = parts[1].Trim();
                types++;
                Type t = Find(full);
                if (t == null) { sb.Append("T\t").Append(full).Append("\tnotfound\n"); continue; }
                found++;
                sb.Append("T\t").Append(full).Append('\n');

                const BindingFlags F = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
                foreach (MethodInfo m in t.GetMethods(F))
                {
                    if (m.IsSpecialName) continue;            // 속성 접근자(get_/set_)는 아래 P 로 나온다
                    ParameterInfo[] ps = m.GetParameters();
                    var one = new StringBuilder();
                    for (int i = 0; i < ps.Length; i++)
                    {
                        if (i > 0) one.Append(',');
                        one.Append(Short(ps[i].ParameterType));
                        if (ps[i].IsOptional) one.Append('=');      // 선택 매개변수 — 스텁이 여기서 끊어도 호출은 선다
                    }
                    sb.Append("M\t").Append(m.Name).Append('\t').Append(one).Append('\n');
                    members++;
                }
                foreach (PropertyInfo p in t.GetProperties(F)) { sb.Append("P\t").Append(p.Name).Append('\n'); members++; }
                foreach (FieldInfo f in t.GetFields(F)) { sb.Append("P\t").Append(f.Name).Append('\n'); members++; }
                foreach (Type n in t.GetNestedTypes(BindingFlags.Public)) { sb.Append("P\t").Append(n.Name).Append('\n'); members++; }
            }

            string dir = Path.Combine(root, GallerySheet.OutDir);
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, OutName), sb.ToString(), new UTF8Encoding(false));
            Debug.Log("[T174] 진짜 표면을 찍었다 — 타입 " + types + "개 중 " + found + "개를 찾았고 멤버 " + members + "줄 · ui-screens/" + OutName);

            Assert.Greater(types, 0, "물어볼 타입이 한 개도 없다 — " + WantPath + " 가 비었다");
            Assert.Greater(found, 0, "물어본 타입을 하나도 못 찾았다 — 리플렉션이 진짜 어셈블리를 못 쥐었다(판정은 파이썬 자가 한다)");
        }
    }
}
