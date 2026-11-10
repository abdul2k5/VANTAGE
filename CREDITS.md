# Vantage — Third-Party Asset Credits

Most assets below are CC0 (public domain) - no attribution is legally required, but the creators are credited here anyway. The Sentinel drone model is CC-BY and its attribution (below) is required, not optional.

## Music

| Use | File | Title | Artist | License |
|---|---|---|---|---|
| Menu | `Assets/Audio/Music/TremLoadingloopl.wav` | Loading Screen Loop | Brandon Morris | CC0 |
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

### Gunshot (layered from the packs above)

`Assets/Audio/Gunshot/gunshot_punchy_blaster.wav` is not a downloaded sound. It is mixed in the editor by `Assets/Editor/GunshotOptionBuilder.cs` from three of the Kenney clips above, so it is CC0 as well.

| Layer | Source clip | Pack |
|---|---|---|
| Body | `laserLarge_000.ogg` | Sci-fi Sounds |
| Low thump | `impactPlate_heavy_001.ogg` | Impact Sounds |
| Top (the previous gunshot) | `laserSmall_002.ogg` | Sci-fi Sounds |

## 3D Models

| Use | Asset | Author | Source | License |
|---|---|---|---|---|
| Player rifle viewmodel + Sentinel-held weapon, Range targets, Range cover crates | [Blaster Kit](https://kenney.nl/assets/blaster-kit) | Kenney | kenney.nl | CC0 |
| Sentinel drone visuals | [Stinger Drone](https://poly.pizza/m/6CUQX98vha4) | Aaron Clifford | Poly Pizza | [CC-BY 3.0](https://creativecommons.org/licenses/by/3.0/) |

> Stinger Drone by Aaron Clifford [CC-BY] (https://creativecommons.org/licenses/by/3.0/) via Poly Pizza (https://poly.pizza/m/6CUQX98vha4)

Files: `Assets/Art/Models/BlasterKit/` (Blaster Kit), `Assets/Art/Models/Drone/StingerDrone/` (Stinger Drone). See `CHANGELOG.md`, Phase 6e, for how each was reworked (recolored to match the existing gunmetal/red/orange palette, weakpoint repositioned onto the drone's sensor pod).
