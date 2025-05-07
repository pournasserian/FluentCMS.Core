namespace FluentCMS.Plugins.AuditTrailManager;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<AuditTrail, AuditTrailResponse>();
    }
}