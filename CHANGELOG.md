# Vantage — Design Changelog

Running log of design decisions and the reasoning behind them, kept for the Game Notes feedback section. Each entry says what changed and why, not just what was built — see git log for full implementation detail.

## Phase 15.10: dot crosshair is a round dot

**Why:** Playtest feedback: the "dot" crosshair style was actually a small square (a sprite-less UI Image), which reads as blocky and doesn't match the round colour swatches right next to it in the same settings tab.

**What changed:** The in-game dot, the settings preview dot and the DOT style tile icon now use the project's existing `CircleChip` sprite (already used by the colour swatches - no new asset). Turned on mipmaps for `CircleChip` so a 128px circle downscaled to an 8px dot stays smooth-edged instead of aliasing; the 32px swatches get slightly smoother edges from the same change. Size/gap/outline/colour settings all still apply unchanged.

## Phase 15.9: one consistent drone damage

**Why:** Playtest feedback: drones hit for different amounts depending on the room (15 in Holding Angle, 20 in Recon-Dash, 25 in The Round), which made the player's health bar unreadable - the same hit could cost a different chunk of HP room to room, so the player couldn't build a reliable sense of "how many more hits can I take". Consistency is the more useful thing to learn here than a hidden damage ramp.

**What changed:** Every drone now deals 20 damage per hit (5 hits from full 100 HP), everywhere. The difficulty curve from Phase 10 is kept, but now lives entirely in **reaction time** (0.8s Rooms 1-3, 0.5s Rooms 4-7, 0.35s The Round) plus room layout - the drones get faster to shoot as the game goes on, not harder-hitting, which the player can actually see and plan around.

## Phase 15.8: room text rebalanced - controls taught where they're used

**Why:** Room 1's intro tried to teach everything at once (move, look, shoot, spread, jump, crouch, checkpoints, restart rule) while leaving out reload, which you can actually need in the Range. Jump and crouch don't matter until there's something to hide from, and Room 3 repeated "LMB to shoot" for the third time instead of teaching what the room is about.

**What changed:**
- **Room 1 (Range):** jump/crouch moved out, reload (R) moved in. Now: move, look, shoot, reload, spread, restart/checkpoint rule.
- **Room 3 (Holding Angle):** dropped the repeated LMB/R line; now teaches the room's actual skill ("peek one side at a time") alongside crouch and jump, the first room where hiding from a held angle matters.
- **Rooms 3-7 objectives** now say exactly what opens the door (see Phase 15.7), so the gating is never a surprise. All texts checked to fit on screen: objectives on one line of the 928px objective bar, intros within their 640x70 panels.

## Phase 15.7: Rooms 3-7 doors are earned, not open from the start

**Why:** The Core Dynamic is "utility before you peek", but in Rooms 3-7 the exit doors were open from room entry and nothing checked that the room's lesson ever happened - a player could sprint past every drone straight to the door, and the Recon room told you to throw a dart without ever requiring it. The lesson was suggested by text, not enforced by the game.

**What changed:**
- New `RoomExitGate` on Rooms 3-7: the door starts locked and opens when the player **uses that room's ability while in that room** (Recon Dart, Smoke Wall, Flash, Dash), **or** clears every drone in the room. The kill fallback exists so winning the fight the hard way never softlocks the player; Holding Angle has no ability yet, so its door is kill-only (both held angles cleared).
- `Ability.OnUsed` event (fires after a successful `TryUse`) - how the gate knows the utility was really used, not just unlocked. Using an ability in a different room doesn't count.
- Checkpoint retry now re-locks the Rooms 5-7 doors (`DoorController.Relock()`, chevrons back to dark via `FloorChevronGuide.SetUnlitInstant()`) and restores their original objectives - otherwise a door opened before dying would stay open on the retry and the gate could be skipped.
- On unlock, each room's objective swaps to a short confirmation ("Sightline blocked. Door unlocked - go.").

**Verified** in Play mode via direct calls: Room 3 stays locked after 1 kill, opens after 2; Recon/Smoke/Flash open on their ability; Smoke used in Room 4 does not open Room 5; Dash room opens via the all-drones-dead fallback; a simulated checkpoint retry re-locks Rooms 5-7 (Recon, pre-checkpoint, stays open), restores objectives and drones, and the Room 5 gate re-opens on a fresh smoke afterwards. No console errors.

## Phase 15.6: second drone in Holding Angle

**Why:** Playtest feedback: Room 3 felt too thin with a single drone - one held angle is a single timing check, then the room is over. A second held angle turns it into the actual skill the room is named for: isolating one angle at a time from cover instead of exposing yourself to everything at once. Kept it as a mirrored Hold pair rather than a diagonal crossfire so Room 5 onward still introduces true crossfire as the next step up.

**What changed:** Added `Sentinel_R2_02` at (43, 0, 5), mirroring `Sentinel_R2_01` at (37, 0, 5) across the room's centre line - both Hold mode, facing the entry, same Room 3 tuning (0.8s reaction, 15 damage, 50 HP, 0.5x body armor). Objective and intro text rewritten for two drones ("Two drones are holding this doorway... peek one angle at a time"), since the old singular wording ("It's holding this doorway") now contradicted what the player sees.
- **Weak Point text caught up with Phase 13 at the same time:** its objective/intro still described the original single drone ("One clean headshot ends it", "the door won't open until it's dead") - now says all four drones, that they can't shoot back, and that two hold still and two move. Both rewritten texts checked to fit their 640x70 panel (preferred height 35px).

**Verified:** in Play mode, both drones register as Room 3 Sentinels (so alert sharing between them works), and `CanSeePlayer()` from five player positions: both see the entry, neither sees behind either crate, peeking left of the main crate exposes only `R2_01`, peeking right exposes only `R2_02` - each peek is a one-on-one fight, as intended.

## Phase 15.5: Weak Point door waits for every drone

**Why:** Playtest bug: the Room 2 exit door opened after only 2 of the 4 drones were down. `WeakPointRoomGate` was written when the room had a single Sentinel and only ever listened for that one's death; Phase 13 added 3 more drones but never told the gate about them, so killing `Sentinel_WeakPoint_01` opened the door whether the rest were still up or not. That undercut the room's point - it's the headshot practice room, and leaving early skips most of the practice.

