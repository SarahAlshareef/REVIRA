
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("space between menu items")]
    [SerializeField] Vector2 spacing = new Vector2(0, -100);


    Button mainButton;
    SettingsMenuItem[] menuItems;

    //is menu opened or not
    bool isExpanded = false;

    Vector2 mainButtonPosition;
    int itemsCount;

    void Start()
    {
        //add all the items to the menuItems array
        itemsCount = transform.childCount - 1;
        menuItems = new SettingsMenuItem[itemsCount];
        for (int i = 0; i < itemsCount; i++)
        {
            // +1 to ignore the main button
            menuItems[i] = transform.GetChild(i + 1).GetComponent<SettingsMenuItem>();
            menuItems[i].gameObject.SetActive(false);
        }

        mainButton = transform.GetChild(0).GetComponent<Button>();
        if (mainButton != null)
        {

            mainButton.onClick.AddListener(ToggleMenu);

            //SetAsLastSibling () to make sure that the main button will be always at the top layer
            mainButton.transform.SetAsLastSibling();
            mainButtonPosition = mainButton.GetComponent<RectTransform>().anchoredPosition;
        }
        else
        {
            Debug.LogError("Main Button component is missing!");
        }
        //set all menu items position to mainButtonPosition
    }
    void ToggleMenu()
    {
        isExpanded = !isExpanded;
        for (int i = 0; i < itemsCount; i++)
        {
            menuItems[i].gameObject.SetActive(isExpanded);

            if (isExpanded)
            {
                Vector2 targetPosition = isExpanded ? mainButtonPosition + spacing * (i + 1) : mainButtonPosition;
                menuItems[i].rectTrans.position = targetPosition;
            }
        }
    }
    void OnDestroy()
    {
        if (mainButton != null)
        {
            //remove click listener to avoid memory leaks
            mainButton.onClick.RemoveListener(ToggleMenu);
        }
    }
}
