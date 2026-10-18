# Adding custom lightsaber animations

The mod can replace any of the player's vanilla animation clips with your own while a lightsaber is held, separately for each form. This is optional. With no animation bundle installed, the borrowed vanilla movesets are used.

## How it works

1. The mod looks for two files next to `LightsaberMod.dll`:
   * `lightsaber_animations`: a Unity AssetBundle containing your `AnimationClip`s.
   * `lightsaber_animations.txt`: rules mapping vanilla clip names to your clips, per form. See `lightsaber_animations.example.txt`.
2. When any player (local or remote) draws a lightsaber, their animator is switched to an `AnimatorOverrideController` built from those rules for their current form, and switched back on sheathing.
3. Each player's form is published over the network, so everyone sees the same animations.

The game still fires its usual attack triggers. Only the clips they play change, so damage, chains and stamina all keep working, provided your clips carry the right animation events (below).

## Pipeline

**The quick route:** use the [Lightsaber Animation Kit](../unity/LightsaberAnimKit/README.md). Its editor menu automates steps 6 to 9 below: renaming Mixamo clips, copying the events, validating and building the bundle. It also has a per-form Mixamo shopping list. The manual steps are kept here for reference.

1. **Match Unity versions.** Check the Unity version in `Valheim/valheim_Data/../Player.log` (first lines) and install exactly that editor version. Bundles from other versions usually fail to load.
2. **Get references.** Run AssetRipper on your own Valheim install to export the player model and the vanilla attack clips you'll replace. Use them only as timing and skeleton references, and don't redistribute them.
3. **Find clip names.** In game, open the F5 console (launch with `-console`) and run `lightsaber_clips` or `lightsaber_clips sword` to list the clip names you can replace.
4. **Animate.** Blender with a humanoid rig, Mixamo sword animations (retargeted) or recorded mocap all work. Export FBX.
5. **Import as Humanoid.** In Unity set *Rig → Animation Type* to **Humanoid**, so clips retarget onto Valheim's skeleton.
6. **Copy the animation events.** This step is essential. Valheim decides when a swing hits, when the next chain step can start and when the attack ends from **events on the clip**, not from code. Open the vanilla clip you're replacing in the ripped project, note every event's function name and time, and add the same events at the matching moments of your clip. A clip without them will play but never deal damage, or will lock the player in the attack.
7. **Keep durations close** to the vanilla clip, or tune event timings to fit. Attack speed bonuses (Ataru, Juyo) still apply on top.
8. **Build the bundle.** Give the clips an AssetBundle name of `lightsaber_animations` and build for Windows (StandaloneWindows64). Copy the output file (no extension) next to the DLL.
9. **Write the rules** in `lightsaber_animations.txt` and restart the game. The BepInEx log reports how many clips and rules loaded, and warns about unknown forms or missing clips line by line.

## Suggested clip list per form

| Form | Primary (2–3 clip combo) | Secondary | Lore reference |
|---|---|---|---|
| Shii-Cho | Wide horizontal sweeps | Spinning disarm | Sweeping, unrefined, multi-opponent |
| Makashi | One-handed lunges and cuts with an extended arm | Riposte | Count Dooku: fencing, precise, economical |
| Soresu | Tight, short cuts close to the body | Defensive shove | Obi-Wan: tight guard, minimal openings |
| Ataru | Spinning, jumping strikes | Somersault strike | Yoda and Qui-Gon: acrobatic, aggressive |
| Shien / Djem So | Heavy two-handed overhead blows | Falling Avalanche downward chop | Anakin and Vader: power, counterattack |
| Niman | Balanced cuts | Strike with an off-hand Force gesture | Diplomatic, Force-integrated |
| Juyo / Vaapad | Fast, erratic multi-direction cuts | Explosive whirlwind | Mace Windu and Maul: ferocious, unpredictable |

Start with one form (Makashi has the fewest moving parts) to prove the pipeline end to end before animating the rest.

## Known caveats

* Swapping a player's animator controller resets its state, so the mod only does it on drawing, sheathing or changing form. Expect a single-frame pose snap at those moments.
* Clips are replaced in the player's whole controller while a saber is out, including idle and movement clips if you map them. Map only what you mean to change.
