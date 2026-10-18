# In-game test checklist

Nothing here has been run in Valheim yet. These are the checks most likely to turn up problems, in the order worth doing them. Testing alone in a local world with `devcommands` is fastest (`spawn LightsaberMod_...` won't work; use `spawn Lightsaber_saber_blue`, `spawn KyberCrystal_red`, `spawn LightsaberEmitterHilt`).

Keep `BepInEx/LogOutput.log` open. The mod logs a warning for anything it couldn't find or hook.

## 1. Loading
- [ ] The log shows `LightsaberMod 0.1.0 loaded` and no exceptions mentioning `LightsaberMod`.
- [ ] No `Compat: no usable ...` warnings. If one appears, it names the game method whose signature changed.
- [ ] No `No vanilla weapon found for ...` warnings (a moveset donor name is wrong).

## 2. Items
- [ ] At a level 3 Forge, the recipes for Emitter Hilt, all 7 crystals and all 21 sabers show up.
- [ ] Inventory icons show the saber, not an iron sword. A sword icon means the icon render failed (a debug log line says why).
- [ ] Held and dropped sabers show the hilt and glowing blade, with no leftover vanilla sword mesh.
- [ ] The blade points out of the hand along the hilt. If it's sideways, note which chassis (saber, staff or shoto).

## 3. Feel and sound
- [ ] Drawing a saber grows the blade and plays the ignition sound. Sheathing plays the retract sound.
- [ ] The hum is audible, rises when swinging and follows the game's SFX volume slider.
- [ ] Swings leave coloured trails, not white or pink ones, and running doesn't.
- [ ] Blocking a hit crackles.

## 4. Forms (H / Shift+H)
- [ ] Switching shows the form name, summary and technique names, and the status icon updates.
- [ ] Each form's primary and secondary attack plays a sensible animation. Note any that freeze, T-pose or deal no damage.
- [ ] Juyo combos change style after a pause between swings.
- [ ] Soresu: blocking arrows (e.g. from a Draugr archer) shows "Bolt deflected" and takes no damage.
- [ ] Shien: same, plus the archer takes damage ("Bolt redirected!").
- [ ] Ataru and Juyo attack visibly faster, and walking animations return to normal speed afterwards.

## 5. The Force
- [ ] G pushes, Shift+G pulls, with the stamina cost and cooldown message.
- [ ] Holding R channels lightning arcs that chain between enemies and drain stamina. Releasing stops the sound and arcs.
- [ ] Jumping twice performs a Force Leap. Landing near enemies triggers the Hawk-Bat Dive, and a high leap causes no fall damage.
- [ ] T throws the saber. It spins out, hits enemies, returns to the hand and can't be swung while in flight. Swapping weapons mid-throw doesn't leave the hand empty.

## 6. Multiplayer (if possible)
- [ ] A second player sees your saber glow and hears its hum.
- [ ] Form damage bonuses, deflection and Force powers work on a dedicated server.
