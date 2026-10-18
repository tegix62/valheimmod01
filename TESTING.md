# In-game test checklist

Nothing here has been run in Valheim yet. These are the checks most likely to turn up problems, in the order worth doing them. Testing alone in a local world with `devcommands` is fastest. Spawn items by prefab name, e.g. `spawn Lightsaber_saber_blue`, `spawn Lightsaber_staff_red`, `spawn Lightsaber_shoto_green`, `spawn KyberCrystal_white`, `spawn LightsaberEmitterHilt`.

Keep `BepInEx/LogOutput.log` open. The mod logs a warning for anything it couldn't find or hook.

## 1. Loading
- [ ] The log shows `LightsaberMod 0.1.0 loaded` and no exceptions mentioning `LightsaberMod`.
- [ ] No `Compat: no usable ...` warnings. If one appears, it names the game method whose signature changed.
- [ ] No `No vanilla weapon found for ...` warnings (a moveset donor name is wrong).

## 2. Items
- [ ] The log shows `Lightsaber power matched to SwordMistwalker: ...`. If it fell back to SwordBlackmetal or the 110 default, the Mistwalker prefab name differs in your game version: set `PowerReference` to the right name.
- [ ] The crystals show up at the Forge. The Emitter Hilt and all 21 sabers show up at the Black Forge (level 2 for sabers).
- [ ] A quality 1 lightsaber's tooltip damage roughly matches Mistwalker's.
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
- [ ] Shien: same, plus the archer takes damage ("Barrier of Blades!"). Any block shows "Fluid Riposte".
- [ ] Makashi: only a block timed just as the hit lands shows "Contentious Opportunity"; arrows hurt more than in other forms.
- [ ] Ataru: the attack speed boost disappears under a roof or in a dungeon and returns outside.
- [ ] Juyo: repeated hits ramp up damage, and getting hit resets it. Blocking Eikthyr's lightning shows "Lightning turned back!".
- [ ] Holding R in any form except Juyo shows the dark-side message once; in Juyo it channels.
- [ ] Jar'Kai: in Niman with a lightsaber in hand, no shield and a shoto in the pack, the shoto ignites in the left hand ("Jar'Kai: second blade drawn"). Check that it points the right way (adjust `JarKaiGripRotation` if not), that attacks switch to the dual-knife moves, and that equipping a shield or switching form retracts it.
- [ ] Jar'Kai: a second player sees the off-hand blade.
- [ ] A second player sees your stance, flips and spins.
- [ ] Ataru and Juyo attack visibly faster, and walking animations return to normal speed afterwards.

## 5. The Force
- [ ] G pushes, Shift+G pulls, with the stamina cost and cooldown message.
- [ ] Holding R channels lightning arcs that chain between enemies and drain stamina. Releasing stops the sound and arcs.
- [ ] Jumping twice performs a Force Leap. Landing near enemies triggers the Hawk-Bat Dive, and a high leap causes no fall damage.
- [ ] T throws the saber. It spins out, hits enemies, returns to the hand and can't be swung while in flight. Swapping weapons mid-throw doesn't leave the hand empty.

## 5b. Stances and flourishes
- [ ] Drawing a saber and standing still shows a different pose per form. Note any that look broken (arms through the body, bent backwards) and fix them with `lightsaber_pose <form> <Bone> x y z`, then `lightsaber_pose save`.
- [ ] Poses blend away while attacking and blocking, then return.
- [ ] Arms never keep twisting further over time. If they do, set `StanceIntensity = 0` and report it.
- [ ] Ataru's Force Leap somersaults; Hawk-Bat Swoop and Vaapad Whirlwind spin, and the character lands facing the same way.

## 6. Multiplayer (if possible)
- [ ] A second player sees your saber glow and hears its hum.
- [ ] Form damage bonuses, deflection and Force powers work on a dedicated server.
- [ ] The other player sees your thrown saber fly out and back, and your Force lightning arcs (with their crackle).
- [ ] The log has no errors mentioning `LightsaberMod_SaberThrow` or `LightsaberMod_Lightning`.

## 7. Custom animations (only once you have a bundle)
- [ ] `lightsaber_clips` prints clip names in the F5 console.
- [ ] With the bundle and mapping installed, the log reports the clip and rule counts with no per-line warnings.
- [ ] Drawing a saber plays your clips for the current form, switching forms changes them, and sheathing restores vanilla.
- [ ] Replaced attacks still deal damage (if not, the clip is missing animation events).
