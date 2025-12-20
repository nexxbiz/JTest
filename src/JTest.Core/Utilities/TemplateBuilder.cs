using System.Text.Json;

namespace JTest.Core.Utilities;

public static class TemplateBuilder
{
    public static string CreateTestTemplate(string testName)
    {
        var template = new
        {
            name = testName,
            steps = new object[]
            {
                new
                {
                    type = "http",
                    method = "GET",
                    url = "{{$.env.baseUrl}}/api/endpoint",
                    save = new { response = "{{$.this.body}}" },
                    assert = new object[]
                    {
                        new { equals = new { actual = "{{$.this.status}}", expected = 200 } }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(template, JsonSerializerOptionsCache.Default);
    }
}
