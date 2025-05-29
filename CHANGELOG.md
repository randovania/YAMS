# Changelog
All notable changes to this project will be documented in this file.

This format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and this project (tries to) adhere to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)

## [2.9.0] - 2025-??-??

## Added
- Support for changing enemy hues.
- Arm Cannon Item.
- An option for random knockback.
- Metroid Lure Items.
- Option for a basic enemy randomizer.
- A knockback trap and Flitt trap item

## Changed
- Project uses dotnet8.

## Fixed
- A crash when loading up a savegame from another seed.
- Save files aren't compared against the word hash anymore.
- A typo in the error message when loading a savegame from another seed.
- Item acquisition messages overlapping with each other.

## [2.8.0] - 2024-09-01

## Added
- Bosses can now be tracked by the Multiworld connector.
- The first log entry in the game, now displays a history of the collected items.
- An option to enforce Screw Attack for exiting Breeding Grounds 2 Overgrown Alley.
- An option for custom suit damage reduction.
- A very basic Entrance rando.
- Option to change the Hue of Tilesets and Backgrounds.
- Options to change the gameplay vertically and horizontally.
- The seed hash will be displayed on the clear screen as well.
- The following offworld sprites: Charge Beam, Energy Tank and Missile Expansion for Prime 1, Missile Launcher for Samus Returns.

## Changed
- When "Skip Gameplay cutscenes" is on, the Tower activation cutscene will also be skipped.
- The Septogg in Breeding Grounds 3 South Gamma will now spawn instantly after defeating the Gamma.
- The offworld sprite for Prime 1 Bombs has been to better fit within the existing styles.

## Fixed
- Python wrapper: Using Music Rando now doesn't leave side effects anymore.
- The baby now requires all DNA in order to be collected again.
- When using Door Lock rando, doors in boss rooms will not close instantly when stepping into the room, but instead stay open until the proper event starts.
- Fixed doors in boss rooms locking themselves when reentering the boss room.
- In Door Lock Rando, when doors unlock in labs rooms, they will now properly switch to ammo doors rather than switching to blue doors.
- The softlock prevention in Super Missile Chamber now doesn't have a shot block overlapping with a crumble block.
- In Door Lock Rando, if another Water Turbine has been destroyed, it will now not cause the vanilla Water Turbine to get destroyed.

## [2.7.6] - 2024-07-05

### Fixed
- Tower rooms now have their correct lighting status.

## [2.7.5] - 2024-07-01

### Fixed
- Crash when a Research Site Hatch gets shuffled over a Research Site.

## [2.7.4] - 2024-06-30

### Fixed
- Fixed Thoth door not being flipped.

## [2.7.3] - 2024-06-29

### Changed
- Updated UTMT depencency. Should result in no noticeable changes.

## [2.7.2] - 2024-06-29

### Fixed
- The game doesn't crash anymore when saving at a save station.

## [2.7.1] - 2024-06-28

### Fixed
- Custom liquid info now properly applies for rooms that originally called `make_deep_water()`.

## [2.7.0] - 2024-06-25

### Added
- Following offworld sprites for Metroid Samus Returns: Bombs, Grapple, Aeion Tank and Morph Ball
- A way to use the game's default liquid value for a room rather than using YAMS' default values.

### Fixed
- Doors that are put over open transitions will now properly show again if they're on the right side of a room.
- Edge case where when specifying custom a custom liquid state, it will use the previous room's state
- Suits not being enabled by default when set as a starting item.
- Room HUD and Multiworld room change not triggering when switching rooms.
- `wave_speed` and `wave_height` being int's instead of double's.

## [2.6.2] - 2024-06-22

### Fixed
- Specifying custom water info now properly works.

## [2.6.1] - 2024-06-21

### Fixed
- Corrected the internal room ID for Hideout Alpha Nest.
- Made Multiworld work again.

## [2.6.0] - 2024-06-14

### Added
- Add the ability for custom darkness levels in rooms.
- Add the ability for custom liquid levels in rooms.

