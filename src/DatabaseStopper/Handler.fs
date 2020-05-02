namespace Cythral.CloudFormation.Monitoring.DatabaseStopper

open System
open System.Runtime.InteropServices
open System.Collections.Generic
open System.Threading.Tasks

open Amazon.CloudWatch
open Amazon.Lambda.Core

open Amazon.RDS
open Amazon.RDS.Model

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[<assembly:LambdaSerializer(typeof<Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer>)>]
()

type Request = { MonitoredDatabasesGroupName: string }

type Handler() =
    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    member __.Handle(request: Request, context: ILambdaContext): Task<List<DBCluster>> =
        async {
            let client = new AmazonRDSClient()
            let request = DescribeDBClustersRequest()
            let! response =
                client.DescribeDBClustersAsync(request)
                |> Async.AwaitTask
            return response.DBClusters
        }
        |> Async.StartAsTask

module main =
    open DatabaseUtils.DatabaseMetrics

    [<EntryPoint>]
    let main argv =
        async {
            let cloudwatchClient = new AmazonCloudWatchClient()

            let database =
                DBCluster(DBClusterIdentifier = "mutedac-cluster-v6msuwf7t7ti")
                |> DatabaseUtils.Database.DBCluster

            let! connectionCount =
                GetDatabaseQueryCountFromLastHour(database, cloudwatchClient)
                |> Async.AwaitTask

            printfn "%f" connectionCount
            return 0
        }
        |> Async.RunSynchronously
