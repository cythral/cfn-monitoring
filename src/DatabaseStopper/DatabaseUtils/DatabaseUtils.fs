namespace Cythral.CloudFormation.Monitoring.DatabaseStopper

open Amazon.RDS
open Amazon.RDS.Model

module DatabaseUtils =
    type Database =
        | DBInstance of DBInstance
        | DBCluster of DBCluster
