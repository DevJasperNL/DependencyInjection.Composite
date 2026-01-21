#pragma warning disable IDE0130
namespace Microsoft.Extensions.DependencyInjection
#pragma warning restore IDE0130
{
    public static class ServiceProviderExtensions
    {
        public static IServiceScope CreateScope(this IServiceProvider serviceProvider,
            Action<IServiceCollection> contextBuilder)
            => CreateScope(serviceProvider, contextBuilder, new ServiceProviderOptions());

        public static IServiceScope CreateScope(this IServiceProvider serviceProvider,
            Action<IServiceCollection> contextBuilder, bool validateScopes)
            => CreateScope(serviceProvider, contextBuilder,
                new ServiceProviderOptions { ValidateScopes = validateScopes });

        public static IServiceScope CreateScope(this IServiceProvider serviceProvider,
            Action<IServiceCollection> contextBuilder, ServiceProviderOptions options)
        {
            var parentScope = serviceProvider.CreateScope();

            try
            {
                var contextServices = new ServiceCollection();
                contextBuilder(contextServices);

                var contextServiceProvider = contextServices.BuildServiceProvider(options);
                var compositeServiceProvider = new CompositeServiceProvider(
                    contextServiceProvider, // We want to prioritize context services. This also follows the pattern of a service provider providing the last registered service.
                    parentScope.ServiceProvider);

                return new LinkedContextScope(compositeServiceProvider, contextServiceProvider, parentScope);
            }
            catch
            {
                parentScope.Dispose();
                throw;
            }
        }
    }
}
