using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.TestResults.WebApi;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;

namespace azmetrics
{
    class Program
    {
        static void Main(string[] args)
        {
            var beginTime = DateTime.Now;

            Uri orgUrl = new Uri("https://dev.azure.com/fabrikam");         // Organization URL, for example: https://dev.azure.com/fabrikam               
            String personalAccessToken = "4jw...gjq";  // See https://docs.microsoft.com/azure/devops/integrate/get-started/authentication/pats
            String project = "Billing";

            // Create a connection
            
            Console.WriteLine("Connecting to Azure DevOps");
            VssConnection connection = new VssConnection(orgUrl, new VssBasicCredential(string.Empty, personalAccessToken));

            // //ShowTestResults(connection, "foobar", buildId).Wait();
            // // var build = GetBuild(connection, project, buildId);
            // // ShowBuildDetails(build.Result);
            // // var coverageData = GetCodeCoverageData(connection, project, buildId);
            // // ShowCodeCoverageData(coverageData.Result);
            // // var repos = GetRepositories(connection, project);
            // // ShowRepositories(repos.Result);
            Console.WriteLine("Getting Builds Definitions");
            var buildDefinitions = GetBuildDefinitions(connection, project).Result;
            // //ShowBuildDefinitions(buildDefinitions);
            // // Console.WriteLine("buildDefinitions.Count: {0}", buildDefinitions.Result.Count);
            // // buildDefinitions.Result.ForEach(x => Console.WriteLine("BuildDefinition.Id: {0}", x.Id));
            IEnumerable<int> buildDefinitionIds = buildDefinitions.Select(x => x.Id);                
            // // Console.WriteLine("buildDefinitionIds: {0}", buildDefinitionIds.Count());
            Console.WriteLine("Getting Builds");
            var builds = GetBuilds(connection, project, buildDefinitionIds).Result;
            // // Console.WriteLine("builds.Count: {0}", builds.Result.Count);

            double totalBlocks = 0;
            double totalLines = 0;
            double coveredBlocks = 0;
            double coveredLines = 0;
            double countOfCodeCoverageRecordsWithBlocks = 0;
            double countOfCodeCoverageRecordsWithLines = 0;
            double countOfBuildsWithoutCodeCoverageRecords = 0;

            Console.WriteLine("Getting Code Coverages for Builds");
            foreach (var build in builds)
            {
                //ShowBuildDetails(build);
                var coverage = GetCodeCoverageData(connection, project, build.Id).Result;
                //ShowCodeCoverageData(coverage);
                if (coverage.Count == 0)
                {
                    countOfBuildsWithoutCodeCoverageRecords++;
                }
                foreach(var codeCoverageArray in coverage)
                {
                    foreach(var codeCoverageDetail in codeCoverageArray.CoverageStats)
                    {
                        if (codeCoverageDetail.Label == "Blocks")
                        {
                            totalBlocks += codeCoverageDetail.Total;
                            coveredBlocks += codeCoverageDetail.Covered;
                            countOfCodeCoverageRecordsWithBlocks++;
                        }
                        else if (codeCoverageDetail.Label == "Lines")
                        {
                            totalLines += codeCoverageDetail.Total;
                            coveredLines += codeCoverageDetail.Covered;
                            countOfCodeCoverageRecordsWithLines++;
                        }
                    }
                }
            }

            // mocked values
            // totalBlocks = 4490958;
            // totalLines = 2169749;
            // coveredBlocks = 341647;
            // coveredLines = 189313;
            // countOfCodeCoverageRecordsWithBlocks = 84;
            // countOfCodeCoverageRecordsWithLines = 84;
            // double percentBlocksCovered = 0.07128763248;
            // double percentLinesCovered = 0.14987349785;
            double percentBlocksCovered = coveredBlocks / totalBlocks;
            double percentLinesCovered = coveredLines / totalLines;

            Console.WriteLine("Coverage Statistics:");
            Console.WriteLine("  Total Blocks: {0:##,#}", totalBlocks);
            Console.WriteLine("  Covered Blocks: {0:##,#}", coveredBlocks);
            Console.WriteLine("  Coverage Records with Blocks: {0:##,#}", countOfCodeCoverageRecordsWithBlocks);
            Console.WriteLine("  Percentage of Covered Blocks: {0:#0.#####%}", percentBlocksCovered);  
            Console.WriteLine("  Total Lines: {0:##,#}", totalLines);
            Console.WriteLine("  Covered Lines: {0:##,#}", coveredLines);  
            Console.WriteLine("  Percentage of Covered Lines: {0:#0.#####%}", percentLinesCovered);  
            Console.WriteLine("  Coverage Records with Lines: {0:##,#}", countOfCodeCoverageRecordsWithLines);  
            Console.WriteLine("  Builds Without Coverage Stats: {0:##,#}", countOfBuildsWithoutCodeCoverageRecords);    
            
            Console.WriteLine();
            var duration = DateTime.Now - beginTime;
            Console.WriteLine("Time Elapsed: {0:c}", duration);          
        } 

