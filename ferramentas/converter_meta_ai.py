#!/usr/bin/env python3
"""Conversor único: catálogos .md do Meta AI -> fichas JSON no formato do importador (etapa 3).

Uso: python converter_meta_ai.py <Avioes.md> <Blindado.md> <pasta_saida>

Regras:
- fonte provisória (meta-ai-*), confiabilidade ESTIMADO em tudo;
- nada é inventado: o que a fonte não traz fica de fora e entra em "_pendencias";
- fichas que não passam no JSON Schema da categoria vão para <saida>/rejeitadas/.
"""
import json
import re
import sys
import unicodedata
from pathlib import Path

import jsonschema

RAIZ = Path(__file__).resolve().parent.parent
ESQUEMAS = RAIZ / "src/PoderBelico.Dados/Esquemas"

PAISES = {  # nome na fonte -> código ISO
    "EUA": "USA", "Brasil": "BRA", "Alemanha": "DEU", "Japão": "JPN", "Reino Unido": "GBR",
    "URSS": "SUN", "Rússia": "RUS", "China": "CHN", "França": "FRA", "Turquia": "TUR",
    "Irã": "IRN", "Portugal": "PRT", "Hungria": "HUN", "Israel": "ISR", "Suécia": "SWE",
    "Coreia do Sul": "KOR", "Índia": "IND", "Polônia": "POL", "Itália": "ITA", "Argentina": "ARG",
}
MESES = {m: i + 1 for i, m in enumerate("Jan Fev Mar Abr Mai Jun Jul Ago Set Out Nov Dez".split())}


def slug(nome: str) -> str:
    nome = nome.replace("+", " plus ")
    s = unicodedata.normalize("NFKD", nome).encode("ascii", "ignore").decode().lower()
    return re.sub(r"[^a-z0-9]+", "-", s).strip("-")


def num(s: str) -> float:
    return float(s.replace(".", "").replace(",", "."))


def primeiro_num(s: str, unidade: str):
    m = re.search(r"([\d.]+(?:,\d+)?)\+?\s*" + unidade, s)
    return num(m.group(1)) if m else None


def inteiro_ou_float(v):
    return int(v) if v is not None and float(v).is_integer() else v


def data_completa(s: str):
    m = re.search(r"(\d{1,2})\s+([A-Z][a-z]{2})\s+(\d{4})", s)
    if m and m.group(2) in MESES:
        return f"{m.group(3)}-{MESES[m.group(2)]:02d}-{int(m.group(1)):02d}"
    return None


def ano(s: str):
    m = re.search(r"(\d{4})", s)
    return int(m.group(1)) if m else None


def paises_em(texto: str):
    achados = []
    # "URSS/Rússia" não é operador composto: tratado à parte pelo chamador
    for nome, iso in PAISES.items():
        if re.search(r"(?<!\w)" + re.escape(nome) + r"(?!\w)", texto) and iso not in achados:
            achados.append(iso)
    return achados


# ---------------------------------------------------------------- aeronaves
def subtipo_aeronave(papel: str, secao: str):
    p = papel.lower()
    if secao == "Drones de Combate":
        return "nao_tripulada"
    if "helicóptero de ataque" in p or p.startswith("ataque /") or p == "ataque":
        return "helicoptero_de_ataque"
    if "helicóptero utilitário" in p or "transporte pesado" in p:
        return "helicoptero_utilitario"
    if "tiltrotor" in p or "transporte" in p:
        return "transporte"
    if "interceptador" in p:
        return "interceptador"
    if "reconhecimento" in p or "espionagem" in p:
        return "reconhecimento"
    if "bombardeiro" in p or "torpedeiro" in p:
        return "bombardeiro"
    if "ataque" in p:
        return "ataque_ao_solo"
    if "caça" in p:
        return "caca"
    return None


STATUS_APOSENTADO = {"F-105 Thunderchief", "F-8 Crusader", "A-7 Corsair II", "F-86 Sabre", "MiG-15"}
STATUS_ATIVO = {"B-52 Stratofortress", "C-130 Hercules", "Tu-95 Bear", "U-2 Dragon Lady"}

FONTE_AVIOES = {"slug": "meta-ai-avioes", "tipo": "SITE", "provisoria": True,
                "titulo": "Catálogo Aviões de Combate (gerado por IA, Meta AI) — fonte provisória"}
FONTE_BLINDADOS = {"slug": "meta-ai-blindados", "tipo": "SITE", "provisoria": True,
                   "titulo": "Catálogo Tanques & Blindados de Combate (gerado por IA, Meta AI) — fonte provisória"}


