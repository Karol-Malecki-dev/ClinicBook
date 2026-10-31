using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Infrastructure.Services.Clinic;

internal static class ClinicTransactionLock
{
    public static async Task<IDbContextTransaction?> BeginAsync(
        ApplicationDbContext dbContext,
        Guid clinicId,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        if (!dbContext.Database.IsRelational())
        {
            return null;
        }

        var transaction = await dbContext.Database.BeginTransactionAsync(isolationLevel);
        try
        {
            if (dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT 1 FROM \"Clinics\" WHERE \"Id\" = {clinicId} FOR UPDATE");
            }

            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }
}
