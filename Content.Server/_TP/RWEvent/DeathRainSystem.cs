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
using Content.Server.Chat.Systems;
using Content.Shared.Camera;
using System.Numerics;
using Robust.Shared.Player;
using Content.Shared.Weather;
using Robust.Shared.Audio.Systems;

//summary
// This system utilizes an update loop to time a death rain storm, which will kill all players not inside a designated safezone (see RainShelterComponent).
// Only creatures or objects with RainCrushableComponent will be affected by the rain. Anything caught in the rain is deleted.
// It also applies a screen shake before the rain arrives, as well as an audio cue.
// Creatures with RainImmuneComponent are safe and cannot be crushed. (This is for creatures that are children of a crushable entity)
//summary


namespace Content.Server._TP.Weather;

public sealed class DeathRainSystem : EntitySystem
{

    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    // Interval in which rain will occur
    private const float UpdateInterval = 20f;
    // Interval in which rumbling and screenshake will occur
    private const float RumbleInterval = 10;

    private float _updateTimer = 0f;


    public override void Initialize()
    {
        base.Initialize();
    }

    // Initate the update loop
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        //_updateTimer += frameTime;

        if (_updateTimer >= RumbleInterval)
        {
            // for all entities that can be crushed, display screenshake and audio
          foreach (var rumbler in EntityManager.EntityQuery<RainCrushableComponent>())
            {
                // shake the screen
                var rumble = rumbler.Owner;
                var kick = new Vector2(_random.NextFloat(), _random.NextFloat()) * 2f;
                _sharedCameraRecoil.KickCamera(rumble, kick);
                // spam audio
                _audio.PlayPvs("/Audio/Ambience/Objects/gravity_gen_hum.ogg", rumble);
            }
        }

        if (_updateTimer >= UpdateInterval)
        {
            _updateTimer = 0f;

            // In shelter?
            foreach (var entity in EntityManager.EntityQuery<RainCrushableComponent>())
            {
                var entityUid = entity.Owner;

                if (TryComp<RainImmuneComponent>(entityUid, out var immune))
                {
                    // This creature is innately immune to rain. Spared.
                     continue;
                }
                // find all shelters, and see if the entity is in range of at least one shelter
                var shelters = GetEntityQuery<RainShelterComponent>();
                foreach (var shelter in _lookup.GetEntitiesInRange(entityUid, 1f))
                {
                     Log.Info("Found shelter");
                     if (shelters.HasComponent(shelter))
                     {
                         Log.Info("Inside shelter");
                         return;
                     }
                }

                // Not in shelter. Bye bye. Say hi to the void for me.

                // _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("meltdown-alert-warning"), component.title, announcementSound: component.MeltdownSound, colorOverride: component.Color);
                // Deletes the entity unfortunate to be caught in the rain
                QueueDel(entityUid);
                }
            }
          }
      }
