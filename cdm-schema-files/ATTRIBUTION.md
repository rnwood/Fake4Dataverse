# CDM Schema Files Attribution

This directory contains Common Data Model (CDM) schema files downloaded from the Microsoft CDM repository.

## Source

These files are from the Microsoft Common Data Model repository:
- **Repository**: https://github.com/microsoft/CDM
- **License**: Creative Commons Attribution 4.0 International (CC BY 4.0)
- **License URL**: https://creativecommons.org/licenses/by/4.0/

## Files Included

The following CDM schema files are included for offline use and to prevent test timeouts:

- `Account.cdm.json` - Account entity schema from core/applicationCommon
- `Contact.cdm.json` - Contact entity schema from core/applicationCommon
- `Opportunity.cdm.json` - Opportunity entity schema from core/applicationCommon/foundationCommon/crmCommon/sales

## Purpose

These files are included in this repository to:
1. Enable offline development and testing
2. Prevent test timeouts caused by network downloads
3. Provide a reliable fallback when the CDM repository is unavailable
4. Ensure consistent test results

## Usage

The Fake4Dataverse CDM import functionality will:
1. First attempt to use the file cache (LocalAppData)
2. Then check these embedded schema files
3. Finally fall back to downloading from GitHub if needed

## Updates

These files are periodically updated to match the latest schemas from the Microsoft CDM repository. To update:

```bash
cd cdm-schema-files
curl -o Account.cdm.json "https://raw.githubusercontent.com/microsoft/CDM/master/schemaDocuments/core/applicationCommon/Account.cdm.json"
curl -o Contact.cdm.json "https://raw.githubusercontent.com/microsoft/CDM/master/schemaDocuments/core/applicationCommon/Contact.cdm.json"
curl -o Opportunity.cdm.json "https://raw.githubusercontent.com/microsoft/CDM/master/schemaDocuments/core/applicationCommon/foundationCommon/crmCommon/sales/Opportunity.cdm.json"
```

## Attribution

As required by the Creative Commons Attribution 4.0 International license:

**Original Work**: Common Data Model
**Author**: Microsoft Corporation
**Source**: https://github.com/microsoft/CDM
**License**: CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/)

This material is used under the terms of the CC BY 4.0 license which permits sharing and adaptation with appropriate credit given to the original creator.
