using System.Globalization;
using System.Net.Http.Json;
using Calculadora.Core.Dominio;

namespace Calculadora.Web.Servicos;

/// <summary>Lê os arquivos estáticos gerados pela GitHub Action (wwwroot/data e wwwroot/config).</summary>
public sealed class DadosPld(HttpClient http)
{
    public sealed record ItemIndice(string Mes, string Submercado, string Arquivo);
    public sealed record Indice(string? AtualizadoEm, List<ItemIndice> Arquivos);
    public sealed record HoraJson(string H, decimal Pld);
    public sealed record ArquivoPld(string Submercado, string Mes, string? AtualizadoEm, string? Fonte, List<HoraJson> Horas);
    private sealed record LimitesJson(int Ano, decimal Minimo, decimal MaximoEstrutural, decimal MaximoHorario);

    private Indice? _indice;
    private List<LimitesPld>? _limites;

    public async Task<Indice> ObterIndiceAsync()
    {
        if (_indice is not null) return _indice;
        try
        {
            _indice = await http.GetFromJsonAsync<Indice>("data/pld/index.json") ?? new Indice(null, []);
        }
        catch (HttpRequestException)
        {
            _indice = new Indice(null, []); // ainda não há dados publicados
        }
        return _indice;
    }

    public async Task<LimitesPld> ObterLimitesAsync(int ano)
    {
        if (_limites is null)
        {
            try
            {
                var lista = await http.GetFromJsonAsync<List<LimitesJson>>("config/limites.json") ?? [];
                _limites = lista.Select(l => new LimitesPld(l.Ano, l.Minimo, l.MaximoEstrutural, l.MaximoHorario)).ToList();
            }
            catch (HttpRequestException)
            {
                _limites = [];
            }
        }

        return _limites.FirstOrDefault(l => l.Ano == ano)
               ?? _limites.OrderByDescending(l => l.Ano).FirstOrDefault()
               ?? LimitesPld.Ano2026;
    }

    /// <summary>Devolve o PLD horário do mês/submercado, ou null se o arquivo ainda não existe.</summary>
    public async Task<(IReadOnlyList<PldHora> Horas, ArquivoPld Arquivo)?> ObterPldAsync(string mes, Submercado sub)
    {
        var indice = await ObterIndiceAsync();
        var item = indice.Arquivos.FirstOrDefault(a => a.Mes == mes && a.Submercado == sub.ToString());
        if (item is null) return null;

        var arquivo = await http.GetFromJsonAsync<ArquivoPld>($"data/pld/{item.Arquivo}");
        if (arquivo is null) return null;

        var horas = arquivo.Horas
            .Select(h => new PldHora(DateTime.ParseExact(h.H, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture), h.Pld))
            .OrderBy(h => h.Hora)
            .ToList();

        return (horas, arquivo);
    }

    /// <summary>Todas as horas de um mês, para cenários sem dados da CCEE.</summary>
    public static IReadOnlyList<PldHora> HorasDoMes(string mes)
    {
        var inicio = DateTime.ParseExact(mes + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var total = DateTime.DaysInMonth(inicio.Year, inicio.Month) * 24;
        return Enumerable.Range(0, total).Select(i => new PldHora(inicio.AddHours(i), 0m)).ToList();
    }
}
