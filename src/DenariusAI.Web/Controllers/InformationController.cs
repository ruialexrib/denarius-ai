using System.Runtime.InteropServices;
using System.Text.Json;
using DenariusAI.Web.Models;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DenariusAI.Web.Controllers;

[Authorize]
public sealed class InformationController(ApplicationInfo appInfo, IHttpClientFactory httpClientFactory) : Controller
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
        return View(new WhatsNewViewModel(appInfo.Version, RuntimeInformation.FrameworkDescription,
            RuntimeInformation.OSDescription, string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase),
            "SQL Server 2022 Express · Online", "denarius-ai-web · Online", "denarius-ai-mcp · Perfil opcional", RepositoryUrl, releases));
    }

    private async Task<IReadOnlyList<ReleaseNoteViewModel>> GetReleasesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient(); client.DefaultRequestHeaders.UserAgent.ParseAdd("DenariusAI/1.0");
            using var response = await client.GetAsync("https://api.github.com/repos/ruialexrib/denarius-ai/releases?per_page=1", cancellationToken);
            if (!response.IsSuccessStatusCode) return [];
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            return document.RootElement.EnumerateArray().Take(1).Select(release => new ReleaseNoteViewModel(
                release.GetProperty("tag_name").GetString() ?? "Versão",
                release.TryGetProperty("published_at", out var date) ? date.GetString()?[..10] ?? string.Empty : string.Empty,
                release.GetProperty("html_url").GetString() ?? RepositoryUrl,
                (release.GetProperty("body").GetString() ?? "Atualizações e melhorias.").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(8).ToList())).ToList();
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException) { return []; }
    }

    private static IReadOnlyList<ReleaseNoteViewModel> LocalReleases(string version) =>
    [
        new($"v{version}", DateTime.Today.ToString("yyyy-MM-dd"), RepositoryUrl,
        [
            "Primeira versão do DenariusAI.",
            "Gestão financeira pessoal e familiar com contabilidade por partidas dobradas, orçamentos e reconciliação.",
            "Importação de extratos Excel com identificação e classificação assistida de novos movimentos.",
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
            ["dashboard"] = Page("dashboard", "Dashboard", "Compreenda os principais indicadores da sua situação financeira.", "Home", "Index", "Abrir Dashboard",
                ("Período e indicadores", ["Escolha o mês e o ano no topo e clique em Aplicar.", "Saldo disponível agrega contas bancárias e dinheiro; património inclui os restantes ativos e Certificados de Aforro.", "Rendimentos, despesas e resultado mensal consideram movimentos ativos do período selecionado."]),
                ("Certificados e orçamento", ["Os cartões dos certificados mostram valor atual, capitalizações atingidas, juro líquido futuro e valor futuro.", "A execução compara o orçamento definido com o realizado associado ao orçamento.", "Os gráficos apresentam a evolução dos últimos seis meses e as categorias com maior execução."])),
            ["movimentos"] = Page("movimentos", "Movimentos", "Registe operações financeiras segundo o princípio das partidas dobradas.", "JournalEntries", "Index", "Consultar movimentos",
                ("Como registar", ["Indique data, descrição, referência e, quando aplicável, o orçamento a que pertence.", "Cada movimento precisa de pelo menos duas linhas e o total dos débitos deve ser igual ao total dos créditos.", "A conta representa onde o valor entra ou sai; a categoria explica a finalidade financeira da linha."]),
                ("Estados e cuidados", ["Revise sempre contas, sentidos e valores antes de gravar.", "Movimentos anulados deixam de afetar saldos e análises, mantendo o histórico de auditoria.", "Filtros, paginação e número de registos por página mantêm-se durante a navegação."])),
            ["assistant"] = Page("assistant", "Assistência por IA", "Utilize linguagem natural para consultar dados ou preparar movimentos.", "Assistant", "Index", "Abrir Assistente",
                ("Preenchimento de movimentos", ["Descreva a operação como a diria normalmente, incluindo valor, data, origem e destino quando souber.", "O assistente sugere os campos; informação em falta deve ser completada na conversa ou manualmente.", "A sugestão nunca é gravada automaticamente: confirme todos os campos e clique em Gravar."]),
                ("Conversação financeira", ["O assistente pode considerar contas, movimentos, orçamentos, categorias e Certificados de Aforro.", "As respostas dependem da qualidade e atualização dos dados registados.", "Não trate a resposta do modelo como aconselhamento financeiro profissional."])),
            ["reconciliation"] = Page("reconciliation", "Reconciliação", "Confirme que os movimentos da aplicação correspondem ao extrato bancário.", "Reconciliation", "Index", "Abrir Reconciliação",
                ("Reconciliação manual", ["Filtre por conta, datas e estado para localizar movimentos.", "Marque como reconciliado apenas quando confirmar o registo no extrato; pode desfazer a operação.", "O estado e a auditoria permitem identificar quando e por quem foi feita a confirmação."]),
                ("Importação Excel", ["Importe ficheiros .xlsx ou .xlsm com Data, Descrição, Referência e Valor, ou Débito e Crédito.", "A aplicação exclui correspondências já existentes e abre uma revisão apenas com novos registos.", "Confirme as sugestões de categoria e contrapartida linha a linha antes de criar os movimentos."])),
            ["budget"] = Page("budget", "Orçamento", "Planeie limites por categoria e acompanhe a execução de cada período.", "Budget", "Index", "Gerir Orçamentos",
                ("Planeamento", ["Crie um orçamento mensal e distribua os valores pelas categorias de despesa.", "O disponível corresponde ao orçamentado menos o realizado.", "A percentagem de execução ajuda a identificar categorias próximas ou acima do limite."]),
                ("Associação aos movimentos", ["O realizado usa o orçamento explicitamente associado ao movimento, não apenas a sua data.", "Ao criar um movimento, é sugerido por defeito o orçamento mais recente.", "Esta associação permite iniciar um novo ciclo orçamental no dia em que recebe o rendimento."])),
            ["certificates"] = Page("certificates", "Certificados de Aforro", "Gira subscrições, capitalizações e projeções da carteira.", "SavingsCertificates", "Index", "Gerir Certificados",
                ("Dados e cálculos", ["Registe data, série/número, descrição, investimento, taxa, valor atual e próxima capitalização.", "Rendimento é a diferença entre valor atual e investimento.", "O juro líquido futuro considera retenção de 28% e capitalização trimestral; o valor futuro soma esse juro ao valor atual."]),
                ("Acompanhamento", ["Atualize o valor atual e a próxima capitalização quando receber nova informação.", "Certificados cuja próxima capitalização já foi atingida aparecem no Dashboard como vencidos.", "Os totais da carteira alimentam o Dashboard, a Análise Financeira e o contexto do assistente."])),
            ["analytics"] = Page("analytics", "Análise Financeira", "Compare períodos, tendências, categorias e património.", "Analytics", "Index", "Abrir Análise",
                ("Filtros e comparações", ["Defina intervalo, grupo, categoria ou conta para limitar a análise.", "Os indicadores comparam rendimentos, despesas, poupança e taxa de poupança com um período anterior de igual duração.", "Consulte o peso e a projeção dos Certificados de Aforro no património."]),
                ("Relatório inteligente", ["Gerar relatório inteligente envia ao modelo um resumo das tabelas financeiras relevantes.", "O resultado abre numa página própria e pode ser exportado em Markdown.", "Revise conclusões e recomendações: o relatório é uma interpretação automática dos dados disponíveis."])),
            ["tables"] = Page("tables", "Tabelas", "Prepare a estrutura contabilística usada por movimentos, orçamentos e análises.", "Accounts", "Index", "Gerir Contas",
                ("Ordem recomendada", ["Crie primeiro os grupos financeiros, que separam rendimento, despesa e património.", "Crie categorias dentro dos grupos para classificar operações e linhas orçamentais.", "Crie depois as contas, indicando tipo, moeda, saldo inicial e categoria patrimonial quando aplicável."]),
                ("Manutenção", ["Prefira desativar elementos já utilizados em vez de alterar o seu significado.", "Nomes claros melhoram filtros, gráficos e sugestões da IA.", "Alterações estruturais afetam a forma como os dados são agregados nas análises."])),
            ["preferences"] = Page("preferences", "Preferências e segurança", "Gira o seu perfil e credenciais de acesso.", "Account", "Profile", "Abrir Preferências",
                ("Perfil pessoal", ["Pode alterar o nome apresentado na barra superior.", "As preferências pertencem apenas ao utilizador autenticado e estão separadas das definições globais.", "Termine a sessão quando utilizar um equipamento partilhado."]),
                ("Palavra-passe e administração", ["Para redefinir a palavra-passe deve confirmar a palavra-passe atual.", "Apenas administradores podem gerir utilizadores, permissões e definições da aplicação.", "Os administradores podem carregar dados de demonstração ou reiniciar os dados financeiros nas Definições."]))
        };

    private static HelpDetailViewModel Page(string id, string title, string subtitle, string controller, string action,
        string actionLabel, params (string Title, string[] Items)[] sections) =>
        new(id, title, subtitle, controller, action, actionLabel,
            sections.Select(section => new HelpSectionViewModel(section.Title, section.Items)).ToList());
}
