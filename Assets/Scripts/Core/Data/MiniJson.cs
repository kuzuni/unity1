using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Forge.Core.Data
{
    /// <summary>
    /// 키 순서를 보존하는 JSON 객체. 정본 JS 객체는 삽입 순서를 지키고 `U.weightedPick` 이 그 순서로 누적 추첨하므로
    /// (마지막 항목이 폴백) 순서가 곧 결과다 — Dictionary 의 «우연한 순서» 에 기대지 않는다.
    /// </summary>
    public sealed class JsonObject : IEnumerable<KeyValuePair<string, object>>
    {
        readonly List<string> _keys = new List<string>();
        readonly Dictionary<string, object> _map = new Dictionary<string, object>();

        public int Count { get { return _keys.Count; } }
        public IReadOnlyList<string> Keys { get { return _keys; } }

        /// <summary>없는 키는 null (JS 의 undefined 자리).</summary>
        public object this[string key]
        {
            get { object v; return _map.TryGetValue(key, out v) ? v : null; }
            set { if (!_map.ContainsKey(key)) _keys.Add(key); _map[key] = value; }
        }

        public bool Has(string key) { return _map.ContainsKey(key); }
        public bool TryGet(string key, out object value) { return _map.TryGetValue(key, out value); }
        /// <summary>JS `delete obj[key]` — 세이브 로드가 폐기 칸(inventory·heldCrafts·activeMount)을 지운다(T13).</summary>
        public bool Remove(string key) { if (!_map.Remove(key)) return false; _keys.Remove(key); return true; }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            for (int i = 0; i < _keys.Count; i++) yield return new KeyValuePair<string, object>(_keys[i], _map[_keys[i]]);
        }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }

    /// <summary>JSON 문법 오류 — 위치(문자 오프셋)를 담는다.</summary>
    public sealed class JsonException : FormatException
    {
        public readonly int Position;
        public JsonException(string message, int position) : base(message + " (offset " + position + ")") { Position = position; }
    }

    /// <summary>
    /// 의존성 0 의 작은 JSON 파서·직렬화기 (T3). 값 표현: 객체 = <see cref="JsonObject"/> · 배열 = <c>List&lt;object&gt;</c> ·
    /// 수 = <c>double</c> · 문자열 = <c>string</c> · 참/거짓 = <c>bool</c> · null = <c>null</c>.
    /// 수를 전부 double 로 두는 것은 정본(JS)이 그렇기 때문이다 — 색 0xRRGGBB 정수도 double 로 들어와 <see cref="J.Int"/> 로 꺼낸다.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string json)
        {
            if (json == null) throw new ArgumentNullException("json");
            var p = new Parser(json);
            p.SkipWs();
            object v = p.ReadValue();
            p.SkipWs();
            if (!p.AtEnd) throw p.Error("trailing characters");
            return v;
        }

        /// <summary>최상위가 객체인 문서 (data/*.json 은 전부 객체다).</summary>
        public static JsonObject ParseObject(string json)
        {
            var o = Parse(json) as JsonObject;
            if (o == null) throw new JsonException("top-level value is not an object", 0);
            return o;
        }

        /// <summary>
        /// 압축 직렬화(공백 없음). 수는 JS `String(number)` 와 같은 표기(<see cref="JsNum.ToString(double)"/>)라
        /// 원작 세이브(JSON.stringify)와 같은 글자가 나온다. <see cref="Big"/> 은 원작 `toJSON` 처럼 문자열로 나간다.
        /// </summary>
        public static string Serialize(object value)
        {
            var sb = new StringBuilder();
            Write(sb, value);
            return sb.ToString();
        }

        static void Write(StringBuilder sb, object v)
        {
            if (v == null) { sb.Append("null"); return; }
            if (v is string) { WriteString(sb, (string)v); return; }
            if (v is bool) { sb.Append((bool)v ? "true" : "false"); return; }
            if (v is Big) { WriteString(sb, v.ToString()); return; }
            if (v is double) { WriteNumber(sb, (double)v); return; }
            if (v is float) { WriteNumber(sb, (float)v); return; }
            if (v is int) { sb.Append(((int)v).ToString(CultureInfo.InvariantCulture)); return; }
            if (v is long) { sb.Append(((long)v).ToString(CultureInfo.InvariantCulture)); return; }
            if (v is uint) { sb.Append(((uint)v).ToString(CultureInfo.InvariantCulture)); return; }
            if (v is JsonObject)
            {
                sb.Append('{');
                bool first = true;
                foreach (var kv in (JsonObject)v)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    WriteString(sb, kv.Key);
                    sb.Append(':');
                    Write(sb, kv.Value);
                }
                sb.Append('}');
                return;
            }
            if (v is IDictionary<string, object>)
            {
                sb.Append('{');
                bool first = true;
                foreach (var kv in (IDictionary<string, object>)v)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    WriteString(sb, kv.Key);
                    sb.Append(':');
                    Write(sb, kv.Value);
                }
                sb.Append('}');
                return;
            }
            if (v is IEnumerable)
            {
                sb.Append('[');
                bool first = true;
                foreach (object item in (IEnumerable)v)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    Write(sb, item);
                }
                sb.Append(']');
                return;
            }
            throw new ArgumentException("MiniJson cannot serialize " + v.GetType().FullName);
        }

        static void WriteNumber(StringBuilder sb, double d)
        {
            // JSON.stringify 는 NaN·Infinity 를 null 로 쓴다.
            if (double.IsNaN(d) || double.IsInfinity(d)) { sb.Append("null"); return; }
            sb.Append(JsNum.ToString(d));
        }

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        sealed class Parser
        {
            readonly string _s;
            int _i;

            public Parser(string s) { _s = s; _i = 0; }
            public bool AtEnd { get { return _i >= _s.Length; } }
            public JsonException Error(string msg) { return new JsonException(msg, _i); }

            public void SkipWs()
            {
                while (_i < _s.Length)
                {
                    char c = _s[_i];
                    if (c == ' ' || c == '\t' || c == '\n' || c == '\r') _i++;
                    else break;
                }
            }

            char Peek() { if (_i >= _s.Length) throw Error("unexpected end"); return _s[_i]; }

            public object ReadValue()
            {
                char c = Peek();
                switch (c)
                {
                    case '{': return ReadObject();
                    case '[': return ReadArray();
                    case '"': return ReadString();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    default:
                        if (c == '-' || (c >= '0' && c <= '9')) return ReadNumber();
                        throw Error("unexpected character '" + c + "'");
                }
            }

            void Expect(string word)
            {
                if (string.CompareOrdinal(_s, _i, word, 0, word.Length) != 0) throw Error("expected " + word);
                _i += word.Length;
            }

            JsonObject ReadObject()
            {
                var o = new JsonObject();
                _i++;
                SkipWs();
                if (Peek() == '}') { _i++; return o; }
                while (true)
                {
                    SkipWs();
                    if (Peek() != '"') throw Error("expected string key");
                    string key = ReadString();
                    SkipWs();
                    if (Peek() != ':') throw Error("expected ':'");
                    _i++;
                    SkipWs();
                    o[key] = ReadValue();
                    SkipWs();
                    char c = Peek();
                    if (c == ',') { _i++; continue; }
                    if (c == '}') { _i++; return o; }
                    throw Error("expected ',' or '}'");
                }
            }

            List<object> ReadArray()
            {
                var a = new List<object>();
                _i++;
                SkipWs();
                if (Peek() == ']') { _i++; return a; }
                while (true)
                {
                    SkipWs();
                    a.Add(ReadValue());
                    SkipWs();
                    char c = Peek();
                    if (c == ',') { _i++; continue; }
                    if (c == ']') { _i++; return a; }
                    throw Error("expected ',' or ']'");
                }
            }

            string ReadString()
            {
                _i++;
                StringBuilder sb = null;
                int start = _i;
                while (true)
                {
                    if (_i >= _s.Length) throw Error("unterminated string");
                    char c = _s[_i];
                    if (c == '"')
                    {
                        string plain = _s.Substring(start, _i - start);
                        _i++;
                        if (sb == null) return plain;
                        sb.Append(plain);
                        return sb.ToString();
                    }
                    if (c == '\\')
                    {
                        if (sb == null) sb = new StringBuilder();
                        sb.Append(_s, start, _i - start);
                        _i++;
                        if (_i >= _s.Length) throw Error("unterminated escape");
                        char e = _s[_i++];
                        switch (e)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                if (_i + 4 > _s.Length) throw Error("bad \\u escape");
                                int code;
                                if (!int.TryParse(_s.Substring(_i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code)) throw Error("bad \\u escape");
                                sb.Append((char)code);
                                _i += 4;
                                break;
                            default: throw Error("bad escape '\\" + e + "'");
                        }
                        start = _i;
                        continue;
                    }
                    if (c < 0x20) throw Error("control character in string");
                    _i++;
                }
            }

            double ReadNumber()
            {
                int start = _i;
                if (_s[_i] == '-') _i++;
                while (_i < _s.Length)
                {
                    char c = _s[_i];
                    if ((c >= '0' && c <= '9') || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-') _i++;
                    else break;
                }
                string text = _s.Substring(start, _i - start);
                double d;
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) throw new JsonException("bad number '" + text + "'", start);
                return d;
            }
        }
    }

    /// <summary>
    /// 파싱된 JSON 트리에서 값을 꺼내는 도우미. 없는 키·틀린 형은 «기본값» 으로 돌린다(정본 JS 의 undefined 관용과 같다) —
    /// 표가 어긋났는지 «검사» 하는 자리는 <see cref="Require"/> 를 쓴다.
    /// </summary>
    public static class J
    {
        public static JsonObject Obj(object v) { return v as JsonObject; }
        public static List<object> Arr(object v) { return v as List<object>; }
        public static bool IsNum(object v) { return v is double; }

        public static double Num(object v, double def = 0) { return v is double ? (double)v : def; }
        public static int Int(object v, int def = 0) { return v is double ? (int)(double)v : def; }
        public static uint UInt(object v, uint def = 0) { return v is double ? (uint)(double)v : def; }
        public static string Str(object v, string def = null) { return v is string ? (string)v : def; }
        public static bool Bool(object v, bool def = false) { return v is bool ? (bool)v : def; }
        public static double? NumOrNull(object v) { return v is double ? (double?)(double)v : null; }

        public static object Require(JsonObject o, string key)
        {
            object v;
            if (o == null || !o.TryGet(key, out v)) throw new JsonException("missing key '" + key + "'", 0);
            return v;
        }

        public static double[] NumArr(object v)
        {
            var a = Arr(v);
            if (a == null) return null;
            var r = new double[a.Count];
            for (int i = 0; i < a.Count; i++) r[i] = Num(a[i]);
            return r;
        }

        public static int[] IntArr(object v)
        {
            var a = Arr(v);
            if (a == null) return null;
            var r = new int[a.Count];
            for (int i = 0; i < a.Count; i++) r[i] = Int(a[i]);
            return r;
        }

        public static string[] StrArr(object v)
        {
            var a = Arr(v);
            if (a == null) return null;
            var r = new string[a.Count];
            for (int i = 0; i < a.Count; i++) r[i] = Str(a[i]);
            return r;
        }

        public static OrderedMap<string> StrMap(object v)
        {
            var o = Obj(v); var m = new OrderedMap<string>();
            if (o != null) foreach (var kv in o) m.Add(kv.Key, Str(kv.Value));
            return m;
        }

        public static OrderedMap<double> NumMap(object v)
        {
            var o = Obj(v); var m = new OrderedMap<double>();
            if (o != null) foreach (var kv in o) m.Add(kv.Key, Num(kv.Value));
            return m;
        }

        public static OrderedMap<int> IntMap(object v)
        {
            var o = Obj(v); var m = new OrderedMap<int>();
            if (o != null) foreach (var kv in o) m.Add(kv.Key, Int(kv.Value));
            return m;
        }

        public static OrderedMap<string[]> StrArrMap(object v)
        {
            var o = Obj(v); var m = new OrderedMap<string[]>();
            if (o != null) foreach (var kv in o) m.Add(kv.Key, StrArr(kv.Value));
            return m;
        }

        public static OrderedMap<T> Map<T>(object v, Func<object, T> conv)
        {
            var o = Obj(v); var m = new OrderedMap<T>();
            if (o != null) foreach (var kv in o) m.Add(kv.Key, conv(kv.Value));
            return m;
        }

        public static List<T> List<T>(object v, Func<object, T> conv)
        {
            var a = Arr(v); var r = new List<T>();
            if (a != null) for (int i = 0; i < a.Count; i++) r.Add(conv(a[i]));
            return r;
        }
    }

    /// <summary>삽입 순서를 지키는 문자열 키 표. 정본 객체 표(등급→값 · 시대→값)의 순서를 그대로 보존한다.</summary>
    public sealed class OrderedMap<T> : IEnumerable<KeyValuePair<string, T>>
    {
        readonly List<string> _keys = new List<string>();
        readonly Dictionary<string, T> _map = new Dictionary<string, T>();

        public int Count { get { return _keys.Count; } }
        public IReadOnlyList<string> Keys { get { return _keys; } }

        public T this[string key]
        {
            get { T v; if (!_map.TryGetValue(key, out v)) throw new KeyNotFoundException("no key '" + key + "'"); return v; }
        }

        public void Add(string key, T value) { if (!_map.ContainsKey(key)) _keys.Add(key); _map[key] = value; }
        public bool Has(string key) { return _map.ContainsKey(key); }
        public bool TryGet(string key, out T value) { return _map.TryGetValue(key, out value); }
        public T Get(string key, T def) { T v; return _map.TryGetValue(key, out v) ? v : def; }
        public string KeyAt(int index) { return _keys[index]; }
        public T ValueAt(int index) { return _map[_keys[index]]; }

        public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
        {
            for (int i = 0; i < _keys.Count; i++) yield return new KeyValuePair<string, T>(_keys[i], _map[_keys[i]]);
        }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }
}
