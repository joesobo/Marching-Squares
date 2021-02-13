using UnityEngine;

public class PlayerController : MonoBehaviour {
    private Rigidbody2D rb;

    public Vector3 velocity;
    public float speed = 5;
    public float walkAcceleration = 1;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update() {
        float xInput = Input.GetAxisRaw("Horizontal");
        float yInput = Input.GetAxisRaw("Vertical");

        velocity.x = Mathf.MoveTowards(velocity.x, (speed / 100) * xInput, walkAcceleration * Time.deltaTime);
        velocity.y = Mathf.MoveTowards(velocity.y, (speed / 100) * yInput, walkAcceleration * Time.deltaTime);

        // transform.Translate(velocity);
        rb.MovePosition(transform.position + velocity);
    }
}
