import os

# Builds probe/BalanceSimulator.cs: a copy that prints the boss fight state when a boss attempt fails.
SRC = "D:/git/solohero/Assets/SoloHero/Scripts/Core/Balance/BalanceSimulator.cs"
DST_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "probe")
os.makedirs(DST_DIR, exist_ok=True)
s = open(SRC, encoding="utf-8", newline="").read()

anchor = "                bool isBoss = _runner.IsBoss;\r\n" if "\r\n" in s else "                bool isBoss = _runner.IsBoss;\n"
assert s.count(anchor) == 1, "anchor"
nl = "\r\n" if "\r\n" in s else "\n"
probe = (
    anchor
    + "                if (isBoss && _probeCount < 40)" + nl
    + "                {" + nl
    + "                    _probeCount++;" + nl
    + "                    double bossAtk = 0d, bossHp = 0d, bossMax = 0d;" + nl
    + "                    for (int i = 0; i < _runner.World.SlotCount; i++)" + nl
    + "                    {" + nl
    + "                        SoloHero.Core.Combat.EnemyBrain e = _runner.World.GetSlot(i);" + nl
    + "                        if (e == null || !e.IsBoss) continue;" + nl
    + "                        bossAtk = e.Atk; bossHp = e.Hp; bossMax = e.MaxHp;" + nl
    + "                    }" + nl
    + "                    double hit = Formulas.HitDamage(_b, bossAtk, _runner.Hero.Def);" + nl
    + "                    System.Console.WriteLine(\"PROBE d\" + _day.Day + \" g\" + g + \" dead=\" + (_runner.Hero.Hp <= 0d) + \" t=\" + (_play - _attemptStartPlay).ToString(\"F1\")"
    + " + \" heroMaxHp=\" + _runner.Hero.MaxHp.ToString(\"E2\") + \" def=\" + _runner.Hero.Def.ToString(\"E2\") + \" atk=\" + _runner.Hero.Stats.Atk.ToString(\"E2\")"
    + " + \" bossAtk=\" + bossAtk.ToString(\"E2\") + \" hit=\" + hit.ToString(\"E2\") + \" hitsToKill=\" + (_runner.Hero.MaxHp / System.Math.Max(1d, hit)).ToString(\"F1\")"
    + " + \" bossHpLeft=\" + (bossMax > 0 ? bossHp / bossMax : -1).ToString(\"F2\") + \" normalAtk=\" + Formulas.EnemyAtk(_b, g).ToString(\"E2\"));" + nl
    + "                }" + nl
)
s = s.replace(anchor, probe)
field_anchor = "            private readonly List<string> _capped = new List<string>();"
assert s.count(field_anchor) == 1, "field"
s = s.replace(field_anchor, field_anchor + nl + "            private int _probeCount;")
open(os.path.join(DST_DIR, "BalanceSimulator.cs"), "w", encoding="utf-8", newline="").write(s)
print("ok")
