using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Api.Models;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Media;
using Jobsy.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

/// <summary>Card thumbnail contract: inline photos surface via /api/vacancies/{id}/image.</summary>
public class VacancyCardApiTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public VacancyCardApiTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Card_with_inline_photo_returns_public_image_path_thumbnail()
    {
        var client = _factory.CreateClient();
        var pins = await client.GetFromJsonAsync<List<VacancyPinDto>>("api/vacancies/pins", JsonOpts);
        Assert.NotNull(pins);
        Assert.NotEmpty(pins!);
        var id = pins[0].Id;

        var png = Convert.ToBase64String(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4 });
        var dataUri = $"data:image/png;base64,{png}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var vacancy = await db.Vacancies.FindAsync(id);
            Assert.NotNull(vacancy);
            vacancy!.ImageUrl = dataUri;
            await db.SaveChangesAsync();
            scope.ServiceProvider.GetRequiredService<IVacancyDiscoveryIndex>().Invalidate();
        }

        var cardResponse = await client.GetAsync($"api/vacancies/{id:D}/card");
        Assert.Equal(HttpStatusCode.OK, cardResponse.StatusCode);
        var card = await cardResponse.Content.ReadFromJsonAsync<VacancyCardDto>(JsonOpts);
        Assert.NotNull(card);
        Assert.Equal(VacancyImageUrls.PublicImagePath(id), card!.ThumbnailUrl);
    }
}
