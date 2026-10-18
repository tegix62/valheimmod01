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

### The seven forms

Press **H** to cycle forms (**Shift+H** goes back). The active form shows on the status bar and only works while a lightsaber is in hand. Form *N* unlocks at weapon skill `(N-1) × 5` with the held saber's skill (configurable, or set to 0 to unlock all).

| Form | Style | Effects |
|---|---|---|
| I. Shii-Cho | Way of the Sarlacc | +8% damage per extra enemy within 8 m (max +40%), -10% attack stamina, -5% damage taken |
| II. Makashi | Way of the Ysalamiri | +35% damage against a lone enemy, -15% damage when 3+ enemies are near, +30% stagger, -20% attack stamina |
| III. Soresu | Way of the Mynock | Blocking deflects **all** ranged attacks, -25% damage taken, +8 stamina per block, +25% stamina regen, -20% damage dealt |
| IV. Ataru | Way of the Hawk-Bat | +20% attack speed, +15% damage, +15% move speed, higher jumps, +25% attack stamina, +10% damage taken |
| V. Shien / Djem So | Way of the Krayt Dragon | Blocking sends 75% of a ranged attack back at the shooter. A block arms a riposte: your next hit within 2.5 s does +50%. Also +15% damage, +50% stagger |
| VI. Niman | Way of the Rancor | Force powers are 75% stronger and 40% cheaper, +5% damage, -5% damage taken, +15% stamina regen |
| VII. Juyo / Vaapad | Way of the Vornskr | +40% damage, +15% attack speed, 8% lifesteal (capped per hit), +25% stagger, +25% damage taken, -15% stamina regen |

### Form movesets

Each form also changes how the saber swings. Valheim can only play animations the player rig already has, so every technique borrows the attack of the vanilla weapon whose motion best fits the lore, then gets its own damage, reach, arc, stamina and stagger. The secondary attack (middle mouse by default) announces the technique's name.

| Form | Primary combo | Secondary technique |
|---|---|---|
| I. Shii-Cho | **Sarlacc Sweep**: wide battleaxe arcs, +25% arc | **Disarming Circle**: atgeir spin that hits everything around you, +40% stagger |
| II. Makashi | **Fencer's Thrust**: spear lunges, +15% reach, narrow line, +15% damage, -20% stamina | **Contra-Riposte**: the sword's committed counter-cut, +30% damage and stagger |
| III. Soresu | **Tight Guard**: short, compact sword cuts, -30% stamina | **Circle of Shelter**: mace bash with low damage, 2.2× knockback and heavy stagger |
| IV. Ataru | **Hawk-Bat Flurry**: Skoll-and-Hati dual-knife chain | **Ataru Leap**: the dual knives' leaping strike, +35% damage |
| V. Shien / Djem So | **Krayt Strikes**: heavy overhand mace blows, +35% stagger | **Falling Avalanche**: sledge ground-slam that hits the area around the target, +40% damage |
| VI. Niman | **Rancor Balance**: standard sword chain | **Force-Guided Strike**: sword secondary that also fires a Force Push (normal cost and cooldown) |
| VII. Juyo / Vaapad | **Vornskr Frenzy**: each new combo draws a random style (sword, dual knives, axe, mace or greatsword), never the same twice in a row | **Vaapad Whirlwind**: Krom greatsword's secondary, +50% damage, +40% stamina |

If a donor weapon is missing, the next one in its list is used, and failing that the saber keeps its own attack (the BepInEx log says which). Set `UseFormMovesets = false` to turn this off.

### The Force

These only work with a lightsaber drawn. They share a 4 s cooldown and cost 25 stamina before form modifiers.

* **G**: Force Push. A 70° cone out to 9 m that knocks back and staggers enemies.
* **Shift+G**: Force Pull. Drags enemies in that cone toward you.

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
| `Patches/CombatPatches.cs` | Harmony hooks: outgoing damage (`Character.Damage`), incoming damage and deflection (`Character.RPC_Damage`), blocks (`Humanoid.BlockAttack`), attack stamina (`Attack.GetAttackStamina`), attack speed (`CharacterAnimEvent`) |
| `Util/Compat.cs` | Calls game methods by reflection where their signatures differ between Valheim versions |

## Status and known limitations

* **Not yet tested in-game.** The project compiles against Jötunn 2.30.2, but the game's own assemblies were stubbed for that build. Report anything that misbehaves along with `BepInEx/LogOutput.log`.
* Blades are simple glowing cylinders. There's no ignition animation and no hum yet.
* Redirected bolts damage the shooter directly; they don't fly back as projectiles.
* Movesets play other weapons' animations while you hold a saber. If one of them doesn't trigger while a one-handed weapon is equipped, that technique will look wrong; the log names which vanilla attack each move uses.
* Jar'Kai (dual-wielding sabers) isn't implemented. Valheim's off-hand slot is for shields.
* `DamageScale` and `UseDurability` are applied when the item prefabs are built at startup, so keep them identical for everyone.