        static private async Task<List<Build>> GetBuilds(VssConnection connection, string projectName, IEnumerable<int> definitions)
        {            
            // Get an instance of the test results client
            var client = connection.GetClient<BuildHttpClient>();
            List<Build> returnValue = new List<Build>(); 
            try
            {
                // Iterate (as needed) to get the full set of build definitions
                var masterBuilds = await client.GetBuildsAsync(
                project: projectName, 
                    definitions: definitions,
                    reasonFilter: BuildReason.IndividualCI | BuildReason.BatchedCI,
                    statusFilter: BuildStatus.Completed, 
                    resultFilter: BuildResult.Succeeded,
                    branchName: "refs/heads/master",
                    maxBuildsPerDefinition: 1);

                returnValue.AddRange(masterBuilds);
                
                // Iterate (as needed) to get the full set of build definitions
                var mainBuilds = await client.GetBuildsAsync(
                project: projectName, 
                    definitions: definitions,
                    reasonFilter: BuildReason.IndividualCI | BuildReason.BatchedCI,
                    statusFilter: BuildStatus.Completed, 
                    resultFilter: BuildResult.Succeeded,
                    branchName: "refs/heads/main",
                    maxBuildsPerDefinition: 1);
                
                returnValue.AddRange(mainBuilds);
            }
            catch (System.Exception)
            {
                
                throw;
            }
            return returnValue;
        }

        static private async Task<Build> GetBuild(VssConnection connection, string projectName, int buildId)
        {
            // Get an instance of the work item tracking client
            BuildHttpClient buildClient = connection.GetClient<BuildHttpClient>();
            Task<Build> returnValue = null;
            
            try
            {
                // Get the specified work item
                await (returnValue = buildClient.GetBuildAsync(projectName, buildId));
            }
            catch (AggregateException aex)
            {
                VssServiceException vssex = aex.InnerException as VssServiceException;
                if (vssex != null)
                {
                    Console.WriteLine(vssex.Message);
                }
            }

            return await returnValue;
        }

