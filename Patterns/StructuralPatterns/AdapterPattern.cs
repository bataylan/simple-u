using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.Pattern.Adapter
{
    //known as Wrapper, middle class serves as translator
    public class ExampleUsage
    {
        public void Run()
        {
            var roundHole = new RoundHole();
            roundHole.radius = 5;

            var roundPeg = new RoundPeg();
            roundPeg.radius = 4;
            roundHole.CheckFit(roundPeg);

            var squarePeg = new SquarePeg();
            squarePeg.width = 4;
            //cant use square peg, since method expect round peg
            // roundHole.CheckFit(squarePeg);
            var squarePegAdapter = new SquarePegAdapter(squarePeg);
            roundHole.CheckFit(squarePegAdapter);
        }
    }

    //client, require radius
    public class RoundHole
    {
        public float radius;

        public bool CheckFit(IRoundPeg roundPeg)
        {
            return roundPeg.Radius <= radius;
        }
    }

    public interface IRoundPeg
    {
        public float Radius { get; }
    }

    public class RoundPeg : IRoundPeg
    {
        public float radius;
        public float Radius => radius;
    }

    //has no radius
    public class SquarePeg
    {
        public float width;
    }

    //pretened as it is round
    public class SquarePegAdapter : IRoundPeg
    {
        public readonly SquarePeg squarePeg;

        public SquarePegAdapter(SquarePeg squarePeg)
        {
            this.squarePeg = squarePeg;
        }

        public float Radius
        {
            get
            {
                return squarePeg.width * ((float)Math.Sqrt(2) / 2);
            }
        }
    }
}
