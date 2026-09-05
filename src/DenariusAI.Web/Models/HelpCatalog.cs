using DenariusAI.Web.ViewModels;

namespace DenariusAI.Web.Models;

/// <summary>
/// Defines the user-facing functional documentation available in the DenariusAI Help Center.
/// </summary>
public static class HelpCatalog
{
    /// <summary>
    /// Gets all Help Center documentation topics keyed by their stable route identifier.
    /// </summary>
    public static IReadOnlyDictionary<string, HelpDetailViewModel> Pages { get; } = BuildPages();

    /// <summary>
    /// Returns the Help Center topics that the current user is allowed to discover.
    /// </summary>
    /// <param name="isAdministrator">Whether the current user has the administrator role.</param>
    /// <returns>An ordered list of visible documentation topics.</returns>
    public static IReadOnlyList<HelpDetailViewModel> VisiblePages(bool isAdministrator) =>
        Pages.Values.Where(page => isAdministrator || !page.AdministratorOnly).ToList();

    /// <summary>
    /// Builds the complete functional documentation catalogue.
    /// </summary>
    /// <returns>A case-insensitive dictionary of Help Center pages.</returns>
    private static IReadOnlyDictionary<string, HelpDetailViewModel> BuildPages()
    {
        var pages = new[]
        {
            Page("dashboard", "Visão geral", "⌂", "Dashboard", "Acompanhe a posição financeira, execução orçamental e evolução do período selecionado.", "Home", "Index", "Abrir Dashboard", featured: true,
                Section("visao-geral", "O que encontra nesta área", "O Dashboard reúne indicadores calculados pela aplicação e atalhos para as áreas que explicam esses valores.",
                    "Selecione Ano e Mês e aplique o período para recalcular os indicadores e gráficos dependentes dessa seleção.",
                    "Saldo disponível representa os ativos líquidos considerados pela aplicação; património agrega os ativos patrimoniais acompanhados pelo Denarius AI.",
                    "Resultado corresponde a rendimentos menos despesas no período aplicável; movimentos anulados não contribuem para os totais ativos.",
                    "Os indicadores de reconciliação mostram operações que ainda necessitam de confirmação bancária."),
                Section("graficos", "Gráficos e comparação", "Use os gráficos para perceber tendência e composição sem perder o detalhe contabilístico.",
                    "Os gráficos anuais apresentam a evolução ao longo dos meses do ano selecionado e os valores exatos ficam disponíveis ao passar o cursor sobre os pontos.",
                    "Orçamentado vs. realizado depende dos movimentos ativos associados explicitamente a cada orçamento.",
                    "Rendimentos vs. despesas utiliza os movimentos classificados no respetivo tipo financeiro; a seleção de período condiciona a leitura."),
                Section("duvidas", "Resolver dúvidas", "Verificações úteis quando um total não corresponde ao esperado.",
                    "Confirme primeiro o período aplicado e se o movimento está ativo.",
                    "Se um valor de orçamento estiver em falta, confirme se o movimento está associado ao orçamento correto e a uma categoria abrangida.",
                    "Se um saldo parecer incorreto, abra o extrato da conta e confirme as partidas que formam o valor.")),

            Page("movimentos", "Contabilidade", "↔", "Movimentos", "Registe operações financeiras com partidas dobradas e mantenha um histórico contabilístico auditável.", "JournalEntries", "Index", "Consultar movimentos",
                Section("conceito", "Como funciona um movimento", "Cada movimento contabilístico é composto por um cabeçalho e por pelo menos duas linhas equilibradas.",
                    "O cabeçalho identifica Data, Descrição, Referência, Notas e, quando aplicável, Orçamento.",
                    "Cada linha identifica Conta, Categoria quando aplicável, descrição da linha e um valor exclusivamente a Débito ou a Crédito.",
                    "O total dos débitos tem de ser igual ao total dos créditos e o movimento deve envolver pelo menos duas contas diferentes."),
                Section("campos", "Campos e classificação", "Os campos determinam como a operação entra nos saldos, categorias e orçamento.",
                    "Data determina quando a operação é reconhecida; Descrição identifica a finalidade; Referência e Notas acrescentam contexto e são opcionais quando o formulário o permite.",
                    "Conta define onde a partida é registada. Categoria classifica a componente de rendimento ou despesa e tem de ser compatível com o tipo da conta.",
                    "Numa despesa, a conta de despesa é debitada e a conta usada para pagamento é creditada; num rendimento, a conta que recebe é debitada e a conta de rendimento é creditada.",
                    "Numa transferência entre ativos, a conta de destino é debitada e a origem é creditada; normalmente não existe categoria."),
                Section("estado", "Editar, reconciliar e anular", "O estado do movimento condiciona as operações disponíveis.",
                    "Movimentos reconciliados não podem ser editados enquanto mantiverem esse estado.",
                    "Anular preserva o histórico contabilístico e retira o movimento dos cálculos ativos; não é equivalente a apagar o registo.",
                    "Use os detalhes do movimento para seguir as contas e extratos relacionados antes de corrigir uma classificação."),
                Section("ia", "Preenchimento assistido por IA", "A IA pode propor dados, mas não substitui a validação contabilística da aplicação nem a confirmação do utilizador.",
                    "Inclua na informação de origem o valor, data, finalidade e contas ou contexto que sejam conhecidos.",
                    "Revise contas, categorias, orçamento, descrição, referência e valores antes de gravar.",
                    "Uma sugestão incompleta ou ambígua deve ser corrigida ou completada pelo utilizador; a IA não grava o movimento automaticamente."),
                Section("duvidas", "Resolver problemas", "Causas frequentes de validação ou resultados inesperados.",
                    "Se o movimento não grava, confirme que existe apenas Débito ou Crédito em cada linha e que os totais estão equilibrados.",
                    "Se uma categoria não aparece, confirme a compatibilidade com o tipo da conta selecionada.",
                    "Se não consegue editar, confirme se o movimento está reconciliado ou anulado.")),

            Page("statements", "Contabilidade", "≡", "Extratos", "Consulte todas as partidas que explicam o saldo de uma conta, categoria ou grupo.", "Accounts", "Index", "Abrir Contas",
                Section("abrir", "Onde abrir um extrato", "Os extratos são uma ferramenta de consulta e auditoria e não alteram registos.",
                    "Na lista de Contas, Grupos ou Categorias, use a ação Extrato no elemento pretendido.",
                    "Nos detalhes de um movimento pode seguir a ligação da conta para consultar o respetivo histórico.",
                    "O cabeçalho identifica o elemento e o tipo de extrato que está a consultar."),
                Section("filtros", "Filtros", "Reduza o histórico apresentado sem alterar os dados guardados.",
                    "Defina De e Até e combine os restantes critérios disponíveis; clique em Aplicar para atualizar os resultados.",
                    "Limpar repõe os filtros e volta ao histórico completo.",
                    "A escolha do número de registos por página acompanha a navegação entre páginas."),
                Section("leitura", "Débito, crédito e saldo", "Leia cada linha como uma partida contabilística do elemento consultado.",
                    "Débito e Crédito mostram o lado da partida; o saldo é calculado sequencialmente, considerando o saldo inicial quando exista.",
                    "O extrato de conta inclui partidas da conta; o de categoria agrega classificações nessa categoria; o de grupo reúne as categorias que lhe pertencem.",
                    "Descrição, referência, movimento e orçamento ajudam a localizar a origem do valor.")),

            Page("reconciliation", "Contabilidade", "✓", "Reconciliação", "Compare os movimentos contabilísticos com as operações reais da conta bancária.", "Reconciliation", "Index", "Abrir Reconciliação",
                Section("processo", "Reconciliação manual", "A reconciliação confirma que uma operação contabilística corresponde à realidade bancária.",
                    "Filtre por conta bancária, datas, estado ou descrição e aplique os critérios.",
                    "Antes de reconciliar confirme data, valor, descrição e referência.",
                    "Desfazer a reconciliação retira a confirmação, mas não altera o movimento contabilístico."),
                Section("restricoes", "Regras e restrições", "As restrições protegem a correspondência entre contabilidade e extrato bancário.",
                    "A reconciliação aplica-se a contas bancárias elegíveis.",
                    "Um movimento reconciliado não pode ser editado até a reconciliação ser desfeita.",
                    "Verifique potenciais duplicados pela combinação de data, referência, descrição e valor."),
                Section("ia", "Colar movimentos e classificar com IA", "A classificação assistida prepara propostas que continuam sob controlo do utilizador.",
                    "Escolha a conta do extrato e cole linhas contendo data, descrição, referência quando exista e valor; na importação, entradas são positivas e saídas negativas.",
                    "A aplicação compara os dados com movimentos existentes e a IA pode sugerir contrapartida, categoria e restantes campos permitidos.",
                    "Revise cada proposta antes de processar. Linhas com validação bloqueante não devem ser reconciliadas nem persistidas.")),

            Page("budget", "Planeamento", "▦", "Orçamentos", "Planeie valores mensais por categoria e acompanhe a execução real.", "Budget", "Index", "Gerir Orçamentos",
                Section("periodo", "Período e planeamento", "Cada orçamento corresponde a um mês e ano específicos.",
                    "Selecione Ano e Mês e introduza montantes não negativos nas categorias que pretende planear.",
                    "Um valor zero representa ausência de planeamento efetivo para essa categoria.",
                    "Use grupo, pesquisa e ordenação para encontrar rapidamente as categorias relevantes."),
                Section("associacao", "Associação aos movimentos", "A execução depende da associação explícita entre o movimento e o orçamento.",
                    "A data por si só não integra automaticamente um movimento no orçamento.",
                    "Movimentos associados devem respeitar as regras de período aplicáveis; diferenças próximas podem originar aviso quando o fluxo o permite.",
                    "Movimentos anulados ficam excluídos da execução ativa."),
                Section("calculos", "Valores apresentados", "Os indicadores são calculados deterministicamente pela aplicação.",
                    "Realizado soma os movimentos ativos associados ao orçamento e classificados nas respetivas categorias.",
                    "Disponível corresponde a Orçamentado menos Realizado.",
                    "Uma execução acima de 100% indica que o realizado ultrapassou o valor planeado.")),

            Page("accounts", "Estrutura financeira", "¤", "Contas", "Configure as contas que recebem as partidas contabilísticas e determinam os saldos.", "Accounts", "Index", "Gerir Contas",
                Section("campos", "Campos", "A ficha da conta define identidade, natureza contabilística e apresentação monetária.",
                    "Nome e Descrição identificam a conta; Tipo determina o comportamento contabilístico.",
                    "Saldo inicial estabelece o ponto de partida do extrato quando aplicável.",
                    "Moeda utiliza um código de três letras; Categoria é usada apenas nos tipos em que a estrutura financeira a exige."),
                Section("regras", "Regras após utilização", "Contas com histórico ficam sujeitas a restrições para preservar a coerência dos movimentos existentes.",
                    "Depois de existirem movimentos, tipo, moeda ou associação estrutural podem deixar de poder ser alterados.",
                    "Rendimentos, Despesas e outros tipos categorizáveis exigem categorias compatíveis.",
                    "Desativar preserva o histórico e impede a utilização futura quando aplicável; não é o mesmo que eliminar dados já usados."),
                Section("extrato", "Extrato e saldo", "Use o extrato para explicar o valor atual da conta.",
                    "Cada partida mostra o lado a débito ou crédito e o movimento de origem.",
                    "Se o saldo parecer incorreto, confirme o saldo inicial, o tipo da conta e as partidas registadas.")),

            Page("groups", "Estrutura financeira", "▤", "Grupos financeiros", "Organize categorias por natureza financeira e mantenha uma estrutura coerente.", "FinancialGroups", "Index", "Gerir Grupos",
                Section("campos", "Campos", "Os grupos são o nível superior da classificação financeira.",
                    "Nome e Descrição identificam o grupo; Tipo define a natureza financeira herdada pelas categorias; Ordem controla a apresentação.",
                    "Use uma designação estável e reconhecível para melhorar filtros, relatórios e sugestões assistidas."),
                Section("regras", "Alterações e utilização", "As dependências existentes limitam alterações que possam invalidar classificações.",
                    "O tipo não deve ser alterado quando o grupo já possui categorias incompatíveis com a nova natureza.",
                    "Categorias ativas podem ter de ser desativadas antes de desativar a estrutura que as suporta.",
                    "Elementos já utilizados devem preservar o histórico em vez de serem fisicamente removidos."),
                Section("extrato", "Extrato do grupo", "O extrato agrega a atividade classificada nas categorias do grupo.",
                    "Use-o para investigar a composição de um total por natureza financeira sem alterar movimentos.")),

            Page("categories", "Estrutura financeira", "☷", "Categorias", "Classifique rendimentos e despesas dentro dos grupos financeiros.", "Categories", "Index", "Gerir Categorias",
                Section("campos", "Campos", "A categoria liga a classificação operacional ao respetivo grupo financeiro.",
                    "Grupo define a natureza herdada; Nome e Descrição identificam a finalidade; Ordem controla a apresentação.",
                    "A categoria deve ser escolhida na linha contabilística correspondente ao rendimento ou à despesa, não numa transferência comum entre ativos."),
                Section("regras", "Compatibilidade e histórico", "A aplicação impede mudanças que tornem os movimentos antigos incoerentes.",
                    "A categoria herda o tipo do grupo e só é disponibilizada em contas ou operações compatíveis.",
                    "Depois de utilizada, a mudança de grupo pode ficar bloqueada; desativar preserva as classificações históricas.",
                    "Se uma categoria não surge num formulário, confirme primeiro a conta e o tipo financeiro selecionados."),
                Section("extrato", "Extrato da categoria", "O extrato reúne todas as partidas classificadas nessa categoria.",
                    "Utilize filtros de data e pesquisa para explicar o total e identificar movimentos incorretamente classificados.")),

            Page("certificates", "Poupança e investimento", "◆", "Certificados de Aforro", "Acompanhe subscrições, valor atual, rendimento e próximas capitalizações.", "SavingsCertificates", "Index", "Gerir Certificados",
                Section("campos", "Campos da subscrição", "Registe os dados necessários para identificar e acompanhar cada subscrição.",
                    "Data identifica a subscrição; Série/Número e Descrição permitem reconhecer o produto.",
                    "Investimento representa o capital aplicado; Taxa e Valor atual suportam os indicadores apresentados.",
                    "Próxima capitalização indica a data relevante para o próximo cálculo ou atualização.",
                    "Valores monetários e taxas sujeitos a validação não podem ser negativos."),
                Section("calculos", "Cálculos", "Os valores derivados são calculados pela aplicação a partir dos dados guardados.",
                    "Rendimento corresponde à diferença entre Valor atual e Investimento.",
                    "Os indicadores de juro e valor futuro utilizam as regras implementadas para a série e os dados introduzidos; trate projeções como estimativas e não como garantia de retorno.",
                    "Atualizar o valor atual permite manter a carteira alinhada com a informação disponível."),
                Section("ciclo", "Editar e eliminar", "As ações sobre a subscrição não alteram automaticamente movimentos contabilísticos relacionados.",
                    "Editar deve atualizar a mesma subscrição e não reutilizar o registo para representar um investimento diferente.",
                    "Eliminar remove o registo de acompanhamento após confirmação; verifique antes se necessita de preservar a referência histórica.")),

            Page("stocks", "Poupança e investimento", "↗", "Portefólio de ações", "Registe posições e watchlist, acompanhe cotações, histórico e previsões indicativas.", "StockPortfolio", "Index", "Abrir Portefólio",
                Section("identificacao", "Identificação do instrumento", "O fornecedor de mercado usa estes campos para localizar o instrumento correto.",
                    "Ticker deve ser o símbolo reconhecido pelo fornecedor, por exemplo EDP.LS; Nome identifica a empresa ou instrumento.",
                    "Mercado identifica a bolsa; Moeda usa o código ISO de três letras da negociação."),
                Section("posicao", "Posição e valorização", "Quantidade e preço médio determinam o custo e o ganho ou perda apresentados.",
                    "Quantidade representa as unidades detidas; Preço médio de compra é o custo médio efetivamente pago por ação.",
                    "Cotação atual e Data da cotação identificam o valor de mercado de referência.",
                    "Uma posição marcada Apenas watchlist é acompanhada sem ser considerada uma posição detida da carteira."),
                Section("historico", "Histórico e recolha", "A data inicial controla o período de análise dentro dos dados disponibilizados pelo fornecedor.",
                    "Recolher histórico desde define a referência temporal; limites do plano do fornecedor podem reduzir a profundidade efetivamente disponível.",
                    "A página de evolução apresenta a série histórica, mínimo, máximo e variação do período com base nos dados recolhidos."),
                Section("previsao", "Previsões de cotações", "A previsão é uma projeção estatística e não uma promessa de preço futuro.",
                    "Ativar previsão permite calcular horizontes de 30, 60 e 90 dias quando existem pelo menos 60 observações.",
                    "Os intervalos apresentados exprimem incerteza do modelo; não devem ser interpretados como recomendação de investimento.",
                    "A previsão não altera quantidades, custos, cotações ou qualquer outro registo da carteira."),
                Section("duvidas", "Resolver problemas", "Verifique a identificação e a disponibilidade de dados antes de concluir que existe erro.",
                    "Se a cotação não for recolhida, confirme ticker, mercado e configuração do serviço de ações.",
                    "Se não existir previsão, confirme se a opção está ativa e se existem observações históricas suficientes.")),

            Page("insurance", "Proteção", "◈", "Seguros", "Gira apólices, calendário, prémios e associações aos movimentos financeiros.", "Insurance", "Index", "Gerir Seguros",
                Section("identificacao", "Identificação", "A apólice identifica o contrato e o objeto protegido.",
                    "Nome identifica o contrato; Seguradora identifica a entidade; Número da apólice é a referência contratual.",
                    "Objeto seguro descreve o bem, pessoa ou risco coberto."),
                Section("cobertura", "Cobertura e pagamento", "Classifique a apólice e a frequência habitual do respetivo prémio.",
                    "Tipo pode representar Habitação, Automóvel, Saúde, Vida, Acidentes pessoais ou Outro.",
                    "Frequência de pagamento pode ser Mensal, Trimestral, Semestral, Anual ou Irregular.",
                    "O registo da apólice não substitui os prémios: cada pagamento é acompanhado separadamente."),
                Section("datas", "Vigência e renovação", "As datas ajudam a acompanhar o ciclo contratual.",
                    "Data de início e Data de fim delimitam a vigência conhecida; Data de renovação assinala a próxima revisão relevante.",
                    "Notas podem guardar franquias, coberturas ou observações que não tenham campo próprio."),
                Section("premios", "Prémios e movimentos", "Os encargos do seguro podem ser associados a movimentos financeiros existentes.",
                    "Quando um prémio está associado a um movimento ativo, esse movimento funciona como fonte de verdade para o estado e a data efetiva do pagamento.",
                    "Comprovativos do prémio são tratados no fluxo próprio e não devem ser confundidos com a identificação da apólice."),
                Section("ia", "Preenchimento assistido", "Na criação, a IA pode interpretar dados copiados e propor o preenchimento do formulário.",
                    "Só os campos reconhecidos devem ser preenchidos; reveja todos os dados propostos.",
                    "Nada é gravado sem submissão e confirmação do formulário pelo utilizador.",
                    "A funcionalidade depende de existir um fornecedor de IA configurado e disponível.")),

            Page("warranties", "Proteção", "□", "Garantias", "Registe garantias, comprovativos e avisos antes da data de expiração.", "Warranties", "Index", "Gerir Garantias",
                Section("campos", "Campos", "A garantia reúne a identificação do bem ou serviço e o respetivo período de cobertura.",
                    "Nome identifica a garantia; Fornecedor ajuda a localizar a entidade onde o bem ou serviço foi adquirido.",
                    "Data de compra e Data de expiração definem o período de cobertura.",
                    "Dias de antecedência define quando o aviso associado deve começar a ser apresentado e aceita valores entre 0 e 3650.",
                    "Notas guardam contexto adicional."),
                Section("documento", "Documento PDF", "Pode associar o comprovativo ou certificado à garantia.",
                    "O ficheiro tem de ser um PDF válido e não pode exceder 10 MB.",
                    "Ao editar, o documento atual só é substituído quando selecionar outro PDF."),
                Section("lembrete", "Lembrete automático", "O aviso acompanha o ciclo da garantia.",
                    "A aplicação atualiza automaticamente o lembrete associado quando altera a garantia.",
                    "Confirme a data de expiração e a antecedência se o aviso aparecer demasiado cedo ou demasiado tarde.")),

            Page("correspondence", "Documentos", "✉", "Correspondência", "Registe correspondência recebida e associe o documento PDF ao respetivo contexto.", "Correspondence", "Index", "Gerir Correspondência",
                Section("campos", "Campos", "Os metadados permitem localizar e compreender o documento sem ter de o abrir.",
                    "Assunto identifica o conteúdo; Remetente identifica a origem; Data de receção regista quando a correspondência foi recebida.",
                    "Notas guardam contexto adicional, decisões ou referências úteis."),
                Section("documento", "Documento PDF", "O documento fica associado ao registo de correspondência.",
                    "Só são aceites ficheiros PDF válidos até 10 MB.",
                    "Numa edição, o ficheiro existente é mantido exceto quando selecionar um novo PDF para o substituir."),
                Section("metadata", "Metadados do documento", "Use a consulta de metadados para verificar a informação técnica disponível sem alterar o ficheiro.",
                    "A consulta serve para apoio documental; não substitui a validação do conteúdo da correspondência.")),

            Page("reminders", "Organização", "!", "Lembretes", "Crie avisos para eventos e prazos e controle quando cada alerta fica ativo.", "Reminders", "Index", "Gerir Lembretes",
                Section("campos", "Campos", "Cada lembrete combina uma descrição, uma data e uma antecedência.",
                    "Texto descreve o evento ou ação a recordar.",
                    "Data do evento identifica o dia relevante.",
                    "Dias de antecedência define quando o alerta começa a ser apresentado e aceita valores entre 0 e 3650."),
                Section("comportamento", "Quando o alerta aparece", "O início do aviso é calculado a partir da data do evento e da antecedência.",
                    "Quando entra na janela de aviso, o lembrete fica disponível para os utilizadores abrangidos.",
                    "O aviso permanece ativo até cada utilizador efetuar a confirmação prevista pelo fluxo.",
                    "Algumas áreas, como Garantias, podem manter lembretes associados automaticamente; altere a origem quando esse vínculo existir.")),

            Page("analytics", "Análise", "⌁", "Análise Financeira", "Explore a evolução financeira com filtros, comparações e relatórios explicativos.", "Analytics", "Index", "Abrir Análise",
                Section("filtros", "Filtros", "Defina um universo de dados coerente antes de interpretar os indicadores.",
                    "Selecione De e Até e, quando disponível, Grupo, Categoria ou Conta; clique em Aplicar para recalcular a análise.",
                    "Use intervalos comparáveis e dados corretamente classificados para evitar interpretações enganadoras."),
                Section("indicadores", "Indicadores e comparações", "Totais, percentagens e variações são calculados deterministicamente pela aplicação.",
                    "Movimentos anulados ficam excluídos dos cálculos ativos, salvo quando a área indique explicitamente análise histórica de auditoria.",
                    "Use os extratos e movimentos subjacentes quando precisar de explicar um valor agregado."),
                Section("relatorio", "Relatório inteligente", "A IA recebe um resumo financeiro previamente calculado e produz uma interpretação em linguagem natural.",
                    "Gerar relatório não delega ao modelo a aritmética contabilística; os valores são preparados pela aplicação.",
                    "O preview apresenta o texto formatado; Ver Markdown mostra a origem textual e a exportação permite guardar o relatório quando disponível.",
                    "Reveja as conclusões: o relatório explica os dados e não constitui aconselhamento financeiro profissional.")),

            Page("assistant", "Inteligência artificial", "✦", "Assistência por IA", "Consulte os seus dados em linguagem natural e prepare sugestões sem perder o controlo das alterações.", "Assistant", "Index", "Abrir Assistente", aiTopic: true,
                Section("consultas", "Conversar sobre os dados", "As perguntas devem fornecer contexto suficiente para restringir a resposta.",
                    "Indique período, conta, categoria, orçamento ou outra referência relevante sempre que a pergunta possa ser ambígua.",
                    "O assistente utiliza apenas as ferramentas e dados autorizados para o utilizador autenticado."),
                Section("movimentos", "Preparar movimentos", "O assistente pode recolher informação e propor um movimento para revisão.",
                    "Inclua valor, data, origem, destino e finalidade quando conhecidos.",
                    "Se faltar informação essencial, a interação pode pedir clarificação em vez de inventar contas, categorias ou valores.",
                    "A sugestão permanece editável e não é gravada sem confirmação do utilizador."),
                Section("limites", "Limites e segurança", "A IA interpreta e explica; as regras financeiras determinísticas continuam na aplicação.",
                    "Não introduza palavras-passe, chaves de API ou outros segredos na conversa.",
                    "Confirme classificações e conclusões, sobretudo quando a informação de origem é incompleta.",
                    "A configuração do fornecedor pode condicionar disponibilidade, latência e limite de contexto.")),

            Page("preferences", "Conta", "◎", "Perfil e segurança", "Atualize os dados da sua conta e mantenha as credenciais protegidas.", "Account", "Profile", "Abrir Perfil",
                Section("perfil", "Perfil", "As preferências pessoais afetam a conta autenticada e não as definições globais da aplicação.",
                    "Atualize o Nome e grave as alterações.",
                    "Alterações a dados de identidade ou email podem estar sujeitas às regras administrativas existentes."),
                Section("password", "Palavra-passe", "Use o fluxo dedicado para alterar a palavra-passe.",
                    "Introduza a palavra-passe atual, a nova e a confirmação conforme solicitado.",
                    "A nova palavra-passe tem de cumprir as regras configuradas e coincidir com a confirmação.",
                    "Termine a sessão em equipamentos partilhados e nunca divulgue credenciais na área de IA.")),

            Page("users", "Administração", "♙", "Utilizadores", "Administre contas, funções e acesso à aplicação.", "Users", "Index", "Gerir Utilizadores", administratorOnly: true,
                Section("gestao", "Criar e editar", "A gestão de utilizadores está reservada a administradores.",
                    "Crie a conta com Nome, Email, Palavra-passe e função de acesso aplicável.",
                    "Na edição, altere apenas os dados e permissões necessários; a redefinição de palavra-passe deve seguir o fluxo próprio."),
                Section("permissoes", "Funções e acesso", "As funções controlam a visibilidade e autorização de operações administrativas.",
                    "Não use a gestão de utilizadores para contornar a autorização de uma área.",
                    "Preserve pelo menos uma conta administrativa funcional antes de remover ou reduzir privilégios de outro administrador."),
                Section("login", "Histórico de autenticação", "Consulte o histórico associado quando precisar de investigar acessos ou falhas de autenticação.",
                    "O histórico é informação administrativa e deve ser tratado como dado de segurança.")),

            Page("audit", "Administração", "⌕", "Auditoria", "Consulte o histórico das alterações relevantes efetuadas na aplicação.", "Audit", "Index", "Abrir Auditoria", administratorOnly: true,
                Section("registos", "O que é registado", "A auditoria permite perceber quem alterou um registo e em que momento, dentro dos eventos suportados.",
                    "Os eventos podem incluir inserção, alteração, anulação ou reativação e eliminação conforme o tipo de registo.",
                    "A listagem identifica o tipo de registo, operação, momento e utilizador; os detalhes mostram a informação relevante antes e depois quando disponível."),
                Section("navegacao", "Consultar histórico", "Use os filtros para localizar o registo ou operação pretendidos.",
                    "Quando uma área possui ligação direta para auditoria, abra primeiro o histórico do registo e depois selecione o evento concreto.",
                    "A auditoria não deve expor palavras-passe, hashes, tokens ou chaves.")),

            Page("login-history", "Administração", "◷", "Histórico de autenticação", "Analise tentativas de autenticação e atividade de acesso relevante.", "Users", "LoginHistory", "Abrir Histórico", administratorOnly: true,
                Section("consulta", "Consultar", "O histórico ajuda a investigar acessos e dificuldades de autenticação.",
                    "Use os filtros e dados apresentados para localizar o utilizador, momento e resultado relevante.",
                    "A informação é administrativa e não substitui os logs técnicos quando a investigação exige detalhe de infraestrutura."),
                Section("seguranca", "Boas práticas", "Trate o histórico como informação de segurança.",
                    "Não copie credenciais ou outros segredos para notas, comentários ou ferramentas externas.",
                    "Uma ocorrência isolada deve ser interpretada no respetivo contexto antes de concluir que existe incidente.")),

            Page("settings", "Administração", "⚙", "Definições e operações globais", "Configure IA e execute operações administrativas sobre os dados com conhecimento das consequências.", "Settings", "Index", "Abrir Definições", administratorOnly: true,
                Section("ai-config", "Configuração de IA", "As definições determinam o fornecedor e comportamento das funcionalidades assistidas.",
                    "Configure o fornecedor, URL quando aplicável, credencial, modelo e parâmetros de geração disponibilizados pelo formulário.",
                    "Os prompts operacionais configuráveis devem refletir o comportamento pretendido e podem afetar extração, classificação e relatórios.",
                    "Use Testar ligação antes de depender da IA; a indisponibilidade do fornecedor não deve alterar regras financeiras determinísticas."),
                Section("backup", "Cópia de segurança e restauro", "Estas operações abrangem dados da aplicação e devem ser tratadas como um processo de continuidade.",
                    "Crie uma cópia de segurança antes de operações destrutivas ou de substituição de dados.",
                    "O restauro valida o formato e relações antes de substituir dados; falhas devem impedir uma alteração parcial.",
                    "Confirme sempre que está a utilizar o ficheiro pretendido e não exponha backups que contenham informação financeira."),
                Section("demo", "Dados de demonstração", "O carregamento de demonstração cria um cenário de exemplo destinado a explorar a aplicação.",
                    "Utilize esta opção apenas quando pretende adicionar ou repor o conjunto de demonstração previsto pelo fluxo atual.",
                    "Não confunda dados de demonstração com uma cópia de segurança de dados reais."),
                Section("reset", "Reinicialização de dados financeiros", "A reinicialização é destrutiva e exige confirmação explícita.",
                    "Leia o contexto apresentado no formulário antes de executar a operação.",
                    "Faça uma cópia de segurança quando necessitar de possibilidade de recuperação.",
                    "A operação deve manter as garantias de integridade definidas pela aplicação e não deve ser usada como mecanismo normal de correção de movimentos."),
                Section("duvidas", "Resolver problemas", "Separe problemas de configuração externa de problemas de dados.",
                    "Se a IA não responder, teste a ligação e confirme fornecedor, endpoint, modelo e credencial sem expor o segredo.",
                    "Se um restauro for recusado, confirme versão/formato do ficheiro e não tente contornar a validação.",
                    "Se uma opção não estiver visível, confirme que iniciou sessão com uma conta administrativa."))
        };

        return pages.ToDictionary(page => page.Id, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates one Help Center documentation page.
    /// </summary>
    /// <param name="id">Stable route identifier.</param>
    /// <param name="category">Logical category displayed in the Help Center.</param>
    /// <param name="icon">Visual marker displayed on the topic card.</param>
    /// <param name="title">Topic title.</param>
    /// <param name="subtitle">Topic summary.</param>
    /// <param name="controller">Destination application controller.</param>
    /// <param name="action">Destination application action.</param>
    /// <param name="actionLabel">Destination link label.</param>
    /// <param name="administratorOnly">Whether only administrators may discover and open the documentation.</param>
    /// <param name="featured">Whether the card uses the primary visual treatment.</param>
    /// <param name="aiTopic">Whether the topic uses the AI visual treatment.</param>
    /// <param name="sections">Ordered documentation sections.</param>
    /// <returns>A configured documentation page.</returns>
    private static HelpDetailViewModel Page(
        string id,
        string category,
        string icon,
        string title,
        string subtitle,
        string controller,
        string action,
        string actionLabel,
        bool administratorOnly = false,
        bool featured = false,
        bool aiTopic = false,
        params HelpSectionViewModel[] sections) =>
        new(id, category, icon, title, subtitle, controller, action, actionLabel, administratorOnly, featured, aiTopic, sections);

    /// <summary>
    /// Creates one section within a Help Center documentation page.
    /// </summary>
    /// <param name="id">Stable in-page anchor.</param>
    /// <param name="title">Section title.</param>
    /// <param name="description">Section description.</param>
    /// <param name="items">Documentation items.</param>
    /// <returns>A configured help section.</returns>
    private static HelpSectionViewModel Section(string id, string title, string description, params string[] items) =>
        new(id, title, description, items);
}
