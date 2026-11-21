namespace IntegrationTests;

[AttributeUsage(AttributeTargets.Method)]
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable("CLINICBOOK_TEST_POSTGRES_CONNECTION_STRING")))
        {
            Skip = "Set CLINICBOOK_TEST_POSTGRES_CONNECTION_STRING to run PostgreSQL-only integration tests.";
        }
    }
}
