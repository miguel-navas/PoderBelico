import { Link, Route, Routes, useLocation, useNavigate, useParams } from 'react-router-dom'
import type { FormEvent } from 'react'
import Busca from './pages/Busca'
import Evento from './pages/Evento'
import Eventos from './pages/Eventos'
import Ficha from './pages/Ficha'
import Inicio from './pages/Inicio'
import Lista from './pages/Lista'
import Metodologia from './pages/Metodologia'
import LinhaDoTempo from './pages/LinhaDoTempo'
import Pais from './pages/Pais'
import Paises from './pages/Paises'
import { LinkF } from './navegacao'
import './catalogo.css'

function CampoBusca() {
  const navegar = useNavigate()
  const { pathname, search } = useLocation()
  const atual = pathname === '/busca' ? (new URLSearchParams(search).get('q') ?? '') : ''

  function enviar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const texto = String(new FormData(e.currentTarget).get('q') ?? '').trim()
    if (texto) navegar(`/busca?q=${encodeURIComponent(texto)}`)
  }

  return (
    <form className="busca" role="search" onSubmit={enviar} key={atual}>
      <input type="search" name="q" aria-label="Buscar" placeholder="Buscar equipamento ou evento" defaultValue={atual} />
      <button type="submit">Buscar</button>
    </form>
  )
}

function PaginaPais() {
  const { codigo = '' } = useParams()
  return <Pais key={codigo} codigo={codigo} />
}

export default function App() {
  const { pathname } = useLocation()
  if (pathname === '/') return <Inicio />
  return (
    <>
      <header className="topo">
        <h1>Poder Bélico</h1>
        <nav aria-label="Principal">
          <Link to="/">Início</Link> · <LinkF to="/equipamentos">Equipamentos</LinkF> · <LinkF to="/eventos">Eventos</LinkF> ·{' '}
          <LinkF to="/linha-do-tempo">Linha do tempo</LinkF> · <LinkF to="/paises">Países</LinkF>
        </nav>
        <CampoBusca />
      </header>
      <main>
        <Routes>
          <Route path="/equipamentos" element={<Lista />} />
          <Route path="/equipamentos/:slug" element={<Ficha />} />
          <Route path="/eventos" element={<Eventos />} />
          <Route path="/eventos/:slug" element={<Evento />} />
          <Route path="/linha-do-tempo" element={<LinhaDoTempo />} />
          <Route path="/paises" element={<Paises />} />
          <Route path="/paises/:codigo" element={<PaginaPais />} />
          <Route path="/metodologia" element={<Metodologia />} />
          <Route path="/busca" element={<Busca />} />
          <Route path="*" element={<p>Página não encontrada.</p>} />
        </Routes>
      </main>
      <footer className="rodape">
        <Link to="/metodologia">Metodologia e fontes de dados</Link>
      </footer>
    </>
  )
}
