using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Samples.InstrumentedTests.Iast.Vulnerabilities.JsonTainting;

internal class PersonForTest
{
    public string Name { get; set; }
    public int Age { get; set; }
    public PersonForTest Parent { get; set; }
    public PersonForTest[] Relatives { get; set; }
}

internal class PersonJsonConverter: JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(PersonForTest);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);
        var person = new PersonForTest();
        person.Name = jObject["name"].Value<string>();
        person.Age = jObject["age"].Value<int>();
        person.Parent = jObject["parent"].ToObject<PersonForTest>();
        person.Relatives = jObject["relatives"].ToObject < PersonForTest[]>();
        return person;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var person = (PersonForTest)value;
        var jObject = new JObject
        {
            { "name", person.Name },
            { "age", person.Age },
            { "parent", JToken.FromObject(person.Parent, serializer) },
            { "relatives", JToken.FromObject(person.Relatives, serializer) }
        };
        jObject.WriteTo(writer);
    }
}

public class NewtonsoftTests : InstrumentationTestsBase
{
    protected string taintedJson = "{\r\n  \"name\": \"John Smith\",\r\n  \"age\": 35,\r\n  \"parent\": {\r\n    \"name\": \"Alice Smith\",\r\n    \"age\": 60\r\n  },\r\n  \"relatives\": [\r\n    {\r\n      \"name\": \"Jane Smith\",\r\n      \"relation\": \"Spouse\",\r\n      \"age\": 34\r\n    },\r\n    {\r\n      \"name\": \"Michael Smith\",\r\n      \"relation\": \"Child\",\r\n      \"age\": 10\r\n    },\r\n    {\r\n      \"name\": \"Emily Smith\",\r\n      \"relation\": \"Child\",\r\n      \"age\": 8\r\n    },\r\n    {\r\n      \"name\": \"Robert Smith\",\r\n      \"relation\": \"Sibling\",\r\n      \"age\": 40\r\n    }\r\n  ]\r\n}";
    protected string untaintedJson = "{\r\n  \"name\": \"Peter Smith\",\r\n  \"age\": 35,\r\n  \"parent\": {\r\n    \"name\": \"Alice Smith\",\r\n    \"age\": 60\r\n  },\r\n  \"relatives\": [\r\n    {\r\n      \"name\": \"Jane Smith\",\r\n      \"relation\": \"Spouse\",\r\n      \"age\": 34\r\n    },\r\n    {\r\n      \"name\": \"Michael Smith\",\r\n      \"relation\": \"Child\",\r\n      \"age\": 10\r\n    },\r\n    {\r\n      \"name\": \"Emily Smith\",\r\n      \"relation\": \"Child\",\r\n      \"age\": 8\r\n    },\r\n    {\r\n      \"name\": \"Robert Smith\",\r\n      \"relation\": \"Sibling\",\r\n      \"age\": 40\r\n    }\r\n  ]\r\n}";

    public NewtonsoftTests()
    {
        AddTainted(taintedJson);
    }

    [Fact]
    public void GivenATaintedJson_WhenDeserializing_ResultIsTainted4()
    {
        var person = (JsonConvert.DeserializeObject(taintedJson) as JObject).ToObject(typeof(PersonForTest)) as PersonForTest;
        AssertPersonIsTainted(person);
    }

    [Fact]
    public void GivenATaintedJson_WhenDeserializing_ResultIsTainted3()
    {
        var person = (JsonConvert.DeserializeObject(taintedJson, new JsonSerializerSettings()) as JObject).ToObject(typeof(PersonForTest)) as PersonForTest;
        AssertPersonIsTainted(person);
    }

    [Fact]
    public void GivenATaintedJson_WhenDeserializing_ResultIsTainted2()
    {
        var person = JsonConvert.DeserializeObject(taintedJson, typeof(PersonForTest)) as PersonForTest;
        AssertPersonIsTainted(person);
    }

    [Fact]
    public void GivenATaintedJson_WhenDeserializing_ResultIsTainted()
    {
        var person = JsonConvert.DeserializeObject<PersonForTest>(taintedJson);
        AssertPersonIsTainted(person);
    }

    [Fact]
    public void GivenATaintedJson_WhenDeserializing_ResultIsTainted5()
    {
        var person = JsonConvert.DeserializeObject<PersonForTest>(taintedJson, new JsonSerializerSettings());
        AssertPersonIsTainted(person);
    }

    [Fact]
    public void GivenATaintedJson_WhenDeserializing_ResultIsTainted7()
    {
        var person = JsonConvert.DeserializeObject(taintedJson, typeof(PersonForTest), [new PersonJsonConverter()]) as PersonForTest;
        AssertPersonIsTainted(person);
    }

    [Fact]
    public void GivenATaintedJson_WhenDeserializing_ResultIsTainted9()
    {
        var person = JsonConvert.DeserializeObject<PersonForTest>(taintedJson, [new PersonJsonConverter()]) as PersonForTest;
        AssertPersonIsTainted(person);
    }

    [Fact]
    public void GivenATaintedJson_WhenDeserializing_ResultIsTainted6()
    {
        var person = JsonConvert.DeserializeObject(taintedJson, typeof(PersonForTest), new JsonSerializerSettings()) as PersonForTest;
        AssertPersonIsTainted(person);
    }

    [Fact]
    public void GivenATaintedJson_WhenDeserializing_ResultIsTainted8()
    {
        var person = JsonConvert.DeserializeObject<PersonForTest>(taintedJson, new PersonJsonConverter()) as PersonForTest;
        AssertPersonIsTainted(person);
    }

    [Fact]
    public void GivenATaintedJson_WhenDeserializing_ResultIsNotTainted()
    {
        var person = JsonConvert.DeserializeObject(untaintedJson) as PersonForTest;
        AssertNotTainted(person);
    }

    [Fact]
    public void GivenATaintedJson_WhenDeserializing_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => JsonConvert.DeserializeObject(null) as PersonForTest);
    }

    [Fact]
    public void GivenATaintedJson_WhenDeserializing_JsonReaderException()
    {
        Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject("invalid json") as PersonForTest);
    }

    private void AssertPersonIsTainted(PersonForTest person)
    {
        AssertTaintedFormatWithOriginalCallCheck(":+-John Smith-+:", person.Name, () => person.Name);
        AssertTaintedFormatWithOriginalCallCheck(":+-Alice Smith-+:", person.Parent.Name, () => person.Parent.Name);
        AssertTaintedFormatWithOriginalCallCheck(":+-Jane Smith-+:", person.Relatives[0].Name, () => person.Relatives[0].Name);
        AssertTaintedFormatWithOriginalCallCheck(":+-Michael Smith-+:", person.Relatives[1].Name, () => person.Relatives[1].Name);
        AssertTaintedFormatWithOriginalCallCheck(":+-Emily Smith-+:", person.Relatives[2].Name, () => person.Relatives[2].Name);
        AssertTaintedFormatWithOriginalCallCheck(":+-Robert Smith-+:", person.Relatives[3].Name, () => person.Relatives[3].Name);
    }
}
