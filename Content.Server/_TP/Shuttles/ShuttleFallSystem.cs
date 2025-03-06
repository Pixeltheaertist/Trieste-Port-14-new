using Content.Server.Shuttles.Components;
using Content.Shared.Gravity;
using Robust.Shared.Map;
using Content.Server._TP.Shuttles_components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.Prototypes;
using Content.Server.Shuttles.Events;
using Content.Server.Falling;
using System.Linq;
using Content.Server.Station.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Random;


namespace Content.Server._TP.Shuttles;

// Summary//
// This system allows shuttles to fly in the atmosphere of Trieste if they have a specific atmospheric thruster.
// It checks every 5 seconds for two things: Shuttles that are flying in the air in Trieste airspace, and the state of the shuttle's thrusters.
// It uses these to see whether a shuttle will fall into the Waste Zone and have it's engines waterlogged (offline), or allow it to fly in atmosphere.
// This will allow for things such as a cargo shuttle that navigates from Trieste to some sort of orbital trading station, Caskies invasion crafts, pirate raids, air combat in VTOL fighters, etc.
// Example: If you shoot off the engines of a VTOL shuttle or EMP it, it will fall from the sky into the ocean surface.
// Summary//


// TODOS:
// Make the crew onboard the ship get knocked over when the ship falls.
// Make it so docking to Trieste prevents falling.
// Small explosions across shuttle when it crashes.
// Make atmospheric thrusters turn off in space.
// Special sound effect for moving from air to space (muech louder helicoper buzzing)
// Add docks to Trieste to allow these vessels to properly dock.
// Make a unique sound for the FTL when falling.

public sealed class ShuttleFallSystem : EntitySystem
{
    [Dependency] private readonly ThrusterSystem _thruster = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    // Timer for fall updates
    private const float UpdateInterval = 6f; // This should help maybe with uhh, making sure it doesnt fall immediately.

    //Timer for flight checks (thrusters and active flight)
    private const float CheckInterval = 2f; // Interval in seconds

    private float _updateTimer = 0f;
    private float _checkTimer = 0f;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AtmosphericThrusterComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DockEvent>(OnDock);
        SubscribeLocalEvent<UndockEvent>(OnUndock);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;
        _checkTimer += frameTime;

        if (_updateTimer >= UpdateInterval)
        {
            _updateTimer = 0f;

            // Get all shuttles
            foreach (var entity in EntityManager.EntityQuery<ShuttleComponent>())
            {
                // Get the EntityUid from the ShuttleComponent
                var entityUid = entity.Owner;

                // Make sure that the target isn't a station (we don't want Trieste falling into the ocean... yet)
                if (TryComp<TriesteComponent>(entityUid, out var station))
                {
                    // IT'S A STATION, ABORT ABORT ABORT
                    continue;
                }


                // Get the map the shuttle is currently on
                var currentMap = Transform(entityUid).MapUid;

                // Ensure that the shuttle is in Trieste airspace
                if (TryComp<TriesteComponent>(currentMap, out var triesteComponent))
                {
                    if (TryComp<AirFlyingComponent>(entity.Owner, out var flight))
                    {
                        if (flight.FirstTimeLoad)
                        {
                            flight.FirstTimeLoad = false; // thats all i had to do LOL i dont need to remove the component
                            return;
                        }

                        Log.Info("Has flying component");
                        // Make sure the ship is actively flying, and is not docked to another flying vessel
                        if (!flight.IsFlying && !flight.DockedToFlier)
                        {

                            // Find where the shuttle will be falling to
                            Log.Info("It be falling, SHITE!!");
                            var destination = EntityManager.EntityQuery<FallingDestinationComponent>().FirstOrDefault();
                            if (destination != null)
                            {
                                var targetMap = Transform(destination.Owner).MapID;
                                var coords = Transform(destination.Owner).Coordinates;
                                var mapCoordinates = _transform.ToMapCoordinates(coords);
                                var mapUid = _mapSystem.GetMap(targetMap);

                                var offsetx = _random.NextFloat(0.0f, 40.0f);
                                var offsety = _random.NextFloat(0.0f, 40.0f);
                                var newcoordx = offsetx;
                                var newcoordy = offsety;


                                _transform.SetCoordinates(entityUid,
                                    Transform(entityUid),
                                    new EntityCoordinates(mapUid, newcoordx, newcoordy));
                            }
                        }
                    }
                }
            }
        }

