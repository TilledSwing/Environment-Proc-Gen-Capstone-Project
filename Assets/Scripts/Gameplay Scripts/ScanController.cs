using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScanController : MonoBehaviour
{
    public float scanRadius;
    public float scanSpeed;
    public float scanDuration;
    public Material scanMaterial;
    public LayerMask scanLayer;
    public GameObject visualScanObj;
    GameObject activeScanObj;
    bool isScanning = false;
    Vector3 scanPosition;
    List<ScanObject> scannedObjects = new();
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha2) && !isScanning)
        {
            StartCoroutine(Scan());
        }
    }

    IEnumerator Scan()
    {
        isScanning = true;
        scanPosition = transform.position;
        scannedObjects.Clear();
        float time = 0f;
        activeScanObj = Instantiate(visualScanObj, scanPosition, Quaternion.identity);
        activeScanObj.transform.localScale = Vector3.zero;

        while (time < scanSpeed)
        {
            time += Time.deltaTime;
            float ratio = Mathf.Clamp01(time / scanSpeed);
            float currentRadius = Mathf.Lerp(0, scanRadius, ratio);
            activeScanObj.transform.localScale = Vector3.one * currentRadius * 2f;

            Collider[] colliders = Physics.OverlapSphere(scanPosition, currentRadius, scanLayer);
            foreach (Collider collider in colliders)
            {
                ScanObject obj = collider.GetComponent<ScanObject>();
                if (scannedObjects.Contains(obj)) continue;
                scannedObjects.Add(obj);
                StartCoroutine(obj.SetScan(scanMaterial, scanDuration));
            }
            yield return null;
        }
        Destroy(activeScanObj);
        isScanning = false;
    }
}
