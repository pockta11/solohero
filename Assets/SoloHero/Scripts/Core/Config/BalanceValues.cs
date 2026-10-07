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
        public double LEVEL_HP_GAIN = 5;
        public double LEVEL_ATK_GAIN = 0.5;
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
        public double UPG_FARM_EXPONENT = 1.31;
        public double ENEMY_HP_BASE = 45;
        // D-123 / D-124: 1.185 -> 1.165 (HP) and 1.175 (ATK); the summon ladder now paces gear, so enemies grow a
        // little slower to keep day 7 in chapter 5.
        public double ENEMY_HP_GROWTH = 1.165;
        public double ENEMY_ATK_BASE = 2.6;
        public double ENEMY_ATK_GROWTH = 1.175;
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
        public double BOSS_HP_MULT = 26;

        /// <summary>D-110: the chapter 1 boss (the first wall) is softer, so a new player clears it in a try or two.</summary>
        public double BOSS_HP_MULT_CH1 = 14;

        /// <summary>D-110: the boss walks in at this share of ENEMY_MOVE_SPEED.</summary>
        public double BOSS_SPEED_MULT = 0.8;
        public double BOSS_ATK_MULT = 0.5;
        public float BOSS_ATK_INTERVAL = 1.8f;
        public double BOSS_GOLD_MULT = 5.0;
        public double BOSS_EXP_MULT = 5.0;
        public int BOSS_TIME_LIMIT = 30;
        public float BOSS_INTRO_TIME = 1.5f;
        public double STAGE_GOLD_BASE = 50;
        // D-122: 1.04 -> 1.035 so gold alone climbs a little slower (day one nearly the same, later chapters slower).
        public double STAGE_GOLD_GROWTH = 1.035;
        public int CHAPTER_CLEAR_GEM = 60;
        public int OFFLINE_CAP = 21600;
        public double OFFLINE_DIVISOR = 2000;
        public int OFFLINE_MIN_SECONDS = 60;
        public double OFFLINE_AD_MULT = 2.0;
        public double GACHA_COST_SINGLE = 150;
        public double GACHA_COST_TEN = 1350;
        public int GACHA_COST_TEN_GEM = 200;
        public int GEM_GOLD_PACK_COST = 50;
        public double GEM_GOLD_PACK_STAGES = 100;
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
        public int SUMMON_LV_MAX = 10;
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

        /// <summary>D-100 daily dungeons: run length, entries per dungeon per local day, rewards, result pause.</summary>
        public float DUNGEON_TIME = 30f;
        public int DUNGEON_DAILY_TICKETS = 2;
        public double DUNGEON_GOLD_PER_KILL = 0.5;
        public double DUNGEON_EXP_MULT = 6;
        public float DUNGEON_RESULT_TIME = 2.5f;

        /// <summary>D-104 job advancement (replaces the D-101 promotion): hero level for the first and second job,
        /// HP / ATK / DEF multiplier per advancement.</summary>
        public int JOB_LV_1 = 10;
        public int JOB_LV_2 = 30;
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

        // D-117 rebirth: permanent boosts bought with Soul (RebirthService). Gold x(1 + GOLD_GAIN)^level, ATK
        // x(1 + ATK_GAIN)^level, offline gold +OFFLINE_GAIN x level; level n -> n + 1 costs COST_BASE + n Soul.
        public double REBIRTH_GOLD_GAIN = 0.1;
        public double REBIRTH_ATK_GAIN = 0.1;
        public double REBIRTH_OFFLINE_GAIN = 0.1;
        public int REBIRTH_COST_BASE = 1;
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
