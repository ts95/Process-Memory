using System;

namespace Avaritis.Memory
{
    internal class InvalidAccessException : Exception
    {
        public InvalidAccessException(string message)
            : base(message)
        {
        }
    }
}
