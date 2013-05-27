using System;

namespace MaxSvett.Memory
{
    internal class InvalidAccessException : Exception
    {
        public InvalidAccessException(string message)
            : base(message)
        {
        }
    }
}
