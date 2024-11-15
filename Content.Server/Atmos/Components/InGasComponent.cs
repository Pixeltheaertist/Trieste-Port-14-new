﻿using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Server.Atmos.Components;

/// <summary>
/// Component to handle non-breathing gas interactions.
/// Detects gasses around entities and applies effects. (this is currently for damage to borgs but ¯\_(ツ)_/¯)
/// </summary>
[RegisterComponent]
public sealed partial class InGasComponent : Component
{

    /// <summary>
    ///     ID of gas to check for as an int. Defaults to water.
    /// </summary>
    [DataField("gasID"), ViewVariables(VVAccess.ReadWrite)]
    public int GasId = 9;

    // amount of gas needed to trigger effect
    [DataField("gasThreshold"), ViewVariables(VVAccess.ReadWrite)]
    public float GasThreshold = 0.1f;

    /// <summary>
    ///   Whether the entity is damaged by water.
    ///   By default things are not
    /// </summary>
    [DataField("damagedByWater"), ViewVariables(VVAccess.ReadWrite)]
    public bool DamagedByGas = false;

    /// Damage caused by gas contact
    [DataField("damage"), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = default!;

    ///<summary>
    /// Prevents gibbing from gas damage, same purpose as the barotrauma one
    /// </summary>
    [DataField("maxDamage"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MaxDamage = 200;

    /// <summary>
    /// Used to track when damage starts/stops. Used in logs.
    /// </summary>
    public bool TakingDamage = false;
}
