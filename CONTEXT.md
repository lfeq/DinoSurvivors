# DinoSurvivors

DinoSurvivors is a bullet heaven roguelite about surviving escalating dinosaur waves long enough to reach an escape chopper.

## Language

**Bullet Heaven Roguelite**:
A run-based survival action game where the player stacks temporary upgrades during a run while permanent meta-progression persists between runs.
_Avoid_: Roguelike

**PC MVP**:
The first playable target for DinoSurvivors, focused on keyboard and mouse play before console or mobile adaptations.
_Avoid_: Simultaneous PC/console/mobile launch

**2D Top-Down MVP**:
The initial presentation mode using 2D top-down gameplay to prove mechanics before the intended 2.5D final presentation.
_Avoid_: 2.5D-first implementation

**Player Fantasy**:
The feeling of being a desperate park survivor who starts underpowered and hunted, then snowballs into a ridiculous dinosaur-clearing force just long enough to escape.
_Avoid_: Pure power fantasy, horror game

**Top-Down Movement**:
WASD movement in a top-down play space where positioning through dinosaur pressure is the primary skill expression.
_Avoid_: Click-to-move, side-scrolling movement

**Follow Camera**:
A camera that follows the player through an arena larger than the screen.
_Avoid_: Whole-arena single-screen camera

**Control Action**:
A named player intent such as Move, Aim, Fire, Pause, Confirm, or Cancel.
_Avoid_: Raw key-specific control design, Interact in MVP

**Mouse Aiming**:
A PC combat control where the mouse controls aim direction independently of WASD movement.
_Avoid_: Pure auto-aim baseline

**Auto-Fire**:
The default firing mode where weapons attack on cooldown toward the mouse aim direction without requiring repeated attack input.
_Avoid_: Mandatory manual firing

**Fire Mode Setting**:
A player setting that can disable Auto-Fire for players who prefer manual attack input.
_Avoid_: Forced auto-fire only

**Pause Menu**:
A gameplay-freezing menu where the player can resume, change settings, or quit the run.
_Avoid_: Real-time pause overlay

**Gameplay HUD**:
The in-run interface showing HP, XP progress, stage timer, stage number, Banked and Unbanked Jurassic Cash, weapon slots, passive slots, and the Exit Marker when active.
_Avoid_: Hidden run state

**Combat Feedback**:
The MVP readability feedback for attacks and damage, including enemy hit flash, floating damage numbers enabled by default with a settings toggle, a basic hit sound, player damage flash, HP changes, enemy death pop, and drops.
_Avoid_: Silent damage, expensive VFX-first feedback

**Manual Fire**:
The optional firing mode where holding the attack input fires weapons on cooldown toward the mouse aim direction.
_Avoid_: Click-spam firing, uncapped firing

**Aimed Weapon**:
A weapon that fires toward the mouse aim direction.
_Avoid_: Auto-targeted weapon

**Autonomous Weapon**:
A weapon that triggers automatically around or near the player without using mouse aim.
_Avoid_: Aimed weapon

**Park Intern**:
The MVP playable character, an underprepared park worker with average speed, low damage, fragile HP, normal pickup radius, normal cooldown, and no starting bonuses beyond the Tranq Pistol.
_Avoid_: Hero soldier, unnamed player

**Tranq Pistol**:
The player's starting Aimed Weapon, with moderate fire rate, low damage, and upgrade potential for knockback or slowing.
_Avoid_: Starting rifle, generic pistol

**Flare Gun**:
An MVP Aimed Weapon that fires a piercing or explosive line shot.
_Avoid_: Generic rifle

**Bug Zapper**:
An MVP Autonomous Weapon that creates close-range electric area damage around the player.
_Avoid_: Generic aura

**Weapon Slot**:
One of three run inventory spaces for weapons.
_Avoid_: Unlimited weapon inventory

**Weapon Level**:
A weapon's run-local upgrade rank, where level 1 unlocks the weapon and level 5 is max level.
_Avoid_: Weapon rarity