        static private void ShowBuildDetails(Build build)
        {
            try
            {
                Console.WriteLine("Build");
                Console.WriteLine("  Id: {0}", build.Id);
                Console.WriteLine("  Name: {0}", build.Definition.Name);
                Console.WriteLine("  BuildNumber: {0}", build.BuildNumber);
                Console.WriteLine("  SourceBranch: {0}", build.SourceBranch);
                Console.WriteLine("  Status: {0}", build.Status);
                Console.WriteLine("  Result: {0}", build.Result);
                Console.WriteLine("  StartTime: {0}", build.StartTime);
                Console.WriteLine("  FinishTime: {0}", build.FinishTime);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static private async Task<List<CodeCoverageData>> GetCodeCoverageData(VssConnection connection, string projectName, int buildId)
        {
            // Get an instance of the test results client
            var client = connection.GetClient<TestResultsHttpClient>();
            List<CodeCoverageData> returnValue = null;
            
            try
            {
                // Get the specified 
                // List<BuildCoverage> codeCoverage = await client.GetBuildCodeCoverageAsync(projectName, buildId, 7);
                var codeCoverage = await client.GetCodeCoverageSummaryAsync(projectName, buildId);
                returnValue = new List<CodeCoverageData>(codeCoverage.CoverageData);
            }
            catch (AggregateException aex)
            {
                VssServiceException vssex = aex.InnerException as VssServiceException;
                if (vssex != null)
                {
                    Console.WriteLine(vssex.Message);
                }
            }

            return returnValue;
        }

        static private void ShowCodeCoverageData(List<CodeCoverageData> codeCoverageData)
        {
            try
            {
                Console.WriteLine("  CodeCoverageData:");
                foreach(var codeCoverageArray in codeCoverageData)
                {
                    foreach(var codeCoverageDetail in codeCoverageArray.CoverageStats)
                    // for(int i = 0; i < codeCoverageArray.CoverageStats.Count; i++)
                    {
                        Console.WriteLine("    {0}", codeCoverageDetail.Label);
                        Console.WriteLine("      Covered: {0}", codeCoverageDetail.Covered);
                        Console.WriteLine("      Total: {0}", codeCoverageDetail.Total);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static private async Task<List<GitRepository>> GetRepositories(VssConnection connection, string projectName)
        {            
            // Get an instance of the test results client
            var client = connection.GetClient<GitHttpClient>();
            List<GitRepository> returnValue = null;

            try
            {
                returnValue = await client.GetRepositoriesAsync(projectName);
            }
            catch (System.Exception)
            {
                
                throw;
            }
            return returnValue;
        }

        static private void ShowRepositories(List<GitRepository> repos)
        {
            try
            {
                Console.WriteLine("  Repositories:");
                foreach(var repo in repos)
                {
                    Console.WriteLine("      Name: {0}", repo.Name);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static private async Task<List<BuildDefinitionReference>> GetBuildDefinitions(VssConnection connection, string projectName)
        {            
            // Get an instance of the test results client
            var client = connection.GetClient<BuildHttpClient>();
            List<BuildDefinitionReference> buildDefinitions = new List<BuildDefinitionReference>();
            // List<Build> builds = await client.GetBuildsAsync(
            //     project: projectName, 
            //     definitions: buildDefinitions.Select(x => x.Id),
            //     reasonFilter: BuildReason.IndividualCI | BuildReason.BatchedCI,
            //     statusFilter: BuildStatus.Completed, 
            //     resultFilter: BuildResult.Succeeded,
            //     branchName: "master");
            try
            {
                // Iterate (as needed) to get the full set of build definitions
                string continuationToken = null;
                do
                {
                    IPagedList<BuildDefinitionReference> buildDefinitionsPage = 
                        await client.GetDefinitionsAsync2(
                            project: projectName, 
                            continuationToken: continuationToken);

                    buildDefinitions.AddRange(buildDefinitionsPage);

                    continuationToken = buildDefinitionsPage.ContinuationToken;
                } while (!String.IsNullOrEmpty(continuationToken));
            }
            catch (System.Exception)
            {
                
                throw;
            }
            return buildDefinitions;
        }

        static private void ShowBuildDefinitions(List<BuildDefinitionReference> buildDefinitions)
        {
            try
            {
                Console.WriteLine("  Build Definitions:");
                var jsonSerializer = Newtonsoft.Json.JsonSerializer.Create();
                string json = JsonConvert.SerializeObject(buildDefinitions[0], Formatting.Indented);
                Console.WriteLine(json);

                // foreach(var definition in buildDefinitions)
                // {
                //     Console.WriteLine("      Id: {0} Name: {1} Description: {2}", definition.Id, definition.Name, definition.Description);
                // }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
