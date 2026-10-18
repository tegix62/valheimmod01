# Lightsaber Animation Kit

Unity editor tools that turn downloaded or recorded animations into the `lightsaber_animations` bundle LightsaberMod loads. They handle the fiddly parts for you:
- renaming Mixamo clips so they don't all share one name,
- copying Valheim's hit-timing events onto your clips,
- checking import settings,
- building the bundle and writing the mapping file.

> These scripts have not been compiled here (the Unity editor isn't available in the environment they were written in). If Unity reports an error on import, the console message names the line.

## One-time setup (≈30 minutes, mostly downloads)

1. **Find Valheim's Unity version.** Open `%USERPROFILE%\AppData\LocalLow\IronGate\Valheim\Player.log` after launching the game once. The Unity version is near the top. Install **exactly that version** through Unity Hub (use the "Archive" link for older versions).
2. **Create a new 3D project** in that version.
3. **Copy this kit in:** drag the `LightsaberAnimKit` folder into the project's `Assets` folder.
4. **Export Valheim's own clips** (needed for hit timing). Run [AssetRipper](https://github.com/AssetRipper/AssetRipper) on your Valheim install, export, and find the player's attack clips (`.anim` files). Copy only the clips you'll replace into the project, e.g. `Assets/Vanilla/`. Don't redistribute them.
   * To see which ones you need, run `lightsaber_clips` in the game's F5 console (launch with `-console`). For example, `lightsaber_clips swing` lists the swing clips. The Valheim clips your form borrows are logged in `BepInEx/LogOutput.log` at startup (`Makashi Riposte: using secondary attack of SwordIron (...)`).

## Each batch of animations (≈1 hour)

1. **Download clips** (see the shopping list below). On Mixamo, choose **FBX for Unity**, **Without Skin**, and tick **In Place** where offered.
2. **Import** them into `Assets/Replacements/`. Select them all and in the Inspector set:
   * **Rig → Animation Type: Humanoid**, then Apply.
   * **Animation tab:** untick *Loop Time* for attacks. Under *Root Transform Rotation* and *Root Transform Position (XZ)*, tick **Bake Into Pose**. Apply.
3. With the FBX files still selected, run **Lightsaber → 1. Name Mixamo Clips After Their Files**. Every Mixamo clip is otherwise called `mixamo.com`.
4. **Create the map:** Assets → Create → Lightsaber → Animation Map. Add one entry per replacement:
   * `form`: `makashi`, `soresu`, ... or `*`.
   * `vanillaClip`: drag in the ripped Valheim clip it replaces.
   * `replacement`: drag in your clip (expand the FBX to find it).
   * Optionally set `modFolder` to the folder containing `LightsaberMod.dll`.
5. **Lightsaber → 2. Validate Map**, then fix anything flagged ERROR.
6. **Lightsaber → 4. Build Bundle + Mapping.** This copies the events, builds `lightsaber_animations` and writes `lightsaber_animations.txt`, then copies both into `modFolder` if you set one.
7. Restart Valheim. The BepInEx log reports how many clips and rules loaded.

If an attack plays but deals no damage, its events didn't come across. Check that the vanilla clip had events (Validate warns if not) and rebuild.

## Shopping list per form

Search Mixamo with these terms and pick what looks right. Exact clip names change, so these are search phrases, not titles. Replace the vanilla clips each form's moveset borrows, listed in the main README's moveset table.

| Form | Look for | Replaces |
|---|---|---|
| Shii-Cho | "sword slash" variants with wide horizontal arcs; "melee 360" for the Sarlacc Sweep | Battleaxe primary chain; atgeir secondary |
| Makashi | "sword lunge", "fencing", "stab", "thrust": one-handed, extended arm | Spear primary; sword secondary |
| Soresu | "sword block", "sword and shield slash" (compact cuts close to the body) | Sword primary chain; atgeir secondary |
| Ataru | "jump attack", "spin attack", "flip" | Knife primary chain; knife secondary |
| Shien / Djem So | "great sword slash", "overhead", "downward slash", "power attack" | Mace primary chain; sledge primary (Falling Avalanche) |
| Niman | "sword combo" (balanced); for Jar'Kai, "dual" or "two weapon" combos | Sword chain; dual-knife chain and secondary |
| Juyo / Vaapad | "great sword combo", "aggressive", "spinning attack" | Its five random styles; greatsword secondary |

**Tips**
* **Chain attacks:** Valheim plays a three-hit chain as three separate clips (often suffixed `0`, `1`, `2`). Map one replacement per step.
* **Clip length:** clips within ~30% of the vanilla length feel right. Events are rescaled to fit, but very long clips make attacks sluggish.
* **Phone mocap:** you can capture your own moves with Move.ai, Rokoko Video or Plask from a phone video, export FBX, and treat it exactly like a Mixamo download.
* **Start small:** one form (Makashi's riposte is a good first test) end to end, then the rest.