**Passive Slot**:
One of three run inventory spaces for passive items.
_Avoid_: Unlimited passive inventory

**Passive Level**:
A passive item's run-local upgrade rank, where level 1 unlocks the passive and level 3 is max level.
_Avoid_: Passive rarity

**Passive Item**:
A temporary run upgrade that modifies player stats without adding weapon-specific interactions.
_Avoid_: Weapon modifier, evolution component

**First Aid Fanny Pack**:
An MVP Passive Item that increases max HP.
_Avoid_: Generic health passive

**Foam Dino Claw**:
An MVP Passive Item that increases damage.
_Avoid_: Generic damage passive

**Running Shoes**:
An MVP Passive Item that increases move speed.
_Avoid_: Generic speed passive

**Energy Drink**:
An MVP Passive Item that improves weapon cooldown.
_Avoid_: Generic cooldown passive

**Souvenir Magnet**:
An MVP Passive Item that increases pickup radius.
_Avoid_: Generic pickup passive

**Run**:
A complete escape attempt made of multiple survival stages and ending when the player dies or escapes by helicopter.
_Avoid_: Single 10-minute match

**Stage**:
A finite rectangular park arena within a run that lasts about 10 minutes before revealing a destination to advance, with no obstacles in the MVP.
_Avoid_: Level, continuous open-world region, endless arena

**Safehouse**:
A boundary-side park building destination that completes a stage when reached, such as a utility shed, lab, visitor center, or maintenance building.
_Avoid_: Building, intermediate stage

**Stage Exit**:
The end-of-stage objective where touching a revealed Safehouse or Heliport zone completes the transition while enemies continue spawning.
_Avoid_: Automatic stage completion, press-to-interact exit

**Exit Marker**:
A persistent directional arrow and world-space marker that guides the player to the active Stage Exit.
_Avoid_: Hidden exit, minimap-only exit

**Safehouse Break**:
A brief between-stage reward screen offering one of three immediate rewards without fully healing the player by default.
_Avoid_: Full heal checkpoint, next-stage buff screen

**Temporary Run Upgrade**:
A weapon, stat increase, or upgrade choice that persists across stages within the same run but is lost when the run ends.
_Avoid_: Permanent upgrade, per-stage upgrade

**Level Up Choice**:
A paused one-of-three reward selection offered when the player gains enough XP, containing eligible temporary weapons, weapon upgrades, or passive stat upgrades with light category balancing.
_Avoid_: Real-time upgrade popup, unrestricted upgrade list, three same-category choices when avoidable

**XP Gem**:
A collectible dropped by defeated enemies that grants progress toward the next Level Up Choice.
_Avoid_: Automatic XP, experience orb

**Pickup Merging**:
A performance rule where excessive nearby XP Gems or Jurassic Cash pickups combine into higher-value pickups.
_Avoid_: Unlimited pickup clutter

**Pickup Radius**:
The player stat that determines how close the player must be to collect XP Gems and other pickups.
_Avoid_: Global magnet by default

**Player Stat**:
One of the MVP player attributes: max HP, damage, move speed, weapon cooldown, or pickup radius.
_Avoid_: Luck, armor

**Field Pickup**:
A timed, missable pickup spawned during a stage that grants a short temporary effect when collected.
_Avoid_: Temporary Run Upgrade, permanent pickup

**Jurassic Cash**:
The run currency found in stages from enemy drops and Safehouse Break rewards, flavored as loose tourist cash or park vouchers.
_Avoid_: Gold, coins, cash

**Unbanked Jurassic Cash**:
Jurassic Cash collected during the current stage that is lost if the player dies before reaching a Safehouse or the Heliport.
_Avoid_: Guaranteed currency

**Banked Jurassic Cash**:
Jurassic Cash secured by reaching a Safehouse or the Heliport and retained after the run ends.
_Avoid_: Stage cash, unspent coins

