# Azure DevOps Metrics - Code Coverage Calculation POC

This POC uses the Azure DevOps REST API to fetch all of the build configurations for the specified Azure DevOps Project and calculates total coverage percentages.

## Get Started

This application is a .NET Core 5 application.

1. Open this folder in VS Code.
2. Change Program.cs so that the following lines of code have correct values.
```
            Uri orgUrl = new Uri("https://dev.azure.com/fabrikam"); // Organization URL, for example: https://dev.azure.com/fabrikam               
            String personalAccessToken = "4jw...gjq";               // See https://docs.microsoft.com/azure/devops/integrate/get-started/authentication/pats
            String project = "Billing";
```
3. Open `Terminal` and run `dotnet build` or use `make build`.
4. Run `dotnet run --project azmetrics` or use `make run`.

## Good Luck!