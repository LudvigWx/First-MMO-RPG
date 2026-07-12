using UnityEngine;

// En "verktygslåda" med referenser som förmågorna behöver för att kunna göra något.
// AbilityCaster bygger den EN gång och skickar in den i varje ability.Activate(...).
// På så vis slipper varje förmåga själv leta reda på target, combat, rörelse osv.
public class AbilityContext
{
    public MonoBehaviour runner;           // för att kunna starta coroutines (t.ex. rörelse)
    public Transform player;               // spelarens transform
    public TargetingController targeting;  // vem som är vald
    public CombatManager combat;
    public RageResource rage;
    public CharacterController controller;  // för rörelse-förmågor (Charge)
    public Camera cam;
    public PlayerAttack playerAttack;       // för Basic Attack-toggle (används i hotbar-steget)
    public PlayerHealth playerHealth;       // för buffs som Shield Wall (skademinskning)
}
