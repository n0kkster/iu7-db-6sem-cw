namespace Analyzer.Client.Infrastructure;

using MudBlazor;


public class ErrorHandler(ISnackbar snackbar) : DelegatingHandler
{
    private readonly ISnackbar _snackbar = snackbar;
    private record HttpErrorResponse(string Type, string Title, int Status, string Detail, string TraceId);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadFromJsonAsync<HttpErrorResponse>(cancellationToken);
                
                var errorMessage = errorContent is null 
                    ? $"Ошибка сервера: {(int)response.StatusCode}" 
                    : errorContent.Detail;

                _snackbar.Add(errorMessage, Severity.Error, config =>
                {
                    config.ShowCloseIcon = true;
                    config.RequireInteraction = true;
                });
            }

            return response;
        }
        catch (HttpRequestException)
        {
            _snackbar.Add("Нет связи с сервером. Проверьте подключение.", Severity.Error);
            throw;
        }
    }
}