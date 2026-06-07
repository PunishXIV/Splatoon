> [!Caution]
>
> Work in progress.
>
> It will be not until at least 2-3 weeks into the battle until all Phase 2 stuff is properly covered.
>
> Until then, you can browse and pick whatever you think will help you.
>
> Otherwise, please wait. 

## [Script] Forsaken Visualizer

This script just displays order of mechanics, your (or other players) markers, and visualizes attacks coming from players in towers. You can use it in conjunction with other scripts. This does not solves mechanic.

> [!Warning]
>
> It is required that you configure the script. 

```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/Dancing%20Mad/P2_Forsaken.cs
```

## **[Script] [Beta]** P2 Forsaken beta guide

Self-only helper for P2 Forsaken / Missing. It reads the live Missing debuffs, tracks the tower pair from map effects, determines the current first / second processing set, and guides only the local player to the configured tower-relative position.

Current model:
- Tower positions are inferred from map effects.
- Basic positions are tower-relative. The configured angle/distance table controls where each role stands relative to the current tower pair.
- Explicit Pair 1 through Pair 4 settings are preferred for pair-based strategies. If all four pairs match the current party, pairs containing one head-stack become the first set and non-head pairs become the second set.
- If explicit pairs are not configured, the script uses priority pairs: 1+3, 2+4, 5+7, and 6+8.
- The settings UI includes Pair validation / ペア設定チェック to warn about incomplete pairs, duplicate players, missing party members, or impossible current set assignment.
- Role names below use Splatoon role positions: ST is written as OT, and D1/D2/D3/D4 are written as M1/M2/R1/R2.

Script file in this repository:
```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/Dancing%20Mad/P2_Forsaken_beta.cs
```

## Current recommended configurations

### KT Yan-style explicit pairs - MT-H1/OT-H2/M1-R1/M2-R2 - AAABBBBA

Pair setup:
- Pair 1: MT-H1
- Pair 2: OT-H2
- Pair 3: M1-R1
- Pair 4: M2-R2

Wave table: AAABBBBA, meaning 1238 resolves waves 1, 2, 3, 8; 4567 resolves waves 4, 5, 6, 7.

Global priority: H1 H2 MT OT M1 M2 R1 R2.

Pair assignment: whole pairs. Pairs containing a head-stack player resolve waves 1/2/3/8; pairs without a head-stack player resolve waves 4/5/6/7.

Initial head-stack rank: Role side. With the priority above, the first head-stack tower is fixed to TH on the left tower and DPS on the right tower.

