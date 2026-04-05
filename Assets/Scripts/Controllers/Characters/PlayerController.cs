using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public enum PlayerState { IDLE, WALK, RUN, ATTACK, SKILL, HIT, DEAD }

/// <summary>
/// 플레이어 이동/공격/스킬/피격/사망.
/// 공격: Space(기본) / J(스킬) — OverlapSphere 범위 판정.
/// </summary>
[RequireComponent(typeof(PlayerAutoController))]
[RequireComponent(typeof(NavMeshAgent))]
public class PlayerController : MonoBehaviour
{
    // ── 스탯 ──────────────────────────────────────────────────────
    private JsonPlayerData _data;
    public  JsonPlayerData Data => _data;

    // JSON에서 읽은 기본값 (불변) — 장비/업그레이드 보너스 계산 기준
    private int   _baseHp;
    private int   _baseAtk;
    private int   _baseDef;
    private float _baseSpd;

    private int _curHp;
    private int _curSp;
    private int _combo;
    public  int CurHp => _curHp;

    // ── 이동 파라미터 ─────────────────────────────────────────────
    public readonly float MOVE_SPEED_RUN_PARAM  = 0.3f;
    public readonly float MOVE_SPEED_WALK_PARAM = 0.05f;

    // ── 공격 설정 ─────────────────────────────────────────────────
    [Header("공격")]
    [SerializeField] float _atkRange      = 1.8f;   // 기본 공격 범위
    [SerializeField] float _atkAngle      = 60f;    // 기본 공격 좌우 허용 각도 (전방 ±60°)
    [SerializeField] float _atkCooldown   = 0.6f;   // 기본 공격 쿨타임
    [SerializeField] float _skillRange    = 3.5f;   // 스킬 범위
    [SerializeField] int   _skillDmgMult  = 3;      // 스킬 배율

    public float AtkRange => _atkRange;

    private float _atkCoolRemain   = 0f;
    private float _skillCoolRemain = 0f;
    private float _dmgCoolRemain   = 0f;   // 전역 피격 쿨타임 (몹 수에 상관없이 초당 1회)
    private const float DMG_COOLDOWN = 1f;
    private bool  _isDead          = false;

    // ── 기본 스탯 소스 ────────────────────────────────────────────
    [Header("기본 스탯 (비워두면 JSON 사용)")]
    [SerializeField] PlayerStatsSO _statsSO;

    // ── UI ────────────────────────────────────────────────────────
    [Header("UI")]
    [SerializeField] VirtualJoystick _joystick;
    [SerializeField] Button          _btnAuto;
    [SerializeField] Image           _imgAutoOn;

    // ── 애니메이션 ────────────────────────────────────────────────
    private Animator    _animator;
    private PlayerState _state = PlayerState.IDLE;

    // ── 자동 모드 ─────────────────────────────────────────────────
    private bool _isAuto = false;
    public  bool IsAuto  => _isAuto;

    private NavMeshAgent _nma;

    // ──────────────────────────────────────────────────────────────

    void Awake()
    {
        // Prefer child animator that has an avatar (the actual character rig).
        // Root animator has no avatar so animations would not play visually.
        _animator = null;
        foreach (var anim in GetComponentsInChildren<Animator>())
        {
            if (anim.avatar != null) { _animator = anim; break; }
        }
        if (_animator == null) _animator = GetComponent<Animator>();
        _data    = _statsSO != null
                   ? _statsSO.ToJsonPlayerData()
                   : JsonDataManager.Instance?.playerData ?? new JsonPlayerData();
        _baseHp  = _data.hp;
        _baseAtk = _data.atk;
        _baseDef = _data.def;
        _baseSpd = _data.speed;
        _curHp   = _baseHp;
        _curSp   = _data.sp;

        _nma                = GetComponent<NavMeshAgent>();
        _nma.updateRotation = false;  // 회전은 PlayerController가 직접 제어
        _nma.updateUpAxis   = false;
        _nma.enabled        = false;  // 수동 모드 기본값 — 자동 모드 ON 시에만 활성화

        // 적 NavMeshAgent가 플레이어 콜라이더와 겹칠 때 물리 분리력으로 밀리는 현상 방지.
        // transform.position으로 이동하므로 X/Z 물리 이동을 완전 차단.
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.constraints = RigidbodyConstraints.FreezePositionX |
                             RigidbodyConstraints.FreezePositionY |
                             RigidbodyConstraints.FreezePositionZ |
                             RigidbodyConstraints.FreezeRotation;
    }

    void Start()
    {
        if (_btnAuto != null)
            _btnAuto.onClick.AddListener(ToggleAuto);

        HUDManager.Instance?.UpdateHP(_curHp, _data.hp);
    }

