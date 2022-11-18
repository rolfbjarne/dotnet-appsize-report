
using System.Diagnostics.Metrics;
using Mono.Cecil;

public class ConsoleUI {
	AppSizeReport report;
	public ConsoleUI (AppSizeReport report)
	{
		this.report = report;
	}

	public int Run ()
	{
		while (true) {
			Console.Clear ();
			Console.WriteLine ($"App size report for {report.Input}");
			Console.WriteLine ($"");
			Console.WriteLine ($"1) View {report.Assemblies.Count} assemblies");
			Console.WriteLine ($"2) View {report.Namespaces.Count} namespaces");
			Console.WriteLine ($"3) View {report.Types.Count} types");
			Console.WriteLine ($"q) Quit");
			Console.WriteLine ();

			var keepAsking = false;
			do {
				Console.Write ($"Enter option: ");
				var line = Console.ReadLine ()!.Trim ();
				switch (line) {
				case "1":
					RunAssemblies ();
					break;
				case "2":
					RunNamespaces ();
					break;
				case "3":
					RunTypes (report.Types, string.Empty);
					break;
				case "q":
				case "quit":
					return 0;
				case null:
				default:
					Console.WriteLine ($"Unknown option: {line}");
					keepAsking = true;
					break;
				}
			} while (keepAsking);
		}
	}

	const string cellSeparator = "  ";
	void PrintTable (IList<(string, string, string)> rows)
	{
		var maxSize = new int [4];
		for (var i = 0; i < rows.Count; i++) {
			var row = rows [i];
			maxSize [1] = Math.Max (maxSize [1], row.Item1?.Length ?? 0);
			maxSize [2] = Math.Max (maxSize [2], row.Item2?.Length ?? 0);
			maxSize [3] = Math.Max (maxSize [3], row.Item3?.Length ?? 0);
		}
		maxSize [0] = rows.Count.ToString ().Length;

		var sb = new StringBuilder ();
		for (var i = 0; i < rows.Count; i++) {
			var row = rows [i];
			if (i > 0) {
				var number = i.ToString ();
				sb.Append (' ', maxSize [0] - number.Length);
				sb.Append (number);
				sb.Append (")");
			} else {
				sb.Append (' ', maxSize [0]);
			}
			sb.Append (cellSeparator);

			sb.Append (row.Item1);
			sb.Append (' ', maxSize [1] - (row.Item1?.Length ?? 0));
			sb.Append (cellSeparator);

			sb.Append (' ', maxSize [2] - (row.Item2?.Length ?? 0));
			sb.Append (row.Item2);
			sb.Append (cellSeparator);

			sb.Append (' ', maxSize [3] - (row.Item3?.Length ?? 0));
			sb.Append (row.Item3);
			sb.AppendLine ();
		}
		Console.Write (sb.ToString ());
	}

	void PrintTable (IList<(string, string, string, string)> rows)
	{
		var maxSize = new int [5];
		for (var i = 0; i < rows.Count; i++) {
			var row = rows [i];
			maxSize [1] = Math.Max (maxSize [1], row.Item1?.Length ?? 0);
			maxSize [2] = Math.Max (maxSize [2], row.Item2?.Length ?? 0);
			maxSize [3] = Math.Max (maxSize [3], row.Item3?.Length ?? 0);
			maxSize [4] = Math.Max (maxSize [4], row.Item4?.Length ?? 0);
		}
		maxSize [0] = rows.Count.ToString ().Length;

		var sb = new StringBuilder ();
		for (var i = 0; i < rows.Count; i++) {
			var row = rows [i];
			if (i > 0) {
				var number = i.ToString ();
				sb.Append (' ', maxSize [0] - number.Length);
				sb.Append (number);
				sb.Append (")");
			} else {
				sb.Append (' ', maxSize [0]);
			}
			sb.Append (cellSeparator);

			sb.Append (row.Item1);
			sb.Append (' ', maxSize [1] - (row.Item1?.Length ?? 0));
			sb.Append (cellSeparator);

			sb.Append (' ', maxSize [2] - (row.Item2?.Length ?? 0));
			sb.Append (row.Item2);
			sb.Append (cellSeparator);

			sb.Append (' ', maxSize [3] - (row.Item3?.Length ?? 0));
			sb.Append (row.Item3);
			sb.Append (cellSeparator);

			sb.Append (' ', maxSize [4] - (row.Item4?.Length ?? 0));
			sb.Append (row.Item4);
			sb.AppendLine ();
		}
		Console.Write (sb.ToString ());
	}

