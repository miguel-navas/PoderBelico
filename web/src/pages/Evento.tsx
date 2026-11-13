import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { ErroHttp, buscarEvento } from '../api'
import type { NoEvento } from '../api'
import {
  formatarPeriodo,
  rotuloForca,
  rotuloTipoEvento,
} from '../rotulos'
import { ListaFontes, Selo } from '../Selo'

function Arvore({ nos }: { nos: NoEvento[] }) {
  return (
    <ul>
      {nos.map((n) => (
        <li key={n.slug}>
          <Link to={`/eventos/${n.slug}`}>{n.nome}</Link>{' '}
          <span className="meta">
            ({rotuloTipoEvento(n.tipo)}, {formatarPeriodo(n.dataInicio, n.dataFim)})
          </span>
          {n.subeventos.length > 0 && <Arvore nos={n.subeventos} />}
        </li>
      ))}
    </ul>
  )
}

export default function Evento() {
  const { slug = '' } = useParams()
  const { data, isPending, error } = useQuery({
    queryKey: ['evento', slug],
    queryFn: () => buscarEvento(slug),
    retry: false,
  })

  if (isPending) return <p>Carregando evento...</p>

  if (error instanceof ErroHttp && error.status === 404) {
    return (
      <section>
        <h2>Evento não encontrado</h2>
        <p>Não existe um evento com o endereço “{slug}”.</p>
        <p>
          <Link to="/busca">Buscar</Link> · <Link to="/eventos">Ver a lista de eventos</Link>
        </p>
      </section>
    )
  }
  if (error || !data) return <p role="alert">Não foi possível carregar o evento.</p>

  return (
    <article className="ficha">
      <p>
        <Link to="/eventos">← Eventos</Link>
        {data.eventoPai && (
          <>
            {' '}
            · Dentro de <Link to={`/eventos/${data.eventoPai.slug}`}>{data.eventoPai.nome}</Link>
          </>
        )}
      </p>

      <h2>{data.nome}</h2>
      <p>
        <Selo confiabilidade={data.confiabilidade} />
        {data.somenteFontesProvisorias && (
          <span className="aviso"> Dados baseados apenas em fontes provisórias, ainda não verificadas.</span>
        )}
      </p>

      <dl>
        <div className="linha">
          <dt>Tipo</dt>
          <dd>{rotuloTipoEvento(data.tipo)}</dd>
        </div>
        <div className="linha">
          <dt>Período</dt>
          <dd>{formatarPeriodo(data.dataInicio, data.dataFim)}</dd>
        </div>
        {data.local && (
          <div className="linha">
            <dt>Local</dt>
            <dd>{data.local}</dd>
          </div>
        )}
        {data.resultado && (
          <div className="linha">
            <dt>Resultado</dt>
            <dd>{data.resultado}</dd>
          </div>
        )}
      </dl>

      {data.descricao && <p>{data.descricao}</p>}

      {data.beligerantes.length > 0 && (
        <>
          <h3>Beligerantes</h3>
          <dl>
            {data.beligerantes.map((l) => (
              <div key={l.lado ?? 'sem-lado'} className="linha">
                <dt>{l.lado ?? 'Lado não informado'}</dt>
                <dd>{l.paises.map((p) => p.nome).join(', ')}</dd>
              </div>
            ))}
          </dl>
        </>
      )}

      {data.subeventos.length > 0 && (
        <>
          <h3>Eventos dentro deste</h3>
          <Arvore nos={data.subeventos} />
        </>
      )}

      <h3>Equipamentos que participaram</h3>
      {data.participacoes.length === 0 ? (
        <p>Nenhum equipamento cadastrado para este evento.</p>
      ) : (
        data.participacoes.map((g) => (
          <section key={g.pais.codigo}>
            <h4>{g.pais.nome}</h4>
            {g.forcas.map((f) => (
              <div key={f.forca}>
                <strong>{rotuloForca(f.forca)}</strong>
                <ul>
                  {f.equipamentos.map((e) => (
                    <li key={e.slug}>
                      <Link to={`/equipamentos/${e.slug}`}>{e.nome}</Link> — {e.papel}
                      {e.quantidadeAprox !== null && <> (cerca de {e.quantidadeAprox})</>}
                      {e.observacao && <span className="meta"> · {e.observacao}</span>}
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </section>
        ))
      )}

      <ListaFontes fontes={data.fontes} />
    </article>
  )
}
