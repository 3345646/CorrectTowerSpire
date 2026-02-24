using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Grappling : MonoBehaviour
{
    [Header("References")]
    public PlayerMovement pMove;
    public Transform cam;
    public Transform gunTip;
    public LayerMask grappleableMask;
    public LineRenderer lr;
    Rigidbody rb;

    [Header("Grappling")]
    public float maxGrappleDistance = 50f;
    public float grappleDelay = 0.1f;           // Delay before the grapple engages
    public float spring = 100f;                 // SpringJoint spring strength
    public float damper = 7f;                   // SpringJoint damping (more = less swing)
    [Range(0f, 1f)] public float maxDistancePercent = 0.8f; // how short the rope becomes relative to start distance

    [Header("Pull/Control")]
    public float reelSpeed = 5f;                // How fast the joint's maxDistance shortens when reeling in
    public KeyCode grappleKey = KeyCode.Mouse1;

    [Header("Cooldown")]
    public float grappleCooldown = 1.0f;
    private float cooldownTimer;

    // runtime
    private Vector3 grapplePoint;
    private SpringJoint joint;
    private bool isGrappling;
    private Coroutine grappleCoroutine;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (pMove == null) pMove = GetComponent<PlayerMovement>();
        if (lr != null) lr.enabled = false;
        if (cam == null) Debug.LogWarning("Grappling: Camera reference missing.");
        if (gunTip == null) Debug.LogWarning("Grappling: gunTip reference missing.");
    }

    void Update()
    {
        // start grapple
        if (Input.GetKeyDown(grappleKey) && cooldownTimer <= 0f)
        {
            StartGrapple();
        }

        // release grapple early
        if (Input.GetKeyUp(grappleKey) && isGrappling)
        {
            StopGrapple();
        }

        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        // Optional: Reel in while holding another key (example: W) - adjust to your controls
        if (isGrappling && joint != null)
        {
            // Example simple reel: shorten the joint's maxDistance over time while player holds "W"
            if (Input.GetKey(KeyCode.W))
            {
                float newMax = Mathf.Max(0.1f, joint.maxDistance - reelSpeed * Time.fixedDeltaTime);
                joint.maxDistance = newMax;
            }
        }
    }

    void LateUpdate()
    {
        if (isGrappling && lr != null)
        {
            // Update line renderer endpoints smoothly
            lr.SetPosition(0, gunTip.position);
            Vector3 endPos = GetGrappleAnchorWorldPosition();
            lr.SetPosition(1, endPos);
        }
    }

    private void StartGrapple()
    {
        if (cooldownTimer > 0f) return;

        // Begin raycast
        if (grappleCoroutine != null) StopCoroutine(grappleCoroutine);
        grappleCoroutine = StartCoroutine(GrappleRoutine());
    }

    private IEnumerator GrappleRoutine()
    {
        // optional: start "shoot" visuals immediately
        if (lr != null) { lr.enabled = true; lr.SetPosition(0, gunTip.position); }

        // Raycast for grapple point
        RaycastHit hit;
        Vector3 origin = cam.position;
        Vector3 dir = cam.forward;
        bool hitSomething = Physics.Raycast(origin, dir, out hit, maxGrappleDistance, grappleableMask);

        if (hitSomething)
        {
            grapplePoint = hit.point;
        }
        else
        {
            // If nothing hit, aim at max distance in forward direction
            grapplePoint = origin + dir * maxGrappleDistance;
        }

        // show rope endpoint immediately
        if (lr != null) lr.SetPosition(1, grapplePoint);

        // Wait for grapple delay (player can cancel by releasing key)
        float timer = 0f;
        while (timer < grappleDelay)
        {
            if (Input.GetKeyUp(grappleKey))
            {
                // canceled before engage
                if (lr != null) lr.enabled = false;
                grappleCoroutine = null;
                yield break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // Engage the grapple
        ExecuteGrapple(hitSomething ? hit : default);
        grappleCoroutine = null;
    }

    private void ExecuteGrapple(RaycastHit hit)
    {
        // create a SpringJoint on the player
        if (joint != null) Destroy(joint);

        joint = gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;

        bool connectedToRigidBody = hit.rigidbody != null;

        if (connectedToRigidBody)
        {
            joint.connectedBody = hit.rigidbody;
            // connectedAnchor is local to the connected body space; using hit.point requires convert to local:
            joint.connectedAnchor = joint.connectedBody.transform.InverseTransformPoint(hit.point);
        }
        else
        {
            // no rigidbody on hit; use world-space connectedAnchor and connectedBody = null
            joint.connectedBody = null;
            joint.connectedAnchor = grapplePoint; // works as world anchor when connectedBody == null
        }

        float distance = Vector3.Distance(transform.position, grapplePoint);
        joint.maxDistance = distance * maxDistancePercent; // initial slack
        joint.minDistance = 0f;
        joint.spring = spring;
        joint.damper = damper;
        joint.massScale = 1f;

        isGrappling = true;
        if (pMove != null)
        {
            pMove.ActiveGrapple = true;
            pMove.freeze = false; // ensure player controller isn't frozen; adapt as needed
        }

        // enable line renderer
        if (lr != null)
        {
            lr.enabled = true;
            lr.positionCount = 2;
            lr.SetPosition(0, gunTip.position);
            lr.SetPosition(1, GetGrappleAnchorWorldPosition());
        }
    }

    private Vector3 GetGrappleAnchorWorldPosition()
    {
        if (joint == null) return grapplePoint;

        if (joint.connectedBody != null)
        {
            // convert the connectedAnchor (local) to world
            return joint.connectedBody.transform.TransformPoint(joint.connectedAnchor);
        }
        else
        {
            return joint.connectedAnchor; // world-space point
        }
    }

    public void StopGrapple()
    {
        if (!isGrappling) return;

        isGrappling = false;

        if (joint != null) Destroy(joint);

        if (lr != null) lr.enabled = false;

        if (pMove != null) pMove.ActiveGrapple = false;

        cooldownTimer = grappleCooldown;
    }

    private void OnDisable()
    {
        // safety cleanup
        StopGrapple();
        if (grappleCoroutine != null) StopCoroutine(grappleCoroutine);
    }
}
