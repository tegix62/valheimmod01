# The seven forms: lore and how the mod represents them

This page compares each form with the source material: what the lore says, what the mod does with it, and where the mod takes liberties. It's meant to make the design choices checkable rather than taken on trust.

## A note on "canon"

Disney-era canon confirms the seven classical forms and several of their practitioners, but it says little about how each form works. Almost all the detail (the "Way of the..." epithets, the alternate names, named techniques, and stated strengths and weaknesses) comes from Legends reference material. Canon hasn't contradicted it. The mod treats that Legends doctrine as the reference, and leans on the films where they show something directly, such as Mace Windu turning back Sidious' lightning.

Gameplay numbers (+25% and so on) are never in the lore. They're translations of described traits. The legend below marks how directly each mechanic follows from the source.

* **Lore:** directly described in the source material.
* **Inferred:** a reasonable reading of described traits, or of a film practitioner.
* **Game:** needed for Valheim, with no lore basis.

## Form I: Shii-Cho (Way of the Sarlacc, Determination Form)

**Lore.** The oldest and most basic form, taught to every Jedi. It uses wide, sweeping strikes suited to fighting several opponents at once. Its contact focus is *sun djem*, disarming or destroying the opponent's weapon (the Disarming Slash), and its signature technique is the **Sarlacc Sweep**, a wide sweep against multiple enemies. It's weak against a single skilled, mobile opponent and has no specialised blaster defence. Jedi in the Clone Wars tempered it with Form III and Form V for that.

| Mod mechanic | Basis |
|---|---|
| +8% damage per extra nearby enemy (max +40%) | Lore: made for multiple opponents |
| -10% damage against a lone enemy | Lore: weak against a single opponent |
| +20% stagger; primary **Disarming Slash** with +30% stagger | Lore: sun djem. Valheim has no disarm, so stagger stands in for it |
| Secondary **Sarlacc Sweep**: an all-round spin | Lore: named technique |
| No ranged defence | Lore |
| -10% attack stamina | Inferred: the simplest, least demanding form |

*Changed in this pass:* Sarlacc Sweep moved to the move that actually sweeps around you. Added the lone-enemy weakness. Removed an unjustified -5% damage-taken bonus.

## Form II: Makashi (Way of the Ysalamiri, Contention Form)

