using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


public class Sampler
{
  private static readonly int[] IntensityLevels = new int[] 
  {
    0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30
  };

  public const int Level = 4;
  public const double ConfidenceThreshold = 0.5;

  public Dictionary<string, Dictionary<string, object>> pointsDict;
  public Dictionary<string, Dictionary<string, object>> pointsPool;
  private int aLimit;
  private int batchNum;
  private int defaultIntensity;
  private int countSamples;

  public Sampler(int a_limit, string filename)
  {
    Debug.Log("Building Sampler");
    this.pointsDict = LoadPoints(filename);
    this.pointsPool = BuildPool();
    this.aLimit = a_limit;
    this.batchNum = 1;
    this.defaultIntensity = IntensityLevels[IntensityLevels.Length / 2];
    this.countSamples = 0;
  }

  private Dictionary<string, Dictionary<string, object>> BuildPool()
  {
    var pool = new Dictionary<string, Dictionary<string, object>>();
    foreach (var pntId in pointsDict["tier_1"].Keys)
    {
      var item = pointsDict["tier_1"][pntId] as JObject;

      if (item == null)
      {
        Debug.LogError($"Casting failed. The object is not a Dictionary<string, object>: {item.GetType()}");
      }
      var point = item["point"].ToObject<List<double>>();

      pool[pntId] = new Dictionary<string, object>
      {
        { "point", item["point"].ToObject<List<double>>() },
        { "priority", 1 },
        { "history", new List<Dictionary<string, object>>() },
        { "final_intensity", null}
      };

    }

    var midPoint = (aLimit / 2.0, aLimit / 2.0);

    if (!pool.Keys.Any())
    {
      Debug.LogError("No keys available in the pool.");
      return new Dictionary<string, Dictionary<string, object>>(); 
    }

    var tmpId = pool.Keys.OrderBy(id => {
      var point = pool[id]["point"] as List<double>;
      var pointTuple = (point[0], point[1]);
      return GetDistance(pointTuple, midPoint);
    }).First();

    var tmpPoint = GetPnt(tmpId, aLimit);
    var tmpPoint1 = (-tmpPoint.Item1, tmpPoint.Item2);
    var tmpPoint2 = (-tmpPoint.Item1, -tmpPoint.Item2);
    var tmpPoint3 = (tmpPoint.Item1, -tmpPoint.Item2);
    var tmpId1 = GetId(tmpPoint1, aLimit);
    var tmpId2 = GetId(tmpPoint2, aLimit);
    var tmpId3 = GetId(tmpPoint3, aLimit);

    foreach (var id in new[] { tmpId, tmpId1, tmpId2, tmpId3 })
    {

      if (pool.ContainsKey(id))
      {
        pool[id]["priority"] = 3;
      }
      else
      {
        Debug.LogError($"Key {id} not present in the pool dictionary.");
      }
    }

    return pool;
  }

  private (double, double) ConvertToPointTuple(JArray array)
  {
    double x = array[0].ToObject<double>();
    double y = array[1].ToObject<double>();
    return (x, y);
  }

  public List<Dictionary<string, object>> SampleBatch(int batchSize = 8)
  {
    List<string> bpIds;
    if (this.batchNum == 1)
    {
      bpIds = GetNextBatch(batchSize / 2);
    }
    else
    {
      bpIds = GetNextBatch(batchSize);
    }

    var batch = new List<Dictionary<string, object>>();
    foreach (var pntId in OrderBatch(bpIds))
    {
      var (intensity, step) = GetIntensity(pntId);
      batch.Add(new Dictionary<string, object>
                {
                    { "id", pntId },
                    { "point", this.pointsPool[pntId]["point"] },
                    { "intensity", intensity },
                    { "step", step }
                });
    }

    this.batchNum += 1;
    this.countSamples += batch.Count;
    return batch;
  }

  public void CollectResponse(List<Dictionary<string, object>> responses)
  {
    foreach (var resp in responses)
    {
      var id = (string)resp["id"];
      var history = new Dictionary<string, object>
                {
                    { "intensity", resp["intensity"] },
                    { "step", resp["step"] },
                    { "conf", resp["conf"] },
                    { "see", resp["see"] }
                };
      ((List<Dictionary<string, object>>)this.pointsPool[id]["history"]).Add(history);

      if ((double)resp["conf"] >= ConfidenceThreshold) continue;

      if ((string)resp["step"] == "half")
      {
        if ((bool)resp["see"])
        {
          this.pointsPool[id]["final_intensity"] = resp["intensity"];
        }
        else
        {
          var idx = FindNextIndex(id, -2);
          if ((bool)((List<Dictionary<string, object>>)this.pointsPool[id]["history"])[idx]["see"])
          {
            this.pointsPool[id]["final_intensity"] = ((List<Dictionary<string, object>>)this.pointsPool[id]["history"])[idx]["intensity"];
          }
          else
          {
            idx = FindNextIndex(id, idx - 1);
            this.pointsPool[id]["final_intensity"] = ((List<Dictionary<string, object>>)this.pointsPool[id]["history"])[idx]["intensity"];
          }

          this.pointsPool[id]["priority"] = 0;
        }
      }
      else
      {
        this.pointsPool[id]["priority"] = (int)this.pointsPool[id]["priority"] + 1;

        if ((int)resp["intensity"] >= IntensityLevels[IntensityLevels.Length - 2])
        {
          if (!((bool)resp["see"]))
          {
            this.pointsPool[id]["final_intensity"] = IntensityLevels[IntensityLevels.Length - 1];
            this.pointsPool[id]["priority"] = 0;
          }
        }
        else if ((int)resp["intensity"] <= IntensityLevels[1])
        {
          if ((bool)resp["see"])
          {
            this.pointsPool[id]["final_intensity"] = IntensityLevels[1];
            this.pointsPool[id]["priority"] = 0;
          }
        }
      }
    }
  }