	void PrintTable(IList<(string, string, string, string, string)> rows)
	{
		var rowCount = 5;
		var maxSize = new int[rowCount + 1];
		for (var i = 0; i < rows.Count; i++)
		{
			var row = rows[i];
			var ituple = (System.Runtime.CompilerServices.ITuple)row;
			for (var k = 0; k < rowCount; k++)
				maxSize[k + 1] = Math.Max(maxSize[k + 1], ((string?)ituple [k])?.Length ?? 0);
		}
		maxSize[0] = rows.Count.ToString().Length;

		var sb = new StringBuilder();
		for (var i = 0; i < rows.Count; i++)
		{
			var row = rows[i];
			if (i > 0)
			{
				var number = i.ToString();
				sb.Append(' ', maxSize[0] - number.Length);
				sb.Append(number);
				sb.Append(")");
			}
			else
			{
				sb.Append(' ', maxSize[0]);
			}
			sb.Append(cellSeparator);

			var ituple = (System.Runtime.CompilerServices.ITuple)row;
			for (var k = 0; k < rowCount; k++)
			{
				var rowValue = (string?)ituple[k];
				sb.Append(rowValue);
				sb.Append(' ', maxSize[k + 1] - (rowValue?.Length ?? 0));
				if (k < rowCount - 1)
					sb.Append(cellSeparator);
			}
			sb.AppendLine();
		}
		Console.Write(sb.ToString());
	}

	public void RunAssemblies ()
	{
		ReportSortMode sortMode = ReportSortMode.ByName;

		while (true) {
			Console.Clear ();
			Console.WriteLine ($"Assembly size report for {report.Input}");
			Console.WriteLine ($"");

			var sorted = report.Assemblies.Sort (sortMode).ToArray ();
			var table = sorted.Select<AssemblyInfo, (string, string, string, string)> (v => new (v.Assembly.Name.Name, v.Types.Count.ToString ("n0"), v.ILSize.ToString ("n0"), v.Size.ToString ("n0") + " bytes")).ToList ();
			table.Insert (0, ("Assembly", "Types", "IL Size", "File size"));
			PrintTable (table);

			Console.WriteLine ($"");
			Console.WriteLine ($"a) Sort by name{(sortMode == ReportSortMode.ByName ? " [current mode]" : string.Empty)}");
			Console.WriteLine ($"b) Sort by type count{(sortMode == ReportSortMode.ByCount ? " [current mode]" : string.Empty)}");
			Console.WriteLine ($"c) Sort by IL size{(sortMode == ReportSortMode.ByILSize ? " [current mode]" : string.Empty)}");
			Console.WriteLine ($"q) Quit");
			Console.WriteLine ();

			OptionResponse<string> stringResponse = (string line, out bool keepLooping) => {
				keepLooping = false;
				switch (line) {
				case "a":
					sortMode = ReportSortMode.ByName;
					return true;
				case "b":
					sortMode = ReportSortMode.ByCount;
					return true;
				case "c":
					sortMode = ReportSortMode.ByILSize;
					return true;
				default:
					return false;
				}
			};

			OptionResponse<AssemblyInfo> numberResponse = (AssemblyInfo entry, out bool keepLooping) => {
				RunAssembly (entry);
				keepLooping = false;
				return true;
			};

			if (!GetOption (stringResponse, numberResponse, sorted))
				return;
		}
	}

