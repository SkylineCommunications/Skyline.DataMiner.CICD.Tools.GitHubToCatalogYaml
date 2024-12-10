# Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml

## Overview

The **Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml** tool automates the creation or updating of a `catalog.yml` file based on a GitHub repository's metadata. This file, which can be automatically generated and maintained by this tool, serves as the primary input for [registering an item to the DataMiner Catalog](https://docs.dataminer.services/user-guide/Cloud_Platform/Catalog/Register_Catalog_Item.html). By extracting essential metadata from GitHub, such as descriptions, tags, and topics, this tool ensures that catalog entries align with the latest repository information, streamlining the process of catalog registration and maintenance within DataMiner's ecosystem.

## Key Features

- **Metadata Extraction**: Retrieves essential information (description, tags, repository URL) from GitHub to populate `catalog.yml`.
- **Type Inference**: Detects artifact type from repository naming conventions or GitHub topics.
- **Automatic Catalog Updates**: Creates or extends `catalog.yml` with up-to-date repository data.

## GitHub UI Mappings to `catalog.yml` Fields

| **GitHub UI**                  | **`catalog.yml` Field** | **Description**                                                                                                 |
|--------------------------------|-------------------------|-----------------------------------------------------------------------------------------------------------------|
| Repository Name                | `Title`                | Used as the title if no title is set in `catalog.yml`.                                                          |
| Description                    | `ShortDescription`     | Fetched from the repository’s description, if not provided in `catalog.yml`.                                    |
| Topics                         | `Tags`                 | GitHub topics are added as tags for the catalog item.                                                           |
| URL                            | `SourceCodeUrl`        | Generated as `https://github.com/{owner}/{repo}`, if missing in `catalog.yml`.                                  |
| Variable: `CATALOGIDENTIFIER`  | `Id`                   | If not specified in the existing YAML or as a variable, an identifier is generated automatically for each catalog entry. |
| Owners                         | `Owners`               | Customizable, with owner email, name, and URL settings.                                                         |

## Auto-Generated Catalog YAML File

This tool not only extends an existing `catalog.yml` or `manifest.yml` but also generates an `auto-generated-catalog.yml` file in the `.githubtocatalog` directory. This secondary file is essential because:

1. **Tracking Catalog IDs**: By committing and pushing `auto-generated-catalog.yml`, the tool can create a unique ID on the first run and reuse this ID on future runs. This avoids duplicate catalog records that can occur if new IDs are generated in every workflow run.
2. **Workflow Automation**: The `auto-generated-catalog.yml` is maintained separately to allow other processes to retrieve it as needed without modifying the primary catalog file in each update.

## Inferring Catalog Item Type

The tool can infer the artifact type in two ways:

1. **Repository Naming Convention**: Follows the [GitHub Repository Naming Convention](https://docs.dataminer.services/develop/CICD/Skyline%20Communications/Github/Use_Github_Guidelines.html#repository-naming-convention) to infer the type from the repository name.

2. **GitHub Topic**: If the repository does not follow the naming convention, the tool relies on a GitHub topic that corresponds to one of the recognized artifact types.

If neither condition is met, the workflow will fail, and an error will be returned.

### Artifact Types

- **AS**: `Automation`
- **C**: `Connector`
- **CF**: `Custom Solution`
- **CHATOPS**: `ChatOps Extension`
- **D**: `Dashboard`
- **DISMACRO**: `Custom Solution`
- **DOC**: `Custom Solution`
- **F**: `Automation`
- **GQIDS**: `Ad Hoc Data Source`
- **GQIO**: `Data Transformer`
- **LSO**: `Automation`
- **PA**: `Automation`
- **PLS**: `Automation`
- **S**: `Custom Solution`
- **SC**: `Scripted Connector`
- **T**: `Custom Solution`
- **UDAPI**: `User-Defined API`
- **V**: `Visual Overview`
- **LCA**: `Custom Solution`

## Installation & Usage

### Prerequisites

Ensure you have [.NET](https://dotnet.microsoft.com/download) installed to run the tool.

### Installation

Install the tool via the terminal:

```bash
dotnet tool install -g Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml
```

### Running the Tool

Execute the tool using the following command and options:

```bash
github-to-catalog-yaml --workspace "/path/to/workspace" --github-token "your_token" --github-repository "owner/repo"
```

### Command Options

- `--workspace` (required): Path to where `catalog.yml` is located or will be created.
- `--github-token` (required): GitHub token (Personal Access Token or `secrets.GITHUB_TOKEN`).
- `--github-repository` (required): GitHub repository name in the format `owner/repo`.
- `--debug` (optional): Enable debug logging for detailed output.

## Example Usage in GitHub Workflow

Below is an example workflow for using this tool to automate catalog management in GitHub Actions. Refer to [Automation Master SDK Workflow](https://github.com/SkylineCommunications/_ReusableWorkflows/blob/main/.github/workflows/Automation%20Master%20SDK%20Workflow.yml) for a complete setup.

```yaml
auto_generate_catalog_yaml:
  name: Auto-Generating Catalog from GitHub
  if: inputs.referenceType == 'branch'
  runs-on: ubuntu-latest
  steps:
    - name: Install .NET Tools
      run: |
        dotnet tool install -g Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml --prerelease

    - name: Create or Extend Catalog.yml
      run: |
        github-to-catalog-yaml --workspace "${{ github.workspace }}" --github-token ${{ secrets.GITHUB_TOKEN }} --github-repository "${{ github.repository }}" --catalog-identifier "${{ vars.catalogIdentifier }}"

    - name: Commit .githubtocatalog/auto-generated-catalog
      shell: pwsh
      run: |
        git config --global user.name "github-actions[bot]"
        git config --global user.email "github-actions[bot]@users.noreply.github.com"
        git add "${{ github.workspace }}/.githubtocatalog/auto-generated-catalog.yml"
        
        # Check if there are any changes to be committed
        git diff --staged --quiet
        if ($LASTEXITCODE -ne 0) {
          git commit -m "auto-generated"
        }
        else {
          Write-Host "No changes to commit."
        }
      
    - name: Push .githubtocatalog/auto-generated-catalog
      run: |
        git push
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

This workflow installs the **Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml** tool, generates or extends the `catalog.yml`, and pushes any updates in `.githubtocatalog/auto-generated-catalog.yml` to avoid duplicate catalog entries.
