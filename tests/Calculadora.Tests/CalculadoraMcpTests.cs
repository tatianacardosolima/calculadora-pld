using Calculadora.Core.Calculo;
using Calculadora.Core.Dominio;

namespace Calculadora.Tests;

public class CalculadoraMcpTests
{
    private static readonly DateOnly Dia = new(2026, 11, 1);

    private static List<PldHora> UmDia(decimal pld) =>
        Enumerable.Range(0, 24)
            .Select(h => new PldHora(Dia.ToDateTime(new TimeOnly(h, 0)), pld))
            .ToList();

    private static Contrato Compra(decimal mwm, decimal preco = 200m) =>
        new("Compra", TipoContrato.Compra, mwm, preco, Dia, Dia);

    [Fact] // US01, critério 1
    public void Subcontratado_paga_a_diferenca_ao_PLD()
    {
        var pld = UmDia(100m);
        var consumo = CalculadoraMcp.PerfilFlat(pld, 10m);

        var r = CalculadoraMcp.Calcular(pld, consumo, [Compra(8m)]);

        Assert.Equal(-48m, r.ExposicaoTotalMWh);
        Assert.Equal(-4_800m, r.ResultadoMcpReais);
        Assert.Equal(24, r.HorasEmDebito);
        Assert.Equal(0, r.HorasEmCredito);
    }

    [Fact] // US01, critério 2
    public void Consumo_igual_ao_contrato_zera_exposicao()
    {
        var pld = UmDia(350m);
        var consumo = CalculadoraMcp.PerfilFlat(pld, 8m);

        var r = CalculadoraMcp.Calcular(pld, consumo, [Compra(8m)]);

        Assert.Equal(0m, r.ExposicaoTotalMWh);
        Assert.Equal(0m, r.ResultadoMcpReais);
    }

    [Fact]
    public void Sobrecontratado_recebe()
    {
        var pld = UmDia(100m);
        var consumo = CalculadoraMcp.PerfilFlat(pld, 5m);

        var r = CalculadoraMcp.Calcular(pld, consumo, [Compra(8m)]);

        Assert.Equal(72m, r.ExposicaoTotalMWh);
        Assert.Equal(7_200m, r.ResultadoMcpReais);
    }

    [Fact] // RN04
    public void Custo_total_soma_contratos_e_debito_no_MCP()
    {
        var pld = UmDia(100m);
        var consumo = CalculadoraMcp.PerfilFlat(pld, 10m);

        var r = CalculadoraMcp.Calcular(pld, consumo, [Compra(8m, preco: 200m)]);

        Assert.Equal(38_400m, r.CustoContratosReais); // 192 MWh × 200
        Assert.Equal(43_200m, r.CustoTotalReais);     // 38.400 + 4.800
    }

    [Fact]
    public void Contrato_de_venda_reduz_o_contratado()
    {
        var pld = UmDia(100m);
        var consumo = CalculadoraMcp.PerfilFlat(pld, 0m);
        var venda = new Contrato("Venda", TipoContrato.Venda, 2m, 180m, Dia, Dia);

        var r = CalculadoraMcp.Calcular(pld, consumo, [Compra(5m), venda]);

        Assert.Equal(72m, r.ExposicaoTotalMWh); // (5 − 2) × 24
    }

    [Fact]
    public void Contrato_fora_da_vigencia_nao_conta()
    {
        var pld = UmDia(100m);
        var consumo = CalculadoraMcp.PerfilFlat(pld, 10m);
        var vencido = new Contrato("Vencido", TipoContrato.Compra, 10m, 200m, Dia.AddDays(-10), Dia.AddDays(-1));

        var r = CalculadoraMcp.Calcular(pld, consumo, [vencido]);

        Assert.Equal(-240m, r.ExposicaoTotalMWh);
    }

    [Fact]
    public void Falta_de_consumo_em_alguma_hora_gera_erro()
    {
        var pld = UmDia(100m);
        var consumo = CalculadoraMcp.PerfilFlat(pld.Take(23), 10m);

        Assert.Throws<ArgumentException>(() => CalculadoraMcp.Calcular(pld, consumo, []));
    }
}
