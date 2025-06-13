 NumberOfLines = File.ReadAllLines(file).Length

  var methodRegex = new Regex(@"(public|private|protected|internal)\s+(static\s+)?\S+\s+(?<name>\S+)\s*\(.*?\)\s*{");
                var ajaxRegex = new Regex(@"\[AjaxMethod\]", RegexOptions.IgnoreCase);
                var paramRegex = new Regex(@"Request\.(QueryString|Params|Form|Cookies|ServerVariables)", RegexOptions.IgnoreCase);

                var methodMatches = methodRegex.Matches(content);

                foreach (Match m in methodMatches)
                {
                    string methodName = m.Groups["name"].Value;

                    var beforeMethod = content.Substring(0, m.Index);
                    bool isAjax = ajaxRegex.Matches(beforeMethod).Count > 0;

                    bool usesParam = false;
                    int numLines = 0;

                    int braceIndex = content.IndexOf('{', m.Index);
                    if (braceIndex >= 0)
                    {
                        int level = 1;
                        int i = braceIndex + 1;
                        while (i < content.Length && level > 0)
                        {
                            if (content[i] == '{') level++;
                            else if (content[i] == '}') level--;
                            i++;
                        }

                        if (i > braceIndex)
                        {
                            var methodBody = content.Substring(braceIndex, i - braceIndex);
                            usesParam = paramRegex.IsMatch(methodBody);
                            numLines = methodBody.Split('\n').Length;
                        }
                    }

                    methodDetails.Add(new MethodDetail
                    {
                        FileName = Path.GetFileName(file),
                        MethodName = methodName,
                        IsAjaxMethod = isAjax ? "Yes" : "No",
                        UsesParameter = usesParam ? "Yes" : "No",
                        NumberOfLines = numLines
                    });
                }
            }
