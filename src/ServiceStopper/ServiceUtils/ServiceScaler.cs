using Amazon.ECS.Model;

using Cythral.CloudFormation.Monitoring.ServiceStopper.Aws;
using ThreadingTask = System.Threading.Tasks.Task;

namespace Cythral.CloudFormation.Monitoring.ServiceStopper.ServiceUtils
{
    public class ServiceScaler
    {
        private EcsFactory ecsFactory = new EcsFactory();

        public virtual ThreadingTask ScaleDown(Service service)
        {
            var client = ecsFactory.Create();
            return client.UpdateServiceAsync(new UpdateServiceRequest
            {
                Cluster = service.ClusterArn,
                Service = service.ServiceArn,
                DesiredCount = 0
            });
        }
    }
}