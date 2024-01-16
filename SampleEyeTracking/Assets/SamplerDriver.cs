using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SamplerDriver : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Running Sampler");
        var s = new Sampler();
        // Log the serialized pointsDict["tier_1"] with indentation
        Debug.Log(JsonUtility.ToJson(s.pointsDict["tier_1"], true));

        // Log each key-value pair in pointsDict["tier_1"]
        foreach (var kvp in s.pointsDict["tier_1"])
        {
            Debug.Log($"{kvp.Key}: {JsonUtility.ToJson(kvp.Value)}");
        }

        // Log each key-value pair in pointsPool
        foreach (var kvp in s.pointsPool)
        {
            // Log the serialized value in pointsPool
            Debug.Log($"{kvp.Key}: {JsonUtility.ToJson(kvp.Value)}");

            // Extract point and log its coordinates
            var p = ((double, double))kvp.Value["point"];
            Debug.Log($"{kvp.Key}: {Mathf.Round(float.Parse(kvp.Key) / 10000) * 10000}");
            var y = Mathf.Round(float.Parse(kvp.Key) / 10000) * 10000 / 1000000 - 24;
            var x = (float.Parse(kvp.Key) - (y + 24) * 1000000) / 100;
            Debug.Log($"{x}, {y}");
        }
    }

}
