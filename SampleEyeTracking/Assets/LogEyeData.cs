using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.MixedReality.OpenXR;
using Microsoft.MixedReality.EyeTracking;
using Microsoft.MixedReality.Toolkit.Input;


public class LogEyeData : BaseInputHandler, IMixedRealityPointerHandler
{
  //The green square for debugging purposes that follows the users eye gaze
  public MoveCanvas canvasObj;
  //UI Debug texts
  public Text debugText;
  public Text fixationScript;

  //ExtendedEyeGazeDataProvider to get eye gaze from 
  public ExtendedEyeGazeDataProvider extendedEyeGazeDataProvider;

  //Camera
  public GameObject camera;
  //Attempt to get clicker to work 
  public MixedRealityInputAction ClickerAction;

  //Audio source to play warnings
  public AudioSource source;

  //Audio clip to play if fixation loss exceeds 2 seconds
  public AudioClip fixationLossClip;

  //Audio clip to play if eye gaze is missing for more than 2 seconds
  public AudioClip eyeGazeMissingClip;

  //Audio clip that was supposed to play once clicker is hit
  public AudioClip clickClip;

  //Data to be sent to database
  private DataClass sendData;

  //Floats to keep track of fixation loss and gaze loss time
  float timer_fixation = 0.0f;
  float timer_gaze = 0.0f;
  void Update()
  {
    //Getting the time for gaze vector
    var timestamp = System.DateTime.Now;

    //Obtain gaze vector for left eye
    var leftGazeReadingInWorldSpace = extendedEyeGazeDataProvider.GetWorldSpaceGazeReading(ExtendedEyeGazeDataProvider.GazeType.Left, timestamp);
    //Obtain gaze vector for right eye
    var rightGazeReadingInWorldSpace = extendedEyeGazeDataProvider.GetWorldSpaceGazeReading(ExtendedEyeGazeDataProvider.GazeType.Right, timestamp);
    //Obtain combined gaze vector for both eyes
    var combinedGazeReadingInWorldSpace = extendedEyeGazeDataProvider.GetWorldSpaceGazeReading(ExtendedEyeGazeDataProvider.GazeType.Combined, timestamp);
    //Script currently looks at left gaze vector but it can be modified
    //to be any of the above in the future
    if (leftGazeReadingInWorldSpace != null)
    {
      // Start timer for whether gaze data is being obtained
      // timer_gaze will be incremented by delta time whenever 
      // gaze data is not found. If the time exceeds two seconds, then
      // a sound is played to alert the user
      timer_gaze = 0.0f;

      debugText.text = leftGazeReadingInWorldSpace.GazeDirection.x + "," + leftGazeReadingInWorldSpace.GazeDirection.y + "," + leftGazeReadingInWorldSpace.GazeDirection.z;
      //Move debug object (green square) to eye gaze 
      canvasObj.MoveCanvasToGazeDistance(leftGazeReadingInWorldSpace.EyePosition, leftGazeReadingInWorldSpace.GazeDirection, 5.0f);
      //Get a ray from eye position casted to the direction of the gaze direction * 10 
      var ray = new Ray(leftGazeReadingInWorldSpace.EyePosition, leftGazeReadingInWorldSpace.GazeDirection * 10);
      RaycastHit hit;
      // If the ray hits any collider
      if (Physics.Raycast(ray, out hit, 100000))
      {
        var lasthit = hit.transform.gameObject;
        // Eye gaze ray hits fixation point
        if (lasthit.tag == "FixationPoint")
        {
          //Start fixation loss timer
          // similar to gaze data timer, if timer exceeds
          // two seconds, then a different sound is played
          // to alert the user
          timer_fixation = 0.0f;
          //Pause all audio warnings
          source.Pause();
          // Update text to inform user that no fixation loss
          fixationScript.text = "No fixation loss";
        }
        //This is if eye gaze vector hits any other collider but fixation point
        else
        {
          //Update canvas
          fixationScript.text = "Fixation loss";
          // Increment timer by deltatime
          timer_fixation += Time.deltaTime;
          //If timer exceeds 2 seconds, play fixation loss clip
          if (timer_fixation >= 2.0f)
          {
            source.Pause();
            source.clip = fixationLossClip;
            source.Play();
          }
        }
      }
      // This is if ray hits no collider
      // It does the same thing as if the ray hits the wrong collider
      else
      {
        timer_fixation += Time.deltaTime;
        if (timer_fixation >= 2.0f)
        {
          source.Pause();
          source.clip = fixationLossClip;
          source.Play();
        }
        fixationScript.text = "Fixation loss";
      }
    }
    //If the eye gaze data is null
    else
    {
      // Increment gaze timer
      timer_gaze += Time.deltaTime;
      //If timer exceeds more than two seconds then play warning sound
      if (timer_gaze >= 2.0f)
      {
        source.Pause();
        source.clip = eyeGazeMissingClip;
        source.Play();
      }
      debugText.text = "Error! Gaze data could not be found!";
    }
    //Raycast left/rightGazeReadingInWorld space vector and see if there are fixation errors 
    var combinedGazeReadingInCameraSpace = extendedEyeGazeDataProvider.GetCameraSpaceGazeReading(ExtendedEyeGazeDataProvider.GazeType.Combined, timestamp);

    if (Input.GetMouseButtonDown(0))
    {
      // Perform the select action here
      HandleSelectAction();
    }

  }

  // Clicker handler 
  public void OnPointerClicked(MixedRealityPointerEventData eventData)
  {
    if (eventData.MixedRealityInputAction == ClickerAction)
    {
      source.PlayOneShot(clickClip);
    }
  }

  public void OnPointerUp(MixedRealityPointerEventData eventData) { }
  public void OnPointerDown(MixedRealityPointerEventData eventData)
  {
    if (eventData.MixedRealityInputAction == ClickerAction)
    {
      source.PlayOneShot(clickClip);
    }
  }

  public void OnPointerDragged(MixedRealityPointerEventData eventData) { }

  protected override void UnregisterHandlers() { }
  protected override void RegisterHandlers() { }

  //Send data to firebase real timedatabase
  [ContextMenu("Send Data")]
  public void SaveData()
  {
      sendData = new DataClass();
      sendData.name = "testdata";
      sendData.data = 123;
      FindObjectOfType<Firebase>().PostToDatabase(sendData);
      Debug.Log("Data Sent");
  }

  public void HandleSelectAction()
  {
    source.PlayOneShot(clickClip);
  }

  
}
