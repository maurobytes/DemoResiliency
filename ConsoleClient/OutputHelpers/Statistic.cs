using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleClient.OutputHelpers
{
    public struct Statistic
    {
        public string Description { get; }

        public double Value { get; }

        public Color Color { get; }

        public Statistic(string description, double value, Color color)
        {
            Description = description;
            Value = value;
            Color = color;
        }
        public Statistic(string description, double value) : this(description, value, Color.Default) { }
    }
}
