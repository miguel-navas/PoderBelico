import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { ErroHttp, buscarFicha, urlPublica } from '../api'
import type { Medida } from '../api'
import {
  formatarNumero,
  formatarPeriodo,
  rotuloForca,
  rotuloStatus,
  rotuloTipoEvento,
} from '../rotulos'
import { ListaFontes, Selo } from '../Selo'

function textoMedida(m: Medida) {
  const principal = `${formatarNumero(m.valor)} ${m.unidade}`
  if (m.convertidos.length === 0) return principal
  const outras = m.convertidos.map((c) => `${formatarNumero(c.valor)} ${c.unidade}`).join(' · ')
  return `${principal} (${outras})`
}

export default function Ficha() {
  const { slug = '' } = useParams()
  const { data, isPending, error } = useQuery({
    queryKey: ['ficha', slug],
    queryFn: () => buscarFicha(slug),
    retry: false,
  })

  if (isPending) return <p>Carregando ficha...</p>

  if (error instanceof ErroHttp && error.status === 404) {
    return (
      <section>
        <h2>Equipamento não encontrado</h2>
        <p>Não existe um equipamento com o endereço “{slug}”.</p>
        <p>
          <Link to="/busca">Buscar equipamentos</Link> · <Link to="/equipamentos">Ver a lista</Link>
        </p>
      </section>
    )
  }
  if (error || !data) return <p role="alert">Não foi possível carregar a ficha.</p>

  const comuns: [string, string | null][] = [
    ['País de origem', data.pais.nome],
    ['Força', rotuloForca(data.forca)],
    ['Categoria', data.categoria.nome],
    ['Fabricante', data.fabricante],
    ['Primeiro uso', data.anoPrimeiroUso?.toString() ?? null],
    ['Entrada em serviço', data.anoEntradaServico?.toString() ?? null],
    ['Unidades produzidas', data.unidadesProduzidas === null ? null : formatarNumero(data.unidadesProduzidas)],
    ['Operadores', data.operadores.map((o) => o.nome).join(', ') || null],
    ['Status', rotuloStatus(data.status)],
  ]

  return (
    <article className="ficha">
      <p>
        <Link to="/equipamentos">← Equipamentos</Link>
      </p>
      <h2>
        {data.nome}
        {data.variante && <span> {data.variante}</span>}
      </h2>

      <p>
        <Selo confiabilidade={data.confiabilidade} />
        {data.somenteFontesProvisorias && (
          <span className="aviso"> Dados baseados apenas em fontes provisórias, ainda não verificadas.</span>
        )}
      </p>

      {data.imagem ? (
        <figure>
          <img src={urlPublica(data.imagem.url)} alt={data.nome} />
          <figcaption>{data.imagem.credito}</figcaption>
        </figure>
      ) : (
        <div className="sem-imagem" role="img" aria-label="Imagem indisponível">
          Imagem indisponível
        </div>
      )}

      {data.descricaoCurta && <p>{data.descricaoCurta}</p>}

      <h3>Dados gerais</h3>
      <dl>
        {comuns
          .filter(([, v]) => v)
          .map(([rotulo, valor]) => (
            <div key={rotulo} className="linha">
              <dt>{rotulo}</dt>
              <dd>{valor}</dd>
            </div>
          ))}
      </dl>

      <h3>Especificações</h3>
      <dl>
        {data.especificacoes.medidas.map((m) => (
          <div key={m.chave} className="linha">
            <dt>{m.rotulo}</dt>
            <dd>{textoMedida(m)}</dd>
          </div>
        ))}
        {data.especificacoes.campos.map((c) => (
          <div key={c.chave} className="linha">
            <dt>{c.rotulo}</dt>
            <dd>
              {c.itens ? (
                <ul>
                  {c.itens.map((i) => (
                    <li key={i}>{i}</li>
                  ))}
                </ul>
              ) : (
                c.valor
              )}
            </dd>
          </div>
        ))}
      </dl>

      <h3>Eventos</h3>
      {data.eventos.length === 0 ? (
        <p>Nenhum evento cadastrado para este equipamento.</p>
      ) : (
        <ul>
          {data.eventos.map((p) => (
            <li key={`${p.evento.slug}-${p.pais.codigo}`}>
              <Link to={`/eventos/${p.evento.slug}`}>{p.evento.nome}</Link>{' '}
              <span className="meta">
                ({rotuloTipoEvento(p.evento.tipo)}, {formatarPeriodo(p.dataInicio, p.dataFim)})
              </span>
              {' — '}
              {p.papel}
              {p.quantidadeAprox !== null && <> (cerca de {formatarNumero(p.quantidadeAprox)})</>}
              <span className="meta"> · por {p.pais.nome}</span>
              {p.observacao && <span className="meta"> · {p.observacao}</span>}
            </li>
          ))}
        </ul>
      )}

      {data.curiosidade && (
        <>
          <h3>Curiosidade</h3>
          <p>{data.curiosidade}</p>
        </>
      )}

      <ListaFontes fontes={data.fontes} />
    </article>
  )
}
