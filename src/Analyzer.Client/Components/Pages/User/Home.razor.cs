namespace Analyzer.Client.Components.Pages.User;

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
using Blazor.Diagrams.Core.Anchors;

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
    private ComponentModel? _selectedComponentModel;

    private bool _isLinkPropertiesPanelOpen = false;
    private LinkModel? _selectedLinkModel;
    private string? _selectedLinkSourceName;
    private string? _selectedLinkTargetName;
    // ===========================

    // Состояние симуляции
    // ===========================
    private bool _isSimulationMode = false;
    // ===========================

    // Управление системами
    // ===========================
    private IReadOnlyCollection<ITSystemDto>? _systems;
    private Guid? _selectedSystemId;
    // ===========================

    // Пользователь, которому принадлежит текущая сессия
    // ===========================
    private UserDto _loggedUser = default!;
    // ===========================

    // =================================================
    // ИНИЦИАЛИЗАЦИЯ И ЖИЗНЕННЫЙ ЦИКЛ
    // =================================================
    protected override async Task OnInitializedAsync()
    {
        try
        {
            var user = await Http.GetFromJsonAsync<UserDto>("api/v1/users/me");
            if (user is null)
            {
                Log.Error("Ошибка получения профиля пользователя.");
                NavManager.NavigateTo("/logout", forceLoad: true);
                return;
            }
            _loggedUser = user;

            _systems = await Http.GetFromJsonAsync<IReadOnlyCollection<ITSystemDto>>(
                $"api/v1/systems/?teamId={_loggedUser.TeamId}");
        }
        catch (Exception e)
        {
            Log.Error($"Ошибка загрузки списка систем: {e.Message}");
            Snackbar.Add("Не удалось получить список систем", Severity.Error);
        }

        var options = new BlazorDiagramOptions
        {
            AllowMultiSelection = false,
            Zoom = { Enabled = true, Inverse = true },
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
            if (_selectedSystemId is not null)
            {
                await RefreshGraphAsync();
                StateHasChanged();        
            }
        }
    }

    private async Task OnSystemSelectedAsync(Guid? systemId)
    {
        _selectedSystemId = systemId;

        _isComponentPropertiesPanelOpen = false;
        _isLinkPropertiesPanelOpen = false;

        if (_selectedSystemId is not null)
        {
            await RefreshGraphAsync();
        }
        else
        {
            Diagram?.Nodes.Clear();
            Diagram?.Links.Clear();
        }
    }

    // =================================================
    // СОБЫТИЯ КЛИКОВ ПО ГРАФУ
    // =================================================
    private async void OnComponentSelected(Model? model, Blazor.Diagrams.Core.Events.PointerEventArgs e)
    {
        if (model is ComponentModel componentModel && 
            _selectedComponentModel == componentModel)
        {
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
        else if (model is ComponentModel componentModel)
        {
            // Убираем выделение с прошлой модели
            _selectedComponentModel?.IsSelected = false;
            _selectedComponentModel?.Refresh();

            _selectedComponentModel = componentModel;
            componentModel.IsSelected = true;
            componentModel.Refresh();
        }
        else if (model is null)
        {
            _selectedComponentModel?.IsSelected = false;
            _selectedComponentModel?.Refresh();
            _selectedComponentModel = null;

            _isComponentPropertiesPanelOpen = false;
            _isLinkPropertiesPanelOpen = false;
            
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

                UpdateDynamicPorts(smartLink);

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
        if (_selectedSystemId is null)
            return;

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
        };
    
        var parameters = new DialogParameters
        {
            ["SelectedSystemId"] = _selectedSystemId,
        };

        var dialog = await DialogService.ShowAsync<AddComponentDialog>("Новый компонент", parameters, options);

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
        if (_selectedSystemId is null)
            return;

        _isLoadingGraph = true;
        Diagram?.Nodes.Clear();
        Diagram?.Links.Clear();

        try
        {
            var components = await Http.GetFromJsonAsync<List<ComponentDto>>($"api/v1/components/?systemId={_selectedSystemId}") ?? [];
            var links = await Http.GetFromJsonAsync<List<LinkDto>>($"api/v1/links/?systemId={_selectedSystemId}") ?? [];

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

    private async Task AutoArrangeNodesAsync()
    {
        if (Diagram is null || !Diagram.Nodes.Any()) 
            return;

        var nodes = Diagram.Nodes.OfType<ComponentModel>().ToList();
        var links = Diagram.Links.OfType<LinkModel>().ToList();

        var inDegree = nodes.ToDictionary(n => n.ComponentId, n => 0);
        var outNodes = nodes.ToDictionary(n => n.ComponentId, n => new List<Guid>());

        foreach (var link in links)
        {
            if (link.Source.Model is PortModel sourcePort &&
                link.Target.Model is PortModel targetPort)
            {
                var sourceId = sourcePort.GetParent<ComponentModel>().ComponentId;
                var targetId = targetPort.GetParent<ComponentModel>().ComponentId;

                if (outNodes.ContainsKey(sourceId)) outNodes[sourceId].Add(targetId);
                if (inDegree.ContainsKey(targetId)) inDegree[targetId]++;
            }

        }

        var columns = new Dictionary<int, List<ComponentModel>>();
        var queue = new Queue<(Guid NodeId, int Level)>();

        foreach (var kvp in inDegree.Where(k => k.Value == 0))
            queue.Enqueue((kvp.Key, 0));

        if (!queue.Any()) 
            queue.Enqueue((nodes.First().ComponentId, 0));

        var visited = new HashSet<Guid>();

        while (queue.Any())
        {
            var (currentId, level) = queue.Dequeue();
            if (visited.Contains(currentId)) 
                continue;

            visited.Add(currentId);

            if (!columns.ContainsKey(level))
                columns[level] = [];

            columns[level].Add(nodes.First(n => n.ComponentId == currentId));

            foreach (var target in outNodes[currentId])
                queue.Enqueue((target, level + 1));
        }

        var unvisited = nodes.Where(n => !visited.Contains(n.ComponentId)).ToList();
        if (unvisited.Any())
        {
            if (!columns.ContainsKey(0))
                columns[0] = [];

            columns[0].AddRange(unvisited);
        }

        int startX = 100;
        int colWidth = 350;
        int rowHeight = 150;

        foreach (var col in columns.OrderBy(c => c.Key))
        {
            var nodesInCol = col.Value;

            int startY = 100 + ((10 - nodesInCol.Count) * rowHeight / 2);
            if (startY < 50) 
                startY = 50;

            for (int i = 0; i < nodesInCol.Count; i++)
            {
                var node = nodesInCol[i];
                node.SetPosition(startX, startY + (i * rowHeight));

                // _pendingPositions[node.ComponentId] = node.Position;
            }

            startX += colWidth;
        }

        Diagram.Refresh();

        // Костыль, костылечек, родненький
        await Task.Delay(15);

        foreach (var link in Diagram.Links)
            if (link is LinkModel linkModel)
                UpdateDynamicPorts(linkModel);

        // _savePositionTimer?.Change(500, Timeout.Infinite);
        Snackbar.Add("Авто-раскладка применена", Severity.Success);
    }
    // ===================================

    // Симуляция
    private async Task RunSimulationAsync(Guid initialFailedComponentId)
    {
        _isLoadingGraph = true;
        StateHasChanged();

        try
        {
            var result = await Http.GetFromJsonAsync<IReadOnlyCollection<Guid>>(
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

    // =================================================
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // =================================================
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

    private void UpdateDynamicPorts(LinkModel link)
    {
        if (link.Source.Model is not PortModel sourcePort ||
            link.Target.Model is not PortModel targetPort)
            return;

        var source = sourcePort.GetParent<ComponentModel>();
        var target = targetPort.GetParent<ComponentModel>();

        var sourceCenterX = source.Position.X + 100;
        var sourceCenterY = source.Position.Y + 40;
        var targetCenterX = target.Position.X + 100;
        var targetCenterY = target.Position.Y + 40;

        var dx = targetCenterX - sourceCenterX;
        var dy = targetCenterY - sourceCenterY;

        PortAlignment sourceAlign, targetAlign;

        if (Math.Abs(dx) > Math.Abs(dy))
        {
            if (dx > 0)
            {
                sourceAlign = PortAlignment.Right;
                targetAlign = PortAlignment.Left;
            }
            else
            {
                sourceAlign = PortAlignment.Left;
                targetAlign = PortAlignment.Right;
            }
        }
        else
        {
            if (dy > 0)
            {
                sourceAlign = PortAlignment.Bottom;
                targetAlign = PortAlignment.Top;
            }
            else
            {
                sourceAlign = PortAlignment.Top;
                targetAlign = PortAlignment.Bottom;
            }
        }

        var newSourcePort = source.GetPort(sourceAlign);
        var newTargetPort = target.GetPort(targetAlign);

        if (newSourcePort is not null && newTargetPort is not null)
        {
            link.SetSource(new SinglePortAnchor(newSourcePort));
            link.SetTarget(new SinglePortAnchor(newTargetPort));

            link.Refresh();
        }
    }
    // ===================================

    public void Dispose()
    {
        Diagram?.PointerDoubleClick -= OnComponentSelected;
        Diagram?.PointerClick -= OnCanvasOrLinkClicked;
        Diagram?.Links.Added -= OnLinkAdded;
    }
}