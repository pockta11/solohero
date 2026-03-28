using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public enum PlayerState { IDLE, WALK, RUN, ATTACK, SKILL, HIT }

/// <summary>
/// 플레이어 이동/공격/상태. VirtualJoystick 입력 + 자동 모드 지원.
/// NavMeshAgent는 PlayerAutoController에서 제어.
/// </summary>
[RequireComponent(typeof(PlayerAutoController))]
public class PlayerController : MonoBehaviour
{
    // ── 스탯 ─────────────────────────────────────────────────────
    private JsonPlayerData _data;
    public  JsonPlayerData Data => _data;

    private int   _curHp;
    private int   _curSp;
    private int   _combo;

    // ── 이동 파라미터 ─────────────────────────────────────────────
    public readonly float MOVE_SPEED_RUN_PARAM  = 0.3f;
    public readonly float MOVE_SPEED_WALK_PARAM = 0.05f;

    // ── UI ───────────────────────────────────────────────────────
    [SerializeField] VirtualJoystick _joystick;
    [SerializeField] ObjectUI        _objectUI;
    [SerializeField] Button          _btnAuto;
    [SerializeField] Image           _imgAutoOn;     // 자동 모드 표시 이미지

    // ── 애니메이션 ────────────────────────────────────────────────
    private Animator     _animator;
    private PlayerState  _state = PlayerState.IDLE;

    private const string PARAM_MOVESPEED = "MoveSpeed";
    private const string PARAM_ATTACK    = "Attack";

    // ── 무기 ─────────────────────────────────────────────────────
    [SerializeField] private Weapon _weapon;

    // ── 자동 모드 ─────────────────────────────────────────────────
    private bool _isAuto = false;
    public  bool IsAuto  => _isAuto;

    private float _skillCoolRemain = 0f;

    // ──────────────────────────────────────────────────────────────

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _data = JsonDataManager.Instance.playerData ?? new JsonPlayerData();
        _curHp = _data.hp;
        _curSp = _data.sp;

        if (_objectUI != null)
        {
            _objectUI.InitHp(_curHp);
            _objectUI.InitSp(_curSp);
        }
    }

    void Start()
    {
        if (_btnAuto != null)
            _btnAuto.onClick.AddListener(ToggleAuto);

        if (_weapon != null)
        {
            _weapon.AddAtk(_data.atk);
            _weapon.SetComboAddCallback(OnCombo);
        }
    }

    void Update()
    {
        if (_skillCoolRemain > 0f)
            _skillCoolRemain -= Time.deltaTime;

        if (!_isAuto)
            HandleJoystickInput();
    }

    void FixedUpdate()
    {
        // 자연 회복
        AddHp(1);
        AddSp(2);
    }

    // ── 조이스틱 입력 ─────────────────────────────────────────────

    void HandleJoystickInput()
    {
        // 조이스틱 입력
        float h = _joystick != null ? _joystick.Horizontal : 0f;
        float v = _joystick != null ? _joystick.Vertical   : 0f;

#if UNITY_EDITOR
        // 에디터 키보드 폴백 (WASD / 방향키)
        float kh = 0f, kv = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  kh = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) kh =  1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    kv =  1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  kv = -1f;
        if (kh != 0f || kv != 0f) { h = kh; v = kv; }
