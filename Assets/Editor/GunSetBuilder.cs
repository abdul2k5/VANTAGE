using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Phase 19.5/19.6: builds the three guns - their numbers on Weapon, their first-person models under
/// PlayerGunViewmodel, their pictures, the Choose Your Weapon screen the Start button opens, and the HUD
/// line naming the gun picked. Everything is written from the values in this file, so it can be re-run
/// after a change (Vantage > Rebuild Guns) and the scene never drifts from it.
///
/// The SMG and the Marksman are made from the rifle's own model (Kenney Blaster Kit, CC0): the same mesh
/// stretched or shortened, with the scope and magazine changed and a few plain blocks added. That keeps
/// all three in one art style without importing anything new. Positions are in metres from the
/// viewmodel's origin, +Z forward, measured off side-on renders of the model.
/// </summary>
public static class GunSetBuilder
{
    const string AccentFolder = "Assets/Art/Materials";
    const string PictureFolder = "Assets/Art/UI/Guns";
    static readonly Color Amber = new Color(0.886f, 0.647f, 0.235f);
    static readonly Color Teal = new Color(0.24f, 0.78f, 0.70f);
    static readonly Color Red = new Color(0.80f, 0.22f, 0.18f);

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

        // Back to the bare model first: the other two guns are copies of it, and must not inherit the rifle's carbine parts.
        var rifleMag = rifle.Find("magazine");
        Remove(rifle, "Kit");
        Remove(rifleMag, "MagCurve");
        Remove(rifleMag, "MagBase");
        rifle.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(AccentFolder + "/Mat_Gun_Gunmetal.mat");
        rifleMag.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(AccentFolder + "/Mat_Gun_Accent.mat");
        rifle.Find("scope").gameObject.SetActive(true);