        if (_updateTimer >= CheckInterval)
        {

            // Get each atmospheric thruster on the map for flight checks
            foreach (var atmoThruster in EntityManager.EntityQuery<AtmosphericThrusterComponent>())
            {
                // Find the thruster's UID for FlightChecks
                var thrusterID = atmoThruster.Owner;

                // Get this for... some reason? Why am I getting this again?
                var currentMap = Transform(thrusterID).MapUid;

                // Perform flight checks to change thruster states
                FlightCheck(thrusterID, atmoThruster);

                // Use the now-changed thruster states to see if a ship is flying.
                FlyCheck(thrusterID, atmoThruster);
            }

            _checkTimer = 0f;
        }
    }


    private void OnInit(Entity<AtmosphericThrusterComponent> ent, ref ComponentInit args)
        {

            // Get the shuttle the engine has spawned on
            var shuttle = Transform(ent).GridUid;


            // If the shuttle exists, slap the flight-capable component on (save ship mappers some brain-ouches)
            if (shuttle.HasValue)
            {
                // Adds the AirFlyingComponent to shuttles with atmospheric thrusters, marking them at in-atmosphere vessels.
                Log.Info("Added AirFlyingComponent");
                EnsureComp<AirFlyingComponent>(shuttle.Value);
            }
            else
            {
                // If it's floating in space, something is wrong because it should be falling but whatever
                return;
            }

        }

        private void FlightCheck(EntityUid thrusterID, AtmosphericThrusterComponent atmoThruster)
        {
            // Get the parent shuttle
            var shuttle = Transform(thrusterID).GridUid;


            // If that shuttle somehow doesn't have the component, cry.
            if (!TryComp<AirFlyingComponent>(shuttle, out var flyingComp))
            {
                return;
            }

            if (TryComp<ThrusterComponent>(thrusterID, out var thruster))
            {
                // If the thruster is on, yeehaw. The shuttle is flying
                if (thruster.IsOn)
                {
                    Log.Info("Shuttle is flying");
                    atmoThruster.Enabled = true;
                }
                else
                {
                    // If it's off... Uh-oh. You might be screwed.
                    Log.Info("Shuttle is unable to fly");
                    atmoThruster.Enabled = false;
                }
            }
        }

        private void FlyCheck(EntityUid thrusterID, AtmosphericThrusterComponent atmoThruster)
        {
            // Get the parent shuttle
            var shuttle = Transform(thrusterID).GridUid;

            // If it somehow doesn't have the component, return.
            if (!TryComp<AirFlyingComponent>(shuttle, out var flyingComp))
            {
                Log.Error("Parent shuttle does not have AirFlyingComponent.");
                return; // FROM WHENCE YOU CAME
            }

            // Temporarily set IsFlying to false
            flyingComp.IsFlying = false;

            // Check every atmospheric thruster to see if it is enabled. Is it a bit intensive? Yes. Do I know how to code it better? Haha, absolutely not!!
            foreach (var engine in EntityManager.EntityQuery<AtmosphericThrusterComponent>())
            {
                // If this engine does not belong to the shuttle we are processing, skip it.
                if (Transform(engine.Owner).GridUid != shuttle)
                {
                    // Wrong shuttle, try again.
                    continue;
                }

                // If at least one thruster is working on the shuttle, cut the loop short and set it as flying.
                if (engine.Enabled)
                {

                    // Neawwww, you're flying!! I beliiieve i can flyyyy...
                    flyingComp.IsFlying = true;

                    break; // <---- I could really use one of these



                    // so here's an example of a break being used
                    // So, pretty much, this loop checks all atmospheric thrusters on a shuttle
                    // all it needs to do is find AT LEAST one, the others don't matter
                    // so i end the loop early if it finds one
                    // So, theres 2 kinds of equal signs
                    // = and ==
                    // yeah sorta except other way
                    // = sets something to the thing
                    // == compares them
                    // and != is NOT EQUAL
                    // lessons with Cheese ^^^
                }

                // If there are no thrusters that are enabled on the shuttle, IsFlying stays off, in which it will fall.
                // Also maybe intensive but, again, i'm awful at coding.
            }
        }


        private void OnDock(DockEvent args)
        {
            if (TryComp<AirFlyingComponent>(args.GridAUid, out var dockedShip))
            {
                // If the ship you are docking to is flying, allow safe disablement of atmospheric thrusters.
                if (TryComp<AirFlyingComponent>(args.GridBUid, out var childShip) && dockedShip.IsFlying)
                {
                    Log.Info("Docked to a flying ship");
                    childShip.DockedToFlier = true;
                }
            }
        }

        private void OnUndock(UndockEvent args)
        {
            if (TryComp<AirFlyingComponent>(args.GridAUid, out var dockedShip))
            {
                // When you undock from your parent ship, disables the safety net. Make sure atmospheric thrusters are online before undocking.
                if (TryComp<AirFlyingComponent>(args.GridBUid, out var childShip))
                {
                    Log.Info("Undocked from a flying ship");
                    childShip.DockedToFlier = false;
                }
            }

        }


    }
