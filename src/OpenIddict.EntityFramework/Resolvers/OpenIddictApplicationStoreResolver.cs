﻿using System;
using System.Collections.Concurrent;
using System.Data.Entity;
using System.Text;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.Models;

namespace OpenIddict.EntityFramework
{
    /// <summary>
    /// Exposes a method allowing to resolve an application store.
    /// </summary>
    public class OpenIddictApplicationStoreResolver<TContext> : IOpenIddictApplicationStoreResolver
        where TContext : DbContext
    {
        private static readonly ConcurrentDictionary<Type, Type> _cache = new ConcurrentDictionary<Type, Type>();
        private readonly IServiceProvider _provider;

        public OpenIddictApplicationStoreResolver([NotNull] IServiceProvider provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// Returns an application store compatible with the specified application type or throws an
        /// <see cref="InvalidOperationException"/> if no store can be built using the specified type.
        /// </summary>
        /// <typeparam name="TApplication">The type of the Application entity.</typeparam>
        /// <returns>An <see cref="IOpenIddictApplicationStore{TApplication}"/>.</returns>
        public IOpenIddictApplicationStore<TApplication> Get<TApplication>() where TApplication : class
        {
            var store = _provider.GetService<IOpenIddictApplicationStore<TApplication>>();
            if (store != null)
            {
                return store;
            }

            var type = _cache.GetOrAdd(typeof(TApplication), key =>
            {
                var root = OpenIddictCoreHelpers.FindGenericBaseType(key, typeof(OpenIddictApplication<,,>));
                if (root == null)
                {
                    throw new InvalidOperationException(new StringBuilder()
                        .AppendLine("The specified application type is not compatible with the Entity Framework 6.x stores.")
                        .Append("When enabling the Entity Framework 6.x stores, make sure you use the built-in generic ")
                        .Append("'OpenIddictApplication' entity (from the 'OpenIddict.Models' package) or a custom entity ")
                        .Append("that inherits from the generic 'OpenIddictApplication' entity.")
                        .ToString());
                }

                return typeof(OpenIddictApplicationStore<,,,,>).MakeGenericType(
                    /* TApplication: */ key,
                    /* TAuthorization: */ root.GenericTypeArguments[1],
                    /* TToken: */ root.GenericTypeArguments[2],
                    /* TContext: */ typeof(TContext),
                    /* TKey: */ root.GenericTypeArguments[0]);
            });

            return (IOpenIddictApplicationStore<TApplication>) _provider.GetRequiredService(type);
        }
    }
}
