using Mono.Options;

string output = string.Empty;
string input = string.Empty;

var options = new OptionSet {
	{ "o|output=", (v) => output = v },
	{ "i|input=", (v) => input = v },
};

var left = options.Parse (args);
if (left.Any ()) {
	Console.Error.WriteLine ("Unexpected command-line arguments:");
	foreach (var l in left)
		Console.Error.WriteLine ("    " + l);
	return 1;
}

var report = new AppSizeReport (input, output);
report.Load ();

return new ConsoleUI (report).Run ();
