# Recommended minimal imports for P2 as of 2026-06-04
- Import script: P2 Forsaken beta / Missing guide

> [!Caution]
>
> P2 Forsaken / Missing strategies are still moving. The beta helper below is table-driven so the same script can be configured for several current public strategies.

## Scripts

### **[Script] [Unfinished]** Forsaken marker viewer
This script is mostly a stub for further development. It currently shows who has which marker.

```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/Dancing%20Mad/P2_Forsaken.cs
```

### **[Script] [Beta]** P2 Forsaken beta / Missing guide
Self-only helper for P2 Forsaken / Missing. It reads the live Missing debuffs, tracks the tower pair from map effects, determines the resolving group, and guides only the local player to the configured tower-relative position.

Default model:
- The tower position itself is inferred from map effects.
- Basic positions are tower-relative. The configured angle/distance table controls where each role stands relative to the current tower pair.
- If Group A and Group B are left empty, Group A is auto-captured from the first Missing forecast as both head-stack players plus the highest-priority circle player and highest-priority fan player. Group B is the remaining four players.
- The global priority controls auto Group A circle/fan selection and same-debuff rank ordering.

Configuration notes:
- Leave Group A and Group B empty when the selected strategy defines Group A from the initial Missing forecast.
- Use fixed Group A/B if your group assigns four-player groups outside the initial debuff forecast.
- Use the Debug tab to verify the detected tower pair, auto groups, live debuff role, and selected position rule during replay review.

Verification:
- Tested against FFLogs-derived saved data from report `7q64RXCZHygAjhJp`, fights 51, 84, and 95.
- The AAABBBBA fixed-partner default matched all checked resolver-group assignments in those P3-reaching pulls.
- Local SplaSim and Splatoon builds passed, and the latest Dalamud/Splatoon log reported `P2_Forsaken_beta ready`.

Script file in this repository:
```
SplatoonScripts/Duties/Dawntrail/Dancing Mad/P2_Forsaken_beta.cs
```

> [!Important]
>
> The exported configurations below target the current local script name `SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate@P2_Forsaken_beta`. If the script namespace is changed for an official release, regenerate these configuration strings before importing them.

#### AAABBBBA fixed-partner auto group
Wave table: Group A resolves waves `1, 2, 3, 8`; Group B resolves waves `4, 5, 6, 7`. This is the same resolver sequence as the current Toxic Friends / fixed-partner / Forsaken Buddies style RaidPlans; exact fixed partner positions can still differ by plan. Avoid treating it as region-specific because the same family is currently shared in both overseas Discord/RaidPlan contexts and NA Kefkabin-style materials.

Priority: `H2 M2 T2 M1 R2 R1 T1 H1`.

