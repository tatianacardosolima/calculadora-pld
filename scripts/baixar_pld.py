"""Baixa o PLD horário dos Dados Abertos da CCEE e grava um JSON por mês e submercado.

Uso:
    python scripts/baixar_pld.py               # mês atual e anterior
    python scripts/baixar_pld.py --mes 2026-10 # um mês específico

A API da CCEE é baseada em CKAN. Nome do dataset e das colunas podem mudar:
ajuste CCEE_DATASET ou as listas COLUNAS_* abaixo se o job falhar.
"""

from __future__ import annotations

import argparse
import json
import os
import sys
from datetime import date, datetime, timezone, timedelta
from pathlib import Path

import requests

API = os.environ.get("CCEE_API", "https://dadosabertos.ccee.org.br/api/3/action")
DATASET = os.environ.get("CCEE_DATASET", "pld_horario")
DESTINO = Path(__file__).resolve().parents[1] / "src" / "Calculadora.Web" / "wwwroot" / "data" / "pld"
LIMITES = Path(__file__).resolve().parents[1] / "src" / "Calculadora.Web" / "wwwroot" / "config" / "limites.json"
MESES_MANTIDOS = 24
FUSO_BR = timezone(timedelta(hours=-3))

# Nomes possíveis das colunas (o primeiro encontrado é usado).
COLUNAS_MES = ["MES_REFERENCIA", "mes_referencia"]
COLUNAS_SUBMERCADO = ["SUBMERCADO", "submercado"]
COLUNAS_DIA = ["DIA", "dia", "DATA", "data"]
COLUNAS_HORA = ["HORA", "hora"]
COLUNAS_PLD = ["PLD_HORA", "PLD", "pld_hora", "pld"]

SUBMERCADOS = {"SUDESTE": "SECO", "SECO": "SECO", "SE/CO": "SECO", "SUL": "S", "S": "S",
               "NORDESTE": "NE", "NE": "NE", "NORTE": "N", "N": "N"}


def coluna(registro: dict, candidatas: list[str]) -> str:
    for c in candidatas:
        if c in registro:
            return c
    raise KeyError(f"Nenhuma das colunas {candidatas} encontrada. Colunas recebidas: {list(registro)}")


def chamar(acao: str, **params) -> dict:
    resp = requests.get(f"{API}/{acao}", params=params, timeout=60)
    resp.raise_for_status()
    corpo = resp.json()
    if not corpo.get("success"):
        raise RuntimeError(f"CKAN {acao} falhou: {corpo.get('error')}")
    return corpo["result"]


def recurso_do_ano(ano: int) -> str:
    pacote = chamar("package_show", id=DATASET)
    for r in pacote["resources"]:
        texto = f"{r.get('name', '')} {r.get('url', '')}"
        if str(ano) in texto and r.get("datastore_active", True):
            return r["id"]
    raise LookupError(f"Recurso do ano {ano} não encontrado no dataset '{DATASET}'.")


def baixar_mes(ano: int, mes: int) -> list[dict]:
    recurso = recurso_do_ano(ano)
    registros, offset, lote = [], 0, 5000
    while True:
        res = chamar("datastore_search", resource_id=recurso,
                     filters=json.dumps({"MES_REFERENCIA": f"{ano}{mes:02d}"}),
                     limit=lote, offset=offset)
        registros += res["records"]
        offset += lote
        if offset >= res.get("total", 0):
            return registros


def interpretar_dia(valor) -> date:
    texto = str(valor)[:10]
    for formato in ("%Y-%m-%d", "%d/%m/%Y", "%Y%m%d"):
        try:
            return datetime.strptime(texto, formato).date()
        except ValueError:
            continue
    raise ValueError(f"Dia em formato desconhecido: {valor!r}")


