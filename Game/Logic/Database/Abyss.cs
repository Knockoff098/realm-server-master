using RotMG.Game.Logic.Behaviors;
using RotMG.Game.Logic.Loots;
using RotMG.Game.Logic.Transitions;
using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game.Logic.Database
{
    class Abyss : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {

            db.Init("Brute of the Abyss", new State("base",
                    new Shoot(16, 3, shootAngle: 30, cooldown: 300),
                    new Prioritize(
                            new Follow(1.5f, 7, 2),
                            new Wander(0.2f)
                        )
                ));

            db.Init("Brute Warrior of the Abyss", new State("base",
                    new Shoot(7, 1, cooldown: 200, fixedAngle: 0f, rotateAngle: 45f/2),
                    new Prioritize(
                            new Charge(0.7, 8, 500),
                            new Wander(0.3f)
                        )
                ));

            db.Init("Demon Mage of the Abyss", new State("base",
                new Shoot(10, count: 8, fixedAngle: 360 / 8, cooldown: 3000, index: 1),
                    new Prioritize(
                            new Wander(0.2f)
                        )
                ));

            db.Init("Demon Warrior of the Abyss", new State("base",
                    new Shoot(13, 1, shootAngle: 30, cooldown: 800),
                    new Grenade(10, 50, 3, cooldown: 250),
                    new Prioritize(
                            new Orbit(1.2f, 4, radiusVariance: 3),
                            new Wander(0.2f)
                        )
                ));

            db.Init("Demon of the Abyss", new State("base",
                    new Shoot(13, 1, shootAngle: 30, cooldown: 1000),
                    new Grenade(10, 75, 5, cooldown: 1200),
                    new Prioritize(
                            new Follow(1.5f, 5, 4),
                            new Wander(0.2f)
                        )
                ));

            db.Init("Imp of the Abyss", new State("base",
                    new Shoot(13, 1, shootAngle: 22, cooldown: 750),
                    new Prioritize(
                            new Follow(1.5f, 5, 4),
                            new Wander(0.2f)
                        )
                ));

            db.Init("Malphas Protector",
                new State("base",
                    new Grenade(10, 50, 3, cooldown: 250),
                    new Prioritize(
                            new Wander(1f)
                        ),
                    new TimedTransition("Slam", 3500)
                ),
                new State("Slam",
                new Shoot(13, 1, shootAngle: 22, cooldown: 750),
                new Charge(1.5f, 15, coolDown: 100),
                new TimedTransition("base", 700)
                    
                ));

         



        }
    }
}
