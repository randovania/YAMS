# Changelog
All notable changes to this project will be documented in this file.

This format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)

## [1.0.3] - 2023-11-24
### Added
- Plasma Beam Chamber's crumble blocks will be gone when the softlock prevention setting is turned on.


## [1.0.2] - 2023-11-24
### Added
- Shell script to make launching randomized game easier on Flatpak.
- Changelog file to keep track of changes.
- A basic JSON schema.

### Changed
- Bumped the [UndertaleModLib](https://github.com/krzys-h/UndertaleModTool) dependency from commit `9d7767df7ede61563ab58564ed2f900631a217d3` to `64b52a65ee4b0e8eecfa59ebf0a440eac52f55e3` to use less memory and be faster.

### Fixed
- Visual time of day discrepancy with Septoggs and the tileset if started at GFS Thoth.
- A flipped water turbine if the vanilla water turbine was set to be changed to one.
- Crash when starting the game and loading a save room which contains a destroyed water turbine.
- "Cancel" button not working properly on "Toggle" Missile-Mode.


## [1.0.1] - 2023-10-08
### Fixed
- Collecting suits right after a room transition no longer causes issues.


## [1.0.0] - 2023-10-06
### Changed
- Text on the pause screen to indicate a feature that lets you see room names on there


## [0.2.3] - 2023-10-24
### Fixed
- A bug, where one Metroid needs to be killed when the DNA goal was set to 0.


## [0.2.2] - 2023-10-21
### Fixed
- Attribution for item drops

## [0.2.1] - 2023-10-20
No changes. Release was done, as the 0.2.0 tag was on the wrong branch.


## [0.2.0] - 2023-10-20
### Added
- Ammo and health drops as new items. These items act as normal drops from enemies.
- Add speed booster upgrades as new items. These items reduce the amount of time needed to speedboost.
- Add flashlight and blindfolds as new items. These items brighten up or darken down rooms.
- In-game randomizer credits.
- In-game randomizer spoiler log.

### Changed
- How the no-respawned blocks are patched to speed them up.

### Fixed
- The door in Water Turbine Station being half-transparent when going into the secret tunnel at the top.
- Songs in music shuffle being used wrongly.
- The intro fanfare being cut off if it's too long.
- Progressive suits giving you Gravity Suit instantly when cutscene skips are on.
- Fix the Suit effect for the Varia cutscene being wrong on Fusion mode.


## [0.1.0] - 2023-09-27
### Added
- The water turbine will now play the "water gets sucked out" cutscene like in vanilla if it is in the vanilla location.
- Forced Power Bomb drops in Thoth rooms with bomb blocks if you have none.
- Forced Super Missile drops in the A3 room near the EMP room if you have none.

### Changed
- If softlock prevention is on, then the shoot blocks in the bg3 bottom gamma room will be replaced by bomb chains in order to make logic less confusing and cause less issues for future entrance rando.
- Error messages to be more detailed

### Fixed
- The A5 activation cutscene looking janky if cutscene skips were on.
- Water Turbines will not magically respawn if you have passed throguh them.
- The Screw Attack blocks being existant but invisible if you leave from the A3/A4 pipe rooms with the "Have screw attack blocks before pipe" option.
- Items properly changing on the minimap, if an item and Metroid share the same minimap tile.
- Ammo doors being incorrectly locked in boss arenas.
- Music shuffle crashing


## [0.0.25] - 2023-09-07
### Changed
- When coming from the right side in drill excavation side, the drill event will be marked as done, thus allowing an escape should mines be your starting location.


## [0.0.24] - 2023-09-07
### Added
- Suppoer for Progressive Jumps (Hijump -> Space Jump) and Progressive Suits (Varia -> Gravity).

### Fixed
- Logbook names for hints having cases where they aren't synced up with Randovania's region names.
- Water turbines / water level respawning with vanilla water turbines.


## [0.0.23] - 2023-09-03
### Changed
- Hidden models to use a new distinct/hidden sprite, instead of using the nothing sprite.
- Skipping the Save Cutscene to be a different option, instead of being coupled to Skip Gameplay Cutscenes.
- ImageSharp dependency to be more recent.
- The Skip Gameplay cutscees option will also skip the baby cutscene and A5 activation cutscene.

### Fixed
- Water turbines not blocking you to be able to go through the transition if they're placed on the edges of a room.
- Some water turbine bugs if it wasn't randomized.
- Serris being able to permalock you.
- ELM being bugged.
- Some issues when starting with ammo.
- Issues when revisting boss rooms (Arachnus, Torizo and other rooms).


## [0.0.22] - 2023-08-26
### Added
- Soids to destroyed water turbines if they're on room bounds, so you cannot completely freeze the game by zipping.
- Music rando.

### Changed
- EMP Battery slots to activate instantly rather than having to wait ~1.5 seconds.

### Fixed
- The "Water Turbine Station" room never having any water and the water fans not being hittable, if the vanilla water turbine was shuffled and the room was entered from the top.
- Right facing water turbines not being hittable with bombs.
- All water turbines being gone from the game once you entered "Water Turbine Station" from below.
- Helper Septoggs despawning under certain conditions.
- Shoot block existing in "Ice Beam Chamber Access" if "Softlock Prevention" is on.
- Crashes when spawning in a room with water turbine doors.
- The doors in Serris Arena not locking correctly in Door Lock Rando.


## [0.0.21] - 2023-08-23
### Changed
- Doors to shine brighter in the dark.


## [0.0.20] - 2023-08-23
### Changed
- The Thoth doors to appear in front of the bridge.


## [0.0.19] - 2023-08-23
### Fixed
- Water turbines appearing even though you alredy crossed through them.


## [0.0.18] - 2023-08-23
### Fixed
- Door bugs when replacing the original water turbine in A2.


## [0.0.17] - 2023-08-23
### Added
- The Hydro Station Water Turbine as a shuffleable door.

### Changed
- A *slightly* cleaner error message will appear if your input game wasn't 1.5.5
- Missiles to not despawn once out of the screen.
- When leaving the item after Arachnus, and then coming back to it, the item will now always get spawned in the middle of the room, rather than the place that Arachnus died at.

### Fixed
- Being able to get permalocked in a boss room if you're repeatedly exiting and entering it.
- Doors locking when fighting Serris.
- Suit cutscenes playing while you're fighting Metroids.
- The wrong font being used in the map menu.


## [0.0.16] - 2023-08-21
### Added
- Feature where if you're on the map, and are hovering over a room while in the "place marker"-mode, you'll see the area and room name.

### Changed
- If new nest pipes option is on, the rooms containing pipes will have a purple background on the map, to be consistent with other rooms having pipes.
- Patcher to be slightly faster.

### Fixed
- "Don't open Missile Doors with Supers" option not working.
- The Distribution Center Right Exterior EMP Door not working properly.


## [0.0.15] - 2023-08-20
### Changed
- The "yams-data.json" to be formatted with more whitespace.

### Fixed
- A bug where locked ammo text was showing when you had launchers.
- Permanently locked doors showing up as Power Bomb doors.


## [0.0.14] - 2023-08-18
### Added
- Seperate text when collecting ammo without its launcher.

### Changed
- The way that room names get displayed, with there now being a setting to toggle between "Never", "on room entrance" and "always".


## [0.0.13] - 2023-08-16
### Added
- A new hint system. DNA is not numbered anymore in hint texts, and hints that used to say "hunter already started with this dna" are omitted from the hint. Should a hint only consist of such messages, a new joke-like text is shown.
- Rare chance of Skippy having Sabre320's redesign for the hit game Skippy The Bot.
- A progress bar to the floating chozo log orb.
- The A5 activated, A5 Tutorial EMP, A5 Near Zeta EMP, A5 Near Bullet Hell EMP, A5 Near Pipe Hub EMP and A5 Near Right Exterior EMP doors as shuffleable doors for door lock rando.

### Changed
- When exporting a seed has crashes, there will be more useful error messages.
- Show different text when collecting ammo but not having a launcher.

### Fixed
- A bug where pressing "load from start location", but then cancelling out of the menu, and then dying will warped you to the start location.


## [0.0.12] - 2023-08-11
### Added
- "Open Missile Doors with Super Missiles" as an option.

### Fixed
- Misalignment of the "Nothing"-Sprite.


## [0.0.11] - 2023-08-10
### Added
- Option to Skip item fanfares (Skip item cutscenes). This option makes all item pickups instantenous and removes the suit cutscenes on varia/gravity.

### Changed
- The skip gameplay cutscenes option to skip the save animation.

### Fixed
- The minimap icon of the surprise Gamma in BG3.
- Screw Attack blocks inherting AM2Random behaviour if "Screw Attack Blocks near Teleporter Pipes" option is enabled.
- Septoggs deciding to dissapear on the "Enable Septogg helpers option" after you collected bombs.
- Metroid larva draining checking for whether you collected the items in Gravity chamber and Varia chamber, as opposed to checking whether you collected Gravity/Varia.


---
Changelogs for versions 0.0.10 and before have not been properly documented yet.