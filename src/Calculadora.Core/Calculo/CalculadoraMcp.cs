using Calculadora.Core.Dominio;

namespace Calculadora.Core.Calculo;

/// <summary>
/// Cálculo didático da exposição no Mercado de Curto Prazo (RN01 a RN04).
/// Não considera perdas, MRE, sazonalização, modulação nem encargos (RN08).
/// </summary>
public static class CalculadoraMcp
{
    public const string AvisoSimplificacao =
        "Cálculo simplificado para estudo. Não substitui a contabilização oficial da CCEE " +
        "(não considera perdas, MRE, sazonalização, modulação, encargos e penalidades).";

    /// <summary>RN01: exposição da hora = recursos − requisitos.</summary>
    public static decimal Exposicao(decimal contratadoLiquidoMWh, decimal consumoMWh) =>
        contratadoLiquidoMWh - consumoMWh;

    /// <summary>Gera um perfil flat (RN06): o mesmo MWh em cada hora da série de PLD.</summary>
    public static IReadOnlyList<ConsumoHora> PerfilFlat(IEnumerable<PldHora> horas, decimal consumoMWm) =>
        horas.Select(h => new ConsumoHora(h.Hora, consumoMWm)).ToList();

    public static ResumoSimulacao Calcular(
        IReadOnlyList<PldHora> pld,
        IReadOnlyList<ConsumoHora> consumo,
        IReadOnlyList<Contrato> contratos,
        IReadOnlyList<string>? avisosAnteriores = null)
    {
        ArgumentNullException.ThrowIfNull(pld);
        ArgumentNullException.ThrowIfNull(consumo);
        ArgumentNullException.ThrowIfNull(contratos);

        var consumoPorHora = consumo.ToDictionary(c => c.Hora, c => c.MWh);
        var faltando = pld.Count(h => !consumoPorHora.ContainsKey(h.Hora));
        if (faltando > 0)
            throw new ArgumentException($"Faltam {faltando} hora(s) de consumo para o período do PLD.", nameof(consumo));

        var horas = pld
            .OrderBy(h => h.Hora)
            .Select(h =>
            {
                var consumoMWh = consumoPorHora[h.Hora];
                var contratado = contratos.Sum(c => c.VolumeComSinalMWh(h.Hora));
                var exposicao = Exposicao(contratado, consumoMWh);
                return new ResultadoHora(h.Hora, h.Valor, consumoMWh, contratado, exposicao, exposicao * h.Valor); // RN02
            })
            .ToList();

        var resultadoMcp = horas.Sum(h => h.ResultadoReais); // RN03

        // RN04: compras são custo, vendas são receita.
        var custoContratos = pld.Sum(h => contratos.Sum(c => c.VolumeComSinalMWh(h.Hora) * c.PrecoMWh));

        return new ResumoSimulacao(
            Horas: horas,
            ExposicaoTotalMWh: horas.Sum(h => h.ExposicaoMWh),
            ResultadoMcpReais: resultadoMcp,
            CustoContratosReais: custoContratos,
            CustoTotalReais: custoContratos - resultadoMcp,
            HorasEmDebito: horas.Count(h => h.ResultadoReais < 0),
            HorasEmCredito: horas.Count(h => h.ResultadoReais > 0),
            Avisos: avisosAnteriores ?? []);
    }
}
