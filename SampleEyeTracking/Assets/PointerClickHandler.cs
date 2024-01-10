using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;

public class PointerClickHandler : MonoBehaviour, IMixedRealityPointerHandler
{
  public string message;

  // Define the input action for the click
  public MixedRealityInputAction ClickerAction;

  // Audio Source and clip for clicker
  public AudioSource source;
  public AudioClip clickClip;


  public void OnPointerClicked(MixedRealityPointerEventData eventData)
  {
    // Check if the clicked input action matches the defined action
    if (eventData.MixedRealityInputAction == ClickerAction)
    {
      Debug.Log(message);
      PlaySound();
    }
  }

  public void PlaySound()
  {
      source.PlayOneShot(clickClip);
  }


    // Other methods from IMixedRealityPointerHandler interface (not shown in this example)
    public void OnPointerDown(MixedRealityPointerEventData eventData) { }
  public void OnPointerDragged(MixedRealityPointerEventData eventData) { }
  public void OnPointerUp(MixedRealityPointerEventData eventData) { }
}
