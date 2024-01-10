using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PrimaryButtonEvent : MonoBehaviour
{
  PlayerControls controls;
  public AudioSource source;

  public AudioClip clickClip;

  void Awake()
  {
    controls = new PlayerControls();

    controls.Gameplay.Click.performed += ctx => PlaySound();
  }

  public void PlaySound()
  {
    source.PlayOneShot(clickClip);
  }

  void OnEnable()
  {
    controls.Gameplay.Enable();
  }

  void OnDisable()
  {
    controls.Gameplay.Disable();
  }
}
