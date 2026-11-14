import { Link } from 'react-router-dom'
import '../inicio.css'

const FORCAS = [
  { codigo: 'EXERCITO', classe: 'army', titulo: 'EXÉRCITO', texto: 'Blindados modernos e forças terrestres' },
  { codigo: 'AEREA', classe: 'air', titulo: 'AERONÁUTICA', texto: 'A-10 Thunderbolt II e aviação de combate' },
  { codigo: 'MARINHA', classe: 'navy', titulo: 'MARINHA', texto: 'Navios de guerra e artilharia naval' },
]

export default function Inicio() {
  return (
    <div className="inicio">
      <div className="inicio-fundo" aria-hidden="true" />
      <div className="inicio-conteudo">
        <h1 aria-label="Poder Bélico">
          <img src={`${import.meta.env.BASE_URL}img/titulo-poder-belico.svg`} alt="" />
        </h1>
        <nav className="force-list" aria-label="Forças militares">
          {FORCAS.map((f) => (
            <Link key={f.codigo} className={`force-card ${f.classe}`} to={`/equipamentos?forca=${f.codigo}`}>
              <span className="card-image" aria-hidden="true" />
              <span className="card-copy">
                <h2>{f.titulo}</h2>
                <p>{f.texto}</p>
              </span>
            </Link>
          ))}
        </nav>
      </div>
    </div>
  )
}
