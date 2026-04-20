using UnityEngine;
using UnityEngine.InputSystem;

public class HoverVehicle : MonoBehaviour
{
    private const float InputDeadzone = 0.05f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float rotationSpeed = 120f;

    [Header("Hover / Alignment")]
    [Tooltip("Point used for hovering. If empty, the vehicle root is used.")]
    [SerializeField] private Transform hoverPoint;
    [SerializeField] private float hoverHeight = 1f;
    [SerializeField] private float rayStartHeight = 5f;
    [SerializeField] private float rayLength = 50f;
    [Tooltip("Layers considered ground for hover/alignment.")]
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Advanced")]
    [Tooltip("Extra probe distance in movement direction to smoothly climb onto slopes.")]
    [SerializeField] private float lookAheadDistance = 1.25f;
    [Tooltip("Maximum height difference allowed when switching to look-ahead surface.")]
    [SerializeField] private float maxAheadHeightGain = 1.25f;
    [Tooltip("Reject surfaces steeper than this normal.y value.")]
    [SerializeField, Range(0f, 1f)] private float minGroundNormalY = 0.2f;
    [Tooltip("Maximum position correction speed while matching hover height.")]
    [SerializeField] private float maxSurfaceSnapSpeed = 8f;
    [Tooltip("Rotation smoothing. 0 = snap.")]
    [SerializeField] private float rotationSmooth = 12f;

    [Header("Debug")]
    [SerializeField] private bool drawDebugRay = false;

    private Vector2 moveInput;
    private Vector3 groundNormal = Vector3.up;
    private float yawDegrees;

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    private void Awake()
    {
        yawDegrees = transform.eulerAngles.y;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        yawDegrees += moveInput.x * rotationSpeed * dt;

        Vector3 yawForward = Quaternion.Euler(0f, yawDegrees, 0f) * Vector3.forward;
        Vector3 moveDir = Vector3.ProjectOnPlane(yawForward, groundNormal);
        if (moveDir.sqrMagnitude <= 0.0001f)
            return;

        moveDir.Normalize();
        transform.position += moveDir * (moveInput.y * moveSpeed * dt);
    }

    private void LateUpdate()
    {
        Vector3 hoverPos = GetHoverPointPosition();
        Vector3 origin = hoverPos + Vector3.up * rayStartHeight;
        Vector3 yawForward = Quaternion.Euler(0f, yawDegrees, 0f) * Vector3.forward;
        float moveSign = Mathf.Abs(moveInput.y) > InputDeadzone ? Mathf.Sign(moveInput.y) : 0f;
        Vector3 aheadOrigin = origin + yawForward * (lookAheadDistance * moveSign);

        if (drawDebugRay)
        {
            Debug.DrawRay(origin, Vector3.down * rayLength, Color.yellow);
            if (moveSign != 0f)
                Debug.DrawRay(aheadOrigin, Vector3.down * rayLength, Color.cyan);
        }

        if (!TryGroundHit(origin, out RaycastHit hitHere))
            return;

        RaycastHit hit = hitHere;
        if (moveSign != 0f && TryGroundHit(aheadOrigin, out RaycastHit hitAhead))
        {
            float aheadHeightGain = hitHere.distance - hitAhead.distance;
            if (aheadHeightGain > 0f && aheadHeightGain <= maxAheadHeightGain)
                hit = hitAhead;
        }

        groundNormal = hit.normal;
        float currentDistanceAlongNormal = Vector3.Dot(hoverPos - hit.point, groundNormal);
        float distanceError = hoverHeight - currentDistanceAlongNormal;
        Vector3 correction = groundNormal * distanceError;
        float maxStep = maxSurfaceSnapSpeed * Time.deltaTime;
        if (correction.magnitude > maxStep)
            correction = correction.normalized * maxStep;
        transform.position += correction;

        Vector3 forwardOnPlane = Vector3.ProjectOnPlane(yawForward, groundNormal);
        if (forwardOnPlane.sqrMagnitude < 0.0001f)
            return;
        forwardOnPlane.Normalize();

        Quaternion targetRotation = Quaternion.LookRotation(forwardOnPlane, groundNormal);
        if (rotationSmooth <= 0f)
        {
            transform.rotation = targetRotation;
        }
        else
        {
            float t = 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
        }
    }

    private bool TryGroundHit(Vector3 origin, out RaycastHit hit)
    {
        return Physics.Raycast(origin, Vector3.down, out hit, rayLength, groundMask, QueryTriggerInteraction.Ignore)
            && hit.normal.y >= minGroundNormalY;
    }

    private Vector3 GetHoverPointPosition()
    {
        return hoverPoint != null ? hoverPoint.position : transform.position;
    }
}
