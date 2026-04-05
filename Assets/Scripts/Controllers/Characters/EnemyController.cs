using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyType  { CHOMPER = 0, GRENADIER, SPITTER, MAX }
public enum EnemyState { IDLE, WALK, RUN, ATTACK, HIT }

/// <summary>
/// 적 AI. NavMeshAgent로 플레이어 추적, 거리에 따른 상태 머신.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    [Header("타입 (Prefab마다 설정)")]
    [SerializeField] private EnemyType _enemyType = EnemyType.CHOMPER;
    public EnemyType EnemyType => _enemyType;

    [Header("공격 콜라이더")]
    [SerializeField] private BoxCollider _atkCollider;

    [Header("UI")]
    [SerializeField] private UIFollow3D _uiFollower;
    [SerializeField] private ObjectUI   _objectUI;
    public ObjectUI UI => _objectUI;

    private JsonEnemyData   _data;
    public  JsonEnemyData   Data => _data;

    private int _curHp;

    private NavMeshAgent     _nma;
    private Rigidbody        _rigid;
    private EnemyAnimManager _animManager;
    private RagdollEvent     _ragdoll;
    private PlayerController _target;

    private EnemyState _state = EnemyState.IDLE;
    public  EnemyState State  => _state;

    private float _dmgTimer   = 0f;
    private const float DMG_INTERVAL = 1f;   // 플레이어에게 데미지 주는 간격(초)

    // ── 끼임 방지 ─────────────────────────────────────────────────
    private float   _pathRefreshTimer = 0f;
    private const float PATH_REFRESH  = 0.35f;  // SetDestination 호출 간격(초)

    private Vector3 _lastPos;
    private float   _stuckTimer       = 0f;
    private const float STUCK_TIMEOUT = 2.0f;   // 이 시간 동안 못 움직이면 끼임으로 판단
    private const float STUCK_THRESHOLD = 0.05f; // 이동 최소 거리

    // ──────────────────────────────────────────────────────────────

    void Awake()
    {
        _nma        = GetComponent<NavMeshAgent>();
        _rigid      = GetComponent<Rigidbody>();
        _animManager= GetComponent<EnemyAnimManager>();
        _ragdoll    = GetComponent<RagdollEvent>();

        var allData = JsonDataManager.Instance.enemyData;
        _data = (allData != null && allData.Length > (int)_enemyType)
              ? allData[(int)_enemyType]
              : new JsonEnemyData();
        _curHp = _data.hp;

        if (_objectUI != null)
            _objectUI.InitHp(_curHp);

        SetAtkCollider(false);
    }

    void Start()
    {
        _target  = FindObjectOfType<PlayerController>();
        _lastPos = transform.position;

        // 몹끼리 밀림 우선순위 랜덤화 — 같은 값이면 서로 밀며 끼임
        _nma.avoidancePriority  = UnityEngine.Random.Range(30, 70);
        _nma.autoBraking        = false;

        if (_uiFollower != null)
            _uiFollower.SetTarget(transform);
    }

    void Update()
    {
        if (_target == null) return;
        TrackPlayer();
        CheckStuck();
        CheckDeath();
    }

    void FixedUpdate()
    {
        // NavMesh가 제어하므로 물리 속도는 0으로 고정
        _rigid.velocity        = Vector3.zero;
        _rigid.angularVelocity = Vector3.zero;
    }

    // ── 추적 상태 머신 ────────────────────────────────────────────

    void TrackPlayer()
    {
        float dist = Vector3.Distance(transform.position, _target.transform.position);

        _pathRefreshTimer -= Time.deltaTime;

        if (dist < _data.pursutied_distance)
        {
            SetState(EnemyState.ATTACK);

            // 1초 간격으로 플레이어에게 데미지
            _dmgTimer -= Time.deltaTime;
            if (_dmgTimer <= 0f)
            {
                _dmgTimer = DMG_INTERVAL;
                _target?.TakeDamage(_data.atk);
            }
        }
        else if (dist < _data.near_distance)
        {
            _dmgTimer = 0f;
            _nma.speed = _data.fast_speed * 0.8f;
            TrySetDestination(_target.transform.position);
            SetState(EnemyState.RUN);
        }
        else
        {
            _dmgTimer = 0f;
            _nma.speed = _data.slow_speed * 0.8f;
            TrySetDestination(_target.transform.position);
            SetState(EnemyState.WALK);
        }
    }

    /// <summary>
    /// 0.35초 간격으로만 SetDestination 호출.
    /// NavMesh 위의 가장 가까운 점을 샘플링해서 유효한 목적지만 설정.
    /// </summary>
    void TrySetDestination(Vector3 target)
    {
        if (_pathRefreshTimer > 0f) return;
        _pathRefreshTimer = PATH_REFRESH;

        // 목적지가 NavMesh 위에 있는지 확인 (반경 2m 이내)
        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            _nma.SetDestination(hit.position);
    }

    /// <summary>
    /// 일정 시간 이상 거의 안 움직이면 끼인 것으로 판단하고 NavMesh 위로 순간이동.
    /// </summary>
    void CheckStuck()
    {
        // 공격 중이거나 NavMeshAgent가 비활성이면 검사 생략
        if (_state == EnemyState.ATTACK || !_nma.enabled || !_nma.isOnNavMesh) return;

        float moved = Vector3.Distance(transform.position, _lastPos);

        if (moved < STUCK_THRESHOLD)
        {
            _stuckTimer += Time.deltaTime;
            if (_stuckTimer >= STUCK_TIMEOUT)
            {
                _stuckTimer = 0f;
                UnstuckSelf();
            }
        }
        else
        {
            _stuckTimer = 0f;
            _lastPos    = transform.position;
        }
    }

    /// <summary>
    /// 플레이어 방향으로 조금 벗어난 NavMesh 위 위치로 워프.
    /// </summary>
    void UnstuckSelf()
    {
        if (_target == null) return;

        // 플레이어 → 나 방향으로 조금 떨어진 NavMesh 위 점 탐색
        Vector3 dir      = (transform.position - _target.transform.position).normalized;
        Vector3 tryPos   = transform.position + dir * 1.5f
                         + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0,
                                       UnityEngine.Random.Range(-1f, 1f));

        if (NavMesh.SamplePosition(tryPos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            _nma.Warp(hit.position);
            _pathRefreshTimer = 0f;  // 즉시 경로 재계산
        }
    }

    void SetState(EnemyState next)
    {
        if (_state == next || _state == EnemyState.HIT) return;

        switch (next)
        {
            case EnemyState.IDLE:
                _animManager?.SetNearBase(true);
                _animManager?.StopPursuit();
                break;
            case EnemyState.WALK:
                _animManager?.SetNearBase(false);
                _animManager?.StopPursuit();
                break;
            case EnemyState.RUN:
                _animManager?.StartPursuit();
                break;
            case EnemyState.ATTACK:
                _animManager?.TriggerAttack();
                StartCoroutine(DoAttack());
                break;
            case EnemyState.HIT:
                _animManager?.TriggerHit();
                break;
        }

        _state = next;
    }

    IEnumerator DoAttack()
    {
        yield return new WaitForSeconds(0.1f);
        SetAtkCollider(true);
        yield return new WaitForSeconds(0.1f);
        SetAtkCollider(false);
    }

    void SetAtkCollider(bool on)
    {
        if (_atkCollider != null)
            _atkCollider.enabled = on;
    }

    // ── 피격 ──────────────────────────────────────────────────────

    void OnCollisionEnter(Collision col)
    {
        if (!col.collider.CompareTag("Weapon")) return;

        SetState(EnemyState.HIT);
        var weapon = col.transform.GetComponent<Weapon>();
        if (weapon != null)
        {
            TakeDamage(weapon.Atk);
            weapon.ComboAddCallback?.Invoke();
        }

        Camera.main?.GetComponent<CameraShake>()?.ShakeCam();
    }

    /// <summary>PlayerController의 OverlapSphere 공격에서 호출.</summary>
    public void ReceiveDamage(int rawDmg)
    {
        if (_curHp <= 0) return;
        SetState(EnemyState.HIT);
        TakeDamage(rawDmg);
        StartCoroutine(HitFlash());
    }

    void TakeDamage(int rawDmg)
    {
        int dmg = Mathf.Max(1, rawDmg - Mathf.RoundToInt(_data.def * 0.2f)
                               + UnityEngine.Random.Range(-3, 3));
        _curHp -= dmg;

        if (_objectUI != null)
        {
            _objectUI.SetCurHp(_curHp);
            _objectUI.SetDamageText(dmg);
        }
    }

    IEnumerator HitFlash()
    {
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer == null) yield break;

        var mat = renderer.material;
        Color orig = mat.color;
        mat.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        if (mat != null) mat.color = orig;

        // HIT 상태 해제
        yield return new WaitForSeconds(0.2f);
        if (_state == EnemyState.HIT) _state = EnemyState.IDLE;
    }

    void CheckDeath()
    {
        if (_curHp > 0) return;

        StageManager.Instance?.OnEnemyKilled();

        if (_objectUI != null) _objectUI.DestroyUI();

        if (_ragdoll != null) _ragdoll.Replace();
        else                  Destroy(gameObject);
    }
}
