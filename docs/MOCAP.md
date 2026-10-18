# Motion capture from your phone (no Unity, no PC)

The mod can play **BVH** motion-capture files directly. Record yourself performing a technique, export BVH, drop it in the mod's `mocap` folder, and map it to a form's attack. The mod retargets the motion onto your Viking at runtime using Unity's built-in humanoid retargeting.

> **Experimental, and untested in game.** The BVH parser is tested. Retargeting can only be checked in Valheim. If something looks wrong, the BepInEx log and a description of what you see are what I need.

## What it does

* Your clip drives the **upper body** (spine, arms, head) while Valheim's legs and footwork carry on underneath. Add `body=full` to a line to drive the legs too, which suits flips and spins.
* **Hit timing is unchanged.** The vanilla attack still plays invisibly underneath and decides when damage lands. Pick or trim clips so the strike happens at about the same moment (see `start=`, `end=` and `speed=` below).
* **Idle clips** loop while you stand ready with a saber drawn, replacing the code-driven stance.
* **Multiplayer:** other players see your clips if they have the same `mocap` folder.

## Recording

Any app or source that exports **BVH** with a **T-pose rest pose** should work. Examples to check (features change, so confirm they export BVH):

* **Phone video to mocap:** Rokoko Video, Plask (works in a browser), Move.ai's apps.
* **Free libraries:** the CMU motion-capture database (BVH conversions) for general movement.

Tips for recording:
* **Hold something saber-length** (a broom handle works) so your grip and arm extension look right.
* **One technique per clip, starting and ending in your ready stance.** Trim dead time with `start=` and `end=`.
* **Keep attacks short:** about 0.6 to 1.2 s, close to Valheim's own swings.
* **Film side-on to the camera** with your whole body in frame, and good light.

## Installing clips

1. Create a `mocap` folder next to `LightsaberMod.dll` (e.g. `BepInEx/plugins/LightsaberMod/mocap/`).
2. Copy your `.bvh` files in. Use file names without spaces.
3. Create `mocap/lightsaber_mocap.txt` (start from `docs/lightsaber_mocap.example.txt`):

```
# key.slot = file.bvh [, more.bvh ...] [speed=1.0] [start=0.0] [end=<clip length>] [body=full]
makashi.primary   = makashi_lunge_1.bvh, makashi_lunge_2.bvh, makashi_lunge_3.bvh
makashi.secondary = makashi_riposte.bvh start=0.2 end=1.1
soresu.idle       = soresu_guard_loop.bvh
ataru.secondary   = hawkbat_swoop.bvh body=full speed=1.2
jarkai.primary    = jarkai_flurry.bvh
*.secondary       = generic_spin.bvh             # fallback for forms without their own
```

* **Keys:** `shiicho`, `makashi`, `soresu`, `ataru`, `shien`, `niman`, `juyo`, `jarkai` (Niman while dual wielding), or `*` as a fallback for every form.
* **Slots:**
  * `primary`: several files = one per chain step, cycling.
  * `secondary`.
  * `idle`: looped.
* **Options:**
  * `speed`: playback rate.
  * `start` and `end`: seconds within the clip.
  * `body=full`: drive the legs too.

4. In the F5 console run `lightsaber_mocap` to reload without restarting. The log lists any problem per line: a missing file, or joints it couldn't identify.

## Joint names

The mod recognises the joint names used by Mixamo, Rokoko, CMU, 3ds Max Biped and most phone-mocap exports (`Hips`, `Spine`, `LeftArm`, `LeftForeArm`, `LeftUpLeg`, ...). If a file is rejected, the log shows which body parts it couldn't find and lists the file's joint names. Send me that line and I'll add the aliases.

## Turning it off

Set `UseMotionCapture = false` in the config, or remove `lightsaber_mocap.txt`.
