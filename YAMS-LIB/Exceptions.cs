namespace YAMS_LIB;

class InvalidAM2RVersionException : Exception
{
    public InvalidAM2RVersionException()
    {
    }

    public InvalidAM2RVersionException(string message)
        : base(message)
    {
    }

    public InvalidAM2RVersionException(string message, Exception inner)
        : base(message, inner)
    {
    }
}