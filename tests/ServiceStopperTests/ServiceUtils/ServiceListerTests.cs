using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.ECS;
using Amazon.ECS.Model;
using Amazon.ResourceGroups;
using Amazon.ResourceGroups.Model;

using Cythral.CloudFormation.Monitoring.ServiceStopper.Aws;
using Cythral.CloudFormation.Monitoring.ServiceStopper.ServiceUtils;

using FluentAssertions;

using NSubstitute;

using NUnit.Framework;

using Task = System.Threading.Tasks.Task;

namespace Cythral.CloudFormation.Monitoring.Tests.ServiceStopper.ServiceUtils
{
    public class ServiceListerTests
    {
        string groupName = "groupName";
        ServiceLister lister = null!;
        ResourceGroupsFactory resourceGroupsFactory = null!;
        EcsFactory ecsFactory = null!;
        IAmazonECS ecsClient = null!;
        IAmazonResourceGroups resourceGroupsClient = null!;

        static string Cluster1Arn = "cluster 1";
        static string Cluster2Arn = "cluster 2";
        static List<string> ClusterArns = new List<string> { Cluster1Arn, Cluster2Arn };
        static List<string> Cluster1ServiceArns = new List<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" };
        static List<Service> Cluster1Services = Cluster1ServiceArns.Select(arn => new Service { ServiceArn = arn }).ToList();
        static List<string> Cluster2ServiceArns = new List<string> { "13", "14" };
        static List<Service> Cluster2Services = Cluster2ServiceArns.Select(arn => new Service { ServiceArn = arn }).ToList();
        static List<Service> Services = Cluster1Services.Concat(Cluster2Services).ToList();
        static Dictionary<string, List<string>> ServiceArns = new Dictionary<string, List<string>>
        {
            [Cluster1Arn] = Cluster1ServiceArns,
            [Cluster2Arn] = Cluster2ServiceArns,
        };

        [SetUp]
        public void Setup()
        {
            lister = new ServiceLister(groupName);
        }

        [SetUp]
        public void SetupResourceGroups()
        {
            resourceGroupsClient = Substitute.For<IAmazonResourceGroups>();
            resourceGroupsClient
            .ListGroupResourcesAsync(Arg.Any<ListGroupResourcesRequest>())
            .Returns(new ListGroupResourcesResponse
            {
                Resources = ClusterArns.Select(arn => new ListGroupResourcesItem
                {
                    Identifier = new ResourceIdentifier { ResourceArn = arn }
                }).ToList()
            });

            resourceGroupsFactory = Substitute.For<ResourceGroupsFactory>();
            resourceGroupsFactory.Create().Returns(resourceGroupsClient);
            ReflectionUtils.SetPrivateField(lister, "resourceGroupsFactory", resourceGroupsFactory);
        }

        [SetUp]
        public void SetupEcs()
        {
            ecsClient = Substitute.For<IAmazonECS>();

            ecsClient
            .ListServicesAsync(Arg.Is<ListServicesRequest>(req => req.Cluster == Cluster1Arn))
            .Returns(new ListServicesResponse
            {
                ServiceArns = Cluster1ServiceArns
            });

            ecsClient
            .ListServicesAsync(Arg.Is<ListServicesRequest>(req => req.Cluster == Cluster2Arn))
            .Returns(new ListServicesResponse
            {
                ServiceArns = Cluster2ServiceArns
            });

            ecsClient
            .DescribeServicesAsync(Arg.Is<DescribeServicesRequest>(req => req.Cluster == Cluster1Arn))
            .Returns(client => new DescribeServicesResponse
            {
                Services = Cluster1Services.FindAll(service =>
                    (client.ArgAt<DescribeServicesRequest>(0)).Services.Contains(service.ServiceArn)
                )
            });

            ecsClient
            .DescribeServicesAsync(
                Arg.Is<DescribeServicesRequest>(arg => arg.Cluster == Cluster2Arn)
            )
            .Returns(client => new DescribeServicesResponse
            {
                Services = Cluster2Services.FindAll(service =>
                    (client.ArgAt<DescribeServicesRequest>(0)).Services.Contains(service.ServiceArn)
                )
            });

            ecsFactory = Substitute.For<EcsFactory>();
            ecsFactory.Create().Returns(ecsClient);
            ReflectionUtils.SetPrivateField(lister, "ecsFactory", ecsFactory);
        }


        [Test]
        public async Task ListGroupResourcesAsyncWasCalled()
        {
            await lister.List();

            await resourceGroupsClient.Received().ListGroupResourcesAsync(Arg.Is<ListGroupResourcesRequest>(req =>
                req.Group == groupName &&
                req.Filters.Any(filter => filter.Name == ResourceFilterName.ResourceType && filter.Values.Contains("AWS::ECS::Cluster"))
            ));
        }

        [Test]
        public async Task ListServicesWasCalledForCluster([ValueSource("ClusterArns")] string clusterArn)
        {
            await lister.List();

            await ecsClient.Received().ListServicesAsync(Arg.Is<ListServicesRequest>(req =>
                req.Cluster == clusterArn
            ));
        }

        [Test]
        public async Task DescribeServicesWasCalledForCluster1Services()
        {
            await lister.List();

            await ecsClient.Received().DescribeServicesAsync(Arg.Is<DescribeServicesRequest>(req =>
                req.Cluster == Cluster1Arn &&
                Cluster1ServiceArns.Take(10).All(req.Services.Contains)
            ));

            await ecsClient.Received().DescribeServicesAsync(Arg.Is<DescribeServicesRequest>(req =>
                req.Cluster == Cluster1Arn &&
                Cluster1ServiceArns.Skip(10).All(req.Services.Contains)
            ));
        }

        [Test]
        public async Task DescribeServicesWasCalledForCluster2Services()
        {
            await lister.List();

            await ecsClient.Received().DescribeServicesAsync(Arg.Is<DescribeServicesRequest>(req =>
                req.Cluster == Cluster2Arn &&
                Cluster2ServiceArns.All(req.Services.Contains)
            ));
        }

        [Test]
        public async Task ServicesWereReturned([ValueSource("Services")] Service service)
        {
            var result = await lister.List();
            result.Should().Contain(s => s.ServiceArn == service.ServiceArn);
        }
    }
}