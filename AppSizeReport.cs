global using System;
global using System.IO;
global using System.Text;
using System.Diagnostics.Metrics;
using Mono.Cecil;

class Resolver : DefaultAssemblyResolver {
	public override AssemblyDefinition Resolve (AssemblyNameReference name)
	{
		var rv = base.Resolve (name);
		// Console.WriteLine ($"Resolve ({name.FullName}) => {rv.FullName}");
		return rv;
	}

	public override AssemblyDefinition Resolve (AssemblyNameReference name, ReaderParameters parameters)
	{
		var rv = base.Resolve (name, parameters);
		// Console.WriteLine ($"Resolve ({name.FullName}, {parameters}) => {rv.FullName}");
		RegisterAssembly (rv);
		return rv;
	}

	public new void RegisterAssembly (AssemblyDefinition ad)
	{
		base.RegisterAssembly (ad);
	}
}

public class AppSizeReport {
	public string Input { get; private set; }
	public string Output { get; private set; }

	public AssemblyInfos Assemblies { get; private set; } = new AssemblyInfos ();
	public NamespaceInfos Namespaces { get; private set; } = new NamespaceInfos ();
	public TypeInfos Types { get; private set; } = new TypeInfos ();

	public AppSizeReport (string input, string output)
	{
		Input = input;
		Output = output;
	}

	public void Load ()
	{
		var assemblies = Directory.GetFileSystemEntries (Input, "*", SearchOption.AllDirectories).Where (v => v.EndsWith (".dll", StringComparison.OrdinalIgnoreCase) || v.EndsWith ("*.exe", StringComparison.OrdinalIgnoreCase)).ToArray ();

		var ro = new ReaderParameters (ReadingMode.Deferred);

		var resolver = new Resolver ();
		foreach (var dir in assemblies.Select (v => Path.GetDirectoryName (v)!).Distinct ())
			resolver.AddSearchDirectory (dir);
		ro.AssemblyResolver = resolver;

		foreach (var assembly in assemblies) {
			var ad = AssemblyDefinition.ReadAssembly (assembly, ro);
			resolver.RegisterAssembly (ad);
			Assemblies.Add (new AssemblyInfo (ad, assembly));
		}

		var byNamespace = Assemblies.SelectMany (v => v.Types).GroupBy (v => {
			var vt = v.Type;
			while (vt.IsNested)
				vt = vt.DeclaringType;
			return vt.Namespace;
		});
		foreach (var ns in byNamespace) {
			Namespaces.Add (new NamespaceInfo (ns.Key, ns));
		}
		Types.AddRange (Assemblies.SelectMany (v => v.Types));
	}

	public void Create (ReportSortMode sortMode, string path)
	{
		var sb = new StringBuilder ();
		var writer = new StringWriter (sb);
		Assemblies.WriteReport (writer, sortMode);
		writer.Flush ();
		File.WriteAllText (path, sb.ToString ());
		Console.WriteLine (sb.ToString ());
	}

	public static string EscapeMarkdown (string text)
	{
		return text.
			Replace ("<", "&lt;").
			Replace (">", "&gt;");
	}

	public AssemblyInfos FindTypeReferencesInAssemblies(IEnumerable<TypeDefinition> types)
	{
		var rv = new AssemblyInfos();
		foreach (var type in types)
			FindTypeReferencesInAssemblies(type, rv);
		return rv;
	}

	public AssemblyInfos FindTypeReferencesInAssemblies (TypeDefinition type)
	{
		var rv = new AssemblyInfos ();
		FindTypeReferencesInAssemblies(type, rv);
		return rv;
	}

	public void FindTypeReferencesInAssemblies(TypeDefinition type, AssemblyInfos infos)
	{
		foreach (var assembly in Assemblies)
		{
			var references = assembly.Assembly.MainModule.GetTypeReferences();
			foreach (var r in references)
			{
				try
				{
					var rDef = r.Resolve();
					if (rDef == type)
					{
						infos.Add(assembly);
						break;
					}
				}
				catch (Exception e)
				{
					Console.Error.WriteLine(e.Message);
				}
			}
		}
	}

