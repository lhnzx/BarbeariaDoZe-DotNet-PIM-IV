using System.Security.Cryptography;

namespace BarbeariaDoZe.Negocio;

public static class SegurancaSenha
{
    private const int Iteracoes = 600_000;
    public static void Validar(string senha)
    {
        if (senha is null || senha.Length < 12 || senha.Length > 128)
            throw new NegocioException("A senha deve ter entre 12 e 128 caracteres.");
    }
    public static string CriarHash(string senha)
    {
        Validar(senha);
        var sal = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, sal, Iteracoes, HashAlgorithmName.SHA256, 32);
        return $"v1.{Iteracoes}.{Convert.ToBase64String(sal)}.{Convert.ToBase64String(hash)}";
    }
    public static bool Verificar(string senha, string? armazenado)
    {
        if (string.IsNullOrEmpty(senha) || senha.Length > 128 || armazenado is null) return false;
        try
        {
            var p = armazenado.Split('.');
            if (p.Length != 4 || p[0] != "v1" || !int.TryParse(p[1], out var n) || n < 600_000 || n > 2_000_000) return false;
            var esperado = Convert.FromBase64String(p[3]);
            var sal = Convert.FromBase64String(p[2]);
            if (esperado.Length != 32 || sal.Length != 16) return false;
            var calculado = Rfc2898DeriveBytes.Pbkdf2(senha, sal, n, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
        catch (FormatException) { return false; }
    }
}
