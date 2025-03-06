using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared.Overlays;
using Robust.Shared.Timing;
using Robust.Shared.Enums;
using Content.Shared.Clothing;
using Content.Shared.TP.Abyss.Components;

// Summary//
// This system controls abyssal pressure suits protecting users from crush depths when equipped.
// Summary//
namespace Content.Server.TP.Abyss.Systems
{
    public sealed class AbyssalSuitSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<AbyssalSuitComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<AbyssalSuitComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(Entity<AbyssalSuitComponent> ent, ref GotEquippedEvent args)
    {
        // On equip, protect the user from abyssal pressures.
        EnsureComp<AbyssalProtectedComponent>(args.Equipee);
    }

    private void OnUnequipped(Entity<AbyssalSuitComponent> ent, ref GotUnequippedEvent args)
    {
        // On unequip, make the user able to be crushed.
        _entityManager.RemoveComponent<AbyssalProtectedComponent>(args.Equipee);
    }
}
}