	public IEnumerable<IMemberDefinition> FindMemberReferencesInOtherAssemblies(TypeInfo type)
	{
		var rv = new List<IMemberDefinition> ();
		var members = type.Members;
		foreach (var assembly in Assemblies)
		{
			var references = assembly.Assembly.MainModule.GetMemberReferences();
			foreach (var r in references)
			{
				try
				{
					var rDef = r.Resolve();
					if (members.Any (v => v.Member == rDef))
					{
						assembly.FindMemberReferences(rDef, rv);
						break;
					}
				}
				catch (Exception e)
				{
					Console.Error.WriteLine(e.Message);
				}
			}
		}
		return rv;
	}
	public AssemblyInfos FindMemberReferencesInOtherAssemblies (IMemberDefinition member)
	{
		var rv = new AssemblyInfos ();
		foreach (var assembly in Assemblies) {
			var references = assembly.Assembly.MainModule.GetMemberReferences ();
			foreach (var r in references) {
				try {
					var rDef = r.Resolve ();
					if (rDef == member) {
						rv.Add (assembly);
						break;
					}
				} catch (Exception e) {
					Console.Error.WriteLine (e.Message);
				}
			}
		}
		return rv;
	}
}

public enum ReportSortMode {
	ByName,
	ByILSize,
	ByCount,
}

public class AssemblyInfos : List<AssemblyInfo> {
	public void WriteReport (TextWriter writer, ReportSortMode sortMode)
	{
		IEnumerable<AssemblyInfo> sorted = Sort (sortMode);
		writer.WriteLine ("# Assemblies");
		writer.WriteLine ();
		writer.WriteLine ($"|Assembly|TypeCount|MemberCount|ILSize|");
		writer.WriteLine ($"|--------|--------:|----------:|-----:|");
		foreach (var info in sorted) {
			writer.WriteLine ($"|{AppSizeReport.EscapeMarkdown (info.Assembly.Name.Name)}|{info.Types.Count}|{info.Types.Sum (v => v.Members.Count)}|{info.ILSize}|");
		}
		writer.WriteLine ();

		writer.WriteLine ("# Types per assembly");
		writer.WriteLine ();
		foreach (var info in sorted)
			info.WriteReport (writer, sortMode);
	}

	int? ilsize = null;
	public int ILSize {
		get {
			if (ilsize is null)
				ilsize = this.Sum (v => v.ILSize);
			return ilsize.Value;
		}
	}

	public IEnumerable<AssemblyInfo> Sort (ReportSortMode sortMode)
	{
		switch (sortMode) {
		case ReportSortMode.ByName:
			return this.OrderBy (v => v.Assembly.FullName);
		case ReportSortMode.ByCount:
			return this.OrderBy (v => v.Types.Count);
		case ReportSortMode.ByILSize:
			return this.OrderBy (v => v.ILSize);
		default:
			throw new NotImplementedException (sortMode.ToString ());
		}
	}
}

public class AssemblyInfo {
	public AssemblyDefinition Assembly;
	public string Path;

	public long Size;
	public TypeInfos Types = new TypeInfos ();

	int? ilsize = null;
	public int ILSize {
		get {
			if (ilsize is null)
				ilsize = Types.Sum (v => v.ILSize);
			return ilsize.Value;
		}
	}

	public AssemblyInfo (AssemblyDefinition assembly, string path)
	{
		Assembly = assembly;
		Path = path;

		Size = new FileInfo (path).Length;

		if (assembly.MainModule.HasTypes)
			AddTypes (assembly.MainModule.Types);
	}

	void AddTypes (IEnumerable<TypeDefinition> types)
	{
		foreach (var type in types) {
			Types.Add (new TypeInfo (type));
			if (type.HasNestedTypes) {
				AddTypes (type.NestedTypes);
			}
		}
	}

	public void FindMemberReferences (TypeDefinition reference, List<IMemberDefinition> referencedIn)
	{
		var references = Assembly.MainModule.GetTypeReferences ();
		foreach (var r in references) {
			var rDef = r.Resolve ();
			if (rDef == reference) {
				FindMemberReferences (r, referencedIn);
				break;
			}
		}
	}

