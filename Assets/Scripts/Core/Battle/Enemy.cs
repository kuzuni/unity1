namespace Forge.Core.Battle
{
    /// <summary>원작 `spawnWave` 가 만드는 적 레코드 그대로. 좌표는 원작 논리 x(오른쪽 +x 에서 걸어 들어온다) — 유니티 z 반전은 장면(T8) 몫.</summary>
    public sealed class Enemy
    {
        public int Id;
        public Big Hp, MaxHp, Atk;
        public double X;
        /// <summary>깊이 레인(`restackMelee`).</summary>
        public double Z;
        /// <summary>정지 자리 — 대열이 아직 안 짜였으면 null → `MeleeX`.</summary>
        public double? StopX;
        public double Speed, AtkTimer;
        public bool IsBoss, Alive;
    }
}