    void Update()
    {
        if (_isDead) return;

        _atkCoolRemain   -= Time.deltaTime;
        _skillCoolRemain -= Time.deltaTime;
        _dmgCoolRemain   -= Time.deltaTime;

        if (!_isAuto)
        {
            HandleMovement();
            HandleAttackInput();
        }
    }

    void FixedUpdate()
    {
        if (_isDead) return;
        AddHp(1);   // 자연 회복
        AddSp(2);
    }

    // ── 이동 ──────────────────────────────────────────────────────

    void HandleMovement()
    {
        float h = _joystick != null ? _joystick.Horizontal : 0f;
        float v = _joystick != null ? _joystick.Vertical   : 0f;

#if UNITY_EDITOR
        float kh = 0f, kv = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  kh = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) kh =  1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    kv =  1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  kv = -1f;
        if (kh != 0f || kv != 0f) { h = kh; v = kv; }
#endif

        Vector3 moveVec  = new Vector3(h, 0f, v);
        float   magnitude = moveVec.magnitude;  // sqrMagnitude 대신 magnitude 사용

        if (moveVec != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(moveVec.normalized);

        if (magnitude > 0.01f)
            transform.position += moveVec.normalized * _data.speed * Time.deltaTime;

        UpdateMoveAnim(magnitude);
    }

    // ── 공격 입력 (키보드) ────────────────────────────────────────

    void HandleAttackInput()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.J))
        {
            bool isSkill = Input.GetKeyDown(KeyCode.J);
            if (isSkill) TrySkill();
            else         TryAttack();
        }
