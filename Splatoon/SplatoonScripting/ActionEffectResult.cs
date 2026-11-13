using ECommons.Hooks.ActionEffectTypes;

namespace Splatoon.SplatoonScripting;

/// <summary>
/// What happened when the server applied one target of an ActionEffect (EffectResult / EffectResultBasic packet).
/// </summary>
/// <param name="RecipientId">Entity ID of the character the packet was addressed to. Its HP (and shield) were written from this packet.</param>
/// <param name="IsSource">
/// True when the packet was addressed to the source of the action rather than its target: the caster's own HP, MP or statuses
/// changed as part of the action (drain, self buff, retaliation). The effects still describe the target.
/// </param>
/// <param name="IsBasic">True for EffectResultBasic, which carries HP only. Shield values are -1 then.</param>
/// <param name="Effects">Effects of the action on this target, copied when the ActionEffect was received.</param>
/// <param name="HpBefore">HP of the recipient right before the packet was applied.</param>
/// <param name="HpAfter">HP of the recipient from the packet.</param>
/// <param name="ShieldBefore">Shield of the recipient in percent of max HP right before the packet was applied.</param>
/// <param name="ShieldAfter">Shield of the recipient in percent of max HP from the packet, or -1 for EffectResultBasic.</param>
/// <param name="DelayMs">Milliseconds since the ActionEffect was received.</param>
public readonly record struct ActionEffectResult(
    uint RecipientId,
    bool IsSource,
    bool IsBasic,
    EffectEntry[] Effects,
    uint HpBefore,
    uint HpAfter,
    int ShieldBefore,
    int ShieldAfter,
    long DelayMs);
