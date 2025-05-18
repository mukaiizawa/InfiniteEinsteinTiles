using UnityEngine;

public class RotatingProjectile : MonoBehaviour
{
    public float Speed = 18f;
    public float RotationSpeed = 360f;

    void Start()
    {
        var collider = GetComponent<Collider2D>();
        if (collider != null) Destroy(collider);
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        var rad = Random.Range(0, 2 * Mathf.PI);
        rb.mass = 1f;
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.AddForce(new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * Speed, ForceMode2D.Impulse);
    }

    void Update()
    {
        transform.Rotate(Vector3.forward * RotationSpeed * Time.deltaTime);
    }

}
