using Microsoft.Xrm.Sdk;
using System.Reflection;

namespace Fake4Dataverse.Service.Services;

/// <summary>
/// Provides dynamic discovery of known types for WCF serialization.
/// This eliminates the need to manually add ServiceKnownType attributes for every
/// OrganizationRequest and OrganizationResponse derived type.
/// 
/// Reference: https://learn.microsoft.com/en-us/dotnet/framework/wcf/feature-details/data-contract-known-types
/// WCF supports dynamic known type discovery through a static method that returns IEnumerable&lt;Type&gt;.
/// </summary>
public static class KnownTypesProvider
{
    private static IEnumerable<Type>? _knownTypes;
    private static readonly object _lock = new object();

    /// <summary>
    /// Discovers all OrganizationRequest and OrganizationResponse derived types.
    /// This method is called by WCF's ServiceKnownType attribute to get known types dynamically.
    /// 
    /// The method scans:
    /// - Microsoft.Xrm.Sdk assembly (core SDK types)
    /// - Microsoft.Crm.Sdk.Messages assembly (CRM-specific message types)
    /// - Microsoft.PowerPlatform.Dataverse.Client assembly (if available)
    /// 
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.organizationrequest
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.organizationresponse
    /// </summary>
    public static IEnumerable<Type> GetKnownTypes(ICustomAttributeProvider provider)
    {
        if (_knownTypes != null)
        {
            return _knownTypes;
        }

        lock (_lock)
        {
            if (_knownTypes != null)
            {
                return _knownTypes;
            }

            var knownTypes = new List<Type>();

            try
            {
                // Get the base types
                var requestBaseType = typeof(OrganizationRequest);
                var responseBaseType = typeof(OrganizationResponse);

                // Get assemblies to scan
                var assembliesToScan = new HashSet<Assembly>
                {
                    requestBaseType.Assembly,  // Microsoft.Xrm.Sdk
                    responseBaseType.Assembly  // Microsoft.Xrm.Sdk
                };

                // Try to add Microsoft.Crm.Sdk.Messages assembly
                try
                {
                    var crmSdkAssembly = Assembly.Load("Microsoft.Crm.Sdk.Messages");
                    assembliesToScan.Add(crmSdkAssembly);
                }
                catch
                {
                    // Assembly not available, continue without it
                }

                // Try to add Microsoft.PowerPlatform.Dataverse.Client assembly
                try
                {
                    var dataverseClientAssembly = Assembly.Load("Microsoft.PowerPlatform.Dataverse.Client");
                    assembliesToScan.Add(dataverseClientAssembly);
                }
                catch
                {
                    // Assembly not available, continue without it
                }

                // Scan for derived types
                foreach (var assembly in assembliesToScan)
                {
                    try
                    {
                        var types = assembly.GetTypes();
                        
                        foreach (var type in types)
                        {
                            // Check if it's a public, non-abstract class
                            if (!type.IsPublic || type.IsAbstract || !type.IsClass)
                            {
                                continue;
                            }

                            // Check if it derives from OrganizationRequest or OrganizationResponse
                            if (requestBaseType.IsAssignableFrom(type) || responseBaseType.IsAssignableFrom(type))
                            {
                                knownTypes.Add(type);
                            }
                        }
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        // Some types couldn't be loaded, use the ones that could
                        foreach (var type in ex.Types)
                        {
                            if (type != null && type.IsPublic && !type.IsAbstract && type.IsClass)
                            {
                                if (requestBaseType.IsAssignableFrom(type) || responseBaseType.IsAssignableFrom(type))
                                {
                                    knownTypes.Add(type);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Continue with other assemblies
                    }
                }

                _knownTypes = knownTypes.Distinct().ToList();
                
                Console.WriteLine($"[KnownTypesProvider] Discovered {_knownTypes.Count()} known types for WCF serialization");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KnownTypesProvider] Error discovering known types: {ex.Message}");
                _knownTypes = new List<Type>();
            }

            return _knownTypes;
        }
    }
}
