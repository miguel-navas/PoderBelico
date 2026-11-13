import { Link } from 'react-router-dom'

export default function Metodologia() {
  return (
    <article>
      <h2>Metodologia</h2>

      <h3>Como os dados são coletados</h3>
      <p>
        Todo equipamento e todo evento precisa de pelo menos uma fonte (livro, artigo, site oficial, entre outras),
        que aparece no fim da página. Os dados entram por arquivos de importação revisados: registros sem fonte ou com
        dados inválidos são rejeitados e nada é apagado automaticamente. Fontes marcadas como provisórias (por exemplo,
        conversas com ferramentas de IA) servem apenas para um primeiro rascunho: um registro que dependa só delas
        nunca é tratado como confirmado.
      </p>

      <h3>Níveis de confiabilidade</h3>
      <dl>
        <dt>Confirmado</dt>
        <dd>Os dados foram conferidos em fonte confiável e definitiva. Nenhum selo de alerta é mostrado.</dd>
        <dt>Estimado</dt>
        <dd>
          Os números são aproximados ou ainda não foram conferidos com rigor. Use como referência, não como valor
          exato.
        </dd>
        <dt>Contestado</dt>
        <dd>As fontes discordam entre si ou o dado é alvo de disputa. A página indica as fontes consultadas.</dd>
      </dl>

      <h3>Unidades</h3>
      <p>
        Os valores são guardados em unidades do sistema métrico e mostrados também em unidades imperiais, sempre com
        arredondamento de uma casa decimal: km/h em milhas por hora (nós, para navios), kg em libras, metros em pés,
        km em milhas (milhas náuticas, para navios), mm em polegadas, hp em kW e lbf em kN. A conversão é feita só na
        exibição; o valor original não muda.
      </p>

      <h3>Como enviar correções</h3>
      <p>
        Encontrou um erro ou tem uma fonte melhor? Envie ao responsável pelo projeto o nome do equipamento ou do
        evento, o campo a corrigir, o valor correto e a fonte que o sustenta. Correções com fonte verificável
        substituem o dado anterior na próxima importação.
      </p>

      <p>
        <Link to="/equipamentos">Ver equipamentos</Link> · <Link to="/eventos">Ver eventos</Link>
      </p>
    </article>
  )
}
