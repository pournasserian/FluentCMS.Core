using FluentCMS.Core.Api.Controllers;
using FluentCMS.Core.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FluentCMS.Core.Plugins.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PluginsController(IPluginManager pluginManager) : BaseController
{
    [HttpGet]
    public ApiResult<IEnumerable<PluginInfoDto>> GetAll()
    {
        var plugins = pluginManager.GetPlugins().Select(p => new PluginInfoDto
        {
            Name = p.Name,
            Version = p.Version,
            Description = p.Description,
            IsEnabled = p.IsEnabled
        });

        return Ok(plugins);
    }

    [HttpGet("{name}")]
    public ApiResult<PluginInfoDto> GetByName(string name)
    {
        var plugin = pluginManager.GetPlugin(name);
        
        if (plugin == null)
        {
            var result = new ApiResult<PluginInfoDto> { Status = 404, IsSuccess = false };
            result.Errors.Add(new ApiError { Description = $"Plugin '{name}' not found" });
            return result;
        }

        var pluginInfo = new PluginInfoDto
        {
            Name = plugin.Name,
            Version = plugin.Version,
            Description = plugin.Description,
            IsEnabled = plugin.IsEnabled
        };

        return Ok(pluginInfo);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ApiResult<PluginInfoDto>> UploadPlugin(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            var result = new ApiResult<PluginInfoDto> { Status = 400, IsSuccess = false };
            result.Errors.Add(new ApiError { Description = "No file was uploaded" });
            return result;
        }

        if (!file.FileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            var result = new ApiResult<PluginInfoDto> { Status = 400, IsSuccess = false };
            result.Errors.Add(new ApiError { Description = "Only .dll files are allowed" });
            return result;
        }

        using var stream = file.OpenReadStream();
        var metadata = await pluginManager.InstallPluginAsync(stream, file.FileName);

        if (metadata == null)
        {
            var result = new ApiResult<PluginInfoDto> { Status = 400, IsSuccess = false };
            result.Errors.Add(new ApiError { Description = "Failed to install the plugin" });
            return result;
        }

        var pluginInfo = new PluginInfoDto
        {
            Name = metadata.Name,
            Version = metadata.Version,
            Description = metadata.Description,
            IsEnabled = metadata.IsEnabled
        };

        return Ok(pluginInfo);
    }

    [HttpPut("{name}/enable")]
    public async Task<ApiResult<PluginInfoDto>> EnablePlugin(string name)
    {
        var success = await pluginManager.EnablePluginAsync(name);
        
        if (!success)
        {
            var result = new ApiResult<PluginInfoDto> { Status = 400, IsSuccess = false };
            result.Errors.Add(new ApiError { Description = $"Failed to enable plugin '{name}'" });
            return result;
        }

        var plugin = pluginManager.GetPlugin(name);
        
        if (plugin == null)
        {
            var result = new ApiResult<PluginInfoDto> { Status = 404, IsSuccess = false };
            result.Errors.Add(new ApiError { Description = $"Plugin '{name}' not found" });
            return result;
        }

        var pluginInfo = new PluginInfoDto
        {
            Name = plugin.Name,
            Version = plugin.Version,
            Description = plugin.Description,
            IsEnabled = plugin.IsEnabled
        };

        return Ok(pluginInfo);
    }

    [HttpPut("{name}/disable")]
    public async Task<ApiResult<PluginInfoDto>> DisablePlugin(string name)
    {
        var success = await pluginManager.DisablePluginAsync(name);
        
        if (!success)
        {
            var result = new ApiResult<PluginInfoDto> { Status = 400, IsSuccess = false };
            result.Errors.Add(new ApiError { Description = $"Failed to disable plugin '{name}'" });
            return result;
        }

        var plugin = pluginManager.GetPlugin(name);
        
        if (plugin == null)
        {
            var result = new ApiResult<PluginInfoDto> { Status = 404, IsSuccess = false };
            result.Errors.Add(new ApiError { Description = $"Plugin '{name}' not found" });
            return result;
        }

        var pluginInfo = new PluginInfoDto
        {
            Name = plugin.Name,
            Version = plugin.Version,
            Description = plugin.Description,
            IsEnabled = plugin.IsEnabled
        };

        return Ok(pluginInfo);
    }

    [HttpDelete("{name}")]
    public async Task<ApiResult> UninstallPlugin(string name)
    {
        var success = await pluginManager.UninstallPluginAsync(name);
        
        if (!success)
        {
            var result = new ApiResult { Status = 400, IsSuccess = false };
            result.Errors.Add(new ApiError { Description = $"Failed to uninstall plugin '{name}'" });
            return result;
        }

        return Ok();
    }
}

public class PluginInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}
