using MySqlConnector;

namespace FitLife.BlazorWebApp.Services;

public record RegistrationResult(bool Success, string? ErrorMessage = null);

public interface IRegistrationService
{
    Task<RegistrationResult> CreateMemberAsync(PaymentSession session);
}

public class RegistrationService : IRegistrationService
{
    private readonly IConfiguration _config;

    public RegistrationService(IConfiguration config) => _config = config;

    public async Task<RegistrationResult> CreateMemberAsync(PaymentSession session)
    {
        var connectionString = _config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            return new RegistrationResult(false, "Database verbinding niet geconfigureerd.");

        try
        {
            await using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();

            // Check if email already in use
            await using (var check = new MySqlCommand(
                "SELECT COUNT(*) FROM users WHERE email = @email", conn))
            {
                check.Parameters.AddWithValue("@email", session.Email);
                var count = Convert.ToInt64(await check.ExecuteScalarAsync());
                if (count > 0)
                    return new RegistrationResult(false,
                        "Dit e-mailadres is al geregistreerd. Probeer in te loggen.");
            }

            var credits = session.Plan switch
            {
                "Rookie"       => 9,
                "Intermediate" => 13,
                "Advanced"     => 999,   // Unlimited
                _              => 0
            };

            var renewalDate = session.IsYearly
                ? DateTime.UtcNow.AddYears(1)
                : DateTime.UtcNow.AddMonths(1);

            var displayName = $"{session.FirstName} {session.LastName}";

            const string sql = """
                INSERT INTO users
                    (email, password_hash, display_name, role,
                     credits, subscription_type, subscription_renewal_date)
                VALUES
                    (@email, @passwordHash, @displayName, 'member',
                     @credits, @subscriptionType, @renewalDate);
                """;

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@email",            session.Email);
            cmd.Parameters.AddWithValue("@passwordHash",     session.PasswordHash);
            cmd.Parameters.AddWithValue("@displayName",      displayName);
            cmd.Parameters.AddWithValue("@credits",          credits);
            cmd.Parameters.AddWithValue("@subscriptionType", session.Plan);
            cmd.Parameters.AddWithValue("@renewalDate",      renewalDate.Date);

            await cmd.ExecuteNonQueryAsync();
            return new RegistrationResult(true);
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return new RegistrationResult(false,
                "Dit e-mailadres is al geregistreerd.");
        }
        catch (Exception ex)
        {
            return new RegistrationResult(false,
                $"Registratie mislukt: {ex.Message}");
        }
    }
}
