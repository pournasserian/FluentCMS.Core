//using Microsoft.Extensions.DependencyInjection;

//namespace FluentCMS.Providers;

//public interface IProviderResolver<TProvider> where TProvider : class
//{
//    TProvider Resolve();
//}

//public sealed class ProviderResolver<TProvider>(IServiceProvider sp) : IProviderResolver<TProvider>
//    where TProvider : class
//{
//    public TProvider Resolve()
//    {
//        var key = $"{options.CurrentValue.Area}:{options.CurrentValue.Name}";
//        return sp.GetRequiredKeyedService<TProvider>(key);
//    }
//}