**Souvenir Shop**:
The meta-progression hub where Banked Jurassic Cash buys permanent park-survivor advantages flavored as tacky Jurassic tourist merchandise.
_Avoid_: Generic upgrade shop

**Permanent Upgrade**:
A persistent advantage bought from the Souvenir Shop that applies across future runs, initially as three-rank linear tracks for max HP, damage, move speed, weapon cooldown, and pickup radius.
_Avoid_: Temporary Run Upgrade

**Save Data**:
Persistent player data containing Banked Jurassic Cash, purchased Permanent Upgrade ranks, settings, and best run summary.
_Avoid_: In-progress run persistence in MVP

**Heliport**:
The final run destination where the escape chopper waits and the T-Rex boss arena begins.
_Avoid_: Final Safehouse

**Boss Arena Lock-In**:
The final-stage state where entering the Heliport arena blocks escape routes until the T-Rex is defeated.
_Avoid_: Open-map boss kiting

**Compy**:
A small, fast, low-HP swarmer dinosaur.
_Avoid_: Generic weak enemy

**Raptor**:
A medium, aggressive chaser dinosaur with higher damage.
_Avoid_: Generic medium enemy

**Triceratops**:
A slow, high-HP bruiser dinosaur that pressures pathing.
_Avoid_: Generic tank enemy

**Direct Pursuit**:
Enemy movement that generally moves toward the player without obstacle navigation in the MVP.
_Avoid_: Obstacle-aware pathfinding in MVP, random wandering baseline

**Contact Damage**:
Enemy damage dealt by touching the player, limited by a per-enemy damage cooldown.
_Avoid_: Ranged regular enemy attacks

**Soft Collision**:
Collision behavior where enemies can impede the player slightly but should not fully trap the player or create enemy traffic jams.
_Avoid_: Full body blocking

**Spawn Zone**:
A valid navigable area where enemies can appear off-screen or near the screen edge outside the player's minimum safe radius.
_Avoid_: On-player spawning, obstacle spawning

**Wave Schedule**:
A stage-specific timed escalation plan that increases spawn rate and enemy mix as minutes pass.
_Avoid_: Pure random spawning

**Live Enemy Cap**:
A stage-specific maximum number of active enemies that protects performance and prevents runaway spawn buildup.
_Avoid_: Unlimited enemy accumulation

**T-Rex**:
The final boss guarding the Heliport, using readable multi-phase attacks such as charge, roar/stun zone, tail sweep, and summon waves.
_Avoid_: Elite dinosaur, regular enemy

## Relationships

