using UnityEngine;

public class GlowballLogic : MonoBehaviour
{
    public float maxAirTime = 6f;
    float creationTime;
    bool hit = false;
    void Start()
    {
        creationTime = Time.time;
    }
    void Update()
    {
        if (!hit) {
            CheckTime();
        }
    }
    void CheckTime()
    {
        float timeExisted = Time.time - creationTime;
        if (timeExisted >= maxAirTime) {
            Destroy(gameObject);
        }
    }
    /// <summary>
    /// Have the glow ball "stick" in place when it collides with something
    /// </summary>
    /// <param name="collision"></param>
    void OnCollisionEnter(Collision collision)
    {
        if (hit) return;
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        ManualAssetIdentification asset = new ManualAssetIdentification(ManualAssetId.GlowBall, transform.position.x, transform.position.y, transform.position.z);
        gameObject.transform.SetParent(collision.gameObject.transform);
        hit = true;
        //Debug.Log("Hit " + collision.gameObject.name);
    }
}
