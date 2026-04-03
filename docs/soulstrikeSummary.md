# Soul Strike — Design Reference Guide

> Developer: Tikitaka Studio / Publisher: Comtus Holdings
> Genre: Idle Action RPG / Engine: Unity / Platform: Android, iOS
> Release: January 17, 2024

Use this as the north star for SoloHero's design decisions.

---

## Core Identity

- **Keyword:** "Cool idle game" — landscape orientation, active skill input, NOT pure idle
- **Screen:** Horizontal landscape (same as SoloHero 1920x1080)
- **Differentiator:** Field movement + active skills for manual control enjoyment on top of auto-battle
- **Monetization:** Gacha (summon tickets), cosmetics (~999 appearance items), battle pass

---

## UI Layout (Landscape)

### HUD (always visible)
- **Top-left:** Player info card — avatar, name, level, HP/SP bars, gold
- **Top-center:** Stage name, kill/progress bar, region debuff indicator
- **Top-right:** Vertical icon buttons — Shop / Pass / Contents / Menu
- **Gameplay area:** 3D field, character moves freely, enemies spawn around

### Bottom Tab Bar (5 tabs)
| Tab | Icon | Function |
|-----|------|----------|
| Character | person | Character stats, job info |
| Growth | up-arrow | Training, Traits, Awakening, Abilities, Constellations |
| **[Center]** | auto icon | **Auto-battle toggle** (highlighted, distinct color) |
| Summon | star | Gacha panel (all summon pools) |
| Equipment | sword | Soul Gear management + Inventory |

### Panels (slide up from bottom, ~50% screen height)
- Each panel has a close (×) button top-right
- Smooth slide animation (DOTween)
- Opening one auto-closes another

---

## Summon System (Gacha)

### Summon Pools (separate tabs within Summon panel)
1. **Job** — Common → Unique (7 tiers)
2. **Skill** — General → Mythic
3. **Companion** — Rare → Mythic
4. **Artifact** — separate resource system
5. **Pet** — Common → Mythic

### Mechanics
- Daily free pulls: 5 initial → up to 30/day per category
- Pickup summons unlock at category level 6 (Mythic tier only, event-driven)
- Pity system exists (guaranteed Mythic after N pulls)
- Buff system: unequipped summon items still provide passive effects

### SoloHero Equivalent (current)
- Single pool: Weapon / Helmet / Armor / Boots (4 slots, 4 grades)
- Pity: 100 pulls = guaranteed Legendary
- Box-Muller normal distribution for grade weighting

### SoloHero Roadmap
- [ ] Skill summon pool (active skills as gacha items)
- [ ] Companion summon pool
- [ ] Daily free pulls (5/day)

---

## Equipment System (Soul Gear)

### Tiers
Common → Uncommon → Rare → Epic → Legendary → Mythic → **Unique**

### Acquisition Loop
1. Soul orbs charge passively as monsters die during combat
2. On full charge: 3 equipment options appear (player picks 1)
3. Dimension Crystals allow rerolls
4. Stellar Essence upgrades equipment tiers

### Mythic Equipment (5-line stat system)
- Line 1 (fixed): slot-specific stat (ATK / HP / DEF / Lifesteal / ATK Speed / Element Strength)
- Lines 2–4: random secondary stats
- Line 5: enchantment / ability bonus
- Strategy: get 3–4 good lines before upgrading to Unique

### Unique Equipment
- Crafted via alchemy from Mythic + materials
- Higher base stats than Mythic
- Socket system: 1–2 gem sockets
- Socket expansion: 20% base success, +1% per failure, resets on success

### SoloHero Equivalent (current)
- 4 slots (Weapon/Helmet/Armor/Boots), 4 grades (Common/Rare/Epic/Legendary)
- Stats: attackBonus, defenseBonus, hpBonus per equipment
- No tier upgrade system yet

### SoloHero Roadmap
- [ ] Soul orb passive charge during combat → equipment drop popup
- [ ] 5-line stat system on Legendary/Mythic tier
- [ ] Equipment upgrade (tier promotion)

---

## Growth System

### Training (stat allocation)
**Basic:** Attack Power, Defense, HP, Critical Chance, Critical Damage
**Advanced:** Attack Speed, Movement Speed, Lifesteal, Mana Recovery

### Traits (5 color categories)
Dark / Light / Yellow / Red / Green — each has upgrade tree with Battle Tokens

### Awakening & Ability (6-line build system)
- Weapon Meta: 2x ATK Power + 2x Element Strength + 2x Crit Damage
- Companion Meta: 2x ATK Power + 1x Element + 1x Crit Damage + 2x Companion Damage

### Constellation System (3 constellations)
- Aquarius → Attack Power
- Taurus → Skill Damage
- Sagittarius → Final Damage
- Stardust from Shelter constellation library

### Gem Crafting
- Gems slot into Unique equipment sockets only
- 3 identical gems → combine → upgrade tier
- Sources: Dark Citadel drops, Guild Shop (3/week)

### SoloHero Equivalent (current)
- UpgradeService: HP / ATK / DEF / SPD (4 stats, max level 50)
- Cost formula: baseCost × (level + 1)

