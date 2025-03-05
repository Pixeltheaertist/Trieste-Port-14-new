using Content.Server.Event.Components;
using Robust.Shared.Prototypes;
using Content.Server.Deathwhale;
using Content.Server.Falling;
using Content.Shared.Salvage.Fulton;
using System.Collections.Generic;
using Content.Server.Mind;
using Content.Shared.Mind;
using Content.Shared.Body.Components;
using Content.Server.Administration.Logs;

namespace Content.Server.Deathwhale;

//summary
// This system controls deathwhale hunting behavior. Namely, the ability for the deathwhale to consume anyone caught in it's range of light.
// If a edible player is caught in range of a deathwhale's searchlight, it will be caught and deleted.
// TODO: Needs refactor before bigger implementation. Should use a fulton style animation, and teleport the player to a indestructible "stomach"
//summary

public sealed class DeathWhaleSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    private const float UpdateInterval = 1f;
    private float _updateTimer = 0f;

    // Store the caught prey UIDs
    private readonly HashSet<EntityUid> _caughtPrey = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeathWhaleComponent, ComponentInit>(OnCompInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _updateTimer += frameTime;

        if (_updateTimer >= UpdateInterval)
        {
            // Check for prey within the DeathWhale's range
            foreach (var entity in EntityManager.EntityQuery<DeathWhaleComponent>())
            {
                var uid = entity.Owner;
                var component = EntityManager.GetComponent<DeathWhaleComponent>(uid); // Get the DeathWhaleComponent

                DeathWhaleCheck(uid, component);
            }

            // Reset the update timer
            _updateTimer = 0f;
        }
    }

    // Log message when the component is initialized
    private void OnCompInit(EntityUid uid, DeathWhaleComponent component, ComponentInit args)
    {

    }

    private void DeathWhaleCheck(EntityUid uid, DeathWhaleComponent component)
    {
        // Iterate through all entities within the DeathWhale's radius
        foreach (var prey in _lookup.GetEntitiesInRange(uid, component.Radius))
        {
            if (!EntityManager.HasComponent<BodyComponent>(prey))
            {
                continue;
            }

            if (_caughtPrey.Contains(prey))
            {
                continue;
            }

            if (EntityManager.HasComponent<DeathWhaleComponent>(prey))
            {
                continue;
            }

            var preycaught = EnsureComp<FultonedComponent>(prey); // This will harpoon the prey and drag them up offscreen to be eaten
            preycaught.Removeable = false;
            preycaught.Beacon = uid;
            QueueDel(prey);
        }
    }
}
