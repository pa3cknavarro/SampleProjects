using Ecommerce.Api.Client.Models;
using Ecommerce.Api.Client.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebClient.Pages;

public class ProvincesModel : PageModel
{
    private readonly EcommerceApiClient _client;

    public ProvincesModel(EcommerceApiClient client) => _client = client;

    public IEnumerable<ProvinceResponse>? Items { get; set; }
    public string? Error { get; set; }

    public async Task OnGetAsync()
    {
        var result = await _client.GetProvincesAsync(new ListQuery { Page = 1, PageSize = 50 });
        if (result.IsSuccess && result.Data is not null)
            Items = result.Data.Items;
        else
            Error = result.Error?.Detail
                ?? result.Error?.Title
                ?? $"Request failed ({result.StatusCode})";
    }
}
