using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.Events;
using UnityEngine.Networking;
#if ENABLE_WINMD_SUPPORT
using Windows.Storage;
using Windows.Storage.Streams;
#endif

public class GetTimeValue : MonoBehaviour
{
  [SerializeField]
  string task = "Task_x";
  [SerializeField]
  float currentTime = 0.0f;
  [SerializeField]
  string CurrObject = "Default";
  public bool canRecord = false;

  public UnityEvent on_Started;
  Time_Logs CurrLogs;
  // Start is called before the first frame update
  void Start()
  {
    CurrLogs = new Time_Logs();
    CurrLogs.task = this.task;
    CurrLogs.logs = new List<Time_Object>();
  }

  // Update is called once per frame
  void Update()
  {
    if (canRecord)
    {
      currentTime += Time.deltaTime;
    }
  }

  public void StartRecording()
  {
    canRecord = true;
    on_Started.Invoke();
  }

  public void UpdateObj(string obj)
  {
    if (obj != CurrObject)
    {
      canRecord = false;
      Time_Object newTime = new Time_Object();
      newTime.Name = CurrObject;
      newTime.Time = currentTime;
      CurrLogs.logs.Add(newTime);
      currentTime = 0.0f;
      CurrObject = obj;
      canRecord = true;
    }
  }

  public async void SaveIntoJson()
  {
    canRecord = false;
    // Add final object
    Time_Object newTime = new Time_Object();
    newTime.Name = CurrObject;
    newTime.Time = currentTime;
    CurrLogs.logs.Add(newTime);
    StartCoroutine(Upload(JsonUtility.ToJson(CurrLogs)));
    // string potion = JsonUtility.ToJson(CurrLogs);
    // string path = System.IO.Path.Combine(Application.persistentDataPath, task + ".json");
    // using (StreamWriter sw = new StreamWriter(path, false))
    // {
    //   sw.WriteLine(potion);
    //   sw.Close();
    // }

  }
  IEnumerator Upload(string jsonString)
  {
    WWWForm form = new WWWForm();
    form.AddField("title", task);
    form.AddField("content", jsonString);

    using (UnityWebRequest www = UnityWebRequest.Post("https://next-jshl-2.vercel.app/api/addData", form))
    {
      yield return www.SendWebRequest();

      if (www.result != UnityWebRequest.Result.Success)
      {
        Debug.Log(www.error);
      }
      else
      {
        Debug.Log("Form upload complete!");
      }
    }
  }
}