        foreach (var name in new[] { "SmgVisual", "MarksmanVisual", "Muzzle_Smg", "Muzzle_Marksman" })
        {
            var old = vm.transform.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        // Phase 19.17: all three guns made to look less like toys. Real surfaces in place of one grey and one
        // bright colour: matte black polymer for bodies and furniture, steel for rails, magazines and small
        // parts, glass on the optics. Each gun keeps its card's colour, but only as a few small marks.
        var amber = AssetDatabase.LoadAssetAtPath<Material>(AccentFolder + "/Mat_Gun_Accent.mat");
        var teal = Accent("Mat_Gun_Accent_Smg", Teal);
        var red = Accent("Mat_Gun_Accent_Marksman", Red);
        var polymer = Surface("Mat_Gun_Polymer", new Color(0.085f, 0.09f, 0.10f), 0.05f, 0.32f);
        var steel = Surface("Mat_Gun_Steel", new Color(0.20f, 0.21f, 0.23f), 0.95f, 0.62f);
        var lens = Surface("Mat_Gun_Lens", new Color(0.04f, 0.10f, 0.18f), 0.9f, 0.96f);

        // The rifle model's barrel is on its -Z end, so unturned it pointed back at the player (stock towards the
        // enemy, tracers leaving the butt plate). Turned round here, and every gun below is a copy of it.
        rifle.localRotation = Quaternion.Euler(0f, 180f, 0f);
        rifle.localPosition = new Vector3(0f, 0f, 0.30f);
        rifleMuzzle.localPosition = new Vector3(0f, 0.027f, 0.62f);

        // ---- SMG: the rifle shortened, a long magazine, and a red-dot frame in place of the scope.
        var smg = Group(vm.transform, "SmgVisual");
        var smgBody = CopyRifle(rifle, smg, new Vector3(0f, 0f, 0.26f), new Vector3(0.011f, 0.0092f, 0.007f));
        Remove(smgBody, "scope");
        smgBody.GetComponent<Renderer>().sharedMaterial = polymer;
        var smgMag = smgBody.Find("magazine");
        smgMag.localScale = new Vector3(1f, 1.7f, 1.35f);
        smgMag.GetComponent<Renderer>().sharedMaterial = steel;
        MagazinePlate(vm.transform, smg, smgMag, teal);
        // After the designer's reference picture (19.16): one rail along the whole top with low flip-up sights
        // at each end, a slotted handguard, and a fat shroud over the muzzle.
        Box(smg, "Rail", new Vector3(0f, 0.095f, 0.28f), new Vector3(0.03f, 0.012f, 0.44f), steel);
        for (int tooth = 0; tooth < 18; tooth++)
            Box(smg, "RailTooth_" + tooth, new Vector3(0f, 0.103f, 0.07f + tooth * 0.024f), new Vector3(0.034f, 0.005f, 0.012f), steel);
        Box(smg, "RearSight", new Vector3(0f, 0.110f, 0.075f), new Vector3(0.034f, 0.012f, 0.014f), steel);
        Box(smg, "FrontSight", new Vector3(0f, 0.110f, 0.485f), new Vector3(0.034f, 0.012f, 0.014f), steel);
        Box(smg, "Handguard", new Vector3(0f, 0.045f, 0.405f), new Vector3(0.066f, 0.09f, 0.19f), polymer);
        for (int slot = 0; slot < 3; slot++)
            Box(smg, "HandguardSlot_" + slot, new Vector3(0f, 0.05f, 0.35f + slot * 0.055f), new Vector3(0.07f, 0.01f, 0.035f), steel);
        Cylinder(smg, "Shroud", new Vector3(0f, 0.037f, 0.56f), 0.041f, 0.13f, polymer);
        for (int groove = 0; groove < 3; groove++)
            Cylinder(smg, "ShroudGroove_" + groove, new Vector3(0f, 0.037f, 0.52f + groove * 0.03f), 0.0425f, 0.006f, steel);
        Cylinder(smg, "ShroudBand", new Vector3(0f, 0.037f, 0.615f), 0.043f, 0.012f, teal);
        // Red-dot optic: an open frame, so the target is seen through it. Its centre is on the line the eye
        // is brought to when aiming (y 0.125), and the flip-up sights stay under that line.
        Box(smg, "OpticBase", new Vector3(0f, 0.104f, 0.14f), new Vector3(0.06f, 0.008f, 0.07f), steel);
        Box(smg, "OpticLeft", new Vector3(-0.027f, 0.127f, 0.14f), new Vector3(0.006f, 0.05f, 0.055f), polymer);
        Box(smg, "OpticRight", new Vector3(0.027f, 0.127f, 0.14f), new Vector3(0.006f, 0.05f, 0.055f), polymer);
        Box(smg, "OpticTop", new Vector3(0f, 0.151f, 0.14f), new Vector3(0.06f, 0.006f, 0.055f), teal);
        // Small parts on the receiver, run right through it so they show on both sides.
        Box(smg, "EjectionPort", new Vector3(0f, 0.055f, 0.20f), new Vector3(0.114f, 0.018f, 0.05f), steel);
        Box(smg, "Selector", new Vector3(0f, 0.018f, 0.10f), new Vector3(0.116f, 0.008f, 0.016f), teal);
        var smgMuzzle = Point(vm.transform, "Muzzle_Smg", new Vector3(0f, 0.037f, 0.63f));
        smg.gameObject.SetActive(false);

        // ---- Marksman: the rifle stretched, a bigger scope, a long barrel with a brake, and a folded bipod.
        var mk = Group(vm.transform, "MarksmanVisual");
        var mkBody = CopyRifle(rifle, mk, new Vector3(0f, 0f, 0.36f), new Vector3(0.01f, 0.01f, 0.0118f));
        mkBody.GetComponent<Renderer>().sharedMaterial = polymer;
        var mkScope = mkBody.Find("scope");
        mkScope.localScale = new Vector3(1.25f, 1.25f, 1.6f);
        mkScope.GetComponent<Renderer>().sharedMaterial = polymer;
        var mkMag = mkBody.Find("magazine");
        mkMag.GetComponent<Renderer>().sharedMaterial = steel;
        MagazinePlate(vm.transform, mk, mkMag, red);
        // Glass at both ends of the scope and a red turret on top, placed from where the scaled scope actually is.
        Vector3 scopeMin, scopeMax;
        BoundsIn(vm.transform, mkScope, out scopeMin, out scopeMax);
        float tubeRadius = (scopeMax.x - scopeMin.x) * 0.5f * 0.52f;
        float tubeY = scopeMax.y - tubeRadius;
        Cylinder(mk, "ScopeFrontLens", new Vector3(0f, tubeY, scopeMax.z + 0.001f), tubeRadius * 0.9f, 0.004f, lens);
        Cylinder(mk, "ScopeRearLens", new Vector3(0f, tubeY, scopeMin.z - 0.001f), tubeRadius * 0.9f, 0.004f, lens);
        Box(mk, "ScopeTurretTop", new Vector3(0f, scopeMax.y - 0.004f, (scopeMin.z + scopeMax.z) * 0.5f), new Vector3(0.024f, 0.02f, 0.03f), red);
        Box(mk, "ScopeTurretSide", new Vector3(tubeRadius + 0.006f, tubeY, (scopeMin.z + scopeMax.z) * 0.5f), new Vector3(0.02f, 0.024f, 0.03f), polymer);
        // A free-float handguard tube with vent slots, then the long bare barrel and its brake.
        Cylinder(mk, "Handguard", new Vector3(0f, 0.04f, 0.61f), 0.036f, 0.34f, polymer);
        for (int vent = 0; vent < 4; vent++)
            Box(mk, "HandguardVent_" + vent, new Vector3(0f, 0.04f, 0.50f + vent * 0.07f), new Vector3(0.076f, 0.01f, 0.04f), steel);
        Cylinder(mk, "HandguardCap", new Vector3(0f, 0.04f, 0.784f), 0.038f, 0.012f, steel);
        Cylinder(mk, "Barrel", new Vector3(0f, 0.027f, 0.86f), 0.013f, 0.30f, steel);
        Cylinder(mk, "Brake", new Vector3(0f, 0.027f, 1.03f), 0.022f, 0.07f, steel);
        Cylinder(mk, "BrakeBand", new Vector3(0f, 0.027f, 0.992f), 0.024f, 0.01f, red);
        Box(mk, "BrakeSlot", new Vector3(0f, 0.027f, 1.035f), new Vector3(0.048f, 0.008f, 0.03f), polymer);
        // Bipod folded back under the barrel: a clamp and two legs.
        Box(mk, "BipodClamp", new Vector3(0f, 0.012f, 0.82f), new Vector3(0.05f, 0.02f, 0.03f), steel);
        Cylinder(mk, "BipodLegL", new Vector3(-0.02f, 0.004f, 0.72f), 0.006f, 0.2f, steel);
        Cylinder(mk, "BipodLegR", new Vector3(0.02f, 0.004f, 0.72f), 0.006f, 0.2f, steel);
        // Receiver parts and a cheek rest.
        Box(mk, "EjectionPort", new Vector3(0f, 0.058f, 0.30f), new Vector3(0.104f, 0.02f, 0.08f), steel);
        Box(mk, "BoltHandle", new Vector3(0.062f, 0.058f, 0.27f), new Vector3(0.03f, 0.012f, 0.012f), steel);
        Box(mk, "Selector", new Vector3(0f, 0.02f, 0.09f), new Vector3(0.106f, 0.008f, 0.02f), red);
        Box(mk, "CheekRest", new Vector3(0f, 0.088f, -0.14f), new Vector3(0.05f, 0.026f, 0.13f), polymer);
        var mkMuzzle = Point(vm.transform, "Muzzle_Marksman", new Vector3(0f, 0.027f, 1.07f));
        mk.gameObject.SetActive(false);

        // ---- Rifle: dressed as a service carbine, so it no longer reads as a short Marksman. The tall scope
        // is swapped for a compact optic on a rail, with a handguard, a front sight post, a solid stock on a
        // buffer tube and a curved magazine. Done last, after the copies above were taken from the bare model.
        rifle.Find("scope").gameObject.SetActive(false);
        var kit = Group(rifle, "Kit");
        // The model is scaled and turned round; undo both so the parts below are placed in the viewmodel's metres like the other guns'.
        kit.SetPositionAndRotation(vm.transform.position, vm.transform.rotation);
        kit.localScale = new Vector3(1f / rifle.localScale.x, 1f / rifle.localScale.y, 1f / rifle.localScale.z);
        // Amber is kept to three small marks so the gun still belongs to its amber card.
        rifle.GetComponent<Renderer>().sharedMaterial = polymer;
        rifleMag.GetComponent<Renderer>().sharedMaterial = steel;

        // Top rail with its cross-slots.
        Box(kit, "Rail", new Vector3(0f, 0.106f, 0.19f), new Vector3(0.03f, 0.008f, 0.34f), steel);
        for (int tooth = 0; tooth < 14; tooth++)
            Box(kit, "RailTooth_" + tooth, new Vector3(0f, 0.113f, 0.035f + tooth * 0.024f), new Vector3(0.034f, 0.006f, 0.012f), steel);

        // Compact optic. Its axis sits where the old scope's did, so aiming brings the same point to the eye.
        Box(kit, "OpticMount", new Vector3(0f, 0.122f, 0.20f), new Vector3(0.026f, 0.018f, 0.09f), steel);
        Cylinder(kit, "OpticTube", new Vector3(0f, 0.146f, 0.20f), 0.02f, 0.13f, polymer);
        Cylinder(kit, "OpticFront", new Vector3(0f, 0.146f, 0.285f), 0.026f, 0.04f, polymer);
        Cylinder(kit, "OpticRear", new Vector3(0f, 0.146f, 0.115f), 0.022f, 0.035f, polymer);
        Cylinder(kit, "OpticFrontLens", new Vector3(0f, 0.146f, 0.306f), 0.022f, 0.004f, lens);
        Cylinder(kit, "OpticRearLens", new Vector3(0f, 0.146f, 0.097f), 0.018f, 0.004f, lens);
        Box(kit, "OpticTurretTop", new Vector3(0f, 0.172f, 0.20f), new Vector3(0.018f, 0.016f, 0.02f), amber);
        Box(kit, "OpticTurretSide", new Vector3(0.026f, 0.146f, 0.20f), new Vector3(0.016f, 0.018f, 0.02f), polymer);

        // Round ribbed handguard between a delta ring and an end cap. It stops short of the muzzle so a length
        // of bare barrel shows, with the sight tower standing on it.
        Cylinder(kit, "Handguard", new Vector3(0f, 0.042f, 0.535f), 0.037f, 0.25f, polymer);
        for (int rib = 0; rib < 7; rib++)
            Cylinder(kit, "HandguardRib_" + rib, new Vector3(0f, 0.042f, 0.44f + rib * 0.032f), 0.0395f, 0.012f, polymer);
        Cylinder(kit, "DeltaRing", new Vector3(0f, 0.042f, 0.405f), 0.042f, 0.022f, steel);
        Cylinder(kit, "HandguardCap", new Vector3(0f, 0.042f, 0.664f), 0.04f, 0.012f, steel);

        // Front sight tower: two legs leaning together, a bridge and a post.
        Box(kit, "FrontSightBase", new Vector3(0f, 0.07f, 0.70f), new Vector3(0.03f, 0.02f, 0.04f), steel);
        Primitive(PrimitiveType.Cube, kit, "FrontSightLegA", new Vector3(0f, 0.099f, 0.689f), new Vector3(0.012f, 0.056f, 0.01f), Quaternion.Euler(18f, 0f, 0f), steel);
        Primitive(PrimitiveType.Cube, kit, "FrontSightLegB", new Vector3(0f, 0.099f, 0.711f), new Vector3(0.012f, 0.056f, 0.01f), Quaternion.Euler(-18f, 0f, 0f), steel);
        Box(kit, "FrontSightBridge", new Vector3(0f, 0.126f, 0.70f), new Vector3(0.014f, 0.008f, 0.02f), steel);
        Box(kit, "FrontSightPost", new Vector3(0f, 0.134f, 0.70f), new Vector3(0.004f, 0.012f, 0.004f), steel);

        // Small parts on the receiver. Each runs right through it, so it shows on both sides.
        Box(kit, "EjectionPort", new Vector3(0f, 0.062f, 0.25f), new Vector3(0.108f, 0.02f, 0.07f), steel);
        Box(kit, "MagRelease", new Vector3(0f, 0.0f, 0.30f), new Vector3(0.108f, 0.012f, 0.012f), steel);
        Box(kit, "Selector", new Vector3(0f, 0.02f, 0.06f), new Vector3(0.108f, 0.008f, 0.02f), amber);
        Box(kit, "ChargingHandle", new Vector3(0f, 0.098f, 0.005f), new Vector3(0.05f, 0.01f, 0.03f), steel);

        // Stock: a body on the buffer tube, a butt plate, and the sloped lower edge.
        Cylinder(kit, "BufferTube", new Vector3(0f, 0.05f, -0.10f), 0.016f, 0.2f, steel);
        Box(kit, "StockBody", new Vector3(0f, 0.055f, -0.16f), new Vector3(0.052f, 0.05f, 0.16f), polymer);
        Box(kit, "StockButt", new Vector3(0f, 0.012f, -0.232f), new Vector3(0.056f, 0.14f, 0.035f), polymer);
        Primitive(PrimitiveType.Cube, kit, "StockLowerEdge", new Vector3(0f, -0.01f, -0.152f), new Vector3(0.046f, 0.02f, 0.148f), Quaternion.Euler(-32.6f, 0f, 0f), polymer);

        // The lower half of the magazine, swept forward, with an amber base plate. Both hang off the magazine
        // itself so they drop with it on a reload.
        Primitive(PrimitiveType.Cube, kit, "MagCurve", new Vector3(0f, -0.185f, 0.235f), new Vector3(0.046f, 0.08f, 0.115f), Quaternion.Euler(-18f, 0f, 0f), steel);
        Primitive(PrimitiveType.Cube, kit, "MagBase", new Vector3(0f, -0.2287f, 0.2492f), new Vector3(0.05f, 0.012f, 0.12f), Quaternion.Euler(-18f, 0f, 0f), amber);
        kit.Find("MagCurve").SetParent(rifleMag, true);
        kit.Find("MagBase").SetParent(rifleMag, true);

        // ---- Numbers. Index 0 is the rifle exactly as it was before there was a second gun.
        var rifleShot = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Gunshot/gunshot_punchy_blaster.wav");
        var smgShot = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Gunshot/gunshot_smg.wav");
        var mkShot = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Gunshot/gunshot_marksman.wav");
        if (smgShot == null || mkShot == null) Debug.LogWarning("GunSetBuilder: run Vantage > Audio > Rebuild Gunshot Sound first - the SMG or Marksman shot is missing.");

        var pictures = RenderPictures(new[] { rifle, smg, mk }, new[] { "gun_rifle", "gun_smg", "gun_marksman" });

        weapon.guns = new[]
        {
            new GunProfile
            {
                displayName = "RIFLE", picture = pictures[0],
                role = "The all-rounder. Dead accurate when you stop, and one shot to the eye kills.",
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
                displayName = "SMG", picture = pictures[1],
                role = "For pushing. Fast and steady on the move, but weak per bullet. Red-dot sight.",
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
                displayName = "MARKSMAN", picture = pictures[2],
                role = "For holding an angle. One shot kills anywhere, but only if you stand still.",
                maxAmmo = 4, fireRate = 1.1f, automatic = false, damage = 100f, reloadSeconds = 4f,
                standingSpreadDegrees = 0f, movingSpreadDegrees = 9f,
                recoilPitchPattern = new[] { 2.6f },
                recoilYawPattern = new[] { 0.3f },
                gunshotSound = mkShot, gunshotVolume = 1f,
            },
        };
        EditorUtility.SetDirty(weapon);

        // Aim positions put each gun's sight line on the player's eye: the scope's axis for the scoped two, the top of the sights for the SMG.
        vm.gunVisuals = new[]
        {
            new GunViewmodelAnimator.GunVisual { root = rifle, magazine = rifleMag, muzzle = rifleMuzzle, aimPosition = new Vector3(0f, -0.104f, 0.2f), aimFieldOfView = 35f, scoped = true },
            // Red dot, not a scope: the gun comes up so the eye looks through the optic's frame, and stays in view, with a milder zoom than the scoped two.
            new GunViewmodelAnimator.GunVisual { root = smg, magazine = smgMag, muzzle = smgMuzzle, aimPosition = new Vector3(0f, -0.125f, 0.30f), aimFieldOfView = 42f, scoped = false },
            new GunViewmodelAnimator.GunVisual { root = mk, magazine = mkMag, muzzle = mkMuzzle, aimPosition = new Vector3(0f, -0.124f, 0.18f), aimFieldOfView = 20f, scoped = true },
        };
        EditorUtility.SetDirty(vm);

        BuildHudLabel(weapon);
        BuildLoadoutScreen(weapon);

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
        panel.sizeDelta = new Vector2(ammoPanel.rect.width, 34f);
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
        text.fontStyle = FontStyle.Bold;
        text.text = weapon.guns[0].displayName;

        hud.gunLabel = text;
        hud.gunLabelBacking = panelGo;
        EditorUtility.SetDirty(hud);
    }

