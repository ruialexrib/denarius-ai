using System.Runtime.InteropServices;
using System.Text.Json;
using DenariusAI.Web.Models;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace DenariusAI.Web.Controllers;

[Authorize]
public sealed class InformationController(ApplicationInfo appInfo, IHttpClientFactory httpClientFactory, IMemoryCache cache) : Controller
{
    private const string RepositoryUrl = "https://github.com/ruialexrib/denarius-ai";

    [HttpGet] public IActionResult Help() => View();

    [HttpGet]
    public IActionResult HelpDetail(string id)
    {
        var pages = HelpPages();
        return pages.TryGetValue(id ?? string.Empty, out var page) ? View(page) : NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> WhatsNew(CancellationToken cancellationToken)
    {
        var releases = await GetReleasesAsync(cancellationToken);
        if (releases.Count == 0) releases = LocalReleases(appInfo.Version);
        var latest = releases.FirstOrDefault();
        var updateAvailable = latest is not null && IsNewerVersion(latest.Version, appInfo.Version);
        return View(new WhatsNewViewModel(appInfo.Version, RuntimeInformation.FrameworkDescription,
            RuntimeInformation.OSDescription, string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase),
            "SQL Server 2022 Express · Online", "denarius-ai-web · Online", "denarius-ai-mcp · Perfil opcional", RepositoryUrl, releases,
            latest?.Version, latest?.Url, updateAvailable));
    }

