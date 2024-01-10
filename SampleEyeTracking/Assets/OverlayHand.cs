using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Microsoft;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;

public class OverlayHand : MonoBehaviour
{
  public GameObject overlayObject;

  GameObject handObj;

  MixedRealityPose pose;
  // Start is called before the first frame update
  void Start()
  {
    handObj = Instantiate(overlayObject, this.transform);
  }

  // Update is called once per frame
  void Update()
  {
    handObj.GetComponent<Renderer>().enabled = false;
    if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Handedness.Right, out pose))
    {
      handObj.GetComponent<Renderer>().enabled = true;
      handObj.transform.position = pose.Position;
    }
  }
}