	public void RunNamespaces ()
	{
		ReportSortMode sortMode = ReportSortMode.ByName;

		while (true) {
			Console.Clear ();
			Console.WriteLine ($"Namespace size report for {report.Input}");
			Console.WriteLine ($"");

			var sorted = report.Namespaces.Sort (sortMode).ToArray ();
			var table = sorted.Select<NamespaceInfo, (string, string, string)> (v => new (v.Namespace, v.Types.Count.ToString (), v.ILSize.ToString ())).ToList ();
			table.Insert (0, ("Namespace", "Types", "IL Size"));
			PrintTable (table);

			Console.WriteLine ($"");
			Console.WriteLine ($"a) Sort by name{(sortMode == ReportSortMode.ByName ? " [current mode]" : string.Empty)}");
			Console.WriteLine ($"b) Sort by type count{(sortMode == ReportSortMode.ByCount ? " [current mode]" : string.Empty)}");
			Console.WriteLine ($"c) Sort by IL size{(sortMode == ReportSortMode.ByILSize ? " [current mode]" : string.Empty)}");
			Console.WriteLine ($"q) Quit");
			Console.WriteLine ();

			OptionResponse<string> stringResponse = (string line, out bool keepLooping) => {
				keepLooping = false;
				switch (line) {
				case "a":
					sortMode = ReportSortMode.ByName;
					return true;
				case "b":
					sortMode = ReportSortMode.ByCount;
					return true;
				case "c":
					sortMode = ReportSortMode.ByILSize;
					return true;
				default:
					return false;
				}
			};

			OptionResponse<NamespaceInfo> numberResponse = (NamespaceInfo entry, out bool keepLooping) => {
				RunTypes (entry.Types, $"namespace: {entry.Namespace}");
				keepLooping = false;
				return true;
			};

			if (!GetOption (stringResponse, numberResponse, sorted))
				return;
		}
	}

