using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A static utility that queues image loads and throttles requests to avoid overload or timeouts.
/// </summary>
public class ImageLoader : MonoBehaviour
{
    private class ImageRequest
    {
        public string url;
        public Image targetImage;
    }

    private static Queue<ImageRequest> requestQueue = new();
    private static bool isProcessing = false;
    private static GameObject loaderObject;

    public static void EnqueueImageLoad(string url, Image targetImage)
    {
        if (string.IsNullOrEmpty(url) || targetImage == null)
        {
            Debug.LogWarning("[ImageLoader] Invalid request.");
            return;
        }

        requestQueue.Enqueue(new ImageRequest { url = url, targetImage = targetImage });

        if (!isProcessing)
        {
            if (loaderObject == null)
            {
                loaderObject = new GameObject("ImageLoader");
                DontDestroyOnLoad(loaderObject);
                loaderObject.AddComponent<ImageLoader>();
            }

            loaderObject.GetComponent<ImageLoader>().StartCoroutine(ProcessQueue());
        }
    }

    private static IEnumerator ProcessQueue()
    {
        isProcessing = true;

        while (requestQueue.Count > 0)
        {
            var request = requestQueue.Dequeue();
            yield return LoadImage(request.url, request.targetImage);
            yield return new WaitForSeconds(0.05f); // slight delay between requests
        }

        isProcessing = false;
    }

    private static IEnumerator LoadImage(string url, Image targetImage)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(www);
            if (texture != null)
            {
                targetImage.sprite = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
            }
        }
        else
        {
            Debug.LogError($"[ImageLoader] Failed to load: {url} - {www.error}");
        }
    }
}
