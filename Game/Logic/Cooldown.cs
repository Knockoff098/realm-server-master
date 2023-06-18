﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RotMG.Game.Logic
{
    public struct Cooldown
    {
        public int CoolDown;
        public int Variance;
        public Cooldown(int cooldown, int variance)
        {
            this.CoolDown = cooldown;
            this.Variance = variance;
        }

        public Cooldown Normalize()
        {
            if (CoolDown == 0)
                return 1000;
            else
                return this;
        }

        public Cooldown Zero()
        {
            if (CoolDown == 0)
                return -1;
            else
                return this;
        }

        public Cooldown Normalize(int def)
        {
            if (CoolDown == 0)
                return def;
            else
                return this;
        }

        public int Next(Random rand)
        {
            if (Variance == 0)
                return CoolDown;

            return CoolDown + rand.Next(-Variance, Variance + 1);
        }

        public static implicit operator Cooldown(int cooldown)
        {
            return new Cooldown(cooldown, 0);
        }
    }
}
