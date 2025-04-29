namespace FluentCMS.Core.Plugins.History;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<EntityHistory, EntityHistoryResponse>();
    }
}