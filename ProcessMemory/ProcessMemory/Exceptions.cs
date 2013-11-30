using System;

namespace Avaritis.Memory
{
    [Serializable]
    internal class InvalidAccessException : Exception
    {
        public InvalidAccessException(string message)
            : base(message)
        {
        }
    }
}
