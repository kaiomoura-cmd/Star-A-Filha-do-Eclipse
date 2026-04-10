/*using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.InputSystem;
public class PlayerOnSpline : MonoBehaviour
{
    public SplineContainer spline;
    public float speed = 5f;

    private float distance;
    private float input;

    public float jumpForce = 8f;
    public float gravity = -20f;

    private float verticalVelocity=0;
    private bool isGrounded;

    public float groundCheckDistance = 1.5f;
    public LayerMask Ground;

    public float verticalOffset=0;
    float currentGroundY;

    BoxCollider col;
    public float groundOffset;
    void Start()
    {
        col = GetComponent<BoxCollider>();
        groundOffset = col.size.y / 2f;
    }

    void Update()
    {
        distance += input * speed * Time.deltaTime;
        distance = Mathf.Clamp(distance, 0, spline.CalculateLength());

        float t = spline.Spline.ConvertIndexUnit(
            distance,
            PathIndexUnit.Distance,
            PathIndexUnit.Normalized
        );

        Vector3 splinePos = spline.EvaluatePosition(t);

        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        if (Physics.SphereCast(origin, 0.1f, Vector3.down, out hit, groundCheckDistance, Ground))
        {
            float slope = Vector3.Dot(hit.normal, Vector3.up);
            bool validGround = slope > 0.2f;
            bool isFalling = verticalVelocity <= 0;
            bool comingFromAbove = transform.position.y > hit.point.y + 0.05f;

            

                if(validGround && isFalling && comingFromAbove)
                {
                currentGroundY = hit.point.y + groundOffset;
                    isGrounded = true;
                    verticalOffset = 0;
                    verticalVelocity = 0;
                }
            
            else
            {
                isGrounded = false;
            }
        }
        else
        {
            isGrounded = false;
        }

        verticalVelocity += gravity * Time.deltaTime;
        verticalOffset += verticalVelocity * Time.deltaTime;

        if (isGrounded && verticalOffset < 0)
        {
            verticalOffset = 0;
        }

        Vector3 finalPos = splinePos;

        finalPos.y = currentGroundY + verticalOffset;

        Vector3 move = finalPos - transform.position;

        finalPos = ChecarColisoes(move, finalPos);
        Vector3 tangent = spline.EvaluateTangent(t);

        transform.position = finalPos;
        transform.forward = tangent;

        Debug.DrawRay(origin, Vector3.down * groundCheckDistance, Color.red);
    }

    public void OnMove(InputValue value)
    {
        input = value.Get<Vector2>().x;
    }

    public void OnJump()
    {
        if (isGrounded)
        {
            verticalVelocity = jumpForce;
            isGrounded = false;
        }
    }

    public Vector3 ChecarColisoes(Vector3 move, Vector3 finalPos)
{
    if (move.magnitude < 0.001f) return finalPos;

    RaycastHit hit;

    Bounds bounds = col.bounds;
    float radius = bounds.extents.x;
    float height = bounds.size.y;
    Vector3 center = bounds.center;

    Vector3 point1 = center + Vector3.up * (height/2 - radius);
    Vector3 point2 = center - Vector3.up * (height/2 - radius);

    if (Physics.CapsuleCast(point1, point2, radius, move.normalized, out hit, move.magnitude, Ground))
    {
        float skinWidth = 0.02f;
        float distance = Mathf.Max(hit.distance - skinWidth, 0f);

        Vector3 moveToHit = move.normalized * distance;
        Vector3 remainingMove = move - moveToHit;

        remainingMove = Vector3.ProjectOnPlane(remainingMove, hit.normal);

        return transform.position + moveToHit + remainingMove;
    }

    return finalPos;
}
}*/