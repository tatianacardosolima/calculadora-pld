using System.Globalization;
using Calculadora.Core.Dominio;

namespace Calculadora.Core.Calculo;

/// <summary>
/// Lê uma curva horária colada de planilha (US03). Formato por linha:
/// <c>2026-11-01 00:00;10,5</c> (separador ; ou tab; decimal com vírgula ou ponto).
/// </summary>
public static class LeitorCurvaCsv
{
    private static readonly string[] FormatosData =
        ["yyyy-MM-dd HH:mm", "yyyy-MM-ddTHH:mm", "dd/MM/yyyy HH:mm", "dd/MM/yyyy H:mm"];

    public sealed record Resultado(IReadOnlyList<ConsumoHora> Horas, IReadOnlyList<string> Erros)
    {
        public bool Valido => Erros.Count == 0 && Horas.Count > 0;
    }

    public static Resultado Ler(string texto)
    {
        var horas = new List<ConsumoHora>();
        var erros = new List<string>();
        var linhas = (texto ?? "").Split('\n', StringSplitOptions.TrimEntries);

        for (var i = 0; i < linhas.Length; i++)
        {
            var linha = linhas[i];
            if (linha.Length == 0) continue;

            var partes = linha.Split([';', '\t']);
            if (partes.Length < 2)
            {
                if (i == 0) continue; // provável cabeçalho
                erros.Add($"Linha {i + 1}: use 'data hora;MWh'.");
                continue;
            }

            if (!DateTime.TryParseExact(partes[0].Trim(), FormatosData, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var hora))
            {
                if (i == 0) continue; // cabeçalho
                erros.Add($"Linha {i + 1}: data/hora inválida '{partes[0].Trim()}'.");
                continue;
            }

            var numero = partes[1].Trim().Replace(',', '.');
            if (!decimal.TryParse(numero, NumberStyles.Number, CultureInfo.InvariantCulture, out var mwh) || mwh < 0)
            {
                erros.Add($"Linha {i + 1}: consumo inválido '{partes[1].Trim()}'.");
                continue;
            }

            horas.Add(new ConsumoHora(hora, mwh));
        }

        foreach (var dup in horas.GroupBy(h => h.Hora).Where(g => g.Count() > 1))
            erros.Add($"Hora repetida: {dup.Key:dd/MM/yyyy HH:mm}.");

        return new Resultado(horas, erros);
    }
}
