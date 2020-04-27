using System;
using System.Threading.Tasks;
using NSubstitute;
using FluentAssertions;
using NUnit.Framework;
using Amazon.ECS;
using Amazon.ECS.Model;
using Cythral.CloudFormation.Monitoring.ServiceStopper.Aws;
using Cythral.CloudFormation.Monitoring.ServiceStopper.ServiceUtils;

using Task = System.Threading.Tasks.Task;

namespace Cythral.CloudFormation.Monitoring.Tests.ServiceStopper.ServiceUtils
{
    public class ServiceScalerTests
    {
        ServiceScaler serviceScaler = null!;
        EcsFactory ecsFactory = null!;
        IAmazonECS ecsClient = null!;

        [SetUp]
        public void SetupTaskStopper()
        {
            serviceScaler = new ServiceScaler();
        }

        [SetUp]
        public void SetupEcs()
        {
            ecsClient = Substitute.For<IAmazonECS>();
            ecsFactory = Substitute.For<EcsFactory>();
            ecsFactory.Create().Returns(ecsClient);
            ReflectionUtils.SetPrivateField(serviceScaler, "ecsFactory", ecsFactory);
        }

        public Service CreateService(string cluster, string arn)
        {
            return new Service
            {
                ClusterArn = cluster,
                ServiceArn = arn
            };
        }

        [Test]
        public async Task ScaleDownShouldCreateClient()
        {
            var serviceArn = "service";
            var clusterArn = "cluster";
            var service = CreateService(clusterArn, serviceArn);

            await serviceScaler.ScaleDown(service);

            ecsFactory.Received().Create();
        }

        [Test]
        public async Task ScaleDownShouldUpdateService()
        {
            var serviceArn = "service";
            var clusterArn = "cluster";
            var service = CreateService(clusterArn, serviceArn);

            await serviceScaler.ScaleDown(service);

            await ecsClient.Received().UpdateServiceAsync(Arg.Is<UpdateServiceRequest>(arg => 
                arg.Cluster == clusterArn && 
                arg.Service == serviceArn &&
                arg.DesiredCount == 0
            ));
        }
    }
}