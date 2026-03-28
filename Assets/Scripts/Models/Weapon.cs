using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 무기. 공격 시 콜라이더/트레일을 활성화.
/// 콜라이더 태그 = "Weapon"
/// </summary>
public class Weapon : MonoBehaviour
{
    [Header("물리 요소")]
    [SerializeField] public CapsuleCollider _collider;

    [Header("이펙트")]
    [SerializeField] public TrailRenderer   _trailFX;
    [SerializeField] public ParticleSystem[] _skillFXs;

    public int Atk { get; private set; }

    private Action _comboAddCallback;
    public Action ComboAddCallback => _comboAddCallback;

    void Awake()
    {
        SetActiveColliderTrail(false);
        Atk = JsonDataManager.Instance.weaponData?.atk ?? 15;
    }

    public void AddAtk(int value) => Atk += value;

    public void SetComboAddCallback(Action cb) => _comboAddCallback = cb;

    public void UseWeapon(int skillIdx = -1)
    {
        if (skillIdx >= 0)
            StartCoroutine(SkillWeapon(skillIdx));
        else
            StartCoroutine(Swing());
    }

    IEnumerator Swing()
    {
        yield return new WaitForSeconds(0.1f);
        SetActiveColliderTrail(true);
        yield return new WaitForSeconds(0.5f);
        SetActiveColliderTrail(false);
    }

    public IEnumerator SkillWeapon(int skillIdx)
    {
        SetActiveSkillFX(skillIdx, true);
        yield return new WaitForSeconds(3f);
        SetActiveSkillFX(skillIdx, false);
    }

    void SetActiveColliderTrail(bool on)
    {
        if (_collider != null) _collider.enabled = on;
        if (_trailFX  != null) _trailFX.enabled  = on;
    }

    public void SetActiveSkillFX(int idx, bool on)
    {
        if (_skillFXs == null || idx >= _skillFXs.Length) return;
        if (on) _skillFXs[idx].Play(true);
        else    _skillFXs[idx].Stop();
    }
}
