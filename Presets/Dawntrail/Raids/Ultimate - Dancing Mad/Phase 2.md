## [Script] Forsaken Visualizer

This script just displays order of mechanics, your (or other players) markers, and visualizes attacks coming from players in towers. You can use it in conjunction with other scripts. This does not solves mechanic.

> [!Warning]
>
> It is required that you configure the script. 

```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/Dancing%20Mad/P2_Forsaken.cs
```

## **[Script] [Beta]** P2 Forsaken beta  guide
## [Script] [Beta] P2 Forsaken beta guide

Self-only helper for P2 Forsaken / Missing. It reads the live Missing debuffs, tracks the tower pair from map effects, determines the current 1238 / 4567 processing set, and guides only the local player to the configured tower-relative position.

Current model:
- Tower positions are inferred from map effects.
- Basic positions are tower-relative. The configured angle/distance table controls where each role stands relative to the current tower pair.
- Explicit Pair 1 through Pair 4 settings are preferred for pair-based strategies. If all four pairs match the current party, the script automatically uses explicit pair mode.
- In explicit pair mode, a head-stack player goes to the 1238 set and their partner goes to the 4567 set. If both players in a pair have fan/circle, the first configured player goes 1238 and the second goes 4567.
- The settings UI includes Pair validation / ペア設定チェック to warn about incomplete pairs, duplicate players, missing party members, or impossible current debuff splits.
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

### KT X-mainstream explicit pairs - MT-OT/H1-H2/M1-M2/R1-R2 - AAABBBBA

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
{"TargetScriptName":"SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate@P2_Forsaken_beta","ConfigurationName":"KT X-mainstream explicit pairs - MT-OT/H1-H2/M1-M2/R1-R2 - AAABBBBA","Configuration":"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","Overrides":null}
```

### Meow LZW Pairstrat explicit pairs - MT-OT/H1-H2/M1-M2/R1-R2 - AAABBBBA

Pair setup:
- Pair 1: MT-OT
- Pair 2: H1-H2
- Pair 3: M1-M2
- Pair 4: R1-R2

Wave table: AAABBBBA, meaning 1238 resolves waves 1, 2, 3, 8; 4567 resolves waves 4, 5, 6, 7.

Global priority: MT OT H1 H2 M1 M2 R1 R2.

Source/reference:
- [Meow³'s Braindead P2 / Pairstrat Static + Uptime](https://raidplan.io/plan/lZWqxfxvyhF9sp3Z)

> [!Note]
>
> This currently uses the same pair map and coordinate table as the KT X-mainstream explicit-pair preset. It is kept as a separate import so users can select by strategy name.

```
{"TargetScriptName":"SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate@P2_Forsaken_beta","ConfigurationName":"Meow LZW Pairstrat explicit pairs - MT-OT/H1-H2/M1-M2/R1-R2 - AAABBBBA","Configuration":"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","Overrides":null}
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
> This import includes Kroxy-Rinon coordinates copied from the Rinon script reference in `message.txt`. The Center-based helper/bait positions are converted to the generic script's reference-tower angle basis. It still needs replay or live validation before treating it as fully proven.

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

Grouping mode: Priority index pairs 1238/4567. Priority slots 1+3, 2+4, 5+7, and 6+8 are paired. Pairs containing one head-stack marker become Group A; non-stack pairs become Group B.

Priority: MT OT H1 H2 M1 M2 R1 R2.

Source:
- [Meow³'s Braindead P2 / Pairstrat Static + Uptime](https://raidplan.io/plan/lZWqxfxvyhF9sp3Z)

This preset currently uses the same grouping, priority, and coordinate table as Pino. The lZW RaidPlan pair structure matches the same 1238/4567 fixed-pair model in the current verified config.

> [!Warning]
>
> This lZW import intentionally mirrors the latest Pino import. Keep it separate so users can import by strategy name, even while the current config payload is identical.

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
</details>
