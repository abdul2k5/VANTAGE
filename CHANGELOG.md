# Vantage — Design Changelog

Running log of design decisions and the reasoning behind them, kept for the Game Notes feedback section. Each entry says what changed and why, not just what was built — see git log for full implementation detail.

## Phase 6b: procedural materials + door beacon fix

**What changed:** Every room was flat-colored primitives - one solid color per object, no texture at all. Built 4 procedural materials (no external assets, generated via Unity's built-in texture tools): light concrete + subtle panel-line grid for walls (shared everywhere), a darker/cooler noise-textured floor material (kept deliberately distinct in value from the walls - readability only holds if materials stay separated, not just lighting), and two cover materials (dark industrial base + bold diagonal hazard-stripe accent) split by room identity - orange for the attack-side rooms (Range, Weak Point, Holding Angle), teal for the site/defense-side rooms (Recon through The Round).

Also fixed a separate issue: every door had a vertical light-beam marker tall enough to poke above the wall line into the top-of-screen HUD text - recolored each to its room's accent and shrank it down to a short wayfinding marker instead of a stray bright column behind the objective text.

**Why:** Matches the Valorant-inspired aesthetic brief - light, clean, legible spaces rather than dark/busy ones - and gives each half of the tutorial a distinct visual identity that reinforces the attack-side/defense-side structure already present in the room sequence, not just naming it in text.

**Note on the hazard stripes:** went bolder/more solid-coverage than "sparingly on the edges only" - the procedural texture tool doesn't support masking a pattern to just a border region, so the stripe pattern covers the full cover face rather than a thin trim. Flagged for revisit if it reads as too busy once more of the presentation pass is in.

## Phase 6a: fix overexposure, wire up URP post-processing

**What changed:** Every room's floor and sky were clipping to pure white under direct light - not a tuning problem, a structural one: there was no `Volume` component anywhere in the scene, and the Main Camera had no `UniversalAdditionalCameraData` component at all, so post-processing could not run regardless of what was configured in `DefaultVolumeProfile.asset` (which was itself empty - `components: []`). Added the camera component, a global `Volume` referencing the profile, and configured Tonemapping (Neutral), Color Adjustments (exposure pulled down, contrast/saturation up slightly), a light Bloom, and a subtle Vignette.

**Why:** This is foundational for everything else in Phase 6 - the planned procedural material pass (6b) needs working exposure to read correctly, and "looks unfinished" was the single most visible presentation problem before any of the other polish work.

**How it was caught:** Live Play-mode screenshots via Unity MCP across all 8 rooms, before and after - confirmed by eye, not just by checking that components were wired. Same lesson as the crosshair fix: correct-looking configuration isn't the same as correct-looking output.

## Fix + feature: crosshair visibility and color options

**What changed:** Diagnosed live in a running build via Unity MCP (play-mode + screenshots, first use of this workflow) rather than reading code alone. The crosshair wasn't actually broken - the Dot style's hitbox was 5x5px at 1920x1080, next to nothing compared to Cross/Brackets' ~16-20px reach, so it read as "missing" even though every wiring/field check came back clean. Bumped it to 8x8. Separately, the Settings menu's style-picker buttons had never had a preview graphic - just the CROSS/DOT/BRACKETS text labels over an empty box - so added real shape previews (clones of the actual crosshair graphics, click-through disabled so they don't intercept the button's own click). Also added a 5-color picker (orange/white/red/green/cyan) that applies across whichever style is active and persists the same way style does.

**Why:** "I don't see crosshairs" turned out to be two different gaps - one genuine sizing bug, one missing feature that looked like a bug (an empty preview box reads as broken even when the underlying button works fine). Verifying with an actual screenshot instead of trusting the wiring was correct is what separated those two causes from each other - everything about the Dot's wiring checked out "correct" right up until seeing it at 5x5 pixels on screen.

## Phase 5: re-sequenced into 8 rooms

**What changed:** Went from 6 rooms to 8, one lesson per room, danger always preceded by a safe practice moment:

- **Range** (old Room 1, relabeled) - added `Sentinel_Range_Target`, a Sentinel with `range = 0` so `CanSeePlayer()` is geometrically always false - it can never see or fire. Pure hit-marker/weak-point-preview feedback before anything can shoot back.
- **Weak Point** (new room) - one Hold-mode Sentinel with `damagePerHit = 10`, a ~0.8s reaction delay, and a new per-instance `bodyDamageMultiplier = 0.5` (half body damage, 4 hits to kill) while a headshot still one-taps. The fight itself teaches "go for the head," not just the objective text.
- **Holding Angle** (old Room 2, Sentinel flipped Patrol→Hold) - this room's Sentinel used to patrol back and forth; now it holds the doorway it's named for. First room in the game to actually use Phase 2's Hold behavior - every other Sentinel had been Patrol-only since Hold was built.
- **Recon** (new room) - a partition wall blocks direct line-of-sight from the entry to a Hold-mode Sentinel tucked behind it. Recon Dart is gated to unlock here (`AbilityController.reconDartUnlocked` was hardcoded available from the start since Phase 3, flagged in its own comment as this exact Phase 5 TODO) - this is the first room where throwing it before rounding the corner has a real reason to exist.
- **Smoke / Flash / Dash / The Round** (old Rooms 3-6, relabeled only) - same content, ability unlocks shifted to match the new room indices (4/5/6; The Round's Breach Charge from Phase 4 untouched).

**Why:** The brief wants one thing taught per room with a safe rep before the real test. Two gaps this surfaced while building it: Recon Dart had never actually been gated to a room (always on since Phase 3), and Hold mode had never actually been used anywhere despite being built in Phase 2 - both fixed as part of giving each new room a real reason to exist, not just a new label.

**How the new rooms were built:** rooms are not a contiguous corridor - `RoomManager.EnterRoom()` teleports the player to each room's `entryPoint` with a fade, so array order (not world position) determines the sequence. All room geometry is plain Cube/Plane primitives with no prefabs, and Sentinels are a ~15-object hand-built hierarchy under one root - both were deep-cloned (fresh fileIDs, translated position) rather than hand-authored from scratch, verified end-to-end before each commit (position/field checks + a batch-mode Unity load with zero `error CS`). `Corridor3D/NavMesh.asset` was rebaked headlessly (`Assets/Editor/RebakeNavMesh.cs`) to cover the two new rooms - any Sentinel not on valid NavMesh throws on `agent.isStopped`, even one that never moves.

## Cleanup: removed the old 2D prototype scene

**What changed:** Deleted `Assets/Scenes/Corridor.unity` (+ `.meta`) - the original 2D prototype (`SpriteRenderer`/`BoxCollider2D`/`Rigidbody2D`), superseded by `Corridor3D.unity` and no longer in Build Settings or referenced by any script. `Corridor3D.unity` was not touched.

**Why:** It was dead weight sitting in the repo - not built, not loaded, not referenced - and kept growing every time it got touched by an untargeted commit. `RoomGoalTrigger.cs` and `RoomIntroTrigger.cs` (both used `OnTriggerEnter2D`, the only places that method appeared in the project) were the last remaining trace of it - confirmed unreferenced anywhere else and deleted in a follow-up pass.

---

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

---

## Fix: HUD bleeding through overlay screens + ultimate feedback

**What changed:**
- `MainMenuPanel` and `LevelCompletePanel` weren't the last sibling in the Canvas, so HUD elements added in later sessions (objective label, hit marker, damage indicator, the ultimate HUD) rendered on top of them - visible as readable "Overwatch Protocol" text over the main menu before pressing Start. Fixed at the source: both panels now call `SetAsLastSibling()` themselves the moment they're shown, so this doesn't silently break again as more UI gets added in Phase 4-6. Also bumped both panels to fully opaque (were 0.95-0.97 alpha, letting a faint ghost bleed through even once ordering was fixed).
- Re-verified the "5 kills not 4" report by actually killing 4 Sentinels through the real `Weapon.cs` path in a clean test: charge hit exactly 100 on kill #4, no off-by-one, no floating-point drift. The ultimate's logic was correct - the problem was that it gave almost no feedback. Reveal markers only appear if a Sentinel happens to be hidden somewhere on screen, and the damage reduction has no visual presence at all, so a player could activate it and perceive nothing happening.
- Added `UltimateActiveOverlay`: a full-screen flash the instant the ultimate fires, a steady screen tint for its whole duration, and an "OVERWATCH ACTIVE - Ns" countdown label - so activation is unmistakable even with zero Sentinels in view. The charge pips now pulse once fully charged (previously just a static color change, easy to miss mid-fight) and a permanent one-line description ("REVEALS ALL SENTINELS + REDUCES DAMAGE TAKEN") now sits under the ultimate HUD at all times.

**Why:** Both are the same lesson - a mechanic that's correct in code but invisible in play doesn't actually read as working. The ordering bug meant a finished screen looked unfinished; the ultimate's lack of feedback meant a finished mechanic felt broken or confusing. Fixing visibility, not logic, was the right call in both cases.

---

## Phase 4: Breach Charge objective (Room 6 - "the round")

**What changed:**
- `BreachChargeSite`: a trigger zone around the charge in Room 6. Hold `4` inside it to arm the charge over 4 seconds; moving, taking damage, releasing the key, or leaving the zone cancels progress - planting has to cost something, same as every other commitment in this game. A progress bar + countdown label (`PlantProgress`) shows while holding.
- `RoundController`: once planted, starts a 30s detonation countdown (`RoundTimer`) and pushes Room 6's two Sentinels at the site in waves (`Sentinel.PushTowards`, a new override that cuts in ahead of Patrol/Hold and resumes normal behavior - including fighting back if it spots the player en route - once it arrives). Timer hitting zero with the player still alive shows `RoundWinPanel` ("BREACH SUCCESSFUL"); dying mid-round shows `RoundLosePanel` ("BREACH FAILED") instead of a normal respawn.
- `PlayerHealth.Die()` now checks `RoundController.IsRoundActive` before respawning - dying during an active round is a loss, not a free reset.
- `Sentinel` gained `PushTowards()` plus the `isPushing` state it drives.

**Bug found during this session's review, not caught earlier:** Room 6 still had the old `LevelCompleteWatcher` wired to the exact same two Sentinels, showing a leftover "Level Complete" panel the instant both were dead - with zero dependency on whether the Breach Charge had ever been planted. That made the entire new objective skippable: killing the two guards normally, same as every other room, still won the level outright. Caught by tracing what else referenced Room 6's Sentinels, not by playtesting. Fixed by disabling the `LevelCompleteWatcher` component on its GameObject (left in the scene, just inert, rather than deleted - non-destructive and easy to re-point if Phase 5's room re-sequencing wants it back for a different room). Also updated Room 6's stale objective string ("Eliminate both Sentinels to secure the facility") to describe the real objective, since it was still describing the old win condition.

**Cleanup:** removed `Assets/_Recovery/0.unity` (+ its `.meta` files) - a stray Unity crash-recovery scene copy that got swept into the pre-crash WIP commit by an untargeted `git add`. It wasn't referenced by Build Settings or anything else; pure clutter from the crash, not a real asset.

**Why:** The Breach Charge is this project's first objective that's about *defending a commitment* rather than just reaching a door, which is new territory for "utility before you peek" - here the peek-then-commit loop happens *during* the hold, against waves that are actively converging on you, not before a single engagement. Getting the old win condition out of the way matters because a defend objective that can be bypassed by playing the old way doesn't teach anything new; it would just be an invisible, unused system sitting next to the real level flow.

**What to playtest:** Hold time (4s) and `moveCancelThreshold` (0.15) - does holding still inside the zone feel readable as "committing," or does it cancel too easily/too rarely? Detonation countdown (30s) and wave timing (`waveDelaysSeconds`: 0s, 10s) against the two Sentinels' actual travel time from their patrol routes to the site - confirm they reliably arrive and engage before detonation, not after. Also confirm a death mid-round reliably shows BREACH FAILED (not a silent respawn) and a survived countdown reliably shows BREACH SUCCESSFUL.
