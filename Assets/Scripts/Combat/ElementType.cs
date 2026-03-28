using System;

/// <summary>
/// 8비트 속성 플래그. 비트 연산으로 다중 속성·상성 계산.
/// 예) 불+번개 복합 속성: ElementType.Fire | ElementType.Lightning
/// </summary>
[Flags]
public enum ElementType : byte
{
    None      = 0,
    Fire      = 1 << 0, // 0b00000001
    Water     = 1 << 1, // 0b00000010
    Earth     = 1 << 2, // 0b00000100
    Wind      = 1 << 3, // 0b00001000
    Lightning = 1 << 4, // 0b00010000
    Ice       = 1 << 5, // 0b00100000
    Light     = 1 << 6, // 0b01000000
    Dark      = 1 << 7, // 0b10000000
}

/// <summary>속성 상성 계산 유틸.</summary>
public static class ElementHelper
{
    /// <summary>
    /// 공격 속성이 방어 속성에 얼마나 유효한지 배율 반환.
    /// 1.5f = 효과적, 0.75f = 저항, 1.0f = 보통
    /// </summary>
    public static float GetMultiplier(ElementType attacker, ElementType defender)
    {
        // 상성 테이블: Fire > Wind, Water > Fire, Lightning > Water, ...
        if ((attacker & ElementType.Fire)      != 0 && (defender & ElementType.Wind)  != 0) return 1.5f;
        if ((attacker & ElementType.Water)     != 0 && (defender & ElementType.Fire)  != 0) return 1.5f;
        if ((attacker & ElementType.Lightning) != 0 && (defender & ElementType.Water) != 0) return 1.5f;
        if ((attacker & ElementType.Earth)     != 0 && (defender & ElementType.Lightning) != 0) return 1.5f;
        if ((attacker & ElementType.Wind)      != 0 && (defender & ElementType.Earth) != 0) return 1.5f;
        if ((attacker & ElementType.Ice)       != 0 && (defender & ElementType.Wind)  != 0) return 1.5f;
        if ((attacker & ElementType.Light)     != 0 && (defender & ElementType.Dark)  != 0) return 1.5f;
        if ((attacker & ElementType.Dark)      != 0 && (defender & ElementType.Light) != 0) return 1.5f;

        // 역상성 (약점의 반대)
        if ((attacker & ElementType.Wind)      != 0 && (defender & ElementType.Fire)  != 0) return 0.75f;
        if ((attacker & ElementType.Fire)      != 0 && (defender & ElementType.Water) != 0) return 0.75f;
        if ((attacker & ElementType.Water)     != 0 && (defender & ElementType.Lightning) != 0) return 0.75f;

        return 1.0f;
    }
}
