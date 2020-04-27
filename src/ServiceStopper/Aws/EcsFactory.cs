using Amazon.ECS;

namespace Cythral.CloudFormation.Monitoring.ServiceStopper.Aws
{
    public class EcsFactory
    {
        public virtual IAmazonECS Create()
        {
            return new AmazonECSClient();
        }
    }
}