using FastEndpoints;
using ShelfsService.src.ShelfsService.Common;
using ShelfsService.src.ShelfsService.Common.DTOs.Items;
using ShelfsService.src.ShelfsService.Common.DTOs.Shelfs;
using ShelfsService.src.ShelfsService.Common.Interfaces;
using ShelfsService.src.ShelfsService.Host.Features.Shelfs;

namespace ShelfsService.src.ShelfsService.Host.Features.Attributes;

sealed record GetAttributesValuesResponse(List<AttributeTypeDto> AttributesValues);
class GetAttributesValues : EndpointWithoutRequest<GetAttributesValuesResponse>
{
    private readonly IAttributeResolver _attributeResolver;

    public GetAttributesValues(IAttributeResolver attributeResolver)
    {
        _attributeResolver = attributeResolver;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Attributes);
        AllowAnonymous();
        //Policies("AdminPolicy");    
    }

    public override async Task<GetAttributesValuesResponse> ExecuteAsync(CancellationToken ct)
    {
        var allValues = _attributeResolver.GetAllData();
        if (allValues == null || !allValues.Any())
        {
            return new GetAttributesValuesResponse(new List<AttributeTypeDto>());
        }
        return new GetAttributesValuesResponse(allValues.ToList());
    }
}
