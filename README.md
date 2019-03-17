# cdss-lib-cdss-cs #

This repository contains [Colorado's Decision Support Systems (CDSS)](https://www.colorado.gov/cdss)
C# library for shared CDSS code.
This library is necessary because the node network code is used in multiple applications,
including StateMod model.
These packages are used by CDSS StateMod C# applications and other C# libraries.

**This code is experimental and is used in support of the experimental
[StateMod C#](https://github.com/OpenCDSS/cdss-app-statemod-cs) code.**

See the following sections in this page:

* [Repository Folder Structure](#repository-folder-structure)
* [Repository Dependencies](#repository-dependencies)
* [Development Environment Folder Structure](#development-environment-folder-structure)
* [Version](#version)
* [Development Environment](#development-environment)
* [Code Generation](#code-generation)
* [Contributing](#contributing)
* [License](#license)
* [Contact](#contact)

--------

## Repository Folder Structure ##

The following are the main folders and files in this repository, listed alphabetically.
See also the [Development Environment Folder Structure](#development-environment-folder-structure)
for overall folder structure recommendations.
**This structure needs to be completely described when the experimental C# StateMod code is functional.**

```
cdss-lib-common-cs/           Source code and development working files.
  .git/                       Git repository folder (DO NOT MODIFY THIS except with Git tools).
  .gitattributes              Git configuration file for repository.
  .gitignore                  Git configuration file for repository.
  LICENSE.md                  Library license file.
  README.md                   This file.
  resources/                  Additional resources.
  src/                        Source code.
  test/                       Unit tests.
```

## Repository Dependencies ##

Repository dependencies fall into two categories as indicated below.

### Repository Dependencies for this Repository ###

This library depends on other repositories listed in the following table.

|**Repository**                                                             |**Description**|
|---------------------------------------------------------------------------|----------------------------------------------------|
|[`cdss-lib-common-cs`](https://github.com/OpenCDSS/cdss-lib-common-cs)     |Library of core utility code used by multiple repositories.|

### Repositories that Depend on this Repository ###

The following repositories are known to depend on this repository:

|**Repository**                                                             |**Description**|
|---------------------------------------------------------------------------|----------------------------------------------------|
|[`cdss-lib-models-cs`](https://github.com/OpenCDSS/cdss-lib-models-cs)     |Library to read/write CDSS StateCU and StateMod model files.|

## Development Environment Folder Structure ##

The following folder structure is recommended for software development.
Top-level folders should be created as necessary.
Repositories are expected to be on the same folder level to allow cross-referencing
scripts in those repositories to work.
StateMod C# is used as an example, and normally use of this repository will occur
through development of the main CDSS applications.
See the application's developer documentation for more information.

```
C:\Users\user\                               Windows user home folder (typical development environment).
/home/user/                                  Linux user home folder (not tested).
/cygdrive/C/Users/user                       Cygdrive home folder (not tested).
  cdss-dev/                                  Projects that are part of Colorado's Decision Support Systems.
    StateMod-CS/                             StateMod C# product folder (will be similar for other applications).
-------------- below here folder names should match exactly -----------------
      git-repos/                             Git repositories for StateMod C#.
        cdss-lib-cdss-cs/                    CDSS shared code repository.
        cdss-lib-common-cs/                  Shared code repository.
        cdss-lib-models-cs/                  Model code repository.
        cdss-app-statemod-cs/                StateMod C# code.
```

## Version ##

A version for the library is typically not defined.
Instead, tags are used to cross-reference the library version with commit of application code such as StateMod C#.
This allows checking out a version of the library consistent with an application version.
This approach might need to change if the library is seen as an independent resource that is used by many third-party applications.

## Development Environment ##

See more information in the
[StateMod C# Repository Development Environment](https://github.com/OpenCDSS/cdss-app-statemod-cs#development-environment)
documentation.

## Code Generation  ##

The C# code in this repository was auto-generated using the
[cdss-lib-common-java](https://github.com/OpenCDSS/cdss-lib-common-java) Java library as input and the
[Tangible Software Solutions](https://www.tangiblesoftwaresolutions.com/product_details/java_to_csharp_converter.html) Java to C# Converter.
The converter was used to interactively convert the input `src` folder with the following options:

1. Accept default conversion options other than exceptions below.
2. ***Options / Miscellaneous Options**
	* Uncheck ***Convert methods to properties if preceded by `get` or `set` (and optionally `is)***.
	This will ensure that C# code is more similar to original Java code.
	* Uncheck ***Convert generic type arguments of Java `wrapper` types (e.g., Integer) to primitive types.***

## Contributing ##

Contributions to this project can be submitted using the following options:

1. Software developers with commit privileges can write to this repository
as per normal OpenCDSS development protocols.
2. Post an issue on GitHub with suggested change.  Provide information using the issue template.
3. Fork the repository, make changes, and do a pull request.
Contents of the current master branch should be merged with the fork to minimize
code review before committing the pull request.

See also the [OpenCDSS / protocols](http://learn.openwaterfoundation.org/cdss-website-opencdss/) for each software application - currently
hosted on the Open Water Foundation website while the OpenCDSS server is
being configured.

## License ##

Copyright Colorado Department of Natural Resources.

The software is licensed under GPL v3+. See the [LICENSE.md](LICENSE.md) file.

## Contact ##

See the [OpenCDSS information](http://learn.openwaterfoundation.org/cdss-website-opencdss) for overall contacts and contacts for each software product.
