// dotnet 검사 빌드 전용 스텁 — Unity 의 Unity.TextMeshPro(com.unity.ugui 2.0) 중 이 레포가 쓰는 표면만 서명을 맞춘다.
// Assets 에는 들어가지 않는다(유니티에서는 진짜 TMP 가 잡힌다). 새 API 를 쓰면 여기에도 같은 서명을 더한다.
using UnityEngine;
using UnityEngine.UI;

namespace TMPro
{
    public enum TextAlignmentOptions
    {
        TopLeft = 0x101, Top = 0x102, TopRight = 0x104, TopJustified = 0x108,
        Left = 0x201, Center = 0x202, Right = 0x204, Justified = 0x208,
        BottomLeft = 0x401, Bottom = 0x402, BottomRight = 0x404, BottomJustified = 0x408,
        MidlineLeft = 0x1001, Midline = 0x1002, MidlineRight = 0x1004,
    }
    [System.Flags] public enum FontStyles { Normal = 0, Bold = 1, Italic = 2, Underline = 4 }
    public class TMP_FontAsset : ScriptableObject
    {
        public Material material;
        // T207 ① — 런타임 폰트 애셋 만들기(에디터 없이 굽는 길). 진짜 TMP 의 서명 그대로다:
        //   public static TMP_FontAsset CreateFontAsset(Font font)   (기본 = sampling 90 · padding 9 · SDFAA · 1024² · Dynamic)
        // ⚠ 긴 오버로드(padding·아틀라스 크기를 직접 주는 것)는 아직 안 쓴다 — 스텁에 없는 서명을 쓰면
        //   dotnet 은 초록인데 유니티에서만 죽는다(결정 465 가 남긴 자리). 두께가 잘리면 그때 같이 넓힌다.
        public static TMP_FontAsset CreateFontAsset(Font font) { return font != null ? CreateInstance<TMP_FontAsset>() : null; }
        public bool HasCharacter(char c) { return false; }
        public bool TryAddCharacters(string chars) { return false; }
    }
    // T207 ② — 아래 서명은 **추측이 아니라 실측**이다: CI #441 의 유니티 잡이 진짜 `TMP_Text` 의
    // 공개 프로퍼티 129개를 이름:타입으로 찍어 왔고(`TmpFontProbeTests` 의 `[T207②]` 줄) 그중 이 레포가
    // 쓰는 것만 그 타입 그대로 옮겼다. 지어내지 마라 — 스텁에 없거나 다른 서명을 쓰면 dotnet 은 초록인데
    // 유니티에서만 죽고, ② 규모에서는 그것이 전 화면 파손이다(결정 457·565·587).
    public enum TextOverflowModes { Overflow = 0, Ellipsis = 1, Masking = 2, Truncate = 3, ScrollRect = 4, Page = 5, Linked = 6 }
    public enum TextWrappingModes { NoWrap = 0, Normal = 1, PreserveWhitespace = 2, PreserveWhitespaceNoWrap = 3 }
    public enum HorizontalAlignmentOptions { Left = 1, Center = 2, Right = 4, Justified = 8, Flush = 16, Geometry = 32 }
    public enum VerticalAlignmentOptions { Top = 256, Middle = 512, Bottom = 1024, Baseline = 2048, Geometry = 4096, Capline = 8192 }
    // T222 가 요청한 표면(워커 I) — «어떤 줄의 끝 글자와 다음 줄의 첫 글자가 둘 다 한글이면 낱말 한가운데서 끊긴 것» 을 재려면 이 넷이 필요하다.
    // ⚠ 이름은 내 기억이 아니라 **다음 CI 로그가 확인해 준다** — `TmpFontProbeTests` 가 이 세 타입의 실제 멤버를 찍는다(결정 573 의 방법).
    //   스텁 파일 자체는 유니티가 컴파일하지 않으므로 여기 적는 것만으로는 아무것도 안 깨진다. 깨질 수 있는 것은 **Assets 쪽에서 쓰는 순간**이고,
    //   그러니 자를 쓰는 워커는 그 로그 줄을 먼저 보고 쓰라(안 맞으면 이 파일만 고치면 된다).
    public struct TMP_CharacterInfo { public char character; public int index; public bool isVisible; }
    public struct TMP_LineInfo { public int firstCharacterIndex, lastCharacterIndex, firstVisibleCharacterIndex, lastVisibleCharacterIndex, characterCount; }
    public class TMP_TextInfo
    {
        public int characterCount, lineCount, pageCount, wordCount;
        public TMP_CharacterInfo[] characterInfo = new TMP_CharacterInfo[0];
        public TMP_LineInfo[] lineInfo = new TMP_LineInfo[0];
    }

