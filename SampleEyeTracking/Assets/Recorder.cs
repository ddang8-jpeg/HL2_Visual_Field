using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.MixedReality.OpenXR;
using Microsoft.MixedReality.EyeTracking;

public class Recorder : MonoBehaviour
{
  public GetTimeValue[] scripts;

  public int currIndex = -1;

  public ExtendedEyeGazeDataProvider extendedEyeGazeDataProvider;

  bool started = false;

  // Start is called before the first frame update
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {
    var timestamp = System.DateTime.Now;

    var combinedGazeReadingInWorldSpace = extendedEyeGazeDataProvider.GetWorldSpaceGazeReading(ExtendedEyeGazeDataProvider.GazeType.Combined, timestamp);
    Debug.Log(combinedGazeReadingInWorldSpace);
    if (combinedGazeReadingInWorldSpace != null)
    {
      //Version 1: Raycast vector and see if it hits object 
      //Version 2: Get eye position and vector direction to make canvas follow
      //Currently using version 2 
      var ray = new Ray(combinedGazeReadingInWorldSpace.EyePosition, combinedGazeReadingInWorldSpace.GazeDirection * 10);
      //RaycastAll?
      RaycastHit hit;
      if (Physics.Raycast(ray, out hit, 100000))
      {
        var lasthit = hit.transform.gameObject;
        if (lasthit.GetComponent<Object_Tag>() && started)
        {
          scripts[currIndex].UpdateObj(lasthit.GetComponent<Object_Tag>().name);
        }
      }
    }
  }

  public void IncremenetIndex()
  {
    if (currIndex < scripts.Length)
    {
      started = false;
      if (currIndex >= 0)
      {
        scripts[currIndex].SaveIntoJson();
        Debug.Log("Saved recording at index: " + currIndex);
      }
      started = true;
      currIndex++;
      if (currIndex < scripts.Length)
      {
        scripts[currIndex].StartRecording();
        Debug.Log("Started recording at index: " + currIndex);
      }
    }
    else
    {
      scripts[currIndex].SaveIntoJson();
    }
  }

  public void SetStarted(bool val)
  {
    started = val;
  }

}
