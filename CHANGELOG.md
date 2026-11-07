# Vantage — Design Changelog

Running log of design decisions and the reasoning behind them, kept for the Game Notes feedback section. Each entry says what changed and why, not just what was built — see git log for full implementation detail.

## Phase 17.9: dying during Overwatch no longer costs your checkpoint charge

**Why:** Testing found two problems when dying with Overwatch running and then retrying. A checkpoint is meant to put you back exactly as you first arrived, but the saved ult charge was never restored (arriving with 50 came back as 0), because charge changes are ignored while Overwatch runs. Overwatch also kept running into Room 5, with its tint and countdown on screen for about 3.7s more, even though its damage reduction had already been removed. So a retry both took your charge and showed an ultimate that wasn't really protecting you.

**What changed:** the retry now cancels a running Overwatch while the screen is black, then restores the saved charge.

**Verified** in Play mode: arriving at the checkpoint with 50 charge, then dying during Overwatch and retrying, gives back 50 with Overwatch off and its tint and countdown gone. The charge is still 50 five seconds later. A retry with no Overwatch running also restores 50. Overwatch can then be fired again, and runs its full 6s with damage reduction on, then off.

## Phase 17.8: Overwatch orbs always give their charge

**Why:** Playtest: "i took the orb in room 7 and my ult didnt charge". Ult charge is ignored while Overwatch is running, so that a kill mid-ultimate can't refill the next one. Orbs went through the same check, so an orb grabbed during Overwatch disappeared and gave nothing. The orb is a reward for going somewhere risky, so losing it silently felt like a bug and punished the player for taking the risk.

**What changed:** orbs now always give their +25, even during Overwatch. Kills during Overwatch still give nothing. That rule exists to stop one ultimate funding the next, and a single fixed +25 orb can't do that.

**Verified** in Play mode by walking into the Dash room orb: 0 to 25 normally, and 0 to 25 during Overwatch, where it used to stay at 0. The charge and one lit ring segment are still there after Overwatch ends. A kill during Overwatch still adds nothing, and a kill afterwards adds 25.

## Phase 17.7: pressing Retry can't send you back to Game Over

**Why:** Testing found that pressing Retry with drones in view often reopened the Game Over screen partway through the fade (2 of 3 presses in one run). The failed attempt's drones stay live until the screen is black, and the player's health isn't restored until then, so one more hit counted as another death. It made a working retry look broken, right at the moment the player is being asked to try again.

**What changed:**
- The player takes no damage from the moment any Game Over opens until the retry restores them. This covers losing to a disarm too, where the player can still be alive on low health.
- Drones lose sight of the player when the retry fade starts, rather than once the screen is already black.

**Verified** in Play mode: 8 of 8 retries went through after mid-round deaths with reinforcements firing. A retry after a disarm loss on 10 health also went through, with health unchanged during the fade. Damage works normally again after the retry.

## Phase 17.6: checkpoint retry no longer locks you out of planting

**Why:** Testing the checkpoint retry after a plant found the game could become impossible to finish. The drones, charge and objective all reset, but the plant site still counted as planted, so back in The Round holding [4] did nothing and there was no way to win. The site's reset existed but nothing called it.

**What changed:** the checkpoint retry now resets the plant site too. You replay from Room 5, clear the defenders and plant again, the same as your first attempt.

## Phase 17.5: post-plant phase - reinforcements, disarm threat, utility refill, visible charge

**Why:** Playtest: "after i planted the breach i wanna see the breach itself and then there should be more going on after planting it rather than just waiting for it to go". The countdown sent the Round's two drones to push the site, but the objective makes you kill those same two before planting. So the waves sent nobody, and the 20s countdown was dead air. The charge itself was a 0.55m dark box with a pea-sized core.

**What changed:**
- **Reinforcements:** after the plant, 2 drones drop in through the roof at about 4s, and 2 more at about 11s. Each landing spot gets a pulsing red warning light and an alarm 1.5s before the drone lands. They push towards the charge. They come from a prefab (`Assets/Prefabs/Sentinel_Reinforcement.prefab`), not hidden scene objects, so checkpoint resets (which re-activate every scene drone) can't wake them early. Reaction time is 0.5-0.7s, because they land in view.
- **Disarm threat:** any drone within 1.8m of the charge disarms it over 4s, shown on the HUD ("DISARMING 65%") and on the device (amber flicker, ring lights going dark). If it finishes, you lose. Progress drains slowly when no drone is on it. **Blinded drones can't disarm**, so a flash buys time. This makes the countdown about keeping drones off the charge instead of hiding in a corner.
- **Utility refill on plant:** +1 charge of each ability, capped at max, so holding the site is played with smoke, flash and dart, the game's core "utility before you peek" idea.
- The detonation takes out every drone left in the room, so one can't kill the player behind the win screen.
- The charge is 1.7x bigger, with a thicker beam and a brighter light.
- Objective text now warns about the reinforcements before the plant and switches to a post-plant line after it. A retry restores it, and the retry also removes every spawned drone and warning light.

**Verified** in Play mode (player invulnerable, both defenders killed, then plant):
- Utility went Smoke 0→1, Flash 0→1, Dash 1→2, Recon stayed at its max of 1.
- The warning lights appeared at about 2s, and the first wave landed at about 4s.
- Drones that could see the player stopped to shoot. With the player hidden, they reached the charge and the disarm climbed to 100%.
- The Game Over screen then showed the disarm message.

