# Calculadora de PLD e Exposição no MCP

Calculadora didática que mostra quanto um agente paga ou recebe no Mercado de Curto Prazo (MCP) a partir do consumo, dos contratos e do PLD horário do submercado. Site 100% estático, publicado no GitHub Pages.

> Cálculo simplificado para estudo. Não substitui a contabilização oficial da CCEE (não considera perdas, MRE, sazonalização, modulação, encargos e penalidades).

## Estrutura

```
src/Calculadora.Core      motor de cálculo (C#, sem dependência de UI)
src/Calculadora.Web       Blazor WebAssembly
tests/Calculadora.Tests   xUnit com os critérios de aceite das user stories
scripts/baixar_pld.py     baixa o PLD horário dos Dados Abertos da CCEE
.github/workflows         deploy-pages.yml (testa e publica) e pld-diario.yml (atualiza dados)
```

## Rodar localmente

Requisitos: .NET SDK 10 e Python 3.12 (só para o script de dados).

```bash
dotnet test                      # roda os testes
dotnet run --project src/Calculadora.Web   # abre em http://localhost:5xxx
```

## Publicar no GitHub Pages

1. Crie o repositório no GitHub (ex.: `calculadora-pld`) e faça o push da branch `main`.
2. Em **Settings → Pages → Build and deployment**, escolha **Source: GitHub Actions**.
3. O workflow **Testar e publicar no GitHub Pages** roda a cada push na `main`.
4. O site fica em `https://<seu-usuario>.github.io/calculadora-pld/`.

## Dados de PLD

O workflow **Atualizar PLD diário** roda às 20:40 (Brasília), baixa o mês atual e o anterior, grava em `src/Calculadora.Web/wwwroot/data/pld/` e republica o site. Também pode ser disparado à mão em **Actions → Atualizar PLD diário → Run workflow**, informando um mês (AAAA-MM).

Enquanto não houver dados, a calculadora funciona com os cenários de PLD fixo, mínimo e teto.

> O nome do dataset (`pld_horario`) e das colunas da API da CCEE precisam ser confirmados na primeira execução. Se o job falhar, ajuste `CCEE_DATASET` e as listas `COLUNAS_*` em `scripts/baixar_pld.py`.

## Limites do PLD

Ficam em `src/Calculadora.Web/wwwroot/config/limites.json`. Atualize todo ano após o despacho da ANEEL (2026: Despacho 3.850/2025).

## Especificação

Requisitos, regras de negócio (RN01–RN08), user stories e arquitetura estão no documento de especificação do projeto.
