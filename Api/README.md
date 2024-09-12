# XL Scrap API

This mod is an API to allow modders to add items designed to be lifted by multiple players at once.

### Compatibility

Works with v64 of Lethal Company, and is required by all clients.

### How does it work?

It works by instantiating "holder" items to the anchor points assigned in the "main" XL item's properties.
Only these holders can be interacted with directly by the player; the main item's position and rotation depends upon their positions.

### What exactly does the API handle?

- Shifting position of the main item relative to the holders
- Rotating the main item to relatively match the position of the holders
- Custom spawn logic to ensure they are spawned correctly in valid positions
- Saving and loading logic to ensure their state is maintained between sessions
- Networking to ensure the items stay synced between players

### What are the next steps for the API?

- Foundations for more than 2 holders have been put in place, it should only need movement restriction based on the position of other anchors
- Pushing and pulling of XL items would facilitate solo gameplay and make interactions more dynamic
- Pseudo-collisions with walls and floors would increase immersion and make for interesting situations like hanging off a cliff possible
- Though it is equaly powerful by defaulting to the first anchor for the XL item's rotation reference, freely setting another point would be convenient

### How can I use the API to add XL items?

To use this API's added functionality when making an item in Unity, simply make use of the `XL Main Item` script in your prefab
in place of the usual `Physics Prop` or `Grabbable Item` scripts.
All fields in this script are to be set as usual, with the addition of the `Anchors` property which dictates where the holders will spawn.

Note that the rotation of your object will always stay constant relative to your first anchor point;
specifically, the item's `transform.forward` will always be set to point at the first holder in the scene.
This means you may need to choose your first holder point carefully, or will need to adjust the model's rotation Blender (or a similar tool).

To see a tutorial and example of XL Scrap being added by this mod, check out the `XL Scrap`.

### Credits

- XL Holder Model:
    > "Project Playtime | Glove Hand" (https://skfb.ly/oCsIJ) by Xoffly is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).