- The **PC MVP** defines the initial control, UI, and performance target.
- The **2D Top-Down MVP** proves gameplay before the intended 2.5D final presentation.
- A **Bullet Heaven Roguelite** run contains temporary upgrade choices and may contribute to permanent meta-progression.
- The **Player Fantasy** depends on escalation from hunted vulnerability to temporary crowd-clearing power.
- The **PC MVP** uses **Top-Down Movement** with **Mouse Aiming**.
- The **PC MVP** defines controls as **Control Actions** rather than raw key assumptions.
- The **2D Top-Down MVP** uses a **Follow Camera**.
- **Auto-Fire** is enabled by default and can be disabled through the **Fire Mode Setting**.
- The **Pause Menu** freezes gameplay and allows settings changes.
- The **Gameplay HUD** exposes the run state needed for moment-to-moment decisions.
- **Combat Feedback** makes hits, damage, deaths, and drops readable in the MVP.
- MVP audio is limited to a basic hit sound.
- **Manual Fire** uses the same weapon cooldowns as **Auto-Fire**.
- **Aimed Weapons** use **Mouse Aiming**.
- **Autonomous Weapons** do not use **Mouse Aiming**.
- The MVP playable character is the **Park Intern**.
- The **Park Intern** starts each run with the **Tranq Pistol**.
- MVP weapons are **Tranq Pistol**, **Flare Gun**, and **Bug Zapper**, with five total weapons planned after MVP.
- The player can hold up to three **Weapon Slots** and three **Passive Slots** during a **Run**.
- Each weapon has five **Weapon Levels**.
- Weapons do not evolve or combine in the MVP.
- Once all **Weapon Slots** are full, **Level Up Choices** stop offering new weapons.
- Each passive item has three **Passive Levels**.
- **Passive Items** are pure stat modifiers in the MVP.
- MVP **Passive Items** are **First Aid Fanny Pack**, **Foam Dino Claw**, **Running Shoes**, **Energy Drink**, and **Souvenir Magnet**.
- MVP **Player Stats** are max HP, damage, move speed, weapon cooldown, and pickup radius.
- Once all **Passive Slots** are full, **Level Up Choices** stop offering new passive items.
- A complete **Run** contains three normal **Stages** followed by one final **Heliport** stage.
- Each **Stage** is a separate park region loaded as its own play space.
- MVP **Stages** do not contain obstacles.
- MVP **Stages** are open arenas distinguished by visual dressing, boundaries, lighting, and **Wave Schedule** identity.
- MVP **Stages** use hard boundaries; the player cannot leave until the **Stage Exit** is active.
- Each normal **Stage** reveals a **Safehouse** after about 10 minutes.
- Reaching a **Safehouse** completes the current **Stage** and advances the **Run**.
- A **Stage Exit** appears after about 10 minutes and requires physical movement into the **Safehouse** zone.
- An **Exit Marker** guides the player to the active **Stage Exit**.
- There is no hard time limit after a **Stage Exit** appears; enemy pressure continues until the player reaches it.
- A **Safehouse** appears at a boundary-side exit point far enough from the player to require traversal.
- A **Safehouse Break** occurs after reaching a **Safehouse** and offers immediate rewards such as a partial heal, Banked Jurassic Cash bonus, or bonus XP.
- HP can be restored through **Field Pickups**, **Safehouse Break** reward choices, or specific **Permanent Upgrades**.
- Normal **Level Up Choices** do not offer healing.
- Player HP, weapons, weapon levels, passive items, **Unbanked Jurassic Cash**, and **Banked Jurassic Cash** carry forward across **Stages**.
- **Temporary Run Upgrades** persist across **Stages** in the same **Run**.
- A **Level Up Choice** pauses gameplay and offers one of three **Temporary Run Upgrades**.
- **Level Up Choices** are random among eligible options but avoid three same-category offers when possible.
- **Level Up Choices** do not include reroll, skip, or banish in the MVP.
- XP required for each **Level Up Choice** increases during a **Run**.
- There is no explicit player level cap in the MVP; finite upgrade pools and XP requirements create a practical cap.
- If no eligible **Temporary Run Upgrades** remain, a **Level Up Choice** grants a small **Unbanked Jurassic Cash** fallback instead of healing.
- Defeated enemies drop **XP Gems**.
- The player collects **XP Gems** by moving within **Pickup Radius**.
- **XP Gems** remain on the ground until collected or the **Stage** ends.
- Uncollected **XP Gems** and temporary pickups are abandoned when the **Stage** ends.
- **Field Pickups** expire if not collected quickly enough.
- A **Field Pickup** effect starts on collection and does not become a **Temporary Run Upgrade**.
- Active **Field Pickup** effects end when the **Stage** ends.
- A **Run** is lost when player HP reaches 0.
- Reaching a **Safehouse** or the **Heliport** converts all **Unbanked Jurassic Cash** into **Banked Jurassic Cash** before any reward screen.
- **Unbanked Jurassic Cash** is lost if the player dies before reaching a **Safehouse** or the **Heliport**.
- Quitting a **Run** keeps **Banked Jurassic Cash** and loses **Unbanked Jurassic Cash** plus **Temporary Run Upgrades**.
- **Banked Jurassic Cash** is retained after the **Run** ends.
- Regular enemies have a small chance to drop **Jurassic Cash**.
- **Safehouse Break** rewards can add **Banked Jurassic Cash**.
- **Banked Jurassic Cash** can be spent in the **Souvenir Shop** on **Permanent Upgrades**.
- MVP **Permanent Upgrades** are three-rank linear tracks for max HP, damage, move speed, weapon cooldown, and pickup radius.
- **Permanent Upgrade** costs escalate by rank, with rank 1 intended to be affordable after a partial successful **Run**.
- MVP has no unlockable characters, weapons, passives, or stages; only **Permanent Upgrade** ranks are purchased.
- **Save Data** persists **Banked Jurassic Cash**, purchased **Permanent Upgrade** ranks, settings, and best run summary.
- In-progress **Runs** are not persisted in the MVP.
- The final **Stage Exit** leads to the **Heliport** instead of a **Safehouse**.
- MVP regular enemy types are **Compy**, **Raptor**, and **Triceratops**; their plural names are Compies, Raptors, and Triceratops.
- Regular enemies use **Direct Pursuit** in the MVP.
- Regular enemies deal **Contact Damage** in the MVP.
- Enemies use **Soft Collision** with the player and with each other.
- Enemies spawn in **Spawn Zones** distributed around the player with randomness.
- Each **Stage** uses a distinct **Wave Schedule** to escalate spawn rate and enemy mix over time.
- Each **Stage** has a **Live Enemy Cap**; scheduled spawns are skipped or delayed while the cap is reached.
- The **Live Enemy Cap** counts living enemies only; dropped **XP Gems** and **Jurassic Cash** pickups do not count toward it.
- **Pickup Merging** prevents excessive **XP Gem** and **Jurassic Cash** pickup clutter.
- Stage 1 emphasizes Compies and introduces Raptors late.
- Stage 2 makes Raptors common and introduces Triceratops.
- Stage 3 uses dense mixed waves and heavy Triceratops pressure.
- The **Heliport** stage focuses on the **T-Rex** with supporting waves.
- The **T-Rex** must be defeated before the player can escape from the **Heliport**.
- After defeating the **T-Rex**, the player must touch the escape chopper zone to win the **Run**.
- The **T-Rex** fight occurs in a boss arena around the **Heliport**.
- Entering the **Heliport** boss arena triggers **Boss Arena Lock-In**.

