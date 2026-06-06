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

Global priority: MT OT H1 H2 M1 M2 R1 R2.

Source/reference:
- [Yan Flash / 絶妖星乱舞](https://yan-flash.com/ultimate/yosei-ranbu)
- [KT / ミッシング検討資料](https://docs.google.com/presentation/d/1RDLS_RW2VSgqPp8KbHKgWV6bMsIFq1tTnhQjYZ1Fx1E/edit?slide=id.g3e847f115ae_3_5#slide=id.g3e847f115ae_3_5)
- [Sora Haruno / KT source tweet](https://x.com/soraharuno_XIV/status/2062576334115873269)

```
{"TargetScriptName":"SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate@P2_Forsaken_beta","ConfigurationName":"KT Yan-style explicit pairs - MT-H1/OT-H2/M1-R1/M2-R2 - AAABBBBA","Configuration":"G0Y3ACwLeHMXKVhGLNwT+48eWlrF8DyI6BZpYN8dxYGVWWA4N/sszim0XTpi+nm/OV/ZDOGyz27MupDrMNnr9QMJp8amzhDRytjK7nOTaggOwSnJ0tUXOi0C0rXX//+1NGU4Pk4O5M8UtyqNr1G7793/fonI4eBOSowWjaqu0TWyyk9SopVdxlTtsO9DwzGSs0DnZv9VNlC5tQ8ZNC0Lq3VGRGZEi6eB5WyNpBCgNdCzUNBYiuyK3kn4X53Dtfw9Lvzyvj2eQCDwqxvOVucQrj1WkG6MP91onHSEx0+XGpWed2/4bPxEh7X+VXm36zIJfSGxmRSbQYUippBDYRoKk0SLtXB7Tm8SmIrwYjNFAbyIiMQEEOj35cxufnW4bNNlzLtizggkfBjPI4kFdRkI6fFU2kQX+O6+XdhL7+bm9LKG+MbaEr/lhNdaJ6kdhpsUTDzi8cNMHHQfWu8fk+WGMxNGEK8XI0mBNGWpfWo9xrQF7c90B1jxKJMMV8il2esakGDHFbOWjwj7JRaQuw8GnAKNoCc4M5FZXgzqjaij2v6EKxB035UEhRFgSOIse7A2Te5LnigJduSaZ3AZKBOkwAwnUY8Zk1PLYVXLlK6ifeXaRoIOyXe7wF0gUQ03LPf/zRLU0YZagkJt8mIdyiaei8N/94qcJJDyOVps/9WmJrKrWyr2Zm+ll2PbZscVdfFNKTiVigcV54rWD8i3L7DxHRWnlHXjWWCGHtymRva71jjM/qPBTILBz+eejLIbiMPdE9Fmi6K0ZiaohHHEPIMOp6IxQ/sVURxNH+a29+kIHWBXpRPXvjPF5ROeEt1xnSlqod+LxZDIR661ubD03gbi5gwE3E2gsm5Nn78CbuaRbZMT1GgjBtPxnQX7AGhxkIIu9X7vS0qPdVmbKv9kTD6GXCv3oSMmKAwRQdgIJlB0988DZaYG+LEmqKODNUEoBg4VXL9UBJ0Nlk/Vg0Q6PMdpmtVP03IZaWI47QwlmpchXZMREEkeFA51RE6S0H+nrVkENwJJ6Q00lGT2bGrwFO/zZQGT/svJy/otbwINYANVsCtAAD/ABUONeT4eYBBMlWYJGKHfOuXdtE8Bgn+yKsC7IVMcFCjEclFkS79P2Y2LKY6tk9bq0DqO8pR8DFXnAEMLEwrblG5EGQzdt5tteU4dN4ENR4qZNMOzDoh5Q1KjLR2eMIEEk//lKKtPK7NX2azrhTMLzdK/aloWPp+AYWDv0FlyXJYRhReRUZA2RrXBZDFkMvvcZdH5JxyA0+d4b9KEBZeaZFSN4HtUCkSVsJCDRMaTa9rsGdRRr7HJG9bOOgCFPj+Zd6913tK2BuG/mE6vK+scstwcGaPOHOBPqmICMfu3tVaz4ky2mPO0xYVv3Kuf1RdEnrOh9sdD4HC4AKpN8h+5xKX1C4KDGj3lr0ZpaG0FtLan+gV5GNTo5npFSk2t4sSMqIIRi9rokhRYZKf7VgMq4w4z6R4zuxmHyU+Zw3fwpngalu+KZKW3+jMYPnRuqWp8+qoan4n6XrFCSrsgpoOjJjUre8dr9had0f8m4zlNqIgbmLCUIzXP06C1FHZNWVN5590tU0UR0OIgBV3KfSLe3TJVFbkUE/A68BeIKQtOd2V/SEN8HVgSp9d6T8bjMzu5srZo8Oj7wuCMPxJI00XcTSBFkFeRGN6TLhoJ0L70mXJEs7UPNpm9jgK/EI/TguSGoknghrSUeBfayXSKUJ7RqG2oNS4zdtIslw7yBsVI8U8PUyCn8Cw8L/HNVvtVmsH4qO4LH4uoUWPMaNM/mIJgUhvYZhCpWctNWu3S+4SHoXwiFPQZ0UJ2WRQbcVywXT0G98ZLc/9lR+UoZcDaYlAXM1j6zEmBDE++msoVPb5sLpD2ASGmmtq15OcolOEqizrCkUMqlVZYj/WQVue4SPhk8zFn7SwoO/7EOkzd91W0uvKhrDyQa3NTKMWT/NQ8eDI/gQyQaBLTnQ7+xcKgEiVDLIu71akUtYM+t0MmLwpyxWxvHnGHvCcFu5ayveTf5b+q+AKEEFnUgd83B2oOFN4+lOe76mTExtD2WLleV5bUO4ptuqKmwsu7zXmX01DypzrC5OoyWLrieeg9bjgGT3UpRVU+D1UB1seSFmB/9tf8bw0mnQa2tc5JLkS0SucRbIBViWZLv5J3NWdKVBpmdtKfLOggBTVO9dv/bo/mCSMyYM4YMxsrWlM34xx9LPxdKShz6asRoiw++tRZC1T59+evpewTxvzv4SOLLkIG7OgjRlOQ3Lp6tbI/qfL+uL1opz5v9AKjDf3EvZhIk4A+WPJDQO3F0euuEQ59yFGof8Dnma5SURUpOF/iR3RWXNRgfkWxQ3RSsYt/kKG52yH+UYlL/JyVry2ePEmN9mtDMFg2ZeH3clIzVJ6XSCI5cJr6+hZAypCWSPk71f2sbykuh/RENk51P+u9pgd3D6sB0IlxE2puy00cmJRk5vG9cWBrSW0jNcysic81kM2kxbhU+8ED","Overrides":null}
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
