string controllersPath = Path.Combine(projectPath, "Controllers");
            Directory.CreateDirectory(controllersPath);

            var files = Directory.EnumerateFiles(projectPath, "*.aspx.cs", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                GenerateController(file, controllersPath);
            }


public static class ControllerGenerator
{
   public static void GenerateController(string filePath, string controllersPath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath).Replace(".aspx", "", StringComparison.OrdinalIgnoreCase);
        string controllerBaseName = fileName.Replace(".cs","");
        string controllerName = controllerBaseName.Pluralize() + "Controller";

        var content = File.ReadAllText(filePath);
        var methodRegex = new Regex(@"(public|private|protected|internal)\s+(static\s+)?async\s+Task\s+(?<name>\S+)\s*\((?<params>[^\)]*)\)\s*{", RegexOptions.IgnoreCase);
        var matches = methodRegex.Matches(content);

        StringBuilder controller = new StringBuilder();
        controller.AppendLine("using Microsoft.AspNetCore.Mvc;");
        controller.AppendLine("using Microsoft.Extensions.Caching.Memory;");
        controller.AppendLine("using Microsoft.Extensions.Logging;");
        controller.AppendLine();
        controller.AppendLine("namespace YourNamespace.Controllers");
        controller.AppendLine("{");
        controller.AppendLine($"    [Route(\"api/[controller]\")]");
        controller.AppendLine("    [ApiController]");
        controller.AppendLine($"    public class {controllerName}(ILogger<{controllerName}> logger,");
        controller.AppendLine($"                                 I{controllerBaseName}Service {controllerBaseName.ToLower()}Service,");
        controller.AppendLine($"                                 IMemoryCache cache,");
        controller.AppendLine($"                                 IAMSRepository dapperRepository)");
        controller.AppendLine("        : BaseApiController");
        controller.AppendLine("    {");

        foreach (Match m in matches)
        {
            // Check for [AjaxMethod] before method
            string beforeMethod = content.Substring(0, m.Index);
            int lookBackStart = Math.Max(0, m.Index - 500);
            string lookBack = content.Substring(lookBackStart, m.Index - lookBackStart);

            if (!Regex.IsMatch(lookBack, @"\[AjaxMethod\]", RegexOptions.IgnoreCase))
            {
                continue; // Skip if not AjaxMethod
            }

            string methodName = m.Groups["name"].Value;
            string paramList = m.Groups["params"].Value.Trim();
            string[] paramParts = paramList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string parameters = string.Join(", ", paramParts.Select(p => p.Trim()));

            string httpVerb = "HttpGet";
            if (methodName.StartsWith("Post", StringComparison.OrdinalIgnoreCase)) httpVerb = "HttpPost";
            else if (methodName.StartsWith("Put", StringComparison.OrdinalIgnoreCase)) httpVerb = "HttpPut";
            else if (methodName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase)) httpVerb = "HttpDelete";

            controller.AppendLine($"        [{httpVerb}(\"{methodName}\")]");
            controller.AppendLine("        [ProducesResponseType(StatusCodes.Status200OK)]");
            controller.AppendLine("        [ProducesResponseType(StatusCodes.Status400BadRequest)]");
            controller.AppendLine("        [ProducesResponseType(StatusCodes.Status500InternalServerError)]");
            controller.AppendLine($"        public async Task<IActionResult> {methodName}({parameters})");
            controller.AppendLine("        {");

            foreach (var p in paramParts)
            {
                var parts = p.Trim().Split(' ');
                if (parts.Length == 2 && parts[0] == "string")
                {
                    string pname = parts[1];
                    controller.AppendLine($"            if (string.IsNullOrEmpty({pname}))");
                    controller.AppendLine("            {");
                    controller.AppendLine($"                return BadRequest(\"{pname} is required.\");");
                    controller.AppendLine("            }");
                    controller.AppendLine();
                }
            }

            // Extract method body
            int bodyStart = m.Index + m.Length - 1;
            int level = 1;
            int i = bodyStart + 1;
            while (i < content.Length && level > 0)
            {
                if (content[i] == '{') level++;
                else if (content[i] == '}') level--;
                i++;
            }

            if (i > bodyStart)
            {
                var body = content.Substring(bodyStart + 1, i - bodyStart - 2);
                foreach (var line in body.Split('\n'))
                {
                    controller.AppendLine("            " + line.Trim());
                }
                controller.AppendLine();
            }

            controller.AppendLine($"            var result = await {controllerBaseName.ToLower()}Service.{methodName}();");
            controller.AppendLine("            return Ok(result);");
            controller.AppendLine("        }");
            controller.AppendLine();
        }

        controller.AppendLine("    }");
        controller.AppendLine("}");

        string outPath = Path.Combine(controllersPath, $"{controllerName}.cs");
        File.WriteAllText(outPath, controller.ToString());

        Console.WriteLine($"Generated: {outPath}");
    }
}
