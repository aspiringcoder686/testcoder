var methodRegex = new Regex(
    @"(public|private|protected|internal)\s+(static\s+)?(async\s+)?\S+\s+(?<name>\w+)\s*\((?<params>[^\)]*)\)\s*{",
    RegexOptions.IgnoreCase);