#endif
    }

    // ── 기본 공격 ─────────────────────────────────────────────────

    public void TryAttack()
    {
        if (_atkCoolRemain > 0f || _isDead) return;
        _atkCoolRemain = _atkCooldown;
        StartCoroutine(DoAttack());
    }

    IEnumerator DoAttack()
    {
        _state = PlayerState.ATTACK;
        if (_animator != null) _animator.SetTrigger("Attack");

        yield return new WaitForSeconds(0.15f);  // 예비 딜레이

        // 정면 범위 내 적 타격
        var hits = Physics.OverlapSphere(
            transform.position + transform.forward * (_atkRange * 0.5f),
            _atkRange,
            ~LayerMask.GetMask("Ignore Raycast"));

        int dmg = Mathf.Max(1, _data.atk + UnityEngine.Random.Range(-3, 6));

        foreach (var hit in hits)
        {
            var enemy = hit.GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                // 전방 ±_atkAngle 범위 밖이면 타격 제외
                Vector3 toEnemy = hit.transform.position - transform.position;
                toEnemy.y = 0f;
                if (toEnemy != Vector3.zero &&
                    Vector3.Angle(transform.forward, toEnemy.normalized) > _atkAngle)
                    continue;
                enemy.ReceiveDamage(dmg);
                _combo++;
                HUDManager.Instance?.ShowCombo(_combo);
                Camera.main?.GetComponent<CameraShake>()?.ShakeCam();
            }
        }

        yield return new WaitForSeconds(0.2f);
        if (_state == PlayerState.ATTACK) _state = PlayerState.IDLE;
    }

    // ── 스킬 ──────────────────────────────────────────────────────

    public void TrySkill()
    {
        if (_skillCoolRemain > 0f || _curSp < 50 || _isDead) return;
        _skillCoolRemain = _data.skill_cooltime;
        AddSp(-50);
        StartCoroutine(DoSkill());
    }

    IEnumerator DoSkill()
    {
        _state = PlayerState.SKILL;
        if (_animator != null) _animator.SetTrigger("Skill1");

        yield return new WaitForSeconds(0.2f);

        // 넓은 범위 전체 타격
        var hits = Physics.OverlapSphere(transform.position, _skillRange,
            ~LayerMask.GetMask("Ignore Raycast"));

        int dmg = Mathf.Max(1, _data.atk * _skillDmgMult + UnityEngine.Random.Range(-5, 10));

        foreach (var hit in hits)
        {
            var enemy = hit.GetComponentInParent<EnemyController>();
            if (enemy != null)
                enemy.ReceiveDamage(dmg);
        }

        Camera.main?.GetComponent<CameraShake>()?.ShakeCam();

        yield return new WaitForSeconds(0.3f);
        if (_state == PlayerState.SKILL) _state = PlayerState.IDLE;
    }

    // ── 피격 ──────────────────────────────────────────────────────

    public void TakeDamage(int rawDmg)
    {
        if (_isDead) return;
        if (_dmgCoolRemain > 0f) return;   // 전역 피격 쿨타임 — 몹 수와 무관하게 초당 1회
        _dmgCoolRemain = DMG_COOLDOWN;

        int dmg = Mathf.Max(1, rawDmg - Mathf.RoundToInt(_data.def * 0.2f)
                               + UnityEngine.Random.Range(-3, 3));
        AddHp(-dmg);

        HUDManager.Instance?.ShowDamageFlash();
        Camera.main?.GetComponent<CameraShake>()?.ShakeCam();

        // 공격/스킬 모션 중에는 피격 모션 생략 — 공격 우선
        if (_state != PlayerState.ATTACK && _state != PlayerState.SKILL)
            if (_animator != null) _animator.SetTrigger("Hit");
    }

    // 데미지는 EnemyController의 1초 타이머에서 TakeDamage()를 직접 호출하므로
    // OnCollisionEnter 충돌 이벤트 기반 데미지는 사용하지 않음

    // ── HP / SP ───────────────────────────────────────────────────

    void AddHp(int value)
    {
        if (_isDead) return;
        _curHp = Mathf.Clamp(_curHp + value, 0, _data.hp);
        HUDManager.Instance?.UpdateHP(_curHp, _data.hp);

        if (_curHp <= 0) Die();
    }

    void AddSp(int value)
    {
        _curSp = Mathf.Clamp(_curSp + value, 0, _data.sp);
        HUDManager.Instance?.UpdateSP(_curSp, _data.sp);
    }

    // ── 사망 ──────────────────────────────────────────────────────

    void Die()
    {
        _isDead = true;
        _state  = PlayerState.DEAD;
        if (_animator != null) _animator.SetTrigger("Die");
        HUDManager.Instance?.ShowDeadUI();
        StartCoroutine(Revive(3f));
    }

    IEnumerator Revive(float delay)
    {
        yield return new WaitForSeconds(delay);
        _isDead = false;
        _curHp  = _data.hp / 2;
        _state  = PlayerState.IDLE;
        HUDManager.Instance?.UpdateHP(_curHp, _data.hp);
        HUDManager.Instance?.HideDeadUI();
        Debug.Log("[Player] 부활");
    }

    // ── 애니메이션 ────────────────────────────────────────────────

    void UpdateMoveAnim(float dist)
    {
        if (_animator == null) return;
        _animator.SetFloat("MoveSpeed", dist);
    }

    public void SetAnimState(PlayerState next, float speed = 0f)
    {
        if (_animator == null) return;
        switch (next)
        {
            case PlayerState.IDLE:
                _animator.SetFloat("MoveSpeed", 0f);
                break;
            case PlayerState.WALK:
                _animator.SetFloat("MoveSpeed", 0.1f);
                break;
            case PlayerState.RUN:
                _animator.SetFloat("MoveSpeed", 0.5f);
                break;
        }
    }

    // ── 자동 모드 ─────────────────────────────────────────────────

    void ToggleAuto() => SetAuto(!_isAuto);

    public void SetAuto(bool on)
    {
        _isAuto      = on;
        _nma.enabled = on;
        if (_imgAutoOn != null) _imgAutoOn.gameObject.SetActive(on);
    }

    // ── 장비 보너스 ───────────────────────────────────────────────

    /// <summary>
    /// 장비+업그레이드 보너스를 한꺼번에 적용. 기본 스탯 기준으로 절대값 재계산.
    /// 중복 호출해도 동일 결과 보장.
    /// </summary>
    public void ApplyEquipmentBonuses(int hpBonus, int atkBonus, int defBonus,
                                      float spdBonus = 0f)
    {
        _data.hp    = _baseHp  + hpBonus;
        _data.atk   = _baseAtk + atkBonus;
        _data.def   = _baseDef + defBonus;
        _data.speed = _baseSpd + spdBonus;
        _curHp = Mathf.Min(_curHp, _data.hp);
        HUDManager.Instance?.UpdateHP(_curHp, _data.hp);
    }

    // ── 디버그 기즈모 ─────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 공격 범위 구
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + transform.forward * (_atkRange * 0.5f), _atkRange);

        // 공격 각도 부채꼴 (좌우 ±_atkAngle)
        float angle = _atkAngle > 0 ? _atkAngle : 60f;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Vector3 leftDir  = Quaternion.Euler(0, -angle, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0,  angle, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftDir  * _atkRange * 1.5f);
        Gizmos.DrawRay(transform.position, rightDir * _atkRange * 1.5f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _skillRange > 0 ? _skillRange : 3.5f);
    }
#endif
}
