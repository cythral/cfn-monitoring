namespace Cythral.CloudFormation.Monitoring.DatabaseStopper

open System
open System.Runtime.InteropServices
open System.Collections.Generic
open System.Threading.Tasks

open Amazon.CloudWatch
open Amazon.Lambda.Core

open Amazon.ResourceGroups
open Amazon.RDS
open Amazon.RDS.Model

open Cythral.CloudFormation.Monitoring.DatabaseStopper.DatabaseUtils
open Cythral.CloudFormation.Monitoring.DatabaseStopper.DatabaseUtils.DatabaseListing
open DatabaseUtils.DatabaseMetrics

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[<assembly:LambdaSerializer(typeof<Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer>)>]
()

type Request() =
    member val MonitoredDatabasesGroupName = "" with get, set

type Handler() =
    member __.StopIfInactive(database: Database, cloudwatchClient: IAmazonCloudWatch, rdsClient: IAmazonRDS) =
        async {
            let! connectionCount =
                GetDatabaseConnectionCountFromLastHour(database, cloudwatchClient)
                |> Async.AwaitTask

            let! uptime =
                GetDatabaseUptimeLastHour(database, cloudwatchClient)
                |> Async.AwaitTask

            if uptime >= 1.0 && connectionCount = 0.0 then
                match database with
                | Database.DBCluster cluster ->
                    let request =
                        StopDBClusterRequest(DBClusterIdentifier = cluster.DBClusterIdentifier)

                    rdsClient.StopDBClusterAsync(request)
                    |> Async.AwaitTask
                    |> ignore

                | Database.DBInstance instance ->
                    let request =
                        StopDBInstanceRequest(DBInstanceIdentifier = instance.DBInstanceIdentifier)

                    rdsClient.StopDBInstanceAsync(request)
                    |> Async.AwaitTask
                    |> ignore

            return None
        }
        |> Async.StartAsTask

    member __.Handle(request: Request, context: ILambdaContext): Task<bool> =
        async {
            let resourceGroupsClient = new AmazonResourceGroupsClient()
            let cloudwatchClient = new AmazonCloudWatchClient()
            let rdsClient = new AmazonRDSClient()
            request.MonitoredDatabasesGroupName <- "test"
            let! databases =
                ListDatabases(request.MonitoredDatabasesGroupName, resourceGroupsClient, rdsClient)
                |> Async.AwaitTask

            let tasks = List<Task> []

            for database in databases do
                let task =
                    __.StopIfInactive(database, cloudwatchClient, rdsClient)

                tasks.Add(task)

            Task.WaitAll(tasks.ToArray())
            return true
        }
        |> Async.StartAsTask
