namespace Cythral.CloudFormation.Monitoring.Tests.DatabaseStopper.DatabaseUtils


open Cythral.CloudFormation.Monitoring.DatabaseStopper.Aws
open Cythral.CloudFormation.Monitoring.DatabaseStopper.DatabaseUtils

open Amazon.ResourceGroups
open Amazon.ResourceGroups.Model
open NUnit.Framework
open FsUnit

module DatabaseListerTests =
    open Cythral.CloudFormation.Monitoring.Tests
    open DatabaseLister

    // [<SetUp>]
    // let Setup () =
    //     ()

    [<Test>]
    let Test1() =
        DatabaseLister.CreateResourceGroupsClient = {  }

        Assert.Pass()
