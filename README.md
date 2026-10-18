# LightsaberMod for Valheim

Craft lightsabers from kyber crystals and fight in all seven classic lightsaber forms. A BepInEx plugin built on [Jötunn](https://valheim-modding.github.io/Jotunn/).

## What's in it

### Crafting chain (all at the Forge)

| Item | Recipe | Forge level |
|---|---|---|
| Emitter Hilt | 4 Iron, 2 Silver, 2 Leather scraps, 1 Surtling core | 2 |
| Blue Kyber Crystal | Crystal + 5 Blueberries | 1 |
| Green Kyber Crystal | Crystal + 2 Guck | 1 |
| Yellow Kyber Crystal | Crystal + 5 Dandelion | 1 |
| Purple Kyber Crystal | Crystal + 5 Thistle | 1 |
| Red Kyber Crystal | Crystal + 3 Bloodbag + 1 Surtling core | 1 |
| White Kyber Crystal | Red Kyber Crystal + 3 Ectoplasm (a purified red crystal) | 1 |
| Dark Kyber Crystal | Crystal + 5 Obsidian + 2 Surtling core | 1 |

Combine hilts and a crystal at a level 3 Forge. Every chassis comes in all seven colours (21 weapons). Upgrades cost 4 Silver per level, up to quality 4.

| Chassis | Needs | Moveset / skill | Base damage |
|---|---|---|---|
| Lightsaber | 1 hilt, 1 crystal | Iron sword / Swords | 55 slash + 25 fire |
| Saberstaff (double-bladed) | 2 hilts, 2 crystals | Iron atgeir / Polearms | 50 slash + 22 fire |
| Shoto | 1 hilt, 1 crystal | Silver knife / Knives | 34 slash + 16 fire |

Lightsabers glow (each blade carries its own coloured light), don't wear out by default, and parry better than vanilla blades. Models are built in code from the hilt and blade shapes, so the mod needs no asset bundle. Inventory icons are rendered from those models.

### Feel and sound

* **Ignition:** drawing a saber grows the blade out of the emitter. Sheathing it plays the retract sound.
* **Hum:** a low beating hum that rises in pitch and volume as the blade swings.
* **Trails:** fast swings leave a glowing band in the blade's colour.
* **Clash:** blocking with a saber crackles.

All sounds are synthesised in code at startup and play through Valheim's SFX volume. Other players' sabers hum and glow for you too.

### The seven forms

Press **H** to cycle forms (**Shift+H** goes back). The active form shows on the status bar and only works while a lightsaber is in hand. Form *N* unlocks at weapon skill `(N-1) × 5` with the held saber's skill (configurable, or set to 0 to unlock all).

| Form | Style | Effects |
|---|---|---|
| I. Shii-Cho | Way of the Sarlacc | +8% damage per extra enemy within 8 m (max +40%), -10% against a lone enemy, +20% stagger (sun djem: disarming), -10% attack stamina |
| II. Makashi | Way of the Ysalamiri | +35% damage against a lone enemy, -15% when 3+ enemies are near, -20% attack stamina. **Contentious Opportunity:** a *perfect parry* makes your next hit +40%. Weak to ranged attacks: +25% damage taken from them |
| III. Soresu | Way of the Mynock | Blocking deflects **all** ranged attacks, -25% damage taken, +8 stamina per block, +25% stamina regen, -20% damage dealt |
| IV. Ataru | Way of the Hawk-Bat | +20% attack speed, +15% damage, +15% move speed, higher jumps, stronger Force Leap. **Needs room:** no attack speed or damage bonus indoors, in dungeons or under a roof. +25% attack stamina, +10% damage taken |
| V. Shien / Djem So | Way of the Krayt Dragon | **Barrier of Blades:** blocking sends 75% of a ranged attack back at the shooter. **Fluid Riposte:** a block makes your next hit within 2.5 s +50%. +15% damage, +50% stagger, saber throw +50%, -10% damage taken |
| VI. Niman | Way of the Rancor | Force powers 75% stronger and 40% cheaper, +5% damage, -5% damage taken, +10% stamina regen. No standout strength by design. **Jar'Kai** (below) |
| VII. Juyo / Vaapad | Way of the Vornskr | +25% damage, +15% attack speed, +25% stagger. **Momentum:** each consecutive hit adds 5% (max +30%), and being hit breaks it. **Vaapad:** blocking turns lightning back on its source. The only form that can call Force lightning. +25% damage taken, -15% stamina regen |

#### Jar'Kai (Niman)

Niman is the foundation of Jar'Kai, two-blade fighting. Valheim's off hand only holds shields and torches, so the second saber isn't equipped. While you're in Niman, holding a one-handed lightsaber or shoto with an **empty off hand**, and carrying **another lightsaber or shoto** in your inventory, that second blade ignites in your left hand automatically. A shoto is preferred, as in the classic saber-and-shoto pairing.

* Each hit adds 40% of the off-hand blade's damage (its follow-through cut).
* Blocks are 30% stronger. Attacks cost 15% more stamina.
* The moveset switches to the dual-knife animations, the only vanilla moves that swing both arms: **Jar'Kai Flurry** and **Twin-Blade Lunge**.
* Other players see the second blade too. Equipping a shield, leaving Niman or sheathing puts it away.

If the off-hand blade sits at a wrong angle, adjust `JarKaiGripRotation` in the config. Set `JarKai = false` to disable it.

Each form's traits follow the lore; see [docs/FORMS.md](docs/FORMS.md) for the reasoning and sources behind every number.

### Form movesets

Each form also changes how the saber swings. Valheim can only play animations the player rig already has, so every technique borrows the attack of the vanilla weapon whose motion best fits the lore, then gets its own damage, reach, arc, stamina and stagger. The secondary attack (middle mouse by default) announces the technique's name.

| Form | Primary combo | Secondary technique |
|---|---|---|
| I. Shii-Cho | **Disarming Slash**: wide battleaxe arcs at the weapon hand, +25% arc, +30% stagger | **Sarlacc Sweep**: atgeir spin that hits everything around you |
| II. Makashi | **Fencer's Thrust**: spear lunges, +15% reach, narrow line, +15% damage, -20% stamina | **Makashi Riposte**: the sword's committed counter-cut, +30% damage and stagger |
| III. Soresu | **Tight Guard**: short, compact sword cuts, -30% stamina | **Circle of Shelter**: low-damage atgeir spin with 2.5× knockback that clears a ring around you |
| IV. Ataru | **Hawk-Bat Flurry**: fast knife chain | **Hawk-Bat Swoop**: the knife's springing strike, +35% damage |
| V. Shien / Djem So | **Krayt Strikes**: heavy overhand mace blows, +35% stagger | **Falling Avalanche**: a powerful overhand sledge blow from above, +40% damage |
| VI. Niman | **Rancor Balance**: standard sword chain (Jar'Kai: **Jar'Kai Flurry**, dual-knife chain) | **Force-Guided Strike**: sword secondary that also fires a Force Push (Jar'Kai: **Twin-Blade Lunge**, dual-knife leap) |
| VII. Juyo / Vaapad | **Vornskr Frenzy**: each new combo draws a random style (sword, knife, axe, mace or greatsword), never the same twice in a row | **Vaapad Whirlwind**: Krom greatsword's secondary, +50% damage, +40% stamina |

If a donor weapon is missing, the next one in its list is used, and failing that the saber keeps its own attack (the BepInEx log says which). Set `UseFormMovesets = false` to turn this off.

### Custom animations (optional)

You can drop an animation bundle and a mapping file next to the DLL to replace the borrowed vanilla swings with your own clips, per form. Everyone in the session sees them. See [docs/ANIMATIONS.md](docs/ANIMATIONS.md). The `lightsaber_clips` console command lists the vanilla clip names to map.

### The Force

These only work with a lightsaber drawn. Push and Pull share a 4 s cooldown and cost 25 stamina before form modifiers. Niman makes every Force power stronger and cheaper.

* **G**: Force Push. A 70° cone out to 9 m that knocks back and staggers enemies.
* **Shift+G**: Force Pull. Drags enemies in that cone toward you.
* **T**: Saber Throw. The blade spins out along your aim (14 m, 20 stamina), cuts every enemy it passes on the way out *and* on the way back, then returns to your hand. It turns back early if it hits a wall. Shien / Djem So throws hit 50% harder. You can't swing while it's in flight.
* **Hold R**: Force Lightning. Arcs from your off hand into the nearest enemy in front (10 m), then chains to up to two more within 5 m, each jump weaker than the last. It deals lightning damage five times a second and drains 15 stamina per second. It's a dark-side power, so by default only Form VII (Juyo) can call it (`LightningDarkSideOnly`). When there's nothing to hit, the arcs crackle into the air.
* **Jump again in mid-air**: Force Leap. Launches you forward and up (15 stamina). Landing from a leap triggers a **Leaping Strike**, a ground slam that damages and knocks back every enemy within 3.5 m. Leaps never cause fall damage. Ataru leaps 40% further, slams 40% harder and pays half the stamina.

All keys, costs and balance values are in `BepInEx/config/tegix62.LightsaberMod.cfg`. Form and Force settings sync from the server's admin config.

## Installing

1. Install [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) and [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/), or use r2modman.
2. Put `LightsaberMod.dll` in `BepInEx/plugins/LightsaberMod/`.
3. Every player and the server need the mod (enforced by Jötunn's version check).

## Building

Requirements: .NET SDK 6+ (or Visual Studio 2022) and a Valheim install with BepInEx.

```sh
cp Environment.props.example Environment.props   # set VALHEIM_INSTALL
cp DoPrebuild.props.example DoPrebuild.props     # first build publicizes the game assemblies
dotnet build -c Release
```

Output: `src/LightsaberMod/bin/Release/LightsaberMod.dll`. If `MOD_DEPLOYPATH` is set, the build copies the DLL into `BepInEx/plugins/LightsaberMod` for you.

To package for Thunderstore, zip `Thunderstore/manifest.json`, `Thunderstore/icon.png`, this `README.md` and the DLL together.

## How it works

| File | Role |
|---|---|
| `Items/SaberFactory.cs` | Clones the vanilla weapons, rewrites their stats, hides their meshes and builds a hilt, blades and lights in their place |
| `Forms/SaberForm.cs` | Data for the seven forms |
| `Forms/FormManager.cs` | Current form, unlocks, keeping the status effect in sync with the held weapon |
| `Forms/FormMoveset.cs` | Data for each form's techniques and the vanilla attacks they borrow |
| `Forms/MovesetManager.cs` | Builds those attacks and swaps them onto the held saber |
| `Forms/ForcePowers.cs` | Push and Pull |
| `Forms/SaberThrow.cs` | The thrown, returning saber |
| `Forms/ForceLightning.cs` | Channelled, chaining lightning and its arcs |
| `Forms/ForceLeap.cs` | Mid-air leap and landing slam |
| `Forms/JarKai.cs` | Niman's two-blade fighting and the off-hand saber model |
| `Items/SaberBlade.cs` | Per-model ignition, hum, swing trails and retract |
| `Items/SaberAudio.cs` | Procedurally generated sounds |
| `Patches/CombatPatches.cs` | Harmony hooks: outgoing damage (`Character.Damage`), incoming damage and deflection (`Character.RPC_Damage`), blocks (`Humanoid.BlockAttack`), attack stamina (`Attack.GetAttackStamina`), attack speed (`CharacterAnimEvent`) |
| `Net/NetSync.cs` | Broadcasts saber throws and Force lightning so other players see them, and publishes each player's form |
| `Animation/FormAnimations.cs` | Loads the optional animation bundle and swaps each saber-wielder's animator per form |
| `Animation/ListClipsCommand.cs` | `lightsaber_clips` console command |
| `Util/Compat.cs` | Calls game methods by reflection where their signatures differ between Valheim versions |

## Status and known limitations

* **Not yet tested in-game.** The project compiles against Jötunn 2.30.2, but the game's own assemblies were stubbed for that build. Report anything that misbehaves along with `BepInEx/LogOutput.log`.
* Blades are simple glowing cylinders, and the synthesised sounds are approximations of the films' sound design.
* Redirected bolts damage the shooter directly; they don't fly back as projectiles.
* Movesets play other weapons' animations while you hold a saber. If one of them doesn't trigger while a one-handed weapon is equipped, that technique will look wrong; the log names which vanilla attack each move uses.
* Jar'Kai's second saber is drawn from the inventory rather than equipped, so it can't be selected separately or swapped mid-fight.
* `DamageScale` and `UseDurability` are applied when the item prefabs are built at startup, so keep them identical for everyone.