	public void RunAssembly (AssemblyInfo info)
	{
		var sortMode = ReportSortMode.ByName;
		var enableAssemblyDrilling = false;
		var enableTypeDrilling = false;

		while (true) {
			Console.Clear();
			Console.WriteLine($"Assembly size report for {report.Input}: {info.Assembly.Name.Name}");
			Console.WriteLine($"");

			var ad = info.Assembly;
			var module = ad.MainModule;

			if (module.AssemblyReferences.Count == 0) {
				Console.WriteLine($"This assembly references no other assemblies");
			} else {
				Console.WriteLine($"This assembly references {module.AssemblyReferences.Count} other assemblies:");
				foreach (var ar in module.AssemblyReferences.OrderBy(v => v.FullName))
					Console.WriteLine($"    {ar.Name}");
			}
			Console.WriteLine();

			var types = info.Types;
			var sorted = types.Sort(sortMode).ToArray();

			var referencedByOtherAssemblies = report.Assemblies.
				Where(v => v.Assembly.MainModule.AssemblyReferences.Any(v => v.FullName == info.Assembly.FullName)).
				Select(v => v.Assembly).
				ToArray();
			if (referencedByOtherAssemblies.Length == 0) {
				Console.WriteLine($"This assembly is not referenced by any other assembly");
			} else {
				Console.WriteLine($"This assembly is referenced by {referencedByOtherAssemblies.Length} other assemblies:");
				foreach (var ar in referencedByOtherAssemblies.OrderBy(v => v.FullName))
					Console.WriteLine($"    {ar.Name}");
			}
			Console.WriteLine();

			Console.WriteLine("Types in this assembly:");
			var typeReferencesInAssemblyMembers = new List<(TypeInfo, IEnumerable<IMemberDefinition>)>();

			var table = sorted
				.Select ((v) => {
					AssemblyInfos? assembliesWithTypes = null;
					IEnumerable<IMemberDefinition>? memberReferences = null;
					if (enableAssemblyDrilling || enableTypeDrilling)
					{
						assembliesWithTypes = report.FindTypeReferencesInAssemblies(v.Type);
						if (enableTypeDrilling)
						{
							memberReferences = report.FindMemberReferencesInOtherAssemblies(v);
							if (memberReferences.Count () > 0)
								typeReferencesInAssemblyMembers.Add((v, memberReferences));
						}
					}
					return (
						(string)v.Type.FullName,
						v.Members.Count.ToString(),
						v.ILSize.ToString(),
						(assembliesWithTypes?.Count.ToString() ?? "<assembly drilling not enabled>"),
						(enableTypeDrilling ? memberReferences?.Count().ToString() : "<type drilling not enabled>")!
					);
				})
				.ToList ();

			table.Insert (0, ("Type", "Members", "IL Size", "Type references in other assemblies", "Member references in other assemblies"));
			PrintTable (table);

			if (typeReferencesInAssemblyMembers.Count > 0)
			{
				Console.WriteLine();
				Console.WriteLine("References to types in other assemblies:");
				foreach (var entry in typeReferencesInAssemblyMembers)
				{
					var grouped = entry.Item2.GroupBy(v => v.DeclaringType.Module.Assembly);
					Console.WriteLine($"    Type {entry.Item1.Type.FullName} is referenced in:");
					foreach (var group in grouped)
					{
						Console.WriteLine($"        {group.Key.ToString()}");
						foreach (var member in group)
						{
							Console.WriteLine($"            {member.ToString()}");
						}
					}
				}
			}

			Console.WriteLine ($"");
			Console.WriteLine ($"a) Sort types by name{(sortMode == ReportSortMode.ByName ? " [current mode]" : string.Empty)}");
			Console.WriteLine ($"b) Sort types by member count{(sortMode == ReportSortMode.ByCount ? " [current mode]" : string.Empty)}");
			Console.WriteLine ($"c) Sort types by IL size{(sortMode == ReportSortMode.ByILSize ? " [current mode]" : string.Empty)}");
			Console.WriteLine ($"e) {(enableAssemblyDrilling ? "Disable" : "Enable")} assembly drilling");
			Console.WriteLine ($"f) {(enableTypeDrilling ? "Disable" : "Enable")} type drilling");
			Console.WriteLine ($"q) Quit");
			Console.WriteLine ();

			OptionResponse<string> stringResponse = (string line, out bool keepLooping) => {
				keepLooping = false;
				switch (line) {
				case "a":
					sortMode = ReportSortMode.ByName;
					return true;
				case "b":
					sortMode = ReportSortMode.ByCount;
					return true;
				case "c":
					sortMode = ReportSortMode.ByILSize;
					return true;
				case "e":
					enableAssemblyDrilling = !enableAssemblyDrilling;
					return true;
				case "f":
					enableTypeDrilling = !enableTypeDrilling;
					return true;
				default:
					return false;
				}
			};

			OptionResponse<TypeInfo> numberResponse = (TypeInfo entry, out bool keepLooping) => {
				RunType (entry, $" assembly: {ad.Name.Name}");
				keepLooping = false;
				return true;
			};

			if (!GetOption (stringResponse, numberResponse, sorted))
				return;
		}
	}

