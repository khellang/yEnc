using System;

namespace yEnc
{
    public class YEncException : Exception
    {
        internal YEncException(string message) : base(message)
        {
        }

        internal YEncException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}