namespace FluentCMS.Core.Plugins.History.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class EntityHistoryController(IEntityHistoryService service, IMapper mapper) : ControllerBase
{
    [HttpGet("{entityId}")]
    public async Task<IEnumerable<EntityHistoryResponse>> GetHistory(Guid entityId, CancellationToken cancellationToken = default)
    {
        var queryResult = await service.GetByEntityId(entityId, cancellationToken);
        var response = mapper.Map<IEnumerable<EntityHistoryResponse>>(queryResult.Items);
        return response;
    }
}
