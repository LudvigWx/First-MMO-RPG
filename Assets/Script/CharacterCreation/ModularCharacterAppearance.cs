using UnityEngine;

// Tunn wrapper runt en instansierad "Modular Character"-prefab från GanzSe-paketet.
// Hittar "FACE DETAILS PARTS" (samma objekt som ModularHeroController.facePartsRoot
// pekar på) och kategorierna under den (HAIRS/EYEBROWS/EYES/FACE HAIRS/EARS/NOSES).
// Varje kategori har alla varianter som barn, en aktiv i taget (SetActive) - det är
// paketets egna mönster, vi återanvänder det men bläddrar ett steg i taget istället
// för att slumpa (som ModularHeroController.RandomizeFaceParts gör).
public class ModularCharacterAppearance
{
    private readonly Transform facePartsRoot;

    public ModularCharacterAppearance(Transform characterRoot)
    {
        facePartsRoot = characterRoot.Find("FACE DETAILS PARTS");
        if (facePartsRoot == null)
        {
            Debug.LogWarning("ModularCharacterAppearance: hittade inte \"FACE DETAILS PARTS\" under " + characterRoot.name + ". Kontrollera att rätt prefab användes.");
        }
    }

    public bool IsValid => facePartsRoot != null;

    public string GetActiveVariantName(string categoryName)
    {
        Transform category = facePartsRoot != null ? facePartsRoot.Find(categoryName) : null;
        if (category == null) return "(kategori saknas)";

        for (int i = 0; i < category.childCount; i++)
        {
            if (category.GetChild(i).gameObject.activeSelf) return category.GetChild(i).name;
        }
        return "(ingen aktiv)";
    }

    // direction = 1 (nästa) eller -1 (föregående), snurrar runt i båda ändar.
    public void Cycle(string categoryName, int direction)
    {
        Transform category = facePartsRoot != null ? facePartsRoot.Find(categoryName) : null;
        if (category == null || category.childCount == 0) return;

        int activeIndex = 0;
        for (int i = 0; i < category.childCount; i++)
        {
            if (category.GetChild(i).gameObject.activeSelf)
            {
                activeIndex = i;
                break;
            }
        }

        int nextIndex = (activeIndex + direction + category.childCount) % category.childCount;
        if (nextIndex == activeIndex) return;

        category.GetChild(activeIndex).gameObject.SetActive(false);
        category.GetChild(nextIndex).gameObject.SetActive(true);
    }
}
