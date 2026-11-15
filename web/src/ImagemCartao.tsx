import type { EquipamentoResumo } from './api'

export function ImagemCartao({ e }: { e: EquipamentoResumo }) {
  return (
    <div className="cartao-img">
      {e.imagem ? (
        <>
          <img src={e.imagem.url} alt="" loading="lazy" decoding="async" />
          <span className="credito">{e.imagem.credito}</span>
        </>
      ) : e.subtipo ? (
        <>
          <img className="generica" src={`/img/categorias/${e.subtipo.codigo}.jpg`} alt="" loading="lazy" decoding="async" />
          <span className="credito">Ilustração da categoria · IA</span>
        </>
      ) : (
        <div className="inicial" aria-hidden="true">{e.categoria.nome.charAt(0)}</div>
      )}
    </div>
  )
}
