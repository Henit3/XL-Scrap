# 0.2.0
- Fixed the masks used to check if the exit is obstructed so Vow fire exit (and similar) work as intended
- Reworked teleport logic so it will not accidentally teleport you into the void on using an entrance/exit, instead stop and warn you
- Fixed a bug where XL Items can spawn in gift boxes
- Added CullFactory compatibility so XL Items won't be invisible after using an entrance/exit

# 0.1.1
- Fixed transpile of spawn logic so it shouldn't have inaccessible spawns as intended
- Fixed teleport logic with certain fire exits so it should not teleport you out of bounds much less
- Added (and defaulted) config to turn off collision of XL Items to prevent clipping with conflicting collisions

# 0.1.0
Initial release:
- Introduces XlMainItem and XlHolderItem superclasses of GrabbableObject
- Holders can be picked up and dropped to shift position & rotation of associated main item
- Support interactions such as entering/leaving interior, and selling of items
- Custom spawning and saving handling