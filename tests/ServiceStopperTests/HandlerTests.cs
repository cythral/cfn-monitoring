using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using NSubstitute;
using FluentAssertions;
using NUnit.Framework;
using Amazon.ECS.Model;

using Task = System.Threading.Tasks.Task;

using Cythral.CloudFormation.Monitoring.ServiceStopper;
using Cythral.CloudFormation.Monitoring.ServiceStopper.ServiceUtils;

namespace Cythral.CloudFormation.Monitoring.Tests.ServiceStopper
{
    public class HandlerTests
    {
        ServiceListerFactory serviceListerFactory = null!;
        ServiceScaler serviceScaler = null!;
        ServiceLister serviceLister = null!;

        static Service StoppableService = new Service {
            Deployments = new List<Deployment> {
                new Deployment {
                    UpdatedAt = DateTime.Now.AddHours(-2)
                }
            }
        };

        static Service UnstoppableService = new Service {
            Deployments = new List<Deployment> {
                new Deployment {
                    UpdatedAt = DateTime.Now.AddMinutes(-30)
                }
            }
        };

        Request CreateRequest(string monitoredClusterGroupName)
        {
            return new Request
            {
                MonitoredClustersGroupName = monitoredClusterGroupName
            };
        }
        
        [SetUp]
        public void SetupServiceLister()
        {
            serviceLister = Substitute.For<ServiceLister>("");
            serviceListerFactory = Substitute.For<ServiceListerFactory>();
            serviceListerFactory.Create(Arg.Any<string>()).Returns(serviceLister);

            serviceLister
            .List()
            .Returns(new List<Service> {
                StoppableService,
                UnstoppableService
            });

            ReflectionUtils.SetPrivateStaticField(typeof(Handler), "serviceListerFactory", serviceListerFactory);
        }

        [SetUp]
        public void SetupServiceScaler()
        {
            serviceScaler = Substitute.For<ServiceScaler>();
            ReflectionUtils.SetPrivateStaticField(typeof(Handler), "serviceScaler", serviceScaler);
        }

        [Test]
        public async Task ServiceListerIsCreated()
        {
            var groupName = "group";
            var request = CreateRequest(groupName);
            await Handler.Handle(request);

            serviceListerFactory.Received().Create(Arg.Is<string>(groupName));
        }

        [Test]
        public async Task ServicesAreListed()
        {
            var groupName = "group";
            var request = CreateRequest(groupName);
            await Handler.Handle(request);

            await serviceLister.Received().List();
        }

        [Test]
        public async Task ServicesUpdatedOverAnHourAgoShouldBeStopped()
        {
            var groupName = "group";
            var request = CreateRequest(groupName);
            await Handler.Handle(request);

            await serviceScaler.Received().ScaleDown(StoppableService);
        }

        [Test]
        public async Task ServicesUpdatedLessThanAnHourAgoShouldNotBeStopped()
        {
            var groupName = "group";
            var request = CreateRequest(groupName);
            await Handler.Handle(request);

            await serviceScaler.DidNotReceive().ScaleDown(UnstoppableService);
        }
    }
}