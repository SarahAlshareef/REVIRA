using UnityEngine;

public class HUDAbsoluteLock : MonoBehaviour
{
    [Header("المراجع")]
    public Transform cameraTransform;       // الكاميرا (CenterEyeAnchor)
    public RectTransform hudPanel;          // الشريط نفسه (Blue background)

    [Header("الإعدادات")]
    public float forwardDistance = 1.3f;    // كم يبعد قدام النظر
    public float verticalOffset = 0.5f;     // كم فوق العين
    public float hudWidthInMeters = 2.0f;   // عرض الشريط
    public float hudHeightInMeters = 0.25f; // سماكة الشريط

    private Canvas parentCanvas;

    void Start()
    {
        if (cameraTransform == null || hudPanel == null)
        {
            Debug.LogWarning("HUDAbsoluteLock: تأكدي من تعيين الكاميرا والشريط.");
            return;
        }

        // نضبط الحجم بناءً على المقياس
        parentCanvas = hudPanel.GetComponentInParent<Canvas>();
        float canvasScale = parentCanvas.transform.localScale.x;

        float width = hudWidthInMeters / canvasScale;
        float height = hudHeightInMeters / canvasScale;

        hudPanel.sizeDelta = new Vector2(width * 1000f, height * 1000f); // 1000 عشان units داخل الـ Canvas
        hudPanel.localScale = Vector3.one;
    }

    void LateUpdate()
    {
        if (cameraTransform == null || hudPanel == null) return;

        Vector3 offset = cameraTransform.forward * forwardDistance + cameraTransform.up * verticalOffset;

        hudPanel.position = cameraTransform.position + offset;
        hudPanel.rotation = Quaternion.LookRotation(cameraTransform.forward);
    }
}
