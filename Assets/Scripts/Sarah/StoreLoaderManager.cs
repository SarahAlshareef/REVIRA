using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Storage;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class StoreLoaderManager : MonoBehaviour
{
    private DatabaseReference dbRef;
    private FirebaseStorage storage;

    public GameObject storePagePrefab;
    public Transform scrollContent;
    public GameObject storePopupPrefab;
    public GameObject constructionPopupPrefab;
    public Canvas mainCanvas;

    private Dictionary<string, StoreData> storeDataDict = new();

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == Firebase.DependencyStatus.Available)
            {
                dbRef = FirebaseDatabase.DefaultInstance.GetReference("REVIRA/stores");
                storage = FirebaseStorage.DefaultInstance;
                LoadStoresFromFirebase();
            }
        });
    }

    void LoadStoresFromFirebase()
    {
        dbRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                foreach (DataSnapshot storeSnapshot in snapshot.Children)
                {
                    string storeId = storeSnapshot.Key;
                    var storeDict = storeSnapshot.Value as Dictionary<string, object>;
                    if (storeDict == null) continue;

                    string name = storeDict["name"].ToString();
                    string description = storeDict["description"].ToString();
                    string imageUrl = storeDict["image"].ToString();
                    string sceneName = storeDict["scene"].ToString();

                    // Safe and correct way to parse bool
                    bool isUnderConstruction = false;
                    if (storeDict.TryGetValue("isUnderConstruction", out object rawFlag))
                    {
                        isUnderConstruction = rawFlag is bool b && b;
                    }

                    Debug.Log($"Loaded Store: {name}, UnderConstruction: {isUnderConstruction}");

                    StoreData data = new StoreData
                    {
                        StoreId = storeId,
                        Name = name,
                        Description = description,
                        ImageUrl = imageUrl,
                        SceneName = sceneName,
                        IsUnderConstruction = isUnderConstruction
                    };

                    storeDataDict[storeId] = data;
                    CreateStorePage(data);
                }
            }
        });
    }

    void CreateStorePage(StoreData data)
    {
        GameObject page = Instantiate(storePagePrefab, scrollContent);
        Image storeImage = page.transform.Find("StoreImage").GetComponent<Image>();
        Button selectButton = page.transform.Find("SelectButton").GetComponent<Button>();

        StartCoroutine(LoadImage(data.ImageUrl, storeImage));

        selectButton.onClick.AddListener(() =>
        {
            if (data.IsUnderConstruction)
            {
                ShowConstructionPopup();
            }
            else
            {
                ShowPopup(data);
            }
        });
    }

    void ShowPopup(StoreData data)
    {
        if (storePopupPrefab == null || mainCanvas == null) return;

        GameObject popup = Instantiate(storePopupPrefab, mainCanvas.transform);

        Transform popupWindow = popup.transform.Find("pop up window");
        if (popupWindow == null) return;

        var nameField = popupWindow.Find("StoreName")?.GetComponent<TextMeshProUGUI>();
        if (nameField == null) return;
        nameField.text = data.Name;

        var descriptionField = popupWindow.Find("Description")?.GetComponent<TextMeshProUGUI>();
        if (descriptionField == null) return;
        descriptionField.text = data.Description;

        Image popupImage = popupWindow.Find("StoreImage")?.GetComponent<Image>();
        if (popupImage == null) return;
        StartCoroutine(LoadImage(data.ImageUrl, popupImage));

        Button enterBtn = popupWindow.Find("Enter effect button (3)/EnterButton")?.GetComponent<Button>();
        Button cancelBtn = popupWindow.Find("cancel effect button (4)/CancelButton")?.GetComponent<Button>();
        if (enterBtn == null || cancelBtn == null) return;

        enterBtn.onClick.AddListener(() => SceneManager.LoadScene(data.SceneName));
        cancelBtn.onClick.AddListener(() => Destroy(popup));
    }

    void ShowConstructionPopup()
    {
        if (constructionPopupPrefab == null || mainCanvas == null) return;

        GameObject popup = Instantiate(constructionPopupPrefab, mainCanvas.transform);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(popup.GetComponent<RectTransform>());

        Button[] allButtons = popup.GetComponentsInChildren<Button>(true);
        foreach (var btn in allButtons)
        {
            if (btn.name == "BackButton")
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => Destroy(popup));
                break;
            }
        }
    }

    IEnumerator LoadImage(string firebaseUrl, Image targetImage)
    {
        if (string.IsNullOrEmpty(firebaseUrl) || storage == null)
            yield break;

        var storageRef = storage.GetReferenceFromUrl(firebaseUrl);
        var task = storageRef.GetDownloadUrlAsync();

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
            yield break;

        UnityWebRequest request = UnityWebRequestTexture.GetTexture(task.Result.ToString());
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
            yield break;

        Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
        if (texture == null)
            yield break;

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        targetImage.sprite = sprite;
    }

    class StoreData
    {
        public string StoreId;
        public string Name;
        public string Description;
        public string ImageUrl;
        public string SceneName;
        public bool IsUnderConstruction;
    }
}