## Phase 17.4: room layouts reshaped around each room's lesson

**Why:** Playtest feedback, second half: "each time i walk into a room its just one single room and like its straight up just shooting... the rooms just one box". Each room *told* you its lesson in text, but the space itself was an open rectangle, so every room played the same way. Now each room gets 1-2 solid pieces chosen so the space *teaches* the lesson, rather than just being decorated. "Nothing too much" was the brief, so 14 pieces in total across 8 rooms.

**What changed (new editor tool `Assets/Editor/RoomLayoutBuilder.cs`, menu *Vantage > Environment > Rebuild Layout Pieces*):**
- **Range:** a waist-high shooting counter with a walk-through gap. Standing aim clears it, and it frames the room as a range.
- **Weak Point:** a knee-high firing line facing the drone lanes.
- **Holding Angle:** two pillars. Behind the left one you can see only the right drone, and vice versa, so "peek one angle at a time" is now built into the room instead of only stated.
- **Recon:** a tall crate stack in the far corner, one more place something could be hiding. It barely changes where the dart works: 657 vs 677 floor spots can still reveal the drone.
- **Smoke:** a wall across the room with one 3m gap that both drones watch. The Smoke Wall is 3.2m wide: smoke the gap, then walk through it, which ties in with 16.8. The crossfire drone no longer sees the entry.
- **Flash:** a wall just short of the drones with a corner on the west side. 44 spots behind it are hidden from every drone yet within the 7m flash radius of all of them, including both ends of the patrol: walk up safely, pop the flash, peek. The Flash ult orb moved past the corner, so it's now a risk-for-reward pickup instead of a free one.
- **Dash:** two staggered cover blocks with an exposed lane between them. Both hiding spots are hidden from all drones and the lane is watched by three, so the dash is the way across. The orb sits beside the second block.
- **The Round:** a 2.2m crate stack splits the approach into two lanes, one to each site cover, so there's more than one way in. A low flank crate gives an off-angle on the patrol.
- All pieces are on the Blocking layer, so they stop drone sight and the dart scan. They carve the NavMesh, so drones path round them. They avoid the hanging lamps and wall vents from 17.1.
- Room text updated to match: the Holding Angle, Smoke, Flash and Dash intro panels, plus the Smoke, Flash and Dash objectives. Dash's "No cover here" was no longer true.

**Shadow fix found along the way:** the renders showed saw-toothed shadow bands across walls and floors. URP soft shadows were switched off, the shadowed lamps used the lowest shadow-map tier, and the moon spread one 2048 map over 50m. Now:
- soft shadows are on;
- shadow distance is 30m with 2 cascades (rooms are only 18m long);
- the additional-light atlas is 4096;
- the 8 shadowed lamps use the high (1024) tier.
These are set in `EnvironmentDresser`, so a rebuild keeps them.

**Verified:**
- Every drone's sightlines were raycast from every free floor spot, using the drones' own check. Every drone can still be seen, and so killed, from 241-572 spots.
- Each room's specific claim was tested and holds:
  - Holding Angle: each pillar hides you from exactly one drone.
  - Smoke: 168 safe spots south of the wall, and the gap is watched.
  - Flash: the safe flash spots exist.
  - Dash: both covers are hidden and the lane is exposed.
  - The Round: the entry is hidden behind the crates.
- In Play mode: all 6 patrol drones still walk their routes on the NavMesh, both Round drones can still path to the site, and all 8 rooms have a complete walkable path from entry to exit door.

## Phase 17.3: Recon pulse no longer whites out the screen

**Why:** Playtest screenshot: after using the Recon Dart in the Dash room, the whole view went white-blue and stayed that way. Two bugs combined.
- **The pulse shell was never deleted.** The shell's grow-and-fade coroutine ran on the dart, and the dart destroys itself one frame before the second pulse finishes fading. That killed the coroutine, so a 24m sphere was left around the player at full size.
- **The shell was never see-through.** The material set `_Surface` to transparent but never set the blend state or the `_SURFACE_TYPE_TRANSPARENT` keyword, so it rendered opaque. Emission plus the new bloom from 17.1 turned that leftover sphere into a white-out.

**What changed:**
- Each pulse shell gets a timed `Destroy` from the engine the moment it spawns, so it disappears even if the dart is gone.
- The shell uses URP Particles/Unlit, which is already in builds via the smoke material. It now has proper alpha blending, no depth write and no shadows, so it is genuinely transparent and fades to nothing.

**Verified** in Play mode: a test dart stuck and pulsed in the Dash room. The shell reached full size at alpha 0.06, and after 4s there were no pulse objects left, even though the dart had already destroyed itself.

## Phase 17.2: Recon Dart aim starts at 0 bounces

**Why:** Playtest: "the dart starts with one bounce not 0 bounces". The default was hard-coded to 1, so a player who just pressed F and aimed at a spot got a ricochet they didn't ask for, and the dart landed somewhere other than where they pointed. A straight shot is the expected default; bouncing is the advanced option for reaching round a corner.

**What changed:** Every aim now starts at 0 bounces, and right-click still cycles 0/1/2. The count resets on each aim instead of remembering the last throw, so pressing F always behaves the same way. The placeholder hint text was updated to match.

**Verified:** I set bounces to 2, then started an aim. It reset to 0, and right-click moved it to 1.

## Phase 17.1: night-shift facility environment pass

