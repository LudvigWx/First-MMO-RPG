using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// MOTORN för förmågor (sitter på PlayerArmature).
// - Håller dina 8 ability-slots (samma slots som hotbaren använder senare).
// - Kollar CENTRALT: cooldown, target, räckvidd och Rage innan en förmåga får gå.
// - Drar Rage och startar cooldown när förmågan faktiskt gick igång.
public class AbilityCaster : MonoBehaviour
{
    [Header("Ability-slots (Element 0 = tangent 1, Element 1 = tangent 2, osv.)")]
    public Ability[] slots = new Ability[8];

    [Header("Låsning")]
    [Tooltip("Så många slots från vänster är DEV-låsta (kan ej bytas in-game). 1 = slot 0 (Basic Attack).")]
    public int lockedSlotCount = 1;

    [Header("Referenser (hittas automatiskt)")]
    public TargetingController targeting;
    public CombatManager combat;
    public RageResource rage;
    public PlayerAttack playerAttack;

    private AbilityContext ctx;
    private float[] cooldownReady = new float[8];   // Time.time då varje slot är redo igen

    void Start()
    {
        if (targeting == null) targeting = GetComponent<TargetingController>();
        if (combat == null) combat = GetComponent<CombatManager>();
        if (rage == null) rage = GetComponent<RageResource>();
        if (playerAttack == null) playerAttack = GetComponent<PlayerAttack>();

        ctx = new AbilityContext
        {
            runner = this,
            player = transform,
            targeting = targeting,
            combat = combat,
            rage = rage,
            controller = GetComponent<CharacterController>(),
            cam = Camera.main,
            playerAttack = playerAttack,
            playerHealth = GetComponent<PlayerHealth>()
        };
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // Tangent 1–8 -> slot 0–7. (Slot 0 / tangent 1 = Basic Attack, nu en förmåga.)
        CheckKey(Keyboard.current.digit1Key, 0);
        CheckKey(Keyboard.current.digit2Key, 1);
        CheckKey(Keyboard.current.digit3Key, 2);
        CheckKey(Keyboard.current.digit4Key, 3);
        CheckKey(Keyboard.current.digit5Key, 4);
        CheckKey(Keyboard.current.digit6Key, 5);
        CheckKey(Keyboard.current.digit7Key, 6);
        CheckKey(Keyboard.current.digit8Key, 7);
    }

    void CheckKey(ButtonControl key, int slotIndex)
    {
        if (key.wasPressedThisFrame) TryCast(slotIndex);
    }

    // Försöker aktivera förmågan i en slot. Publik så hotbar-knappar kan anropa den senare.
    public void TryCast(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length) return;

        Ability ability = slots[slotIndex];
        if (ability == null)
        {
            Debug.Log("Slot " + (slotIndex + 1) + " är tom.");
            return;
        }

        // 1) Cooldown?
        if (Time.time < cooldownReady[slotIndex])
        {
            float kvar = cooldownReady[slotIndex] - Time.time;
            Debug.Log(ability.abilityName + " på cooldown (" + kvar.ToString("F1") + "s kvar).");
            return;
        }

        // 2) Kräver target?
        Enemy target = targeting != null ? targeting.CurrentTarget : null;
        if (ability.requiresTarget && (target == null || target.IsDead))
        {
            Debug.Log(ability.abilityName + ": ingen giltig target.");
            return;
        }

        // 3) Inom räckvidd?
        if (ability.requiresTarget && ability.range > 0f && target != null)
        {
            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist > ability.range)
            {
                Debug.Log(ability.abilityName + ": för långt bort (" + dist.ToString("F1") + "m, kräver " + ability.range + "m).");
                return;
            }
        }

        // 4) Nog med Rage?
        if (!rage.HasEnough(ability.rageCost))
        {
            Debug.Log(ability.abilityName + ": inte nog Rage (" + rage.currentRage + "/" + ability.rageCost + ").");
            return;
        }

        // 5) Kör förmågan
        bool fired = ability.Activate(ctx);
        if (!fired) return;   // förmågan avbröt sig själv -> inget dras

        // 6) Dra Rage + starta cooldown
        rage.Spend(ability.rageCost);
        cooldownReady[slotIndex] = Time.time + ability.cooldown;
    }

    // Hjälpmetoder som hotbar-UI:t använder senare för att rita cooldown-svepet:
    public float CooldownRemaining(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length) return 0f;
        return Mathf.Max(0f, cooldownReady[slotIndex] - Time.time);
    }

    public float CooldownFraction(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length || slots[slotIndex] == null) return 0f;
        float cd = slots[slotIndex].cooldown;
        if (cd <= 0f) return 0f;
        return Mathf.Clamp01(CooldownRemaining(slotIndex) / cd);
    }

    // ---- Slot-låsning (för den framtida in-game hotbar-editorn) ----
    // Låsta slots (t.ex. slot 0 = Basic Attack) kan bara ändras av oss i Inspectorn,
    // aldrig av spelaren in-game. Menyn ska anropa SetSlot/ClearSlot, som vägrar röra låsta.
    public bool IsSlotLocked(int index) => index < lockedSlotCount;

    public bool SetSlot(int index, Ability ability)
    {
        if (index < 0 || index >= slots.Length) return false;
        if (IsSlotLocked(index))
        {
            Debug.Log("Slot " + (index + 1) + " är låst (Basic Attack kan inte bytas in-game).");
            return false;
        }
        slots[index] = ability;
        return true;
    }

    public bool ClearSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return false;
        if (IsSlotLocked(index))
        {
            Debug.Log("Slot " + (index + 1) + " är låst.");
            return false;
        }
        slots[index] = null;
        return true;
    }
}
