namespace Configuration.Abstractions
{
    public interface IConfigManager
    {
        IConfigSource Settings { get; set; }
        IConfigSource Secrets { get; set; }
    }
}