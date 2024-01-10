using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCanvas : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void MoveCanvasToGaze(Vector3 eyePosition, Vector3 gazeDirection){
        Vector3 normalizedGaze = gazeDirection.normalized;
        transform.position = new Vector3(eyePosition.x + (500 * normalizedGaze.x), eyePosition.y + (500 * normalizedGaze.y), eyePosition.z + (500 * normalizedGaze.z)); 
        transform.rotation = Quaternion.LookRotation(transform.position - eyePosition);
    }

     public void MoveCanvasToGazeDistance(Vector3 eyePosition, Vector3 gazeDirection, float dist){
        Vector3 normalizedGaze = gazeDirection.normalized;
        transform.position = new Vector3(eyePosition.x + (dist * normalizedGaze.x), eyePosition.y + (dist * normalizedGaze.y), eyePosition.z + (dist * normalizedGaze.z)); 
        transform.rotation = Quaternion.LookRotation(transform.position - eyePosition);
    }
}
