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
        if (!_player.IsAuto) return;
        TrackNearest();
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

        transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
        float dist = Vector3.Distance(transform.position, target.position);

        if (dist > _player.MOVE_SPEED_RUN_PARAM * 10f)
        {
            _player.SetAnimState(PlayerState.RUN,  _player.MOVE_SPEED_RUN_PARAM);
            _nma.speed = _player.Data.speed * _player.MOVE_SPEED_RUN_PARAM;
            _nma.SetDestination(target.position);
        }
        else if (dist > _player.MOVE_SPEED_WALK_PARAM * 5f)
        {
            _player.SetAnimState(PlayerState.WALK, _player.MOVE_SPEED_WALK_PARAM);
            _nma.speed = _player.Data.speed * _player.MOVE_SPEED_WALK_PARAM;
            _nma.SetDestination(target.position);
        }
        else
        {
            _nma.ResetPath();
            _player.OnAttack();
        }
    }
}