## Example dialogue

> **Dev:** "Should we call DinoSurvivors a roguelike?"
> **Domain expert:** "No — it is a **Bullet Heaven Roguelite** because permanent progression exists outside individual runs."
>
> **Dev:** "Should the player feel unstoppable from the start?"
> **Domain expert:** "No — the **Player Fantasy** requires starting hunted and earning temporary power before escaping."
>
> **Dev:** "Does combat use pure auto-aim?"
> **Domain expert:** "No — the **PC MVP** uses **Top-Down Movement** with **Mouse Aiming**."
>
> **Dev:** "Does the player have to click constantly to shoot?"
> **Domain expert:** "No — **Auto-Fire** is enabled by default, but players can disable it with the **Fire Mode Setting**."
>
> **Dev:** "Can players fire faster by clicking rapidly in manual mode?"
> **Domain expert:** "No — **Manual Fire** is hold-to-fire and still obeys weapon cooldowns."
>
> **Dev:** "Does every weapon aim at the cursor?"
> **Domain expert:** "No — **Aimed Weapons** use the cursor, while **Autonomous Weapons** trigger around or near the player."
>
> **Dev:** "What does the player start with?"
> **Domain expert:** "The player starts each run with the **Tranq Pistol**, an **Aimed Weapon**."
>
> **Dev:** "Does a run end after surviving 10 minutes?"
> **Domain expert:** "No — a complete **Run** has three normal **Stages** and a final **Heliport** stage; surviving 10 minutes reveals a **Safehouse**, completing only the current **Stage**."
>
> **Dev:** "Do upgrades reset when entering the next Safehouse?"
> **Domain expert:** "No — player HP, weapons, weapon levels, passive items, currency state, and **Temporary Run Upgrades** persist across **Stages** until the **Run** ends."
>
> **Dev:** "Does leveling happen in real time?"
> **Domain expert:** "No — a **Level Up Choice** pauses gameplay and offers one of three **Temporary Run Upgrades**."
>
> **Dev:** "Does XP go straight to the player when enemies die?"
> **Domain expert:** "No — enemies drop **XP Gems**, and the player must move within **Pickup Radius** to collect them."
>
> **Dev:** "Can players collect leftover XP after entering a Safehouse?"
> **Domain expert:** "No — uncollected **XP Gems** and temporary pickups are abandoned when the **Stage** ends."
>
> **Dev:** "Is a 30-second double-damage pickup a run upgrade?"
> **Domain expert:** "No — it is a **Field Pickup**, a timed, missable pickup with a short temporary effect."
>
> **Dev:** "Does a Field Pickup effect carry into the next Stage?"
> **Domain expert:** "No — active **Field Pickup** effects end when the **Stage** ends."
>
> **Dev:** "If the player dies with Jurassic Cash picked up in the current Stage, do they keep it?"
> **Domain expert:** "No — only **Banked Jurassic Cash** is retained; **Unbanked Jurassic Cash** is lost on death."
>
> **Dev:** "When does current-stage Jurassic Cash become safe?"
> **Domain expert:** "When the player reaches a **Safehouse** or the **Heliport**, all **Unbanked Jurassic Cash** immediately becomes **Banked Jurassic Cash**."
>
> **Dev:** "Is the meta-upgrade screen just a generic stat shop?"
> **Domain expert:** "No — it is the **Souvenir Shop**, where **Banked Jurassic Cash** buys **Permanent Upgrades** as tacky Jurassic tourist merchandise."
>
> **Dev:** "Which regular dinosaurs are in MVP?"
> **Domain expert:** "**Compies**, **Raptors**, and **Triceratops** are the MVP regular enemies."
>
> **Dev:** "Can the player just dodge the T-Rex and board the helicopter?"
> **Domain expert:** "No — the **T-Rex** is the final boss and must be defeated before escaping from the **Heliport**."
>
> **Dev:** "Does the run end automatically when the T-Rex dies?"
> **Domain expert:** "No — after defeating the **T-Rex**, the player must touch the escape chopper zone to win."

