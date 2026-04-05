using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 자동 모드일 때 NavMeshAgent로 가장 가까운 몬스터를 추적하고 공격.
/// PlayerController와 같은 오브젝트에 부착.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class PlayerAutoController : MonoBehaviour
{
    private PlayerController _player;
    private NavMeshAgent     _nma;

    void Awake()
    {
        _player = GetComponent<PlayerController>();
        _nma    = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (!_player.IsAuto || !_nma.enabled) return;
        TrackNearest();
    }

    public void SetAuto(bool on)
    {
        if (_player == null) return;
        _player.SetAuto(on);
        if (!on && _nma != null && _nma.enabled)
            _nma.ResetPath();
    }

    void TrackNearest()
    {
        var target = SpawnManager.Instance?.GetNearestMonster(transform.position);

        if (target == null)
        {
            _player.SetAnimState(PlayerState.IDLE);
            _nma.ResetPath();
            return;
        }

        float dist    = Vector3.Distance(transform.position, target.position);
        float atkRange = _player.AtkRange;

        // 항상 타겟 방향으로 회전
        transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));

        if (dist > atkRange * 2.5f)
        {
            // 멀리 있으면 전력질주
            _nma.speed = _player.Data.speed;
            _nma.SetDestination(target.position);
            _player.SetAnimState(PlayerState.RUN);
        }
        else if (dist > atkRange)
        {
            // 가까이 있으면 걸어서 접근
            _nma.speed = _player.Data.speed * 0.5f;
            _nma.SetDestination(target.position);
            _player.SetAnimState(PlayerState.WALK);
        }
        else
        {
            // 공격 범위 내 — 멈추고 공격
            _nma.ResetPath();
            _player.TryAttack();
        }
    }
}
