namespace Cythral.CloudFormation.Monitoring.DatabaseStopper.Aws

open System
open System.Threading
open System.Threading.Tasks

open Amazon.ResourceGroups
open Amazon.ResourceGroups.Model

type ResourceGroupsFactory() =
    member this.Create(): IAmazonResourceGroups = upcast new AmazonResourceGroupsClient()