    public abstract class TMP_Text : MaskableGraphic
    {
        public virtual string text { get; set; }
        public float fontSize { get; set; }                       // ⚠ Single 이다 — `int x = t.fontSize` 는 안 된다
        public TextAlignmentOptions alignment { get; set; }        // ⚠ TextAnchor 아님
        public HorizontalAlignmentOptions horizontalAlignment { get; set; }
        public VerticalAlignmentOptions verticalAlignment { get; set; }
        public bool enableAutoSizing { get; set; }
        public float fontSizeMin { get; set; }
        public float fontSizeMax { get; set; }
        public FontStyles fontStyle { get; set; }
        public bool richText { get; set; }
        public Material fontSharedMaterial { get; set; }
        public TMP_FontAsset font { get; set; }
        public TextOverflowModes overflowMode { get; set; }
        public TextWrappingModes textWrappingMode { get; set; }
        public bool enableWordWrapping { get; set; }               // 아직 있다(구식이지만 살아 있는 이름)
        public float lineSpacing { get; set; }
        public float characterSpacing { get; set; }
        public float paragraphSpacing { get; set; }
        public Vector4 margin { get; set; }
        public float outlineWidth { get; set; }                    // 주인이 말한 «진짜 머티리얼 아웃라인» 이 이 둘이다
        public Color outlineColor { get; set; }
        public int maxVisibleCharacters { get; set; }
        public TMP_TextInfo textInfo { get; }
        public float preferredWidth { get; }                       // ⚠ TMP 에도 있다 — 고칠 필요가 없던 자리(결정 587)
        public float preferredHeight { get; }
        public bool isTextTruncated { get; }
        // T502 — 월드(3D) 글자가 쓰는 하나: true 면 글자 크기 1 = 로컬 1 단위(uGUI 와 같은 셈) · false(3D 기본)면 ×0.1. 진짜 TMP_Text 의 공개 프로퍼티다.
        public bool isOrthographic { get; set; }
        public void ForceMeshUpdate(bool ignoreActiveState = false, bool forceTextReparsing = false) { }
        public Vector2 GetPreferredValues() { return Vector2.zero; }
        public Vector2 GetPreferredValues(float width, float height) { return Vector2.zero; }
    }
    public class TextMeshProUGUI : TMP_Text { }
    // T502 — 월드 공간 글자(데미지 팝 · 발밑 숫자). MeshRenderer 로 그리므로 스프라이트와 같은 정렬 층·순서를 갖는다 — 진짜 TextMeshPro 의 공개 프로퍼티 둘만 옮겼다.
    public class TextMeshPro : TMP_Text
    {
        public int sortingLayerID { get; set; }
        public int sortingOrder { get; set; }
    }
    // 프리팹 입력칸 — UiKit.Adopt 가 이것을 떼고 uGUI InputField 로 갈아 끼운다(T96-profile 2단계)
    public class TMP_InputField : Selectable
    {
        public enum LineType { SingleLine = 0, MultiLineSubmit = 1, MultiLineNewline = 2 }
        public TMP_Text textComponent { get; set; }
        public Graphic placeholder { get; set; }
        public int characterLimit { get; set; }
        public LineType lineType { get; set; }
        // T207 ② — 이제 조각의 입력칸을 «그대로 쓴다»(uGUI InputField 로 다시 세우지 않는다) → 우리 코드가 이 표면을 직접 부른다.
        // 이름은 uGUI InputField 와 같다(TMP 가 그 API 를 그대로 본떴다).
        public string text { get; set; }
        public class OnChangeEvent : UnityEngine.Events.UnityEvent<string> { }
        public OnChangeEvent onValueChanged { get; } = new OnChangeEvent();
        public void ActivateInputField() { }
    }
}
