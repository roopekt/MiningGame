using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float jumpVelocity = 5f;
    [SerializeField] private float bigJumpVelocity = 10f;
    [SerializeField] private float swimForce = 30f;
    [SerializeField] private float swimMaxVelocity = 4f;
    [SerializeField] private float waterDrag = 5f;
    [SerializeField] private Transform water;

    private Rigidbody rb;
    private Vector3 movement;//horizontal movement

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;//a hack around physics being simulated during splash screen
    }

    void Update()
    {
        rb.isKinematic = false;//see Awake

        movement = transform.forward * Input.GetAxis("Vertical") * movementSpeed +
                   transform.right * Input.GetAxis("Horizontal") * movementSpeed;

    }
    private void FixedUpdate()
    {
        if (GameManager.inventoryModeActive)
            return;

        Vector3 vel = rb.velocity;

        vel.x = movement.x;//move
        vel.z = movement.z;

        //swim if in water
        if (water != null && water.position.y > transform.position.y)
        {
            if ((Input.GetKey("space") || Input.GetKey("left shift")) && rb.velocity.y < swimMaxVelocity) { rb.AddForce(new Vector3(0, swimForce, 0)); }
            else { rb.AddForce(rb.velocity * -waterDrag); }
        }

        rb.velocity = vel;
    }

    void OnCollisionStay(Collision collision)
    {
        if (GameManager.inventoryModeActive)
            return;

        Vector3 vel = rb.velocity;

        //jump
        if (Input.GetKey("left shift"))//big jump
        {
            vel.y = bigJumpVelocity;
        }
        else if (Input.GetKey("space"))//normal jump
        {
            vel.y = jumpVelocity;
        }

        rb.velocity = vel;
    }
}