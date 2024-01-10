using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Raycast_Hit : MonoBehaviour
{
  public GetTimeValue script;
  // Start is called before the first frame update
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {
    var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    RaycastHit hit;
    if (Physics.Raycast(ray, out hit))
    {
      GameObject obj = hit.transform.gameObject;
      QuadScript val = hit.transform.gameObject.GetComponent<QuadScript>();
      if (val != null)
      {
        val.addHitPoint(hit.textureCoord.x * 4 - 2, hit.textureCoord.y * 4 - 2);
        Debug.Log("Hit object at: " + hit.transform.InverseTransformPoint(hit.point));
      }
      if (obj.GetComponent<Object_Tag>())
      {
        script.UpdateObj(obj.GetComponent<Object_Tag>().name);
      }
      else
      {
        script.UpdateObj("Default");
      }


    }
    else
    {
      script.UpdateObj("Default");
    }

  }
}
