using UnityEngine;

/// <summary>
/// A test teammate that jogs back and forth, so you can try Pass and Through.
/// It does not pick up the ball (its PlayerTeam has Can Possess switched off), so passes just reach it.
/// Not real AI.
///
/// Attach to: a capsule with a CharacterController and a PlayerTeam set to Home.
/// (FPPFootball > Add Test Teammate creates it for you.)
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerTeam))]
public class TeammateDummy : MonoBehaviour
{
    [Tooltip("The teammate jogs between its start point and start + this offset.")]
    [SerializeField] private Vector3 patrolOffset = new Vector3(0f, 0f, 12f);
    [SerializeField] private float speed = 3f;
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;
    private Vector3 pointA;
    private Vector3 pointB;
    private bool goingToB = true;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        pointA = transform.position;
        pointB = pointA + patrolOffset;
    }

    private void Update()
    {
        Vector3 target = goingToB ? pointB : pointA;
        Vector3 to = target - transform.position;
        to.y = 0f;

        Vector3 direction = Vector3.zero;
        if (to.magnitude < 0.3f)
        {
            goingToB = !goingToB;
        }
        else
        {
            direction = to.normalized;
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        controller.Move((direction * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
    }
}
