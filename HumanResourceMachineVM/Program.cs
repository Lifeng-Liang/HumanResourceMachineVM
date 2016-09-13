using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace HumanResourceMachineVM
{
	public class Machine
	{
		public string Human;
		public string[] Mem;
		public List<string> Input;
		public List<string> Src;
		public Dictionary<string, int> Labels;
		public Dictionary<string, int> Variables;
		public int CodeIndex;
		public int StepIndex;
		public bool Running;
		public List<string> Log;
		public int MaxStep;

		public Machine(string srcFile, string inputFile, string memFile, int maxStep)
		{
			Src = Read(srcFile);
			Input = Read(inputFile);
			Labels = GetLabels(Src);
			Variables = GetVariables(Src);
			int cc = Src.Count(code => code.StartsWith("//"));
			var codeCount = Src.Count - Labels.Count - Variables.Count - cc;
			Console.WriteLine($"<Codes : {codeCount}>");
			var memTemp = Read(memFile);
			var memList = new List<string>();
			foreach (var l in memTemp)
			{
				var ss = l.Split(' ');
				foreach (var s in ss)
				{
					memList.Add(s == "_" ? null : s);
				}
			}
			Mem = memList.ToArray();
			Human = null;
			CodeIndex = 0;
			Running = true;
			MaxStep = maxStep;
			Log = new List<string>();
		}

		public void Run()
		{
			int inputIndex = 0;
			int stepCount = 0;
			while(Running)
			{
				if (CodeIndex >= Src.Count)
				{
					break;
				}
				var code = Src[CodeIndex];
				CodeIndex++;
				if (code.StartsWith(":") || code.StartsWith("//") || code.StartsWith("="))
				{
					continue;
				}
				var ss = code.Split(new[] { ' ' }, 2);
				switch (ss[0].Trim().ToLower())
				{
				case "inbox":
					if (inputIndex >= Input.Count)
					{
						Running = false;
						break;
					}
					Human = Input[inputIndex];
					inputIndex++;
					break;
				case "outbox":
					Console.WriteLine(Human);
					Human = null;
					break;
				case "jump":
					CodeIndex = Labels[ss[1]];
					break;
				case "jumpifzero":
					CodeIndex = Jump(ss[1], n => n == 0);
					break;
				case "jumpifnegative":
					CodeIndex = Jump(ss[1], n => n < 0);
					break;
				case "add":
					ProcessAddr(ss[1], t => Human = (int.Parse(Human) + int.Parse(Mem[int.Parse(t)])).ToString());
					break;
				case "sub":
					ProcessAddr(ss[1], t => Human = (int.Parse(Human) - int.Parse(Mem[int.Parse(t)])).ToString());
					break;
				case "bump+":
					Human = Bump(ss[1], n => n + 1);
					break;
				case "bump-":
					Human = Bump(ss[1], n => n - 1);
					break;
				case "copyto":
					ProcessAddr(ss[1], t => Mem[int.Parse(t)] = Human);
					break;
				case "copyfrom":
					ProcessAddr(ss[1], t => Human = Mem[int.Parse(t)]);
					break;
				default:
					Console.WriteLine("<Unknown Code> " + ss[0]);
					Running = false;
					break;
				}
				Log.Add($"{code}\t// human:{Human},\tcodeIndex:{CodeIndex},\tinputIndex:{inputIndex}");
				stepCount++;
				if (stepCount >= MaxStep) {
					break;
				}
			}
			Console.WriteLine($"<Steps : {stepCount}>");
		}

		public void PrintLog()
		{
			foreach (var s in Log)
			{
				Console.WriteLine(s);
			}
		}

		private int Jump(string addr, Func<int, bool> callback)
		{
			int n;
			if (int.TryParse(Human, out n))
			{
				if (callback(n))
				{
					return Labels[addr];
				}
			}
			return CodeIndex;
		}

		private string Bump(string addr, Func<int, int> callback)
		{
			string result = null;
			ProcessAddr(addr, t => {
				var b = int.Parse(t);
				var m = int.Parse(Mem[b]);
				m = callback(m);
				Mem[b] = m.ToString();
				result = Mem[b];
			});
			return result;
		}

		private void ProcessAddr(string addr, Action<string> callback)
		{
			if (addr.StartsWith("[") && addr.EndsWith("]"))
			{
				var t = addr.Substring(1, addr.Length - 2);
				t = Mem[GetVarValue(t)];
				callback(t);
			}
			else
			{
				callback(GetVarValue(addr).ToString());
			}
		}

		private int GetVarValue(string addr)
		{
			int n;
			if (int.TryParse(addr, out n)) {
				return n;
			}
			return Variables[addr];
		}

		private static Dictionary<string, int> GetLabels(List<string> src)
		{
			var labels = new Dictionary<string, int>();
			for (int i = 0; i < src.Count; i++)
			{
				var code = src[i];
				if (code.StartsWith(":"))
				{
					labels.Add(code.Substring(1), i);
				}
			}
			return labels;
		}

		private static Dictionary<string, int> GetVariables(List<string> src)
		{
			var variables = new Dictionary<string, int>();
			foreach (var code in src) {
				if (code.StartsWith("=")) {
					var def = code.Substring(1);
					var ss = def.Split(new char[]{ ' ' }, 2);
					variables.Add(ss[0].Trim().ToLower(), int.Parse(ss[1]));
				}
			}
			return variables;
		}

		private static List<string> Read(string fileName)
		{
			var list = new List<string>();
			using (var r = new StreamReader(new FileStream(fileName, FileMode.Open, FileAccess.Read)))
			{
				string line;
				while ((line = r.ReadLine()) != null)
				{
					if (line != String.Empty)
					{
						list.Add(line);
					}
				}
			}
			return list;
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			int n;
			if (args.Length < 4 || !File.Exists(args[0]) || !File.Exists(args[1]) || !File.Exists(args[2]) || !int.TryParse(args[3], out n))
			{
				Console.WriteLine("Usage: HumanResourceMachineVM srcFile inputFile memFile maxStep [/debug]\nExample: HumanResourceMachineVM 1.hrm 1.txt c.mem 10000 /debug");
				return;
			}
			var m = new Machine(args[0], args[1], args[2], n);
			try {
				m.Run();
			} catch (Exception ex) {
				Console.WriteLine(ex);				
			}
			if (args.Length > 4 && args[4].ToLower() == "/debug")
			{
				m.PrintLog();
			}
		}
	}
}
