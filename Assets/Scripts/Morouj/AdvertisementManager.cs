using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using System.Collections;

public class AdvertisementManager : MonoBehaviour
{
    [Header("Poster 1")]
    public RawImage poster1Image;
    public Texture defaultPoster1;

    [Header("Poster 2")]
    public RawImage poster2Image;
    public Texture defaultPoster2;

    private string storeID = "storeID_123";
    private DatabaseReference dbRef;

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        LoadAdvertisement("Poster1", poster1Image, defaultPoster1);
        LoadAdvertisement("Poster2", poster2Image, defaultPoster2);
    }

    void LoadAdvertisement(string posterName, RawImage posterUI, Texture defaultImage)
    {
        dbRef.Child("REVIRA").Child("stores").Child(storeID).Child("Advertisements").Child(posterName)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    var data = task.Result;
                    string imageUrl = data.Child("imagePath").Value.ToString();
                    string startDate = data.Child("startDate").Value.ToString();
                    string endDate = data.Child("endDate").Value.ToString();
                    bool isActive = Convert.ToBoolean(data.Child("isActive").Value);

                    DateTime now = DateTime.Now;
                    DateTime start = DateTime.Parse(startDate);
                    DateTime end = DateTime.Parse(endDate);

                    if (isActive && now >= start && now <= end)
                    {
                        StartCoroutine(DownloadImage(imageUrl, posterUI, defaultImage));
                    }
                    else
                    {
                        posterUI.texture = defaultImage;
                    }
                }
                else
                {
                    posterUI.texture = defaultImage;
                }
            });
    }

    IEnumerator DownloadImage(string url, RawImage target, Texture fallback)
    {
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                target.texture = fallback;
            }
            else
            {
                Texture downloaded = ((DownloadHandlerTexture)uwr.downloadHandler).texture;
                target.texture = downloaded;
            }
        }
    }
}