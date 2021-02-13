using UnityEngine;

public class PlayerController : MonoBehaviour {
    private Rigidbody2D rb;

    public Vector3 velocity;
    public float speed = 5;
    public float walkAcceleration = 1;

    public int jumpforce = 5;

    public float isGroundedRayLength = 0.1f;
    public LayerMask layerMaskForGrounded;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded) {
            rb.AddForce(new Vector2(0, jumpforce), ForceMode2D.Force);
        }
    }

    private void FixedUpdate() {
        float xInput = Input.GetAxisRaw("Horizontal");
        float yInput = Input.GetAxisRaw("Vertical");

        velocity.x = Mathf.MoveTowards(velocity.x, (speed / 100) * xInput, walkAcceleration * Time.deltaTime);
        velocity.y = Mathf.MoveTowards(velocity.y, (speed / 100) * yInput, walkAcceleration * Time.deltaTime);

        rb.MovePosition(transform.position + velocity);
    }

    public bool isGrounded {
        get {
            Vector3 position = transform.position;
            position.y = GetComponent<Collider2D>().bounds.min.y + 0.1f;
            float length = isGroundedRayLength + 0.1f;
            Debug.DrawRay(position, Vector3.down * length);
            bool grounded = Physics2D.Raycast(position, Vector3.down, length, layerMaskForGrounded.value);
            return grounded;
        }
    }
}
