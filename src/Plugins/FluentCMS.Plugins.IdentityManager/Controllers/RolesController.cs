namespace FluentCMS.Plugins.IdentityManager.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class RolesController(IRoleService roleService, IMapper mapper)
{
    [HttpGet]
    public async Task<ApiPagedResult<RoleResponse>> GetAll(CancellationToken cancellationToken = default)
    {
        var roles = await roleService.GetAll(cancellationToken);
        var rolesResponse = new ApiPagedResult<RoleResponse>(mapper.Map<IEnumerable<RoleResponse>>(roles));
        return rolesResponse;
    }

    [HttpGet("{id:guid}")]
    public async Task<ApiResult<RoleResponse>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await roleService.GetById(id, cancellationToken);
        var roleResponse = mapper.Map<RoleResponse>(role);
        return new ApiResult<RoleResponse>(roleResponse);
    }

    [HttpPost]
    public async Task<ApiResult<RoleResponse>> Add([FromBody] RoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = mapper.Map<Role>(request);
        role.Type = RoleTypes.UserDefined;
        await roleService.Add(role, cancellationToken);
        var roleResponse = mapper.Map<RoleResponse>(role);
        return new ApiResult<RoleResponse>(roleResponse);
    }

    [HttpPut("{id:guid}")]
    public async Task<ApiResult<RoleResponse>> Update(Guid id, [FromBody] RoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await roleService.GetById(id, cancellationToken);
        if (role.Type != RoleTypes.UserDefined)
            throw new EnhancedException("Role.CanNotUpdate", "Cannot update a built-in role.");

        mapper.Map<Role>(request);
        
        await roleService.Update(role, cancellationToken);

        var roleResponse = mapper.Map<RoleResponse>(role);
        return new ApiResult<RoleResponse>(roleResponse);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ApiResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await roleService.GetById(id, cancellationToken);
        
        if (role.Type != RoleTypes.UserDefined)
            throw new EnhancedException("Role.CanNotDelete", "Cannot delete a built-in role.");

        await roleService.Remove(id, cancellationToken);
        return new ApiResult();
    }
}
