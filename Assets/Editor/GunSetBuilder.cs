using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Phase 19.5: builds the three guns - their numbers on Weapon, their first-person models under
/// PlayerGunViewmodel, and the HUD line that lists them. Everything is written from the values in this
/// file, so it can be re-run after a change (Vantage > Rebuild Guns) and the scene never drifts from it.
///
/// The SMG and the Marksman are made from the rifle's own model (Kenney Blaster Kit, CC0): the same mesh
/// stretched or shortened, with the scope and magazine changed and a few plain blocks added. That keeps
/// all three in one art style without importing anything new. Positions are in metres from the
/// viewmodel's origin, +Z forward, measured off side-on renders of the model.
/// </summary>
public static class GunSetBuilder
{
    const string AccentFolder = "Assets/Art/Materials";

    [MenuItem("Vantage/Rebuild Guns")]
    public static void Rebuild()
    {
        var vm = Object.FindFirstObjectByType<GunViewmodelAnimator>(FindObjectsInactive.Include);
        if (vm == null || vm.weapon == null) { Debug.LogError("GunSetBuilder: no GunViewmodelAnimator with a Weapon in the scene."); return; }
        var weapon = vm.weapon;

        var rifle = vm.transform.Find("RifleVisual");
        var rifleMuzzle = vm.transform.Find("Muzzle");
        if (rifle == null || rifleMuzzle == null) { Debug.LogError("GunSetBuilder: RifleVisual or Muzzle missing under " + vm.name); return; }
        rifle.gameObject.SetActive(true);

        foreach (var name in new[] { "SmgVisual", "MarksmanVisual", "Muzzle_Smg", "Muzzle_Marksman" })
        {
            var old = vm.transform.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        var gunmetal = AssetDatabase.LoadAssetAtPath<Material>(AccentFolder + "/Mat_Gun_Gunmetal.mat");
        var teal = Accent("Mat_Gun_Accent_Smg", new Color(0.24f, 0.78f, 0.70f));
        var red = Accent("Mat_Gun_Accent_Marksman", new Color(0.80f, 0.22f, 0.18f));

        // The rifle model's barrel is on its -Z end, so unturned it pointed back at the player (stock towards the
        // enemy, tracers leaving the butt plate). Turned round here, and every gun below is a copy of it.
        rifle.localRotation = Quaternion.Euler(0f, 180f, 0f);
        rifle.localPosition = new Vector3(0f, 0f, 0.30f);
        rifleMuzzle.localPosition = new Vector3(0f, 0.027f, 0.62f);

        // ---- SMG: the rifle shortened, no scope, a long magazine, a foregrip and open sights.
        var smg = Group(vm.transform, "SmgVisual");
        var smgBody = CopyRifle(rifle, smg, new Vector3(0f, 0f, 0.26f), new Vector3(0.011f, 0.0092f, 0.007f));
        Remove(smgBody, "scope");
        var smgMag = smgBody.Find("magazine");
        smgMag.localScale = new Vector3(1f, 1.7f, 1.35f);
        smgMag.GetComponent<Renderer>().sharedMaterial = teal;
        Box(smg, "Foregrip", new Vector3(0f, -0.055f, 0.33f), new Vector3(0.03f, 0.08f, 0.034f), teal);
        Box(smg, "RearSight", new Vector3(0f, 0.082f, 0.10f), new Vector3(0.036f, 0.018f, 0.016f), gunmetal);
        Box(smg, "FrontSight", new Vector3(0f, 0.066f, 0.37f), new Vector3(0.01f, 0.04f, 0.012f), teal);
        var smgMuzzle = Point(vm.transform, "Muzzle_Smg", new Vector3(0f, 0.024f, 0.48f));
        smg.gameObject.SetActive(false);

        // ---- Marksman: the rifle stretched, a bigger scope, a long barrel with a brake, and a folded bipod.
        var mk = Group(vm.transform, "MarksmanVisual");
        var mkBody = CopyRifle(rifle, mk, new Vector3(0f, 0f, 0.36f), new Vector3(0.01f, 0.01f, 0.0118f));
        var mkScope = mkBody.Find("scope");
        mkScope.localScale = new Vector3(1.25f, 1.25f, 1.6f);
        mkScope.GetComponent<Renderer>().sharedMaterial = red;
        mkBody.Find("magazine").GetComponent<Renderer>().sharedMaterial = red;
        Cylinder(mk, "Barrel", new Vector3(0f, 0.027f, 0.86f), 0.011f, 0.30f, gunmetal);
        Box(mk, "Brake", new Vector3(0f, 0.027f, 1.03f), new Vector3(0.036f, 0.036f, 0.07f), red);
        Box(mk, "Bipod", new Vector3(0f, 0.01f, 0.74f), new Vector3(0.05f, 0.012f, 0.2f), gunmetal);
        var mkMuzzle = Point(vm.transform, "Muzzle_Marksman", new Vector3(0f, 0.027f, 1.07f));
        mk.gameObject.SetActive(false);

        // ---- Numbers. Index 0 is the rifle exactly as it was before there was a second gun.
        var rifleShot = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Gunshot/gunshot_punchy_blaster.wav");
        var smgShot = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Gunshot/gunshot_smg.wav");
        var mkShot = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Gunshot/gunshot_marksman.wav");
        if (smgShot == null || mkShot == null) Debug.LogWarning("GunSetBuilder: run Vantage > Audio > Rebuild Gunshot Sound first - the SMG or Marksman shot is missing.");

        weapon.guns = new[]
        {
            new GunProfile
            {
                displayName = "RIFLE", unlockRoomIndex = 0,
                maxAmmo = 12, fireRate = 0.25f, automatic = true, damage = 25f, reloadSeconds = 1.4f,
                standingSpreadDegrees = 0f, movingSpreadDegrees = 4.5f,
                recoilPitchPattern = new[] { 0.9f, 0.9f, 0.8f, 0.6f, 0.4f, 0.25f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f },
                recoilYawPattern = new[] { 0f, 0f, 0.1f, 0.3f, 0.5f, -0.7f, -0.7f, 0.7f, 0.7f, -0.7f, -0.7f },
                gunshotSound = rifleShot, gunshotVolume = 0.75f,
            },
            // Fast and forgiving on the move, weak per bullet: two eye hits to kill where the rifle needs one.
            // The gun for closing in on drones that are already blinded.
            new GunProfile
            {
                displayName = "SMG", unlockRoomIndex = 2,
                maxAmmo = 24, fireRate = 0.09f, automatic = true, damage = 12f, reloadSeconds = 1.1f,
                standingSpreadDegrees = 0.5f, movingSpreadDegrees = 2.5f,
                recoilPitchPattern = new[] { 0.35f, 0.35f, 0.3f, 0.3f, 0.25f, 0.2f, 0.15f, 0.12f, 0.1f, 0.1f },
                recoilYawPattern = new[] { 0f, 0.1f, 0.15f, -0.1f, -0.25f, 0.2f, 0.3f, -0.3f, -0.3f, 0.3f },
                gunshotSound = smgShot, gunshotVolume = 0.5f,
            },
            // One shot kills anywhere on the drone, then over a second before the next. Useless on the move.
            // The gun for a set-up shot: smoke or flash first, then take the angle.
            new GunProfile
            {
                displayName = "MARKSMAN", unlockRoomIndex = 4,
                maxAmmo = 4, fireRate = 1.1f, automatic = false, damage = 100f, reloadSeconds = 2.2f,
                standingSpreadDegrees = 0f, movingSpreadDegrees = 9f,
                recoilPitchPattern = new[] { 2.6f },
                recoilYawPattern = new[] { 0.3f },
                gunshotSound = mkShot, gunshotVolume = 0.85f,
            },
        };
        weapon.switchSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/InterfaceSounds/switch_002.ogg");
        EditorUtility.SetDirty(weapon);

        // Aim positions put each gun's sight line on the player's eye: the scope's axis for the scoped two, the top of the sights for the SMG.
        vm.gunVisuals = new[]
        {
            new GunViewmodelAnimator.GunVisual { root = rifle, magazine = rifle.Find("magazine"), muzzle = rifleMuzzle, aimPosition = new Vector3(0f, -0.104f, 0.2f), aimFieldOfView = 35f, scoped = true },
            // No scope: the gun comes up to the eye and stays in view, with a milder zoom.
            new GunViewmodelAnimator.GunVisual { root = smg, magazine = smgMag, muzzle = smgMuzzle, aimPosition = new Vector3(0f, -0.125f, 0.30f), aimFieldOfView = 48f, scoped = false },
            new GunViewmodelAnimator.GunVisual { root = mk, magazine = mkBody.Find("magazine"), muzzle = mkMuzzle, aimPosition = new Vector3(0f, -0.124f, 0.18f), aimFieldOfView = 20f, scoped = true },
        };
        EditorUtility.SetDirty(vm);

        BuildHudLabel(weapon);

        EditorSceneManager.MarkSceneDirty(vm.gameObject.scene);
        Debug.Log("GunSetBuilder: 3 guns rebuilt.");
    }

    static void BuildHudLabel(Weapon weapon)
    {
        var hud = Object.FindFirstObjectByType<HudUI>(FindObjectsInactive.Include);
        if (hud == null || hud.ammoLabel == null) return;
        var ammoPanel = (RectTransform)hud.ammoLabel.transform.parent;

        var old = ammoPanel.parent.Find("GunLabel_BG");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        // Same corner and backing as the ammo panel, sitting just above it.
        var panelGo = new GameObject("GunLabel_BG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var panel = (RectTransform)panelGo.transform;
        panel.SetParent(ammoPanel.parent, false);
        panel.SetSiblingIndex(ammoPanel.GetSiblingIndex() + 1);
        panel.anchorMin = ammoPanel.anchorMin;
        panel.anchorMax = ammoPanel.anchorMax;
        panel.pivot = ammoPanel.pivot;
        panel.anchoredPosition = ammoPanel.anchoredPosition + new Vector2(0f, ammoPanel.rect.height + 6f);
        panel.sizeDelta = new Vector2(430f, 34f);
        var backing = panelGo.GetComponent<Image>();
        var ammoBacking = ammoPanel.GetComponent<Image>();
        if (ammoBacking != null) { backing.sprite = ammoBacking.sprite; backing.color = ammoBacking.color; backing.type = ammoBacking.type; }
        backing.raycastTarget = false;

        var textGo = new GameObject("GunLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        var rect = (RectTransform)textGo.transform;
        rect.SetParent(panel, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(12f, 0f);
        rect.offsetMax = new Vector2(-12f, 0f);
        var text = textGo.GetComponent<Text>();
        text.font = hud.ammoLabel.font;
        text.fontSize = 17;
        text.alignment = TextAnchor.MiddleRight;
        text.color = new Color(0.62f, 0.62f, 0.6f, 1f);
        text.supportRichText = true;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.text = "<color=#E2A53C><b>[1] RIFLE</b></color>     [2] SMG     [3] MARKSMAN";

        hud.gunLabel = text;
        hud.gunLabelBacking = panelGo;
        EditorUtility.SetDirty(hud);
    }

    /// <summary>The rifle's gold accent material in another colour, saved as its own asset so the guns can be told apart at a glance.</summary>
    static Material Accent(string name, Color color)
    {
        string path = AccentFolder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            AssetDatabase.CopyAsset(AccentFolder + "/Mat_Gun_Accent.mat", path);
            mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        }
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Transform Group(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static Transform Point(Transform parent, string name, Vector3 localPosition)
    {
        var t = Group(parent, name);
        t.localPosition = localPosition;
        return t;
    }

    static Transform CopyRifle(Transform rifle, Transform parent, Vector3 localPosition, Vector3 localScale)
    {
        var copy = Object.Instantiate(rifle.gameObject, parent);
        copy.name = "Body";
        copy.transform.localPosition = localPosition;
        copy.transform.localRotation = rifle.localRotation;
        copy.transform.localScale = localScale;
        return copy.transform;
    }

    static void Remove(Transform parent, string child)
    {
        var t = parent.Find(child);
        if (t != null) Object.DestroyImmediate(t.gameObject);
    }

    static void Box(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material)
    {
        Primitive(PrimitiveType.Cube, parent, name, localPosition, size, Quaternion.identity, material);
    }

    /// <summary>A cylinder lying along the gun (its length on Z).</summary>
    static void Cylinder(Transform parent, string name, Vector3 localPosition, float radius, float length, Material material)
    {
        // Unity's cylinder is 2 units tall along Y.
        Primitive(PrimitiveType.Cylinder, parent, name, localPosition, new Vector3(radius * 2f, length * 0.5f, radius * 2f), Quaternion.Euler(90f, 0f, 0f), material);
    }

    static void Primitive(PrimitiveType type, Transform parent, string name, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        // A viewmodel part must never stop a bullet or a drone's line of sight.
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        go.transform.localScale = localScale;
        go.GetComponent<Renderer>().sharedMaterial = material;
    }
}