def parse_campos(linhas):
    campos = {}
    for ln in linhas:
        for m in re.finditer(r"\*\*([^*:]+):\*\*\s*(.*?)(?=\s*\|\s*\*\*|$)", ln.strip().lstrip("- ")):
            campos[m.group(1).strip()] = m.group(2).strip()
    return campos


def converter_aeronave(nome, secao, linhas, campos):
    pend = []
    primeira = linhas[0].lstrip("- ")
    partes = [x.strip() for x in re.sub(r"^\*\*[^*]+:\*\*\s*", "", primeira).split("•")]
    fabricante, pais_nome, papel = partes[0], partes[1], " • ".join(partes[2:])
    pais = PAISES.get(pais_nome.split("/")[0]) if "/" not in pais_nome else None
    if "/" in pais_nome:  # URSS/Rússia
        pais = "SUN"
        pend.append(f"país de origem '{pais_nome}' resolvido como SUN (pelo ano do primeiro voo); conferir")
    elif pais is None:
        pend.append(f"país '{pais_nome}' não mapeado")

    sub = subtipo_aeronave(papel, secao)
    if sub is None:
        pend.append(f"subtipo não mapeado para o papel '{papel}'")

    esp = {"subtipo": sub}
    tri = campos.get("Tripulação")
    if tri:
        esp["tripulacao"] = int(re.match(r"\d+", tri).group())
    else:
        pend.append("tripulação não informada (aeronave não tripulada: o schema exige tripulacao >= 1)")

    pv = campos.get("Primeiro voo") or campos.get("Primeiro voo orbital", "")
    d = data_completa(pv)
    if d:
        esp["data_primeiro_voo"] = d
    elif pv:
        pend.append(f"primeiro voo '{pv}' sem dia completo; só o ano foi usado")

    for chave, campo in (("comprimento_m", "Comprimento"), ("altura_m", "Altura")):
        if campo in campos:
            esp[chave] = inteiro_ou_float(primeiro_num(campos[campo], "m"))
    env = campos.get("Envergadura")
    if env:
        esp["envergadura_m"] = inteiro_ou_float(primeiro_num(env, "m"))
        if "(" in env:
            pend.append(f"envergadura variável na fonte ('{env}'); usado o primeiro valor")
    for campo in ("Diâmetro do rotor", "Diâmetro rotor"):
        if campo in campos:
            pend.append(f"{campo} ({campos[campo]}) sem campo no schema")

    mot = campos.get("Motores") or campos.get("Motor")
    if mot:
        m = re.match(r"(\d+)x\s+(.*?)(?:\s*•\s*(.*))?$", mot)
        if m:
            motor = {"quantidade": int(m.group(1)), "modelo": m.group(2).strip()}
            pot = m.group(3) or ""
            if (v := primeiro_num(pot, "lbf")) is not None:
                motor["empuxo_lbf"] = inteiro_ou_float(v)
            elif (v := primeiro_num(pot, "hp")) is not None and "shp" not in pot:
                motor["potencia_hp"] = inteiro_ou_float(v)
            elif pot:
                pend.append(f"potência '{pot}' não convertida (shp/outra unidade sem campo no schema)")
            esp["motores"] = motor
        else:
            pend.append(f"motor em formato não reconhecido: '{mot}'")

    vel = campos.get("Velocidade máxima") or campos.get("Velocidade")
    if vel and (v := primeiro_num(vel, "km/h")) is not None:
        esp["velocidade_max_kmh"] = inteiro_ou_float(v)
    if "Alcance" in campos and (v := primeiro_num(campos["Alcance"], "km")) is not None:
        esp["alcance_km"] = inteiro_ou_float(v)
    if "Teto" in campos and (v := primeiro_num(campos["Teto"], "m")) is not None:
        esp["teto_m"] = inteiro_ou_float(v)
    for campo in ("Autonomia", "Carga útil", "Carga"):
        if campo in campos:
            pend.append(f"{campo} ('{campos[campo]}') sem campo no schema")

    arm = campos.get("Armamento")
    if arm and not arm.lower().startswith("nenhum"):
        esp["armamento"] = [{"tipo": "armamento", "descricao": arm}]
        if arm.startswith("Baias") or "classificados" in arm:
            pend.append("armamento descrito como dado classificado")

    # operadores
    ops = []
    if "Operadores" in campos:
        ops = paises_em(campos["Operadores"])
        if re.search(r"\+|países|outros", campos["Operadores"]):
            pend.append(f"operadores adicionais não listados ('{campos['Operadores']}')")
    else:
        pend.append("operadores não informados")

    # unidades / serviço / status
    unidades = None
    if "Unidades" in campos:
        u = campos["Unidades"]
        m = re.match(r"([\d.]+)(\+?)", u)
        if m and not re.match(r"[\d.]+\s*-\s*[\d.]+", u):
            unidades = int(m.group(1).replace(".", ""))
            if m.group(2) or "entregues" in u or "planejados" in u:
                pend.append(f"unidades '{u}' é valor aproximado/parcial; usado o número base")
            if "planejados" in u:
                unidades = None
                pend.append("unidades são planejadas, não produzidas; omitido")
        else:
            pend.append(f"unidades '{u}' é uma faixa; omitido")
    serv = campos.get("Em serviço", "")
    entrada = ano(serv) if "Previsto" not in serv else None
    status = "ATIVO"
    if re.search(r"\d{4}\s*-\s*\d{4}", serv) or nome in STATUS_APOSENTADO:
        status = "APOSENTADO"
    elif "Previsto" in serv or (not serv and "Em serviço" not in campos):
        status = "PROTOTIPO"
    elif nome in STATUS_ATIVO:
        status = "ATIVO"
    elif entrada and entrada < 1960:
        status = "APOSENTADO"
        pend.append("status APOSENTADO inferido pelo ano de entrada em serviço (< 1960); a fonte não diz")
    else:
        pend.append("status ATIVO assumido; a fonte só traz o ano de entrada em serviço")
    if not serv:
        pend.append("sem ano de entrada em serviço; status PROTOTIPO assumido")

    ficha = {
        "slug": slug(nome), "nome": nome, "pais": pais, "forca": "AEREA", "categoria": "AERONAVE",
        "fabricante": fabricante, "ano_primeiro_uso": ano(pv),
    }
    if entrada:
        ficha["ano_entrada_servico"] = entrada
    if unidades:
        ficha["unidades_produzidas"] = unidades
    ficha.update({
        "status": status, "confiabilidade": "ESTIMADO", "descricao_curta": papel,
        "curiosidade": campos.get("Curiosidade", ""), "operadores": ops, "especificacoes": esp,
        "fontes": [FONTE_AVIOES],
    })
    if "Nota" in campos:
        pend.append(f"nota da fonte: {campos['Nota']}")
    return ficha, pend


