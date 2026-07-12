using UnityEngine;

// BAS-RECEPTET för alla förmågor.
// Detta är ett abstrakt ScriptableObject – du skapar aldrig en "Ability" direkt,
// utan konkreta typer som ärver den (t.ex. ChargeAbility). Varje faktisk förmåga
// blir sedan ett eget data-objekt (.asset) i Unity som du fyller i.
public abstract class Ability : ScriptableObject
{
    [Header("Info")]
    public string abilityName = "Ny förmåga";
    [TextArea] public string description;
    public Sprite icon;                 // placeholder tills vi har konst

    [Header("Regler")]
    public bool isFreeFiller = false;   // ENDAST Basic Attack ska ha denna ibockad (gratis, ingen cooldown)
    public float cooldown = 5f;         // sekunder mellan användningar
    public int rageCost = 20;           // Rage som krävs och dras
    public bool requiresTarget = true;  // måste man ha en fiende vald?
    public float range = 5f;            // max avstånd till målet för att få användas (0 = spelar ingen roll)

    // Körs när förmågan aktiveras. Returnera true om den faktiskt gick igång
    // (då drar AbilityCaster Rage + startar cooldown). Returnera false för att avbryta.
    public abstract bool Activate(AbilityContext ctx);

    // Påminner dig i Unity om REGELN: allt utom Basic Attack måste ha cooldown OCH Rage-kostnad.
    void OnValidate()
    {
        if (!isFreeFiller && (cooldown <= 0f || rageCost <= 0))
        {
            Debug.LogWarning(name + ": Alla förmågor UTOM Basic Attack måste ha BÅDE cooldown (>0) OCH Rage-kostnad (>0).");
        }
    }
}