  private int FindNextIndex(string id, int idx)
  {
    while ((double)((List<Dictionary<string, object>>)this.pointsPool[id]["history"])[idx]["conf"] < ConfidenceThreshold)
    {
      idx -= 1;
    }

    return idx;
  }

  private (int?, string) GetIntensity(string pntId)
  {
    var hstry = (List<Dictionary<string, object>>)this.pointsPool[pntId]["history"];

    if (hstry.Count <= 0) return (GetIntensityFromNeighbors(pntId), "full");

    var intensity = (int)hstry.Last()["intensity"];
    var step = (string)hstry.Last()["step"];

    // Possibly redundant, may remove
    if (hstry.Count <= 0)
    {
      var intnst = GetIntensityFromNeighbors(pntId);
      return (intnst, "full");
    }

    // Low confidence: repeat the point
    if ((double)hstry.Last()["conf"] < ConfidenceThreshold) return (intensity, step);

    // Return null if not full step
    if ((string)hstry.Last()["step"] != "full") return (null, null);

    // If full step not seen
    if (!(bool)hstry.Last()["see"])
    {
      if (intensity < 30)
      {
        var (level, s) = GetStepSize(hstry);
        return (intensity + level, s);
      }
      else
      {
        Debug.Log("Intensity went above 30");
        return (null, null);
      }
    }

    // If full step and was seen
    if (intensity > 0)
    {
      var (level, s) = GetStepSize(hstry);
      return (intensity - level, s);
    }
    else
    {
      Debug.Log("Intensity went below zero");
      return (null, null);
    }
  }


  private (int, string) GetStepSize(List<Dictionary<string, object>> history)
  {
    var flags = history.Where(h => (double)h["conf"] >= ConfidenceThreshold).Select(h => (int)h["see"]).ToList();
    if (flags.Count <= 1)
    {
      return (Level, "full");
    }

    if (flags.Sum() == 0 || flags.Sum() == flags.Count)
    {
      return (Level, "full");
    }
    else
    {
      return (Level / 2, "half");
    }
  }

  private int GetIntensityFromNeighbors(string pntId)
  {
    var innerDict = this.pointsDict["tier_1"][pntId] as JObject;
    var nghbrs = innerDict["n_tier_1"];
    var intensities = new List<int>();
    var sampled = this.pointsPool.Keys.Where(_id => (int)this.pointsPool[_id]["priority"] == 0).ToList();
    foreach (var _id in nghbrs)
    {
      if (sampled.Contains(_id.ToString()))
      {
        if (this.pointsPool[_id.ToString()]["final_intensity"] != null)
        {
          intensities.Add((int)this.pointsPool[_id.ToString()]["final_intensity"]);
        }
      }
    }

    if (intensities.Count > 0)
    {
      var ind = Array.BinarySearch(IntensityLevels.ToArray(), (int)Mathf.Round((float)intensities.Average()));
      ind = (ind < 0) ? ~ind - 1 : ind;
      return IntensityLevels[ind];
    }
    else
    {
      return IntensityLevels[IntensityLevels.Length / 2];
    }
  }

  private List<string> GetNextBatch(int numSamples)
  {
    var batch = this.pointsPool.Keys
            .OrderBy(x => (int)this.pointsPool[x]["priority"])
            .Take(numSamples)
            .Where(pntId => (int)this.pointsPool[pntId]["priority"] > 0)
            .ToList();
    return batch;
  }

  private (double, double) GetPnt(string id, int aLimit)
  {
    var y = Mathf.Round(float.Parse(id) / 10000) * 10000 / 1000000 - aLimit;
    var x = (float.Parse(id) - (y + aLimit) * 1000000) / 100;
    return (x, y);
  }

  private string GetId((double, double) point, int aLimit)
  {
    var id = (int)((aLimit + Math.Round((double)point.Item2 * 100) / 100) * 10000000 + 100 * Math.Round((double)point.Item1 * 100) / 100);
    if (id < 0)
    {
      Debug.LogError("Id cannot be negative number: " + id 
        + "\nitem2: " + (double)point.Item2 + "\nitem1: " + (double)point.Item1);
    }
    return $"id";
  }

  private List<string> OrderBatch(List<string> pnts)
  {
    return pnts;
  }

  private void AdaptPriorities()
  {
    // To be implemented
  }

  private static Dictionary<string, Dictionary<string, object>> LoadPoints(string filename)
  {
    using (var reader = new StreamReader(filename))
    {
      var pointsDct = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(reader.ReadToEnd());
      return pointsDct;
    }
  }

  // Calculates Euclidean distance between two points
  private double GetDistance((double, double) p1, (double, double) p2)
  {
    return Math.Sqrt(Math.Pow((double)(p1.Item1 - p2.Item1), 2) + Math.Pow((double)(p1.Item2 - p2.Item2), 2));
  }


}

