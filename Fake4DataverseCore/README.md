Fake4Dataverse: Test Automation for Microsoft Dataverse and Power Platform
=================================================================================

**Note:** This is a fork of the original FakeXrmEasy project by Jordi Montaña. See the [main README](../README.md) for information about this fork and its relationship to the original project.

|Build|Code Quality|
|-----|------------|
|![.NET Core](https://github.com/DynamicsValue/fake-xrm-easy-core/workflows/CI/badge.svg)|[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=DynamicsValue_fake-xrm-easy-core&metric=alert_status&token=275c67c8b62adac17e4e9a0ae210c77703d671d1)](https://sonarcloud.io/dashboard?id=DynamicsValue_fake-xrm-easy-core)||

<b>Streamline unit testing</b> in Dataverse by faking the `IOrganizationService` to work with an in-memory context.

<b>Drive your development</b> by unit testing any plugin, code activity, or 3rd party app using the `OrganizationService` easier and faster than ever before.


|Version|Package Name|NuGet|
|-----------|------|-----|
|Dynamics v9 (>= 9.x)|Fake4Dataverse.9|[![Nuget](https://buildstats.info/nuget/Fake4Dataverse.9?v=4.0.0)](https://www.nuget.org/packages/Fake4Dataverse.9)|
|Dynamics 365 (8.2.x)|Fake4Dataverse.365|[![Nuget](https://buildstats.info/nuget/Fake4Dataverse.365?v=4.0.0)](https://www.nuget.org/packages/Fake4Dataverse.365)|
|Dynamics CRM 2016 ( >= 8.0 && <= 8.1)|Fake4Dataverse.2016|[![Nuget](https://buildstats.info/nuget/Fake4Dataverse.2016?v=4.0.0)](https://www.nuget.org/packages/Fake4Dataverse.2016)|
|Dynamics CRM 2015 (7.x)|Fake4Dataverse.2015|[![Nuget](https://buildstats.info/nuget/Fake4Dataverse.2015?v=4.0.0)](https://www.nuget.org/packages/Fake4Dataverse.2015)|
|Dynamics CRM 2013 (6.x)|Fake4Dataverse.2013|[![Nuget](https://buildstats.info/nuget/Fake4Dataverse.2013?v=4.0.0)](https://www.nuget.org/packages/Fake4Dataverse.2013)|
|Dynamics CRM 2011 (5.x)|Fake4Dataverse|[![Nuget](https://buildstats.info/nuget/Fake4Dataverse?v=4.0.0)](https://www.nuget.org/packages/Fake4Dataverse)|

Supports Dynamics CRM 2011, 2013, 2015, 2016, and Dynamics 365 (8.x and 9.x). <b>NOTE:</b> With the release of Dynamics 365 v9 we are changing the naming convention for new packages to match the major version.

## Semantic Versioning

The NuGet packages use semantic versioning like this:

    x.y.z  => Major.Minor.Patch
       
x: stands for the major version. The current major version is 4.

y: minor version. Any minor updates add new functionality without breaking changes. An example of these would be a new operator or a new fake message executor.

z: patch. Any update to this number means new bug fixes for the existing functionality. A new minor version might also include bug fixes too.

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues.

When raising an issue:
* <u>**Please provide a sample unit test**</u> to reproduce any issues detected where possible. This will speed up the resolution.
* Attach all generated early bound typed entities required (if you're using early bound).

**If you're using the framework, please do [Star the project](https://github.com/rnwood/fake-xrm-free)**

## Roadmap

*  TODO: Implement remaining Dataverse messages. To know which ones have been implemented so far, see `FakeMessageExecutor` implementation status.
*  TODO: Increase test coverage.



## Tests disappeared?

Try deleting anything under the VS test explorer cache: `%Temp%\VisualStudioTestExplorerExtensions`

