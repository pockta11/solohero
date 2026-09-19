/// <summary>
/// 피해를 받을 수 있는 모든 오브젝트의 표준 인터페이스.
/// 플레이어·몬스터·구조물 등 피해 로직을 통일.
/// </summary>
public interface IDamageable
{
    bool IsAlive { get; }
    void TakeDamage(int amount);
}
