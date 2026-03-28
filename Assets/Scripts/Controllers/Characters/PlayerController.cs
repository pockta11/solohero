using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum PlayerState { IDLE, WALK, RUN, ATTACK, SKILL, HIT, DEAD }

/// <summary>
/// 플레이어 이동/공격/스킬/피격/사망.
/// 공격: Space(기본) / J(스킬) — OverlapSphere 범위 판정.
/// </summary>
[RequireComponent(typeof(PlayerAutoController))]
public class PlayerController : MonoBehaviour
{
    // ── 스탯 ──────────────────────────────────────────────────────
    private JsonPlayerData _data;
    public  JsonPlayerData Data => _data;

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
    [SerializeField] float _atkCooldown   = 0.6f;   // 기본 공격 쿨타임
    [SerializeField] float _skillRange    = 3.5f;   // 스킬 범위
    [SerializeField] int   _skillDmgMult  = 3;      // 스킬 배율

    private float _atkCoolRemain   = 0f;
    private float _skillCoolRemain = 0f;
    private bool  _isDead          = false;

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

    // ──────────────────────────────────────────────────────────────

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _data  = JsonDataManager.Instance.playerData ?? new JsonPlayerData();
        _curHp = _data.hp;
        _curSp = _data.sp;
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

        Vector3 moveVec = new Vector3(h, 0f, v);
        float   dist    = moveVec.sqrMagnitude;

        if (moveVec != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(moveVec.normalized);

        transform.position += moveVec.normalized * (_data.speed * dist) * Time.deltaTime;
        UpdateMoveAnim(dist);
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

        int dmg = Mathf.Max(1, rawDmg - Mathf.RoundToInt(_data.def * 0.2f)
                               + UnityEngine.Random.Range(-3, 3));
        AddHp(-dmg);

        HUDManager.Instance?.ShowDamageFlash();
        Camera.main?.GetComponent<CameraShake>()?.ShakeCam();

        if (_animator != null) _animator.SetTrigger("Hit");
    }

    void OnCollisionEnter(Collision col)
    {
        if (!col.transform.CompareTag("Enemy")) return;
        var enemy = col.transform.GetComponentInParent<EnemyController>();
        if (enemy != null) TakeDamage(enemy.Data.atk);
    }

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

    public void SetAnimState(PlayerState next, float speed = 0f) { }  // AutoController용

    // ── 자동 모드 ─────────────────────────────────────────────────

    void ToggleAuto()
    {
        _isAuto = !_isAuto;
        if (_imgAutoOn != null) _imgAutoOn.gameObject.SetActive(_isAuto);
    }

    // ── 장비 보너스 ───────────────────────────────────────────────

    public void ApplyEquipmentBonuses(int hpBonus, int atkBonus, int defBonus)
    {
        _data.hp  += hpBonus;
        _data.atk += atkBonus;
        _data.def += defBonus;
        _curHp = _data.hp;
        HUDManager.Instance?.UpdateHP(_curHp, _data.hp);
    }

    // ── 디버그 기즈모 ─────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + transform.forward * (_atkRange * 0.5f), _atkRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _skillRange > 0 ? _skillRange : 3.5f);
    }
#endif
}
