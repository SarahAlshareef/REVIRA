using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using Microsoft.MixedReality.Toolkit.Experimental.UI;

public class ShowKeyboard : MonoBehaviour, IPointerClickHandler
{
    private TMP_InputField inputField;

    public float distance =  1.0f;
    public float verticalOffset = -0.5f;

    public Transform positionSource;

    // Start is called before the first frame update
    void Start()
    {
        inputField = GetComponent<TMP_InputField>();
        inputField.onSelect.AddListener(x => OpenKeyboard());
    }

    public void OpenKeyboard()
    {
        NonNativeKeyboard.Instance.InputField = inputField;
        NonNativeKeyboard.Instance.PresentKeyboard(inputField.text);

        Vector3 direction = positionSource.forward;
        direction.y = 0;
        direction.Normalize();

        Vector3 targetPosition = positionSource.position + direction * distance + Vector3.up * verticalOffset;
        NonNativeKeyboard.Instance.RepositionKeyboard(targetPosition);
    
}

    public void OnPointerClick(PointerEventData eventData)
    {
        if (NonNativeKeyboard.Instance != null)
        {
            NonNativeKeyboard.Instance.PresentKeyboard(inputField.text);
        }
    }
}
