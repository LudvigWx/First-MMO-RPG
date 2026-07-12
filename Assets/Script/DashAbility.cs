using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

public class DashAbility : MonoBehaviour
{
    [Header("Referenser")]
    public CombatManager combat;
    public CharacterController controller;
    public StarterAssetsInputs starterInputs;
    public Camera mainCamera;

    [Header("Dash-inställningar")]
    public float dashSpeed = 14f;      // hur snabb dashen är
    public float dashDuration = 0.2f;  // hur länge den varar
    public float dashCooldown = 2f;    // sekunder mellan dashar

    private bool isDashing = false;
    private float nextDashTime = 0f;

    void Start()
    {
        // Hittar allt automatiskt på samma objekt / huvudkameran
        if (combat == null) combat = GetComponent<CombatManager>();
        if (controller == null) controller = GetComponent<CharacterController>();
        if (starterInputs == null) starterInputs = GetComponent<StarterAssetsInputs>();
        if (mainCamera == null) mainCamera = Camera.main;
    }

    void Update()
    {
        // Tangent Q = dash (lätt att byta: t.ex. spaceKey, leftShiftKey)
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
        {
            TryDash();
        }
    }

    void TryDash()
    {
        // 1) Dash tillåts BARA i combat mot en boss
        if (combat == null || !combat.DashAllowed)
        {
            Debug.Log("Dash inte tillgänglig (bara i strid mot en boss).");
            return;
        }

        // 2) Cooldown?
        if (Time.time < nextDashTime)
        {
            float kvar = nextDashTime - Time.time;
            Debug.Log("Dash på cooldown (" + kvar.ToString("F1") + "s kvar).");
            return;
        }

        // 3) Redan mitt i en dash?
        if (isDashing) return;

        nextDashTime = Time.time + dashCooldown;
        StartCoroutine(DashRoutine());
    }

    IEnumerator DashRoutine()
    {
        isDashing = true;

        Vector3 dir = GetDashDirection();
        Debug.Log("DASH! 💨");

        float t = 0f;
        while (t < dashDuration)
        {
            controller.Move(dir * dashSpeed * Time.deltaTime);
            t += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
    }

    Vector3 GetDashDirection()
    {
        Vector2 move = starterInputs.move;

        // Håller du en rörelsetangent? Dasha åt det hållet (kamera-relativt)
        if (move.sqrMagnitude > 0.01f)
        {
            Vector3 camF = mainCamera.transform.forward; camF.y = 0f; camF.Normalize();
            Vector3 camR = mainCamera.transform.right; camR.y = 0f; camR.Normalize();
            Vector3 dir = camF * move.y + camR * move.x;
            return dir.normalized;
        }

        // Ingen input → dasha framåt (dit karaktären tittar)
        return transform.forward;
    }
}