## Flagged ambiguities

- "roguelike" was used in the draft GDD, but the resolved canonical term is **Bullet Heaven Roguelite** because the design includes permanent meta-progression.
- PC, console, and mobile were all listed initially; resolved that **PC MVP** is the first target and other platforms are deferred.
- Presentation starts as **2D Top-Down MVP**, while the final visual target is 2.5D.
- "session" and "10 min" were initially ambiguous; resolved that 10 minutes opens progression to the next **Stage** rather than automatically completing the **Run**.
- "level" is avoided for survival areas because **Level Up** will describe player progression during a run.
- **Stages** are separate park regions, not connected areas of one continuous open world.
- Obstacles and obstacle-aware navigation are out of MVP scope.
- Regular enemy behavior comes primarily from stats and direct pressure, not complex individual AI.
- Ranged regular enemies are out of MVP scope.
- Elite enemies are out of MVP scope.
- Weapon and passive inventory is capped at three each, not the genre-common six each.
- Weapon evolution and weapon combination mechanics are out of MVP scope.
- Luck and armor are out of MVP scope.
- Unlockable characters, weapons, passives, and stages are out of MVP scope.
- Level-up reroll, skip, and banish mechanics are out of MVP scope.
- Post-MVP weapon roster expansion from three to five total weapons is planned but out of MVP scope.
- Item replacement is out of MVP scope; full slots remove new item offers from **Level Up Choices**.
