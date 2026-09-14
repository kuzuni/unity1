using System;
using System.Collections.Generic;
using UnityEngine;
using Forge.Game.Audio;
using Forge.Game.Ui;

namespace Forge.Game
{
    /// <summary>
    /// T120 — 호스트가 **이름 문자열**로 부르는 소리를 T30 래퍼(<see cref="Sfx"/>)에 잇는다.
    ///
    /// 왜 있나: <see cref="ForgeHost"/> 는 `PlaySfx("craft")`·`("equipSnap")`·`("anvilHit")` 를 부르지만 그 훅
    /// <c>ForgeHost.Sfx</c>(<see cref="Action{T}"/>)를 레포 어디서도 안 꽂아 **대장간 소리 셋이 통째로 무음**이었다
    /// (T119 1회차가 `check_sfx_calls.py` 를 세우다 캤다 · 소리는 PNG 에도 테스트에도 안 드러나 아무도 못 봤다).
    /// 훅을 남긴 뜻(테스트가 갈아끼운다)은 지킨다 — **비어 있을 때만** 기본값을 채우고, 이미 꽂혀 있으면 안 덮는다.
    ///
    /// 왜 `ForgeHost` 안이 아니라 여기인가: 그 파일은 T87(대장간 연출) lock 이 쥐고 있다. `OnReady` 와 공개 `Sfx` 필드가
    /// 밖에서 꽂을 길을 이미 열어 두었으므로 규약(«같은 파일이면 뒤 번호가 기다린다»)을 지키면서 **자기 파일**로 잇는다
    /// (T111 `GearDetailUi.json` · T117 `Ui/CoinBurst` 와 같은 길).
    ///
    /// 대장간 레벨업(`levelUp`)도 여기서 운다 — 정본 `forge.js` 303 `tickUpgrade` 가 레벨을 올리며 `SFX.levelUp()` 을
    /// 부르고, 클론에서 그 자리에 해당하는 신호가 <c>ForgeEngine.LevelReached</c> 다.
    ///
    /// 정본 `ui.js` 2943 은 `SFX.anvilHit(h === 2)` 로 **셋째 타격만 강하게** 운다 — 훅이 이름 하나만 받으므로 세기는
    /// 이름에 싣는다: `anvilHit`(약) · `anvilHitStrong`(강). <see cref="ForgeHost"/> 의 타격 루프가 셋째에 후자를 부른다(T120 3회차).
    /// </summary>
    public static class HostSfx
    {
        static void AnvilWeak() { Sfx.AnvilHit(false); }
        static void AnvilStrong() { Sfx.AnvilHit(true); }

        /// <summary>이름 → T30 래퍼. 원작 `SFX[name]()` 과 같은 짝이다(비교는 <see cref="StringComparer.Ordinal"/> · ROUTINE §1).</summary>
        static readonly Dictionary<string, Action> Table = new Dictionary<string, Action>(StringComparer.Ordinal)
        {
            { "craft", Sfx.Craft },
            { "equipSnap", Sfx.EquipSnap },
            { "anvilHit", AnvilWeak },
            { "anvilHitStrong", AnvilStrong },
            { "levelUp", Sfx.LevelUp },
        };

        /// <summary>지금까지 이 길로 운 횟수 · 마지막 이름(테스트·디버그).</summary>
        public static int Played { get; private set; }
        public static string LastName { get; private set; }

        /// <summary>훅 본체 — 표에 없는 이름은 조용히 흘린다(호스트가 새 이름을 붙여도 예외로 죽지 않게).</summary>
        public static void Play(string name)
        {
            Action a;
            if (name == null || !Table.TryGetValue(name, out a)) return;
            Played++;
            LastName = name;
            a();
        }

        static bool installed;
        static ForgeHost hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (installed) return;
            installed = true;
            ForgeHost.OnReady += Attach;
            Attach();
        }

        /// <summary>호스트가 서 있으면 훅을 꽂는다(한 인스턴스에 한 번 · 이미 꽂힌 훅은 안 덮는다).</summary>
        public static void Attach()
        {
            ForgeHost h = ForgeHost.Instance;
            if (h == null || ReferenceEquals(h, hooked)) return;
            hooked = h;
            if (h.Sfx == null) h.Sfx = Play;
            if (h.Engine != null) h.Engine.LevelReached += OnForgeLevel;
        }

        static void OnForgeLevel(int lv) { Sfx.LevelUp(); }
    }
}
