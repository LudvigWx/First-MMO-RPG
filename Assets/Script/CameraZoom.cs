using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraZoom : MonoBehaviour
{
    public CinemachineThirdPersonFollow thirdPersonFollow;

    [Header("Zoom-inställningar")]
    public float zoomSpeed = 1.5f;   // Hur mycket per scroll-steg
    public float minDistance = 1.5f; // Närmast inzoomat
    public float maxDistance = 8f;   // Längst utzoomat

    void Start()
    {
        if (thirdPersonFollow == null)
            thirdPersonFollow = GetComponent<CinemachineThirdPersonFollow>();
    }

    void Update()
    {
        if (thirdPersonFollow == null || Mouse.current == null) return;

        // Läs scrollhjulet (positivt = scrolla upp)
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            // Scrolla upp = zooma in (mindre avstånd)
            float step = Mathf.Sign(scroll) * zoomSpeed;
            float newDist = thirdPersonFollow.CameraDistance - step;
            thirdPersonFollow.CameraDistance = Mathf.Clamp(newDist, minDistance, maxDistance);
        }
    }
}