def agrupar(registros: list[dict]) -> dict[str, list[dict]]:
    if not registros:
        return {}
    exemplo = registros[0]
    c_sub, c_dia = coluna(exemplo, COLUNAS_SUBMERCADO), coluna(exemplo, COLUNAS_DIA)
    c_hora, c_pld = coluna(exemplo, COLUNAS_HORA), coluna(exemplo, COLUNAS_PLD)

    por_sub: dict[str, list[dict]] = {}
    for r in registros:
        sub = SUBMERCADOS.get(str(r[c_sub]).strip().upper())
        if sub is None:
            continue
        hora = datetime.combine(interpretar_dia(r[c_dia]), datetime.min.time()) + timedelta(hours=int(r[c_hora]))
        valor = float(str(r[c_pld]).replace(",", "."))
        por_sub.setdefault(sub, []).append({"h": hora.strftime("%Y-%m-%dT%H:%M"), "pld": round(valor, 2)})

    for horas in por_sub.values():
        horas.sort(key=lambda x: x["h"])
    return por_sub


def validar(horas: list[dict], ano: int) -> list[str]:
    avisos = []
    limites = {l["ano"]: l for l in json.loads(LIMITES.read_text(encoding="utf-8"))}.get(ano)
    if limites:
        fora = [h for h in horas if not limites["minimo"] <= h["pld"] <= limites["maximoHorario"]]
        if fora:
            avisos.append(f"{len(fora)} hora(s) fora dos limites de {ano}")
    if len({h["h"] for h in horas}) != len(horas):
        avisos.append("horas duplicadas")
    return avisos


def gravar(mes_ref: str, sub: str, horas: list[dict]) -> None:
    DESTINO.mkdir(parents=True, exist_ok=True)
    conteudo = {
        "submercado": sub,
        "mes": mes_ref,
        "atualizadoEm": datetime.now(FUSO_BR).strftime("%d/%m/%Y %H:%M"),
        "fonte": "CCEE Dados Abertos",
        "horas": horas,
    }
    (DESTINO / f"{mes_ref}_{sub}.json").write_text(
        json.dumps(conteudo, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")


def atualizar_indice() -> None:
    arquivos = sorted(p for p in DESTINO.glob("*_*.json"))
    meses = sorted({p.stem.split("_")[0] for p in arquivos}, reverse=True)
    manter = set(meses[:MESES_MANTIDOS])
    for p in arquivos:
        if p.stem.split("_")[0] not in manter:
            p.unlink()
    indice = {
        "atualizadoEm": datetime.now(FUSO_BR).strftime("%d/%m/%Y %H:%M"),
        "arquivos": [{"mes": p.stem.split("_")[0], "submercado": p.stem.split("_")[1], "arquivo": p.name}
                     for p in sorted(DESTINO.glob("*_*.json")) if p.stem.split("_")[0] in manter],
    }
    (DESTINO / "index.json").write_text(json.dumps(indice, ensure_ascii=False, indent=2), encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--mes", help="AAAA-MM; se omitido, baixa o mês atual e o anterior")
    args = parser.parse_args()

    hoje = datetime.now(FUSO_BR).date()
    if args.mes:
        meses = [args.mes]
    else:
        anterior = (hoje.replace(day=1) - timedelta(days=1))
        meses = [anterior.strftime("%Y-%m"), hoje.strftime("%Y-%m")]

    erros = 0
    for mes_ref in meses:
        ano, mes = map(int, mes_ref.split("-"))
        try:
            por_sub = agrupar(baixar_mes(ano, mes))
        except Exception as ex:  # noqa: BLE001 - o job deve seguir para os outros meses
            print(f"[ERRO] {mes_ref}: {ex}", file=sys.stderr)
            erros += 1
            continue
        for sub, horas in por_sub.items():
            for aviso in validar(horas, ano):
                print(f"[AVISO] {mes_ref} {sub}: {aviso}")
            gravar(mes_ref, sub, horas)
            print(f"[OK] {mes_ref} {sub}: {len(horas)} horas")

    atualizar_indice()
    return 1 if erros == len(meses) else 0


if __name__ == "__main__":
    sys.exit(main())
