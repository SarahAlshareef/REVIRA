using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class HUDLook : MonoBehaviour
{
    public Transform cameraTransform; // اسحبي الكاميرا هنا (CenterEyeAnchor)
    public float distanceFromCamera = 1.3f; // المسافة من اللاعب
    public float verticalOffset = 0.4f;     // كم يكون فوق مجال الرؤية
    public float horizontalFOV = 110f;      // مجال الرؤية الأفقي (للحساب)
    public float canvasHeight = 0.3f;       // ارتفاع الشريط بوحدات العالم

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        // احسب العرض اللي يغطي FOV
        float halfFOVRad = Mathf.Deg2Rad * (horizontalFOV / 2f);
        float totalWidth = 2f * Mathf.Tan(halfFOVRad) * distanceFromCamera;

        // اضبط الحجم بالـ World Units
        rectTransform.sizeDelta = new Vector2(totalWidth * 1000f, canvasHeight * 1000f); // لأن scale صغير
        transform.localScale = Vector3.one * 0.001f; // مقاس مناسب لـ VR
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // حط الشريط قدام الكاميرا + فوق شوي
        Vector3 offset = cameraTransform.forward * distanceFromCamera + cameraTransform.up * verticalOffset;
        transform.position = cameraTransform.position + offset;

        // خليه يواجه نفس اتجاه الكاميرا
        transform.rotation = Quaternion.LookRotation(cameraTransform.forward);
    }
}