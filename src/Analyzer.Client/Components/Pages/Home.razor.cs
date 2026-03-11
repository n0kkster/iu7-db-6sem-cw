namespace Analyzer.Client.Components.Pages;

using Microsoft.AspNetCore.Components;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using Blazor.Diagrams.Core.PathGenerators;
using Blazor.Diagrams.Core.Routers;
using Blazor.Diagrams.Options;
using MudBlazor;
using Serilog;

using Analyzer.Shared.DTO;
using Analyzer.Domain.Enums;
using Analyzer.Client.Models;
using Analyzer.Client.Components.Dialogs;
using Analyzer.Client.Components.Widgets;
using Blazor.Diagrams.Core.Geometry;

using BlazorLinkModel = Blazor.Diagrams.Core.Models.LinkModel;
using LinkModel = Models.LinkModel;
using Microsoft.AspNetCore.Components.Forms;

public partial class Home : ComponentBase, IDisposable
{
    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private HttpClient Http { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private NavigationManager NavManager { get; set; } = default!;

    // Основное состояние страницы
    // ===========================
    private BlazorDiagram? Diagram { get; set; } = null;
    private bool _isLoadingGraph = true;
    // ===========================

    // Состояния панелей 
    // ===========================
    private bool _isComponentPropertiesPanelOpen = false;
    private Guid? _selectedComponentId;

    private bool _isLinkPropertiesPanelOpen = false;
    private LinkModel? _selectedLinkModel;
    private string? _selectedLinkSourceName;
    private string? _selectedLinkTargetName;
    // ===========================

    // Состояние симуляции
    // ===========================
    private bool _isSimulationMode = false;
    // ===========================

    // Состояние импорта и экспорта
    // ===========================
    private bool _isImportingSystem = false;
    private bool _isExportingSystem = false;
    // ===========================

    // =================================================
    // ИНИЦИАЛИЗАЦИЯ И ЖИЗНЕННЫЙ ЦИКЛ
    // =================================================
    protected override void OnInitialized()
    {
        var options = new BlazorDiagramOptions
        {
            AllowMultiSelection = false,
            Zoom = { Enabled = true, Inverse = false },
            Links = { DefaultRouter = new NormalRouter(), DefaultPathGenerator = new SmoothPathGenerator() }
        };

        Diagram = new BlazorDiagram(options);
        Diagram.RegisterComponent<ComponentModel, ComponentWidget>();

        Diagram.PointerDoubleClick += OnComponentSelected;
        Diagram.PointerClick += OnCanvasOrLinkClicked;

        Diagram.Links.Added += OnLinkAdded;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await RefreshGraphAsync();
            StateHasChanged();
        }
    }

    // =================================================
    // СОБЫТИЯ КЛИКОВ ПО ГРАФУ
    // =================================================
    private async void OnComponentSelected(Model? model, Blazor.Diagrams.Core.Events.PointerEventArgs e)
    {
        if (model is ComponentModel componentModel)
        {
            _selectedComponentId = componentModel.ComponentId;
            _isComponentPropertiesPanelOpen = true;
            _isLinkPropertiesPanelOpen = false;
            StateHasChanged();
        }
    }

    private void OnCanvasOrLinkClicked(Model? model, Blazor.Diagrams.Core.Events.PointerEventArgs e)
    {
        if (model is LinkModel linkModel)
        {
            if (linkModel.Source.Model is PortModel sourcePort &&
                linkModel.Target.Model is PortModel targetPort)
            {
                _selectedLinkModel = linkModel;

                _selectedLinkSourceName = sourcePort.GetParent<ComponentModel>().Name;
                _selectedLinkTargetName = targetPort.GetParent<ComponentModel>().Name;

                _isLinkPropertiesPanelOpen = true;
                _isComponentPropertiesPanelOpen = false;
                StateHasChanged();
            }
            else
            {
                Console.WriteLine("Каким-то образом выбрана некорректная связь..");
                return;
            }
        }
        // TODO: фикс обработки клика на холст при закрытии панели
        else if (model is null)
        {
            _isComponentPropertiesPanelOpen = false;
            _isLinkPropertiesPanelOpen = false;
            Diagram?.UnselectAll();
            StateHasChanged();
        }
    }

    private void OnLinkAdded(BaseLinkModel baseLink)
    {
        baseLink.TargetAttached += OnLinkTargetAttached;
    }

