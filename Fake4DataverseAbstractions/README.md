Fake4Dataverse.Abstractions
========================================================

Abstractions for Fake Xrm Easy

|Build|Quality Gate|Coverage|Security|Maintainability|Reliability|
|-----| ---------- | ------ | ------ | ------------- | --------- |
|![Build](https://github.com/DynamicsValue/fake-xrm-easy-abstractions/workflows/CI/badge.svg)|[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=DynamicsValue_fake-xrm-easy-abstractions&metric=alert_status&token=ecafcd1ed73f047af28e3cbdd3375fe1a789814c)](https://sonarcloud.io/dashboard?id=DynamicsValue_fake-xrm-easy-abstractions)|[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=DynamicsValue_fake-xrm-easy-abstractions&metric=coverage&token=ecafcd1ed73f047af28e3cbdd3375fe1a789814c)](https://sonarcloud.io/dashboard?id=DynamicsValue_fake-xrm-easy-abstractions)|[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=DynamicsValue_fake-xrm-easy-abstractions&metric=security_rating&token=ecafcd1ed73f047af28e3cbdd3375fe1a789814c)](https://sonarcloud.io/dashboard?id=DynamicsValue_fake-xrm-easy-abstractions)|[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=DynamicsValue_fake-xrm-easy-abstractions&metric=sqale_rating&token=ecafcd1ed73f047af28e3cbdd3375fe1a789814c)](https://sonarcloud.io/dashboard?id=DynamicsValue_fake-xrm-easy-abstractions)|[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=DynamicsValue_fake-xrm-easy-abstractions&metric=reliability_rating&token=ecafcd1ed73f047af28e3cbdd3375fe1a789814c)](https://sonarcloud.io/dashboard?id=DynamicsValue_fake-xrm-easy-abstractions)|

# Building

In order to build this project you will need .NET Core 3.1 and PowerShell 7.

## Building on Windows

* [Installing PowerShell on Windows](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-core-on-windows?view=powershell-6)
   
## Building on Linux

* [Installing Powershell on Linux](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-core-on-linux?view=powershell-6)

Once installed, run the following commands:


The following will just build the solution

    pwsh ./build

This command will build AND push the output package into a predefined local folder (this will create a local-packages folder where Fake4Dataverse.Abstractions will be pushed)

    pwsh ./build-push-local


# Contributing



