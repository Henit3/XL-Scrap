# 0.1.0
Initial release:
- Introduces XlMainItem and XlHolderItem superclasses of GrabbableObject
- Holders can be picked up and dropped to shift position & rotation of associated main item
- Support interactions such as entering/leaving interior, and selling of items
- Custom spawning and saving handling

# 0.1.1
- Fixed transpile of spawn logic so it shouldn't have inaccessible spawns as intended
- Fixed teleport logic with certain fire exits so it should not teleport you out of bounds much less
- Added (and defaulted) config to turn off collision of XL Items to prevent clipping with conflicting collisions