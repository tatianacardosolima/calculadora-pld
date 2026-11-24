namespace Calculadora.Core.Dominio;

/// <summary>Submercados do SIN usados na formação do PLD.</summary>
public enum Submercado { SECO, S, NE, N }

public enum TipoContrato { Compra, Venda }

/// <summary>PLD de uma hora em um submercado (R$/MWh).</summary>
public sealed record PldHora(DateTime Hora, decimal Valor);

/// <summary>
/// Contrato com volume flat em MWm, válido de <see cref="Inicio"/> a <see cref="Fim"/> (inclusive).
/// </summary>
public sealed record Contrato(
    string Nome,
    TipoContrato Tipo,
    decimal VolumeMWm,
    decimal PrecoMWh,
    DateOnly Inicio,
    DateOnly Fim)
{
    public bool VigenteEm(DateTime hora)
    {
        var dia = DateOnly.FromDateTime(hora);
        return dia >= Inicio && dia <= Fim;
    }

    /// <summary>Volume da hora em MWh, com sinal: compra positiva, venda negativa.</summary>
    public decimal VolumeComSinalMWh(DateTime hora) =>
        !VigenteEm(hora) ? 0m : Tipo == TipoContrato.Compra ? VolumeMWm : -VolumeMWm;
}

/// <summary>Consumo medido (ou previsto) de uma hora, em MWh.</summary>
public sealed record ConsumoHora(DateTime Hora, decimal MWh);

public sealed record ResultadoHora(
    DateTime Hora,
    decimal Pld,
    decimal ConsumoMWh,
    decimal ContratadoMWh,
    decimal ExposicaoMWh,
    decimal ResultadoReais);

public sealed record ResumoSimulacao(
    IReadOnlyList<ResultadoHora> Horas,
    decimal ExposicaoTotalMWh,
    decimal ResultadoMcpReais,
    decimal CustoContratosReais,
    decimal CustoTotalReais,
    int HorasEmDebito,
    int HorasEmCredito,
    IReadOnlyList<string> Avisos)
{
    /// <summary>Custo médio da energia consumida (R$/MWh); zero se não houve consumo.</summary>
    public decimal CustoMedioMWh(decimal consumoTotalMWh) =>
        consumoTotalMWh == 0 ? 0 : CustoTotalReais / consumoTotalMWh;
}
