namespace FluentCMS.DataSeeder;

/// <summary>
/// Exception thrown when an error occurs during the seeding process
/// </summary>
public class SeedingException : Exception
{
    public SeedingException() : base()
    {
    }

    public SeedingException(string message) : base(message)
    {
    }

    public SeedingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
