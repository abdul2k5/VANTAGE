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

**Addendum:** added auto-reload at 0 ammo (no need to press R when empty) per your request after playtesting Phase 1.

---

## Phase 2: Smarter Sentinels

**What changed:**
- New `Sentinel.BehaviorMode` (Patrol / Hold), Inspector-configurable per Sentinel. Hold mode stops the NavMeshAgent and faces an optional `holdPoint` Transform — a Sentinel can now watch a doorway or angle instead of always walking a route.
- A reaction delay (random 0.3-0.5s, both Inspector-exposed) between a Sentinel first spotting the player and its first shot. Previously it fired the instant it saw you, which made a well-timed peek or ability unable to ever land safely.
- A looping, 3D-spatial hover-hum `AudioSource` on every Sentinel (loop on, spatial blend 1, 2-14 unit falloff). No clip assigned yet — that's Phase 6 — but the component is live and will start humming the moment a clip is set.

**Why:** All three serve "utility before you peek" directly. Hold turns some encounters into a held angle you have to out-think rather than a patrol route you just wait out — closer to how a real defended doorway should feel. The reaction delay is what makes Smoke/Flash/Dash actually *work* mechanically: if a Sentinel fires instantly on sight, no ability-driven peek can ever beat it, which would make the core dynamic feel broken rather than rewarded. The audio hook sets up Sentinels being a presence you track by ear before you've used any utility at all - audio as another input to the "do I have a safe window" decision, not just sight.

**What to playtest once a sound clip exists:** reaction delay window (0.3-0.5s) against your actual Flash/Smoke timings - it should be tight enough to matter but not so generous every fight is trivial once you've peeked once.

---

## Phase 3: Recon Dart + Ultimate ("Overwatch Protocol")

**What changed:**
- **Recon Dart** (F, unlocked from the start - see note below): a thrown projectile with real physics (Rigidbody, gravity, a 1.5s fuse if it never lands). On landing it pulses and reveals every Sentinel within 8 units through walls for 3s - shown as a screen-space marker that tracks each revealed Sentinel's position even when occluded, no custom shader needed. Built as a new `Ability` subclass, slots into the existing `AbilityHudUI` as a 4th slot (now first in the row, matching Phase 5's planned unlock order).
- **Ultimate - "Overwatch Protocol"** (X): a charge-based (not cooldown-based) system. Charges 25% per kill - 4 kills to full - plus a new `UltOrbPickup` component for placing bonus-charge orbs in risky spots (mechanism built now; actual placement is a Phase 5 room-design decision). At full charge, activating reveals every Sentinel in the level (not just nearby - a genuinely wide reveal, confirmed working across rooms 40+ units apart) and halves incoming damage for 6 seconds. A new 5-pip HUD readout shows charge building and turns gold when ready.
- `Sentinel.cs` gained a `Reveal()`/`IsRevealed` pair, mirroring the existing `Blind()` pattern - both Recon Dart and the ultimate just call into it.
- `PlayerHealth.cs` gained a `DamageMultiplier` property (separate from the existing `Invulnerable` flag) so the ultimate's damage reduction and Dash's full invulnerability can coexist as distinct mechanics.

**Why:** Recon Dart is "utility before you peek" in its most literal form - it answers "is it safe to go in" before you commit to anything, for free, from outside the fight. The ultimate is the same idea raised to room/round scale: when you have to push a position, a wide reveal plus damage reduction is you buying a few seconds of safety to make the right read, not a kill-everything panic button - it fits the core dynamic instead of undercutting it.

**Bug avoided by design, not caught after the fact:** the ultimate's `AddCharge` explicitly no-ops while the ultimate is already active, so a kill landed mid-ultimate can't instantly refill the next one.

**Note:** `reconDartUnlocked` currently defaults to `true` (available from room 1) since the room-to-ability unlock mapping is being fully rebuilt in Phase 5 anyway - not worth tuning a threshold that's about to be replaced. Ult orb placement is also deferred to Phase 5, once the actual room layout exists to place "exposed/risky" spots in.

**What to playtest:** Recon Dart's reveal radius (8u) and duration (3s) - does it give enough info to commit confidently, or too much to make the peek decision trivial? Ultimate's charge rate (4 kills) and effect duration (6s, 50% reduction) - these are first-guess numbers, not tuned against real play yet.
