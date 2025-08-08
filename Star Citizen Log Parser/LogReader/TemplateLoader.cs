using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

namespace Star_Citizen_Log_Parser.LogReader
{
    public static class TemplateLoader
    {
        public static List<LogTemplate> LoadFromYaml(string path)
        {
            var yaml = File.ReadAllText(path);
            return LoadFromYamlContent(yaml);
        }

        public static List<LogTemplate> LoadFromYamlContent(string yaml)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var templates = deserializer.Deserialize<List<LogTemplate>>(yaml);
            templates.ForEach(t => t.Init());
            return templates;
        }
    }
}