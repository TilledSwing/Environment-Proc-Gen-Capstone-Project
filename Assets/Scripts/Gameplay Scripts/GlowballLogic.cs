using UnityEngine;

public class GlowballLogic : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        // Debug.Log("Hit " + collision.gameObject.name);
    }
}
