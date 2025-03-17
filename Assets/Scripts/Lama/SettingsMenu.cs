
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("space between menu items ")]
    [SerializeField] Vector2 spacing;

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
        }
        mainButton = transform.GetChild(0).GetComponent<Button>();
        mainButton.onClick.AddListener(ToggleMenu);

        //SetAsLastSibling () to make sure that the main button will be always at the top layer
        mainButton.transform.SetAsLastSibling();

        mainButtonPosition = mainButton.transform.position;

        //set all menu items position to mainButtonPosition
        ResetPositions();
    }

    void ResetPositions()
    {
        for (int i = 0; i < itemsCount; i++)
        {
            menuItems[i].trans.position = mainButtonPosition;
        }
    }

    void ToggleMenu()
    {

        isExpanded = !isExpanded;
        if (isExpanded)
        {   //menu opened
            for (int i = 0; i < itemsCount; i++)
            {
                menuItems[i].trans.position = mainButtonPosition + spacing * (i + 1);

            }
        }
        else
        { //menu closed

            for (int i = 0; i < itemsCount; i++)
            {
                menuItems[i].trans.position = mainButtonPosition;
            }
        }
    }
    void OnDestroy()
    {
        //remove click listener to avoid memory leaks
        mainButton.onClick.RemoveListener(ToggleMenu);

    }
}

