# Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml

## About

This tool helps extend or create a `catalog.yml` file by pulling data from a GitHub repository. It automates the extraction of metadata such as description, tags, and repository information to ensure that the catalog YAML file is up-to-date with the repository's data.

### About DataMiner

DataMiner is a leading platform for vendor-independent control and monitoring of devices and services. It provides a comprehensive data acquisition and control layer, enabling access to data from on-premises, cloud, or hybrid setups. The platform supports thousands of connectors and allows users to build their own.

For more information, visit [About DataMiner](https://aka.dataminer.services/about-dataminer).

### About Skyline Communications

Skyline Communications offers world-class solutions to leading companies across the globe, providing tools to streamline their operations. Explore more on [Skyline's proven track record](https://aka.dataminer.services/about-skyline).

## Getting Started

1. **Install the tool:**

   Run the following command in your terminal to install the tool globally:

   ```bash
   dotnet tool install -g Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml
   ```

2. **Run the tool:**

   After installation, you can run the tool by invoking the command below, followed by necessary arguments:

   ```bash
   github-to-catalog-yaml [options]
   ```

### Purpose

This tool automates the process of generating or updating a `catalog.yml` file for a GitHub repository. It pulls information such as the repository description, GitHub topics, and inferred artifact types to ensure that the catalog file is correctly populated with relevant metadata.

> **Important**: The process will fail if it cannot detect the **Catalog Item Type** from either the repository name or the GitHub topics. Ensure the repository follows naming conventions or uses the appropriate topics to avoid this failure.

## Command Options

The following options are required for the tool to function correctly:

### `--workspace`

- **Description**: The path to the workspace where the `catalog.yml` file is located or will be created.
- **Required**: Yes
- **Example**: `--workspace /path/to/workspace`

### `--github-token`

- **Description**: Your GitHub token, either a Personal Access Token (PAT) or the `secrets.GITHUB_TOKEN` provided by GitHub Actions.
- **Required**: Yes
- **Example**: `--github-token ghp_abc123def456`

### `--github-repository`

- **Description**: The full GitHub repository name in the format `owner/repo` (e.g., `SkylineCommunications/SLC-AS-MediaOps-Apps`).
- **Required**: Yes
- **Example**: `--github-repository SkylineCommunications/SLC-AS-MediaOps-Apps`

### `--debug`

- **Description**: Enables debug logging for more verbose output. By default, this is set to `false`.
- **Required**: No
- **Example**: `--debug true`

## How the Tool Works

The tool follows these key steps to process and update the `catalog.yml`:

1. **Extract Metadata from GitHub**: 
   The tool pulls essential data such as the repository description, GitHub topics, and repository name.
   
2. **Check or Generate the Catalog ID**: 
   If the `catalog.yml` file does not contain an ID, the tool will generate a new one or retrieve an existing ID from the repository.

3. **Update Description**: 
   If the file is missing a description, the tool fetches the repository description from GitHub and adds it to the catalog file.

4. **Add Tags**: 
   The tool automatically retrieves GitHub topics and adds them to the catalog YAML as tags.

5. **Infer Catalog Item Type**:
   
   The tool can identify the artifact types either from the repository name or from GitHub topics:

   - If the repository follows the naming conventions outlined in the [GitHub Repository Naming Convention](https://docs.dataminer.services/develop/CICD/Skyline%20Communications/Github/Use_Github_Guidelines.html#repository-naming-convention), the tool can automatically detect the type from the repository name itself without needing any GitHub topic.
   
   - If a different naming convention is used, the tool relies on the presence of a GitHub topic with the value of one of the [Artifact Types](#artifact-types) to infer the type.

If neither the repository name follows the convention nor the appropriate GitHub topic is present, the tool will fail to detect the type and will throw an error.

6. **Save or Create the File**: 
   If the tool successfully processes the metadata, it will save the updated or newly created `catalog.yml` file in the specified workspace.

### Artifact Types

  - **AS**: automationscript
  - **C**: connector
  - **CF**: companionfile
  - **CHATOPS**: chatopsextension
  - **D**: dashboard
  - **DISMACRO**: dismacro
  - **DOC**: documentation
  - **F**: functiondefinition
  - **GQIDS**: gqidatasource
  - **GQIO**: gqioperator
  - **LSO**: lifecycleserviceorchestration
  - **PA**: processautomation
  - **PLS**: profileloadscript
  - **S**: solution
  - **SC**: scriptedconnector
  - **T**: testingsolution
  - **UDAPI**: userdefinedapi
  - **V**: visio

## Example Usage

```bash
github-to-catalog-yaml --workspace "/path/to/workspace" --github-token "ghp_abc123def456" --github-repository "SkylineCommunications/SLC-AS-MediaOps-Apps"
```

This command extends or creates the `catalog.yml` file in the provided workspace, using data from the GitHub repository `SkylineCommunications/SLC-AS-MediaOps-Apps`.
