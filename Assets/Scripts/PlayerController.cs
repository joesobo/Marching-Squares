using UnityEngine;

public class PlayerController : MonoBehaviour {
    public Vector3 velocity;
    public float speed = 5;
    public float walkAcceleration = 1;

    public int jumpforce = 5;
    public float jumpDecelleration = 1;

    public float isGroundedRayLength = 0.1f;
    public LayerMask layerMaskForGrounded;

    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded()) {
            velocity.y = jumpforce;
        }else{
            if (velocity.y > 0) {
                velocity.y -= Time.deltaTime * jumpDecelleration;
            }
        }

        if (velocity.y < 0) {
            velocity.y = 0;
        }
    }

    private void FixedUpdate() {
        float xInput = Input.GetAxisRaw("Horizontal");

        velocity.x = Mathf.MoveTowards(velocity.x, (speed / 100) * xInput, walkAcceleration * Time.deltaTime);

        rb.velocity = (Vector2)velocity;
    }

    private bool isGrounded() {
        RaycastHit2D hit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0f, Vector2.down, .1f, layerMaskForGrounded.value);
        if (hit.collider != null) {
            return true;
        }

        return false;
    }
}
