namespace Cythral.CloudFormation.Monitoring.DatabaseStopper.DatabaseUtils


open System
open System.Collections.Generic

open Amazon.CloudWatch
open Amazon.CloudWatch.Model

module DatabaseMetrics =

    let GetDatabaseConnectionCountFromLastHour (database: Database, cloudWatchClient: IAmazonCloudWatch) =
        async {
            let dimension =
                match database with
                | DBCluster cluster -> Dimension(Name = "DBClusterIdentifier", Value = cluster.DBClusterIdentifier)
                | DBInstance instance -> Dimension(Name = "DBInstanceIdentifier", Value = instance.DBInstanceIdentifier)

            let period = TimeSpan.FromHours(1.0)
            let startTime = DateTime.Now - period
            let endTime = DateTime.Now

            let request =
                GetMetricStatisticsRequest
                    (Namespace = "AWS/RDS",
                     MetricName = "DatabaseConnections",
                     Dimensions = List<Dimension> [ dimension ],
                     StartTimeUtc = startTime,
                     EndTimeUtc = endTime,
                     Period = int period.TotalSeconds,
                     Statistics = List<string> [ "Sum" ])

            let! response =
                cloudWatchClient.GetMetricStatisticsAsync(request)
                |> Async.AwaitTask

            return if response.Datapoints.Count > 0 then response.Datapoints.[0].Sum else 0.0
        }
        |> Async.StartAsTask

    let GetDatabaseUptimeLastHour (database: Database, cloudWatchClient: IAmazonCloudWatch) =
        async {
            let dimension =
                match database with
                | DBCluster cluster -> Dimension(Name = "DBClusterIdentifier", Value = cluster.DBClusterIdentifier)
                | DBInstance instance -> Dimension(Name = "DBInstanceIdentifier", Value = instance.DBInstanceIdentifier)

            let period = TimeSpan.FromHours(1.0)
            let startTime = DateTime.Now - period
            let endTime = DateTime.Now

            let request =
                GetMetricStatisticsRequest
                    (Namespace = "AWS/RDS",
                     MetricName = "EngineUptime",
                     Dimensions = List<Dimension> [ dimension ],
                     StartTimeUtc = startTime,
                     EndTimeUtc = endTime,
                     Period = int period.TotalSeconds,
                     Unit = StandardUnit.Seconds,
                     Statistics = List<string> [ "Maximum" ])

            let! response =
                cloudWatchClient.GetMetricStatisticsAsync(request)
                |> Async.AwaitTask

            return if response.Datapoints.Count > 0 then response.Datapoints.[0].Maximum else 0.0
        }
        |> Async.StartAsTask
