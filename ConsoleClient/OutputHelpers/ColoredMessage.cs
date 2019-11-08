using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleClient.OutputHelpers
{
    public struct ColoredMessage
    {
        public Color Color { get; }
        public string Message { get; }

        public ColoredMessage(string message, Color color)
        {
            Message = message;
            Color = color;
        }

        public ColoredMessage(string message) : this(message, Color.Default) { }
    }
}
