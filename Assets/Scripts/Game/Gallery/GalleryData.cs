using System.IO;
using UnityEngine;
using Forge.Core.Data;

namespace Forge.Game.Gallery
{
    /// <summary>
    /// `StreamingAssets/data/*.json`(T2) 을 파일로 읽어 <see cref="GameData"/> 로 세운다 — 에디터·스탠드얼론용(T5 도감·PlayMode).
    /// WebGL/Android 는 StreamingAssets 를 File 로 못 읽는다 — 그 경로(UnityWebRequest)는 부팅 로더(T8/T13)가 맡는다.
    /// </summary>
    public static class GalleryData
    {
        public const string DataFolder = "data";
        static GameData cached;

        public static string DataDir { get { return Path.Combine(Application.streamingAssetsPath, DataFolder); } }

        public static GameData Load(bool force = false)
        {
            if (cached != null && !force) return cached;
            cached = GameData.LoadDirectory(DataDir);
            return cached;
        }
    }
}
