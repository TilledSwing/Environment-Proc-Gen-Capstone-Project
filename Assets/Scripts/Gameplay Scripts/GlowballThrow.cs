using UnityEngine;

public class GlowballThrow : MonoBehaviour
{
    Camera playerCamera;
    public GameObject glowball;
    public float throwForce = 20f;
    void Start()
    {
        playerCamera = Camera.main;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            GameObject thrownBall = Instantiate(glowball, gameObject.transform.position + gameObject.transform.forward * 2f, Quaternion.identity);
            Rigidbody ballRB = thrownBall.GetComponent<Rigidbody>();
            ballRB.AddForce(playerCamera.transform.forward * throwForce, ForceMode.Impulse);
        }
    }
}
