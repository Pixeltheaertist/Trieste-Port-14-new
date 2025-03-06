using Content.Shared.EntityList;
using Robust.Shared.Prototypes;

namespace Content.Server._TP.Shuttles_components;

[RegisterComponent]
public sealed partial class AtmosphericThrusterComponent : Component
{
    [DataField]
    public bool Enabled = true;
}