### Changed
- Internal refactor to make future code editing easier.

### Fixed
- Fixed credits for Infinite Wall Jump Boots and Infinite Bomb Propulsion.


## [2.5.0] - 2024-05-27

### Changed
- The sprites for Wall Jump Boots have been changed to stand out less
- The sprite for the Bullet Hell Room Access EMP Door has been changed to be more distinct.

## [2.4.0] - 2024-05-24

### Added
- All Bosses now drop Power Bombs on death.

## [2.3.0] - 2024-05-24

### Added
- Added ammo drops to Queen

### Changed
- The Walljump item has been renamed to Walljump Boots.
- The Infinite Bomb Jump item has been renamed to Infinite Bomb Propulsion.
- The sprites for the Infinite Bomb Jump item have been changed to stand out less.

### Fixed
- Long Beam now doesn't cause an error message when received in a Multiworld.
- The Open Transition in Waterfalls Entryway now looks less odd when it's shuffled as a door.

## [2.2.0] - 2024-05-19

### Added
- Added Long Beam as an Item
- Added Infinite Bomb Jumping as an Item
- Added Walljumping as an Item
- Make it possible to use colored text for logbooks.

### Changed
- Doors that have been shuffled over open transitions look slightly nicer.
- The sprites for the EMP doors changed to be more distinct.

### Fixed
- Hideout Alpha Nest, Waterfalls entryway and Skreek Street look less weird if a door is shuffled over an open transition.
- Fix music in pipe rooms.

## [2.1.2] - 2024-05-13

### Fixed
- Pyinstaller not finding the hook correctly. For real this time.
- License file not being preset in the wheel.

## [2.1.1] - 2024-05-13

### Fixed
- Pyinstaller not finding the hook correctly.

## [2.1.0] - 2024-05-12

### Added

- There is now an option to have a different amount of DNA to unlock the lab.
- The following sprites have been added: Spider Ball for Prime 1, Missile Launcher, Speed Booster Upgrades and Super Missile Launcher for Dread, Baby Metroid, Beam Burst, Lightning Armor, Phase Drift, Scan Pulse and Spider Ball for Metroid Samus Returns, and Grapple Beam for Super Metroid.

### Changed

- Removed Background Pillars in Water Turbine Station, to avoid issues when placing doors over these open transitions.
- When Doors are shuffled over Research Site Hatches, they are now not obscured by the rock background.
- The following sprites were changed in order to fit more with AM2R's art style: Ice Missiles and Storm Missiles for Dread, Annihilator Beam, Dark Suit, Dark Visor, Echo Visor, Light Suit and Progressive Suit for Echoes, Gravity Suit, Phazon Suit and Varia Suit for Prime 1.

### Fixed

- Several things when placing doors on open transitions: they're now more in the background and stick out less.
- When Research Site Hatches are shuffled to Ammo doors (Missile, Super Missile, Power Bomb), they will now get unlocked automatically when going through them.
- Pipe rooms keeping their previous water or darkness level in pipe shuffle.

## [2.0.0] - 2024-04-09

### Changed
- The JSON input now uses (Super) Missile/Power Bomb *Tanks* instead of Expansions.

## [1.2.15] - 2024-03-13

### Fixed
- Hitting Zetas with Charge Beam works again.

## [1.2.14] - 2024-02-24

### Added
- When hitting a Metroid with Charge Beams, but the other items not disabled, a message will appear
- The following sprites were added for Dread: Energy Tanks, Missile Tanks, Missile Tank+, Power Bomb Launcher, Power Bomb Tank, Varia Suit

### Changed
- The following sprites were changed in order to fit more with AM2R's art style: Dread's Energy Part, Dread's Wide Beam, Echoes' Amber Translator, Echoes' Cobalt Translator, Echoes' Dark Agon Key, Echoes' Darkburst, Echoes' Dark Torvus Key, Echoes' Emerald Translator, Echoes' Ing Hive Key, Echoes' Sky Temple Key, Echoes' Super Missiles, Echoes' Violet Translator

