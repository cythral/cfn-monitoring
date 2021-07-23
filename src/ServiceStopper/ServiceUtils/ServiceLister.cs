using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.ECS;
using Amazon.ECS.Model;
using Amazon.ResourceGroups;
using Amazon.ResourceGroups.Model;

using Cythral.CloudFormation.Monitoring.ServiceStopper.Aws;

using Task = System.Threading.Tasks.Task;

namespace Cythral.CloudFormation.Monitoring.ServiceStopper.ServiceUtils
{
    public class ServiceLister
    {
        private ResourceGroupsFactory resourceGroupsFactory = new ResourceGroupsFactory();
        private EcsFactory ecsFactory = new EcsFactory();

        private string monitoredClustersGroupName;

        public ServiceLister(string monitoredClustersGroupName)
        {
            this.monitoredClustersGroupName = monitoredClustersGroupName;
        }

        public virtual async Task<List<Service>> List()
        {
            var clusterArns = await GetClusterArns();
            var clusterToServiceArns = GetClusterToServiceArnsDict(clusterArns);
            return GetServices(clusterToServiceArns);
        }

        private async Task<List<string>> GetClusterArns()
        {
            var client = resourceGroupsFactory.Create();
            var listGroupResourcesResponse = await client.ListGroupResourcesAsync(new ListGroupResourcesRequest
            {
                Group = monitoredClustersGroupName,
                Filters = new List<ResourceFilter> {
                    new ResourceFilter {
                        Name = ResourceFilterName.ResourceType,
                        Values = new List<string> { "AWS::ECS::Cluster" }
                    }
                }
            });

            return listGroupResourcesResponse.Resources.Select(resource => resource.Identifier.ResourceArn).ToList();
        }

        private Dictionary<string, List<string>> GetClusterToServiceArnsDict(List<string> clusterArns)
        {
            var result = new Dictionary<string, List<string>>();
            var tasks = new List<Task>();

            foreach (var clusterArn in clusterArns)
            {
                var task = Task.Run(async delegate
                {
                    var serviceArns = await GetServiceArnsForCluster(clusterArn);

                    lock (result)
                    {
                        result.Add(clusterArn, serviceArns);
                    }
                });

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
            return result;
        }

        private async Task<List<string>> GetServiceArnsForCluster(string clusterArn)
        {
            var client = ecsFactory.Create();
            var response = await client.ListServicesAsync(new ListServicesRequest
            {
                Cluster = clusterArn
            });

            return response?.ServiceArns ?? new List<string>();
        }

        private List<Service> GetServices(Dictionary<string, List<string>> clusterToServiceArns)
        {
            var results = new List<Service>();
            var tasks = new List<Task>();

            foreach (var entry in clusterToServiceArns)
            {
                var task = Task.Run(async delegate
                {
                    var services = await GetServicesForCluster(entry.Key, entry.Value);

                    lock (results)
                    {
                        results.AddRange(services);
                    }
                });

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
            return results;
        }

        private async Task<List<Service>> GetServicesForCluster(string clusterArn, List<string> serviceArns)
        {
            var ecsClient = ecsFactory.Create();
            var services = new List<Service>();

            for (int i = 0; i < serviceArns.Count; i += 10)
            {
                var serviceArnsSlice = serviceArns.Skip(i).Take(10).ToList();
                var response = await ecsClient.DescribeServicesAsync(new DescribeServicesRequest
                {
                    Cluster = clusterArn,
                    Services = serviceArnsSlice,
                });

                services.AddRange(response?.Services ?? new List<Service>());
            }

            return services;
        }
    }
}