Source/reference:
- [Yan Flash / 絶妖星乱舞](https://yan-flash.com/ultimate/yosei-ranbu)
- [KT / ミッシング検討資料](https://docs.google.com/presentation/d/1RDLS_RW2VSgqPp8KbHKgWV6bMsIFq1tTnhQjYZ1Fx1E/edit?slide=id.g3e847f115ae_3_5#slide=id.g3e847f115ae_3_5)
- [Sora Haruno / KT source tweet](https://x.com/soraharuno_XIV/status/2062576334115873269)

```
{"TargetScriptName":"SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate@P2_Forsaken_beta","ConfigurationName":"KT Yan-style explicit pairs - MT-H1/OT-H2/M1-R1/M2-R2 - AAABBBBA","Configuration":"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","Overrides":null}
```

### KT Reen-style explicit pairs - MT-OT/H1-H2/M1-M2/R1-R2 - AAABBBBA

Pair setup:
- Pair 1: MT-OT
- Pair 2: H1-H2
- Pair 3: M1-M2
- Pair 4: R1-R2

Wave table: AAABBBBA, meaning 1238 resolves waves 1, 2, 3, 8; 4567 resolves waves 4, 5, 6, 7.

Global priority: MT OT H1 H2 M1 M2 R1 R2.

Source/reference:
- [Reen Kelly / KT source tweet](https://x.com/reen_kelly/status/2062438946399994034?s=46&t=23SvjfY0SHlA9udFwVXHxg)

```
{"TargetScriptName":"SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate@P2_Forsaken_beta","ConfigurationName":"KT Reen-style explicit pairs - MT-OT/H1-H2/M1-M2/R1-R2 - AAABBBBA","Configuration":"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","Overrides":null}
```

### Meow LZW / EU Pairstrat explicit pairs - H1-MT/OT-H2/R2-M2/M1-R1 - AAABBBBA

Pair setup:
- Pair 1: H1-MT
- Pair 2: OT-H2
- Pair 3: R2-M2
- Pair 4: M1-R1

Wave table: AAABBBBA, meaning 1238 resolves waves 1, 2, 3, 8; 4567 resolves waves 4, 5, 6, 7.

Global priority: H1 H2 MT OT M1 M2 R1 R2. This keeps helper ranks role-based for the RaidPlan note `Helpers to HTMR fixed positions`.

Source/reference:
- [Meow³'s Braindead P2 / Pairstrat Static + Uptime](https://raidplan.io/plan/lZWqxfxvyhF9sp3Z)

> [!Note]
>
> This preset uses explicit RaidPlan pairs and whole-pair assignment. Pairs containing a first-tower stack resolve first; the other two pairs help first. Wave 1 odd-set cone positions are kept on the left tower, and stack support is moved away from the far-left old default toward the H1 helper position shown in the RaidPlan.

```
{"TargetScriptName":"SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate@P2_Forsaken_beta","ConfigurationName":"Meow LZW / EU Pairstrat explicit pairs - H1-MT/OT-H2/R2-M2/M1-R1 - AAABBBBA","Configuration":"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","Overrides":null}
```

### Kroxy-Rinon / 1238-4567 pair
Wave table: Group A resolves waves 1, 2, 3, 8; Group B resolves waves 4, 5, 6, 7.

A/B sequence: AAABBBBA.

Grouping mode: Priority index pairs 1238/4567. Priority slots 1+3, 2+4, 5+7, and 6+8 are paired. Pairs containing one head-stack marker become Group A; non-stack pairs become Group B.

Initial head-stack rank: Priority order. This keeps the support-side stack left and DPS-side stack right for Kroxy-Rinon instead of sorting the two stack players by partner debuff.

Priority: H2 H1 OT MT M1 M2 R1 R2.

Source:
- [Kroxy-Rinon 3/4/1 (Center/N Stacks)](https://raidplan.io/plan/UATE__aDcw1-bgVv)

This preset follows the Rinon whole-pair model: Group A is the support stack plus that player's light-party buddy, and the DPS stack plus that player's light-party buddy. The priority order forms light-party buddy pairs as H2+OT, H1+MT, M1+R1, and M2+R2.

> [!Warning]
>
> This import includes Kroxy-Rinon coordinates copied from the Rinon script reference in `message.txt`. The Center-based helper/bait positions are converted to the generic script's reference-tower angle basis, and the even-wave `H0/C2/F2` table is aligned with the Meow LZW placement after replay/UI review. It still needs replay or live validation before treating it as fully proven.

```
{"TargetScriptName":"SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate@P2_Forsaken_beta","ConfigurationName":"Kroxy-Rinon / 1238-4567 pair","Configuration":"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","Overrides":null}
```


<details>
<summary>Legacy / older notes and configurations</summary>
### **[Script] [Beta]** P2 Forsaken beta  guide
Self-only helper for P2 Forsaken. It reads the live Missing debuffs, tracks the tower pair from map effects, determines the resolving group, and guides only the local player to the configured tower-relative position.

Default model:
- The tower position itself is inferred from map effects.
- Basic positions are tower-relative. The configured angle/distance table controls where each role stands relative to the current tower pair.
- The grouping mode controls how empty Group A/B settings are auto-captured from the first Missing forecast.
- The default grouping mode captures Group A as both head-stack players plus the highest-priority circle player and highest-priority fan player. The `1238/4567` pair mode instead pairs priority slots `1+3`, `2+4`, `5+7`, and `6+8`.
- The global priority controls auto group selection and same-debuff/support rank ordering.

Configuration notes:
- Leave Group A and Group B empty when the selected strategy defines Group A from the initial Missing forecast.
- Use fixed Group A/B if your group assigns four-player groups outside the initial debuff forecast.
- Use the Debug tab to verify the detected tower pair, auto groups, live debuff role, and selected position rule during replay review.

Script file in this repository:
```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/Dancing%20Mad/P2_Forsaken_beta.cs
```

#### AAABBBBA fixed-partner auto group
Wave table: Group A resolves waves `1, 2, 3, 8`; Group B resolves waves `4, 5, 6, 7`. This is the same resolver sequence as the current Toxic Friends / fixed-partner / Forsaken Buddies style RaidPlans; exact fixed partner positions can still differ by plan. Avoid treating it as region-specific because the same family is currently shared in both overseas Discord/RaidPlan contexts and NA Kefkabin-style materials.

Priority: `H2 M2 OT M1 R2 R1 MT H1`.

Related public plans:
- [Toxic Friends - UMAD P2](https://raidplan.io/plan/8wo9cYItAo2mkKSc#2)
- [Fixed Partners Modified Toxic Friends](https://raidplan.io/plan/aZjFjQN2CVs3LxIH)
- [Forsaken AAABBBBA a lot of fixed stuff](https://raidplan.io/plan/sGTZEzSxNJ790PTQ)
- [Forsaken Buddies](https://raidplan.io/plan/cKt22s4zMP50lDOV)
- [NA Kefkabin / UMAD P2: Forsaken](https://docs.google.com/presentation/d/1xzKhp29UPlZFICbZTtcFSJr_dQDCj93UKISG1_-SwhU/preview#slide=id.g3e5d74e9fd1_0_0)

```
{"TargetScriptName":"SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate@P2_Forsaken_beta","ConfigurationName":"AAABBBBA fixed-partner auto group","Configuration":"Gx82IqomswAeCji5ynIwYsM2mECqWBv82B29qvpCQZgn6UB2uzalic5wYPj9JlmcU2i7tCPEA/r/7rdULyrbSL/hnr/qg7RDaAlXdSKiVXDU2XayfvSEXkdoSuikG9tcFl3JZdkbXYaxvfn/v9+3cR7RVifECZU2dmnESX3ePufuOyIeEY16FRFr4u3Hn0mBSEmsToi0QksczplQndWhy4DV/IxBkJOieGMyEv+FV2kH9BiqyXJmd74UPyBFt27+j5mCfOMn2An93PwbY/m4SgwkR1n2uA2pyTdjkeRDYt5BnMEMA9+VJzwu+0Zm/fXBSSbGTLgsPDLA0WMp6xRFhyvELOhgSEqazgA9wdOnCt8R+zZfMMuSTp8kllViORUEB8hxHOM4GKbecbcmvSlgLtIPbfTykHFSWpUigTgAYm/Im9v9GnjFDMxZhCVeFIiimxQrQ1Hyp7z3TSprgztn6GZ6YD4nskj9cTuSC+DbabrTCe/tMXc9c3c+zoHxntx8Dkz0FCbh/ZrE+yK7nG8kxD+axBmpvhkpS4e3Cj4SYlfLuz1xAsbV/e90gNZ3vdnohMl+sVI0SFeRO6D3I/99Uz8711i6Nec1Y+wlzjIb4E5UT0LgdsbYdgjToKNET1hAJopqatxEnyTakLx6okb1Gp7h22A+HKMYb4O565m7w3EOgPfkhnMAoqcwCZ1JLoc0KsBY5YVOpezWhuSFkuRWYYKjfaTg436DgNZBQD5GPzIYXg3JGwKE8fEY2zOAyGxcYLYTrX+1F5uWtRlkE9vbpqGyWBsfy//e+A47I4h0zsaa/+o/w7L3v81jmpf62/n5jyIzDP2QGWcyOAKDvgdWVcVfBCJ2oMhO5vW/M8PQwn/3uKr7I9rdsuow/GdBB2twc/7pJLeD+Lr7NuZUYaiKRLZDjUM/TkXCIvFofLhPlLPMafnd8Cf0gMO2NJ2p1T8LSUcCE1b/LNkfsYmx1B5zMA6PmVXMu6tgK41iJp9B8ac2x5u9Reei2FKY/L3YPfoqPj2vg6nqj3ENc6Yx4GJ8J4+HGBVE7LG8rGucD8rNTPTzdKPRPiUIQwCUf1dnFXQN6tOISqCTLaWHmwrdWghAKyBxFyxxGRlqT8qeMExkZ2mq1am1fzKwa4KXgaz8BlpOsruYmbLqr3zZwGzwcvazeStYQAs4QH24DSCBH+BBoCZ8Hw8IOGydVgWY0N3nvLcd0OeL/qmqod6NzWEAKBDlEdiJ71Nul46H1iqttPEmjKuT8ympBwDjncNmNnM3kgfjz3nDlebisIE2DFQLacuWVMzvkpltG/jYuQBO+e+3nNsZfTaxudDOV29kDDpp2aiN6+cIDPAOXpjiq7IyRiCLqR4hQYTQujT/4a/mtB54AH/Ag7wTzUowT6nMo6Z5nEPfdEVy+Qkn4vI8fZR0KGnA+KOWKf4M+Qy+5zFjjNLJEwus/pGqmLh4gEFBokeCdgK9fbL7uevbxq1ewFJxWEHSUPTaSATQxxVc7sxQZqxsBjMb6M4YUqVQSoHqRXanIcxjBQ4q8q60sXDFAC6L2gpH9c/ng5iDJRUHAJhDcDC8QKvyRqpSqI8V3xl2j6OhpNhSmPy90GUx7B5HU0nKKIPAqb9PaUucnUr9uUmZYiBnt/e6juubUfFbWGrf50cBWbxAcIIYQUjVyIiDLpQREF9cpzwYAKfqHLxeAQmZuBSlDFTMwhuqvPBTqLpKtWpS0ErLMBpUaC2HhcKHP0CICB/UmcI24CExz5qCrwWqzuT1qnYudiUAru+ylXH8gx2AE2Mw21WOXsuLK+z4ebownQqlnZi4ZH1VejMpxsTRCiwcx1s9j0wO5AOq9JYILK82wUxJjAwpj6i4XPKkHCOdlRCHqLnCnxErgytCPjDZGBAtfef1Qqt3XeX6RPng2icJYJs/FxQXpbmXjHUUF1lFKN+lJlYqKfXVvEjpL1BLXlS0wXc0/1hYIAVy6Vfgm11SKk2P6NkhDXE5LVyO+y6JI7mfAeYK3Ff1d/VJxQMIIyKk8XO4zAV7RCXekm4IQSomhctxnz7odOBMT9T9sywPmJ6YD+//8J78MK2+xIAy/32vzcxOp/e+d/f34RCZTsF80EqJxE457Dd0pViPi9NknwER0ASQ56mrvHin5NF9uN/Dy/sfHvSfkRm6ZGKTiWz+Z44mgepks9XjOeUn1tGvXzQS/lM5/1n8qVftCjKY8gOLP4WgrbEToj4DCrV88KVB/5u9m+qCC14ydapETOG+jHlSOHLNOhI9Q8zxh4ROcCTeIMUp2etBjaXkPhbtEYp8on0wB5IWzKgKrATN4824FM2lS6MQWzTGrNAtsmArU9ltufoA","Overrides":null}
```

#### Pino / 1238-4567 pair
Wave table: Group A resolves waves 1, 2, 3, 8; Group B resolves waves 4, 5, 6, 7.

A/B sequence: AAABBBBA.

Grouping mode: Priority index pairs 1238/4567. Priority slots 1+3, 2+4, 5+7, and 6+8 are paired. Pairs containing one head-stack marker become Group A; non-stack pairs become Group B.

Priority: MT OT H1 H2 M1 M2 R1 R2.

This preset follows the reference script P2_Missing_1238_4567_Pair.cs: in 022, both cones use the left tower and both circles use the right tower.

```
{"TargetScriptName":"SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate@P2_Forsaken_beta","ConfigurationName":"Pino / 1238-4567 pair","Configuration":"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","Overrides":null}
```

#### EU Pairstrat / 1238-4567 pair
Wave table: Group A resolves waves 1, 2, 3, 8; Group B resolves waves 4, 5, 6, 7.

A/B sequence: AAABBBBA.

Grouping mode: Explicit pair whole-pair 1238/4567. Configured pairs are H1+MT, OT+H2, R2+M2, and M1+R1. Pairs containing one head-stack marker become Group A; non-stack pairs become Group B.

Priority: H1 H2 MT OT M1 M2 R1 R2.

Source:
- [Meow³'s Braindead P2 / Pairstrat Static + Uptime](https://raidplan.io/plan/lZWqxfxvyhF9sp3Z)

This preset follows the lZW RaidPlan pair structure and is no longer a mirror of Pino. It uses explicit whole-pair assignment with H1+MT, OT+H2, R2+M2, and M1+R1 pairs. Odd-set cone positions are kept on the left tower; even-set cone positions stay on top of both towers.

> [!Warning]
>
> This lZW import has strategy-specific pairs and coordinates. It should not be treated as interchangeable with Pino/KT imports without checking the group's chosen pair rule.

```
{"TargetScriptName":"SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate@P2_Forsaken_beta","ConfigurationName":"EU Pairstrat / 1238-4567 pair","Configuration":"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","Overrides":null}
```
#### Yan Flash / Poikos tank LB3 1-4/5-8
Wave table: Group A resolves waves `1, 2, 3, 4`; Group B resolves waves `5, 6, 7, 8`.

A/B sequence: `AAAABBBB`.

Priority: `H2 H1 MT OT M1 M2 R1 R2`.

Source:
- [Yan Flash / 絶妖星乱舞](https://yan-flash.com/ultimate/yosei-ranbu)

> [!Warning]
>
> Yan Flash describes this as Poikos tank LB3 handling. This preset intentionally leaves Group A and Group B empty. The script should auto-capture the first tower group from the initial forecast: both head-stack players plus the highest-priority circle and fan players. Do not fill Group A/B with TH/DPS or priority first-half/second-half groups unless your group uses a truly fixed-membership variant.

```
{"TargetScriptName":"SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate@P2_Forsaken_beta","ConfigurationName":"Yan Flash / Poikos tank LB3 1-4/5-8","Configuration":"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","Overrides":null}
```

#### Drippy regular 1458-2367 auto group
Wave table: Group A resolves waves `1, 4, 5, 8`; Group B resolves waves `2, 3, 6, 7`.

A/B sequence: `ABBAABBA`.

Priority: `MT OT H1 H2 R2 R1 M2 M1`.

Source:
- [Drippy / P2 ミッシング](https://drippy-sokuhou.com/articles/kefka-p2-missing)

This priority is chosen so the auto group picks the head players, the highest-priority TH circle/fan player, and the lowest-priority DPS circle/fan player, matching the article's described group split.

> [!Warning]
>
> Drippy describes same-mark left/right movement using the players' current positions. The current script expresses that with fixed priority ranks instead. If your group needs exact current-position sorting, the script logic needs a small extension.

```
{"TargetScriptName":"SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate@P2_Forsaken_beta","ConfigurationName":"Drippy regular 1458-2367 auto group","Configuration":"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","Overrides":null}
```
</details>

## **[Script] [Beta]** P2 Trine guide

Self-only helper for P2 Trine. It shows:
- half-room safe-side wait;
- first Trine dodge position;
- final tankbuster spread position.

The script solves the route from Trine telegraph objects and Trine action positions. The final tankbuster split uses the script priority:
- Priority 1 tank: near / MT position.
- Priority 2 tank: far / OT position.

Script file in this repository:
```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/Dancing%20Mad/P2_Trine_Beta.cs
```

# Individual scripts
> [!Important]
>
> These are scripts for individual mechanic resolutions. Their descriptions may be incomplete yet.
>
> There is a good chance that they will remain temporary until merging into a single script. 

## [JP] Old Yarn

P2_Missing_1238_4567 (called Old Yarn)
Resolve By Party Priority [0-3] / [4-7]

- Stack + Priority High → FirstHalf
- Remained 2 → SecondHalf

```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/Dancing%20Mad/P2_Forsaken_Pattern/P2_Missing_1234_5678.cs
```

## [JP] New Yarn

Resolve By Party Priority [0, 2] / [1, 3] / [4, 6] / [5, 7]

[0, 2] & [1.,3]

- Contain Stack Pair → FirstHalf
- Remained Pair→ SecondHalf

[4, 6] & [5, 7]

- Contain Stack Pair → FirstHalf
- Remained Pair→ SecondHalf

```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/Dancing%20Mad/P2_Forsaken_Pattern/P2_Missing_1238_4567_Pair.cs
```

## [JP] Missing Poikos Strat

Resolve By Party Priority [0-3] / [4-7]

- Stack + Priority High → FirstHalf
- Remained 2 → SecondHalf

```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/Dancing%20Mad/P2_Forsaken_Pattern/P2_Missing_1238_4567_Pair.cs
```

## [Maybe JP] Halfswap strat/Custom roles
```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/Dancing%20Mad/P2_Forsaken_Pattern/P2_Missing_1234_5678_CustomRoles.cs
```

## Missing 1238/4567 KT Strat

with pair-based half assignment and generic priority roles
```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/Dancing%20Mad/P2_Forsaken_Pattern/P2_Missing_1238_4567_KT_Strat.cs
```

## Drippy strat
```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/Dancing%20Mad/P2_Forsaken_Pattern/P2_Missing_1458_2367_Drippy.cs
```

## [NA/EU] Rinon
```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/Dancing%20Mad/P2_Forsaken_Pattern/P2_Missing_1238_4567_Rinon.cs
```

## Presets

### [International] Clones bait AOE and projection whitelist for All Things Ending

```
~Lv2~{"Name":"Clones bait","Group":"DMU","ZoneLockH":[1363],"ElementsL":[{"Name":"Kefka Casts","type":1,"radius":5.5,"Donut":0.1,"color":3370385663,"fillIntensity":0.4,"refActorNPCNameID":7131,"refActorRequireCast":true,"refActorCastId":[47826,47827],"refActorUseCastTime":true,"refActorCastTimeMin":3.0,"refActorCastTimeMax":8.0,"refActorComparisonType":6,"Conditional":true,"Nodraw":true},{"Name":"Aoe 1","type":1,"radius":5.0,"Donut":0.1,"color":3370385663,"fillIntensity":0.4,"refActorModelID":4967,"TargetAlteration":1100,"refActorComparisonType":1,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":100.0,"DistanceMax":9.0,"IsDead":false},{"Name":"Aoe 2","type":1,"radius":5.0,"Donut":0.1,"color":3370385663,"fillIntensity":0.4,"refActorModelID":4967,"TargetAlteration":1101,"refActorComparisonType":1,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":100.0,"DistanceMax":9.0,"IsDead":false},{"Name":"Aoe 3","type":1,"radius":5.0,"Donut":0.1,"color":3370385663,"fillIntensity":0.4,"refActorModelID":4967,"TargetAlteration":1102,"refActorComparisonType":1,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":100.0,"DistanceMax":9.0,"IsDead":false},{"Name":"Aoe 4","type":1,"radius":5.0,"Donut":0.1,"color":3370385663,"fillIntensity":0.4,"refActorModelID":4967,"TargetAlteration":1103,"refActorComparisonType":1,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":100.0,"DistanceMax":9.0}],"ForcedProjectorActions":[47836,47837]}
```
