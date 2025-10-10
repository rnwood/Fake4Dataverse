# Fake4Dataverse: Test Automation Framework for Microsoft Dataverse and Power Platform

## About This Fork

Fake4Dataverse is a fork of the FakeXrmEasy project, originally created by Jordi Montaña. This fork continues the development of the testing framework under the MIT License, based on the last version of FakeXrmEasy that was available under that license.

**Author:** Rob Wood  
**Original Author:** Jordi Montaña (@jordimontana)  
**License:** MIT (see LICENSE.txt files in each project folder)

### Fork Basis

This fork is based on an early development version of FakeXrmEasy v2 (version 2.0.1), which was released under the MIT License. The original FakeXrmEasy project subsequently changed its licensing model. This fork preserves the last MIT-licensed version to ensure continued open-source availability for the community.

**Original Repositories:**
- Core: https://github.com/DynamicsValue/fake-xrm-easy-core
- Abstractions: https://github.com/DynamicsValue/fake-xrm-easy-abstractions
- Legacy: https://github.com/jordimontana82/fake-xrm-easy

The original repositories were licensed under MIT at the time this fork was created, as evidenced by the LICENSE files in those repositories.

## Why This Fork?

The original Fake4Dataverse project was an invaluable tool for the Dynamics 365 / Dataverse community, providing a comprehensive testing framework that enabled developers to write unit tests without requiring a live CRM instance. However, the original project moved to a commercial licensing model after version 2.x.

This fork serves several purposes:

1. **Preserve Open Source Access**: By forking from the last MIT-licensed version, we ensure that the community continues to have access to a free, open-source testing framework for Dataverse development.

2. **Community-Driven Development**: This fork is maintained by the community, for the community. We welcome contributions and aim to keep the project aligned with community needs.

3. **Modern Platform Support**: While respecting the original codebase, we aim to update and maintain compatibility with modern versions of Dataverse, Power Platform, and .NET.

4. **Legal Clarity**: This fork is completely legal and in accordance with the MIT License under which the original Fake4Dataverse was released. The MIT License explicitly permits forking, modification, and redistribution of the code.

## Is Forking from the Last MIT Version Legal?

**Absolutely yes.** The MIT License is one of the most permissive open-source licenses and explicitly grants the rights to:
- Use the software for any purpose
- Make copies and distribute them
- Modify the source code
- Distribute modified versions

The original Fake4Dataverse was released under the MIT License, which means that version and all prior versions remain available under that license permanently. The license cannot be retroactively revoked. When Jordi Montaña chose to change the licensing model for future versions, previous MIT-licensed versions remained under the MIT License.

This fork:
- Is based on version 2.x, which was released under the MIT License
- Properly acknowledges the original author (Jordi Montaña) in all LICENSE files
- Maintains all original copyright notices as required by the MIT License
- Continues to use the MIT License for all derivatives

## Acknowledgments

We are deeply grateful to **Jordi Montaña** for creating Fake4Dataverse and releasing it under the MIT License. His work has been instrumental to thousands of developers in the Dynamics 365 and Power Platform community. This fork aims to honor that legacy by continuing to provide free, open-source testing tools to the community.

## Project Structure

This is a monorepo containing three main projects:

### 1. Fake4DataverseAbstractions
- **Location**: `/Fake4DataverseAbstractions/`
- **Purpose**: Contains abstractions, interfaces, POCOs, enums, and base types used across the framework
- **Former Name**: Fake4Dataverse.Abstractions

### 2. Fake4DataverseCore  
- **Location**: `/Fake4DataverseCore/`
- **Purpose**: Core implementation including middleware, CRUD operations, query translation, and message executors
- **Former Name**: Fake4Dataverse.Core

### 3. Fake4Dataverse (Legacy Package)
- **Location**: `/Fake4Dataverse/`
- **Purpose**: Legacy/compatibility package
- **Former Name**: Fake4Dataverse

## Getting Started

**📚 Migrating from FakeXrmEasy?** Check out the comprehensive [Migration Guide](./Fake4Dataverse/README.md#migration-guide) for step-by-step instructions on migrating from FakeXrmEasy v1.x or v3.x to Fake4Dataverse v2.x.

Please refer to the README files in each project folder for specific build instructions and usage examples:
- [Fake4DataverseAbstractions README](./Fake4DataverseAbstractions/README.md)
- [Fake4DataverseCore README](./Fake4DataverseCore/README.md)
- [Fake4Dataverse README](./Fake4Dataverse/README.md)

## Building

Each project has its own build script. See individual project READMEs for details.

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues.

## Support

This is a community-maintained project. While we strive to provide a quality framework, this software is provided "as is" without warranty of any kind, as specified in the MIT License.

## License

This project is licensed under the MIT License - see the LICENSE.txt files in each project directory for details.
