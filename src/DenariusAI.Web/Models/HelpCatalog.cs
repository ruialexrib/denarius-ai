using DenariusAI.Web.ViewModels;

namespace DenariusAI.Web.Models;

/// <summary>
/// Defines the functional documentation available in the DenariusAI Help Center.
/// </summary>
public static class HelpCatalog
{
    /// <summary>
    /// Gets all Help Center topics keyed by their stable route identifier.
    /// </summary>
    public static IReadOnlyDictionary<string, HelpDetailViewModel> Pages { get; } = BuildPages();

    /// <summary>
    /// Returns the topics that the current user is allowed to discover.
    /// </summary>
    /// <param name="isAdministrator">Whether the current user has the administrator role.</param>
    /// <returns>An ordered list of visible documentation topics.</returns>
    public static IReadOnlyList<HelpDetailViewModel> VisiblePages(bool isAdministrator) =>
        Pages.Values.Where(page => isAdministrator || !page.AdministratorOnly).ToList();

    /// <summary>
    /// Builds the complete Help Center catalogue.
    /// </summary>
    /// <returns>A case-insensitive dictionary of documentation pages.</returns>
    private static IReadOnlyDictionary<string, HelpDetailViewModel> BuildPages()
    {
        var pages = new[]
        {
            FeaturedPage("dashboard", "Visão geral", "⌂", "Dashboard", "Acompanhe indicadores, orçamento e evolução financeira do período selecionado.", "Home", "Index", "Abrir Dashboard",
                Section("periodo", "Período e indicadores", "O Dashboard resume informação calculada pela aplicação.",
                    "Selecione Ano e Mês e clique em Aplicar para recalcular os indicadores e gráficos dependentes do período.",
                    "Resultado corresponde a rendimentos menos despesas; movimentos anulados não contribuem para os totais ativos.",
                    "Os indicadores de reconciliação identificam operações que ainda necessitam de confirmação bancária."),
                Section("graficos", "Gráficos", "Use os gráficos para perceber tendência e composição.",
                    "Os gráficos anuais percorrem os meses do ano selecionado; passe o cursor pelos pontos para consultar valores exatos.",
                    "Orçamentado vs. realizado depende dos movimentos ativos associados explicitamente ao orçamento.",
                    "Rendimentos vs. despesas depende da classificação contabilística dos movimentos."),
                Section("duvidas", "Resolver problemas", "Comece sempre pelos dados que alimentam o indicador.",
                    "Confirme o período, o estado ativo do movimento e a associação ao orçamento quando aplicável.",
                    "Para explicar um saldo, abra o extrato da conta e verifique as partidas que formam o valor.")),

            Page("movimentos", "Contabilidade", "↔", "Movimentos", "Registe operações por partidas dobradas e mantenha um histórico contabilístico auditável.", "JournalEntries", "Index", "Consultar movimentos",
                Section("estrutura", "Estrutura do movimento", "Cada movimento tem um cabeçalho e pelo menos duas linhas contabilísticas.",
                    "Data e Descrição identificam a operação; Referência, Notas e Orçamento acrescentam contexto quando aplicável.",
                    "Cada linha contém Conta, Categoria quando aplicável, descrição e exclusivamente um valor a Débito ou a Crédito.",
                    "O total dos débitos tem de ser igual ao total dos créditos e devem existir pelo menos duas contas diferentes."),
                Section("classificacao", "Classificação", "A classificação determina onde a operação entra nos saldos e análises.",
                    "Numa despesa, debite a conta de despesa e credite a conta usada para pagamento.",
                    "Num rendimento, debite a conta que recebe e credite a conta de rendimento.",
                    "Numa transferência entre ativos, debite o destino e credite a origem; normalmente não existe categoria.",
                    "A categoria tem de ser compatível com o tipo da conta e representa a componente de rendimento ou despesa."),
                Section("estado", "Editar, reconciliar e anular", "O estado do movimento condiciona as ações disponíveis.",
                    "Movimentos reconciliados não podem ser editados enquanto mantiverem esse estado.",
                    "Anular preserva o histórico e retira o movimento dos cálculos ativos; não equivale a apagar o registo.",
                    "Use os detalhes e extratos relacionados para validar uma correção antes de alterar a classificação."),
                Section("ia", "Preenchimento assistido", "A IA propõe; o utilizador confirma.",
                    "Revise data, valor, contas, categorias, orçamento, descrição e referência antes de gravar.",
                    "Informação ambígua deve ser completada pelo utilizador; a IA não grava automaticamente um movimento."),
                Section("erros", "Resolver problemas", "As validações protegem o equilíbrio contabilístico.",
                    "Se não conseguir gravar, confirme que cada linha tem apenas Débito ou Crédito e que os totais estão equilibrados.",
                    "Se uma categoria não estiver disponível, confirme primeiro a conta e a respetiva natureza financeira.")),

            Page("statements", "Contabilidade", "≡", "Extratos", "Consulte as partidas que explicam o saldo de contas, categorias e grupos.", "Accounts", "Index", "Abrir Contas",
                Section("acesso", "Onde abrir", "Os extratos são uma ferramenta de consulta e auditoria.",
                    "Nas listas de Contas, Grupos ou Categorias, use a ação Extrato no elemento pretendido.",
                    "Nos detalhes de um movimento pode seguir a ligação da conta para abrir o respetivo histórico."),
                Section("filtros", "Filtros", "Os filtros reduzem os resultados sem alterar os dados guardados.",
                    "Defina De e Até e combine os critérios disponíveis; clique em Aplicar para atualizar os resultados.",
                    "Limpar repõe os filtros e o número de registos por página acompanha a paginação."),
                Section("leitura", "Débito, crédito e saldo", "Cada linha representa uma partida do elemento consultado.",
                    "O saldo é calculado sequencialmente e considera o saldo inicial quando existe.",
                    "O extrato de conta mostra as partidas da conta; o de categoria agrega a classificação; o de grupo reúne as categorias do grupo.")),

            Page("reconciliation", "Contabilidade", "✓", "Reconciliação", "Compare movimentos contabilísticos com as operações reais da conta bancária.", "Reconciliation", "Index", "Abrir Reconciliação",
                Section("manual", "Reconciliação manual", "A reconciliação confirma a correspondência entre contabilidade e banco.",
                    "Filtre por conta, datas, estado ou descrição e aplique os critérios.",
                    "Confirme data, referência, descrição e valor antes de reconciliar.",
                    "Desfazer retira a confirmação sem alterar os valores do movimento."),
                Section("regras", "Regras", "As restrições preservam a coerência do histórico.",
                    "A reconciliação aplica-se às contas bancárias elegíveis.",
                    "Movimentos reconciliados não podem ser editados até a reconciliação ser desfeita.",
                    "Confirme potenciais duplicados pela combinação de data, referência, descrição e valor."),
                Section("importacao", "Colar movimentos com IA", "A classificação assistida prepara propostas para revisão.",
                    "Escolha a conta e cole linhas com data, descrição, referência quando exista e valor; entradas são positivas e saídas negativas na fronteira de importação.",
                    "A IA pode sugerir contrapartida e categoria, mas cada linha continua editável antes de processar.",
                    "Linhas com validação bloqueante não devem ser reconciliadas nem persistidas.")),

            Page("budget", "Planeamento", "▦", "Orçamentos", "Planeie valores mensais por categoria e acompanhe a execução real.", "Budget", "Index", "Gerir Orçamentos",
                Section("periodo", "Período e valores", "Cada orçamento corresponde a um mês e ano.",
                    "Selecione Ano e Mês e introduza montantes não negativos nas categorias que pretende planear.",
                    "Um valor zero representa ausência de planeamento efetivo para essa categoria."),
                Section("associacao", "Associação aos movimentos", "A execução depende da associação explícita do movimento ao orçamento.",
                    "A data do movimento, por si só, não o integra automaticamente no orçamento.",
                    "Movimentos anulados ficam excluídos da execução ativa."),
                Section("calculos", "Cálculos", "Os valores são calculados deterministicamente pela aplicação.",
                    "Realizado soma os movimentos ativos associados ao orçamento e classificados nas respetivas categorias.",
                    "Disponível corresponde a Orçamentado menos Realizado; execução acima de 100% representa excesso face ao planeado.")),

            Page("accounts", "Estrutura financeira", "¤", "Contas", "Configure as contas que recebem as partidas e determinam os saldos.", "Accounts", "Index", "Gerir Contas",
                Section("campos", "Campos", "A ficha define identidade, natureza contabilística e moeda.",
                    "Nome e Descrição identificam a conta; Tipo determina o comportamento contabilístico.",
                    "Saldo inicial estabelece o ponto de partida do extrato quando aplicável.",
                    "Moeda usa um código de três letras e a Categoria apenas é aplicável aos tipos suportados."),
                Section("regras", "Regras após utilização", "O histórico limita alterações estruturais.",
                    "Depois de existirem movimentos, tipo, moeda ou associação estrutural podem ficar bloqueados.",
                    "As categorias disponíveis têm de ser compatíveis com a natureza da conta.",
                    "Desativar preserva o histórico e impede nova utilização quando aplicável."),
                Section("saldo", "Saldo e extrato", "O extrato explica o saldo através das partidas registadas.",
                    "Se o saldo parecer incorreto, confirme saldo inicial, tipo da conta e movimentos ativos.")),

            Page("groups", "Estrutura financeira", "▤", "Grupos financeiros", "Organize categorias por natureza financeira.", "FinancialGroups", "Index", "Gerir Grupos",
                Section("campos", "Campos", "Os grupos são o nível superior da classificação financeira.",
                    "Nome e Descrição identificam o grupo; Tipo define a natureza herdada pelas categorias; Ordem controla a apresentação."),
                Section("regras", "Regras", "As dependências existentes impedem alterações incoerentes.",
                    "O tipo não deve ser alterado quando já existem categorias incompatíveis.",
                    "Categorias ativas podem ter de ser desativadas antes de desativar a estrutura que as suporta.",
                    "Elementos utilizados preservam o histórico em vez de serem removidos fisicamente."),
                Section("extrato", "Extrato", "O extrato do grupo agrega partidas classificadas nas respetivas categorias.",
                    "Use-o para explicar totais por natureza financeira sem alterar movimentos.")),

            Page("categories", "Estrutura financeira", "☷", "Categorias", "Classifique rendimentos e despesas dentro dos grupos financeiros.", "Categories", "Index", "Gerir Categorias",
                Section("campos", "Campos", "A categoria liga a operação ao respetivo grupo.",
                    "Grupo define a natureza herdada; Nome e Descrição identificam a finalidade; Ordem controla a apresentação."),
                Section("compatibilidade", "Compatibilidade", "A aplicação filtra categorias incompatíveis com a conta ou operação.",
                    "A categoria deve classificar a componente de rendimento ou despesa, não uma transferência comum entre ativos.",
                    "Depois de utilizada, a mudança de grupo pode ficar bloqueada; desativar preserva o histórico."),
                Section("duvidas", "Resolver problemas", "Se uma categoria não aparecer, verifique a estrutura financeira.",
                    "Confirme o grupo, o tipo herdado e a conta selecionada antes de procurar um erro no movimento.")),

            Page("certificates", "Poupança e investimento", "◆", "Certificados de Aforro", "Acompanhe subscrições, valor atual, rendimento e capitalizações.", "SavingsCertificates", "Index", "Gerir Certificados",
                Section("campos", "Campos", "A subscrição guarda os dados necessários ao acompanhamento.",
                    "Data, Série/Número e Descrição identificam a subscrição.",
                    "Investimento representa o capital aplicado; Taxa e Valor atual alimentam os indicadores.",
                    "Próxima capitalização identifica a data relevante seguinte; valores sujeitos a validação não podem ser negativos."),
                Section("calculos", "Cálculos", "Os valores derivados são calculados pela aplicação.",
                    "Rendimento corresponde à diferença entre Valor atual e Investimento.",
                    "Juros e valores futuros apresentados são projeções baseadas nas regras implementadas e não garantias de retorno."),
                Section("ciclo", "Editar e eliminar", "O registo de acompanhamento é independente dos movimentos contabilísticos.",
                    "Editar deve atualizar a mesma subscrição; não reutilize o registo para outro investimento.",
                    "Eliminar remove o registo de acompanhamento após confirmação e não apaga automaticamente movimentos financeiros.")),

            Page("stocks", "Poupança e investimento", "↗", "Portefólio de ações", "Registe posições e watchlist, acompanhe cotações, histórico e previsões indicativas.", "StockPortfolio", "Index", "Abrir Portefólio",
                Section("identificacao", "Identificação", "O fornecedor usa estes dados para localizar o instrumento.",
                    "Ticker é o símbolo reconhecido pelo fornecedor, por exemplo EDP.LS; Nome identifica a empresa ou instrumento.",
                    "Mercado identifica a bolsa e Moeda usa o código ISO de três letras."),
                Section("posicao", "Posição", "Quantidade e preço médio suportam os indicadores de valorização.",
                    "Quantidade representa as unidades detidas; Preço médio de compra é o custo médio por ação.",
                    "Cotação atual e Data da cotação identificam o valor de mercado de referência.",
                    "Apenas watchlist acompanha o instrumento sem o considerar uma posição detida."),
                Section("historico", "Histórico", "A data inicial define a referência temporal dentro dos dados disponibilizados.",
                    "Recolher histórico desde condiciona o período apresentado; limites do fornecedor podem reduzir a profundidade disponível.",
                    "A evolução apresenta série histórica, mínimo, máximo e variação com base nos dados recolhidos."),
                Section("previsao", "Previsão", "As previsões são estatísticas e não constituem recomendação de investimento.",
                    "Quando ativa, a previsão calcula horizontes de 30, 60 e 90 dias se existirem pelo menos 60 observações.",
                    "Os intervalos representam incerteza e a previsão nunca altera automaticamente a carteira."),
                Section("erros", "Resolver problemas", "Valide primeiro a identificação e a disponibilidade do serviço externo.",
                    "Se não houver cotação, confirme ticker, mercado e configuração do serviço de ações.",
                    "Se não houver previsão, confirme a opção e a quantidade de observações históricas.")),

            Page("insurance", "Proteção", "◈", "Seguros", "Gira apólices, calendário e prémios associados a movimentos financeiros.", "Insurance", "Index", "Gerir Seguros",
                Section("campos", "Identificação e cobertura", "A ficha identifica o contrato e o objeto protegido.",
                    "Nome, Seguradora, Número da apólice e Objeto seguro identificam o contrato.",
                    "Tipo pode ser Habitação, Automóvel, Saúde, Vida, Acidentes pessoais ou Outro.",
                    "Frequência de pagamento pode ser Mensal, Trimestral, Semestral, Anual ou Irregular."),
                Section("datas", "Vigência", "As datas suportam o acompanhamento do ciclo contratual.",
                    "Data de início e Data de fim delimitam a vigência conhecida; Data de renovação identifica a próxima revisão.",
                    "Notas permitem guardar coberturas, franquias ou observações sem campo próprio."),
                Section("premios", "Prémios", "Os pagamentos são acompanhados separadamente da identificação da apólice.",
                    "Um prémio pode ser associado a um movimento existente e ter comprovativos próprios.",
                    "Quando associado, o movimento ativo funciona como fonte de verdade para estado e data efetiva do pagamento."),
                Section("ia", "Preenchimento assistido", "Na criação, a IA pode propor campos a partir de dados copiados.",
                    "Só os campos reconhecidos devem ser preenchidos e todos permanecem editáveis.",
                    "Nada é gravado sem confirmação e a funcionalidade depende de um fornecedor de IA configurado.")),

            Page("warranties", "Proteção", "□", "Garantias", "Registe garantias, comprovativos e avisos antes da expiração.", "Warranties", "Index", "Gerir Garantias",
                Section("campos", "Campos", "A garantia reúne identificação e período de cobertura.",
                    "Nome identifica a garantia; Fornecedor identifica a origem da compra ou serviço.",
                    "Data de compra e Data de expiração definem o período coberto.",
                    "Dias de antecedência define quando o aviso começa e aceita valores entre 0 e 3650; Notas guardam contexto adicional."),
                Section("pdf", "Documento PDF", "Pode guardar o comprovativo ou certificado.",
                    "O ficheiro tem de ser um PDF válido até 10 MB.",
                    "Ao editar, o documento atual só é substituído quando selecionar outro PDF."),
                Section("aviso", "Lembrete associado", "A garantia mantém um aviso coerente com as datas configuradas.",
                    "O lembrete associado é atualizado automaticamente quando altera a garantia.",
                    "Se o aviso aparecer numa data inesperada, confirme a expiração e os dias de antecedência.")),

            Page("correspondence", "Documentos", "✉", "Correspondência", "Registe correspondência recebida e associe o respetivo documento.", "Correspondence", "Index", "Gerir Correspondência",
                Section("campos", "Campos", "Os metadados permitem localizar o documento sem ter de o abrir.",
                    "Assunto identifica o conteúdo; Remetente identifica a origem; Data de receção regista quando chegou.",
                    "Notas guardam contexto adicional."),
                Section("pdf", "Documento PDF", "O documento fica associado ao registo.",
                    "Só são aceites ficheiros PDF válidos até 10 MB.",
                    "Numa edição, o ficheiro existente é mantido exceto quando selecionar um novo PDF."),
                Section("metadata", "Metadados", "A consulta de metadados fornece informação técnica do documento sem alterar o ficheiro.",
                    "Use-a como apoio documental e confirme o conteúdo no próprio PDF quando necessário.")),

            Page("reminders", "Organização", "!", "Lembretes", "Crie avisos para eventos e prazos.", "Reminders", "Index", "Gerir Lembretes",
                Section("campos", "Campos", "Cada lembrete combina uma descrição, uma data e uma antecedência.",
                    "Texto descreve o evento; Data do evento identifica o dia relevante.",
                    "Dias de antecedência define quando o alerta começa e aceita valores entre 0 e 3650."),
                Section("comportamento", "Quando é apresentado", "O alerta é calculado a partir da data e antecedência.",
                    "O aviso fica disponível quando entra na janela definida e permanece ativo até cada utilizador efetuar a confirmação prevista.",
                    "Lembretes criados por outra área, como Garantias, devem ser ajustados na origem quando exista associação automática.")),

            Page("analytics", "Análise", "⌁", "Análise Financeira", "Explore comparações, tendências e relatórios explicativos.", "Analytics", "Index", "Abrir Análise",
                Section("filtros", "Filtros", "Defina um universo coerente antes de interpretar resultados.",
                    "Selecione De e Até e, quando disponível, Grupo, Categoria ou Conta; clique em Aplicar para recalcular.",
                    "Use intervalos comparáveis e dados corretamente classificados."),
                Section("calculos", "Indicadores", "Totais, percentagens e variações são calculados pela aplicação.",
                    "Movimentos anulados ficam excluídos dos cálculos ativos, salvo indicação explícita em contrário.",
                    "Use extratos e movimentos subjacentes para explicar valores agregados."),
                Section("relatorio", "Relatório inteligente", "A IA recebe valores previamente calculados e produz uma interpretação.",
                    "Gerar relatório não delega ao modelo a aritmética contabilística.",
                    "Reveja as conclusões; o texto explica os dados e não constitui aconselhamento financeiro profissional.")),

            AiPage("assistant", "Inteligência artificial", "✦", "Assistência por IA", "Consulte dados em linguagem natural e prepare sugestões sob controlo do utilizador.", "Assistant", "Index", "Abrir Assistente",
                Section("consultas", "Consultar dados", "Dê contexto suficiente para a pergunta.",
                    "Indique período, conta, categoria, orçamento ou outra referência relevante quando a pergunta possa ser ambígua.",
                    "O assistente utiliza apenas dados e ferramentas autorizados para o utilizador autenticado."),
                Section("propostas", "Preparar propostas", "A IA pode interpretar texto e propor dados para revisão.",
                    "Se faltar informação essencial, deve pedir clarificação em vez de inventar contas, categorias, datas ou valores.",
                    "A proposta permanece editável e não é persistida sem confirmação."),
                Section("seguranca", "Limites e segurança", "A IA interpreta e explica; as regras financeiras permanecem determinísticas.",
                    "Não introduza palavras-passe, chaves de API ou outros segredos.",
                    "Confirme classificações e conclusões e tenha em conta os limites do fornecedor e do modelo configurados.")),

            Page("preferences", "Conta", "◎", "Perfil e segurança", "Atualize os dados da sua conta e mantenha as credenciais protegidas.", "Account", "Profile", "Abrir Perfil",
                Section("perfil", "Perfil", "As alterações afetam a conta autenticada.",
                    "Atualize o Nome e grave; alterações a email ou identidade podem depender das regras administrativas existentes."),
                Section("password", "Palavra-passe", "Use o fluxo próprio para alterar credenciais.",
                    "Introduza palavra-passe atual, nova palavra-passe e confirmação conforme solicitado.",
                    "A nova palavra-passe tem de cumprir as regras configuradas e coincidir com a confirmação.",
                    "Termine a sessão em equipamentos partilhados.")),

            AdminPage("users", "Administração", "♙", "Utilizadores", "Administre contas, funções e acesso.", "Users", "Index", "Gerir Utilizadores",
                Section("gestao", "Criar e editar", "A gestão de utilizadores é administrativa.",
                    "Crie a conta com Nome, Email, Palavra-passe e função aplicável.",
                    "Na edição, altere apenas os dados e permissões necessários e use o fluxo adequado para redefinir credenciais."),
                Section("permissoes", "Funções", "As funções controlam o acesso às áreas protegidas.",
                    "Não reduza privilégios ou remova contas de forma a deixar a instalação sem um administrador funcional."),
                Section("historico", "Histórico de autenticação", "Use o histórico para investigar acessos e falhas de autenticação.",
                    "Trate esta informação como dado de segurança e não copie credenciais para notas ou comentários.")),

            AdminPage("audit", "Administração", "⌕", "Auditoria", "Consulte o histórico de alterações relevantes efetuadas na aplicação.", "Audit", "Index", "Abrir Auditoria",
                Section("eventos", "Eventos", "A auditoria permite identificar operações suportadas e respetivos autores.",
                    "Os registos podem incluir inserção, alteração, anulação ou reativação e eliminação conforme a entidade.",
                    "A listagem identifica tipo, operação, momento e utilizador; os detalhes apresentam informação relevante antes e depois quando disponível."),
                Section("consulta", "Consultar", "Use filtros ou ligações diretas das áreas funcionais.",
                    "Quando existe ligação por registo, abra primeiro o respetivo histórico e depois o evento concreto.",
                    "A auditoria não deve expor palavras-passe, hashes, tokens ou chaves.")),

            AdminPage("settings", "Administração", "⚙", "Definições e operações globais", "Configure IA, cópias de segurança, demonstração e operações destrutivas.", "Settings", "Index", "Abrir Definições",
                Section("ia", "Configuração de IA", "A configuração determina o fornecedor e o comportamento das funcionalidades assistidas.",
                    "Configure fornecedor, URL quando aplicável, credencial, modelo e parâmetros disponíveis.",
                    "Os prompts operacionais configuráveis afetam extração, classificação e relatórios.",
                    "Use Testar ligação e não exponha a credencial ao diagnosticar problemas."),
                Section("backup", "Cópia de segurança e restauro", "Estas operações fazem parte da continuidade dos dados.",
                    "Crie uma cópia de segurança antes de operações destrutivas ou de substituição de dados.",
                    "O restauro valida formato e relações antes da substituição; um ficheiro inválido deve ser rejeitado sem alteração parcial.",
                    "Proteja os ficheiros de backup porque podem conter informação financeira."),
                Section("demo", "Dados de demonstração", "O carregamento de demonstração cria o cenário de exemplo previsto pela aplicação.",
                    "Use esta opção apenas quando pretende trabalhar com dados de demonstração e não a confunda com restauro de dados reais."),
                Section("reset", "Reinicialização financeira", "A reinicialização é destrutiva e exige confirmação explícita.",
                    "Leia o contexto apresentado no formulário e faça uma cópia de segurança quando precisar de possibilidade de recuperação.",
                    "Não use a reinicialização como mecanismo normal de correção de movimentos."),
                Section("duvidas", "Resolver problemas", "Separe problemas de configuração externa de problemas de dados.",
                    "Se a IA não responder, teste fornecedor, endpoint e modelo.",
                    "Se um restauro for recusado, confirme versão e formato do ficheiro em vez de contornar a validação.",
                    "Se uma opção administrativa não estiver visível, confirme a função da conta autenticada."))
        };

        return pages.ToDictionary(page => page.Id, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a standard Help Center page.
    /// </summary>
    /// <param name="id">Stable route identifier for the page.</param>
    /// <param name="category">Functional category displayed in the Help Center.</param>
    /// <param name="icon">Icon displayed for the topic.</param>
    /// <param name="title">Topic title.</param>
    /// <param name="subtitle">Short functional description of the topic.</param>
    /// <param name="controller">MVC controller used by the related action link.</param>
    /// <param name="action">MVC action used by the related action link.</param>
    /// <param name="actionLabel">Label displayed for the related action link.</param>
    /// <param name="sections">Documentation sections that compose the page.</param>
    /// <returns>A standard Help Center page definition.</returns>
    private static HelpDetailViewModel Page(string id, string category, string icon, string title, string subtitle,
        string controller, string action, string actionLabel, params HelpSectionViewModel[] sections) =>
        new(id, category, icon, title, subtitle, controller, action, actionLabel, false, false, false, sections);

    /// <summary>
    /// Creates the featured Help Center page.
    /// </summary>
    /// <param name="id">Stable route identifier for the page.</param>
    /// <param name="category">Functional category displayed in the Help Center.</param>
    /// <param name="icon">Icon displayed for the topic.</param>
    /// <param name="title">Topic title.</param>
    /// <param name="subtitle">Short functional description of the topic.</param>
    /// <param name="controller">MVC controller used by the related action link.</param>
    /// <param name="action">MVC action used by the related action link.</param>
    /// <param name="actionLabel">Label displayed for the related action link.</param>
    /// <param name="sections">Documentation sections that compose the page.</param>
    /// <returns>A featured Help Center page definition.</returns>
    private static HelpDetailViewModel FeaturedPage(string id, string category, string icon, string title, string subtitle,
        string controller, string action, string actionLabel, params HelpSectionViewModel[] sections) =>
        new(id, category, icon, title, subtitle, controller, action, actionLabel, false, true, false, sections);

    /// <summary>
    /// Creates an AI-focused Help Center page.
    /// </summary>
    /// <param name="id">Stable route identifier for the page.</param>
    /// <param name="category">Functional category displayed in the Help Center.</param>
    /// <param name="icon">Icon displayed for the topic.</param>
    /// <param name="title">Topic title.</param>
    /// <param name="subtitle">Short functional description of the topic.</param>
    /// <param name="controller">MVC controller used by the related action link.</param>
    /// <param name="action">MVC action used by the related action link.</param>
    /// <param name="actionLabel">Label displayed for the related action link.</param>
    /// <param name="sections">Documentation sections that compose the page.</param>
    /// <returns>An AI-focused Help Center page definition.</returns>
    private static HelpDetailViewModel AiPage(string id, string category, string icon, string title, string subtitle,
        string controller, string action, string actionLabel, params HelpSectionViewModel[] sections) =>
        new(id, category, icon, title, subtitle, controller, action, actionLabel, false, false, true, sections);

    /// <summary>
    /// Creates an administrator-only Help Center page.
    /// </summary>
    /// <param name="id">Stable route identifier for the page.</param>
    /// <param name="category">Functional category displayed in the Help Center.</param>
    /// <param name="icon">Icon displayed for the topic.</param>
    /// <param name="title">Topic title.</param>
    /// <param name="subtitle">Short functional description of the topic.</param>
    /// <param name="controller">MVC controller used by the related action link.</param>
    /// <param name="action">MVC action used by the related action link.</param>
    /// <param name="actionLabel">Label displayed for the related action link.</param>
    /// <param name="sections">Documentation sections that compose the page.</param>
    /// <returns>An administrator-only Help Center page definition.</returns>
    private static HelpDetailViewModel AdminPage(string id, string category, string icon, string title, string subtitle,
        string controller, string action, string actionLabel, params HelpSectionViewModel[] sections) =>
        new(id, category, icon, title, subtitle, controller, action, actionLabel, true, false, false, sections);

    /// <summary>
    /// Creates one documentation section.
    /// </summary>
    /// <param name="id">Stable anchor identifier for the section.</param>
    /// <param name="title">Section title.</param>
    /// <param name="description">Short description of the section purpose.</param>
    /// <param name="items">Documentation items displayed in the section.</param>
    /// <returns>A Help Center documentation section.</returns>
    private static HelpSectionViewModel Section(string id, string title, string description, params string[] items) =>
        new(id, title, description, items);
}
