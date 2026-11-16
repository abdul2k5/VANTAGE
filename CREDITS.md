# Vantage — Credits

Vantage was designed and built by **Abdul Hadi Ghani** (game design, programming, level design, interface and sound mixing) for COMP3218 Coursework 1, University of Southampton, 2026.

The same credits are shown in the game: finish The Round, then press CONTINUE on the results screen. That screen is written in `Assets/Scripts/WinCreditsUI.cs`, so a change to the assets below needs making there too.

## Third-party assets

Most assets below are CC0 (public domain) - no attribution is legally required, but the creators are credited here anyway. The Sentinel drone model is CC-BY and its attribution (below) is required, not optional.

## Music

| Use | File | Title | Artist | License |
|---|---|---|---|---|
| Menu, and the win screen after the jingle | `Assets/Audio/Music/TremLoadingloopl.wav` | Loading Screen Loop | Brandon Morris | CC0 |
| Gameplay (Range through Dash) | `Assets/Audio/Music/darkcity_1.mp3` | Dark City | cinameng | CC0 |
| The Round | `Assets/Audio/Music/fight_looped.wav` | Fast Fight / Battle Music Looped | Ville Nousiainen | CC0 |

## Sound Effects

All by [Kenney](https://kenney.nl), CC0.

| Pack | Files used | Folder |
|---|---|---|
| Sci-fi Sounds | laser/forceField/thruster/engine/explosion variants | `Assets/Audio/SciFiSounds/` |
| Impact Sounds | footstep/impact variants | `Assets/Audio/ImpactSounds/` |
| Interface Sounds | click/confirmation/tick variants | `Assets/Audio/InterfaceSounds/` |
| Music Jingles | STEEL family (win/lose stingers) | `Assets/Audio/MusicJingles/` |

Full per-sound mapping (which file plays for which game event) is in `CHANGELOG.md`, Phase 6d.

### Gunshots and the Dash sound (layered from the packs above)

The four clips in `Assets/Audio/Gunshot/` are not downloaded sounds. They are mixed in the editor by `Assets/Editor/GunshotOptionBuilder.cs` from the Kenney clips above, so they are CC0 as well. The Rifle's shot (`gunshot_punchy_blaster.wav`) is made from these three layers:

| Layer | Source clip | Pack |
|---|---|---|
| Body | `laserLarge_000.ogg` | Sci-fi Sounds |
| Low thump | `impactPlate_heavy_001.ogg` | Impact Sounds |
| Top (the previous gunshot) | `laserSmall_002.ogg` | Sci-fi Sounds |

| Clip | Made from |
|---|---|
| `gunshot_smg.wav` (SMG) | `laserSmall_002.ogg` (Sci-fi Sounds), `impactPlate_light_001.ogg` (Impact Sounds) |
| `gunshot_marksman.wav` (Marksman) | `explosionCrunch_000.ogg`, `laserLarge_000.ogg`, `impactMetal_002.ogg`, `lowFrequency_explosion_000.ogg` (Sci-fi Sounds), `impactPlate_heavy_001.ogg` (Impact Sounds) |
| `dash_burst.wav` (Dash) | the first 0.4 seconds of `thrusterFire_001.ogg` (Sci-fi Sounds) |

`Assets/Audio/Sentinel/sentinel_spotted.wav` (the drone's alert tone) is generated in the editor, not sampled from anything.

## 3D Models

| Use | Asset | Author | Source | License |
|---|---|---|---|---|
| All three player guns (the SMG and Marksman are built from the rifle's model) + Sentinel-held weapon, Range targets, Range cover crates | [Blaster Kit](https://kenney.nl/assets/blaster-kit) | Kenney | kenney.nl | CC0 |
| Sentinel drone visuals | [Stinger Drone](https://poly.pizza/m/6CUQX98vha4) | Aaron Clifford | Poly Pizza | [CC-BY 3.0](https://creativecommons.org/licenses/by/3.0/) |

> Stinger Drone by Aaron Clifford [CC-BY] (https://creativecommons.org/licenses/by/3.0/) via Poly Pizza (https://poly.pizza/m/6CUQX98vha4)

Files: `Assets/Art/Models/BlasterKit/` (Blaster Kit), `Assets/Art/Models/Drone/StingerDrone/` (Stinger Drone). See `CHANGELOG.md`, Phase 6e, for how each was reworked (recolored to match the existing gunmetal/red/orange palette, weakpoint repositioned onto the drone's sensor pod).
