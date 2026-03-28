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
        _target = FindObjectOfType<PlayerController>();
        if (_uiFollower != null)
            _uiFollower.SetTarget(transform);
    }

    void Update()
    {
        if (_target == null) return;
        TrackPlayer();
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

        if (dist < _data.pursutied_distance)
        {
            SetState(EnemyState.ATTACK);
        }
        else if (dist < _data.near_distance)
        {
            _nma.speed = _data.fast_speed;
            _nma.SetDestination(_target.transform.position);
            SetState(EnemyState.RUN);
        }
        else if (dist < _data.far_distance)
        {
            _nma.speed = _data.slow_speed;
            _nma.SetDestination(_target.transform.position);
            SetState(EnemyState.WALK);
        }
        else
        {
            _nma.ResetPath();
            SetState(EnemyState.IDLE);
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
