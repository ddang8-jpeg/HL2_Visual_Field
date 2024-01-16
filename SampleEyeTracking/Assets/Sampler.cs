using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


public class Sampler
{
    public static List<int> IntensityLevels = new List<int>
        {
            0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30
        };

    public const int Level = 4;
    public const double ConfidenceThreshold = 0.5;
    public string filename = "Assets/points_24.json";
    public int aLimit = 24;

    public Dictionary<string, Dictionary<string, object>> pointsDict;
    public Dictionary<string, Dictionary<string, object>> pointsPool;
    public Dictionary<string, Dictionary<string, object>> sampledPool;
    private int batchNum;
    private int defaultIntensity;
    private int countSamples;

    public Sampler()
    {
        Debug.Log("Building Sampler");
        this.aLimit = aLimit;
        this.pointsDict = LoadPoints(filename);
        this.pointsPool = BuildPool();
        this.sampledPool = new Dictionary<string, Dictionary<string, object>>();
        this.batchNum = 1;
        this.defaultIntensity = IntensityLevels[IntensityLevels.Count / 2];
        this.countSamples = 0;
    }

    private Dictionary<string, Dictionary<string, object>> BuildPool()
    {
        var pool = new Dictionary<string, Dictionary<string, object>>();
        foreach (var pntId in this.pointsDict["tier_1"].Keys)
        {
            // Cast inner dictionary to type
            var innerDict = ((JObject)this.pointsDict["tier_1"][pntId]).ToObject<Dictionary<string, object>>();

            if (innerDict == null) Debug.LogError($"Failed to cast {pntId} to Dictionary<string, object>");

            // For each pntId in the keys of this.pointsDict["tier_1"]
            // Create a new entry in the pool dictionary with pntId as the key
            pool[pntId] = new Dictionary<string, object>
            {
                // Set the "point" property of the new entry to the value from this.pointsDict["tier_1"][pntId]["point"]
                { "point", innerDict["point"] },

                // Set the "priority" property to 1
                { "priority", 1 },

                // Set the "history" property to a new empty list of dictionaries
                { "history", new List<Dictionary<string, object>>() },

                // Set the "final_intensity" property to null
                { "final_intensity", null }
            };
        }


        var midPoint = (this.aLimit / 2.0, this.aLimit / 2.0);
        var tmpId = pool.Keys
            .OrderBy(x => GetDistance(ConvertToPointTuple((JArray)pool[x]["point"]), midPoint))
            .First();
        var tmpPnt = GetPnt(tmpId, this.aLimit);
        var tmpPnt1 = (-tmpPnt.Item1, tmpPnt.Item2);
        var tmpPnt2 = (-tmpPnt.Item1, -tmpPnt.Item2);
        var tmpPnt3 = (tmpPnt.Item1, -tmpPnt.Item2);
        var tmpId1 = GetId(tmpPnt1, this.aLimit);
        var tmpId2 = GetId(tmpPnt2, this.aLimit);
        var tmpId3 = GetId(tmpPnt3, this.aLimit);

        foreach (var pntId in new List<string> { tmpId, tmpId1, tmpId2, tmpId3 })
        {
            pool[pntId]["priority"] = 3;
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

                if ((int)resp["intensity"] >= IntensityLevels[IntensityLevels.Count - 2])
                {
                    if (!((bool)resp["see"]))
                    {
                        this.pointsPool[id]["final_intensity"] = IntensityLevels[IntensityLevels.Count - 1];
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
        var innerDict = (Dictionary<string, object>)this.pointsDict["tier_1"][pntId];
        var nghbrs = (List<string>)innerDict["n_tier_1"];
        var intensities = new List<int>();
        var sampled = this.pointsPool.Keys.Where(_id => (int)this.pointsPool[_id]["priority"] == 0).ToList();
        foreach (var _id in nghbrs)
        {
            if (sampled.Contains(_id))
            {
                if (this.pointsPool[_id]["final_intensity"] != null)
                {
                    intensities.Add((int)this.pointsPool[_id]["final_intensity"]);
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
            return IntensityLevels[IntensityLevels.Count / 2];
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
        return $"{(int)((aLimit + Mathf.Round((float)point.Item2 * 100) / 100) * 10000000 + 100 * Mathf.Round((float)point.Item1 * 100) / 100)}";
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
        return Mathf.Sqrt(Mathf.Pow((float)(p1.Item1 - p2.Item1), 2) + Mathf.Pow((float)(p1.Item2 - p2.Item2), 2));
    }


}

