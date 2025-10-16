namespace Fake4Dataverse.Abstractions 
{
    public interface IXrmRealContext: IXrmBaseContext 
    {
        string ConnectionStringName { get; set; }
    }
}