	public void FindMemberReferences (IMemberDefinition reference, List<IMemberDefinition> referencedIn)
	{
		var references = Assembly.MainModule.GetMemberReferences ();
		foreach (var r in references) {
			var rDef = r.Resolve ();
			if (rDef == reference) {
				FindMemberReferences (r, referencedIn);
				break;
			}
		}
	}

	public void FindMemberReferences (MemberReference reference, List<IMemberDefinition> referencedIn)
	{
		var md = Assembly.MainModule;
		if (!md.HasTypes)
			return;

		FindMemberReferences (reference, referencedIn, md.Types);
	}

	void FindMemberReferences (MemberReference reference, List<IMemberDefinition> referencedIn, IEnumerable<TypeDefinition> types)
	{
		foreach (var type in types)
			FindMemberReferences (reference, referencedIn, type);
	}

	void FindMemberReferences (MemberReference reference, List<IMemberDefinition> referencedIn, TypeDefinition type)
	{
		if (type.HasNestedTypes)
			FindMemberReferences (reference, referencedIn, type.NestedTypes);

		if (ContainsReference (type.BaseType, reference)) {
			referencedIn.Add (type);
		} else if (type.HasInterfaces) {
			foreach (var ti in type.Interfaces)
				if (ContainsReference (ti.InterfaceType, reference))
					referencedIn.Add (type);
		}

		if (type.HasMethods) {
			foreach (var method in type.Methods)
				if (ContainsReference (method, reference))
					referencedIn.Add (method);
		}
	}

	bool ContainsReference (MethodDefinition method, MemberReference reference)
	{
		if (method.ReturnType == reference)
			return true;

		if (method.HasParameters) {
			foreach (var param in method.Parameters) {
				if (ContainsReference (param.ParameterType, reference))
					return true;
			}
		}

		if (!method.HasBody)
			return false;

		foreach (var il in method.Body.Instructions) {
			if (il.Operand == reference)
				return true;
			if (il.Operand is TypeReference tr && ContainsReference (tr, reference))
				return true;
		}

		return false;
	}

	bool ContainsReference (TypeReference typeReference, MemberReference reference, int depth = 0)
	{
		if (typeReference is null)
			return false;

		if (typeReference == reference)
			return true;

		if (depth >= 20) {
			Console.WriteLine ($"STOP {typeReference}          {reference}");
			return false;
		}

		if (typeReference is TypeSpecification ts) {
			if (ContainsReference (ts.GetElementType (), reference, depth + 1))
				return true;
		}

		if (typeReference.ContainsGenericParameter) {
			foreach (var t in typeReference.GenericParameters)
				if (ContainsReference (t, reference, depth + 1))
					return true;
		}

		if (typeReference is GenericInstanceType git) {
			if (git.HasGenericArguments) {
				foreach (var t in git.GenericArguments) {
					if (t is GenericParameter)
						continue;

					if (ContainsReference (t, reference, depth + 1))
						return true;
				}
			}
		}

		if (typeReference is GenericParameter gp) {
			if (gp.HasConstraints) {
				foreach (var constraint in gp.Constraints)
					if (ContainsReference (constraint.ConstraintType, reference, depth + 1))
						return true;
			}
		}

		return false;
	}

	public void WriteReport (TextWriter writer, ReportSortMode sortMode)
	{
		IEnumerable<TypeInfo> sorted;
		switch (sortMode) {
		case ReportSortMode.ByName:
			sorted = Types.OrderBy (v => v.Type.FullName);
			break;
		case ReportSortMode.ByCount:
			sorted = Types.OrderBy (v => v.Members.Count);
			break;
		case ReportSortMode.ByILSize:
			sorted = Types.OrderBy (v => v.ILSize);
			break;
		default:
			throw new NotImplementedException (sortMode.ToString ());
		}

		writer.WriteLine ($"## {Assembly.Name.Name}");
		writer.WriteLine ();
		writer.WriteLine ($"Type count: {Types.Count}");
		writer.WriteLine ($"ILSize: {ILSize}");
		writer.WriteLine ();

		writer.WriteLine ($"|Type|MemberCount|ILSize|");
		writer.WriteLine ($"|----|----------:|-----:|");
		foreach (var info in sorted)
			writer.WriteLine ($"{AppSizeReport.EscapeMarkdown (info.Type.FullName)}|{info.Members.Count}|{info.ILSize}|");
		writer.WriteLine ();
	}
}