    private async void OnLinkTargetAttached(BaseLinkModel baseLink)
    {
        if (baseLink is BlazorLinkModel linkModel)
        {
            baseLink.TargetAttached -= OnLinkTargetAttached;

            if (linkModel.Source.Model is not PortModel sourcePort
                || linkModel.Target.Model is not PortModel targetPort)
            {
                Snackbar.Add("Ошибка создания связи! Источник или приемник не является портом!", Severity.Error);
                Diagram?.Links.Remove(linkModel);
                return;
            }

            var sourceId = sourcePort.GetParent<ComponentModel>().ComponentId;
            var targetId = targetPort.GetParent<ComponentModel>().ComponentId;

            var options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
            };

            var dialog = await DialogService.ShowAsync<LinkConfigDialog>("Настройка связи", options);
            var result = await dialog.Result;

            if ((!result?.Canceled ?? false) && result!.Data is LinkConfigDialog.LinkConfigResult config)
            {
                var linkDto = new CreateLinkDto(sourceId, targetId, config.Severity, config.Protocol);
                var response = await Http.PostAsJsonAsync("api/v1/links/", linkDto);
                var createdLinkGuid = await response.Content.ReadFromJsonAsync<Guid>();

                Diagram?.Links.Remove(linkModel);

                var smartLink = new LinkModel(sourcePort, targetPort)
                {
                    LinkId = createdLinkGuid,
                    Severity = config.Severity,
                    Protocol = config.Protocol,
                    PathGenerator = new SmoothPathGenerator(),
                    Router = new NormalRouter()
                };

                ApplyLinkStyles(smartLink, config.Severity, config.Protocol);
                Diagram?.Links.Add(smartLink);
            }
            else
            {
                Diagram?.Links.Remove(linkModel);
            }
        }
    }

    private async Task OpenAddComponentDialog()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
        };

        var dialog = await DialogService.ShowAsync<AddComponentDialog>("Новый компонент", options);

        var result = await dialog.Result;

        if (!result?.Canceled ?? false)
        {
            await RefreshGraphAsync();
        }
    }

    public async Task DeleteLink(LinkModel linkModel)
    {
        if (linkModel is null)
            return;
        Diagram?.Links.Remove(linkModel);
        _isLinkPropertiesPanelOpen = false;
        StateHasChanged();
    }

    // =================================================
    // ЗАГРУЗКА И ОТРИСОВКА ГРАФА
    // =================================================
    private async Task RefreshGraphAsync()
    {
        _isLoadingGraph = true;
        Diagram?.Nodes.Clear();
        Diagram?.Links.Clear();

        try
        {
            var components = await Http.GetFromJsonAsync<List<ComponentDto>>("api/v1/components/") ?? [];
            var links = await Http.GetFromJsonAsync<List<LinkDto>>("api/v1/links/") ?? [];

            var compDict = RenderGraph(components);
            RenderLinks(links, compDict);
        }
        catch (Exception e)
        {
            Log.Error($"Ошибка во время выполнения запроса: {e.Message}");
        }
        finally
        {
            _isLoadingGraph = false;
        }
    }

    public Dictionary<Guid, ComponentModel> RenderGraph(List<ComponentDto> components)
    {
        int x = 100, y = 100;
        var compDict = new Dictionary<Guid, ComponentModel>();
        foreach (var comp in components)
        {
            var node = new ComponentModel(comp.Id, comp.Name, comp.Type, new Point(x, y));
            Diagram?.Nodes.Add(node);
            compDict.Add(comp.Id, node);

            x += 250;
            if (x > 800) { x = 100; y += 200; }
        }

        return compDict;
    }

    private void RenderLinks(List<LinkDto> links, Dictionary<Guid, ComponentModel> compDict)
    {
        foreach (var link in links)
        {
            if (compDict.TryGetValue(link.SourceId, out var sourceNode) &&
                compDict.TryGetValue(link.TargetId, out var targetNode))
            {
                var sourcePort = sourceNode.GetPort(PortAlignment.Bottom)!;
                var targetPort = targetNode.GetPort(PortAlignment.Top)!;

                var linkModel = new LinkModel(sourcePort, targetPort)
                {
                    LinkId = link.Id,
                    Severity = link.Severity,
                    Protocol = link.Protocol,
                    PathGenerator = new SmoothPathGenerator(),
                    Router = new NormalRouter(),
                };

                ApplyLinkStyles(linkModel, link.Severity, link.Protocol);
                Diagram?.Links.Add(linkModel);
            }
        }
    }
    // ===================================

    // Симуляция
    private async Task RunSimulationAsync(Guid initialFailedComponentId)
    {
        _isLoadingGraph = true;
        StateHasChanged();

        try
        {
            var result = await Http.GetFromJsonAsync<List<Guid>>(
                $"api/v1/analysis/simulate/{initialFailedComponentId}");

            if (result is null || !result.Any())
            {
                Snackbar.Add("Сбой не привел к каскадному отказу других компонентов.", Severity.Success);
                return;
            }

            _isSimulationMode = true;

            var allFailedIds = result.ToHashSet();
            allFailedIds.Add(initialFailedComponentId);

            foreach (var node in Diagram!.Nodes)
            {
                if (node is ComponentModel componentModel)
                {
                    if (allFailedIds.Contains(componentModel.ComponentId))
                    {
                        componentModel.IsFailed = true;
                        componentModel.IsDimmed = false;
                    }
                    else
                    {
                        componentModel.IsFailed = false;
                        componentModel.IsDimmed = true;
                    }
                    componentModel.Refresh();
                }
            }

            foreach (var link in Diagram.Links)
            {
                if (link is LinkModel linkModel)
                {
                    if (linkModel.Source.Model is PortModel sourcePort &&
                        linkModel.Target.Model is PortModel targetPort)
                    {
                        var sourceId = sourcePort.GetParent<ComponentModel>().ComponentId;
                        var targetId = targetPort.GetParent<ComponentModel>().ComponentId;

                        if (allFailedIds.Contains(sourceId) && allFailedIds.Contains(targetId))
                        {
                            linkModel.Color = "#ff3f5f";
                            linkModel.Width = 4;
                            linkModel.IsDimmed = false;
                        }
                        else
                        {
                            linkModel.Color = "#e2e8f0";
                            linkModel.IsDimmed = true;
                        }
                        linkModel.Refresh();
                    }
                }
            }

            Snackbar.Add($"Симуляция завершена. Затронуто узлов: {allFailedIds.Count}", Severity.Warning);
        }
        catch (Exception e)
        {
            Snackbar.Add($"Ошибка при выполнении симуляции: {e.Message}", Severity.Error);
        }
        finally
        {
            _isLoadingGraph = false;
            StateHasChanged();
        }
    }

    private void ResetSimulation()
    {
        _isSimulationMode = false;

        foreach (var node in Diagram!.Nodes)
        {
            if (node is ComponentModel componentModel)
            {
                componentModel.IsFailed = false;
                componentModel.IsDimmed = false;
                componentModel.Refresh();
            }
        }

        foreach (var link in Diagram.Links)
        {
            if (link is LinkModel linkModel)
            {
                linkModel.IsDimmed = false;
                ApplyLinkStyles(linkModel, linkModel.Severity, linkModel.Protocol);
                linkModel.Refresh();
            }
        }

        StateHasChanged();
    }
    // ===================================

    private async Task ImportSystemAsync(InputFileChangeEventArgs e)
    {
        try
        {
            var file = e.File;
            if (file is null)
                return;

            _isImportingSystem = true;

            // Ограничиваем размер загружаемого файла (например, 10 МБ)
            long maxFileSize = 10 * 1024 * 1024;

            using var stream = file.OpenReadStream(maxFileSize);
            using var content = new MultipartFormDataContent();

            // Упаковываем файл в HTTP-запрос
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            content.Add(fileContent, "file", file.Name);

            // Отправляем на бэкенд
            var response = await Http.PostAsync("api/v1/systems/import", content);
            response.EnsureSuccessStatusCode();

            Snackbar.Add("Система успешно импортирована", Severity.Success);

            // Принудительно перерисовываем граф новыми данными
            await RefreshGraphAsync();
        }
        catch (Exception ex)
        {
            Log.Error($"Ошибка импорта: {ex.Message}");
            Snackbar.Add("Не удалось импортировать систему", Severity.Error);
        }
        finally
        {
            _isImportingSystem = false;
        }
    }

    private async Task ExportSystemAsync()
    {
        // а где хранить активную систему? на фронте наверное все таки
        _isExportingSystem = true;
        NavManager.NavigateTo("http://localhost:1555/api/v1/systems/export", forceLoad: true);
        _isExportingSystem = false;
    }

    // Вспомогательные методы
    // ===================================
    private void ApplyLinkStyles(LinkModel linkModel,
        LinkSeverity severity, ProtocolType protocol)
    {
        linkModel.Color = severity switch
        {
            LinkSeverity.High => "#ff3f5f",
            LinkSeverity.Mid => "#ffb545",
            LinkSeverity.Low => "#3dcb6c",
            _ => "#74718e"
        };
        linkModel.Width = severity == LinkSeverity.High ? 3 : 2;
        linkModel.TargetMarker = LinkMarker.Arrow;

        linkModel.Labels.Clear();
        linkModel.Labels.Add(new LinkLabelModel(linkModel, protocol.ToString()));

        Diagram?.Refresh();
    }
    // ===================================

    public void Dispose()
    {
        Diagram?.PointerDoubleClick -= OnComponentSelected;
        Diagram?.PointerClick -= OnCanvasOrLinkClicked;
        Diagram?.Links.Added -= OnLinkAdded;
    }
}