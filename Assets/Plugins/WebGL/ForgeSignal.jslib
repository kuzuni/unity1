// T8 — WebGL 템플릿(T26 `Assets/WebGLTemplates/Forge/index.html`)의 신호 계약: window.forgeSignal(name) → window.__forgeSignals.
// 전투 씬이 전투 진입 프레임에 'battle' 을 보낸다(배포 스모크 tools/webgl_smoke.js 가 이것을 기다린다).
mergeInto(LibraryManager.library, {
  ForgeSignal: function (ptr) {
    var name = UTF8ToString(ptr);
    try { if (typeof window !== 'undefined' && typeof window.forgeSignal === 'function') window.forgeSignal(name); } catch (e) { console.warn('forgeSignal', e); }
  }
});
