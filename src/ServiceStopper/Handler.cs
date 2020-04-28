using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Cythral.CloudFormation.Monitoring.ServiceStopper.ServiceUtils;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Cythral.CloudFormation.Monitoring.ServiceStopper
{
    public class Handler
    {
        private static ServiceListerFactory serviceListerFactory = new ServiceListerFactory();
        private static ServiceScaler serviceScaler = new ServiceScaler();

        public static async Task Handle(Request request, ILambdaContext? context = null)
        {
            var serviceLister = serviceListerFactory.Create(request.MonitoredClustersGroupName);
            var services = await serviceLister.List();
            var tasks = new List<Task>();

            foreach (var service in services)
            {
                var updatedAt = service.Deployments[0].UpdatedAt;
                var diff = DateTime.Now - updatedAt;

                if (diff.Hours == 0)
                {
                    continue;
                }

                var task = serviceScaler.ScaleDown(service);
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
        }
    }
}