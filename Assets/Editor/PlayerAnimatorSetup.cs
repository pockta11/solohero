using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Tools > Setup > Setup Player Animator
/// Creates PlayerAnimator.controller with RPGHero animations
/// and wires it to the Player object in GameScene.
/// </summary>
public static class PlayerAnimatorSetup
{
    private const string ControllerPath = "Assets/Animations/PlayerAnimator.controller";

    [MenuItem("Tools/Setup/Setup Player Animator")]
    public static void SetupPlayerAnimator()
    {
        // --- 1. Load animation clips ---
        string animBase = "Assets/RPGHero/Animations/";
        AnimationClip idle    = AssetDatabase.LoadAssetAtPath<AnimationClip>(animBase + "Idle_SwordShield.anim");
        AnimationClip walk    = AssetDatabase.LoadAssetAtPath<AnimationClip>(animBase + "Walk_SwordShield.anim");
        AnimationClip run     = AssetDatabase.LoadAssetAtPath<AnimationClip>(animBase + "Run_SwordShield.anim");
        AnimationClip attack  = AssetDatabase.LoadAssetAtPath<AnimationClip>(animBase + "NormalAttack01_SwordShield.anim");
        AnimationClip attack2 = AssetDatabase.LoadAssetAtPath<AnimationClip>(animBase + "NormalAttack02_SwordShield.anim");
        AnimationClip hit     = AssetDatabase.LoadAssetAtPath<AnimationClip>(animBase + "GetHit_SwordShield.anim");
        AnimationClip die     = AssetDatabase.LoadAssetAtPath<AnimationClip>(animBase + "Die_SwordShield.anim");

        if (idle == null || walk == null || attack == null)
        {
            Debug.LogError("[PlayerAnimatorSetup] RPGHero animation clips not found. Check Assets/RPGHero/Animations/");
            return;
        }

        // --- 2. Create Animator Controller ---
        System.IO.Directory.CreateDirectory("Assets/Animations");
        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        // --- 3. Add parameters ---
        ctrl.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Attack",    AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Skill1",    AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Hit",       AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Die",       AnimatorControllerParameterType.Trigger);

        var root = ctrl.layers[0].stateMachine;

        // --- 4. Create BlendTree for Idle/Walk/Run ---
        BlendTree moveTree;
        var moveState = ctrl.CreateBlendTreeInController("Move", out moveTree, 0);
        moveTree.blendParameter = "MoveSpeed";
        moveTree.blendType = BlendTreeType.Simple1D;
        moveTree.AddChild(idle, 0f);
        moveTree.AddChild(walk, 0.1f);
        moveTree.AddChild(run,  0.5f);
        root.defaultState = moveState;

        // --- 5. Create states ---
        var attackState = root.AddState("Attack");
        attackState.motion = attack;

        var skillState = root.AddState("Skill");
        skillState.motion = attack2 != null ? attack2 : attack;

        var hitState = root.AddState("Hit");
        hitState.motion = hit;

        var dieState = root.AddState("Die");
        dieState.motion = die;

        // --- 6. Transitions: Any State → Triggers ---
        AddAnyStateTrigger(root, attackState, "Attack", exitTime: false, duration: 0.1f);
        AddAnyStateTrigger(root, skillState,  "Skill1", exitTime: false, duration: 0.1f);
        AddAnyStateTrigger(root, hitState,    "Hit",    exitTime: false, duration: 0.05f);
        AddAnyStateTrigger(root, dieState,    "Die",    exitTime: false, duration: 0.1f);

        // --- 7. Return to Move after Attack/Skill/Hit ---
        AddExitTransition(attackState, moveState, duration: 0.2f);
        AddExitTransition(skillState,  moveState, duration: 0.2f);
        AddExitTransition(hitState,    moveState, duration: 0.15f);

        AssetDatabase.SaveAssets();
        Debug.Log("[PlayerAnimatorSetup] PlayerAnimator.controller created at " + ControllerPath);

        // --- 8. Assign to Player in scene ---
        AssignToPlayer(ctrl);
    }

    static void AddAnyStateTrigger(AnimatorStateMachine sm, AnimatorState dst,
                                   string param, bool exitTime, float duration)
    {
        var t = sm.AddAnyStateTransition(dst);
        t.AddCondition(AnimatorConditionMode.If, 0, param);
        t.hasExitTime       = exitTime;
        t.duration          = duration;
        t.canTransitionToSelf = false;
    }

    static void AddExitTransition(AnimatorState src, AnimatorState dst, float duration)
    {
        var t = src.AddTransition(dst);
        t.hasExitTime  = true;
        t.exitTime     = 1f;
        t.duration     = duration;
    }

    static void AssignToPlayer(AnimatorController ctrl)
    {
        var player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogWarning("[PlayerAnimatorSetup] 'Player' object not found in scene. Open GameScene and run again.");
            return;
        }

        // Assign controller to the child animator that has an avatar (character rig).
        // Do NOT use the root Player's animator — it has no avatar so animations won't play.
        Animator targetAnimator = null;
        foreach (var anim in player.GetComponentsInChildren<Animator>())
        {
            if (anim.avatar != null) { targetAnimator = anim; break; }
        }

        if (targetAnimator == null)
        {
            Debug.LogWarning("[PlayerAnimatorSetup] No child Animator with avatar found. Add RPGHeroPolyart prefab as child of Player first.");
            return;
        }

        targetAnimator.runtimeAnimatorController = ctrl;
        Debug.Log($"[PlayerAnimatorSetup] Controller assigned to {targetAnimator.gameObject.name}.");

        // Disable capsule renderer on root if present
        var meshRenderer = player.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
            Debug.Log("[PlayerAnimatorSetup] Capsule MeshRenderer disabled.");
        }

        // Remove any stray Animator added to the root Player (no avatar = useless)
        var rootAnimator = player.GetComponent<Animator>();
        if (rootAnimator != null)
        {
            Object.DestroyImmediate(rootAnimator);
            Debug.Log("[PlayerAnimatorSetup] Removed root Animator (no avatar).");
        }

        EditorUtility.SetDirty(player);
    }
}
