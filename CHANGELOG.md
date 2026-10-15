# Vantage — Design Changelog

Running log of design decisions and the reasoning behind them, kept for the Game Notes feedback section. Each entry says what changed and why, not just what was built — see git log for full implementation detail.

## Redesign: Valorant-inspired tactical FPS, new core dynamic

**What changed:** Re-scoped the whole prototype around one explicit core dynamic — **"Utility before you peek."** The player should never win by running in; every encounter is meant to be: use an ability to create a safe window (block a sightline, blind, reveal, or outrun), then commit to the gunfight. Previously the game had abilities and Sentinels but no single mechanic tying them together — this redesign makes that the organizing principle for every room, system, and HUD element going forward.

**Why:** The coursework grades "Core Dynamic" at 20% — the single highest-weighted line item after Fulfilling the Brief. A prototype with good individual systems but no unifying mechanic doesn't score well there. Naming the dynamic explicitly, and being willing to flag features that don't reinforce it instead of building them anyway, is meant to keep every subsequent phase pointed at that grade criterion specifically.

**IP note:** Explicitly Valorant-*inspired*, not a clone — no Riot assets, audio, icons, fonts, or names. All ability/objective names are original and built to fit Vantage's own fiction (the facility clearance-drill premise established earlier).

---

## Phase 1: Gunplay that rewards stopping

**What changed:**
- Firing while moving now applies random spread that scales with current horizontal speed (`Weapon.cs`: `standingSpreadDegrees` / `movingSpreadDegrees` / `fullSpeedForMaxSpread`, all Inspector-exposed). Standing still is perfectly accurate by default.
- The crosshair widens in step with current spread (`CrosshairController.SetSpreadFraction`), so inaccuracy is something the player can *see*, not just feel.
- Headshots: each Sentinel's red eye is a separate hit collider carrying a new `Weakpoint` component (2.5x damage multiplier) — one headshot kills (62.5 dmg vs 50 hp), confirmed against a body shot's 2-hit kill.
- Hit marker (white flash, scales up red for headshots) and a red damage-direction indicator that rotates to point at the actual world-space source of incoming damage.
- A headshot sound *hook* is wired (`Weapon.headshotSound` + `AudioSource`) but no clip is assigned yet — audio is Phase 6.

**Why:** This is the mechanical backbone of "utility before you peek." If movement doesn't cost accuracy, there's no reason to ever stop, which means there's no moment where an ability's safe window actually matters. Headshots exist so a well-earned peek (after using utility) is rewarded with a fast, clean kill rather than a damage trade — that's the "commit" half of the loop paying off.

**Bug found during testing, not assumed fixed:** the Sentinel's body hitbox (a capsule, radius 0.35) fully enclosed the eye's position at its default size — every raycast aimed at the eye hit the body collider first, making headshots geometrically impossible regardless of aim precision. Fixed by shrinking the body capsule (radius 0.3) and pulling its center back (-0.1 on its local forward axis) so the eye collider is no longer occluded. Caught by testing an actual aimed raycast against the live scene, not by code review.

**What to playtest:** `movingSpreadDegrees` (currently 4.5°) and `fullSpeedForMaxSpread` (5, matches move speed) — tune until "stop to be accurate" feels like a real tactical choice rather than either a non-issue or overly punishing. Also worth checking the headshot hitbox feels fair at different ranges/angles now that it's been widened from its initial (too small) default.
