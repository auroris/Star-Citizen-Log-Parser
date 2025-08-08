using Star_Citizen_Log_Parser.LogReader;

namespace Tests
{
    [TestClass]
    public class Complex_Test
    {
        [TestMethod]
        public void Template_Matches_ContextEstablisher_Model_State_Change()
        {
            string yaml = @"
- id: context-establisher-state-change
  label: ContextEstablisher Model State Change
  tags: [replication, context, model, state-change, network, loading, persistence]
  template: ""<ContextEstablisher Model Change State> The Model is changing state meaning views must update their model state. oldState={old_state} newState={new_state} localState={local_state} remoteState={remote_state} modelState={model_state} ViewName=\""{view}\"" connection={{connection_a}, {connection_b}} node_id={node_id} playerGEID={player_geid} sessionId=\""{session_id}\""""
";

            string logLine = @"[Notice] <ContextEstablisher Model Change State> The Model is changing state meaning views must update their model state. oldState=11 newState=12 localState=13 remoteState=0 modelState=11 ViewName=""GameClient <local>:16"" connection={2, 1} node_id=00000000-0000-0000-0000-00000000e999 playerGEID=201990709661 sessionId=""b42779d5860da28ff910aaa9db2746b1""";

            var templates = TemplateLoader.LoadFromYamlContent(yaml);
            var template = templates[0];

            var (regex, tokens) = LogTemplate.CompileTemplate(template.Template); // use your CompileTemplate method
            var match = regex.Match(logLine);

            Assert.IsTrue(match.Success, "Regex should match log line");

            Assert.AreEqual("11", match.Groups["old_state"].Value);
            Assert.AreEqual("12", match.Groups["new_state"].Value);
            Assert.AreEqual("13", match.Groups["local_state"].Value);
            Assert.AreEqual("0", match.Groups["remote_state"].Value);
            Assert.AreEqual("11", match.Groups["model_state"].Value);
            Assert.AreEqual("GameClient <local>:16", match.Groups["view"].Value);
            Assert.AreEqual("2", match.Groups["connection_a"].Value);
            Assert.AreEqual("1", match.Groups["connection_b"].Value);
            Assert.AreEqual("00000000-0000-0000-0000-00000000e999", match.Groups["node_id"].Value);
            Assert.AreEqual("201990709661", match.Groups["player_geid"].Value);
            Assert.AreEqual("b42779d5860da28ff910aaa9db2746b1", match.Groups["session_id"].Value);
        }

        [TestMethod]
        public void Template_Matches_HaulingPickCreated()
        {
            string yaml = @"
- id: hauling-pick-created
  label: Hauling Pick Created
  tags: [mission, hauling, objective]
  template: ""[Notice] <CreateHaulingObjectiveHandler> Pick created - [Cient] sourcename: {sourcename}, missionId: {mission_id}, locationName: {location_name}, locationHash: {location_hash}, locationSuperGUID: {location_guid}, objectiveId: {objective_id}, objectiveTokenDebugName: {token_debug_name}, itemGuid: {item_guid}""";

            string logLine = @"[Notice] <CreateHaulingObjectiveHandler> Pick created - [Cient] sourcename: HaulCargo_OpenDelivery, missionId: 00000000-0000-0000-0000-000000000000, locationName: UNKNOWN LOCATION NAME, locationHash: 0, locationSuperGUID: , objectiveId: , objectiveTokenDebugName: Cargo Hauling, itemGuid: a789f57a-e12b-4bcd-8132-e0c03d84fc89";

            var templates = TemplateLoader.LoadFromYamlContent(yaml);
            var template = templates[0];

            var (regex, tokens) = LogTemplate.CompileTemplate(template.Template); // use your CompileTemplate method
            var match = regex.Match(logLine);

            Assert.IsTrue(match.Success, "Regex should match log line");
        }
    }
}
