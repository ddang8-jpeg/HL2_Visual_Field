using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.MixedReality.Toolkit.Utilities;
using Newtonsoft.Json;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.Profiling;

public class SamplerDriver : MonoBehaviour
{
  public float inputTimeDuration;
  private float currentTime = 0f;

  public int batch_size = 8;
  public string filename;
  public int aLimit = 24;
  public string outPntsDict;
  public string outPntsPool;
  private Sampler sampler;
  private bool waitingForUser = true;

  // Start is called before the first frame update
  void Start()
  {
    Debug.Log("Running Sampler");
    sampler = new Sampler(aLimit, filename);
    // Log the serialized pointsDict["tier_1"] with indentation
    Debug.Log(JsonUtility.ToJson(sampler.pointsDict["tier_1"], true));

    // Log each key-value pair in pointsDict["tier_1"]
    foreach (var kvp in sampler.pointsDict["tier_1"])
    {
      // Debug.Log($"{kvp.Key}: {JsonUtility.ToJson(kvp.Value)}");
      string json = JsonConvert.SerializeObject(kvp, Formatting.Indented);
      File.WriteAllText(outPntsDict, json);
    }

    // Log each key-value pair in pointsPool
    foreach (var kvp in sampler.pointsPool)
    {
      // Log the serialized value in pointsPool
      // Debug.Log($"{kvp.Key}: {JsonUtility.ToJson(kvp.Value)}");

      // Debug.Log($"{kvp.Key}: {JsonUtility.ToJson(kvp.Value)}");
      string json = JsonConvert.SerializeObject(kvp, Formatting.Indented);
      File.WriteAllText(outPntsPool, json);

      /*
      // Extract point and log its coordinates
      var p = ((double, double))kvp.Value["point"];
      Debug.Log($"{kvp.Key}: {Mathf.Round(float.Parse(kvp.Key) / 10000) * 10000}");
      var y = Mathf.Round(float.Parse(kvp.Key) / 10000) * 10000 / 1000000 - 24;
      var x = (float.Parse(kvp.Key) - (y + 24) * 1000000) / 100;
      Debug.Log($"{x}, {y}");
      */
    }
  }

  private void Update()
  {
    int num_pts = sampler.pointsPool.Count;
    while (num_pts > 0)
    {
      var batch = sampler.SampleBatch(batch_size);

      currentTime += Time.deltaTime;
      for (int p = 0; p < batch_size; p++)
      {
        if (Input.GetMouseButtonDown(0))
        {
          // set response to 'see' and switch stimulus
          currentTime = 0f;
        }

        if (currentTime >= inputTimeDuration)
        {
          // set response to 'not see' and swtich stimulus
          currentTime = 0f;
        }
      }
      
      sampler.CollectResponse(batch);
      // Set num_pts to number of points in the point pool with priority 0
      num_pts = sampler.pointsPool.Count(id => sampler.pointsPool[id.Key]["priority"].ToString() == "0");
    }
  }
}
