﻿using System;
using System.Collections.Generic;
using Nuve.Morphologic.Structure;
using Nuve.Orthographic;

namespace Nuve.Orthographic
{
    public enum Order { One=1, Two=2, Three=3, Four=4, Fife } ;

    public class OrthographyRule
    {
        private readonly string _id;
        private readonly Order _type;
        private readonly List<Transformation> _transformations;

        public Order Type { get { return _type; } }
        public string Id { get { return _id; } }

        public bool IsLeftRule { get { return _type == Order.Two; } }
        public bool IsRightRule { get { return _type == Order.Three; } }
        public bool IsSelfRule { get { return _type == Order.Four; } }
        public bool IsDefaultRule { get { return _type == Order.Fife; } }

        public OrthographyRule(string id, int order, List<Transformation> transformations)
        {
            _type = (Order) order;
            _id = id;
            //if (!Enum.TryParse(direction, out _type))
            //{
            //    throw new ArgumentException("Invalid Orthography Rule direction: " + direction);
            //}
            _transformations = transformations;
        }

        public void Process(Allomorph allomorph)
        {
            foreach (Transformation transformation in _transformations)
            {
                if (transformation.Condition.IsTrue(allomorph))
                {
                    transformation.Transform(allomorph);
                    break;
                }
            }
        }

        public override string ToString()
        {
            return Id;
        }
    }
}
