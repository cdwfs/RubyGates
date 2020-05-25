# RubyGates ChangeLog

## 2020-05-25

### Added
* Add Branch gate type. These gates route their input signal to one of two output paths. Click to change which path is taken.
* Add tutorial level for Branches

### Fixed
* Optimization: Buttons no longer trigger redundant material updates every single frame.
* Fixed potential race conditions in gate propagation logic

## 2020-05-18

### Added
* Added XOR gate type (including a tutorial level, and some cameos in existing levels)

### Fixed
* Adjusted OR gate artwork
* Adjusted NOT attach point location
* Input wires now connect to dual-input gates (AND, OR, XOR) in the correct locations.
* Temporarily switched scripting backend from IL2CPP to Mono to avoid a regression in the Entities package. Will switch back later.

## 2020-04-25

### Added
* New sprite art for all gate types
* One additional level
* Completing a level now gives you the choice of retrying the same level or advancing to the next one.
* Push ESC to pause. From the pause menu, you can resume, restart the level, or quit the game.

### Fixed
* Replaying certain levels after completing them no longer immediately completes them again.
* Fixed crash during level load if a gate had >1 output nodes.

## 2020-04-18

### Added
* First playable