**Why:** Playtest feedback: "the features gameplay is good but... whats lacking is the vibes", and every room felt like "just one box". The rooms were flat-lit grey boxes. Nothing told you where you were or gave the space a mood, so each room read as a bare test chamber. The chosen direction was a **night-shift facility**: dark, cool ambient light, warm work lamps hanging from the rafters, and each room's colour picked out in glowing strips. That gives the drones a setting that makes sense (security drones in an empty facility after hours), and the pools of lamp light naturally draw the eye to the cover and the drones.

**What changed:**
- **New editor tool `Assets/Editor/EnvironmentDresser.cs`**, under the menu *Vantage > Environment > Rebuild All / Rebuild Dressing / Apply Night Lighting*. The dressing is generated, not hand-placed, so it can be rebuilt in one click after any layout change. The dressing is purely visual: every piece has its collider removed and is marked static, so gameplay, drone sight and the NavMesh are untouched.
- **Each room now has:**
  - I-beam rafters with wall pilasters, plus purlins;
  - wall caps and baseboards;
  - pipes and brackets on the east wall, vents on the west;
  - yellow safety lines on the floor;
  - **emissive accent strips in the room's colour** (amber, red, orange, blue, teal, white-gold, violet);
  - **four hanging lamps with real spot lights**, one of them casting soft shadows.
- **Night lighting:**
  - procedural night skybox with no sun disc;
  - cool three-colour ambient light;
  - dark blue exponential fog;
  - a dim blue moon as the directional light.
- **Pipeline changes so the lamps work:** URP now runs Forward+ with per-pixel additional lights and additional-light shadows on. With the old limits, most of the 32 lamps would have been culled.
- **Bug found and fixed:** the post-processing profile had held four *null* components since Phase 6a. They were added but never saved into the asset, so the game has never actually had post-processing. The profile is rebuilt properly with:
  - ACES tonemapping;
  - colour adjustments;
  - a slight cool white balance;
  - bloom, so the strips and bulbs glow;
  - a soft vignette.

**Verified:**
- Rendered with post-processing from the Smoke, Holding Angle and The Round entry spots.
- Accent strips and bulbs glow.
- No blown-out floor hotspots.
- The back walls where the drones sit are still lit, so the drones' red eyes read clearly from the entry.
- Layout and gameplay are unchanged. Pass 2, which reshapes each room's layout, follows separately.

## Phase 16.8: walk through smoke

**Why:** Playtest request - the player wants to walk through their own smoke. The Smoke Wall's invisible blocker had a solid collider, so the player bumped into it like a wall. In tactical shooters smoke is something you push *through*, not around: walking into or through it is how you cross a sightline with cover.

**What changed:** Player <-> Smoke collisions are switched off in the physics layer matrix (`ProjectSettings/DynamicsManager.asset`). The blocker stays solid for raycasts, which the collision matrix doesn't affect - so drone sight is still blocked, and bullets still pass through as in Phase 15.20. Standing *inside* the smoke hides you: a drone's sight ray hits the smoke's face before reaching you.

**Verified** in Play mode in the Smoke room: the player walks from z=-7.5 to z=-1.5 straight through a smoke at z=-5; standing inside it, neither drone can see them, while the crossfire drone can see the same spot with the smoke removed.

## Phase 16.7: Recon Dart aim preview readable at a glance

**Why:** Playtest screenshot: aiming at a nearby wall, the preview showed a blue drip hanging off the crosshair and the player "was a bit confused at first to where the dart would land". Looking straight down the arc, the part of the line nearest the eye drew as a thick blob over the crosshair, and the landing marker (a small flat disc) was lost inside it.

**What changed:**
- The arc isn't drawn within 1.5m of the camera, then fades in and widens over its first 20%, so the far end - where the dart actually goes - reads strongest and nothing covers the crosshair.
- The landing spot is now a **hollow ring with a centre dot**, flat on the surface, gently pulsing - a target, clear from any angle.
- A small dot marks **each bounce point**, so a bank shot's ricochet is visible too.
- The BOUNCES hint moved further below the crosshair, out of the line's usual path.

**Verified** by rendering the playtester's situation (aiming flat at a wall ~6m away, 0 bounces): a thin line drops to a clear ring-and-dot on the wall, nothing over the crosshair. A 1-bounce aim shows the bounce dot on the wall ahead and the line continuing off it.

## Phase 16.6: Recon Dart rebuilt as an aimed, bouncing scout arrow

**Why:** Playtest feedback: the Recon Dart was "some weird thingy" - a glowing ball lobbed by physics that revealed every drone within 8m *through walls*, wherever it happened to land. There was no skill in it: no aiming beyond pointing, no way to place it deliberately, and since walls didn't matter neither did placement. The scout-arrow design from tactical shooters fixes all three: you aim with a preview, you can bank it off walls to reach round a corner without exposing yourself, and the scan only sees what the arrow can see - so *where* it lands is the decision. That is "scout before you commit" made into an actual skill. (Mechanics only: no names, models, sounds or art from any existing game - the arrow is built from primitives, sounds are CC0 Kenney.)

