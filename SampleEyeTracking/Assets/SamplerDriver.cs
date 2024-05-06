using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.MixedReality.Toolkit.Utilities;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Profiling;
using System;
using Newtonsoft.Json.Linq;
using Microsoft.MixedReality.Toolkit;
using UnityEngine.Experimental.GlobalIllumination;

public class SamplerDriver : MonoBehaviour
{
  public int batch_size = 8;
  public string filename;
  public int aLimit = 24;
  public double timeout;
  public string outPntsDict;
  public string outPntsPool;
  private Sampler sampler;
  private bool waitingForUser = true;
  private TimeSpan inputTimeDuration;
  public GameObject canvas;

  // Start is called before the first frame update
  void Start()
  {
    Debug.Log("Running Sampler");
    sampler = new Sampler(aLimit, filename);
    inputTimeDuration = TimeSpan.FromSeconds(timeout);
    // Log the serialized pointsDict["tier_1"] with indentation

    // Debug.Log(JsonUtility.ToJson(sampler.pointsDict["tier_1"], true));

    /*
    foreach (var key in sampler.pointsDict["tier_1"].Keys) {
      Debug.Log("Key: " + key);
    }
    */

    File.WriteAllText(outPntsDict, JsonConvert.SerializeObject(sampler.pointsDict, Formatting.Indented));
    File.WriteAllText(outPntsPool, JsonConvert.SerializeObject(sampler.pointsPool, Formatting.Indented));


     StartCoroutine(ProcessBatches());

    /*
    // Extract point and log its coordinates
    var p = ((double, double))kvp.Value["point"];
    Debug.Log($"{kvp.Key}: {Mathf.Round(float.Parse(kvp.Key) / 10000) * 10000}");
    var y = Mathf.Round(float.Parse(kvp.Key) / 10000) * 10000 / 1000000 - 24;
    var x = (float.Parse(kvp.Key) - (y + 24) * 1000000) / 100;
    Debug.Log($"{x}, {y}");
    */

  }

  private void SaveData()
{
  StringBuilder jsonBuilder = new StringBuilder();
  jsonBuilder.Append("{ \"tier_1\": {");  // Start of JSON object

  bool firstItem = true;
  foreach (var kvp in sampler.pointsDict["tier_1"])
  {
    if (!firstItem)
    {
      jsonBuilder.Append(",");
    }
    firstItem = false;

    string jsonEntry = JsonConvert.SerializeObject(kvp.Value, Formatting.Indented);
    jsonBuilder.Append($"\"{kvp.Key}\": {jsonEntry}");
  }

  jsonBuilder.Append("}}");  // End of JSON object

  string json = jsonBuilder.ToString();
  // Debug.Log(json);  // Log the complete JSON string

  // Write the complete JSON to file
  try
  {
    File.WriteAllText(outPntsDict, json);
  }
  catch (Exception ex)
  {
    Debug.LogError($"Failed to write to file: {ex.Message}");
  }
}


  IEnumerator ProcessBatches()
  {
    int num_pts = sampler.pointsPool.Count;
    Debug.Log("starting batch with points: " + num_pts);
    
    while (num_pts > 0)
    {
      DateTime currentTime = DateTime.Now;
      Debug.Log("Starting batch :" + DateTime.Now);

      var batch = sampler.SampleBatch(batch_size);
      Debug.Log("batch: " + batch.Count);

      double point1 = 0;
      double point2 = 0;
      // var responses = new List<Dictionary<string, object>>();
      foreach (var pnt in batch)
      {
        if (pnt.TryGetValue("point", out object pointObject) && pointObject is List<double> pointList)
        {
          point1 = pointList[0];
          point2 = pointList[1];
        }

        ;
         
        List<double> points = new List<double>();

        Debug.Log($"point: {pointObject.GetType()} and {pointObject}");

        canvas.GetComponent<Point_Spawner>().SpawnObject((float)point1 * 10, (float)point2 * 10, 1);
          /*
          while (DateTime.Now.Subtract(currentTime) >= (inputTimeDuration))
          {
            if (Input.GetMouseButtonDown(0))
            {
              // responses.Add(CollectResponse(pnt, true));
              Debug.Log("Clicked in ProcessBatches()");
              break;
            }
          }

          */

        //need to implement missed stimuli case

      }

      // sampler.CollectResponse(batch);
      // Set num_pts to number of points in the point pool with priority 0
      // num_pts = sampler.pointsPool.Count(id => sampler.pointsPool[id.Key]["priority"].ToString() == "0");
      num_pts -= 1;
    }
    
    yield return null;

  }

  Dictionary<string, object> CollectResponse(Dictionary<string, object> pnt, bool sees)
  {
    // Here you would add the point to a response collection to be processed
    var response = new Dictionary<string, object>
        {
            { "id", pnt["id"] },
            { "point", pnt["point"] },
            { "intensity", pnt["intensity"] },
            { "conf", 1 },
            { "see", sees }
        };

    // Assuming you have a method to handle adding this response to the batch
    Debug.Log($"Response Collected: {JsonUtility.ToJson(response)}");
    return response;
  }

}
