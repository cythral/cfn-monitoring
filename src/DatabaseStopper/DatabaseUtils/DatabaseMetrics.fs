namespace Cythral.CloudFormation.Monitoring.DatabaseStopper.DatabaseUtils

open System
open System.Collections.Generic

open Amazon.CloudWatch
open Amazon.CloudWatch.Model

module DatabaseMetrics =
    let GetDatabaseQueryCountFromLastHour (database: Database, cloudWatchClient: IAmazonCloudWatch) =
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
                     StartTime = startTime,
                     EndTime = endTime,
                     Period = int period.TotalSeconds,
                     Statistics = List<string> [ "Sum" ])

            let! response =
                cloudWatchClient.GetMetricStatisticsAsync(request)
                |> Async.AwaitTask

            return response.Datapoints.[0].Sum
        }
        |> Async.StartAsTask
