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

public partial class Home : ComponentBase, IDisposable
{
    [Inject] 
    private IDialogService DialogService { get; set; } = default!;
    
    [Inject] 
    private HttpClient Http { get; set; } = default!;
    
    [Inject] 
    private ISnackbar Snackbar { get; set; } = default!;

    // Основное состояние страницы
    // ===========================
    private BlazorDiagram? Diagram { get; set; } = null;
    private bool _isLoadingGraph = true;
    // ===========================

    // Состояния панелей 
    // ===========================
    private bool _isComponentPropertiesPanelOpen = false;
    private Guid? _selectedComponentId;
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
            StateHasChanged();
        }
    }

    private void OnCanvasOrLinkClicked(Model? model, Blazor.Diagrams.Core.Events.PointerEventArgs e)
    {
        if (model == null)
        {
            _isComponentPropertiesPanelOpen = false;
            Diagram?.UnselectAll(); 
            StateHasChanged();
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
                    PathGenerator = new SmoothPathGenerator(),
                    Router = new NormalRouter(),
                };

                ApplyLinkStyles(linkModel, link.Severity, link.Protocol);
                Diagram?.Links.Add(linkModel);
            }
        }
    }
    // ===================================

    // Вспомогательные методы
    // ===================================
    private void ApplyLinkStyles(LinkModel linkModel, LinkSeverity severity, ProtocolType protocol)
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
    }
}