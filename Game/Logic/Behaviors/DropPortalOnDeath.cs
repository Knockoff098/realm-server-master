﻿using RotMG.Common;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game.Logic.Behaviors
{
    class DropPortalOnDeath : Behavior
    {
        private static Random Random = new Random();

        private readonly ushort _target;
        private readonly float _probability;
        private readonly int? _timeout;
        private readonly float _dist = 0.0f;

        public DropPortalOnDeath(string target, float probability = 1, int timeout = 30, float dist=0.0f)
        {

            if (Resources.Id2Object.ContainsKey(target))
            {
                _target = Resources.Id2Object[target].Type;
            }
            else _target = 0x0717;

            _dist = dist;
            _probability = probability;
            _timeout = timeout; // a value of 0 means never timeout, 
            // null means use xml timeout, 
            // a value means override xml timeout with that value (in seconds)
        }

        public override void Death(Entity host)
        {
            var owner = host.Parent;

            if (Random.NextDouble() < _probability)
            {
                var timeoutTime = _timeout.Value;
                var entity = Entity.Resolve(_target);
                var randomDirection = new Vector2(MathUtils.PlusMinus(), MathUtils.PlusMinus()) * _dist;
                host.Parent.AddEntity(entity, host.Position + randomDirection);

                if (timeoutTime != 0)
                    Manager.AddTimedAction(timeoutTime * 1000, () =>
                    {
                        try
                        {
                            entity.Parent?.RemoveEntity(entity);
                        }
                        catch
                        //couldn't remove portal, Owner became null. Should be fixed with RealmManager implementation
                        {
                            Console.WriteLine("Couldn't despawn portal.");
                        }
                    });
            }
        }

    }
}
