using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

public class MmoCameraControl : MonoBehaviour
{
    public StarterAssetsInputs starterInputs;

    void Start()
    {
        if (starterInputs == null) starterInputs = GetComponent<StarterAssetsInputs>();
    }

    void Update()
    {
        // Håller spelaren in höger musknapp?
        bool rightHeld = Mouse.current != null && Mouse.current.rightButton.isPressed;

        // Kameran roterar bara medan höger musknapp hålls in
        starterInputs.cursorInputForLook = rightHeld;

        // När man släpper: nolla blicken så kameran inte glider vidare
        if (!rightHeld)
        {
            starterInputs.LookInput(Vector2.zero);
        }

        // Göm och lås muspekaren medan man roterar, visa den annars
        Cursor.lockState = rightHeld ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !rightHeld;
    }
}