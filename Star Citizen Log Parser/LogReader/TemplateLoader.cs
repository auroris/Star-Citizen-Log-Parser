using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Star_Citizen_Log_Parser.LogReader
{
    public static class TemplateLoader
    {
        public static List<LogTemplate> LoadFromYaml(string path)
        {
            var yaml = File.ReadAllText(path);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var templates = deserializer.Deserialize<List<LogTemplate>>(yaml);
            templates.ForEach(template => { template.Init(); });
            return templates;
        }
    }
}