**What changed:**
- **Hold F to aim** - a live cyan line shows the full arc, every ricochet and a marker where it will stick. **Right-click while aiming cycles 0/1/2 bounces**; the count shows under the crosshair. **Release F to fire.** Opening a menu/card/settings cancels the aim without spending the charge.
- **An actual arrow:** dark shaft, glowing pointed head, two fins, a short trail and a small light; it points along its flight path. Fast (22 m/s) with lighter gravity, so it flies in a readable arc.
- **Bounces then sticks:** each ricochet reflects off the surface losing 20% speed (metal tick sound); the next contact buries the tip (impact sound). Hitting a drone sticks to the drone.
- **Two scan pulses, 1.2s apart:** each reveals drones within 12m that have **line of sight** to the dart, for 2.5s. Walls block the scan (same layer drones use for their own sight). A visible expanding shell plus a hum marks each pulse.
- The preview and the real arrow run the **same flight code** (fixed 0.02s sub-steps), so the arrow lands where the line says - and fast frames can't tunnel it through thin walls.
- Recon room text and the unlock card now explain aim / right-click / release.
- Ability charges (1 per room) and the Recon room's exit gate (opens on use) are unchanged.

**Verified** in Play mode in the Recon room: preview vs real landing point for 0/1/2 bounces agree within the 0.12m tip bury (e.g. predicted (-120.00, 1.94, -8.80) / actual (-120.00, 1.95, -8.68) on a one-bounce shot). A dart stuck on the near side of the partition, 8m from the hidden drone, did **not** reveal it (wall in the way); one landed past the partition revealed it on the first pulse. From the room entry, 60 of 210 direct throws, 76/199 one-bounce and 76/199 two-bounce throws reveal the drone - plenty of valid shots, but not every throw. Renders of the arrow in flight and of the aim preview checked.

## Phase 16.5: armor

**Why:** The playtester asked for a shield for the player. Of the three options discussed (per-room armor, a regenerating shield, a barrier ability), per-room armor was chosen: it is the closest match to how armor works in tactical shooters (a separate pool on top of health, not a free heal), and it fits the per-room budget Phase 16 already set up for ability charges. Since every death now ends the run (Phase 9), it also gives newer players a bit more room for one mistake per room - without the regenerating option's "hide and wait" loop, which would undercut the push to use utility.

**What changed:**
- **50 armor** on top of 100 health. Damage hits armor first; a hit bigger than the armor left takes the rest from health. The Overwatch damage reduction applies before armor.
- **No recovery mid-room; refilled on every room entry** (and on checkpoint retry). Health still does *not* refill between rooms, so health is the run-long resource and armor is the per-room one.
- **HUD:** a slim blue bar with its number, directly above the health bar in the same style.
- **Debrief:** DAMAGE TAKEN now shows how much armor absorbed, e.g. "140 (65 by armor)".

**Verified** in Play mode: 20-damage hits take armor 50 -> 30 -> 10, then the third hit splits 10 armor / 10 health (HP 90), the fourth is all health (70); the next room refills armor to 50 while HP stays 70; a 15 hit leaves 35 armor; with Overwatch's 0.5x a 20 hit takes 10 armor. The run stats total 95 damage with 65 absorbed. HUD render shows the armor bar at 25/50 above health at 70.

## Phase 16.4: checkpoint retry starts clean - full HP, no old smoke, arrival grace

**Why:** Playtest report: after RETRY FROM CHECKPOINT, the smoke thrown before dying was still standing, and the health bar read 60 instead of 100.
- **Smoke:** nothing cleared deployed utility on a retry, so the old smoke just ran out its timer in the "fresh" room.
- **Health:** the restore *did* set 100 - the player was then shot back down to 60 before they could see anything. Traced it: in Rooms 5-8 the crossfire drone moved in Phase 15.19 can see the exact entry spot, and the retry teleports the player there while the screen is still black (0.45s hold + 0.35s fade-in). Both Smoke-room drones have a 0.5s reaction and 20 damage, so two hits landed during the fade: 100 -> 60. Normal room transitions had the same blind window.

**What changed:**
- **Arrival grace:** drones can't see the player while a room transition is faded, nor for 1s after it clears (`RoomManager.entryGraceSeconds`) - used by both normal room transitions and checkpoint retry. Firing ends it early, so it can't be used to shoot drones that can't see you back. Like a round-start barrier: you get to read the room before it can read you.
- **Retry clears deployed utility:** smoke walls (blocker + cloud), flash bursts, recon darts and their reveal pulses from the failed attempt are removed.
- **Found while testing - unlock cards repeating:** the unlock-card tracker re-synced its "already shown" flags to the live unlock state every frame. A retry re-locks Flash and Dash (back in Room 5), which reset those flags, so the Flash and Dash cards would have shown a second time on the way back through. The flags now only ever go true.

**Verified** in Play mode, stepping real frames through an actual retry (died in Room 6 with a smoke out): at the black screen the player is at the Room 5 entry with 100 HP and the smoke is gone; through the fade and the 1s grace HP stays 100 and the crossfire drone can't see them; once grace ends it spots the standing player and fires on its normal 0.5s reaction. Before the fix the same 0.8s window took 40 HP.

## Phase 16.3: end-of-run debrief

**Why:** The run ended on a single line ("BREACH SUCCESSFUL" / "MISSION FAILED") with no feedback on *how* you played, so nothing told the player what to do better next time - and nothing invited a second run. Tactical shooters end every match on a scoreboard. Separately, the win screen had **no buttons at all**: the cursor unlocked with nowhere to click, so the only way out was quitting.

