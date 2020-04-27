using Amazon.ResourceGroups;

namespace Cythral.CloudFormation.Monitoring.ServiceStopper.Aws
{
    public class ResourceGroupsFactory
    {
        public virtual IAmazonResourceGroups Create()
        {
            return new AmazonResourceGroupsClient();
        }
    }
}