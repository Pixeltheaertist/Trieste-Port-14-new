﻿using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Events;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Content.Server.Ladder;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Shared.Climbing.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Ghost;

namespace Content.Shared.Ladder;

public sealed class LadderSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

       // SubscribeLocalEvent<LadderdownComponent, StepTriggeredOffEvent>(OnStepTriggeredDown);
       // SubscribeLocalEvent<LadderupComponent, StepTriggeredOffEvent>(OnStepTriggeredUp);
        SubscribeLocalEvent<LadderdownComponent, ClimbedOnEvent>(OnClimbedDown);
        SubscribeLocalEvent<LadderupComponent, ClimbedOnEvent>(OnClimbedUp);
    }

    private void OnClimbedDown(EntityUid uid, LadderdownComponent component, ref ClimbedOnEvent args)
    {
       if (HasComp<GhostComponent>(args.Climber))
           {
            return;
           }

          if (HasComp<HandsComponent>(args.Climber))
            {
           var destination = EntityManager.EntityQuery<LadderupComponent>().FirstOrDefault();
            if (destination != null)
            {
                Transform(args.Climber).Coordinates = Transform(destination.args.Climber).Coordinates;
                 _popup.PopupEntity(Loc.GetString("ladder-down"), ("owner", args.Climber), args.Climber, PopupType.Medium);
            }
            }
    }

     private void OnClimbedUp(EntityUid uid, LadderupComponent component, ref ClimbedOnEvent args)
    {
       if (HasComp<GhostComponent>(args.Climber))
         {
            return;
         }

        if (HasComp<HandsComponent>(args.Climber))
            {
       var destination = EntityManager.EntityQuery<LadderdownComponent>().FirstOrDefault();
            if (destination != null)
            {
                Transform(args.Climber).Coordinates = Transform(destination.args.Climber).Coordinates;
                _popup.PopupEntity(Loc.GetString("ladder-up"), ("owner", args.Climber), args.Climber, PopupType.Medium);
            }
            }
    }
    
}
