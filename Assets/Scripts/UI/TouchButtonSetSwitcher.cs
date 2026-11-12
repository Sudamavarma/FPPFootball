using UnityEngine;

/// <summary>
/// Shows the ATTACKING buttons or the DEFENDING buttons depending on PlayerPossession.
/// Uses CanvasGroups (not SetActive) so a finger that is still holding a button
/// is released cleanly when the set changes.
///
/// Attach to: the TouchControlsCanvas root.
/// Inspector: drag AttackButtons and DefendButtons into the two slots.
/// </summary>
public class TouchButtonSetSwitcher : MonoBehaviour
{
    [SerializeField] private CanvasGroup attackingButtons;
    [SerializeField] private CanvasGroup defendingButtons;
    [Tooltip("Leave empty to find the one on the Player automatically.")]
    [SerializeField] private PlayerPossession possession;

    private bool applied;
    private bool lastAttacking;

    private void Start()
    {
        if (possession == null)
            possession = FindFirstObjectByType<PlayerPossession>();
        if (possession == null)
            Debug.LogWarning("TouchButtonSetSwitcher: no PlayerPossession found in the scene.", this);
    }

    private void Update()
    {
        if (possession == null) return;

        bool attacking = possession.IsAttacking;
        if (applied && attacking == lastAttacking) return;

        SetGroup(attackingButtons, attacking);
        SetGroup(defendingButtons, !attacking);
        lastAttacking = attacking;
        applied = true;
    }

    private static void SetGroup(CanvasGroup group, bool visible)
    {
        if (group == null) return;
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }
}

