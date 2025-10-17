using UnityEngine;

public class UICameraControl : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // controls camera movements
        Vector3 camMov = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 
        Input.GetAxis("Up/Down"));
        GetComponent<Camera>().transform.Translate(camMov * Time.deltaTime * 50);
        GetComponent<Camera>().transform.Rotate(0, 0, Input.GetAxis("Rotate"), Space.Self);
    }
}
