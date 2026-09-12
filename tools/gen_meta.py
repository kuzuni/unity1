#!/usr/bin/env python3
"""Assets/ 아래 .meta 를 텍스트로 생성한다 (유니티 에디터 없이 작업하는 워커용).

- GUID = md5(에셋 경로) 의 16진 32자 — 결정적이라 어느 워커가 만들어도 같은 값이 나온다.
- 이미 있는 .meta 는 절대 덮어쓰지 않는다 (유니티가 나중에 재직렬화한 것도 보존).
- 대응 에셋이 사라진 고아 .meta 는 지운다.
- `--check` 는 빠진/고아 .meta 가 있으면 exit 1 (CI 게이트).

사용:  python3 tools/gen_meta.py [--check]
"""
import hashlib, os, sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
ASSETS = os.path.join(ROOT, 'Assets')

def guid(rel):
    return hashlib.md5(rel.replace('\\', '/').encode('utf-8')).hexdigest()

TAIL = "  userData: \n  assetBundleName: \n  assetBundleVariant: \n"

def body(rel, is_dir):
    if is_dir:
        return "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n" + TAIL
    ext = os.path.splitext(rel)[1].lower()
    if ext == '.cs':
        return ("MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n"
                "  executionOrder: 0\n  icon: {instanceID: 0}\n" + TAIL)
    if ext == '.shader':
        # 셰이더는 ShaderImporter 다 (T225 — 이 레포의 첫 자작 셰이더). DefaultImporter 로 두면 유니티가 처음 열 때
        # 임포터를 갈아 끼우며 .meta 를 다시 쓴다 — guid 는 살아남지만 그 전까지는 «임포터가 안 맞는 에셋» 이라 경고가 난다.
        return "ShaderImporter:\n  externalObjects: {}\n  defaultTextures: []\n  nonModifiableTextures: []\n" + TAIL
    if ext == '.asmdef':
        return "AssemblyDefinitionImporter:\n  externalObjects: {}\n" + TAIL
    if ext in ('.json', '.txt', '.md', '.csv'):
        return "TextScriptImporter:\n  externalObjects: {}\n" + TAIL
    if ext in ('.ttf', '.otf'):
        name = os.path.splitext(os.path.basename(rel))[0].split('-')[0]
        return ("TrueTypeFontImporter:\n  externalObjects: {}\n  serializedVersion: 4\n  fontSize: 16\n"
                "  forceTextureCase: -2\n  characterSpacing: 0\n  characterPadding: 1\n  includeFontData: 1\n"
                f"  fontNames:\n  - {name}\n  fallbackFontReferences: []\n  customCharacters: \n"
                "  fontRenderingMode: 0\n  ascentCalculationMode: 1\n  useLegacyBoundsCalculation: 0\n"
                "  shouldRoundAdvanceValue: 1\n" + TAIL)
    if ext in ('.ogg', '.wav', '.mp3'):
        # 오디오(T28) — Vorbis 압축 · 메모리 압축 유지(loadType 1) · 배경음도 ≤ 1MB 라 스트리밍 불필요 · 2D
        return ("AudioImporter:\n  externalObjects: {}\n  serializedVersion: 7\n  defaultSettings:\n    serializedVersion: 2\n"
                "    loadType: 1\n    sampleRateSetting: 0\n    sampleRateOverride: 44100\n    compressionFormat: 1\n    quality: 0.7\n"
                "    conversionMode: 0\n    preloadAudioData: 0\n  platformSettingOverrides: {}\n  forceToMono: 0\n  normalize: 1\n"
                "  preloadAudioData: 0\n  loadInBackground: 0\n  ambisonic: 0\n  3D: 0\n" + TAIL)
    if ext == '.png':
        # 그림(T70 번개 시트) — 스프라이트 단일 모드(textureType 8 · spriteMode 1 → 스프라이트 fileID 21300000 · gen_catalog.py 가 그 값을 쓴다).
        # 무압축(textureCompression 0): 시트가 작고(≤ 1MB) WebGL 의 DXT/ETC 알파 블록이 볼트 가장자리를 뭉갠다.
        return ("TextureImporter:\n  internalIDToNameTable: []\n  externalObjects: {}\n  serializedVersion: 12\n"
                "  mipmaps:\n    mipMapMode: 0\n    enableMipMap: 0\n    sRGBTexture: 1\n    linearTexture: 0\n    fadeOut: 0\n"
                "    borderMipMap: 0\n    mipMapsPreserveCoverage: 0\n    alphaTestReferenceValue: 0.5\n"
                "    mipMapFadeDistanceStart: 1\n    mipMapFadeDistanceEnd: 3\n"
                "  bumpmap:\n    convertToNormalMap: 0\n    externalNormalMap: 0\n    heightScale: 0.25\n    normalMapFilter: 0\n"
                "  isReadable: 0\n  streamingMipmaps: 0\n  streamingMipmapsPriority: 0\n  vTOnly: 0\n  ignoreMasterTextureLimit: 0\n"
                "  grayScaleToAlpha: 0\n  generateCubemap: 6\n  cubemapConvolution: 0\n  seamlessCubemap: 0\n  textureFormat: 1\n"
                "  maxTextureSize: 2048\n"
                "  textureSettings:\n    serializedVersion: 2\n    filterMode: 1\n    aniso: 1\n    mipBias: 0\n"
                "    wrapU: 1\n    wrapV: 1\n    wrapW: 1\n"
                "  nPOTScale: 0\n  lightmap: 0\n  compressionQuality: 50\n  spriteMode: 1\n  spriteExtrude: 1\n  spriteMeshType: 1\n"
                "  alignment: 0\n  spritePivot: {x: 0.5, y: 0.5}\n  spritePixelsToUnits: 100\n"
                "  spriteBorder: {x: 0, y: 0, z: 0, w: 0}\n  spriteGenerateFallbackPhysicsShape: 1\n  alphaUsage: 1\n"
                "  alphaIsTransparency: 1\n  spriteTessellationDetail: -1\n  textureType: 8\n  textureShape: 1\n"
                "  singleChannelComponent: 0\n  flipbookRows: 1\n  flipbookColumns: 1\n  maxTextureSizeSet: 0\n"
                "  compressionQualitySet: 0\n  textureFormatSet: 0\n  ignorePngGamma: 0\n  applyGammaDecoding: 0\n  cookieLightType: 0\n"
                "  platformSettings:\n"
                + ''.join(f"  - serializedVersion: 3\n    buildTarget: {t}\n    maxTextureSize: 2048\n    resizeAlgorithm: 0\n"
                          "    textureFormat: -1\n    textureCompression: 0\n    compressionQuality: 50\n    crunchedCompression: 0\n"
                          "    allowsAlphaSplitting: 0\n    overridden: 0\n    androidETC2FallbackOverride: 0\n"
                          "    forceMaximumCompressionQuality_BC6H_BC7: 0\n"
                          for t in ('DefaultTexturePlatform', 'WebGL', 'Standalone', 'Server'))
                + "  spriteSheet:\n    serializedVersion: 2\n    sprites: []\n    outline: []\n    physicsShape: []\n    bones: []\n"
                  "    spriteID: 5e97eb03825dee720800000000000000\n    internalID: 0\n    vertices: []\n    indices: \n    edges: []\n"
                  "    weights: []\n    secondaryTextures: []\n    nameFileIdTable: {}\n"
                  "  spritePackingTag: \n  pSDRemoveMatte: 0\n  pSDShowRemoveMatteOption: 0\n" + TAIL)
    return "DefaultImporter:\n  externalObjects: {}\n" + TAIL

