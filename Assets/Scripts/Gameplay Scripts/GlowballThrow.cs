using UnityEngine;
using UnityEngine.Rendering;

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
            ThrowGlowball();
        }
    }
    /// <summary>
    /// Throw a glowball object from in front of the player in the direction they are looking
    /// </summary>
    public void ThrowGlowball()
    {
        GameObject thrownBall = Instantiate(glowball, gameObject.transform.position + gameObject.transform.forward * 2f, Quaternion.identity);
        Rigidbody ballRB = thrownBall.GetComponent<Rigidbody>();
        ballRB.AddForce(playerCamera.transform.forward * throwForce, ForceMode.Impulse);
    }
}