	public void RunTypes (TypeInfos types, string container)
	{
		ReportSortMode sortMode = ReportSortMode.ByName;

		while (true) {
			Console.Clear ();
			Console.WriteLine ($"Type size report for {report.Input}: {container}");
			Console.WriteLine ($"");

			var sorted = types.Sort (sortMode).ToArray ();
			var table = sorted.Select<TypeInfo, (string, string, string, string)> (v => new (v.Type.FullName, v.Members.Count.ToString (), v.ILSize.ToString (), report.FindTypeReferencesInAssemblies (v.Type).Count.ToString ())).ToList ();
			table.Insert (0, ("Type", "Members", "IL Size", "References in other assemblies"));
			PrintTable (table);

			Console.WriteLine ($"");
			Console.WriteLine ($"a) Sort by name{(sortMode == ReportSortMode.ByName ? " [current mode]" : string.Empty)}");
			Console.WriteLine ($"b) Sort by member count{(sortMode == ReportSortMode.ByCount ? " [current mode]" : string.Empty)}");
			Console.WriteLine ($"c) Sort by IL size{(sortMode == ReportSortMode.ByILSize ? " [current mode]" : string.Empty)}");
			Console.WriteLine ($"q) Quit");
			Console.WriteLine ();

			OptionResponse<string> stringResponse = (string line, out bool keepLooping) => {
				keepLooping = false;
				switch (line) {
				case "a":
					sortMode = ReportSortMode.ByName;
					return true;
				case "b":
					sortMode = ReportSortMode.ByCount;
					return true;
				case "c":
					sortMode = ReportSortMode.ByILSize;
					return true;
				default:
					return false;
				}
			};

			OptionResponse<TypeInfo> numberResponse = (TypeInfo entry, out bool keepLooping) => {
				RunType (entry, container + $" type: {entry.Type.FullName}");
				keepLooping = false;
				return true;
			};

			if (!GetOption (stringResponse, numberResponse, sorted))
				return;
		}
	}

	public void RunType (TypeInfo type, string container)
	{
		ReportSortMode sortMode = ReportSortMode.ByName;

		while (true) {
			Console.Clear ();
			Console.WriteLine ($"Type size report for {report.Input}: {container} type: {type.Type.FullName}");
			Console.WriteLine ($"");

			var referencedByOtherAssemblies = report.FindTypeReferencesInAssemblies (type.Type).ToArray ();
			if (referencedByOtherAssemblies.Length == 0) {
				Console.WriteLine ($"This type is not referenced in any other assembly");
			} else {
				Console.WriteLine ($"This type is referenced in {referencedByOtherAssemblies.Length} other assemblies:");
				foreach (var ar in referencedByOtherAssemblies.OrderBy (v => v.Assembly.Name.Name)) {
					var referencedIn = new List<IMemberDefinition> ();
					ar.FindMemberReferences (type.Type, referencedIn);
					Console.WriteLine ($"    {ar.Assembly.Name.Name}: referenced in {referencedIn.Count} member(s):");
					var maxMembers = 25;
					foreach (var ri in referencedIn.Take (maxMembers)) {
						Console.WriteLine ($"        {ri.FullName}");
					}
					if (referencedIn.Count > maxMembers)
						Console.WriteLine ($"        ... skipped {referencedIn.Count - maxMembers} members ...");
				}
			}
			Console.WriteLine ();


			var sorted = type.Members.Sort (sortMode).ToArray ();
			var table = sorted.Select<MemberInfo, (string, string, string, string)> (v => new (v.Member.FullName, v.Member.GetType ().Name, v.ILSize.ToString (), report.FindMemberReferencesInOtherAssemblies (v.Member).Count ().ToString ())).ToList ();
			if (table.Count > 0) {
				table.Insert (0, ("Member", "Member type", "IL Size", "References in other assemblies"));
				PrintTable (table);
			} else {
				Console.WriteLine ("This type has no members");
			}

			Console.WriteLine ($"");
			if (table.Count > 0) {
				Console.WriteLine ($"a) Sort by name{(sortMode == ReportSortMode.ByName ? " [current mode]" : string.Empty)}");
				Console.WriteLine ($"c) Sort by IL size{(sortMode == ReportSortMode.ByILSize ? " [current mode]" : string.Empty)}");
			}
			Console.WriteLine ($"q) Quit");
			Console.WriteLine ();

			OptionResponse<string> stringResponse = (string line, out bool keepLooping) => {
				keepLooping = false;
				switch (line) {
				case "a":
					sortMode = ReportSortMode.ByName;
					return true;
				case "b":
					sortMode = ReportSortMode.ByCount;
					return true;
				case "c":
					sortMode = ReportSortMode.ByILSize;
					return true;
				default:
					return false;
				}
			};

			OptionResponse<MemberInfo> numberResponse = (MemberInfo entry, out bool keepLooping) => {
				RunMember (entry, container + $" type: {type.Type.FullName}");
				keepLooping = false;
				return true;
			};

			if (!GetOption (stringResponse, numberResponse, sorted))
				return;
		}
	}

