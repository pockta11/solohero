namespace SoloHero.Core.Config
{
    public sealed class BalanceValues
    {
        public double HP_BASE = 100;
        public double ATK_BASE = 10;
        public double DEF_BASE = 20;
        public double ATKSPD_BASE = 1.0;
        public double MOVE_SPEED = 2.0;
        public double ATTACK_RANGE = 1.6;
        public double BASIC_SKILL_MULT = 3.0;
        public int BASIC_SKILL_TARGETS = 3;
        public double BASIC_SKILL_RANGE = 2.4;
        public double CRIT_RATE_BASE = 5;
        public double CRIT_MULT = 1.5;
        public double DEF_REF_MULT = 3;
        // D-141 level-up AP (MapleStory style, replaces the flat per-level gains): every level past 1 grants AP_PER_LEVEL
        // points. A point in the main stat (STR / INT / DEX by job line) adds AP_MAIN_ATK to base ATK, one in vitality
        // AP_VIT_HP to base HP. Auto mode splits them AP_AUTO_MAIN : AP_AUTO_VIT, which gives the old +0.5 ATK / +5 HP a
        // level. A manual reset costs AP_RESET_GEM gems.
        public int AP_PER_LEVEL = 3;
        public double AP_MAIN_ATK = 0.25;
        public double AP_VIT_HP = 5;
        public int AP_AUTO_MAIN = 2;
        public int AP_AUTO_VIT = 1;
        public int AP_RESET_GEM = 50;
        public double EXP_REQ_BASE = 200;
        public double EXP_REQ_GROWTH = 1.18;
        public double UPG_COST_GROWTH = 1.12;
        public double UPG_BASE_HP = 300;
        public double UPG_BASE_ATK = 450;
        public double UPG_BASE_DEF = 450;
        public double UPG_BASE_SPD = 600;
        public double UPG_STAT_MULT = 1.16;
        public double UPG_GAIN_SPD = 0.02;
        public int UPG_MAX_LEVEL_SPD = 100;
        // D-142 crit lanes, opened by hero level: crit rate +UPG_GAIN_CRIT points a level (capped), crit damage
        // +UPG_GAIN_CRITDMG (a fraction added to CRIT_MULT) a level.
        public double UPG_BASE_CRIT = 300;
        public double UPG_BASE_CRITDMG = 300;
        public double UPG_GAIN_CRIT = 2;
        public int UPG_MAX_LEVEL_CRIT = 25;
        public double UPG_GAIN_CRITDMG = 0.1;
        public int UPG_UNLOCK_LV_CRIT = 15;
        public int UPG_UNLOCK_LV_CRITDMG = 25;
        // D-143 limit break: a lane stops every LIMIT_STEP levels until broken with LIMIT_STONE_BASE x k breakthrough
        // stones (the k-th break). Stones come from the daily dungeons (DUNGEON_STONES a run) and the tower
        // (TOWER_STONES a floor, TOWER_STONES_RARE more on every TOWER_RARE_EVERY floor).
        public int LIMIT_STEP = 10;
        public int LIMIT_STONE_BASE = 10;
        public double UPG_FARM_EXPONENT = 1.31;
        public double ENEMY_HP_BASE = 45;
        // D-123 / D-124: 1.185 -> 1.165 (HP) and 1.175 (ATK); the summon ladder now paces gear. D-145: 1.21 for both
        // slows the stage curve (day 7 in chapter 4, day 14 in chapter 5-6) after the new growth axes (AP, crit lanes,
        // limit breaks, daily dungeons in the sim). ATK 1.22 let a boss's 0.1x attack kill a fast (ad) hero.
        public double ENEMY_HP_GROWTH = 1.21;
        public double ENEMY_ATK_BASE = 2.6;
        // D-151: 1.21 -> 1.17. The hero's HP grows about x1.13 a stage (the owned effects raise ATK only), so at 1.21 the
        // hero fell from 15 enemy hits of HP at 2-10 to under one by 7-10: from week two bosses killed the hero (D-139 wants
        // time-outs) and a third of normal attempts ended in death.
        public double ENEMY_ATK_GROWTH = 1.17;
        // D-151 late curve: past ENEMY_GROWTH_LATE_START (stage 7-10) enemies grow by the _LATE rates instead. At 1.21 for
        // ever a stage costs x1.117 more play than the last (x3 a chapter) while income grows x1.035, so a 30-minute player
        // stalled for months at a chapter boss from month four on. The first month (chapters 1-7) keeps its curve. Attack
        // grows slower than HP (the hero's HP grows about x1.07 a stage late), so bosses stay a wall of HP.
        // D-167: HP 1.125 -> 1.145 once the P1 axes (third job, job talents, summon level 20, stone levels, the score
        // dungeon, the codex) were in - at 1.125 a light player reached chapter 26 at six months (target 16-20).
        public int ENEMY_GROWTH_LATE_START = 70;
        public double ENEMY_HP_GROWTH_LATE = 1.145;
        public double ENEMY_ATK_GROWTH_LATE = 1.07;
        public double ENEMY_DEF = 0;
        public double ENEMY_EXP_BASE = 3.75;
        public double ENEMY_EXP_GROWTH = 1.10;
        public float ENEMY_ATK_INTERVAL = 1.2f;
        public int SPAWN_WAVE_SIZE = 8;
        public float SPAWN_WAVE_GAP = 0.3f;
        public double SPAWN_WAVE_SPACING = 0.4;
        public int SPAWN_MAX_ALIVE = 8;
        public double SPAWN_OFFSET_X = 0.8;
        public int KILL_TARGET_NORMAL = 16;
        // D-110 enemies walk in: the nearest ENEMY_FRONT_SLOTS stop ENEMY_STAND_MIN + k x ENEMY_STAND_STEP from the hero
        // and fight, the rest queue ENEMY_QUEUE_SPACING apart behind them and step up as the front falls.
        public double ENEMY_MOVE_SPEED = 1.6;
        public int ENEMY_FRONT_SLOTS = 3;
        public double ENEMY_STAND_MIN = 1.0;
        public double ENEMY_STAND_STEP = 0.2;
        public double ENEMY_QUEUE_SPACING = 0.45;
        public double BOSS_STAND = 1.2;
        // D-110 roles: HP / ATK / speed multipliers of the plain soldier; ranged ones stop and shoot from afar.
        public double ENEMY_FAST_HP_MULT = 0.6;
        public double ENEMY_FAST_ATK_MULT = 0.6;
        public double ENEMY_FAST_SPEED_MULT = 1.8;
        public double ENEMY_TANK_HP_MULT = 2.4;
        public double ENEMY_TANK_ATK_MULT = 0.9;
        public double ENEMY_TANK_SPEED_MULT = 0.65;
        public double ENEMY_RANGED_HP_MULT = 0.7;
        public double ENEMY_RANGED_ATK_MULT = 0.6;
        public double ENEMY_RANGED_SPEED_MULT = 0.9;
        public double ENEMY_RANGED_STAND = 3.6;
        public double ENEMY_RANGED_RANGE = 4.2;
        public float ENEMY_RANGED_INTERVAL = 1.6f;
        public double ENEMY_PROJECTILE_SPEED = 9;
        // D-124: a boss is a wall of HP against the 30 s clock, not a hard hitter (user: the boss should not kill the
        // hero in a few hits). HP 10 -> 26 (chapter 1: 6 -> 14), attack 2.5 -> 0.5 of a normal enemy's.
        // D-139: still 9% of boss fails were the hero falling (up to half on some paths); attack 0.5 -> 0.1 makes every
        // fail a time-out, HP 26 -> 27 keeps the day-7 curve.
        public double BOSS_HP_MULT = 27;

        /// <summary>D-110: the chapter 1 boss (the first wall) is softer, so a new player clears it in a try or two.</summary>
        public double BOSS_HP_MULT_CH1 = 14;

        /// <summary>D-110: the boss walks in at this share of ENEMY_MOVE_SPEED.</summary>
        public double BOSS_SPEED_MULT = 0.8;
        public double BOSS_ATK_MULT = 0.1;
        public float BOSS_ATK_INTERVAL = 1.8f;
        public double BOSS_GOLD_MULT = 5.0;
        public double BOSS_EXP_MULT = 5.0;
        public int BOSS_TIME_LIMIT = 30;
        public float BOSS_INTRO_TIME = 1.5f;
        public double STAGE_GOLD_BASE = 50;
        // D-122: 1.04 -> 1.035 so gold alone climbs a little slower (day one nearly the same, later chapters slower).
        public double STAGE_GOLD_GROWTH = 1.035;
        public int CHAPTER_CLEAR_GEM = 60;

        // D-153 (L-02 / L-03) difficulty tiers: every TIER_CHAPTERS chapters is one tier (normal, hard, hell, nightmare,
        // abyss, abyss II, ...). Stats still follow g; a tier only names and colours its stages. The first clear of a tier's
        // last boss opens the next tier and pays TIER_CLEAR_GEM gems and summon tickets.
        public int TIER_CHAPTERS = 10;
        public int TIER_CLEAR_GEM = 500;
        public int TIER_CLEAR_GEAR_TICKETS = 10;
        public int TIER_CLEAR_SKILL_TICKETS = 2;
        public int TIER_CLEAR_PET_TICKETS = 2;

        // D-154 (L-08) level rewards (genre "growth reward"): every LEVEL_REWARD_EVERY levels pays gems, gear tickets and
        // breakthrough stones the moment the level is reached; every LEVEL_REWARD_BIG_EVERY levels adds more gems and
        // LEVEL_REWARD_BIG_TICKETS skill and pet tickets each.
        public int LEVEL_REWARD_EVERY = 10;
        public int LEVEL_REWARD_GEM = 50;
        public int LEVEL_REWARD_GEAR_TICKETS = 3;
        public int LEVEL_REWARD_STONES = 10;
        public int LEVEL_REWARD_BIG_EVERY = 50;
        public int LEVEL_REWARD_BIG_GEM = 200;
        public int LEVEL_REWARD_BIG_TICKETS = 3;
        public int OFFLINE_CAP = 21600;
        // D-110: 2000; D-145: 1800, so offline gold stays 20 % or more of all gold now that the daily dungeons pay too.
        public double OFFLINE_DIVISOR = 1800;
        public int OFFLINE_MIN_SECONDS = 60;
        public double OFFLINE_AD_MULT = 2.0;
        public double GACHA_COST_SINGLE = 150;
        public double GACHA_COST_TEN = 1350;
        public int GACHA_COST_TEN_GEM = 200;
        // D-172: the bulk pull (genre x100 button) costs the ten-pull price per ten (no extra discount), and one tap of the
        // ticket button spends up to SUMMON_TICKET_MAX tickets.
        public int SUMMON_BULK_PULLS = 100;
        public int SUMMON_TICKET_MAX = 100;
        // D-173 (L-19): a pickup banner's target takes PICKUP_SHARE % of its grade's draws (grade rates unchanged); 70, not
        // 50: the Legendary pets are a pair, where half would be no pickup at all.
        public double PICKUP_SHARE = 70;
        public int GEM_GOLD_PACK_COST = 50;
        public double GEM_GOLD_PACK_STAGES = 100;
        // D-181 store (IAP): gem bundles (the first purchase of each pays IAP_FIRST_BONUS_MULT times), a gold bundle sized
        // like the gem gold pack (stage clears of the farming stage) and the VIP pack: VIP_DAYS of permanent battle speed
        // (AD_SPEED_MULT, no ad), attendance / daily mission rewards x VIP_DAILY_MULT, an offline cap of VIP_OFFLINE_CAP
        // seconds and rewarded ads granted without watching. IAP_GRANT_HISTORY transaction ids are kept so a replayed
        // receipt never grants twice.
        public int IAP_GEM_SMALL = 300;
        public int IAP_GEM_MEDIUM = 1000;
        public int IAP_GEM_LARGE = 3300;
        public double IAP_FIRST_BONUS_MULT = 2;
        public double IAP_GOLD_PACK_STAGES = 400;
        public int VIP_DAYS = 30;
        public double VIP_DAILY_MULT = 2;
        public int VIP_OFFLINE_CAP = 43200;
        public int IAP_GRANT_HISTORY = 20;
        public double GACHA_RATE_C = 55;
        public double GACHA_RATE_R = 33;
        public double GACHA_RATE_E = 10;
        public double GACHA_RATE_L = 2;
        public int GACHA_PITY = 100;
        public bool GACHA_PITY_RESET_ON_LEGENDARY = true;

        // D-113 gear gacha: seven grades with very rare top tiers (skill summons keep GACHA_RATE_* and GACHA_PITY).
        // Percent per pull; GEAR_PITY pulls without a Legendary or better guarantee a Legendary.
        public double GEAR_RATE_C = 60;
        public double GEAR_RATE_U = 28;
        public double GEAR_RATE_R = 9;
        public double GEAR_RATE_E = 2.5;
        public double GEAR_RATE_L = 0.45;
        public double GEAR_RATE_M = 0.045;
        public double GEAR_RATE_A = 0.005;
        public int GEAR_PITY = 200;

        // D-115 summon level: each summon (gear, skill, pet) levels up with its own cumulative pulls - level L needs
        // STEP x (L - 1) x L / 2 pulls - and every level multiplies the good grades (gear and pets: Rare and up;
        // skills: Epic and up) by 1 + SUMMON_LV_RATE_GAIN x (L - 1); the lowest grade gives up the difference.
        // D-159 (L-09): 20 levels. Past SUMMON_LV_LATE_FROM each level takes SUMMON_LV_LATE_STEP_MULT x the pulls, the
        // good grades keep that level's boost and only the top grades (gear and pets: Mythic, Ancient) rise, by
        // SUMMON_LV_TOP_GAIN per level - the refund rule below still holds at level 20. Each of those levels pays
        // SUMMON_LV_REWARD_* tickets of its own summon.
        public int SUMMON_LV_MAX = 20;
        public int SUMMON_LV_LATE_FROM = 10;
        public int SUMMON_LV_LATE_STEP_MULT = 2;
        public double SUMMON_LV_TOP_GAIN = 0.06;
        public int SUMMON_LV_REWARD_GEAR = 100;
        public int SUMMON_LV_REWARD_SKILL = 10;
        public int SUMMON_LV_REWARD_PET = 30;
        // D-123: hundreds of pulls per level (gear 100, 300, 600, 1000, ...), MapleStory Idle style.
        public int SUMMON_LV_STEP_GEAR = 100;
        public int SUMMON_LV_STEP_SKILL = 20;
        public int SUMMON_LV_STEP_PET = 50;
        public double SUMMON_LV_RATE_GAIN = 0.12;

        // D-121 / D-123 grade gates (MapleStory Idle: the summon level opens the next grade): the summon level from which
        // a grade can drop. Below it the grade's rate is 0 and Common takes it. Level 1 gives Common only and every level
        // opens the next grade (gear and pets: Uncommon 2 .. Ancient 7; skills: Rare 2, Epic 3, Legendary 4). The pity
        // only counts once its grade (Legendary) is open.
        public int SUMMON_OPEN_U = 2;
        public int SUMMON_OPEN_R = 3;
        public int SUMMON_OPEN_E = 4;
        public int SUMMON_OPEN_L = 5;
        public int SUMMON_OPEN_M = 6;
        public int SUMMON_OPEN_A = 7;
        public int SKILL_SUMMON_OPEN_R = 2;
        public int SKILL_SUMMON_OPEN_E = 3;
        public int SKILL_SUMMON_OPEN_L = 4;

        public int TUTORIAL_FREE_PULLS = 10;
        public double TUTORIAL_GOLD = 1000;
        public int TUTORIAL_REWARD_STAGE = 1;
        // Gold for a duplicate of a max-level item. D-115: even at the top summon level a maxed collection returns
        // under half of the ten-pull price per pull, so pulling never pays for itself.
        public double REFUND_C = 20;
        public double REFUND_U = 40;
        public double REFUND_R = 80;
        public double REFUND_E = 250;
        public double REFUND_L = 1000;
        public double REFUND_M = 4000;
        public double REFUND_A = 15000;
        public double EQUIP_ENHANCE_GAIN = 0.1;
        public int EQUIP_MAX_LEVEL = 10;

        // D-160 (L-09) stone enhancement: gear of EQUIP_STONE_MIN_GRADE (Epic, the grades promotion never touches) and up
        // at EQUIP_MAX_LEVEL climbs on to EQUIP_STONE_MAX_LEVEL with break stones; the step to +L costs
        // EQUIP_STONE_COST x (L - EQUIP_MAX_LEVEL). Duplicates still stop at EQUIP_MAX_LEVEL. D-167: cost 10 -> 30, so with
        // the score dungeon's stones the levels spread over the year instead of landing in months two to four.
        public int EQUIP_STONE_MAX_LEVEL = 20;
        public int EQUIP_STONE_MIN_GRADE = 3;
        public int EQUIP_STONE_COST = 30;

        // D-116 equipment promotion (genre "promotion"): an item at EQUIP_MAX_LEVEL goes back to +0 and gives the same slot's
        // next grade at +EQUIP_PROMOTE_ENHANCE (an owned copy gains that many levels, capped), up to EQUIP_PROMOTE_MAX_GRADE
        // (3 = Epic; Legendary and above only come from summons). Cost EQUIP_PROMOTE_COST x EQUIP_PROMOTE_COST_GROWTH^grade.
        public int EQUIP_PROMOTE_MAX_GRADE = 3;
        public int EQUIP_PROMOTE_ENHANCE = 5;
        public double EQUIP_PROMOTE_COST = 500;
        public double EQUIP_PROMOTE_COST_GROWTH = 3;
        public int AD_OFFLINE_DAILY = 5;
        public int AD_GEM_DAILY = 5;
        public double AD_GEM_REWARD = 5;
        public int AD_BOOSTER_DAILY = 1;
        public double AD_BOOSTER_SECONDS = 600;
        public double AD_BOOSTER_GOLD_MULT = 2.0;

        // D-120 ad summons (genre "watch an ad, summon free"): free ten-pulls a day; the caller makes the pulls.
        public int AD_FREE_GEAR_DAILY = 2;
        public int AD_FREE_PET_DAILY = 1;
        public int AD_FREE_SUMMON_PULLS = 10;

        // D-129 ad speed booster (A-6, genre "2x speed"): the battle runs AD_SPEED_MULT times faster for AD_SPEED_SECONDS of
        // real time, AD_SPEED_DAILY times a day (once: three a day put ad players two to three chapters ahead by day 7).
        public int AD_SPEED_DAILY = 1;
        public double AD_SPEED_SECONDS = 1800;
        public float AD_SPEED_MULT = 2f;

        // D-127 recommended combat power (calibrated on the sim's first clears): REC_CP_BASE x REC_CP_GROWTH^(g - 1),
        // bosses x REC_CP_BOSS_MULT. Shown next to the hero's combat power, green once met. D-145: refit (median first
        // clear) after the faster enemy growth: 150 x 1.15 -> 185 x 1.172. D-151: refit on 365-day runs in two pieces
        // like the enemies: 135 x 1.177 up to ENEMY_GROWTH_LATE_START, then x REC_CP_GROWTH_LATE a stage, bosses x1.2
        // (185 x 1.172 fitted the first week only). D-167: late 1.115 -> 1.127 with the steeper late curve.
        public double REC_CP_BASE = 135;
        public double REC_CP_GROWTH = 1.177;
        public double REC_CP_GROWTH_LATE = 1.127;
        public double REC_CP_BOSS_MULT = 1.2;

        // D-128 summon tickets and the daily shop: one ticket is one pull of its summon. Every day one free gear bundle
        // and one discounted bundle per summon (gems); the gold package moved here from the summon page.
        public int SHOP_FREE_GEAR_TICKETS = 5;
        public int SHOP_DEAL_TICKETS = 10;
        public int SHOP_DEAL_GEAR_GEM = 150;
        public int SHOP_DEAL_SKILL_GEM = 180;
        public int SHOP_DEAL_PET_GEM = 180;

        // D-130 infinite tower: floor f fights the boss of stage TOWER_G_OFFSET + f against the boss timer; winning heals
        // and climbs on, losing returns to the stage. Every floor pays gems, every TOWER_TICKET_EVERY floors gear
        // tickets, every TOWER_RARE_EVERY floors skill and pet tickets.
        public int TOWER_UNLOCK_STAGE = 10;
        public int TOWER_G_OFFSET = 4;
        public double TOWER_GEM_BASE = 3;
        public double TOWER_GEM_PER_FLOOR = 0.1;
        public int TOWER_TICKET_EVERY = 5;
        public int TOWER_GEAR_TICKETS = 5;
        public int TOWER_RARE_EVERY = 10;
        public int TOWER_SKILL_TICKETS = 2;
        public int TOWER_PET_TICKETS = 2;
        public float TOWER_FLOOR_INTRO = 0.8f;
        // D-144: a tower floor's boss has TOWER_BOSS_HP_MULT x the HP of the chapter boss at its stage, so the tower is
        // a harder ladder than the stages instead of the same wall again. D-143 stones per floor.
        public double TOWER_BOSS_HP_MULT = 2;
        public int TOWER_STONES = 1;
        public int TOWER_STONES_RARE = 5;

        // D-162 (L-15) tower milestones: every TOWER_MILESTONE_EVERY floors pays TOWER_MILESTONE_GEM x (floor /
        // TOWER_MILESTONE_EVERY) gems and the tickets and stones below on top of the floor's own reward.
        public int TOWER_MILESTONE_EVERY = 50;
        public double TOWER_MILESTONE_GEM = 500;
        public int TOWER_MILESTONE_GEAR_TICKETS = 20;
        public int TOWER_MILESTONE_SKILL_TICKETS = 5;
        public int TOWER_MILESTONE_PET_TICKETS = 5;
        public int TOWER_MILESTONE_STONES = 50;
        // Gear effects per grade, C U R E L M A (D-113 added U, M and A).
        public double SWORD_ATK_C = 1.10;
        public double SWORD_ATK_U = 1.18;
        public double SWORD_ATK_R = 1.30;
        public double SWORD_ATK_E = 1.70;
        public double SWORD_ATK_L = 2.40;
        public double SWORD_ATK_M = 3.60;
        public double SWORD_ATK_A = 5.50;
        public double HELM_DEF_C = 1.10;
        public double HELM_DEF_U = 1.17;
        public double HELM_DEF_R = 1.25;
        public double HELM_DEF_E = 1.50;
        public double HELM_DEF_L = 2.00;
        public double HELM_DEF_M = 2.80;
        public double HELM_DEF_A = 4.00;
        public double ARMOR_HP_C = 1.10;
        public double ARMOR_HP_U = 1.17;
        public double ARMOR_HP_R = 1.25;
        public double ARMOR_HP_E = 1.50;
        public double ARMOR_HP_L = 2.00;
        public double ARMOR_HP_M = 2.80;
        public double ARMOR_HP_A = 4.00;
        public double BOOTS_ATKSPD_C = 0.03;
        public double BOOTS_ATKSPD_U = 0.05;
        public double BOOTS_ATKSPD_R = 0.08;
        public double BOOTS_ATKSPD_E = 0.15;
        public double BOOTS_ATKSPD_L = 0.25;
        public double BOOTS_ATKSPD_M = 0.35;
        public double BOOTS_ATKSPD_A = 0.50;
        public double BOOTS_CRIT_C = 1;
        public double BOOTS_CRIT_U = 2;
        public double BOOTS_CRIT_R = 3;
        public double BOOTS_CRIT_E = 6;
        public double BOOTS_CRIT_L = 10;
        public double BOOTS_CRIT_M = 14;
        public double BOOTS_CRIT_A = 20;
        // D-109 accessories: gloves ATK x, necklace HP x, ring crit damage + (fraction of a hit), earring skill damage + (fraction).
        public double GLOVES_ATK_C = 1.05;
        public double GLOVES_ATK_U = 1.08;
        public double GLOVES_ATK_R = 1.12;
        public double GLOVES_ATK_E = 1.25;
        public double GLOVES_ATK_L = 1.50;
        public double GLOVES_ATK_M = 1.90;
        public double GLOVES_ATK_A = 2.50;
        public double NECKLACE_HP_C = 1.05;
        public double NECKLACE_HP_U = 1.08;
        public double NECKLACE_HP_R = 1.12;
        public double NECKLACE_HP_E = 1.25;
        public double NECKLACE_HP_L = 1.50;
        public double NECKLACE_HP_M = 1.90;
        public double NECKLACE_HP_A = 2.50;
        public double RING_CRITDMG_C = 0.10;
        public double RING_CRITDMG_U = 0.17;
        public double RING_CRITDMG_R = 0.25;
        public double RING_CRITDMG_E = 0.50;
        public double RING_CRITDMG_L = 1.00;
        public double RING_CRITDMG_M = 1.50;
        public double RING_CRITDMG_A = 2.20;
        public double EARRING_SKILL_C = 0.05;
        public double EARRING_SKILL_U = 0.08;
        public double EARRING_SKILL_R = 0.12;
        public double EARRING_SKILL_E = 0.25;
        public double EARRING_SKILL_L = 0.50;
        public double EARRING_SKILL_M = 0.75;
        public double EARRING_SKILL_A = 1.10;

        // D-109 equipment owned bonus: every owned item adds ATK +x% (x enhance), equipped or not.
        public double EQUIP_OWNED_ATK_C = 0.5;
        public double EQUIP_OWNED_ATK_U = 0.75;
        public double EQUIP_OWNED_ATK_R = 1;
        public double EQUIP_OWNED_ATK_E = 2;
        public double EQUIP_OWNED_ATK_L = 4;
        public double EQUIP_OWNED_ATK_M = 8;
        public double EQUIP_OWNED_ATK_A = 15;
        public int SKILL_SLOT_COUNT = 6;
        public int SKILL_UNLOCK_LV_1 = 1;
        public int SKILL_UNLOCK_LV_2 = 5;
        public int SKILL_UNLOCK_LV_3 = 12;
        public int SKILL_UNLOCK_LV_4 = 20;
        public int SKILL_UNLOCK_LV_5 = 30;
        public int SKILL_UNLOCK_LV_6 = 45;
        public int SKILL_MAX_LEVEL = 10;
        public double SKILL_LEVEL_GAIN = 10;
        public double SKILL_UPG_BASE_C = 900;
        public double SKILL_UPG_BASE_R = 1500;
        public double SKILL_UPG_BASE_E = 2400;
        public double SKILL_UPG_BASE_L = 4000;
        public double SKILL_UPG_COST_GROWTH = 1.15;
        public float SKILL_SEQUENCE_GAP = 0.4f;
        public double SKILL_SUMMON_COST_SINGLE = 5000;
        public double SKILL_SUMMON_COST_TEN = 45000;
        public int SKILL_SUMMON_COST_TEN_GEM = 200;
        public double SKILL_REFUND_C = 500;
        public double SKILL_REFUND_R = 1500;
        public double SKILL_REFUND_E = 4000;
        public double SKILL_REFUND_L = 15000;
        public double SKILL_OWNED_ATK_C = 0.25;
        public double SKILL_OWNED_ATK_R = 0.5;
        public double SKILL_OWNED_ATK_E = 1;
        public double SKILL_OWNED_ATK_L = 2;
        public double SKILL_BOSS_STUN_MULT = 0.5;

        /// <summary>D-098 combo: a skill hit on a stunned / frozen enemy deals this much (shatter).</summary>
        public double SKILL_SHATTER_MULT = 1.5;

        /// <summary>D-098 combo: a skill hit on a burning / poisoned enemy deals this much (ignite).</summary>
        public double SKILL_IGNITE_MULT = 1.3;

        /// <summary>D-099 attendance: gems on odd cycle days, clear gold x stages on even days, the day-7 gems.</summary>
        public double ATTENDANCE_GEM = 30;
        public double ATTENDANCE_GOLD_STAGES = 20;
        public double ATTENDANCE_DAY7_GEM = 150;
        /// <summary>D-166: the 28-day board's last day (days 7, 14 and 21 pay ATTENDANCE_DAY7_GEM).</summary>
        public double ATTENDANCE_DAY28_GEM = 300;

        // D-163 (L-17) free season pass: PASS_SEASON_DAYS-day seasons from PASS_EPOCH_UTC (2026-10-12 00:00 UTC, a Monday)
        // on the trusted clock; PASS_XP_PER_LEVEL XP a level; XP from claimed daily / weekly missions and one per
        // PASS_KILLS_PER_XP kills. D-170: set the epoch to the launch week's Monday so the first public season is 1.
        public long PASS_EPOCH_UTC = 1791763200;
        public int PASS_SEASON_DAYS = 28;
        public int PASS_XP_PER_LEVEL = 200;
        public int PASS_XP_DAILY = 20;
        public int PASS_XP_WEEKLY = 100;
        public int PASS_KILLS_PER_XP = 50;

        /// <summary>D-100 daily dungeons: run length, entries per dungeon per local day, rewards, result pause.</summary>
        public float DUNGEON_TIME = 30f;
        public int DUNGEON_DAILY_TICKETS = 2;
        public double DUNGEON_GOLD_PER_KILL = 0.5;
        public double DUNGEON_EXP_MULT = 6;
        public int DUNGEON_STONES = 2;

        /// <summary>D-143: the daily dungeons open with the first boss, like the tower and the pet summon.</summary>
        public int DUNGEON_UNLOCK_STAGE = 10;

        // D-165 (L-13) score dungeon (Soul Strike's training ground): SCORE_DUNGEON_TIME seconds against a harmless
        // training boss - the farming stage's chapter boss with SCORE_HP_MULT x its HP - SCORE_DAILY_TICKETS times a day.
        // The damage in that chapter boss's HP sets the tier: tier 1 from SCORE_TIER_BASE, one more each time it doubles
        // (capped at SCORE_TIER_MAX). Tier k pays SCORE_STONES_BASE + SCORE_STONES_PER_TIER x (k - 1) stones, tier 0
        // DUNGEON_STONES.
        public float SCORE_DUNGEON_TIME = 30f;
        public int SCORE_DAILY_TICKETS = 2;
        public double SCORE_HP_MULT = 1000;
        public double SCORE_TIER_BASE = 0.125;
        public int SCORE_TIER_MAX = 20;
        public int SCORE_STONES_BASE = 4;
        public int SCORE_STONES_PER_TIER = 3;
        // D-174 (L-13) material dungeon: DUNGEON_TIME seconds of waves at the farming stage; every MATERIAL_KILLS_PER_STONE
        // kills pay one awakening stone at once. MATERIAL_DAILY_TICKETS entries a day, open once stage MATERIAL_UNLOCK_STAGE
        // is cleared; sweeps pay the best run's kills.
        public int MATERIAL_DAILY_TICKETS = 2;
        public int MATERIAL_KILLS_PER_STONE = 2;
        public int MATERIAL_UNLOCK_STAGE = 30;
        // D-174 (L-09) skill awakening: a max-level skill climbs to AWAKEN_MAX_STARS stars. Star s+1 costs
        // AWAKEN_COST_BASE x (s+1) x AWAKEN_GRADE_MULT^grade awakening stones; each star adds AWAKEN_SKILL_GAIN % to the
        // skill's values (on top of its level scale) and AWAKEN_OWNED_GAIN % to its owned effect (10, not 20: the 365-day sim
        // put day 30 / 90 on the top of their bands at 20).
        public int AWAKEN_MAX_STARS = 5;
        public double AWAKEN_COST_BASE = 10;
        public double AWAKEN_GRADE_MULT = 2;
        public double AWAKEN_SKILL_GAIN = 20;
        public double AWAKEN_OWNED_GAIN = 10;
        // D-177 pet awakening (genre): a max-level pet climbs to PET_AWAKEN_MAX_STARS stars for awakening stones (the skills'
        // material). Star s+1 costs PET_AWAKEN_COST_BASE x (s+1) x PET_AWAKEN_GRADE_MULT^grade (seven grades, so a gentler
        // multiplier than the skills' 2: an Ancient pet's five stars come to about 2,500 stones); each star adds
        // PET_AWAKEN_ATK_GAIN % to the pet's attack and PET_AWAKEN_OWNED_GAIN % to its owned effect.
        public int PET_AWAKEN_MAX_STARS = 5;
        public double PET_AWAKEN_COST_BASE = 10;
        public double PET_AWAKEN_GRADE_MULT = 1.6;
        public double PET_AWAKEN_ATK_GAIN = 20;
        public double PET_AWAKEN_OWNED_GAIN = 10;
        // D-175 (L-14) weekly raid: RAID_WEEKLY_TICKETS entries a week (Monday 0:00 local), open once stage RAID_UNLOCK_STAGE
        // is cleared. Level L (1..RAID_LEVELS; the next opens when L is beaten) fights the chapter boss of stage
        // RAID_BASE_STAGE + RAID_LEVEL_STEP x (L-1) with RAID_HP_MULT x its boss HP and RAID_ATK_MULT x that stage's enemy
        // attack, for RAID_TIME seconds; the hero falling ends the run. Every RAID_PATTERN_INTERVAL seconds the boss warns for
        // RAID_TELEGRAPH seconds, then slams (RAID_SLAM_MULT x its hit within RAID_SLAM_RANGE), charges (RAID_CHARGE_MULT x its
        // hit) or calls RAID_SUMMON_COUNT minions. Medals: RAID_MEDALS_PER_LEVEL x L x the share of its HP dealt, plus
        // RAID_CLEAR_MEDALS x L on a kill; a level's first kill pays RAID_FIRST_GEM x L gems.
        public int RAID_WEEKLY_TICKETS = 3;
        public int RAID_UNLOCK_STAGE = 40;
        public int RAID_LEVELS = 10;
        public int RAID_BASE_STAGE = 40;
        public int RAID_LEVEL_STEP = 20;
        public double RAID_HP_MULT = 3;
        public double RAID_ATK_MULT = 1.5;
        public float RAID_TIME = 60f;
        public float RAID_PATTERN_INTERVAL = 7f;
        public float RAID_TELEGRAPH = 1.4f;
        public double RAID_SLAM_MULT = 4;
        public double RAID_SLAM_RANGE = 3;
        public double RAID_CHARGE_MULT = 2.5;
        public int RAID_SUMMON_COUNT = 3;
        // D-175: a raid minion has RAID_MINION_HP_MULT x a normal enemy's HP of the stage, so the summon pulls the hero's attacks
        // off the boss for a while instead of dying in the same swing.
        public double RAID_MINION_HP_MULT = 20;
        public int RAID_MEDALS_PER_LEVEL = 6;
        public int RAID_CLEAR_MEDALS = 6;
        public int RAID_FIRST_GEM = 100;
        // D-175: the raid window's recommended power is the level's stage boss's x RAID_REC_MULT (3x HP, but twice the time).
        public double RAID_REC_MULT = 1.5;
        public float DUNGEON_RESULT_TIME = 2.5f;

        /// <summary>D-104 job advancement (replaces the D-101 promotion): hero level for the first and second job,
        /// HP / ATK / DEF multiplier per advancement. D-157 (L-05): the third job at JOB_LV_3.</summary>
        public int JOB_LV_1 = 10;
        public int JOB_LV_2 = 30;
        public int JOB_LV_3 = 60;
        public double JOB_STAT_MULT = 1.05;

        /// <summary>
        /// D-102 / D-114 pets: level cap, damage gain per level, level-up cost (base x grade mult^grade x growth^(level-1));
        /// the grade is the seven-step gear ladder.
        /// </summary>
        public int PET_MAX_LEVEL = 50;
        public double PET_LEVEL_GAIN = 1.1;
        public double PET_COST_BASE = 500;
        public double PET_COST_GRADE_MULT = 1.6;
        public double PET_COST_GROWTH = 1.15;

        // D-114 pet summon: the gear rate table and pity ceiling (GEAR_RATE_*, GEAR_PITY) on its own counter. It opens
        // once the first boss is beaten (highest cleared stage >= PET_UNLOCK_STAGE), so the first minutes stay on upgrades.
        public int PET_UNLOCK_STAGE = 10;
        public double PET_SUMMON_COST_SINGLE = 300;
        public double PET_SUMMON_COST_TEN = 2700;
        public int PET_SUMMON_COST_TEN_GEM = 200;

        // A duplicate enhances the pet (+1, attack and owned effect x(1 + PET_ENHANCE_GAIN) each) up to PET_MAX_ENHANCE,
        // then refunds PET_REFUND_{grade} gold. Every owned pet adds ATK +PET_OWNED_ATK_{grade}% x enhance
        // x (1 + PET_LEVEL_OWNED_GAIN x (level - 1)): gold levels raise the owned effect too (genre).
        public double PET_LEVEL_OWNED_GAIN = 0.1;
        public double PET_ENHANCE_GAIN = 0.1;
        public int PET_MAX_ENHANCE = 10;
        public double PET_REFUND_C = 30;
        public double PET_REFUND_U = 60;
        public double PET_REFUND_R = 120;
        public double PET_REFUND_E = 500;
        public double PET_REFUND_L = 2000;
        public double PET_REFUND_M = 8000;
        public double PET_REFUND_A = 30000;
        public double PET_OWNED_ATK_C = 2;
        public double PET_OWNED_ATK_U = 3;
        public double PET_OWNED_ATK_R = 5;
        public double PET_OWNED_ATK_E = 8;
        public double PET_OWNED_ATK_L = 15;
        public double PET_OWNED_ATK_M = 30;
        public double PET_OWNED_ATK_A = 60;
        public float SKILL_DOT_TICK = 0.5f;
        public int TALENT_POINTS_PER_LEVEL = 1;
        public int TALENT_TIER_STEP = 5;
        public int TALENT_RESET_GEM = 100;
        public int STAGES_PER_CHAPTER = 10;
        public int MVP_CHAPTERS = 5;
        public float STAGE_CLEAR_DELAY = 2f;
        public float STAGE_RETRY_DELAY = 3f;
        public float BOSS_FAIL_AUTO_RETREAT = 5f;
        public float DEATH_ANIM_TIME = 1f;
        public int FAIL_STREAK_STEP_DOWN = 1;
        public int PPU = 32;
        public int PIXEL_REF_WIDTH = 270;
        public int PIXEL_REF_HEIGHT = 480;
        public int BATTLE_VIEW_RATIO = 55;
        public int HERO_SCREEN_X = 30;
        public int SAVE_DEBOUNCE = 250;
    }
}