# ---------------------------------------------------------------- navios
def converter_navio(nome, linhas, campos):
    pend = []
    pais_nome, papel = [x.strip() for x in campos["País"].split("•")][:2]
    sub = {"Navio": None, "Submarino Nuclear": "submarino"}.get(papel)
    if "Porta-Aviões" in nome:
        sub = "porta_avioes"
    elif "Destroyer" in nome:
        sub = "destroier"
    esp = {"subtipo": sub}
    esp["classe"] = re.search(r"Classe ([^)]+)", nome).group(1)
    if "Deslocamento" in campos:
        esp["deslocamento_t"] = inteiro_ou_float(primeiro_num(campos["Deslocamento"], "t"))
    if "Comprimento" in campos:
        esp["comprimento_m"] = inteiro_ou_float(primeiro_num(campos["Comprimento"], "m"))
    if "Propulsão" in campos:
        esp["propulsao"] = campos["Propulsão"]
    vel = campos.get("Velocidade")
    if vel:
        esp["velocidade_max_kmh"] = inteiro_ou_float(primeiro_num(vel, "km/h"))
        if "submerso" in vel:
            pend.append("velocidade informada é submersa")
    if "Tripulação" in campos:
        esp["tripulacao"] = int(campos["Tripulação"].replace(".", ""))
    if "Armamento" in campos:
        esp["armamento"] = [{"descricao": campos["Armamento"]}]
    if "Aeronaves" in campos:
        pend.append(f"aeronaves embarcadas ('{campos['Aeronaves']}') sem campo no schema")
    if "Submarino (Classe Virginia / Ohio)" == nome:
        pend.append("a fonte mistura duas classes (Virginia e Ohio); ficha precisa ser separada")
    serv = campos.get("Em serviço", "")
    ficha = {
        "slug": slug(nome), "nome": nome, "pais": PAISES[pais_nome], "forca": "MARINHA", "categoria": "NAVIO",
        "ano_primeiro_uso": None, "status": "ATIVO", "confiabilidade": "ESTIMADO", "descricao_curta": papel,
        "curiosidade": campos.get("Curiosidade", ""), "operadores": [PAISES[pais_nome]],
        "especificacoes": esp, "fontes": [FONTE_AVIOES],
    }
    if serv:
        ficha["ano_entrada_servico"] = ano(serv)
    if "Unidades" in campos:
        ficha["unidades_produzidas"] = int(re.match(r"[\d.]+", campos["Unidades"]).group().replace(".", ""))
        pend.append(f"unidades '{campos['Unidades']}' aproximado; usado o número base")
    pend.append("equipamento cadastrado por classe; status ATIVO assumido")
    ficha = {k: v for k, v in ficha.items() if v is not None}
    return ficha, pend