**Lore.** Developed for lightsaber-versus-lightsaber duelling. It's precise, economical and fencing-like (Count Dooku is its best-known practitioner). It's strong against one opponent and weak against several. It **lacks effective blaster-deflection training**. Named techniques include the **Makashi Riposte** (alter the angle of the opponent's attack, then counter at once) and **Contentious Opportunity** (spot an opening in the guard and strike it).

| Mod mechanic | Basis |
|---|---|
| +35% damage against a lone enemy, -15% when 3+ are near | Lore |
| +25% damage taken from ranged attacks | Lore: poor blaster deflection |
| **Contentious Opportunity**: only a *perfect parry* opens a +40% counter | Lore: technique; the parry window is the game translation |
| Secondary **Makashi Riposte** | Lore: named technique |
| Primary: spear thrusts with long reach and a narrow arc | Inferred: fencing lunges (Valheim's closest animation) |
| -20% attack stamina | Lore: economy of motion |

*Changed in this pass:* added the blaster weakness. The counter now requires a timed parry rather than any block, which is what separates it from Djem So. Renamed the invented "Contra-Riposte" to the lore's Makashi Riposte. Dropped an unjustified +30% stagger.

## Form III: Soresu (Way of the Mynock, Resilience Form)

**Lore.** Developed as blasters spread. It uses tight, economical movements that leave minimal openings and deflect fire, and its practitioners outlast their opponents. It's almost exclusively defensive. Its named technique is the **Circle of Shelter**, a defensive perimeter around the user and any allies.

| Mod mechanic | Basis |
|---|---|
| Blocking deflects every ranged attack | Lore |
| -25% damage taken, -20% damage dealt | Lore: defence at the cost of offence |
| +8 stamina per block, +25% stamina regen | Inferred: outlasting the opponent |
| Primary **Tight Guard**: short, compact cuts | Lore: economical movement |
| Secondary **Circle of Shelter**: low-damage spin that drives everyone back | Lore: technique (it protects the user; it can't shield allies in this mod) |

*Changed in this pass:* Circle of Shelter now clears a ring around you (an all-round spin) instead of a single-target bash.

## Form IV: Ataru (Way of the Hawk-Bat, Aggression Form)

**Lore.** Acrobatic and aggressive, with Force-assisted leaps, flips and spins (Yoda, Qui-Gon Jinn). It **needs open space** to work and performs poorly in confined quarters. It tires the user in prolonged fights. Its named technique is the **Hawk-Bat Swoop**, an extremely quick strike meant to land before the opponent can react.

| Mod mechanic | Basis |
|---|---|
| +20% attack speed, +15% damage, +15% move speed, higher jumps | Lore: aggression and acrobatics |
| Force Leap 40% stronger and half cost | Lore: Force-assisted acrobatics |
| **No attack speed or damage bonus indoors, in dungeons or under a roof** | Lore: weak in confined spaces |
| +25% attack stamina | Lore: weak in prolonged fights |
| +10% damage taken | Inferred: commits fully to offence |
| Secondary **Hawk-Bat Swoop**: a springing fast strike | Lore: named technique |

*Changed in this pass:* added the confined-space weakness. Renamed the secondary to the lore's Hawk-Bat Swoop. The leap's landing hit is now "Leaping Strike" to avoid reusing a lore name.

## Form V: Shien / Djem So (Way of the Krayt Dragon, Perseverance Form)

**Lore.** Two variants built on Soresu's defence, and most practitioners trained both. **Shien** is the older anti-blaster variant, which redirects fire back at the shooters (**Barrier of Blades**). **Djem So** came later for lightsaber duels: block or parry, then counter-attack immediately with power (**Fluid Riposte**, and **Falling Avalanche**, a powerful overhand strike from above). Anakin Skywalker, later Darth Vader, is its best-known practitioner.

| Mod mechanic | Basis |
|---|---|
| **Barrier of Blades**: blocking sends 75% of a ranged attack back at the shooter | Lore: Shien |
| **Fluid Riposte**: any block makes the next hit +50% | Lore: Djem So |
| +15% damage, +50% stagger | Lore: Djem So's power |
| Secondary **Falling Avalanche**: an overhand blow from above | Lore: named technique |
| -10% damage taken | Lore: built on Soresu |
| Saber throw +50% | Inferred from practitioners: Vader throws his saber in the films. This is not stated as Form V doctrine |

*Changed in this pass:* the redirect and riposte now carry their lore names. Falling Avalanche is described as the overhand strike it is, not a ground-shaking slam (the sledge animation is the closest Valheim has).

## Form VI: Niman (Way of the Rancor, Moderation Form)

**Lore.** A balanced blend of the earlier forms that integrates Force powers into combat, and the foundation for Jar'Kai (two-blade) fighting. It's criticised for having **no standout strength**, and mastering it takes a long time.

| Mod mechanic | Basis |
|---|---|
| Force powers 75% stronger and 40% cheaper | Lore: Force integration |
| Secondary **Force-Guided Strike**: a sword strike carrying a Force Push | Lore: Force woven into bladework (move name is the mod's own) |
| Small, even bonuses (+5% damage, -5% damage taken, +10% stamina regen) | Lore: balance without a standout strength |
| Jar'Kai | **Not implemented.** Valheim's off hand holds shields, not a second saber |

*Changed in this pass:* stamina regen trimmed from +15% to +10% so no single stat stands out.

## Form VII: Juyo / Vaapad (Way of the Vornskr, Ferocity Form)

**Lore.** The most demanding form. It's ferocious and seemingly erratic, and it draws on intense emotion, close enough to the dark side that the Jedi restricted it. Sith such as Darth Maul used it freely. **Vaapad** is Mace Windu's variant, the only Form VII variant the Council recognised. It channels the darkness of the duel in a controlled way, often described as a loop between the two fighters, and turns an opponent's power back on them. In *Revenge of the Sith*, Windu holds Sidious' lightning on his blade and drives it back toward him.

| Mod mechanic | Basis |
|---|---|
| +25% damage, +15% attack speed, +25% stagger | Lore: ferocity |
| Each new combo takes a random style | Lore: erratic and unpredictable |
| **Momentum**: each consecutive hit +5% (max +30%), broken by being hit | Inferred: the "loop" of the duel's darkness, which needs the fighter to keep control |
| **Blocking turns lightning back on its source** | Lore and film: Windu against Sidious |
| The only form that can call Force lightning | Inferred: the dark-side form. See below |
| +25% damage taken, -15% stamina regen | Lore: a dangerous, demanding form |

*Changed in this pass:* **removed lifesteal**, which has no basis in the lore. It's replaced by momentum, which models Vaapad's flow and its fragility. Added lightning redirection. Base damage lowered from +40% to +25% to make room for momentum.

## Force powers

| Power | Basis |
|---|---|
| Force Push / Pull | Lore: universal telekinesis |
| Force Leap | Lore: Force-assisted jumps (Ataru especially) |
| Saber throw | Lore and film: Vader, Luke and others throw sabers |
| **Force lightning** | **Dark side only** in the lore. By default only Form VII can call it. Set `LightningDarkSideOnly = false` for a looser, non-canonical game where any form can |

## Liberties that remain

* **The numbers.** Every percentage is a game translation, and balance may need tuning once it's played.
* **Animations.** Each form borrows the closest vanilla Valheim animation (e.g. spear thrusts for Makashi's lunges) until custom clips exist. See [ANIMATIONS.md](ANIMATIONS.md).
* **Invented move names.** Tight Guard, Fencer's Thrust, Hawk-Bat Flurry, Krayt Strikes, Rancor Balance, Force-Guided Strike, Vornskr Frenzy and Vaapad Whirlwind are the mod's own descriptive names for moves the lore doesn't name. The named techniques (Disarming Slash, Sarlacc Sweep, Makashi Riposte, Contentious Opportunity, Circle of Shelter, Hawk-Bat Swoop, Barrier of Blades, Fluid Riposte and Falling Avalanche) come from the Legends material.
* **Unlock order.** Forms unlock in numerical order with weapon skill. Jedi learned Shii-Cho first, but a real Jedi wouldn't necessarily progress I → VII.
* **Jar'Kai** (dual sabers) is absent.

## Sources consulted

These are secondary summaries of the Legends material. Wookieepedia itself couldn't be reached while this was written, so verifying against it is a good next step.

* [Form I, Wookieepedia](https://starwars.fandom.com/wiki/Form_I) (search summary); [JEDI HoloNet: Form I](https://www.jediholo.net/archives/documents-2/form-i-shii-cho/); [Archivum: Form I](https://starwars.archivum.wiki/article/form_i)
* [Form II (Legends), Wookieepedia](https://starwars.fandom.com/wiki/Form_II/Legends) (search summary); [Ultrasabers: Form II](https://ultrasabers.com/?p=1259900); [JEDI HoloNet: Form II](https://www.jediholo.net/archives/documents-2/form-ii-makashi/)
* [Form III, Wookieepedia](https://starwars.fandom.com/wiki/Form_III) (search summary); [Ultrasabers: Form III](https://ultrasabers.com/?p=1259954); [Archivum: Form III](https://starwars.archivum.wiki/article/form_iii)
* [Form IV, Wookieepedia](https://starwars.fandom.com/wiki/Form_IV) (search summary); [Ultrasabers: Form IV](https://ultrasabers.com/holocron/a-deeper-look-lightsaber-combat-forms-form-iv-ataru/); [Archivum: Form IV](https://starwars.archivum.wiki/article/Form_IV)
* [Ultrasabers: Form V](https://ultrasabers.com/holocron/a-deeper-look-lightsaber-combat-forms-form-v-shien-djem-so); [Archivum: Form V](https://starwars.archivum.wiki/article/Form_V); [JEDI HoloNet: Form V](https://www.jediholo.net/?p=5347)
* [Form VI, Wookieepedia](https://starwars.fandom.com/wiki/Form_VI) (search summary); [Ultrasabers: Form VI](https://ultrasabers.com/holocron/a-deeper-look-lightsaber-combat-forms-form-vi-niman/); [JEDI HoloNet: Form VI](https://jediholo.net/?p=5352)
* [Ultrasabers: Form VII](https://ultrasabers.com/holocron/a-deeper-look-at-lightsaber-combat-forms-form-vii-vapaad-juyo); [Archivum: Form VII](https://starwars.archivum.wiki/article/Form_VII)