    /// <summary>
    /// Side-on pictures of the guns for their cards, rendered from the first-person models themselves so a
    /// picture can never show something different from the gun in hand. All three share one scale, so the
    /// SMG looks short and the Marksman long.
    /// </summary>
    static Sprite[] RenderPictures(Transform[] visuals, string[] names)
    {
        const int width = 1024, height = 424;
        if (!AssetDatabase.IsValidFolder(PictureFolder)) AssetDatabase.CreateFolder("Assets/Art/UI", "Guns");

        // Far below the level, so nothing else is in shot.
        var stage = new Vector3(0f, -500f, 0f);
        var copies = new GameObject[visuals.Length];
        var bounds = new Bounds[visuals.Length];
        float halfLength = 0f, halfHeight = 0f;
        for (int i = 0; i < visuals.Length; i++)
        {
            copies[i] = Object.Instantiate(visuals[i].gameObject);
            copies[i].transform.SetPositionAndRotation(stage, visuals[i].localRotation);
            copies[i].SetActive(true);
            var renderers = copies[i].GetComponentsInChildren<Renderer>();
            bounds[i] = renderers[0].bounds;
            for (int r = 1; r < renderers.Length; r++) bounds[i].Encapsulate(renderers[r].bounds);
            halfLength = Mathf.Max(halfLength, bounds[i].extents.z);
            halfHeight = Mathf.Max(halfHeight, bounds[i].extents.y);
            copies[i].SetActive(false);
        }

        var camGo = new GameObject("GunPictureCamera");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 6f;

        // A key light from the camera's side and a softer one from below-behind, so the dark gunmetal keeps its shape.
        var key = NewLight("GunPictureKey", new Vector3(-1f, -0.7f, 0.35f), 2f);
        var fill = NewLight("GunPictureFill", new Vector3(-0.6f, 0.8f, -0.5f), 0.6f);

        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var sprites = new Sprite[visuals.Length];
        for (int i = 0; i < visuals.Length; i++)
        {
            copies[i].SetActive(true);
            // Half-way between one scale for all three and filling the frame: the SMG still looks short, but not tiny.
            float shared = Mathf.Max(halfHeight, halfLength * height / width);
            float own = Mathf.Max(bounds[i].extents.y, bounds[i].extents.z * height / width);
            cam.orthographicSize = (shared + own) * 0.5f * 1.06f;
            // Looking along -X puts the barrel (+Z) on the right of the picture.
            camGo.transform.position = bounds[i].center + Vector3.right * 3f;
            camGo.transform.rotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            copies[i].SetActive(false);

            string path = PictureFolder + "/" + names[i] + ".png";
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(key);
        Object.DestroyImmediate(fill);
        for (int i = 0; i < copies.Length; i++) Object.DestroyImmediate(copies[i]);
        return sprites;
    }

    static GameObject NewLight(string name, Vector3 direction, float intensity)
    {
        var go = new GameObject(name);
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = intensity;
        light.shadows = LightShadows.None;
        go.transform.rotation = Quaternion.LookRotation(direction);
        return go;
    }

    /// <summary>
    /// Choose Your Weapon: one card per gun, with its picture, what it is for and its numbers read off the
    /// GunProfile, so the card can't disagree with the gun. Sits inside the menu panel, on top of it; the
    /// Start button opens it and a card starts the run.
    /// </summary>
    static void BuildLoadoutScreen(Weapon weapon)
    {
        var menu = Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
        var hud = Object.FindFirstObjectByType<HudUI>(FindObjectsInactive.Include);
        if (menu == null || menu.menuPanel == null || hud == null) { Debug.LogError("GunSetBuilder: no main menu to put the weapon screen on."); return; }
        var font = hud.ammoLabel.font;

        var old = menu.menuPanel.transform.Find("LoadoutPanel");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var panel = UiRect("LoadoutPanel", menu.menuPanel.transform);
        Stretch(panel);
        // Nearly opaque: the menu's buttons underneath must not show through or take a click.
        var backing = panel.gameObject.AddComponent<Image>();
        backing.color = new Color(0.03f, 0.04f, 0.06f, 0.96f);

        var title = Label(panel, "Title", "CHOOSE YOUR WEAPON", font, 56, FontStyle.Bold, Amber, TextAnchor.MiddleCenter);
        Place(title.rectTransform, new Vector2(0f, 420f), new Vector2(1200f, 70f));
        var sub = Label(panel, "Subtitle", "You carry it for the whole run.   Click a card, or press 1, 2 or 3.", font, 24, FontStyle.Normal, new Color(0.72f, 0.72f, 0.7f), TextAnchor.MiddleCenter);
        Place(sub.rectTransform, new Vector2(0f, 362f), new Vector2(1200f, 36f));

        var accents = new[] { Amber, Teal, Red };
        for (int i = 0; i < weapon.guns.Length; i++)
        {
            var gun = weapon.guns[i];
            var card = UiRect("Card_" + gun.displayName, panel);
            Place(card, new Vector2((i - 1) * 540f, -50f), new Vector2(500f, 700f));
            var cardImage = card.gameObject.AddComponent<Image>();
            cardImage.color = Color.white;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = cardImage;
            var colors = button.colors;
            colors.normalColor = new Color(0.09f, 0.10f, 0.13f, 1f);
            colors.highlightedColor = new Color(0.17f, 0.19f, 0.24f, 1f);
            colors.pressedColor = new Color(0.24f, 0.26f, 0.32f, 1f);
            colors.selectedColor = colors.normalColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            UnityEventTools.AddIntPersistentListener(button.onClick, menu.ChooseGun, i);

            var bar = UiRect("Accent", card);
            Place(bar, new Vector2(0f, 346f), new Vector2(500f, 8f));
            Plain(bar, accents[i]);

            var key = Label(card, "Key", "[" + (i + 1) + "]", font, 24, FontStyle.Bold, accents[i], TextAnchor.MiddleLeft);
            Place(key.rectTransform, new Vector2(-190f, 310f), new Vector2(80f, 36f));

            var well = UiRect("PictureWell", card);
            Place(well, new Vector2(0f, 190f), new Vector2(460f, 200f));
            Plain(well, new Color(0.27f, 0.30f, 0.36f, 1f));
            var picture = UiRect("Picture", well);
            Stretch(picture, 4f);
            var pictureImage = picture.gameObject.AddComponent<Image>();
            pictureImage.sprite = gun.picture;
            pictureImage.preserveAspect = true;
            pictureImage.raycastTarget = false;

            var name = Label(card, "Name", gun.displayName, font, 44, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            Place(name.rectTransform, new Vector2(0f, 22f), new Vector2(460f, 56f));
            var role = Label(card, "Role", gun.role, font, 22, FontStyle.Normal, new Color(0.78f, 0.78f, 0.76f), TextAnchor.UpperCenter);
            role.horizontalOverflow = HorizontalWrapMode.Wrap;
            Place(role.rectTransform, new Vector2(0f, -52f), new Vector2(430f, 70f));

            string[] labels = { "DAMAGE", "FIRE RATE", "MAGAZINE", "RELOAD", "ON THE MOVE" };
            string[] values =
            {
                gun.damage.ToString("0"),
                (1f / gun.fireRate).ToString(gun.fireRate > 0.5f ? "0.0" : "0") + " / sec" + (gun.automatic ? "" : "  (one per click)"),
                gun.maxAmmo.ToString(),
                gun.reloadSeconds.ToString("0.0") + " s",
                gun.movingSpreadDegrees <= 3f ? "Steady" : gun.movingSpreadDegrees <= 6f ? "Loose" : "Wild",
            };
            for (int row = 0; row < labels.Length; row++)
            {
                float y = -130f - row * 40f;
                var label = Label(card, "Stat_" + labels[row], labels[row], font, 21, FontStyle.Normal, new Color(0.6f, 0.6f, 0.58f), TextAnchor.MiddleLeft);
                Place(label.rectTransform, new Vector2(-115f, y), new Vector2(200f, 34f));
                var value = Label(card, "Value_" + labels[row], values[row], font, 23, FontStyle.Bold, Color.white, TextAnchor.MiddleRight);
                Place(value.rectTransform, new Vector2(85f, y), new Vector2(260f, 34f));
            }
        }

        panel.gameObject.SetActive(false);
        menu.loadoutPanel = panel.gameObject;
        menu.weapon = weapon;
        EditorUtility.SetDirty(menu);

        // The Start button used to start the run itself.
        var start = menu.menuPanel.transform.Find("StartButton");
        if (start == null)
        {
            foreach (var b in menu.menuPanel.GetComponentsInChildren<Button>(true)) if (b.name == "StartButton") start = b.transform;
        }
        var startButton = start != null ? start.GetComponent<Button>() : null;
        if (startButton == null) { Debug.LogError("GunSetBuilder: StartButton not found - wire it to MainMenuController.ShowLoadout by hand."); return; }
        for (int i = startButton.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(startButton.onClick, i);
        UnityEventTools.AddPersistentListener(startButton.onClick, menu.ShowLoadout);
        EditorUtility.SetDirty(startButton);
    }

    static RectTransform UiRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.layer = parent.gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    /// <summary>Centre-anchored: position is from the parent's centre.</summary>
    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static void Plain(RectTransform rect, Color color)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    static Text Label(Transform parent, string name, string content, Font font, int size, FontStyle style, Color color, TextAnchor anchor)
    {
        var rect = UiRect(name, parent);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = anchor;
        text.text = content;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
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

    /// <summary>A plain surface (colour, how metallic, how glossy) saved as its own material, made from the gunmetal one so it keeps the same shader and settings.</summary>
    static Material Surface(string name, Color color, float metallic, float smoothness)
    {
        string path = AccentFolder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            AssetDatabase.CopyAsset(AccentFolder + "/Mat_Gun_Gunmetal.mat", path);
            mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        }
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    /// <summary>The corners of a part's box, in the viewmodel's metres. The guns are only ever turned half-way round, so the box stays square to the axes.</summary>
    static void BoundsIn(Transform space, Transform part, out Vector3 min, out Vector3 max)
    {
        var b = part.GetComponent<MeshFilter>().sharedMesh.bounds;
        min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        max = -min;
        for (int i = 0; i < 8; i++)
        {
            var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            var p = space.InverseTransformPoint(part.TransformPoint(corner));
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
    }

    /// <summary>A base plate in the gun's colour under its magazine. It hangs off the magazine, so it drops with it on a reload.</summary>
    static void MagazinePlate(Transform space, Transform group, Transform magazine, Material material)
    {
        Vector3 min, max;
        BoundsIn(space, magazine, out min, out max);
        Box(group, "MagBase", new Vector3((min.x + max.x) * 0.5f, min.y + 0.008f, (min.z + max.z) * 0.5f), new Vector3(max.x - min.x + 0.004f, 0.012f, max.z - min.z + 0.004f), material);
        group.Find("MagBase").SetParent(magazine, true);
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
