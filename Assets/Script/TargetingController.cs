using UnityEngine;
using UnityEngine.InputSystem;

public class TargetingController : MonoBehaviour
{
    [Header("Referenser")]
    public Camera mainCamera;
    public LayerMask clickableLayers = ~0;

    public Enemy CurrentTarget { get; private set; }

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    void Update()
    {
        // Vänsterklick = försök välja (eller avmarkera om man klickar bredvid)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TrySelectTarget();
        }

        // Escape = avmarkera
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClearTarget();
        }
    }

    void TrySelectTarget()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, clickableLayers))
        {
            // Leta efter Enemy på träffat objekt ELLER någon av dess föräldrar - så att
            // klick på t.ex. en karaktärsmodells egna colliders (ben, kroppsdelar) också
            // träffar rätt om Enemy-scriptet sitter på ett föräldraobjekt.
            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                SetTarget(enemy);
                return;   // Klart — vi valde en fiende
            }
        }

        // Kom vi hit klickade vi INTE på en fiende → avmarkera
        ClearTarget();
    }

    public void SetTarget(Enemy enemy)
    {
        CurrentTarget = enemy;
        Debug.Log("Valde target: " + enemy.enemyName + " (Level " + enemy.level + ")");
    }

    public void ClearTarget()
    {
        CurrentTarget = null;
    }
}