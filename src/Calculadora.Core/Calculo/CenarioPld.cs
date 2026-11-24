using Calculadora.Core.Dominio;

namespace Calculadora.Core.Calculo;

public enum TipoCenario
{
    /// <summary>Usa o PLD publicado pela CCEE.</summary>
    Real,
    /// <summary>Todas as horas no PLD mínimo.</summary>
    Minimo,
    /// <summary>Todas as horas no teto horário.</summary>
    TetoHorario,
    /// <summary>Um valor fixo informado pelo usuário.</summary>
    Fixo,
    /// <summary>PLD real ajustado por um percentual (ex.: +20 ou -15).</summary>
    Percentual
}

public sealed record CenarioPld(TipoCenario Tipo, decimal Parametro = 0m)
{
    public static CenarioPld Real { get; } = new(TipoCenario.Real);

    /// <summary>
    /// Aplica o cenário às horas informadas, respeitando os limites (RN05).
    /// Devolve a nova série e os avisos gerados.
    /// </summary>
    public (IReadOnlyList<PldHora> Serie, IReadOnlyList<string> Avisos) Aplicar(
        IReadOnlyList<PldHora> horas, LimitesPld limites)
    {
        var avisos = new List<string>();

        decimal Valor(PldHora h) => Tipo switch
        {
            TipoCenario.Real => h.Valor,
            TipoCenario.Minimo => limites.Minimo,
            TipoCenario.TetoHorario => limites.MaximoHorario,
            TipoCenario.Fixo => Parametro,
            TipoCenario.Percentual => h.Valor * (1 + Parametro / 100m),
            _ => throw new ArgumentOutOfRangeException(nameof(Tipo))
        };

        var ajustadas = 0;
        var serie = horas.Select(h =>
        {
            var bruto = Valor(h);
            if (limites.ForaDosLimites(bruto)) ajustadas++;
            return h with { Valor = limites.Limitar(bruto) };
        }).ToList();

        if (ajustadas > 0)
            avisos.Add($"{ajustadas} hora(s) ajustada(s) aos limites do PLD de {limites.Ano} " +
                       $"(R$ {limites.Minimo:N2} a R$ {limites.MaximoHorario:N2}/MWh).");

        var diasAcimaEstrutural = serie
            .GroupBy(h => h.Hora.Date)
            .Count(d => d.Average(h => h.Valor) > limites.MaximoEstrutural);

        if (diasAcimaEstrutural > 0)
            avisos.Add($"{diasAcimaEstrutural} dia(s) com média acima do PLD máximo estrutural " +
                       $"(R$ {limites.MaximoEstrutural:N2}/MWh). Na liquidação real, a média diária seria limitada.");

        return (serie, avisos);
    }
}
