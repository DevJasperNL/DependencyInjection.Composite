#pragma warning disable IDE0130
namespace Microsoft.Extensions.DependencyInjection
#pragma warning restore IDE0130
{
    /// <summary>
    /// Extension methods for <see cref="IServiceProvider"/> to create contextual scopes with additional services.
    /// </summary>
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// Creates a new <see cref="IServiceScope"/> with additional context-specific services.
        /// </summary>
        /// <param name="serviceProvider">The parent service provider.</param>
        /// <param name="contextBuilder">An action to configure the context-specific services.</param>
        /// <returns>A new <see cref="IServiceScope"/> that combines the parent and context services.</returns>
        public static IServiceScope CreateScope(this IServiceProvider serviceProvider,
            Action<IServiceCollection> contextBuilder)
            => CreateScope(serviceProvider, contextBuilder, new ServiceProviderOptions());

        /// <summary>
        /// Creates a new <see cref="IServiceScope"/> with additional context-specific services.
        /// </summary>
        /// <param name="serviceProvider">The parent service provider.</param>
        /// <param name="contextBuilder">An action to configure the context-specific services.</param>
        /// <param name="validateScopes">A value indicating whether to validate scopes for the context services.</param>
        /// <returns>A new <see cref="IServiceScope"/> that combines the parent and context services.</returns>
        public static IServiceScope CreateScope(this IServiceProvider serviceProvider,
            Action<IServiceCollection> contextBuilder, bool validateScopes)
            => CreateScope(serviceProvider, contextBuilder,
                new ServiceProviderOptions { ValidateScopes = validateScopes });

        /// <summary>
        /// Creates a new <see cref="IServiceScope"/> with additional context-specific services.
        /// </summary>
        /// <param name="serviceProvider">The parent service provider.</param>
        /// <param name="contextBuilder">An action to configure the context-specific services.</param>
        /// <param name="options">Options for the context service provider.</param>
        /// <returns>A new <see cref="IServiceScope"/> that combines the parent and context services.</returns>
        /// <remarks>
        /// The context services take precedence over the parent services when resolving dependencies.
        /// When the returned scope is disposed, both the context and parent scopes are disposed.
        /// </remarks>
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