def ler_avioes(caminho):
    secao, itens, atual = None, [], None
    for ln in Path(caminho).read_text(encoding="utf-8").splitlines():
        if ln.startswith("## "):
            secao = ln[3:].strip()
        elif ln.startswith("### "):
            atual = {"nome": ln[4:].strip(), "secao": secao, "linhas": []}
            itens.append(atual)
        elif ln.startswith("- ") and atual:
            atual["linhas"].append(ln)
        elif ln.startswith("---") or ln.startswith("*Gerado"):
            atual = None
    return itens


# ---------------------------------------------------------------- blindados
def converter_tanque(nome, pais_nome, resumo, detalhe):
    pend = ["status ATIVO assumido; a fonte não traz status", "sem ano de entrada em serviço na fonte",
            "sem fabricante na fonte", "sem data/ano de primeiro uso na fonte"]
    esp = {"subtipo": "tanque_leve" if re.match(r"Tanque leve", detalhe) else "tanque_de_batalha"}
    iso = {"Rússia/URSS": "SUN"}.get(pais_nome) or PAISES.get(pais_nome)
    if iso is None:
        pend.append(f"país '{pais_nome}' não mapeado")
    if pais_nome == "Rússia/URSS":
        pend.append("país 'Rússia/URSS' resolvido como SUN; conferir")
    m = re.search(r"Motor\s+(.*?)\s+(\d[\d.]*)\s*hp", detalhe)
    if m:
        esp["potencia_hp"] = int(m.group(2).replace(".", ""))
        modelo = m.group(1).strip()
        if modelo:
            esp["motor_modelo"] = modelo
    else:
        m = re.search(r"Motor\s+([^,]*?)\s+(\d[\d.]*)\s*hp", detalhe)
    if m2 := re.search(r"Vel\.\s*([\d.,]+)\s*km/h", detalhe):
        esp["velocidade_max_kmh"] = inteiro_ou_float(num(m2.group(1)))
    if m3 := re.search(r"Canhão\s+(\d+)\s*mm\s*([^,]*)", detalhe):
        arma = {"calibre_mm": int(m3.group(1))}
        if m3.group(2).strip():
            arma["modelo"] = m3.group(2).strip()
        esp["armamento_principal"] = arma
    if m4 := re.search(r"Peso\s+([\d.,]+)\s*t", detalhe):
        esp["peso_kg"] = inteiro_ou_float(round(num(m4.group(1)) * 1000, 1))
    if m5 := re.search(r"Tripulação\s+(\d+)", detalhe):
        esp["tripulacao"] = int(m5.group(1))
    if m6 := re.search(r"Alcance\s+([\d.]+)\s*km", detalhe):
        esp["alcance_km"] = int(m6.group(1).replace(".", ""))
    resto = re.sub(r"^.*?Alcance\s+[\d.]+\s*km\.?\s*", "", detalhe).strip()
    resto = re.sub(r"^Curiosidade:\s*", "", resto)
    status = "PROTOTIPO" if nome == "EE-T1 Osório" else "ATIVO"
    if status == "PROTOTIPO":
        pend[0] = "status PROTOTIPO: a fonte chama o Osório de protótipo"
    if nome == "Challenger 1":
        status = "APOSENTADO"
        pend[0] = "status APOSENTADO por conhecimento geral (substituído pelo Challenger 2); a fonte não diz"
    ficha = {
        "slug": slug(nome), "nome": nome, "pais": iso, "forca": "EXERCITO", "categoria": "TANQUE",
        "status": status, "confiabilidade": "ESTIMADO", "descricao_curta": resumo,
        "curiosidade": resto, "operadores": [iso] if iso else [], "especificacoes": esp,
        "fontes": [FONTE_BLINDADOS],
    }
    pend.append("operadores: assumido apenas o país de origem; a fonte não lista operadores")
    return ficha, pend


def ler_blindados(caminho):
    texto = Path(caminho).read_text(encoding="utf-8").splitlines()
    resumos, detalhes = {}, {}
    for ln in texto:
        if m := re.match(r"^(.+?) — (.+?) — (.+)$", ln):
            resumos[m.group(1).strip()] = (m.group(2).strip(), m.group(3).strip())
        if m := re.match(r"^([^:]+): (.*(?:Vel\.|Alcance).*)$", ln):
            detalhes[m.group(1).strip()] = m.group(2).strip()
    lotes_so_nomes = []
    for i, ln in enumerate(texto):
        if m := re.match(r"^Lote (\d+) \(6\)$", ln):
            if int(m.group(1)) <= 6:
                lotes_so_nomes.append((int(m.group(1)), [x.strip() for x in texto[i + 1].split(",")]))
    return resumos, detalhes, lotes_so_nomes


