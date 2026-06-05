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

Global priority: MT OT H1 H2 M1 M2 R1 R2.

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
{"TargetScriptName":"SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate@P2_Forsaken_beta","ConfigurationName":"AAABBBBA fixed-partner auto group","Configuration":"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","Overrides":null}
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
{"TargetScriptName":"SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate@P2_Forsaken_beta","ConfigurationName":"Yan Flash / Poikos tank LB3 1-4/5-8","Configuration":"G9c2Iqom8xBWBea57gGNhi0v6KH0hQZF5yMkmQURT9KD/VtHF73hwTDL2i7OKbRd2hHiAa+pOq1/pDSwA8qkzaUWoqDiAj59uk7DNrmbOhPRyDiK7nPTIjwEJ4dOiPj/v18/Q17EVv8hTqi0b5dG/Kkz++xz7hdMNOlVRKyJt4mTsRKJpEyItEJKHM0tpFHz2z3iiU4pU8QiMUxxLREPPf2GGHgM83EuLGPzgb4aHL06+Fj914yCwrZPaCb0a8mf5DSpQfQMRBx7eQY+DUQaQqu8IxW9zjGIM5idwHLDHh6ZnVXtOi8fSlsm7NbE1L/JTiZzDkXz08FKwME8ImY6DvQET2zwTbHn1ZvNTJY49yRpWCUNpyLf4HAxDIthMC/5u9269KaAuUjfs0nMDukmxZIU+RHaG/COHLndrwFWWoH5BmGZI90Y5ya5ap4Q+5H0vkxlrW8nZYLQ2ahDJoVojbeRPPzPp+lOJ7A3xrdX8u3Nj3F0YrBwn6OzBlsuEXxK4h3ay+arEe7X8iZEuVbaBse56vcMD/tC3rLHCRg397/TAVpfOLLRCZP9YqVokK4id0DvR/KHLf10VmP5ylxWi2FqtkprADpXOtUB3mZMrQcwDVpI9EQDyExOLcvV0IeJNhjPz0tdrGEXn8dy4hgCvAG+vZJvb3KMoxCDheMchTXYcgnOSK4EM9J+6uFoWzLZvR3JCyXJLYUGTqZggoyH9QFKB3HxLqQILXr3E1IjqZNNE6wN2kK/fAFnusX77EsYmb0SuTORbuQrrlltkw6SO10XbfEY2X8Gw0/prN94AFsdci9LmWSz2EEN0U8mWSv1NHsTtBo/yz8YsvMXkScwFmmk3Urzbr1xRuvfAvlvMXTXXqtLuBE+ZG08ywyX9AJK6zSrIUg1+1QRaVSK7LH8nUzMsEUkGGVtoHNYHbjMKbMLH3K8VxvN+JF4OshPygj+OxMfKGiyU9amsmUwrmSwXgk4WBXrzjWd5HYQX7leRUlLbq3RwnyC8kDQJM9axb8tHn605UrmbJre8T30gENXc9HkMhwrcVsGEJbhWGPuUzdA2r9igYDaNszrq2AritKOv4AkLasPL3cUnR2B5QCFpnp7/4EKzuDBWKr6o+PFKvXRI2RxU2CIAdIF0WqQcp3zQbmZCX01fhKREKvEHsCR4FW63AkyVLXe/FzKpDL/U+1Uh0EkIkEXIGkmHnpZL0xBdWrS3DImdet3GvY18AKQld9AS+p2LmYmM/07XzYwG7yc/WzeChbQAg5QH24DSOAHeBCoCd/HAwIOW6dVAabj1jnvbQf0qaJ/qmoodlOzF3ATyRrw6/D3Kbez74vUpI22uPHi0iR8ji2D/eLkwLT2iZtIgsX3eNUV5WqvATX2k3m0zZO7cvlFMrNtA5xJPwApf8/Lyb1T2sM1Z3skfbB7xfRsycHkJRwNbPUYR/iSLC1AwIo5kdVcCJ71Uf7DX81pPfAA/oAHhiyTkqsau7I8qdrveehdVySXn3DCrU/S+0kDMQM6SPUs40tIZpC9gBajG1EZi6EKUmOqSlmNmLeT6k3EjaaZ9lsTVcm23YaiYAtftS40ZtX4gFhtfnsEmwA1InbljhzgTAc2doMb3XSVWRdojMR4xWYZffDrxIUIs3KJfmcKWTmm9r16Y8haVt8B+1WwGcy7SBn0UqqSpw9kvqj4oI6jRGA5QKGp3hZQ8UEdV0myqADr0TpEvzVbnQP8E3kT6jNKjP5YPFu+HG7MOc60L/OjgCxYwEJRgMC5MiyyhQqX/qslJrkUvlz3m2DC5jkQgVGuhSj9K4vAxpo6vAgNXa0yUPKZqAydWAUBW29WWNhzeAjvBWWTAxfnYKMPeWh1+0T+ckqdhTzrvxdotu3O94wASHQd7SdzvYmIVOxYEzqOJh1Rskmz25dExPHsqBWuyOAwPuqJaWwgLyj5XamBa9qeVkYkRQaXj1VUwXliTpHuUoiB1VQrnJEqgypEIRBZS+Mss0R7ptW7rnJxwrx3zo0B2PYHXZtRmGWUteLiLKsI5bvIpErhkp/mBZf+AVlonKILsiP5z8ICEpANtbPdJafS9JA+A2mQC1GhitZeOHEozwFgqtXak/hGfEzxBEKHEEn8Hi5mAt+0FHdzeiAEyYgJVbR2/0VXfrMdW2q+y+yA7djK1ed/pEfrXdYT6Y/8j/fazOx0+vDH+F6EayHN0Vs9OsmQ1CmH+3ouFOXxcZTc0x8EkgAKPG1VEBtMAS3C/R9efP4lQbdk1TZABfVBy17tJU9jgHW22QKSnMKe1Anrl4zU+n0x+Um5cOa6xQhIX4orUi6cuRi0M2qC5DugT9N8L8yrYP2NUVtwxuvI1KniMIFFGfuYUOSb5VB0gJjiLwmV4FC8hoQS3utJjaKoMJojGGSi2Vb9X/Wm2TolEev1cFILdKFpVwO7xulSkYOmMiXjaluNAQ==","Overrides":null}
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
