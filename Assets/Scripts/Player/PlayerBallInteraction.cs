using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerBallInteraction : MonoBehaviour
{
    [Header("Dribble")]
    [SerializeField] private float dribbleSpeed = 7f;

    [Header("Kick")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float kickForce = 14f;
    [SerializeField] private float kickRange = 1.5f;
    [SerializeField] private float kickLift = 0.2f;

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
        bool kickPressed =
            (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) ||
            (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);

        if (kickPressed) TryKick();
    }

    private void TryKick()
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
        dir.y = Mathf.Max(dir.y, 0f) + kickLift;
        dir.Normalize();

        ball.AddForce(dir * kickForce, ForceMode.VelocityChange);
    }
}