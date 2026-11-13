using ECommons.Hooks.ActionEffectTypes;

namespace Splatoon.SplatoonScripting;

/// <summary>
/// What happened when the server applied one entry of an EffectResult / EffectResultBasic packet.
/// </summary>
/// <param name="HasActionEffect">
/// True when the entry was linked to the ActionEffect it belongs to. When false, the set passed with this result is empty (default) and must not be used,
/// <see cref="SourceId"/> is 0xE0000000 and <see cref="Effects"/> is empty. Sequence 0 means the change does not come from an action
/// (damage over time tick, fall damage, HP sync); other unlinked entries belong to an ActionEffect that was not seen (e.g. received before the plugin loaded).
/// </param>
/// <param name="Sequence">Global sequence of the action (Header.GlobalEffectCounter), 0 when not from an action.</param>
/// <param name="SourceId">Entity ID of the source of the action, 0xE0000000 when unknown.</param>
/// <param name="TargetId">Entity ID of the target of the action. Without an ActionEffect and with sequence 0, the recipient.</param>
/// <param name="RecipientId">Entity ID of the character the packet was addressed to. Its HP (and shield) were written from this packet.</param>
/// <param name="IsSource">
/// True when the packet was addressed to the source of the action rather than its target: the caster's own HP, MP or statuses
/// changed as part of the action (drain, self buff, retaliation). The effects still describe the target.
/// </param>
/// <param name="IsBasic">True for EffectResultBasic, which carries HP only. Shield values are -1 then.</param>
/// <param name="Effects">Effects of the action on the target, copied when the ActionEffect was received. Shared, do not modify. Empty without an ActionEffect.</param>
/// <param name="HpBefore">HP of the recipient right before the packet was applied.</param>
/// <param name="HpAfter">HP of the recipient from the packet.</param>
/// <param name="ShieldBefore">Shield of the recipient in percent of max HP right before the packet was applied.</param>
/// <param name="ShieldAfter">Shield of the recipient in percent of max HP from the packet, or -1 for EffectResultBasic.</param>
/// <param name="DelayMs">Milliseconds since the ActionEffect was received, -1 without an ActionEffect.</param>
public readonly record struct ActionEffectResult(
    bool HasActionEffect,
    uint Sequence,
    uint SourceId,
    uint TargetId,
    uint RecipientId,
    bool IsSource,
    bool IsBasic,
    EffectEntry[] Effects,
    uint HpBefore,
    uint HpAfter,
    int ShieldBefore,
    int ShieldAfter,
    long DelayMs);