**What changed:** `WeakPointRoomGate` now takes an array of Sentinels (all 4 drones wired in) and counts kills, unlocking only when the last one dies. The "clean headshot" vs "try the eye next time" objective message now reflects all 4 kills - silent success only if every drone was dropped with a headshot, the nudge if any one was a body shot.

**Verified:** in Play mode, killed the drones one at a time: door stayed locked after kills 1-3 and opened on kill 4; with one body-shot kill in the mix, the objective correctly showed the nudge.

## Phase 15.4: room instructions stay on screen longer

**Why:** Playtest feedback: the yellow per-room instruction text vanished before it could be read. Each room's brief is a full paragraph (controls, mechanic explanation, checkpoint note) that the player is meant to read while also looking around the room, and 8s wasn't enough to do both.

**What changed:** `AutoHidePanel.visibleSeconds` raised 8s → 18s on all 7 `<room>_InstructionsPanel` objects (Range, Weak Point, Holding Angle, Recon, Smoke, Flash, Dash), keeping the same 1s fade. All rooms changed together so the instruction timing stays consistent from room to room. The script's own default was also raised (6s → 18s) so any newly added panel matches.

## Phase 15.3: floor guide strips replaced with unlock-linked chevrons

**Why:** The dashed amber floor strips from Phase 15 read as road markings, not game signage — and in the Range they ran in a straight line through the centre target instead of stopping short of it. They also carried no information: lit the same whether the exit door was locked or not, so they never actually told the player anything they didn't already know from looking at the door itself.

**What changed:**
- Deleted all 7 `FloorGuide_<room>` dashed-strip objects (Range, Weak Point, Holding Angle, Recon, Smoke, Flash, Dash — The Round has no exit door, so never had one).
- New `FloorChevronGuide` component + `ExitChevrons_<room>` object in each of those 7 rooms: a short row of 3 floor chevrons (two thin angled bars each) in front of the exit door, apex pointing at the door. Dark/unlit while the door is locked; `DoorController.Unlock()` now also triggers a first-to-last sweep across the row (~0.4s total, each chevron flashing to a brighter peak before settling to a soft amber glow) so the light visibly ripples toward the exit at the moment it opens. Doors that start unlocked (every room except Range and Weak Point) show their chevrons lit from room entry instead of mid-game — `DoorController.ApplyInstant()` now drives this the same way it already drove the light-strip colour.
- `FloorChevronGuide` reuses the project's existing low-emission amber (`0.12, 0.06, 0.015`) already proven not to bloom from the old guide strips, with a slightly brighter transient peak (`0.26, 0.13, 0.03`) only during the sweep itself.
- **Recon is the only room where the exit door isn't visible from the entry** — confirmed by comparing a positioned camera capture from each room's entry point against its door: every other room's door is in plain sight (Holding Angle's cover crate sits to one side, not in the sightline), but Recon's door is almost entirely blocked by the room's L-shaped cover wall, forcing a jog to the east around it. Added 3 extra always-lit chevrons (`EntryChevrons_Recon`, `FloorChevronGuide.alwaysLit = true`, not gated to the door) near the entry, bent along the actual walkable path around the cover instead of pointing straight at the (hidden) door.

**Verified:** live Play-mode test of the Range door — `RangeRoomGate`'s own `door.Unlock()` call correctly triggers the chevron sweep (confirmed mid-sweep: chevron nearest the entry at peak brightness, the other two still dark, matching the first-to-last order); door light strip and chevrons both read correctly locked (red door, dark chevrons) and unlocked (amber door, lit chevrons) in direct-camera screenshots. The Editor's Game View wasn't ticking real frames this session (same staleness issue noted in earlier phases), so the full real-time 0.4s sweep couldn't be watched frame-by-frame — verified instead via mid-coroutine property inspection (exactly one chevron group at peak colour, consistent with the coroutine having run exactly one scheduling step) rather than assumed.

## Phase 15.2: fade fires at the door, not at the far wall

**Why:** Phase 15's airlock trigger stayed at the old position near the far wall, 2m past the new door - so walking through the open panels did nothing until the player also crossed the empty stretch behind them and reached the wall. Playtest feedback: the fade should happen right as you walk through the door, not after an extra few steps of nothing.

**What changed:** Moved all 7 `AirlockTrigger_<room>` objects from the old wall position (z=8.2) to the door's own position (z=6.2), so the fade-and-teleport fires the moment the player passes through the opened panels.

## Phase 15.1: door sign fixes (reported straight out of a playtest)

**Why:** Three real bugs found by playing the Phase 15 doors, not by reading the code:
1. Each sign read "ROOM 3: HOLDING ANGLE" while the player was standing in Room 2, reading as wrong instead of as "the next room is Room 3" - the top HUD's "ROOM 2/8" and the sign's "ROOM 3" sitting a few lines apart on screen just looked like a contradiction, regardless of which one was "technically" correct.
2. Signs projected into the top HUD bars at normal approach distance. Moving every door 2m closer to the player in Phase 15 (for the airlock) meant the same sign height/size that used to be fine now loomed much larger on screen well before the player reached the door - verified with the same `WorldToScreenPoint`-against-the-HUD-bars technique Phase 11 used for the original wall-callout version of this exact bug class.
3. Doors looked like a frame dropped in the middle of an empty floor, disconnected from any wall - because Phase 15 moved the functional door 2m off the real wall to make room for the airlock, but never filled the gap between the new frame and the room's actual side walls.