def validar(ficha):
    cat = ficha["categoria"].lower()
    esq = json.loads((ESQUEMAS / f"{ {'aeronave':'aeronave','navio':'navio','tanque':'tanque'}[cat] }.schema.json").read_text(encoding="utf-8"))
    erros = [e.message for e in jsonschema.Draft7Validator(esq).iter_errors(ficha["especificacoes"])]
    for obrig in ("slug", "nome", "pais", "forca", "categoria", "status", "confiabilidade"):
        if not ficha.get(obrig):
            erros.append(f"campo obrigatório ausente: {obrig}")
    return erros


def gravar(ficha, pend, saida: Path, sub: str, relatorio):
    erros = validar(ficha)
    if pend:
        ficha["_pendencias"] = pend
    destino = saida / ("rejeitadas" if erros else sub)
    destino.mkdir(parents=True, exist_ok=True)
    if erros:
        ficha["_erros_de_validacao"] = erros
    (destino / f"{ficha['slug']}.json").write_text(json.dumps(ficha, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    relatorio.append((ficha["slug"], sub, "REJEITADA" if erros else "ok", erros, pend))


def main(avioes, blindados, saida):
    saida = Path(saida)
    rel, ignorados = [], []
    for it in ler_avioes(avioes):
        campos = parse_campos(it["linhas"])
        if it["secao"] == "Outras Naves":
            if "País" in campos and campos["País"].split("•")[0].strip() in PAISES:
                f, p = converter_navio(it["nome"], it["linhas"], campos)
                gravar(f, p, saida, "navios", rel)
            else:
                ignorados.append(it["nome"])
            continue
        f, p = converter_aeronave(it["nome"], it["secao"], it["linhas"], campos)
        gravar(f, p, saida, "aeronaves", rel)

    resumos, detalhes, lotes = ler_blindados(blindados)
    alias = {"K2 PIP": "K2 Black Panther PIP"}
    for nome, det in detalhes.items():
        nome_resumo = alias.get(nome, nome)
        if nome_resumo in resumos:
            pais, resumo = resumos[nome_resumo]
            f, p = converter_tanque(nome_resumo, pais, resumo, det)
            gravar(f, p, saida, "blindados", rel)

    pendentes = [(n, l) for l, ns in lotes for n in ns]
    nomes_pendentes = "\n".join(f"- {n} (lote {l})" for n, l in pendentes)
    (saida / "BLINDADOS-SEM-DADOS.md").write_text(
        "# Blindados citados no arquivo, sem ficha técnica\n\n"
        "O Blindado.md traz dados técnicos só dos lotes 7 a 11. Estes (lotes 1 a 6) aparecem apenas pelo nome:\n\n"
        + nomes_pendentes + "\n\nTambém só por nome: emblemas Força Terrestre, Força Naval, Força Aérea (não são equipamentos).\n",
        encoding="utf-8")

    ok = [r for r in rel if r[2] == "ok"]
    rej = [r for r in rel if r[2] != "ok"]
    linhas = ["# Relatório da conversão", "",
              f"- Fichas válidas: {len(ok)} (aeronaves {sum(1 for r in ok if r[1]=='aeronaves')}, "
              f"navios {sum(1 for r in ok if r[1]=='navios')}, blindados {sum(1 for r in ok if r[1]=='blindados')})",
              f"- Rejeitadas pelo schema: {len(rej)}",
              f"- Ignoradas (fora do escopo militar): {', '.join(ignorados) or '—'}",
              f"- Blindados sem dados (lotes 1–6): {len(pendentes)}", ""]
    if rej:
        linhas += ["## Rejeitadas", ""]
        for s, _, _, e, _ in rej:
            linhas.append(f"- `{s}`: {'; '.join(e)}")
        linhas.append("")
    linhas += ["## Pendências por ficha", ""]
    for s, _, st, _, p in rel:
        if p:
            linhas.append(f"### {s} ({st})")
            linhas += [f"- {x}" for x in p]
            linhas.append("")
    (saida / "RELATORIO.md").write_text("\n".join(linhas), encoding="utf-8")
    print(f"válidas={len(ok)} rejeitadas={len(rej)} ignoradas={ignorados} sem_dados={len(pendentes)}")


if __name__ == "__main__":
    main(*sys.argv[1:4])
