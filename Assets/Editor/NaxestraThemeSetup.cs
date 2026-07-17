using UnityEngine;
using UnityEditor;
using System;
using System.IO;

// Skapar en startuppsättning theme-assets från "Fantasy Wooden GUI: Free"-paketet och
// kopplar in dem i PlayerHudUI.prefab / HotbarUI.prefab. Kör via menyn:
// Tools > Naxestra > Setup Fantasy Wooden Theme
//
// Säker att köra om flera gånger — befintliga assets/kopplingar skrivs inte över, så du
// kan duplicera FantasyWooden_*-assets och peka om enskilda prefab-fält utan att detta
// script återställer dina ändringar.
public static class NaxestraThemeSetup
{
    private const string PackageFolder = "Assets/Fantasy Wooden GUI  Free/normal_ui_set A/";
    private const string ThemesFolder = "Assets/Themes/FantasyWooden";
    private const string PlayerHudPrefabPath = "Assets/Prefabs (Woxstrm)/PlayerHudUI.prefab";
    private const string HotbarPrefabPath = "Assets/Prefabs (Woxstrm)/HotbarUI.prefab";

    [MenuItem("Tools/Naxestra/Setup Fantasy Wooden Theme")]
    public static void Setup()
    {
        EnsureFolder(ThemesFolder);

        Sprite buttonNormal = LoadPackageSprite("TextBTN_Medium.png");
        Sprite buttonPressed = LoadPackageSprite("TextBTN_Medium_Pressed.png");
        Sprite panelBackground = LoadPackageSprite("UI board Medium  parchment.png");

        // Paketet innehåller inga bar-/slot-sprites — använder Unitys inbyggda rundade
        // platshållar-sprite istället (samma sprite Unitys egna Button/Panel-verktyg använder).
        Sprite barBackground = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        Sprite barFill = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        CreateOrLoad<ButtonTheme>(ThemesFolder + "/FantasyWooden_Button.asset", t =>
        {
            t.normalSprite = buttonNormal;
            t.pressedSprite = buttonPressed;
            t.tint = Color.white;
            t.textColor = new Color(0.22f, 0.14f, 0.07f);
        });

        CreateOrLoad<PanelTheme>(ThemesFolder + "/FantasyWooden_Panel.asset", t =>
        {
            t.backgroundSprite = panelBackground;
            t.tint = Color.white;
            t.borderColor = Color.white;
        });

        // fillSprite lämnas tom (null) med flit: fyllnads-Imagen använder Image.Type.Filled
        // (för att maskera hur mycket av baren som är fylld), vilket INTE stöder 9-slice —
        // en rundad-hörn-sprite (som barFill) skulle bli en förvrängd "kudde"/lins när baren
        // är bred och tunn. Med fillSprite=null behåller ThemedBar/PlayerHudUI istället den
        // befintliga platta platshållar-spriten och byter bara färg.
        BarTheme healthBarTheme = CreateOrLoad<BarTheme>(ThemesFolder + "/FantasyWooden_HealthBar.asset", t =>
        {
            t.backgroundSprite = barBackground;
            t.backgroundTint = new Color(0.08f, 0.08f, 0.08f, 0.9f);
            t.fillColor = Color.red;
        });

        BarTheme rageBarTheme = CreateOrLoad<BarTheme>(ThemesFolder + "/FantasyWooden_RageBar.asset", t =>
        {
            t.backgroundSprite = barBackground;
            t.backgroundTint = new Color(0.08f, 0.08f, 0.08f, 0.9f);
            t.fillColor = new Color(1f, 0.55f, 0f);
        });

        BarTheme xpBarTheme = CreateOrLoad<BarTheme>(ThemesFolder + "/FantasyWooden_XPBar.asset", t =>
        {
            t.backgroundSprite = barBackground;
            t.backgroundTint = new Color(0.08f, 0.08f, 0.08f, 0.9f);
            t.fillColor = new Color(0.4f, 0.7f, 1f);
        });

        SlotTheme slotTheme = CreateOrLoad<SlotTheme>(ThemesFolder + "/FantasyWooden_Slot.asset", t =>
        {
            t.emptySprite = barBackground;
            t.emptyTint = new Color(0.08f, 0.08f, 0.08f, 0.9f);
            t.highlightedSprite = barFill;
            t.highlightedTint = new Color(0.85f, 0.65f, 0.15f);
        });

        AssetDatabase.SaveAssets();

        WirePlayerHudPrefab(healthBarTheme, rageBarTheme, xpBarTheme);
        WireHotbarPrefab(slotTheme);

        EditorUtility.DisplayDialog("Klart",
            "Fantasy Wooden-teman skapade i " + ThemesFolder + ".\n\n" +
            "Kör om \"Tools > Naxestra > Build Escape Menu\" för att applicera Button/Panel-temat på Escape-menyns knappar och paneler.",
            "OK");
    }

    private static T CreateOrLoad<T>(string path, Action<T> configure) where T : ScriptableObject
    {
        T existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;

        T asset = ScriptableObject.CreateInstance<T>();
        configure(asset);
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static Sprite LoadPackageSprite(string fileName)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PackageFolder + fileName);
        if (sprite == null)
            Debug.LogWarning("NaxestraThemeSetup: Hittade inte sprite \"" + fileName + "\" i " + PackageFolder);
        return sprite;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string folderName = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static void WirePlayerHudPrefab(BarTheme healthTheme, BarTheme rageTheme, BarTheme xpTheme)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerHudPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogWarning("NaxestraThemeSetup: Hittade inte " + PlayerHudPrefabPath);
            return;
        }

        PlayerHudUI hud = prefabRoot.GetComponentInChildren<PlayerHudUI>(true);
        if (hud != null)
        {
            if (hud.healthBarTheme == null) hud.healthBarTheme = healthTheme;
            if (hud.rageBarTheme == null) hud.rageBarTheme = rageTheme;
            if (hud.xpBarTheme == null) hud.xpBarTheme = xpTheme;
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerHudPrefabPath);
        }
        else
        {
            Debug.LogWarning("NaxestraThemeSetup: " + PlayerHudPrefabPath + " saknar PlayerHudUI-komponenten.");
        }

        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    private static void WireHotbarPrefab(SlotTheme slotTheme)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(HotbarPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogWarning("NaxestraThemeSetup: Hittade inte " + HotbarPrefabPath);
            return;
        }

        HotbarUI hotbar = prefabRoot.GetComponentInChildren<HotbarUI>(true);
        if (hotbar != null)
        {
            if (hotbar.abilitySlotTheme == null) hotbar.abilitySlotTheme = slotTheme;
            if (hotbar.quickSlotTheme == null) hotbar.quickSlotTheme = slotTheme;
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, HotbarPrefabPath);
        }
        else
        {
            Debug.LogWarning("NaxestraThemeSetup: " + HotbarPrefabPath + " saknar HotbarUI-komponenten.");
        }

        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }
}