    private async Task<IReadOnlyList<ReleaseNoteViewModel>> GetReleasesAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue<IReadOnlyList<ReleaseNoteViewModel>>("github-latest-release", out var cached)) return cached ?? [];
        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DenariusAI/1.0");
            using var response = await client.GetAsync("https://api.github.com/repos/ruialexrib/denarius-ai/releases?per_page=1", cancellationToken);
            if (!response.IsSuccessStatusCode) return [];
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var releases = document.RootElement.EnumerateArray().Take(1).Select(release => new ReleaseNoteViewModel(
                release.GetProperty("tag_name").GetString() ?? "Versão",
                release.TryGetProperty("published_at", out var date) ? date.GetString()?[..10] ?? string.Empty : string.Empty,
                release.GetProperty("html_url").GetString() ?? RepositoryUrl,
                (release.GetProperty("body").GetString() ?? "Atualizações e melhorias.").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(8).ToList())).ToList();
            cache.Set("github-latest-release", releases, TimeSpan.FromMinutes(15));
            return releases;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException) { return []; }
    }

    private static bool IsNewerVersion(string releaseVersion, string currentVersion) =>
        Version.TryParse(releaseVersion.TrimStart('v', 'V').Split('-', '+')[0], out var latest)
        && Version.TryParse(currentVersion.Split('-', '+')[0], out var current)
        && latest > current;

    private static IReadOnlyList<ReleaseNoteViewModel> LocalReleases(string version) =>
    [
        new($"v{version}", DateTime.Today.ToString("yyyy-MM-dd"), RepositoryUrl,
        [
            "Primeira versão do DenariusAI.",
            "Gestão financeira pessoal e familiar com contabilidade por partidas dobradas, orçamentos e reconciliação.",
            "Colagem conversacional de extratos com identificação e classificação assistida de novos movimentos.",
            "Dashboard e análise financeira com comparações, projeções e relatórios inteligentes exportáveis em Markdown.",
            "Carteira de Certificados de Aforro integrada no Dashboard, na análise e no assistente.",
            "Assistente Mistral e preenchimento de movimentos através de linguagem natural.",
            "Gestão de utilizadores, permissões, preferências e definições globais da aplicação.",
            "Centro de ajuda detalhado para as principais operações."
        ])
    ];

    private static IReadOnlyDictionary<string, HelpDetailViewModel> HelpPages() =>
        new Dictionary<string, HelpDetailViewModel>(StringComparer.OrdinalIgnoreCase)
        {
            ["dashboard"] = Page("dashboard", "Dashboard", "Leia os indicadores e acompanhe a evolução financeira.", "Home", "Index", "Abrir Dashboard",
                ("Selecionar o período", ["Escolha Ano e Mês e clique em Aplicar; os cartões e a execução são recalculados.", "O gráfico anual apresenta janeiro a dezembro do ano selecionado.", "Passe o cursor pelos pontos para consultar valores exatos."]),
                ("Interpretar", ["Saldo disponível soma contas bancárias e dinheiro; património inclui ativos e certificados.", "Resultado é Rendimentos menos Despesas; Por reconciliar conta movimentos ainda não confirmados.", "Apenas movimentos ativos afetam os valores e as projeções não são garantias."])),
            ["movimentos"] = Page("movimentos", "Movimentos", "Crie, consulte, edite e anule operações por partidas dobradas.", "JournalEntries", "Index", "Consultar movimentos",
                ("Criar e campos", ["Clique em Novo movimento e preencha Data, Descrição e Orçamento; Referência e Notas são opcionais.", "Adicione duas ou mais linhas com Conta, Categoria, Descrição e apenas Débito ou Crédito.", "Débitos e créditos têm de ser iguais e devem existir duas contas diferentes.", "A IA apenas sugere; confirme e grave manualmente."]),
                ("Classificar", ["Numa despesa, debite a conta de despesa e credite a conta de pagamento.", "Num rendimento, credite a conta de rendimento e debite a conta que recebe.", "A categoria tem de ser compatível com o tipo da conta."]),
                ("Editar e anular", ["Consultar permite abrir os extratos das contas.", "Só pode editar movimentos ativos não reconciliados.", "Anular preserva a auditoria e retira o movimento dos cálculos; não pode ser revertido."])),
            ["assistant"] = Page("assistant", "Assistência por IA", "Use linguagem natural para consultar dados e preparar movimentos.", "Assistant", "Index", "Abrir Assistente",
                ("Interagir", ["Indique período, conta ou categoria na pergunta.", "Para movimentos inclua valor, data, origem, destino e finalidade.", "Responda às perguntas ou complete campos manualmente."]),
                ("Regras", ["A IA nunca grava automaticamente.", "Não introduza palavras-passe ou chaves.", "Confirme cálculos e classificações; não substitui aconselhamento profissional."])),
            ["statements"] = Page("statements", "Extratos", "Consulte o histórico e acompanhe o saldo de contas, categorias e grupos.", "Accounts", "Index", "Abrir Contas",
                ("Onde abrir", ["Na lista de Contas, Grupos ou Categorias, selecione Extrato na linha pretendida.", "Nos detalhes de um movimento, use o link da conta para abrir diretamente o respetivo extrato.", "O cabeçalho identifica sempre o elemento consultado e o tipo de extrato."]),
                ("Filtrar", ["Indique primeiro as datas De e Até, e depois os restantes critérios disponíveis.", "Clique em Aplicar; mudar um campo sem aplicar não atualiza os resultados.", "Limpar repõe os filtros e volta a apresentar o histórico completo.", "Escolha o número de registos por página; a opção mantém-se ao navegar entre páginas."]),
                ("Ler as linhas", ["Débito e Crédito mostram o efeito de cada partida sobre o elemento consultado.", "O saldo é calculado linha a linha, começando pelo saldo inicial quando existe.", "Os movimentos mais recentes aparecem primeiro; o saldo continua a representar a sequência cronológica correta.", "Descrição, referência e orçamento ajudam a localizar a origem da operação."]),
                ("Diferenças", ["O extrato de conta apresenta apenas partidas em que essa conta foi utilizada.", "O extrato de categoria agrega partidas classificadas nessa categoria.", "O extrato de grupo reúne as partidas das categorias pertencentes ao grupo.", "Um extrato não cria, edita nem reconcilia movimentos; serve para consulta e auditoria."]),
                ("Resolver problemas", ["Se faltar uma operação, confirme as datas e limpe os restantes filtros.", "Se o saldo parecer invertido, verifique o tipo da conta e o lado — débito ou crédito — usado no movimento.", "Se uma categoria não aparecer, confirme se foi atribuída à linha correta e se é compatível com a conta."])),
            ["reconciliation"] = Page("reconciliation", "Reconciliação", "Compare movimentos com operações reais do banco.", "Reconciliation", "Index", "Abrir Reconciliação",
                ("Reconciliar", ["Filtre por conta, datas, estado ou descrição e clique em Aplicar.", "Confirme data, referência e valor antes de Reconciliar.", "Desfazer retira a confirmação sem alterar valores."]),
                ("Colar movimentos", ["Escolha a conta do extrato e cole os movimentos na conversa.", "Inclua data, descrição, referência e valor; entradas são positivas e saídas negativas.", "A IA remove correspondências já registadas e sugere categoria e contrapartida.", "Na revisão confirme ou altere cada classificação antes de criar os movimentos."]),
                ("Regras", ["Só contas bancárias podem ser reconciliadas.", "Movimentos reconciliados não podem ser editados.", "Confirme duplicados pela data, referência, descrição e valor."])),
            ["budget"] = Page("budget", "Orçamento", "Planeie limites por categoria e período.", "Budget", "Index", "Gerir Orçamentos",
                ("Criar e editar", ["Selecione Ano e Mês, introduza montantes não negativos e clique em Gravar.", "Valor zero remove o planeamento efetivo.", "Use Grupo, pesquisa e ordenação."]),
                ("Execução", ["Associe explicitamente o orçamento ao movimento.", "O realizado não depende apenas da data; o orçamento recente é sugerido.", "Disponível é Orçamentado menos Realizado; acima de 100% indica excesso."])),
            ["certificates"] = Page("certificates", "Certificados de Aforro", "Gira subscrições e projeções.", "SavingsCertificates", "Index", "Gerir Certificados",
                ("Criar e campos", ["Preencha Data, Série/Número, Descrição, Investimento, Taxa, Valor atual e Próxima capitalização.", "Valores e taxa não podem ser negativos.", "Antiguidade, rendimento, diferença, juro e valor futuro são calculados."]),
                ("Editar e eliminar", ["Editar atualiza valor, taxa ou capitalização; não reutilize para outra subscrição.", "Eliminar remove definitivamente após confirmação.", "A eliminação não apaga movimentos contabilísticos."]),
                ("Cálculos", ["Rendimento é Valor atual menos Investimento.", "Juro líquido futuro aplica retenção de 28% e capitalização trimestral.", "Valor futuro soma o juro ao valor atual."])),
            ["analytics"] = Page("analytics", "Análise Financeira", "Explore comparações e relatórios.", "Analytics", "Index", "Abrir Análise",
                ("Filtros", ["Defina De e Até e, opcionalmente, Grupo, Categoria ou Conta.", "Clique em Aplicar para recalcular tudo.", "Use dados classificados e intervalos comparáveis."]),
                ("Relatório inteligente", ["Gerar relatório envia um resumo ao modelo.", "O preview é formatado; Ver Markdown mostra a fonte em monospace.", "Pode exportar .md; reveja as conclusões."])),
            ["tables"] = Page("tables", "Tabelas", "Configure grupos, categorias e contas.", "Accounts", "Index", "Gerir Contas",
                ("Grupos", ["Crie Nome, Descrição, Tipo e Ordem.", "Não pode mudar o tipo quando existem categorias.", "Desative primeiro categorias ativas; itens usados não são eliminados.", "Extrato mostra partidas das categorias do grupo."]),
                ("Categorias", ["Escolha Grupo, Nome, Descrição e Ordem.", "A categoria herda o tipo do grupo.", "Não pode mudar o grupo depois de usada; pode desativar.", "Extrato inclui classificações diretas e herdadas."]),
                ("Contas", ["Preencha Nome, Descrição, Tipo, Saldo inicial, Moeda de três letras e Categoria opcional.", "Rendimento, Despesa e Património exigem categorias compatíveis.", "Com movimentos não pode alterar tipo, moeda ou categoria.", "Use Consultar, Extrato, Editar e Desativar; desativar preserva histórico."]),
                ("Regras", ["Siga Grupo → Categoria → Conta → Orçamento → Movimento.", "Nomes claros melhoram filtros e IA.", "Tabelas estruturais usadas não são apagadas."])),
            ["preferences"] = Page("preferences", "Preferências e segurança", "Atualize perfil e credenciais.", "Account", "Profile", "Abrir Preferências",
                ("Perfil", ["Altere o Nome e guarde.", "O email pode exigir intervenção do administrador.", "Preferências não alteram definições globais."]),
                ("Palavra-passe", ["Introduza atual, nova e confirmação.", "A nova deve cumprir regras e coincidir.", "Termine a sessão em equipamentos partilhados."])),
            ["administration"] = Page("administration", "Administração", "Gira utilizadores, IA e dados.", "Settings", "Index", "Abrir Definições",
                ("Utilizadores", ["Crie Nome, Email, Palavra-passe e função.", "Editar permite mudar dados, função e redefinir palavra-passe.", "Eliminar remove o acesso; preserve o último administrador."]),
                ("Definições", ["Configure URL, chave, modelo, temperatura, tokens e prompts.", "Grave e use Testar ligação.", "São globais e exclusivas de administradores."]),
                ("Dados", ["Carregar demonstração adiciona exemplos.", "Reiniciar dados é destrutivo e exige confirmação.", "Faça cópia de segurança antes."]))
        };

    private static HelpDetailViewModel Page(string id, string title, string subtitle, string controller, string action,
        string actionLabel, params (string Title, string[] Items)[] sections) =>
        new(id, title, subtitle, controller, action, actionLabel,
            sections.Select(section => new HelpSectionViewModel(section.Title, section.Items)).ToList());
}
