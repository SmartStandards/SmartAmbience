# Change log
This files contains a version history including all changes relevant for semantic Versioning...

*(it is automatically maintained using the ['KornSW-VersioningUtil'](https://github.com/KornSW/VersioningUtil))*

## Upcoming Changes

*(none)*



## v 2.4.6
released **2025-06-27**, including:
 - Fix: Exception when accessing context.Features



## v 2.4.5
released **2025-06-27**, including:
 - Fixed constellations when cContextAdaper for Asp.netCoreWebApi is used after Request-loop has finished (disposed Feature-Collection in HttpContext) and made it more Robust



## v 2.4.4
released **2025-05-30**, including:
 - Removed .NET 4.6-Targets and enabled .NET 8.0-Targets (while switching build-runner from Win-2019 to WIN-2022)



## v 2.4.3
released **2025-04-15**, including:
 - Added T4 Template for SmartStandards.Logging



## v 2.4.2
released **2025-04-01**, including:
 - AssemblyInfo.cs entfernt



## v 2.4.1
released **2025-04-01**, including:
 - Fehler in Solution-Struktur behoben
 - Update auf MS-Test 3.0.4



## v 2.4.0
released **2025-01-29**, including:
 - **new Feature**: added Target for .NET4.8.1
 - **new Feature**: added Target for .NET8
 - removed Target for .NET5



## v 2.3.0
(reverted)



## v 2.2.0
released **2025-01-17**, including:
 - **new Feature**: added support, for null-values as regular payload for ambient-fields



## v 2.1.1
released **2024-05-17**, including:
 - small Fix: against null-ref Exception when setting null as value into AmbientField.ContextAdapter property



## v 2.1.0
released **2024-04-05**, including:
 - added helper class to build 'Scoped-Singletons' (**new Feature**)



## v 2.0.1
released **2024-01-16**, including:
 - adopted small adjustments (internal naming) from Vodi



## v 2.0.0
released **2023-11-26**, including:
 - we have made a **Breaking Change** by adjusting the namespace to 'DistributedDataFlow' in order with come signature changes
 - added the **new Feature** of 'FlowingContracts' to filter the set of included data when capturing or restoring



## v 1.2.0
released **2023-11-17**, including:
 - **new Feature**: added support for global endpoints (without key prefix)



## v 1.1.0
released **2023-07-11**, including:
 - added the 'AmbientFieldAdapterMiddleware' (**new Feature**) (+setup extension) to configure a ASP .net core Webapi environment for appropriate usage of AmbientFields
 - added the 'ExtractFlowedDataAttribute' to automatically apply incoming flowed data (from HTTP-headers) into 'AmbientFields' (**new Feature**)
 - adjusted some small changes to the AmbientField



## v 1.0.0
released **2023-07-06**, including:
 - added AmbienceHub in order to support Distributed Data Flow (**MVP** state now reached)
 - Reorganized solution structure to be compliant to our standards



