namespace Cythral.CloudFormation.Monitoring.DatabaseStopper.DatabaseUtils

open Amazon.RDS
open Amazon.RDS.Model

type Database =
    | DBInstance of DBInstance
    | DBCluster of DBCluster
