using Proyecto26;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

//Firebase class to connect app to firebase
public class Firebase : MonoBehaviour
{
    public string url;

    //Posts data to Firebase database using the REST client
    public void PostToDatabase(DataClass data)
    {
        RestClient.Post(url + data.name + ".json", data);
    }
}
