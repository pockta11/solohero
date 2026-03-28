using UnityEngine;

/// <summary>
/// 적 Animator 래퍼. EnemyController에서 호출.
/// 실제 파라미터 이름은 사용할 애니메이터 컨트롤러에 맞게 조정할 것.
/// </summary>
[RequireComponent(typeof(Animator))]
public class EnemyAnimManager : MonoBehaviour
{
    private Animator _animator;
    public  Animator Animator => _animator;

    public static readonly int hashInPursuit = Animator.StringToHash("InPursuit");
    public static readonly int hashAttack    = Animator.StringToHash("Attack");
    public static readonly int hashHit       = Animator.StringToHash("Hit");
    public static readonly int hashThrown    = Animator.StringToHash("Thrown");
    public static readonly int hashNearBase  = Animator.StringToHash("NearBase");

    void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void StartPursuit()  => _animator.SetBool(hashInPursuit, true);
    public void StopPursuit()   => _animator.SetBool(hashInPursuit, false);
    public void SetNearBase(bool v) => _animator.SetBool(hashNearBase, v);
    public void TriggerAttack() => _animator.SetTrigger(hashAttack);
    public void TriggerHit()    => _animator.SetTrigger(hashHit);
    public void TriggerDeath()  => _animator.SetTrigger(hashThrown);
}
