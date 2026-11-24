using Calculadora.Core.Calculo;

namespace Calculadora.Tests;

public class LeitorCurvaCsvTests
{
    [Fact] // US03, critério 1
    public void Le_curva_com_cabecalho_e_virgula_decimal()
    {
        const string csv = "data;mwh\n2026-11-01 00:00;10,5\n2026-11-01 01:00;9,75\n";

        var r = LeitorCurvaCsv.Ler(csv);

        Assert.True(r.Valido);
        Assert.Equal(2, r.Horas.Count);
        Assert.Equal(10.5m, r.Horas[0].MWh);
    }

    [Fact] // US03, critério 2
    public void Aponta_linhas_com_erro()
    {
        const string csv = "2026-11-01 00:00;10\n2026-11-01 xx;9\n2026-11-01 02:00;-1\n";

        var r = LeitorCurvaCsv.Ler(csv);

        Assert.False(r.Valido);
        Assert.Equal(2, r.Erros.Count);
        Assert.Contains(r.Erros, e => e.StartsWith("Linha 2"));
        Assert.Contains(r.Erros, e => e.StartsWith("Linha 3"));
    }

    [Fact]
    public void Detecta_hora_repetida()
    {
        const string csv = "2026-11-01 00:00;10\n2026-11-01 00:00;11\n";

        var r = LeitorCurvaCsv.Ler(csv);

        Assert.False(r.Valido);
        Assert.Contains(r.Erros, e => e.StartsWith("Hora repetida"));
    }
}
