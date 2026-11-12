using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerBallInteraction : MonoBehaviour
{
    [Header("Dribble")]
    [SerializeField] private float dribbleSpeed = 7f;

    [Header("Kick (Shoot)")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float kickForce = 14f;
    [SerializeField] private float kickRange = 1.5f;
    [SerializeField] private float kickLift = 0.2f;

    [Header("Pass (placeholder: kicks where you look)")]
    [SerializeField] private float passForce = 9f;
    [SerializeField] private float passLift = 0.05f;

    [Header("Through ball (placeholder: flat, longer)")]
    [SerializeField] private float throughForce = 12f;
    [SerializeField] private float throughLift = 0.02f;

    [Header("Clear (defending: big lofted kick)")]
    [SerializeField] private float clearForce = 18f;
    [SerializeField] private float clearLift = 0.7f;

    private PlayerInputReader input;
    private PlayerPossession possession;

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        possession = GetComponent<PlayerPossession>();
        if (input == null)
            Debug.LogError("PlayerBallInteraction needs a PlayerInputReader on the same GameObject.", this);
    }

    // Walking into the ball pushes it along (dribbling)
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rb = hit.rigidbody;
        if (rb == null || rb.isKinematic) return;

        Vector3 dir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        Vector3 v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(dir.x * dribbleSpeed, v.y, dir.z * dribbleSpeed);
    }

    private void Update()
    {
        if (input == null) return;

        bool attacking = possession == null || possession.IsAttacking;

        if (attacking)
        {
            if (input.ShootPressed) Kick(kickForce, kickLift);
            else if (input.PassPressed) Kick(passForce, passLift);
            else if (input.ThroughPressed) Kick(throughForce, throughLift);
        }
        else
        {
            if (input.ClearPressed) Kick(clearForce, clearLift);
        }
    }

    private void Kick(float force, float lift)
    {
        if (cameraTransform == null) return;

        Vector3 origin = transform.position + transform.forward * 1f;
        Collider[] hits = Physics.OverlapSphere(origin, kickRange);

        Rigidbody ball = null;
        float best = float.MaxValue;
        foreach (Collider c in hits)
        {
            Rigidbody rb = c.attachedRigidbody;
            if (rb == null || rb.isKinematic) continue;
            float d = (rb.position - origin).sqrMagnitude;
            if (d < best) { best = d; ball = rb; }
        }
        if (ball == null) return;

        Vector3 dir = cameraTransform.forward;
        dir.y = Mathf.Max(dir.y, 0f) + lift;
        dir.Normalize();

        ball.AddForce(dir * force, ForceMode.VelocityChange);
    }
}