**What changed:**
- **`RunStats`** tallies the whole run: total time, time per room, shots/hits (accuracy), headshot share of drone hits, kills, damage taken, each ability's uses, Overwatch uses, orbs collected, checkpoint retries. Time only counts while playing (menus, unlock cards and settings are paused). A checkpoint retry keeps counting, since the debrief covers the whole run, failed attempts included; a restart starts fresh.
- **Win screen:** two columns, RUN stats and ROOM TIMES (all 8 rooms), plus new **PLAY AGAIN** and **MAIN MENU** buttons. The world now freezes when the win screen opens, so a surviving drone can't shoot the player and open Game Over on top of it.
- **Game Over screen:** the same stats plus the room the run ended in. Buttons moved down to make space.
- **One coaching tip** on both screens, picked from the weakest area in priority order: low accuracy -> "stop moving, tap or burst"; mostly body shots -> "aim at the red eye"; unused utility -> "smoke, flash or dart before you peek"; Overwatch left charged -> "press [X]"; orbs left behind; heavy damage -> "crouch behind cover, walk so they don't hear you". A good run gets "Clean run. Now do it faster." Each tip points back at a mechanic the tutorial teaches, so the end screen becomes part of the teaching.

**Verified** in Play mode with a simulated run across all 8 rooms: every stat and room time is correct; collecting the same orb twice counts once; the clock stops when the win screen opens; the orb tip fires for 1/2 orbs. Rendered both screens: columns line up, nothing overlaps. PLAY AGAIN reloads straight into a fresh run (menu skipped, time unfrozen, stats reset).

## Phase 16.2: footsteps and walking

**Why:** Sound is half of how tactical shooters are played: running is loud and gives your position away, walking is quiet. Vantage had no movement sound and drones had no hearing, so there was never a reason to move carefully - running was strictly better than anything slower. This adds a cost to speed that fits the core dynamic: take your time on the approach, and you get to choose when the fight starts.

