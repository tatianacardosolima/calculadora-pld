namespace Calculadora.Core.Dominio;

/// <summary>
/// Limites regulatórios do PLD definidos anualmente pela ANEEL (RN05).
/// Os valores ficam em wwwroot/config/limites.json; <see cref="Ano2026"/> é só o padrão de fallback.
/// </summary>
public sealed record LimitesPld(int Ano, decimal Minimo, decimal MaximoEstrutural, decimal MaximoHorario)
{
    /// <summary>Despacho ANEEL 3.850/2025, vigente de 01/01/2026 a 31/12/2026.</summary>
    public static LimitesPld Ano2026 { get; } = new(2026, 57.31m, 785.27m, 1611.04m);

    /// <summary>Limita um PLD horário ao intervalo [mínimo, máximo horário].</summary>
    public decimal Limitar(decimal pld) => Math.Clamp(pld, Minimo, MaximoHorario);

    public bool ForaDosLimites(decimal pld) => pld < Minimo || pld > MaximoHorario;
}
