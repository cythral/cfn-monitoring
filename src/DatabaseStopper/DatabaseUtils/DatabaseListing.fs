namespace Cythral.CloudFormation.Monitoring.DatabaseStopper.DatabaseUtils

open System
open System.Linq
open System.Collections.Generic
open System.Threading.Tasks

open Amazon.ResourceGroups
open Amazon.ResourceGroups.Model
open Amazon.RDS
open Amazon.RDS.Model


module DatabaseListing =
    let ListClusters (clusterArns: seq<string>, client: IAmazonRDS): Task<List<Database>> =
        async {
            let filter =
                Filter(Name = "db-cluster-id", Values = List<string> clusterArns)

            let request =
                DescribeDBClustersRequest(Filters = List<Filter> [ filter ])

            let! response =
                client.DescribeDBClustersAsync(request)
                |> Async.AwaitTask

            let clusters =
                query {
                    for cluster in response.DBClusters do
                        select (Database.DBCluster cluster)
                }

            return List<Database> clusters
        }
        |> Async.StartAsTask

    let ListInstances (instanceArns: seq<string>, client: IAmazonRDS): Task<List<Database>> =
        async {
            let filter =
                Filter(Name = "db-instance-id", Values = List<string> instanceArns)

            let request =
                DescribeDBInstancesRequest(Filters = List<Filter> [ filter ])

            let! response =
                client.DescribeDBInstancesAsync(request)
                |> Async.AwaitTask

            let instances =
                query {
                    for instance in response.DBInstances do
                        select (Database.DBInstance instance)
                }

            return List<Database> instances
        }
        |> Async.StartAsTask

    let GetClusterArnsFromResponse (response: ListGroupResourcesResponse) =
        query {
            for identifier in response.ResourceIdentifiers do
                where (identifier.ResourceType = "AWS::RDS::DBCluster")
                select identifier.ResourceArn
        }

    let GetClustersFromResponse (response: ListGroupResourcesResponse, rdsClient: IAmazonRDS) =
        async {
            let clusterArns = GetClusterArnsFromResponse(response)
            let clusters = List<Database> []

            if clusterArns.Count() > 0 then
                let! newClusters =
                    ListClusters(clusterArns, rdsClient)
                    |> Async.AwaitTask

                clusters.AddRange(newClusters)

            return clusters
        }
        |> Async.StartAsTask

    let GetInstanceArnsFromResponse (response: ListGroupResourcesResponse) =
        query {
            for identifier in response.ResourceIdentifiers do
                where (identifier.ResourceType = "AWS::RDS::DBInstance")
                select identifier.ResourceArn
        }

    let GetInstancesFromResponse (response: ListGroupResourcesResponse, rdsClient: IAmazonRDS) =
        async {
            let instanceArns = GetInstanceArnsFromResponse(response)
            let instances = List<Database> []

            if instanceArns.Count() > 0 then
                let! newInstances =
                    ListInstances(instanceArns, rdsClient)
                    |> Async.AwaitTask

                instances.AddRange(newInstances)

            return instances
        }
        |> Async.StartAsTask


    let ListDatabases (groupName: string, resourceGroupsClient: IAmazonResourceGroups, rdsClient: IAmazonRDS) =
        async {
            let filterValues =
                List<string>
                    [ "AWS::RDS::DBCluster"
                      "AWS::RDS::DBInstance" ]

            let filter =
                ResourceFilter(Name = ResourceFilterName.ResourceType, Values = filterValues)

            let request =
                ListGroupResourcesRequest(GroupName = groupName, Filters = List<ResourceFilter> [ filter ])

            let! response =
                resourceGroupsClient.ListGroupResourcesAsync(request)
                |> Async.AwaitTask

            let! clusters =
                GetClustersFromResponse(response, rdsClient)
                |> Async.AwaitTask

            let! instances =
                GetInstancesFromResponse(response, rdsClient)
                |> Async.AwaitTask

            let both = List<Database>()
            both.AddRange(clusters)
            both.AddRange(instances)

            return both
        }
        |> Async.StartAsTask