### SoloHero Roadmap
- [ ] Critical Chance / Critical Damage stats
- [ ] Attack Speed stat
- [ ] Skill system (active skills with cooldowns)

---

## Stage / Progression System

### Standard Stages
- Format: Chapter-Stage (e.g., 1-1 through 6-100)
- Each stage: spawn N enemies → kill all → reward gold → auto-advance

### Conqueror Mode (endgame)
- Unlocks after clearing 6-100
- Free movement on open map (no fixed stages)
- Percentage charge system (100% = teleport unlock)
- Resets bi-weekly
- Timed events: Treasure Chests, Traveling Merchants, Field Bosses

### SoloHero Equivalent (current)
- Formula-based scaling: kills = base(5)+(ch-1)×10+(stage-1)×2
- Auto-advance to next stage after clear
- No Conqueror mode yet

### SoloHero Roadmap
- [ ] Conqueror-style open map after clearing all chapters
- [ ] Field boss events
- [ ] Timed chest/merchant events

---

## Dungeon / Challenge Content

| Content | Reward | Mechanic |
|---------|--------|----------|
| Goblin Dungeon | Silver, Gold, EXP crystals | Kill monsters in time limit |
| Labyrinth Maze | Origin Elements | Defeat 4 bosses |
| Orc Training Ground | Awakening Potions | Attack dummies for score |
| Endless Jar | Spiritstone | Jar breaks per hit (tick damage) |
| Crystal Dungeon | Ether | Protect crystal from waves |
| Specter Hunt | Mysterious Seeds | Predictable boss movement |
| Trial Tower | Various | Floor challenge, debuffs at floor 10+ |
| Boss Raid | Abyss Stones | 10-tier difficulty, progressive debuffs |
| Dark Citadel | Gems | Random room debuffs |

### SoloHero Roadmap
- [ ] At least 2–3 dungeon types post-launch
- [ ] Boss Raid (weekly, guild or solo)
- [ ] Trial Tower (infinite floor challenge)

---

## Shelter (Base Building)

Hub system providing passive bonuses through upgradeable facilities:

| Facility | Benefit |
|----------|---------|
| Crafting Table | Item crafting |
| Alchemy Pot | Equipment tier upgrades |
| Artifact Holder | Artifact passive bonuses |
| Constellation Library | Stardust production |
| Spirit Research Lab | Spirit bonuses |
| Restaurant (level 8+) | Superior recipe buffs |
| Black Market (level 3+) | Mythic item reveals |
| Wardrobe | Equipment presets (3) |

### SoloHero Note
Not planned for initial release. Consider as mid-term content (post-launch).

---

## Guild System

- Entry: Conqueror 10+
- Activities: Donations, attendance, dungeons, raids, conquest battles
- Guild Dungeons: 2 entries/day
- Guild Raids: 3 entries/week, check boss weakness (Freeze / Stun / Provoke)
- Guild Conquest Wars: Saturday 12:00–24:00, territory bonuses (e.g., Wind +20%, Crit Damage +10%)

### SoloHero Note
Post-launch feature. Skip for v1.0.

---

## Companion System

- Up to 4 active companions follow the character
- Inactive companions still provide passive stat bonuses
- Companions participate in Dispatch missions
- Synergy with specific skills matters more than raw tier in some content

### SoloHero Roadmap
- [ ] 1–2 companion slots (passive stat bonus only, v1.5)
- [ ] Companion summon pool (separate from equipment gacha)

---

## Offline Reward / Passive Income

- Soul orbs accumulate passively while offline (equipment drops offline)
- Gold accumulates offline
- Conqueror mode: online farming more efficient than offline
- AdMob rewarded ad → 2x offline reward (same as SoloHero plan)

---

## Economy & Currencies

| Currency | Source | Use |
|----------|--------|-----|
| Gold | Stage clear, offline | Training, upgrades |
| Ether | Dungeon, stages | Equipment option reroll |
| Dimension Crystals | Events, dungeons | Equipment reroll |
| Stellar Essence | Raids, events | Equipment tier upgrade |
| Abyss Stones | Boss Raid | Raid shop |
| Battle Tokens | Level-up | Trait upgrades |
| Summon Tickets | Events, daily free | Gacha pulls |

### SoloHero Current
- Only currency: Gold (offline reward + stage clear)
- Roadmap: add premium currency for x10 pulls

---

## Key Design Lessons from Soul Strike

1. **Horizontal layout is the differentiator** — lean into it fully
2. **Active skills matter** — pure idle is boring; give players things to press
3. **Separate summon pools** add longevity (players always have something to pull)
4. **Soul orb drop system** is elegant — passive charge during combat, select from 3 options
5. **Shelter/base building** adds a meta-game layer outside combat
6. **Auto-battle toggle** must be prominent (center tab, distinct color)
7. **Stage format 1-1 through 6-100** then open-world Conqueror mode is a solid progression arc
8. **Optimization issues were the main complaint** — keep draw calls and particle effects controlled
9. **Repetitive clicking fatigue** — auto systems must be robust and satisfying
10. **Companion diversity** beats pure damage optimization in many content types — reward experimentation
