using Calculadora.Core.Calculo;
using Calculadora.Core.Dominio;

namespace Calculadora.Tests;

public class CenarioPldTests
{
    private static readonly LimitesPld Limites = LimitesPld.Ano2026;

    private static List<PldHora> UmDia(decimal pld) =>
        Enumerable.Range(0, 24)
            .Select(h => new PldHora(new DateTime(2026, 11, 1, h, 0, 0), pld))
            .ToList();

    [Fact] // US04, critério 1
    public void Teto_horario_usa_o_maximo_e_avisa_sobre_o_estrutural()
    {
        var (serie, avisos) = new CenarioPld(TipoCenario.TetoHorario).Aplicar(UmDia(300m), Limites);

        Assert.All(serie, h => Assert.Equal(1611.04m, h.Valor));
        Assert.Contains(avisos, a => a.Contains("máximo estrutural"));
    }

    [Fact] // US04, critério 2
    public void Valor_fixo_abaixo_do_minimo_e_ajustado()
    {
        var (serie, avisos) = new CenarioPld(TipoCenario.Fixo, 10m).Aplicar(UmDia(300m), Limites);

        Assert.All(serie, h => Assert.Equal(57.31m, h.Valor));
        Assert.Single(avisos);
    }

    [Fact]
    public void Percentual_aplica_sobre_o_PLD_real()
    {
        var (serie, avisos) = new CenarioPld(TipoCenario.Percentual, 20m).Aplicar(UmDia(100m), Limites);

        Assert.All(serie, h => Assert.Equal(120m, h.Valor));
        Assert.Empty(avisos);
    }

    [Fact]
    public void Cenario_real_nao_altera_valores_validos()
    {
        var original = UmDia(250m);
        var (serie, avisos) = CenarioPld.Real.Aplicar(original, Limites);

        Assert.Equal(original, serie);
        Assert.Empty(avisos);
    }
}