### Fixed
- Rare crash when receiving a flashlight/blindfold in a Multiworld session.

## [1.2.13] - 2024-02-07

### Fixed
- When receiving a suit in a Multiworld, the suit transition cutscene will not place you at a room's (0,0) coordinate anymore.

## [1.2.12] - 2024-02-03

### Fixed
- When receiving a suit in a Multiworld, the suit transition cutscene will now not play in invalid rooms anymore.

## [1.2.11] - 2024-01-30

### Fixed
- Ammo will now show correctly in external autotrackers after collecting a Launcher.

## [1.2.10] - 2024-01-28

### Fixed
- The yams.json file is now properly deleted for race seeds

## [1.2.9] - 2024-01-28

### Changed
- Changed sprites for Flashlight, Blindfold, Light Beam, Sonic Boom, Prime 1 Super Missiles, Flamethrower, Ice Spreader and Wave Buster
- The yams.json file is now deleted for race seeds.

## [1.2.8] - 2024-01-27

### Fixed
- Launching the `start-rando.sh` shell script now works more reliably.

## [1.2.7] - 2024-01-26

### Fixed
- Patcher crashing when patching.

## [1.2.6] - 2024-01-26

### Added
- Added sprites for Progressive Jump, Progressive Suit and a Generic CaveStory sprite.

### Changed
- Changed how the Dread Wide Beam sprite looks

## [1.2.5] - 2024-01-23

### Fixed
- Getting progressive items in Multiworlds should hopefully now properly work for external item trackers.

## [1.2.4] - 2024-01-22

### Fixed
- Made the new Dread and Prime 1 sprites actually work.

## [1.2.3] - 2024-01-21

### Added
- New Dread sprites: Diffusion beam, Flash Shift, Pulse Radar, Phantom Cloak and Space Jump.
- New Prime 1 sprites: Seperate sprites for all individual artifacts.

### Fixed
- ELM wil now work again.
- Getting progressive items in Multiworlds will now properly work for external item trackers.

### Removed
- The game will not log Multiworld occurences to a debug log anymore.

## [1.2.2] - 2024-01-17

### Fixed
- Fix Echoes Power Bomb Launcher sprites not being actually added.

## [1.2.1] - 2024-01-17
### Changed
- Rename `sItemDarkBeam` to `sItemDarkBeamEchoes` and `sItemArtifact` to `sItemArtifactPrime`.

### Fixed
- Ammo and Launchers will not give a bunch ammo to Multiworld trackers.
- Progressives should now always display correctly on Multiworld trackers.

## [1.2.0] - 2024-01-14
### Added
- The Baby now checks for all DNA being collected and will display a message if not.
- Multiworld support has been added. This includes sprites for it.

## [1.1.4] - 2024-01-05
### Fixed
- Entering "Hatchling Room Underside" will now show the Metroid scan notification only once.

## [1.1.3] - 2024-01-02
### Fixed
- Item fanfare and starting fanfare overlapping when starting with random items.
- Don't loop the spinjump sound effect, when spinjumping into a progressive Space Jump.

## [1.1.2] - 2023-12-31
### Fixed
- Unexpected behaviour when changing a left facing Research Site hatch to a Research Site hatch.

## [1.1.1] - 2023-12-30
### Fixed
- Crashes when shuffling Research Site hatches, and weird behaviour when its being shuffled to.

## [1.1.0] - 2023-12-27
### Added
- An item collection screen will now be shown, when the user starts with random items.
- When softlock prevention is active, then the first two crumble blocks in Super Missile Chamber will be shoot blocks instead.
- Clearer GUI symbols, when expansions have been collected, but not their corresponding launcher.
- Feature, that allows you to place Doors on transitions where there are none.
- Research Site Hatches as shuffleable doors for Door Lock Rando.

### Changed
- When softlock prevention is active, then in the EMP Escape route room, instead of the bottom row of speedbooster blocks being gone, now every pillar but the leftmost one is gone.

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
