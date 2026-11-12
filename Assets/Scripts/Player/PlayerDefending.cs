using UnityEngine;

/// <summary>
/// PLACEHOLDER for Tackle and Defend. They only print a message to the Console for now,
/// so the buttons can be tested. Real behaviour comes once the team decides how they should work.
///
/// Attach to: the Player GameObject.
/// </summary>
public class PlayerDefending : MonoBehaviour
{
    private PlayerInputReader input;
    private PlayerPossession possession;
    private bool wasDefending;

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        possession = GetComponent<PlayerPossession>();
        if (input == null)
            Debug.LogError("PlayerDefending needs a PlayerInputReader on the same GameObject.", this);
    }

    private void Update()
    {
        if (input == null) return;
        if (possession != null && possession.IsAttacking) { wasDefending = false; return; }

        if (input.TacklePressed)
            Debug.Log("Tackle pressed (not implemented yet).");

        bool defending = input.DefendHeld;
        if (defending != wasDefending)
        {
            Debug.Log(defending ? "Defend held (not implemented yet)." : "Defend released.");
            wasDefending = defending;
        }
    }
}