	public void RunMember (MemberInfo member, string container)
	{
		var printIL = false;
		while (true) {
			Console.Clear ();
			Console.WriteLine ($"Member size report for {report.Input}: {container}");
			Console.WriteLine ($"");

			Console.WriteLine ($"Member: {member.Member.FullName}");
			Console.WriteLine ($"ILSize: {member.ILSize}");
			Console.WriteLine ();

			var references = report.FindMemberReferencesInOtherAssemblies (member.Member).Sort (ReportSortMode.ByName).ToArray ();
			if (references.Length == 0) {
				Console.WriteLine ($"Not referenced by other assemblies.");
			} else {
				Console.WriteLine ($"Referenced by {references.Length} assemblies:");
				foreach (var r in references) {
					var referencedIn = new List<IMemberDefinition> ();
					r.FindMemberReferences (member.Member, referencedIn);
					Console.WriteLine ($"    {r.Assembly.Name.Name}: referenced in {referencedIn.Count} member(s):");
					var maxMembers = 25;
					foreach (var ri in referencedIn.Take (maxMembers)) {
						Console.WriteLine ($"        {ri.FullName}");
					}
					if (referencedIn.Count > maxMembers)
						Console.WriteLine ($"        ... skipped {referencedIn.Count - maxMembers} members ...");
				}
			}
			Console.WriteLine ($"");

			if (printIL) {
				var body = (member.Member as MethodDefinition)?.Body;
				if (body is not null) {
					Console.WriteLine ("IL:");
					foreach (var instr in body.Instructions)
						Console.WriteLine ($"    {instr}");
				} else {
					Console.WriteLine ("This member has no IL.");
				}
				Console.WriteLine ();
			}

			Console.WriteLine ($"");
			Console.WriteLine ($"i) {(printIL ? "Hide" : "Print")} IL");
			Console.WriteLine ($"q) Quit");
			Console.WriteLine ();

			OptionResponse<string> stringResponse = (string line, out bool keepLooping) => {
				keepLooping = false;
				switch (line) {
				case "i":
					printIL = !printIL;
					return true;
				default:
					return false;
				}
			};

			if (!GetOption (stringResponse))
				return;
		}
	}

	// returns false if quitting
	public bool GetOption (OptionResponse<string> stringResponse)
	{
		return GetOption<object> (stringResponse, null, null);
	}

	// returns false if quitting
	public bool GetOption<T> (OptionResponse<string> stringResponse, OptionResponse<T>? numberResponse, IList<T>? list)
	{
		do {
			Console.Write ($"Enter option: ");
			var line = Console.ReadLine ()!.Trim ();
			switch (line) {
			case "r":
			case "reload":
				return true;
			case "q":
			case "quit":
				return false;
			case "":
			case null:
				continue;
			default:
				bool keepLooping;
				if (list is not null && numberResponse != null && int.TryParse (line, out var entry)) {
					if (entry < 1 || entry > list.Count) {
						Console.WriteLine ($"Unknown option: {line}");
						continue;
					}

					if (!numberResponse (list [entry - 1], out keepLooping)) {
						Console.WriteLine ($"Unknown option: {line}");
						continue;
					}
						
				} else {
					if (!stringResponse (line, out keepLooping)) {
						Console.WriteLine ($"Unknown option: {line}");
						continue;
					}
				}
				if (!keepLooping)
					return true;
				break;
			}
		} while (true);
	}

	public delegate bool OptionResponse<T> (T option, out bool keepLooping);
}

