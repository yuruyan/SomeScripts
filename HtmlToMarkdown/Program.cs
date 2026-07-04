if (args.Length < 2) {
    Console.Error.WriteLine("Usage: HtmlToMarkdown <inputFile> <outputFile>");
    return;
}

try {
    var converter = new ReverseMarkdown.Converter();

    string html = File.ReadAllText(args[0]);
    string result = converter.Convert(html);

    File.WriteAllText(args[1], result);
} catch (FileNotFoundException ex) {
    Console.Error.WriteLine($"Error: File not found — {ex.FileName}");
    Environment.Exit(1);
} catch (UnauthorizedAccessException) {
    Console.Error.WriteLine($"Error: Access denied — {args[0]} or {args[1]}");
    Environment.Exit(1);
} catch (Exception ex) {
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}