**What changed:**
- Sign text dropped the "ROOM N:" numbering entirely (source of the confusion) in favor of "NEXT: <name>" - unambiguous regardless of what the top HUD is currently showing.
- Shrunk each sign ~2.5x (1.28m x 0.26m, down from 3.2m x 0.64m) and lowered it, confirmed via `WorldToScreenPoint` to clear the HUD bars from any approach distance beyond ~1.7m - at that point the player's attention is on the doorway in front of them, not the sign above it, same as real signage you're standing directly under.
- Caught and fixed two follow-on bugs from the shrink itself while verifying: the new sign position sat inside the door lintel's own geometry and was being occluded by it (pulled the sign 0.25m forward, clear of the frame); and shrinking had overwritten the sign's original negative-X localScale, which was there to counter a text-mirroring issue - text was rendering backwards until that was restored.
- Added wall-filler panels (reusing the room's own wall material) on both sides of every door frame, closing the gap back to the real side walls so the door reads as built into a wall instead of floating on the floor.

## Phase 15: real doors on every room exit, with earned unlocks

**Why:** Every room exit used to be an invisible trigger on a flat gray cube, with a thin beacon line as the only "this is the way out" cue - functional, but it gave the player no goal beyond "find the gray wall." It also meant the Range and Weak Point rooms could be skipped without actually doing the lesson (hit the targets, land a headshot) they exist to teach.

**What changed:**
- **`DoorController`** (new): a real sliding door - frame, two panels, a light strip above (red when locked, amber once open). `Unlock()` slides the panels apart over 0.6s with a Kenney door-hiss SFX; panel colliders sit on the existing `Blocking` layer, so a locked door blocks both player movement and Sentinel line-of-sight the same way a wall does, and disable once open. Doors in this game only ever go locked -> open once per playthrough - a full restart already reloads the scene back to authored defaults - so there's no re-lock path.
- **Every door moved 2m into the room from the old trigger position**, which now sits stripped down to just its trigger collider (`AirlockTrigger_<room>`, same `DoorTrigger` component, untouched) at the far wall. The gap between the new door and that wall is the airlock: walking through the opened panels and into that short stretch is what now fires the fade-and-teleport, not standing at the wall.
- **Room 1 (Range) and Room 2 (Weak Point) doors start locked.** New `RangeTarget` (on each practice target's hit collider) + `RangeRoomGate` unlocks Range's door once all 3 have been hit. New `Sentinel.OnKilled(bool headshot)` event + `WeakPointRoomGate` unlocks Weak Point's door on any kill - headshot or body shot - but body-shot kills swap the objective text to "Door unlocked. Try the eye next time." instead of staying silent about missing the point of the room. Every other door starts open - those rooms teach getting past a live threat, not killing it, so there's nothing to gate.
- **Door signs** repurpose the existing wall-callout Canvases (previously just room-name branding) - moved above each new frame and retexted to name the *next* room ("ROOM 6: FLASH"), except the door into The Round, which reads "SITE A". Recoloured to the same amber used everywhere else in the HUD/checkpoint system, since a couple of the old callouts had their own per-room accent colors that no longer fit a shared signage system.
- **Floor guide strips** (dim amber dashes) run from each room's entry toward its exit door.
- Removed the old beacon line objects entirely.
- Updated the Range and Weak Point objective text to say the door is locked until the lesson's done.

**Verified:** panel/frame layer confirmed on `Blocking` (matches `Sentinel.blockingMask`); locked door gap height (2.4m) is less than the player's max vertical reach while jumping (1.8m stand height + 0.8m jump apex = 2.6m), so a locked door can't be jumped over - the capsule would hit the panel first. Both gates tested live in Play mode: hitting all 3 Range targets unlocks `DoorFrame_Room1` and updates the HUD objective text; a simulated headshot kill on the Weak Point Sentinel unlocks its door and sets the headshot objective text - the body-shot branch is the same code path with the other string, not independently simulated.

## Phase 14: diegetic checkpoint pad + terminal (replaces the flag)

**Why:** The checkpoint used to be a floating flag marker planted mid-room — readable, but it didn't belong to the world, and it sat away from the actual respawn point. The lecturer-feedback checkpoint system (Phase 9) needed feedback that reads as part of the level, sits exactly where the player will actually reappear, and still makes the "this is now safe" moment land with the same banner/chime it already had.

**What changed:**
- **`CheckpointStation`** (new script) owns both pieces and starts them dim/standby; `CheckpointManager.NotifyRoomEntered` calls `station.Activate()` at the same moment it already triggers the banner and chime, so all three stay in sync. If checkpoints are off in Settings, `Activate()` is simply never called and both stay dim for the whole run - no extra gating needed.
- **Floor pad** at the exact Room 5 entry point: a 1.5m disc with a 12-segment emissive ring that sweeps on clockwise over 0.5s and settles on solid amber, instead of popping instantly.
- **Wall terminal** near the entry (south wall): a screen reading STANDBY in red, a red status light, both flipping to CHECKPOINT LOGGED ✓ / amber on activation.
- Kept emission intensities low (toned down twice after an initial pass still read too hot) so neither piece blooms out the way the old beacon line did.
- Deleted the old flag marker GameObject entirely (it had already lost its visual at some point - just an empty inactive transform).
- Retrying from the checkpoint re-enters Room 5 without calling `Activate()` again (it's a no-op once `activated` is true), so both pieces are already showing the logged state on respawn - no special-casing needed.

## Phase 13: Weak Point room — disarmed multi-drone aim practice

**Why:** Room 2 ("Weak Point") is meant to teach the headshot mechanic, but it only ever had one Sentinel that could shoot back — not much room to actually practice landing headshots before the stakes go up in Room 3 onward. Also, that one Sentinel was facing the wrong way: its Hold mode had no `holdPoint` assigned, so it just kept its original spawn facing, which pointed deeper into the room instead of back toward the door the player walks in through — it could never actually see the player coming.

**What changed:**
- **`Sentinel.canShoot`** (new field, default true): when false, the Sentinel still detects the player, turns to face them, and reacts to being shot (`ForceAlert`), it just never calls `Fire()`. A general-purpose toggle, not a Room 2 special case — reusable anywhere a "shoot-at-but-not-shot-by" target is useful.
- **Fixed `Sentinel_WeakPoint_01`'s facing** (180° — now watches the entry door, like every other Hold Sentinel's `holdPoint` already does) and set `canShoot = false`.
- **3 more drones added**, all `canShoot = false`: a second stationary one mirrored on the other side of the room (also watching the entry), and two Patrol drones sliding back and forth across the room at different depths and desynced start points, so the player has to track moving targets, not just stationary ones.
- **Fixed the Patrol drones showing up frozen, not moving:** `Sentinel.Update()` previously stopped *all* movement and locked into facing the player the moment it was in line of sight, regardless of `canShoot` - in this small room with a 22u/±50° detection cone, that's almost always. Disarmed (`canShoot = false`) Patrol-mode drones now skip that lock-on entirely and just keep walking their route; disarmed Hold-mode drones (the 2 stationary ones) still turn to face the player as intended - only the "freeze and stare" behavior needed splitting off from "react to being seen" at all.

## Phase 12: checkpoint retry — input freeze + unfaded teleport fix

**Why:** Dying and retrying from the Room 5 checkpoint left the player completely unable to move, look, or shoot. Root cause: `GameOverController.ShowGameOver()` unlocks the cursor for the Game Over screen's buttons (`Cursor.lockState = None`), but `RetryFromCheckpoint()` never locked it back — every input-reading script (`PlayerController`, `FollowCamera`, `Weapon`, `UltimateController`) bails out of its own `Update()` whenever the cursor isn't locked, by design, so the player was frozen on purpose by code that just forgot to undo the unlock. Every other "resume gameplay" path (Settings menu, Main menu) already re-locks the cursor on close; checkpoint retry was the one path that didn't.

**What changed:**
- `GameOverController.RetryFromCheckpoint()` now re-locks/hides the cursor before handing off to `CheckpointManager`.
- `CheckpointManager.RetryFromCheckpoint()` now fades to black before teleporting the player back to Room 5 and fades back in after, instead of snapping the camera instantly mid-frame — matching the fade every normal room transition (`RoomManager.GoToNextRoom`) already uses, so a checkpoint retry doesn't visibly jump cameras for a frame.

## Phase 11: bullet impacts, crouch/jump, HUD fixes (3 items)

**What changed:**
- **Bullet-hole decals** (`BulletImpactPool`) spawn on every shot that hits world geometry - player or Sentinel - oriented to the surface, pooled at 60 with a 15s fade, plus a dust puff and impact sound. Sentinels get a spark instead of a hole (they already have a hit marker); their misses now leave a hole on whatever's behind the player instead of just vanishing. Range targets are exempt from the pool/fade entirely, so a player's shot grouping stays visible on the board for their whole Room 1 visit.
- **Crouch** (Ctrl/C) smoothly lowers the CharacterController and camera, halves move speed, and tightens weapon spread further - crouched + stationary is now the single most accurate state in the game. A Sentinel's line-of-sight check now reads the player's actual represented eye height instead of a fixed guess, so standing still behind 1.2m cover doesn't hide you the way crouching does - cover is a crouch-dependent spatial choice, not a free pass just for being near it.
- **Jump** (Space) is deliberately a movement tool, not a combat one: apex capped low enough (0.8m, Inspector-exposed) that it can't reach the top of any cover crate, forces maximum weapon spread in the air regardless of speed, and carries a brief landing penalty so it can't substitute for an ability or chain into a mobility exploit. Blocked while crouched or mid-Breach-Charge-plant; jumping also cancels an in-progress plant outright.
- **HUD fixes:** wall callouts ("RANGE" etc.) no longer project into the top-of-screen room-title/objective bars from the room entry viewpoint; the ultimate's description text got the same dark-backing-panel treatment every other HUD text already has, instead of sitting unreadable in dim grey with nothing behind it.

**Why:** All three came from the same note: bullet holes give the player visible, lasting feedback that teaches "stop to shoot" (the grouping on the Range board makes the lesson concrete, not just stated in the objective text). Crouch adds a second spatial-reasoning option on top of cover - the player now has to actually choose a stance, not just stand near a crate. Jump is explicitly scoped to movement only (low apex, worst-case spread in the air) specifically so it can never become a way to bypass cover or skip using an ability, which would have undercut the same "utility before you peek" dynamic the Phase 10 difficulty pass was protecting.

## Phase 10: Sentinel difficulty pass (7 items)

**Why:** Playtesting showed the Core Dynamic wasn't actually being enforced — a player could clear every room without ever taking a hit, because the Sentinels were easy to beat in a straight gunfight: 7u detection range against the player's 50u weapon range meant you could just outrange them, ±30° vision meant side-stepping worked, there was no reaction to being shot from outside that cone, no body armor outside Room 2, no aim inaccuracy (every Alert shot landed, at any range), and rooms 5-7 each had exactly one Sentinel so there was never a second angle to cover. "Utility before you peek" was the stated design pillar but nothing actually punished skipping it. Each item below is its own commit (`Difficulty fix N/7`).

**What changed:**
- **Detection widened** — range 7u → 22u, half-angle ±30° → ±50° on all 8 Sentinels. Line-of-sight/blocking-layer raycasting is untouched; only the thresholds that gate whether a Sentinel notices at all. The player's weapon range (50u) still out-ranges every Sentinel, but not by enough to snipe safely anymore.
- **React to damage** — `Sentinel.TakeDamage` now calls a new `ForceAlert()`: getting shot from outside the detection cone/range (through smoke, from behind, a peek-and-reposition) snaps the Sentinel into Alert and turns/fires toward the hit source for `forcedAlertSeconds` (2.5s default) even with no current line of sight, after the same fair reaction-delay window a normal spot gets.
- **Alert sharing** — the first frame a Sentinel spots the player (not every already-alert frame), it calls `AlertRoommates()`: every other active Sentinel sharing its room (`OwnerRoomIndex`) gets `ForceAlert()`'d toward the same last-known position. A room with 2+ Sentinels can no longer be cleared one at a time by peeking each in isolation.
- **Body armor everywhere** — `bodyDamageMultiplier` 1.0 → 0.5 on the 7 Sentinels that didn't already have it (only the Room 2 teaching Sentinel did). Body shots now take 4 hits (25 dmg × 0.5 = 12.5/hit vs 50 HP) instead of 2; the eye weakpoint still one-taps (25 × 2.5 headshot multiplier = 62.5, always lethal regardless of body armor).
- **Per-room curve** — reaction delay/damage now actually escalate instead of being nearly flat across all 8 rooms: Rooms 1-3 (Weak Point/Holding Angle) 0.8s reaction, 10-15 dmg; Rooms 4-7 (Recon/Smoke/Flash/Dash) 0.5s reaction, 20 dmg; Room 8 (The Round) 0.35s reaction, 25 dmg. Every value this touches (range, angle, reaction delay, damage, fire interval, HP, body armor, forced-alert duration) is a public Inspector field on `Sentinel` already — no code change needed for this item, tuning data only.
- **Crossfire in Rooms 5-8** — Rooms 5-7 (Smoke/Flash/Dash) had exactly one Sentinel each, so no in-room crossfire was geometrically possible. Added a second Sentinel to each (cloned from the room's existing one, so it inherits the same tuned stats), in Hold mode at the far corner near the exit door, watching diagonally back across the room toward the entry corner — crossing the original Sentinel's lane near the room's center, where the player has to pass regardless. Flash still blinds every Sentinel in radius and Smoke still blocks the forward lane both angles run through, so the ability remains the full answer — it's just no longer optional. Room 8 already had 2 Sentinels but on parallel patrol lanes at different z (never actually crossing); repositioned `Sentinel_R6_02` from a parallel lane to a diagonal Hold watch that crosses `Sentinel_R6_01`'s lane instead. `RoundController`'s post-plant `PushTowards` still overrides Hold/Patrol either way, so none of the wave-push logic needed touching.
- **Aim inaccuracy by distance** — `Fire()` previously had zero miss chance at any range. Added a per-Sentinel curve: near-perfect (3% miss) within `perfectAimDistance` (8u default), ramping linearly to `missChanceAtMaxRange` (40% default) by the time the player is at the edge of the Sentinel's detection range. The tracer/muzzle flash still plays on a miss, so it reads as "shot at and missed," not "did nothing."

**Verification:** every mechanic checked via direct `execute_code` state inspection in Play mode rather than assumed from reading the code - confirmed `TakeDamage` on a Sentinel outside sight range sets its own forced-alert timer without alerting its roommate (damage-reaction and alert-sharing are deliberately separate triggers), confirmed an organic spot (`AlertRoommates`) does alert the roommate, confirmed the miss-chance formula returns ~3% at 5u and ~35% at 20u against a 22u-range Sentinel (matches the interpolation math), and confirmed both new crossfire Sentinels land correctly on the baked NavMesh with the right `OwnerRoomIndex` with a Play-mode screenshot showing neither clipping into room geometry.

## Phase 9: lecturer feedback — midpoint checkpoint

**Why:** Direct lecturer feedback on the lose condition: *"add some sort of midpoint saving, and give players the option to turn it off."* Before this, dying had two completely different stakes depending on where it happened — anywhere in Rooms 1-7 it was a silent, free respawn in place (`PlayerHealth.Die()` just teleported you back with full health and moved on), and only dying in Room 8 (The Round) actually ended the attempt with a lose screen. That split was never a deliberate difficulty curve, just a side effect of The Round being the only room that happened to check in with a controller on death. The lecturer's note reframes dying as something that should always cost the player the attempt, with a single mid-run save to make an 8-room tutorial run survivable without turning into "replay Room 1 forever" on every mistake.

**Why Room 5 specifically:** Room 5 ("Smoke") is the exact midpoint of the 8-room sequence and the natural seam in the tutorial's own structure — Rooms 1-4 (Range, Weak Point, Holding Angle, Recon) teach the gunplay and information-gathering basics, while Rooms 5-8 (Smoke, Flash, Dash, The Round) are where every utility ability is live and the final objective is assembled. Checkpointing at the start of the utility half means a death late in that half doesn't force replaying the parts of the tutorial that are already mastered, while a death in the basics half still means starting over — consistent with "utility before you peek" being the thing worth protecting progress on.

**What changed:**
- **Every death now ends the attempt.** `PlayerHealth.Die()` no longer silently respawns in Rooms 1-7 — it always calls the new `GameOverController.ShowGameOver()`, which pauses the game and shows a retitled "MISSION FAILED" screen (reusing the old Round-only "BREACH FAILED" panel, generalized since it's no longer Round-specific).
- **Midpoint checkpoint at Room 5.** `CheckpointManager` captures a snapshot the first time the player enters Room 5 (`RoomManager.EnterRoom` now notifies it) — a "CHECKPOINT REACHED" banner, a confirmation chime, and a small amber flag marker placed in the room that stays up for the rest of the run. The snapshot is just the ultimate charge at that exact moment (`UltimateController.CurrentChargeAmount`/`SetCharge`) — ability unlocks and cooldowns don't need snapshotting since `AbilityController.UnlockForRoom`/`ResetAllCooldowns` already derive correctly from the room index alone.
- **Game Over buttons adapt to checkpoint state:** RETRY FROM CHECKPOINT + RESTART FROM BEGINNING when a checkpoint exists and is enabled, a single RESTART otherwise, plus MAIN MENU always. Retrying from checkpoint resets Rooms 5-8's Sentinels (`Sentinel.ResetState`, keyed off a new `OwnerRoomIndex`) and the Breach Charge/round state (`BreachChargeDevice.ResetState`, `BreachChargeSite.ResetState`, `RoundController.ResetState`) regardless of how far past Room 5 the player got before dying, then restores full HP/ammo and the snapshotted ultimate charge.
- **Settings > General gained a CHECKPOINTS: ON/OFF toggle** (`PlayerPrefs`, default ON). OFF is hardcore mode — no banner, no marker, every death restarts from Room 1, even if the player passed Room 5 earlier in the run with it on.
- **RESTART FROM BEGINNING vs MAIN MENU** both do the same full scene reload used everywhere else in the project (`SettingsMenuController.QuitToMenu`'s existing trick for a total reset), but RESTART FROM BEGINNING sets a new `MainMenuController.SkipMainMenuOnNextLoad` flag first so it drops straight into Room 1 instead of showing the title screen — the two buttons needed to actually behave differently, not just say different things.
- Updated the Room 1 intro text to set expectations up front: "Die and the drill restarts. Reach Room 5 to unlock a checkpoint."

**What to playtest:** dying in an early room (should go straight to Room 1, no checkpoint option); reaching Room 5, continuing on and dying in Room 7 or 8, then using RETRY FROM CHECKPOINT (should land back in Room 5 with full HP/ammo, only Recon+Smoke unlocked, and every Room 5-8 Sentinel back up even if one was killed earlier in that attempt); toggling CHECKPOINTS off mid-run and confirming a later death skips straight to a single RESTART with no banner on a fresh Room 5 entry.

## Phase 8: playtest fixes (5 items)

Another real-playtest batch, each item its own commit (`Phase 8 fix N/5`).

**What changed:**
- **AudioMixer + routing** — volume sliders were showing 0 because `AudioManager` had no mixer in the loop at all: every `AudioSource` in the project (music, SFX, Sentinel hover hum, the one created per gunshot) played straight to the `AudioListener`, so the three sliders were cosmetic. Built `VantageMixer` (Master > Music/SFX, each exposed as a dB parameter the sliders drive via a log curve) and routed every known `AudioSource` to it at its own `Start()`, plus a periodic sweep in `AudioManager` that force-routes anything left unassigned — a backstop, not the primary path, so it should stay silent in normal play. Sentinel hover hum no longer autoplays in `Awake()`: it now only plays once the game is running and the Sentinel's own room (nearest `RoomManager` entry point, computed once) matches the player's current room, so 9 Sentinels across 8 rooms don't all hum at once. `AudioListener.pause` now follows the main menu and settings panel, with music exempted so it keeps playing under both.
- **HUD text panels** — room title, objective, and every room's instructions text each had a backing panel authored as a *sibling*, not a parent, of its text — so a panel's own visibility toggle never touched the text sitting on top of it, and vice versa. Three panels (`RoundTimer_BG`, `UltimateActiveLabel_BG`, the ultimate's ready-banner panel) were left permanently active this way, rendering as empty dark boxes on every single room regardless of round/ultimate state. Reparented every one of these pairs so the backing panel is the actual parent, added `ContentSizeFitter` + padding so each panel hugs its own text instead of a hand-tuned fixed size, and put room title/objective/the active instructions panel into one `VerticalLayoutGroup` stack so they can't overlap regardless of text length. Also fixed the ammo count's backing panel, which was a loose sibling drifting independently of the ammo text.
- **Settings menu layout** — every control in every tab was hand-positioned at an absolute offset (General had a large empty gap below the sensitivity slider; Crosshair's preview box overlapped the tab bar). Rebuilt all three tabs with Vertical/HorizontalLayoutGroup rows and grew the panel 600→760 tall so Crosshair, the tallest tab, fits without its bottom rows spilling past the edge. Separately, `ShowTab()` was only swapping each tab button's background color, not its label's — so whichever tab wasn't General rendered dark text on a dark background and was nearly unreadable; it now swaps both together.
- **White gun viewmodel** — the Kenney Blaster Kit FBX's embedded material had no texture: confirmed no `colormap` image exists anywhere in the project, so it was never imported alongside the mesh, leaving the URP/Lit default of flat white `_BaseColor`. New `Mat_Gun_Gunmetal` (dark body) and `Mat_Gun_Accent` (the project's existing amber) replace it on the rifle body vs. magazine/scope.
- **Stray Range drone** — `Sentinel_Range_Target`, floating over the Room 1 wall callout, turned out to be a fully-configured Sentinel (Stinger Drone model, 8 renderers, damage, hover hum, death sound) — the original placeholder target from before the proper stand targets (`RangeTarget_L/R/Center` with bullseye colliders, built in Phase 7) existed. Its `range = 0` made it harmless but it was still a visible leftover the stand targets already replaced. Confirmed it wasn't referenced by `RoundController.siteDefenders` or anywhere else in the scene before deleting it.

**Why:** Same thread as Phase 7 — things that were technically wired up but never actually did what their own names implied. A slider that doesn't change the volume, a panel that doesn't hide when it's supposed to, a tab you can't read, a gun that lost its texture on import and nobody noticed because the mesh still rendered. None of these needed new mechanics, just the existing ones actually connected to what's in front of the player.

**Process notes:**
- One real near-miss: `UltimateHudUI.readyBanner` is the "OVERWATCH PROTOCOL READY" callout, not a generic ability-name label — the field was discovered via the component's actual serialized references, not assumed from the GameObject's name ("Label"), after an initial guess would have overwritten its runtime-set text and left it permanently active instead of toggled.
- Several `AudioSource.PlayClipAtPoint` calls (Sentinel death) had no way to target a mixer group — replaced with a new `AudioManager.PlaySfxAtPoint` that builds a temporary routed `AudioSource` instead, so the backstop sweep didn't have to carry sources the primary path could just route correctly from the start.
- The Unity window went fully OS-invisible partway through this session (confirmed via `EnumWindows`, not just an unfocused-window guess) — Game View screenshots silently returned a stale cached frame and `Update()` stopped ticking entirely (`Time.frameCount` frozen) rather than erroring, so a `ShowWindow`-to-restore step before any screenshot was needed to get real frames back. Worth remembering alongside Phase 7's domain-reload note: this toolchain fails silently, not loudly, when the Editor window loses real OS presence.

**What to playtest:** a full room-by-room run listening for the Sentinel hum only ever coming from the room you're actually in; opening settings mid-game and confirming music keeps playing while gunfire/ambient SFX cut out; the full Breach Charge countdown and an ultimate activation to confirm their banners size to their own text instead of leaving any empty space.

## Phase 7: playtest fixes (7 items)

A batch of fixes from a real playtest pass, not a planned phase — each item below is its own commit (`Phase 7 fix N/7`) so they can be read and reverted independently if one doesn't land.

**What changed:**
- **Range room cleanup** — the 3 "floating glowing spheres" turned out to be the Kenney target meshes rendering with no material (an embedded FBX default) and no visible mount; gave them a proper white board + red bullseye material and a pole stand so they read as practice targets, and fixed the center one silently using a drone-visor prop mesh instead of a target. Removed the 3 decorative floor crates — Range has no combat Sentinel, so they were clutter, not cover; verified every other room's actual cover boxes are already the right height (1.2m) and already block Sentinel line-of-sight raycasts, so nothing else needed changing.
- **HUD text readability** — room title, objective line, and ammo count floated directly over the 3D scene with no backing, unreadable against Range's bright sky in particular; gave them dark semi-transparent backing panels (sized to each label's own rect) plus a drop shadow, same treatment on the Esc hint, round timer, and ultimate-active banner. Root-caused the "orange on orange" ability-tile complaint: `AbilityHudUI` tints a slot's border the same accent orange as its name label's text the instant an ability comes off cooldown, so a *ready* tile was genuinely unreadable, not just low-contrast — set both labels to white with a shadow so they hold up against both the ready and not-ready tile colors instead of matching one of them. Also enabled Best Fit on the key labels so `SHIFT` stops clipping its tile.
- **Breach Charge countdown 30s → 20s** — shortens the post-plant window per playtest feedback (30s let the push feel passive); checked Sentinel push distances and wave delays against the new window before changing it, not after.
- **Settings menu redesign** — root-caused the reported overlap bug: the Phase 6d volume sliders and the original crosshair controls used two different RectTransform anchor conventions in the same flat panel, so they landed on top of each other by coincidence of coordinate systems, not because anyone dragged them there. Rebuilt as GENERAL / CROSSHAIR / AUDIO tabs instead of just re-spacing the same flat list — a single unsorted panel wasn't going to scale past two features anyway. Added a dim overlay behind the panel, hid the gameplay HUD while paused, a live-updating mouse sensitivity slider, Resume/Restart Room/Quit to Menu, and a crosshair tab with a live preview box (mirrors the real HUD crosshair's style/color/size/gap/outline), round color chips (a procedurally generated circle sprite) with a selected-state ring, and working size/gap/outline controls that didn't exist before.
- **Ultimate display redesign** — replaced the 5-bar linear meter (a separate row above the ability bar) with a segmented ring around the ultimate icon, placed as a tile at the end of the ability bar right after Dash. Segment count reads from `UltimateController.SegmentCount` (`maxCharge / chargePerKill`) every frame rather than being hardcoded, so a future balance change to the charge cost can't silently desync the HUD. Added a "+1" popup on every charge gain (kill or orb), a gold pulse + `[X]` key prompt + brief "OVERWATCH PROTOCOL READY" banner on the first frame it's fully charged.
- **Ability unlock tutorial popups** — a Valorant-style card (icon, name, key, one-line description, one tip tied to "utility before you peek") the first time each ability's unlocked flag flips true, plus one for the ultimate the first time it reaches full charge or at the start of the last room if it never did. Polls for the false→true transition every frame since there was no unlock event to hook into. Pauses time and unlocks the cursor while up, gated behind a new `AbilityUnlockCardUI.IsShowingCard` static flag so a dismiss-click can't also fire the weapon or open the settings panel underneath it.
- **Breach Charge device + site visuals** — the objective had no physical presence: a flat pink disc and plain countdown text were the only signs anything was happening. Built an original canister prop (not a reskin of anyone's spike) with a glowing core, a ring of lights, a real point light, and a vertical beam, driven entirely by external progress values (`SetDeployProgress` while holding `[4]`, `SetCountdownProgress` once planted) rather than its own timers, so it can never drift out of sync with the actual plant bar or round timer. Detonation is now sequenced — flash, camera shake, particle burst, explosion sound, *then* BREACH SUCCESSFUL, not all at once. Site marker is now a teal outline ring with a painted "A" (matching Room 6's wall-callout color) instead of the flat pink disc; the trigger zone itself is untouched.

**Why:** All seven came out of the same playtest pass, so the common thread is "things that were technically correct but didn't read right in actual play" — unreadable text, a settings panel that was accidentally broken by two features authored at different times, a meter and a round objective with no real presence, and onboarding that assumed the player would just know what a newly-unlocked key did. None of these are new mechanics; all of them are the mechanics finally being legible.

**Process note:** mid-session, Unity compiled new script fields cleanly but the running Editor's live AppDomain hadn't picked them up — confirmed via both `execute_code` reflection and the component-property tool failing identically on a field that definitely existed on disk. The `last_domain_reload` timestamp in editor state was stuck the entire time the Unity window was unfocused (tabbed away to the terminal); asking for the window to be refocused immediately unstuck it, and the next forced refresh completed the reload. Worth remembering: an unfocused Editor can silently stall domain reloads with no error of its own.

**What to playtest:** the full Breach Charge sequence end to end (plant → device deploys → countdown pulses faster and turns red in the last 5s → detonate → BREACH SUCCESSFUL); every tab of the new settings menu, especially that the crosshair preview and the actual HUD crosshair never disagree; stepping through all 4 ability unlocks plus the ultimate's first full charge to confirm each card only shows once; and the ultimate ring's segment count if `chargePerKill` ever changes — it should resize itself without any code changes.

## Phase 6e: real gun and drone models

**What changed:**
- **Player rifle** - `PlayerGunViewmodel`'s four primitive cubes (Receiver/Barrel/Stock/Grip) replaced with a real rifle mesh from Kenney's Blaster Kit (CC0), picked for silhouette (scope rail, underslung magazine, stock) over the pack's pistols/SMGs. The existing `Muzzle` transform - what `Weapon.cs` fires tracers from - was kept alive and reparented rather than recreated, so the field reference in the scene never broke; its position just shifted ~0.5cm to track the new mesh's barrel tip.
- **Sentinel drone** - every one of the 9 Sentinels (`Body`'s Hull/SensorHead/Fins/ThrusterGlow primitives, plus the held-gun primitives, which don't make sense on a flying drone) replaced with a CC-BY "Stinger Drone" model from Poly Pizza - picked over an alternative "Anti-Gravity Drone" candidate specifically because its source listing describes it as built to be an enemy that fires lasers, a closer fit than a generic aircraft model. The `Eye` GameObject (the `Weakpoint` component + its `SphereCollider`, which `Weapon.cs` headshot detection reads directly off the hit collider) was kept and re-positioned onto the new mesh's small red sensor pod at the drone's nose rather than deleted and rebuilt, so headshot detection needed no code changes. Same preserve-don't-recreate treatment for `Muzzle`. `CapsuleCollider`, `NavMeshAgent`, and the hover-hum `AudioSource` (added at runtime in `Sentinel.Awake()`) all live on the Sentinel root, untouched by any of this.
- **Range room dressing** - added Blaster Kit targets and crates around the existing `Sentinel_Range_Target` (the always-safe, range-0 Sentinel used purely for weak-point preview): two targets flanking it, one centered beyond it, and three crates as cover between the entry and the targets - giving the tutorial's first room a literal "peek from behind cover" rep before Room 2 ever asks for one for real.
- **Recolor pass** - both packs ship their own stock colors (the Blaster Kit's multicolor variation palette, the drone's default materials), neither of which matched Vantage's existing look. Recolored everything to the game's established palette instead of using either pack's defaults: dark gunmetal (matching the old primitive gun/Sentinel color exactly) for the rifle and the drone's body panels, the existing weak-point red for the drone's sensor accents, and the existing thruster-glow orange for its warning-light accents. Range room crates got the real `Mat_Cover_Orange` material (the actual shared asset Room 2's cover already uses, not a recreated color) so they read as the same material, not a same-colored lookalike.

**Why:** A tutorial that still looks like placeholder primitives at Phase 6 undercuts every other polish pass before it - real geometry is the most visible "is this finished" signal there is. Keeping `Muzzle` and the `Weakpoint`-carrying `Eye` alive through the swap (reparent + reposition, not delete + recreate) mattered specifically because both are live reference points other scripts read at runtime; recreating them would have silently broken tracer origins and headshot detection without a single compile error to catch it.

**Licensing note:** the Blaster Kit is CC0 like every other Kenney asset already in this project. The Stinger Drone is CC-BY 3.0 - the one non-CC0 asset in Vantage so far - so its attribution is in `CREDITS.md` and is not optional the way the rest of the credits list is.

**How the drone's forward-facing sensor was found, not guessed:** the raw OBJ has no labelled "front" - dumped every submesh's local bounding box and material to find the smallest, most isolated red-colored part of the mesh (a tiny ~1cm pod, distinct from the four larger red rotor-guard discs), reasoned it was the sensor/lens rather than a structural rotor light, then verified the choice with an edit-time scene screenshot once placed rather than trusting the reasoning alone.

## Phase 6d: full audio pass

**What changed:** Built `AudioManager` - a singleton hub for master/music/SFX volume (persisted via `PlayerPrefs`, same pattern as the crosshair settings) and one-shot SFX/music playback. Every gameplay event named in the brief got an `AudioClip` field, routed through the shared manager rather than each script owning its own volume logic: gunshot, reload, empty-ammo click, hit, headshot, all 4 ability activations (one shared field on the `Ability` base class rather than duplicated per subclass), the ultimate, damage taken, Sentinel death, Sentinel hover hum, Breach Charge plant, the detonation beep (already existed), detonation itself, round win, round lose, and UI clicks on every interactive control. Settings panel gained 3 volume sliders, built as full Unity `Slider` hierarchies and wired to `AudioManager` via *persistent* event listeners (not runtime `.AddListener()`, which doesn't survive a scene save - caught this the first time by checking the serialized listener data directly rather than trusting the registration call).

Music: a looping track for the menu, a second for Range through Dash (all seven share one track), and a third swapped in specifically for The Round. `RoomManager` now owns the gameplay/round switch on every room transition.

Sourced from 4 CC0 Kenney packs (Sci-fi Sounds, Impact Sounds, Interface Sounds, Music Jingles) plus 3 CC0 music tracks the user supplied directly - full credits and per-sound mapping in `CREDITS.md` and the commit log.

**Why:** A tutorial that's silent on every action (gunfire, abilities, hits, wins) doesn't confirm to the player that anything happened - the kill feed (6c) solved this for kills specifically, audio solves it for everything else. Distinct per-ability sounds (rather than one generic "ability used" blip) let a player recognize *which* ability just fired by ear alone, which matters once Smoke/Flash/Dash/Recon are all unlocked and being used in quick succession.

**What I couldn't verify from here:** whether any of this actually sounds right - correct volume balance, no clipping, no jarring mismatches between tracks. There's no way to "hear" through this toolchain the way a screenshot lets me "see" a UI bug. Confirmed everything that's checkable at the data level (every field holds the intended clip, the switching logic picks the right track at the right room, no console errors) and left actual listening to a real playtest.

## Phase 6c: ability icons, kill feed, wall callouts

**What changed:**
- **Ability icons** - every ability slot (Recon/Smoke/Flash/Dash + the Ultimate) now shows a hand-drawn pixel glyph (radar rings, cloud, lightning bolt, forward chevrons, sparkle) instead of just a key letter. The key-bind letter ("Q"/"SHIFT"/etc.) shrank from filling the whole 96x96 slot to a small top-left corner badge, making room for the icon to be the dominant visual.
- **Kill feed** - a stacking, auto-expiring notification list (top-right, 3s visible + 0.5s fade, matches `AutoHidePanel`'s existing timing convention) showing "SENTINEL ELIMINATED" or "SENTINEL ELIMINATED - HEADSHOT" (accent orange) on every kill.
- **Wall callouts** - one room-name sign per room on the far wall (first thing visible on entry), in that room's accent color - the orange/teal attack-side/defense-side split from 6b now shows up in text, not just material color.

**Why:** All three are "a tutorial should tell you what's happening without you having to read a paragraph" - icons let you recognize an ability at a glance instead of decoding a key letter, the kill feed confirms a kill registered without checking the Sentinel's health bar, and callouts reinforce the room identity (and the attack/defense structure) the moment you walk in, the way real tactical-shooter callouts do.

**How the icons were actually built:** the procedural pattern/gradient/noise tools can't produce arbitrary shapes (no "draw a cloud" primitive) - computed each icon as an explicit 24x24 pixel array instead, then had Unity read the array from disk and assemble it into a `Texture2D`/`Sprite` directly, rather than passing the array through the tool call parameter - a first attempt that way silently truncated mid-transfer (caught by checking the actual pixel count Unity received, not by trusting the call's reported success).

**Bug caught by screenshot, not by inspection:** the wall callout signs initially rendered with mirrored (backwards) text - a known World Space Canvas gotcha where a 180-degree Y rotation (needed to face the sign toward the room entry) flips the text horizontally. Fixed by also negating the canvas's local X scale, which cancels the mirror while keeping the correct facing direction. Would not have been caught without actually looking at a Play-mode screenshot - every wiring-level check (sprite assigned, color correct, position correct) would have reported success.

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
