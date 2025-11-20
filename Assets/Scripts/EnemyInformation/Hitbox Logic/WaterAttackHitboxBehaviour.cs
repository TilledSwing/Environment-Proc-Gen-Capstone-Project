using UnityEngine;

public class WaterAttackHitboxBehaviour : StateMachineBehaviour
{
    [Tooltip("Which hitboxes to enable for this attack (on the enemy GameObject or its children).")]
    public string[] hitboxNames;

    [Tooltip("Start time of hitbox activation (0 = animation start, 1 = end).")]
    [Range(0f, 1f)] public float hitStart = 0.2f;

    [Tooltip("End time of hitbox activation (0 = animation start, 1 = end).")]
    [Range(0f, 1f)] public float hitEnd = 0.5f;
    public bool isFirstStateOfAttack;

    private bool active = false;
    private EnemyHitbox[] allHitboxes;
    private AttackHitRegistry registry;
    

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Find all hitboxes under the enemy
        allHitboxes = animator.GetComponentsInChildren<EnemyHitbox>(true);
        registry = animator.GetComponentInParent<AttackHitRegistry>();
        if (isFirstStateOfAttack)
                registry.ResetHits();   
        active = false;
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float t = stateInfo.normalizedTime % 1f; // animation progress 0–1

        if (!active && t >= hitStart)
        {
            SetHitboxes(animator, true);
            active = true;
        }
        else if (active && t >= hitEnd)
        {
            SetHitboxes(animator, false);
            active = false;
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Safety: ensure hitboxes are off when exiting
        SetHitboxes(animator, false);
        active = false;
    }

    private void SetHitboxes(Animator animator, bool enable)
    {
        foreach (var hb in allHitboxes)
        {
            if (hitboxNames.Length == 0 || System.Array.Exists(hitboxNames, n => hb.name == n))
            {
                if (enable) hb.EnableHitbox();
                else hb.DisableHitbox();
            }
        }
    }
}
