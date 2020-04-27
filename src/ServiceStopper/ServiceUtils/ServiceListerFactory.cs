namespace Cythral.CloudFormation.Monitoring.ServiceStopper.ServiceUtils
{
    public class ServiceListerFactory
    {
        public virtual ServiceLister Create(string monitoredClustersGroupName)
        {
            return new ServiceLister(monitoredClustersGroupName);
        }
    }
}