using RotMG.Game.Logic.Behaviors;
using RotMG.Game.Logic.Transitions;
using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game.Logic.Database
{
    class Pets : IBehaviorDatabase
    {
        public void Init(BehaviorDb db)
        {

            db.Init("Panda", new State("base",
                               
                               new Prioritize(
                                       new Charge(0.7, 8, 500),
                                       new Wander(0.3f)
                                   )
                           ));

            db.Init("Raven", new PetFollow(
                    1f, range: 2
                ));

            db.Init("Bee", new PetFollow(
                    1f, range: 2
                ));

            db.Init("Baby Dragon", new PetFollow(
                    1f, range: 2
                ));

            db.Init("Demon Frog", new PetFollow(
                    1f, range: 2
                ));

            db.Init("Blue Snail", new PetFollow(
                1f, range: 2
            ));

        }
    }
}