public class TypeInfos : List<TypeInfo> {
	public IEnumerable<TypeInfo> Sort (ReportSortMode sortMode)
	{
		switch (sortMode) {
		case ReportSortMode.ByName:
			return this.OrderBy (v => v.Type.FullName);
		case ReportSortMode.ByCount:
			return this.OrderBy (v => v.Members.Count);
		case ReportSortMode.ByILSize:
			return this.OrderBy (v => v.ILSize);
		default:
			throw new NotImplementedException (sortMode.ToString ());
		}
	}
}

public class TypeInfo {
	public TypeDefinition Type;
	public MemberInfos Members = new MemberInfos ();

	int? ilsize = null;
	public int ILSize {
		get {
			if (ilsize is null)
				ilsize = Members.Sum (v => v.ILSize);
			return ilsize.Value;
		}
	}

	public TypeInfo (TypeDefinition type)
	{
		Type = type;

		if (type.HasEvents)
			Members.AddRange (type.Events.Select (v => new MemberInfo (v)));
		if (type.HasFields)
			Members.AddRange (type.Fields.Select (v => new MemberInfo (v)));
		if (type.HasMethods)
			Members.AddRange (type.Methods.Select (v => new MemberInfo (v)));
		if (type.HasProperties)
			Members.AddRange (type.Properties.Select (v => new MemberInfo (v)));
	}
}

public class MemberInfos : List<MemberInfo> {
	public IEnumerable<MemberInfo> Sort (ReportSortMode sortMode)
	{
		switch (sortMode) {
		case ReportSortMode.ByName:
			return this.OrderBy (v => v.Member.FullName);
		case ReportSortMode.ByILSize:
			return this.OrderBy (v => v.ILSize);
		default:
			throw new NotImplementedException (sortMode.ToString ());
		}
	}
}

public class MemberInfo {
	public IMemberDefinition Member;

	int? ilsize = null;
	public int ILSize {
		get {
			if (ilsize is null) {
				var size = 0;
				if (Member is MethodDefinition md) {
					if (md.HasBody)
						size = md.Body.CodeSize;
				} else if (Member is FieldDefinition fd) {
					// no IL here
				} else if (Member is EventDefinition ed) {
					// no IL here
				} else if (Member is PropertyDefinition pd) {
					// no IL here
				} else {
					System.Diagnostics.Debugger.Break ();
					throw new NotImplementedException (Member.GetType ().FullName);
				}
				ilsize = size;

			}
			return ilsize.Value;
		}
	}

	public MemberInfo (IMemberDefinition member)
	{
		Member = member;
	}

}

public class NamespaceInfos : List<NamespaceInfo> {

	public IEnumerable<NamespaceInfo> Sort (ReportSortMode sortMode)
	{
		switch (sortMode) {
		case ReportSortMode.ByName:
			return this.OrderBy (v => v.Namespace);
		case ReportSortMode.ByCount:
			return this.OrderBy (v => v.Types.Count);
		case ReportSortMode.ByILSize:
			return this.OrderBy (v => v.ILSize);
		default:
			throw new NotImplementedException (sortMode.ToString ());
		}
	}
}

public class NamespaceInfo {
	public string Namespace;
	public TypeInfos Types = new TypeInfos ();

	int? ilsize = null;
	public int ILSize {
		get {
			if (ilsize is null)
				ilsize = Types.Sum (v => v.ILSize);
			return ilsize.Value;
		}
	}

	public NamespaceInfo (string @namespace, IEnumerable<TypeInfo> types)
	{
		this.Namespace = @namespace;
		Types.AddRange (types);
	}
}
