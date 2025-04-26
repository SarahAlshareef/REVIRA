using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class ProfileImageManager : MonoBehaviour
{
    public static ProfileImageManager Instance { get; private set; }

    private List<Image> registeredImages = new List<Image>();

    private const string MaleImageURL = "https://firebasestorage.googleapis.com/v0/b/fir-unity-29721.firebasestorage.app/o/Advertisements_images%2Fimage%201.jpeg?alt=media&token=99a17cc2-9dfe-49fc-8e57-695a5f63d05a";
    private const string FemaleImageURL = "https://firebasestorage.googleapis.com/v0/b/fir-unity-29721.firebasestorage.app/o/Advertisements_images%2Fimage%202.jpeg?alt=media&token=545918b9-6a74-4f14-b6f3-dfbc7fc6e06b";

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void RegisterProfileImage(Image img)
    {
        if (!registeredImages.Contains(img))
            registeredImages.Add(img);

        UpdateImage(img);
    }

    public void UpdateAllProfileImages()
    {
        foreach (var img in registeredImages)
        {
            UpdateImage(img);
        }
    }

    private void UpdateImage(Image img)
    {
        string gender = UserManager.Instance?.Gender?.ToLower();

        if (gender == "male")
        {
            StartCoroutine(LoadProfileImage(MaleImageURL, img));
        }
        else if (gender == "female")
        {
            StartCoroutine(LoadProfileImage(FemaleImageURL, img));
        }
        else
        {
            Debug.LogWarning("Gender not set correctly. Profile image not updated.");
        }
    }

    private IEnumerator LoadProfileImage(string url, Image img)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            img.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
        else
        {
            Debug.LogError("Failed to load profile image: " + request.error);
        }
    }
}
