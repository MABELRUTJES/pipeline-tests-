using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Vector3 offset = new Vector3(0f, 6f, -12f);
    [SerializeField] private float followSpeed = 12f;
    [SerializeField] private float rotateSpeed = 12f;
    [SerializeField] private float pitchDownDegrees = 20f;

    private void LateUpdate()
    {
        if (player == null)
            return;

        float dt = Time.deltaTime;
        float posT = 1f - Mathf.Exp(-followSpeed * dt);
        float rotT = 1f - Mathf.Exp(-rotateSpeed * dt);

        // Shooter-style: position follows behind the player, rotation follows player's heading.
        Vector3 desiredPos = player.position + (player.rotation * offset);
        transform.position = Vector3.Lerp(transform.position, desiredPos, posT);

        Vector3 flatForward = Vector3.ProjectOnPlane(player.forward, Vector3.up);
        if (flatForward.sqrMagnitude < 0.0001f)
            flatForward = Vector3.forward;
        flatForward.Normalize();

        Quaternion desiredRot = Quaternion.LookRotation(flatForward, Vector3.up);
        desiredRot = desiredRot * Quaternion.Euler(pitchDownDegrees, 0f, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotT);
    }
}
