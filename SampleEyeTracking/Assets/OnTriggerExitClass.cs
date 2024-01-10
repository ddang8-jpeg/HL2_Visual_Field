using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class OnTriggerExitClass : MonoBehaviour
{
  public string tag;
  public UnityEvent onExit;
  // Start is called before the first frame update
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {

  }

  void OnTriggerExit(Collider other)
  {
    if (other.gameObject.tag == tag)
    {
      onExit.Invoke();
    }
  }
}
