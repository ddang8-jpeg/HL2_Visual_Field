using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Point_Spawner : MonoBehaviour
{
  // Stimuli point object
  public GameObject imageObject;
  // Boolean that must be satisfied to spawn stimuli
  bool canSpawn = true;
  // Brightness override component of HL2
  public BrightnessOverrideComponent bOverride;

  void Start()
  {
    //Set HL2 to maximum brightness initially 
    bOverride.Brightness = 1.0f;
  }

  //This function spawns the fixation point at a specifc x, y position in the
  // canvas with a specific brightness.
  // Param1: X position of point
  // Param2: Y position of point
  // Param3: Alpha/brightness value of point from a range of 0.0f to 1.0f
  public void SpawnObject(float x_pos, float y_pos, float brightness)
  {
    if (canSpawn)
    {
      //CHange the position of the object
      imageObject.transform.localPosition = new Vector3(x_pos, y_pos, 0.0f);
      Image image = imageObject.GetComponent<Image>();
      //Set alpha value to brightness provdied
      var tempColor = image.color;
      tempColor.a = brightness;
      image.color = tempColor;
      StartCoroutine("activateImage");
    }
  }

  //This function sets the stimuli active for 0.2 seconds 
  //After the stimuli is deactivated, it waits for 1 second
  //before script can spawn any more stimuli
  IEnumerator activateImage()
  {
    imageObject.SetActive(true);
    canSpawn = false;
    yield return new WaitForSeconds(0.2f);
    imageObject.SetActive(false);
    yield return new WaitForSeconds(1.0f);
    canSpawn = true;
  }

  //Currently set to spawn stimuli at random positions every frame 
  void Update()
  {
    float brightness = Random.Range(0.0f, 1.0f);
    int x_cord = Random.Range(-200, 200);
    int y_cord = Random.Range(-200, 200);
    SpawnObject(x_cord, y_cord, brightness);
  }

  public void spawnLeft()
  {
    SpawnObject(-88.0f, 0.0f, 1.0f);
  }

  public void spawnLeftDim()
  {
    SpawnObject(-88.0f, 0.0f, 0.5f);
  }

  public void spawnRight()
  {
    SpawnObject(88.0f, 0.0f, 1.0f);
  }

  public void spawnRightDim()
  {
    SpawnObject(88.0f, 0.0f, 0.5f);
  }

  public void spawnUp()
  {
    SpawnObject(0.0f, 88.0f, 1.0f);
  }

  public void spawnUpDim()
  {
    SpawnObject(0.0f, 88.0f, 0.5f);
  }
}