**What changed:**
- **Running footsteps:** a step sound every 1.9m while running on the ground (5 CC0 Kenney concrete clips, never the same twice in a row).
- **Drones hear them:** an un-alerted drone in the current room within 9m of a running footstep stops and turns toward it for 2s. It does **not** fire at the sound - but if turning gives it line of sight, it spots you through the normal reaction delay. Already-alert drones ignore sounds (so noise can't pull their aim off you), and drones in other rooms never hear you through walls.
- **Walk = hold Left Alt:** 55% speed, silent. (Shift was already Dash.) Crouching is also silent. Jumping/airborne and Dash make no footsteps.
- Every armed drone sits 12m+ from its room's entry, so entering a room running isn't punished on its own - you're only heard if you run up close.
- **Teaching it:** the Recon room text now ends "Running is loud - drones nearby turn toward your footsteps. Hold [Alt] to walk silently." (Recon is the sneak-up room: one drone "past that corner".) The Weak Point text also gained "Holding fire makes the gun climb - tap or burst" for Phase 16.1. Both still fit the panel (3 lines).

**Verified** in Play mode on the Recon drone: from an open spot 7m away outside its view cone, one running step made it turn and spot the player; from the same spot with no step it never saw them. At 12m a step isn't heard. A drone in another room isn't affected. Holding walk (or standing still) makes movement silent.

## Phase 16.1: recoil spray pattern

**Why:** Holding the trigger was exactly as accurate as tapping - the only accuracy rule was movement spread (Phase 1). In tactical shooters the gun itself also punishes a long spray, and the fix is a skill you can learn: tap or burst, or pull down against a *fixed* pattern. That fits Vantage's core dynamic (the patient, controlled shot wins) and gives the Range room something to practise beyond "stand still".

**What changed:**
- Each shot in a continuous spray kicks the view by a fixed amount: up hard for the first few shots, then a smaller climb with a left/right snake (about 5 degrees up over a full 12-round magazine). The pattern is identical every time, so it can be learned.
- The **first shot is always where the crosshair is**; the kick happens after it. A pause longer than 0.4s starts a fresh spray.
- The kick moves the camera itself rather than secretly bending the bullet, so the crosshair visibly climbs and the player can pull the mouse down against it. After 0.3s without firing the view settles back at 10 degrees/s. The kick is kept separate from the player's own aim, so settling never undoes their mouse movement.
- Room changes clear any leftover kick.

**Verified** in Play mode: 12 shots fired in one spray climb 0.9, 1.8, 2.6, 3.2 ... 5.05 degrees with the sideways snake; after a pause the next shot starts the pattern from the beginning (0.9); recovery returns the view exactly to where the player was aiming.

## Phase 16: ability charges per room + Overwatch orbs

**Why:** Two Valorant-inspired gaps found when reviewing the game as a whole. (1) Abilities were cooldown-only, so utility was effectively unlimited - you could smoke, wait 6s and smoke again, and *when* to spend it never mattered. In tactical shooters utility is a limited budget per round, and choosing when to use it is the skill. (2) The ultimate unlock card already told players Overwatch Protocol "charges from kills and Overwatch orbs", and `UltOrbPickup` existed, but no orb had ever been placed in the scene - the card promised something the game didn't have.

**What changed:**
- **Charges:** each ability now has a per-room charge count, refilled on every room entry (including checkpoint retry). The cooldown is now just a short lockout between uses so charges can't be double-tapped. Recon Dart 1 charge (1s), Smoke Wall 2 (1.5s), Flash 2 (1.5s), Dash 2 (0.6s - short enough to chain two dashes). Each HUD tile shows charge pips in its top-right (filled = left, hollow = spent); a tile with no charges left stays fully greyed out for the rest of the room instead of looking like a cooldown about to finish.
- **Orbs:** two gold glowing orbs (+25 ult charge each - one segment of the four-segment bar, the same as a kill; 50 was tried first but meant the two orbs alone filled the ult without a single kill), one in the Flash room and one in the Dash room - the rooms right before The Round, so grabbing both (plus a couple of kills) means arriving at The Round with Overwatch ready. Walk through to collect. Checkpoint retry respawns collected orbs from Room 5 onward, so a retry matches the first arrival at the checkpoint (the ult charge itself was already snapshotted there).
- The room exit gates from Phase 15.7 still open on the first use of the room's ability - charges don't change that.

**Verified** in Play mode: Smoke on Room 5 entry shows 2/2 (pips ●●); use, immediate repeat blocked by the lockout, second use, third use refused at 0 charges, pips ○○ and tile fully greyed; entering Room 6 refills Smoke to 2 and Flash starts at 2/2; Dash chains two uses then refuses a third; Recon allows one use. Each orb adds exactly its charge amount (tested at 50 before the drop to 25), disappears, and a second trigger adds nothing; checkpoint reset restores both. The Smoke room's exit door opens on the first smoke with one charge left. A camera render of the Flash-room orb shows a readable gold sphere with a soft light pool, not a blown-out glare.

## Phase 15.21: blind drones fire at where you were, not where you are

**Why:** After 15.20 let bullets pass through smoke both ways, an alerted drone that couldn't see the player still aimed every shot at the player's *live* position - so it tracked you perfectly through smoke (or around a corner) as if it could see you. The playtester suggested giving drones bad aim while you're in smoke. Considered and rejected a flat higher miss chance: it's random, so getting hit or not feels like luck and teaches nothing. Instead, drones now behave like a real shooter who's lost sight of you.

**What changed:**
- Each Sentinel tracks a **last known player position** - updated every frame it can see you, and when it's shot or called in by a roommate (`ForceAlert`).
- When it fires without sight (`FireBlind`), it aims at that last known position plus a small spread (0.5m around, +/-0.3m vertical), and the shot is a real ray: it only damages the player if it physically reaches their collider. Missed rounds leave bullet holes where they land, so the player can see the burst chewing up the spot they just left. Cover still stops these shots; smoke doesn't.
- Aimed fire when the drone *can* see you is unchanged.
- The result is a learnable rule that fits the core dynamic: shoot from smoke and stand still, you get hit; shoot and reposition, you don't.

**Verified** in Play mode with smoke between the player and an alerted drone, 40 shots per case at an open spot 11m away: stand still 28/40 hits; step 0.75m 0/40; step 1.5m 0/40; no smoke (drone sees you) 35/40, same as before. At a spot where a crate covers the player's lower body, half the blind shots hit the crate instead - partial cover still helps while blind.

## Phase 15.20: denser smoke you can shoot through

**Why:** Playtest feedback, two parts. (1) Up close the smoke was still faintly see-through - door and crate outlines showed through it, because the puffs spawned in a layer only 0.4m deep, so a close view looked through very few overlapping puffs. (2) The player couldn't shoot through it: the screenshot showed a bullet hole sitting on the (invisible) smoke box. The smoke was on the Blocking layer, which stopped drone sight, drone bullets *and* player bullets alike.

**What changed:**
- **New `Smoke` layer** for the Smoke Wall's blocker. Drone sight (`CanSeePlayer`) treats it as solid; bullets in **both** directions pass through it. The player's weapon explicitly excludes it (its hit mask is "Everything", which would otherwise include any new layer).
- **This reverses part of Phase 15.12**, which had smoke stopping drone bullets. On reflection that went against Phase 10's original intent - a drone shot from inside a smoke is meant to return fire ("getting shot from... through smoke is no longer a free action"). Now smoke is a real trade: they can't see you, you can shoot through it, but a drone you hit will spray back through it. Crates, walls and doors still stop all bullets.
- **Denser look:** puffs now spawn through a 1.0m-deep volume (the blocker stays 0.4m), 70 -> 110 puffs, sizes 1.5-2.4 -> 1.8-2.8, per-puff opacity 0.9 -> 1.0.

**Verified** in Play mode: a player shot through the smoke at a drone now hits the drone (the same shot with the old mask hit `SmokeWall`); the drone still can't see the player through it; an alerted drone's return fire through the smoke lands (100 damage from 5 shots). Close-up capture from just behind the smoke: nothing behind it visible except incoming tracers.

## Phase 15.19: crossfire drones back inside Rooms 5-7

**Why:** Found while verifying Phase 15.18: in Smoke, Flash and Dash, the second ("crossfire") drone added in Phase 10 sat at z=7, near the exit door. Phase 15 later moved every exit door 2m into the room (to z=6.2) and Phase 15.1 filled the gap beside each door with short wall pieces - which put those three drones *behind* the wall, in the airlock space. Their line of sight hit `WallFillerLeft` before reaching any part of the room: zero coverage. So Rooms 5-7 had effectively been one-drone rooms since Phase 15, Phase 10's crossfire was gone, and the Phase 15.7 "or clear every drone" door fallback was impossible there (you can't shoot a drone through a wall).

**What changed:** Moved `Sentinel_R3_02` (Smoke), `Sentinel_R4_02` (Flash) and `Sentinel_R5_02` (Dash) 2.2m forward to z=4.8, in front of the door's side walls, still facing their original diagonal hold points so the crossfire geometry Phase 10 designed is unchanged. Each spot checked first: on the NavMesh, no overlap with walls/cover, clear lines to the room centre and entry.

