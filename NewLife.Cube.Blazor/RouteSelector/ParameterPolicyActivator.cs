using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace BigCookieKit.AspCore.RouteSelector
{
    internal static class ParameterPolicyActivator
    {
        public static T ResolveParameterPolicy<T>(
            IDictionary<string, Type> inlineParameterPolicyMap,
            IServiceProvider serviceProvider,
            string inlineParameterPolicy,
            out string parameterPolicyKey)
            where T : IParameterPolicy
        {

            if (inlineParameterPolicyMap == null)
            {
                throw new ArgumentNullException(nameof(inlineParameterPolicyMap));
            }

            if (inlineParameterPolicy == null)
            {
                throw new ArgumentNullException(nameof(inlineParameterPolicy));
            }

            string argumentString;
            var indexOfFirstOpenParens = inlineParameterPolicy.IndexOf('(');
            if (indexOfFirstOpenParens >= 0 && inlineParameterPolicy.EndsWith(")", StringComparison.Ordinal))
            {
                parameterPolicyKey = inlineParameterPolicy.Substring(0, indexOfFirstOpenParens);
                argumentString = inlineParameterPolicy.Substring(
                    indexOfFirstOpenParens + 1,
                    inlineParameterPolicy.Length - indexOfFirstOpenParens - 2);
            }
            else
            {
                parameterPolicyKey = inlineParameterPolicy;
                argumentString = null;
            }

            if (!inlineParameterPolicyMap.TryGetValue(parameterPolicyKey, out var parameterPolicyType))
            {
                return default;
            }

            if (!typeof(T).IsAssignableFrom(parameterPolicyType))
            {
                if (!typeof(IParameterPolicy).IsAssignableFrom(parameterPolicyType))
                {
                    throw new RouteCreationException("");
                }

                return default;
            }

            try
            {
                return (T)CreateParameterPolicy(serviceProvider, parameterPolicyType, argumentString);
            }
            catch (RouteCreationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new RouteCreationException(
                    $"An error occurred while trying to create an instance of '{parameterPolicyType.FullName}'.",
                    exception);
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2006:UnrecognizedReflectionPattern", Justification = "This type comes from the ConstraintMap.")]
        private static IParameterPolicy CreateParameterPolicy(IServiceProvider serviceProvider, Type parameterPolicyType, string argumentString)
        {
            ConstructorInfo activationConstructor = null;
            object[] parameters = null;
            var constructors = parameterPolicyType.GetConstructors();

            if (constructors.Length == 1 && GetNonConvertableParameterTypeCount(serviceProvider, constructors[0].GetParameters()) == 1)
            {
                activationConstructor = constructors[0];
                parameters = ConvertArguments(serviceProvider, activationConstructor.GetParameters(), new string[] { argumentString });
            }
            else
            {
                var arguments = argumentString?.Split(',', StringSplitOptions.TrimEntries) ?? Array.Empty<string>();

                var matchingConstructors = constructors
                    .Where(ci => GetNonConvertableParameterTypeCount(serviceProvider, ci.GetParameters()) == arguments.Length)
                    .OrderByDescending(ci => ci.GetParameters().Length)
                    .ToArray();

                if (matchingConstructors.Length == 0)
                {
                    throw new RouteCreationException("");
                }
                else
                {
                    if (matchingConstructors.Length == 1
                        || matchingConstructors[0].GetParameters().Length > matchingConstructors[1].GetParameters().Length)
                    {
                        activationConstructor = matchingConstructors[0];
                    }
                    else
                    {
                        throw new RouteCreationException("");
                    }

                    parameters = ConvertArguments(serviceProvider, activationConstructor.GetParameters(), arguments);
                }
            }

            return (IParameterPolicy)activationConstructor.Invoke(parameters);
        }

        private static int GetNonConvertableParameterTypeCount(IServiceProvider serviceProvider, ParameterInfo[] parameters)
        {
            if (serviceProvider == null)
            {
                return parameters.Length;
            }

            var count = 0;
            for (var i = 0; i < parameters.Length; i++)
            {
                if (typeof(IConvertible).IsAssignableFrom(parameters[i].ParameterType))
                {
                    count++;
                }
            }

            return count;
        }

        private static object[] ConvertArguments(IServiceProvider serviceProvider, ParameterInfo[] parameterInfos, string[] arguments)
        {
            var parameters = new object[parameterInfos.Length];
            var argumentPosition = 0;
            for (var i = 0; i < parameterInfos.Length; i++)
            {
                var parameter = parameterInfos[i];
                var parameterType = parameter.ParameterType;

                if (serviceProvider != null && !typeof(IConvertible).IsAssignableFrom(parameterType))
                {
                    parameters[i] = serviceProvider.GetRequiredService(parameterType);
                }
                else
                {
                    parameters[i] = Convert.ChangeType(arguments[argumentPosition], parameterType, CultureInfo.InvariantCulture);
                    argumentPosition++;
                }
            }

            return parameters;
        }
    }
}
