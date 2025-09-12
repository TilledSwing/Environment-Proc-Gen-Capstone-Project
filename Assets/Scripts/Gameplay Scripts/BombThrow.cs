using UnityEngine;

public class BombThrow : MonoBehaviour
{
    Camera playerCamera;
    public GameObject bomb;
    public float throwForce = 10f;
    void Start()
    {
        playerCamera = Camera.main;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ThrowBomb();
        }
    }
    /// <summary>
    /// Throw a glowball object from in front of the player in the direction they are looking
    /// </summary>
    public void ThrowBomb()
    {
        GameObject thrownBall = Instantiate(bomb, gameObject.transform.position + gameObject.transform.forward * 2f, Quaternion.identity);
        Rigidbody bombRB = thrownBall.GetComponent<Rigidbody>();
        bombRB.AddForce(playerCamera.transform.forward * throwForce, ForceMode.Impulse);
    }
}
