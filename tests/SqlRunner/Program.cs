using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
// Utilitário de testes: lê SQL da entrada padrão; nunca faz parte da API publicada.
var connection = Environment.GetEnvironmentVariable("PIM_TEST_CONNECTION") ?? throw new InvalidOperationException("Configure PIM_TEST_CONNECTION para um banco de testes.");
var sql = await Console.In.ReadToEndAsync();
var resultados = new List<object>();
try
{
    await using var db = new SqlConnection(connection); await db.OpenAsync();
    foreach (var lote in Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(lote)) continue;
        await using var cmd = new SqlCommand(lote, db) { CommandTimeout = 90 };
        await using var reader = await cmd.ExecuteReaderAsync();
        do
        {
            var rows = new List<Dictionary<string, object?>>();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                for (var i=0;i<reader.FieldCount;i++) row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                rows.Add(row);
            }
            if (reader.FieldCount > 0) resultados.Add(rows);
        } while (await reader.NextResultAsync());
    }
    Console.WriteLine(JsonSerializer.Serialize(new { ok = true, resultados }));
}
catch (SqlException e) { Console.WriteLine(JsonSerializer.Serialize(new { ok = false, numero = e.Number, erro = e.Message })); Environment.ExitCode = 1; }