#endif

        Vector3 moveVec = new Vector3(h, 0f, v);
        float dist = moveVec.sqrMagnitude;
        float speed = _data.speed * dist;

        if (moveVec != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(moveVec.normalized);

        transform.position += moveVec.normalized * speed * Time.deltaTime;
        SetAnimMoveSpeed(dist);
    }

    // ── 애니메이션 ────────────────────────────────────────────────

    public void SetAnimMoveSpeed(float dist)
    {
        PlayerState next;
        if      (dist > MOVE_SPEED_RUN_PARAM)  next = PlayerState.RUN;
        else if (dist > MOVE_SPEED_WALK_PARAM) next = PlayerState.WALK;
        else next = _isAuto ? PlayerState.ATTACK : PlayerState.IDLE;

        SetAnimState(next, dist);
    }

    public void SetAnimState(PlayerState next, float speed = 0f)
    {
        if (next == _state) return;

        switch (next)
        {
            case PlayerState.IDLE:
            case PlayerState.WALK:
            case PlayerState.RUN:
                if (_animator != null)
                    _animator.SetFloat(PARAM_MOVESPEED, speed);
                break;

            case PlayerState.ATTACK:
                if (_skillCoolRemain <= 0f)
                    SetAnimState(PlayerState.SKILL);
                else if (_animator != null)
                    _animator.SetTrigger(PARAM_ATTACK);
                break;

            case PlayerState.SKILL:
                _skillCoolRemain = _data.skill_cooltime;
                StartCoroutine(DoSkill());
                break;

            case PlayerState.HIT:
                if (_animator != null)
                    _animator.SetTrigger("Hit");
                break;
        }

        _state = next;
    }

    // ── 공격 ──────────────────────────────────────────────────────

    public void OnAttack()
    {
        SetAnimState(PlayerState.ATTACK);
        if (_weapon != null) _weapon.UseWeapon();
    }

    IEnumerator DoSkill()
    {
        if (_animator != null)
        {
            int randSkill = UnityEngine.Random.Range(1, 3);
            _animator.SetTrigger("Skill" + randSkill);
            if (_weapon != null) _weapon.UseWeapon(randSkill - 1);
        }
        yield return new WaitForSeconds(_data.skill_cooltime);
        _skillCoolRemain = 0f;
    }

    // ── 피격 ──────────────────────────────────────────────────────

    void OnCollisionEnter(Collision col)
    {
        if (!col.transform.CompareTag("Enemy")) return;

        var enemy = col.transform.GetComponentInParent<EnemyController>();
        if (enemy != null)
            TakeDamage(enemy.Data.atk);
    }

    public void TakeDamage(int rawDmg)
    {
        SetAnimState(PlayerState.HIT);

        if (Camera.main != null)
            Camera.main.GetComponent<CameraShake>()?.ShakeCam();

        int dmg = rawDmg - Mathf.RoundToInt(_data.def * 0.2f);
        dmg += UnityEngine.Random.Range(-5, 5);
        dmg = Mathf.Max(1, dmg);

        AddHp(-dmg);
        if (_objectUI != null) _objectUI.SetDamageText(dmg);
    }

    // ── 회복 / 콤보 ───────────────────────────────────────────────

    void AddHp(int value)
    {
        _curHp = Mathf.Clamp(_curHp + value, 0, _data.hp);
        if (_objectUI != null) _objectUI.SetCurHp(_curHp);
    }

    void AddSp(int value)
    {
        _curSp = Mathf.Clamp(_curSp + value, 0, _data.sp);
        if (_objectUI != null) _objectUI.SetCurSp(_curSp);
    }

    void OnCombo()
    {
        _combo++;
        if (_objectUI != null) _objectUI.SetComboText(_combo);
    }

    // ── 장비 보너스 ───────────────────────────────────────────────

    /// <summary>PlayerEquipmentApplier에서 호출. 장비 보너스를 현재 스탯에 추가.</summary>
    public void ApplyEquipmentBonuses(int hpBonus, int atkBonus, int defBonus)
    {
        _data.hp  += hpBonus;
        _data.atk += atkBonus;
        _data.def += defBonus;
        _curHp = _data.hp;
        if (_objectUI != null) _objectUI.InitHp(_curHp);
        if (_weapon   != null) _weapon.AddAtk(atkBonus);
    }

    // ── 자동 모드 ─────────────────────────────────────────────────

    void ToggleAuto()
    {
        _isAuto = !_isAuto;
        if (_imgAutoOn != null) _imgAutoOn.gameObject.SetActive(_isAuto);
    }
}
