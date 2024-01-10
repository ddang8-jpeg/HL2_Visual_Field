using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OnTriggerEnterClass : MonoBehaviour
{
  public string tag;
  public UnityEvent onEnter;
  // Start is called before the first frame update
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {

  }

  void OnTriggerEnter(Collider other)
  {
    if (other.gameObject.tag == tag)
    {
      onEnter.Invoke();
    }
  }
}