Related public plans:
- [Toxic Friends - UMAD P2](https://raidplan.io/plan/8wo9cYItAo2mkKSc#2)
- [Fixed Partners Modified Toxic Friends](https://raidplan.io/plan/aZjFjQN2CVs3LxIH)
- [Forsaken AAABBBBA a lot of fixed stuff](https://raidplan.io/plan/sGTZEzSxNJ790PTQ)
- [Forsaken Buddies](https://raidplan.io/plan/cKt22s4zMP50lDOV)
- [NA Kefkabin / UMAD P2: Forsaken](https://docs.google.com/presentation/d/1xzKhp29UPlZFICbZTtcFSJr_dQDCj93UKISG1_-SwhU/preview#slide=id.g3e5d74e9fd1_0_0)

```
{"TargetScriptName":"SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate@P2_Forsaken_beta","ConfigurationName":"AAABBBBA fixed-partner auto group","Configuration":"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","Overrides":null}
```

#### Yan Flash / Poikos tank LB3 1-4/5-8
Wave table: Group A resolves waves `1, 2, 3, 4`; Group B resolves waves `5, 6, 7, 8`.

A/B sequence: `AAAABBBB`.

Priority: `H2 H1 T1 T2 M1 M2 R1 R2`.

Source:
- [Yan Flash / 絶妖星乱舞](https://yan-flash.com/ultimate/yosei-ranbu)

> [!Warning]
>
> Yan Flash describes this as Poikos tank LB3 handling. This preset assumes Group A can be auto-captured from the initial Missing forecast using the priority above. If your group fixes red/blue members manually, fill Group A and Group B manually in the settings.

```
{"TargetScriptName":"SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate@P2_Forsaken_beta","ConfigurationName":"Yan Flash / Poikos tank LB3 1-4/5-8","Configuration":"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","Overrides":null}
```

#### Drippy regular 1458-2367 auto group
Wave table: Group A resolves waves `1, 4, 5, 8`; Group B resolves waves `2, 3, 6, 7`.

A/B sequence: `ABBAABBA`.

Priority: `T1 T2 H1 H2 R2 R1 M2 M1`.

Source:
- [Drippy / P2 ミッシング](https://drippy-sokuhou.com/articles/kefka-p2-missing)

This priority is chosen so the auto group picks the head players, the highest-priority TH circle/fan player, and the lowest-priority DPS circle/fan player, matching the article's described group split.

> [!Warning]
>
> Drippy describes same-mark left/right movement using the players' current positions. The current script expresses that with fixed priority ranks instead. If your group needs exact current-position sorting, the script logic needs a small extension.

```
{"TargetScriptName":"SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate@P2_Forsaken_beta","ConfigurationName":"Drippy regular 1458-2367 auto group","Configuration":"GyU2IqonhwCWBbw5OpVgYyrsmb52oY3EgTAPZDlJ2Qm3g1a9ti+czAKwOKfQdmlHiAe8tjRldzb5VoCouO9d01P4JcPeKSbCqUs13UhlFzQh9xWKnbFTciFZuvpCp0VguvZatUwldoKpuAAUoGJxxQSFuK4nrELIMGQo2d7onM8sZPZQpkbA0AS5uAE0I2bI9+3X7jCreZfZmRNPdEo8k4gnmpay9gfXRuJ1fugVk3aJElmGznZCpZ5fDD8JEdjc7H+MFOQ2fkI7oZ+LfzeE46/2oPINPzFDk6Hq4fF4nOGovWUQdzDbwdalDh5r47UvH7fcFmrMhHVuzsHhr1TmIYras41pYK0uMWo6AnqCp7sC3xF7mU1plCUePolaVlHLKc852BwFPQpa3eQv7nZBbwqYh/RdG+b0oH5SnJUCjlgLCr0mS273a8ClZmC2RVjkxJaUdaNkVVcY/THtrTJZ69yJq3GxyR8jUcTb8TmSdeDraXrTCe7tCXO9MHceS89kR2mePVMdlclxfk3ifRTT+UYG/KPbXFhPPQ3B2hcBnyJim8vbdDgB4+L+dzpA6w+WbFRh8r1YKRqkq8gb0N8j/t2mfjLWWLw0pxVjqBwjxAawI9lTbeBzxtCuFdOgPqInWkBGkmripog+SbQBefWtpnyNjf6lPyuOYYi3Icz1wtxhLD2QHaVh9kB1VCaHR5JLQY1sYWTvpIa57Pqa9IWa5GYigsMXQEHH3QIBqQNZ+RAu0CB4pU39IIAIn+qR0xFAaDQuENuxk3+1lxYtaTOoRazhVBsqU2vjHfzdMyj1IMI5nc325Rkte/9bxDtey6ezD7wnnmF+XsSZXPbCZdeXIqrSXwRC7UARnRrX/84IQwn/3a9G2+m1N20IeRjbp4G15uCL2aaT3A7i8+6beCDNDBVEvB1yHOpxzBJmsUejw3einGbOlt8176AHHLLF4YzN7pKJOiKQMLtLjnrEa4ZSbpgBpx1iVjBvr4KtMFIz+QSMP/Gqqy1FZ2VoMUiuyjZobh/7uf5PV5nqv6Jfw4x3HS5G3/lxHyJbofZYTtTVzgflZib4WW5wtIsJfBdgy/2qs2x1BXQZRpQcHT8Retih0KWFAKQC5HeCEuaRIfa0dRVIJrWyNHFSqZU/GdgWwctAVn4DLSXZXsxMXvVXvmxgNng5+928FSygBRygLm4DSOAHeBBoCd/HAwKKrduqANN227nv7Qb0adE/NTXku5HZDVjyWHk4tvd9yu3c8cBcpIU22rhxdWI+xvXAwuiZlsjW2A3Ewei73PCEOdttgPUd5UTaiHqWMb9JZq5twNPKBWDSf7/lZKYLisjmTEQsTH1O6Lm8PYVfwGY9eQG+KrN6CFgxViFUK4RnbZD/8FdTrQcewB/wwO4EYxK0k/LysG8dZ8F/J4rk8hNOwPEoNdmMF9D5qG6CryGWYc+hxOiik8YTiPBR5TBh3gA9gkiIBNEEqvpK62fOvgVchAKCwvERiIqJrg07UIHAClsGLGQGAxY2s5nM6LMokLuQsuLBNDpgd4ErI8bMGb23BLRwRuXrsymTr6jUABZmIGl1zxOpVFKVXH2s3FLrDl+GEkOLQXJVpkmi1h2+TCVKowT0xttdNJtjZBz1525zkQqkKPnKdTpX45Qvfqp9mx8FZP4CwvF8BA6VPUMGglDygG7Fe/KdgeVYnsOmV0AFQ5wLknopJsH15V14FXKuXJEaJTTjMnQFJVyrfUKxRV+Ai3B/HcmCk32wy9PG4CtJWwrPb5WB2BkB2IrLxvT11zoAJjpgMr1lJnoJRRWWp0ijh7Nsq1yDMbPVgVRlbGRJ0hFlv+KlenuX4GAalemDJhBFmQlnSLJkkFxVgZI4JWdJ51wIrWpQxfJkywAlaAKTsnWJtPSaa0ind13l/CT51lknCGjbjyWpRWXgJbOAIpRThPJdaLKlkORH80RSP0CmIqvoQXfG/cvChAnKld9/b1unVJrep6swhXE1LKDkvRJxX/YRYVAxn5+8evqo4h0IIRI0zu/D6UQQ+4OMi7YbkqsKJCDqAiUr5jaqJ1x5sy6cKoH9t7kuK2BdWFPiaJ/5oEJ2JvItVOFJwj6ArTxyUvmT2J3E7uCDYBa6Ot26mOzBIYFkTFLy1ruke0hS31mTE2kRMC5AL6kgqpCtbliZZCWiul52Bc0uUZP7R65jIRG2o5ofemsbX0jlo1+WtnCabSo5RN56Lx4xVvMND0TByD1N3d2FHOVPZ+CUaX2CnS4sq2plTQIRRRqpug+m+JSidX2+4Lb3/zF3R/2umqwI+R4QmRWdr/Tpvzw4IVH9V++SqVsVM/HXV+xBAUSdLB9v7IuOGANmOAafJCTGBAjxJTS/sGzj2v+P+ZszbCO6VYSprGJtIXCEvVkNu6TMIyigjH32icV/sc+giuU5n5QmKUTAQSzv8aZudltumFAmJzOfvjtpqT1VieJYNtAQADPcoq8rfRE7/E1z7nGmRjM15WWaAw==","Overrides":null}
```