**Verified** in Play mode over an 11x11 grid of floor positions per room: each moved drone now sees the player from 115/121 spots in Smoke and Flash (the misses are behind the cover crate) and 121/121 in Dash (no cover - that room is about Dash), and the player has a clear shot back at it from the same spots. Before: 0 spots.

**Difficulty note:** this restores Phase 10's intended crossfire, so Rooms 5-7 are harder than they were in playtests since Phase 15 - those playtests were effectively against one drone per room.

## Phase 15.18: Smoke Wall faces the right way and looks like smoke

**Why:** Playtest screenshot: pressing Q spawned a solid teal slab running *away* from the player down the lane. Two bugs:
1. **Wrong orientation.** The code's own comment said the wall should span across your view, but it was rotated to look along the player's *right* vector - which turned its 3.2m width to point down the lane and left only its 0.4m thickness across your view. It barely blocked the lane it was meant to block.
2. **Not smoke.** It was a plain cube with a URP Lit material that only half-set "transparent" (no surface keyword / blend / depth-write settings), so it rendered as an opaque teal block. Even fixed, a transparent box would read as a force field, not smoke - and a see-through smoke would let the player see drones that can't see them, a one-way advantage that undercuts "utility as a trade".

**What changed:**
- Orientation fixed: the wall now spans 3.2m across your facing direction, 0.4m deep, 2.2m tall.
- The cube is now an **invisible blocker** (still on the Blocking layer, so drone sight and drone bullets both stop at it, exactly as before). What you see is a new particle cloud over it: 70 soft puffs from a procedurally generated wispy-edged texture (`tex_smoke_puff.png`, made in code - no external assets), using a proper transparent URP particle material (`Mat_Smoke`, referenced from the ability rather than `Shader.Find`'d so the shader is guaranteed in builds). Puffs bloom in over 0.35s, drift and turn slowly, and thin out over the last quarter of the 6s duration (same expiry cue as before). Dense enough in the middle that you can't see through it either.
- Colour softened from saturated teal toward a grey-teal smoke tone.

**Verified** in Play mode: wall extent 3.20m across view / 0.40m along it; box renderer off, 70 puffs live with `Mat_Smoke`; positioned captures from the player's side (behind-smoke crate and drone fully hidden), from a distance (reads as a soft cloud with no hard edges) and from the drones' side. Blocking: a drone with clear sight did 100 damage from 5 shots; with the smoke between, it could not see the player and did 0.

## Phase 15.17: sensitivity slider drives both look axes; lower minimum

**Why:** Checking the sensitivity range turned up a real bug: the settings slider only ever changed horizontal look. Yaw is handled by `PlayerController` (which the slider updates), but pitch is handled by `FollowCamera` on the child camera, which had its own separate `mouseSensitivity` fixed at 2 and never read the setting. A player who set sensitivity to 1.0 got 1.0 left/right but 2.0 up/down - vertical movement twice as fast as horizontal, which makes precise aim (headshots especially) feel inconsistent. Separately, the slider's floor was 1.0 and the playtester was already sitting at it, so it was too high for low-sensitivity players.

**What changed:**
- `FollowCamera` now reads sensitivity from its parent `PlayerController`, so one setting drives both axes (its own field remains only as a fallback if no player is found).
- Slider minimum lowered 1.0 -> 0.3 (max still 10; the code clamp already allowed down to 0.1).

**Verified** in Play mode: moving the slider to 0.3 and 4.0 updates both the yaw and pitch sensitivity to the same value and the on-screen readout; playtester's saved setting restored afterwards.

## Phase 15.16: Holding Angle eased back to a teaching room

**Why:** Playtest feedback: Room 3 was dying-hard even for an experienced FPS player, so a newcomer would bounce off it. It's only the third room, and it had picked up four separate difficulty increases in one session: a second drone (15.6), damage 15 -> 20 (15.9), the door requiring both kills (15.7), and standing behind a crate no longer hiding you (15.13) - plus a death there restarts from Room 1, since the checkpoint is at Room 5. Each change was reasonable alone; stacked on an early room they weren't.

**What changed:** Kept both drones (the "clear one angle at a time" lesson stays) and kept damage at the game-wide 20, but slowed Room 3's drones only: reaction delay 0.8s -> 1.5s (time from spotting you to first shot) and fire interval 0.8s -> 1.2s. A peek now gives a comfortable window to aim and land a headshot (one-shot kill) before taking a hit, and a bad peek costs one hit instead of two. The difficulty curve now reads: Room 3 1.5s, Rooms 4-7 0.5s, The Round 0.35s.

## Phase 15.15: Game Over buttons stack without a gap

**Why:** Playtest screenshot: on the Game Over screen before reaching the checkpoint, there was a big empty gap between RESTART and MAIN MENU. Each button had a fixed height authored for the 3-button checkpoint layout (RETRY FROM CHECKPOINT / RESTART FROM BEGINNING / MAIN MENU); the 2-button layout just hides the middle one, leaving its slot empty.

**What changed:** `GameOverController` now stacks whichever buttons are visible top-down from the first slot (60px apart) each time the screen opens. Verified in Play mode: no checkpoint = RESTART at -110, MAIN MENU at -170; checkpoint reached = the three buttons at -110 / -170 / -230.

## Phase 15.14: unlock cards can't be skipped by accident; one ultimate notification, not two

**Why:** Playtest bug: the Overwatch Protocol card "sometimes pops up and sometimes doesn't". The card closes on Space or left-click and accepted that input from its very first frame. The ultimate fills on a kill, so the card opens mid-firefight: the killing click could land on the same frame the card opened (Unity doesn't guarantee whether `Weapon` or `AbilityUnlockCardUI` updates first, so this varied run to run), or the player's next click a split second later closed it - either way it vanished before it could be read. Separately, on the first charge-up the card and the "READY - PRESS [X]" banner both appeared at once, saying the same thing twice.

**What changed:**
- **Dismiss grace period:** the card ignores Space/clicks for its first 0.6 real seconds (unscaled time, since the card pauses the game). The "[SPACE] OR CLICK TO CONTINUE" hint is hidden until then and fades in as input becomes accepted, so the player can see when it's safe to continue. The card's own button goes through the same check. Applies to all five cards (Recon, Smoke, Flash, Dash, Overwatch), not just the ultimate.
- **One notification per charge-up:** the first time the ultimate fills, only the card shows (it has the full explanation and the key). Every later charge-up shows only the short banner.

**Follow-up fix (same phase):** the next playtest still showed both on the first charge-up. The banner check only asked "has the ultimate card been shown yet?" - correct if `UltimateHudUI` updates first on the charge-up frame, but if `AbilityUnlockCardUI` updates first, the card has already opened and marked itself shown by the time the HUD asks, so the HUD read it as a later charge-up. The original verification below only tested the HUD-first order and wrongly claimed it held "regardless of which script updates first". Fix: the HUD also skips the banner while the ultimate card is the one on screen (`IsShowingUltimateCard`). Re-verified with **both** update orders explicitly: first charge-up = card only, no banner (and none after dismiss); second charge-up = banner only.

**Verified (original, HUD-first order only)** in Play mode by stepping `Update()` manually (the Editor's frame loop was stalled again, see earlier phases): card opens on charge-up with banner suppressed; dismiss attempts at 0s and 0.3s ignored, hint invisible; at 0.7s hint fading in and a click closes the card and restores time scale; a second charge-up shows the banner ("...PRESS [X]") and no card.

## Phase 15.13: crouch actually matters at crates; ultimate banner says which key

**Why (crouch):** Phase 11 raised the standing eye height used for drone sight to 1.5 (above the 1.2m crates) so a standing player shows over cover and only crouching hides you. But that change only edited the script's default - the scene's saved value stayed at the old 1.0, and a saved scene value overrides a script default. So in the actual game, standing behind any crate hid you completely and crouch did nothing at cover, contradicting Room 3's own instruction ("Hold [Ctrl] to crouch behind cover"). Found while verifying Phase 15.12.

**What changed (crouch):** Scene value set to 1.5, matching Phase 11's intent. Verified in Play mode at both Room 3 crates: standing - both drones see you and hit (100-120 damage from 6 shots); crouched - neither sees you, 0 damage.

**Why (banner):** Playtest question: the "OVERWATCH PROTOCOL READY" banner announced the ultimate was ready but never said how to use it - the key was only shown as a small "[X]" on the ability ring.

**What changed (banner):** Now reads "OVERWATCH PROTOCOL READY - PRESS [X]", built from `UltimateController.activateKeyDisplay` so it can't drift if the key changes. Fits the existing banner box (308px text in a 360px box). Verified the live banner text in Play mode.

## Phase 15.12: drone shots no longer pass through cover

**Why:** Playtest bug: in Holding Angle the player kept taking damage while hiding behind a crate. Root cause: `Sentinel.Fire()` never checked line of fire - it applied damage straight to the player whenever its fire timer ran out while Alert. And Alert outlives sight on purpose: it holds for 2.5s after being shot (Phase 10 #2) and spreads to every drone in the room the moment one spots the player (Phase 10 #3). So: peek once, get seen, duck back - and both drones kept hitting you through the crate for 2.5 seconds, including the one that never saw you at all. The bug dated from Phase 10, but Phase 15.6's second drone made it far more noticeable, and 15.6's verification only checked what each drone could *see*, not where its shots *landed*.

**What changed:** `Fire()` now linecasts from the muzzle to the player against the Blocking layer. If something's in the way, the shot hits it (tracer ends there, bullet hole on the surface) and does no damage. The aim point is now the same point `CanSeePlayer()` checks, so "it can see you" and "it can hit you" always agree. Alerted drones still turn and fire at your last known position - you hear and see suppressing fire hitting your cover - they just can't hurt you through it. Smoke Wall is on the same Blocking layer, so it now stops bullets as well as sight, which is what "Use Smoke Wall to block their view" already promised.

**Verified** in Play mode, 10 shots from each Room 3 drone per position: 0 damage behind the main crate (both stances) and behind the crouch crate; in the open, only the drone with a clear line did damage, the other's shots hit the crate.

## Phase 15.11: "OVERWATCH PROTOCOL READY" banner text centred

**Why:** Playtest screenshot: the ultimate-ready banner's text sat in the bottom half of its dark box instead of the middle. The label was anchored to the box's centre but had its pivot at its own top edge, so its top edge (not its middle) landed on the box's midline - pushing the whole line of text down by half its height.

**What changed:** Label pivot set to centre (0.5, 0.5). Verified via world-corner geometry: label and box now share the same centre exactly (both X and Y).

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