def main():
    check = '--check' in sys.argv
    missing, orphans = [], []
    for dirpath, dirnames, filenames in os.walk(ASSETS):
        dirnames.sort(); filenames.sort()
        entries = [(d, True) for d in dirnames] + [(f, False) for f in filenames if not f.endswith('.meta')]
        for name, is_dir in entries:
            full = os.path.join(dirpath, name)
            rel = os.path.relpath(full, ROOT).replace('\\', '/')
            meta = full + '.meta'
            if not os.path.exists(meta):
                missing.append(rel)
                if not check:
                    with open(meta, 'w', encoding='utf-8', newline='\n') as f:
                        f.write(f"fileFormatVersion: 2\nguid: {guid(rel)}\n" + body(rel, is_dir))
        for f in filenames:
            if f.endswith('.meta') and not os.path.exists(os.path.join(dirpath, f[:-5])):
                # 폴더 .meta 인데 폴더가 없는 것은 «빈 폴더» 다 (git 은 빈 폴더를 안 담는다) — 고아가 아니다.
                with open(os.path.join(dirpath, f), encoding='utf-8', errors='ignore') as fh:
                    if 'folderAsset: yes' in fh.read(400):
                        continue
                orphans.append(os.path.relpath(os.path.join(dirpath, f), ROOT))
                if not check:
                    os.remove(os.path.join(dirpath, f))
    if check:
        for m in missing: print('missing .meta:', m)
        for o in orphans: print('orphan .meta:', o)
        sys.exit(1 if (missing or orphans) else 0)
    print(f'generated {len(missing)} .meta, removed {len(orphans)} orphan(s)')

if __name__ == '__main__':
    main()
