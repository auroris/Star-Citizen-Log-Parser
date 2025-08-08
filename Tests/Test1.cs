using Star_Citizen_Log_Parser.LogReader;

namespace Tests
{
    [TestClass]
    public class TemplateLoaderTests
    {
        [TestMethod]
        public void CanLoadSimpleTemplate()
        {
            string yaml = @"
- id: simple
  label: Simple
  tags: [test]
  template: ""foo='{bar}'""
";
            var templates = TemplateLoader.LoadFromYamlContent(yaml);

            Assert.AreEqual(1, templates.Count);
            Assert.AreEqual("simple", templates[0].Id);
        }
    }
}
