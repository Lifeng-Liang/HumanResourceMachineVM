using System;
using System.Collections.Generic;
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
		public int CodeIndex;
		public int StepIndex;
		public bool Running;
		public List<string> Log;

		public Machine(string srcFile, string inputFile, string memFile)
		{
			Src = Read(srcFile);
			Input = Read(inputFile);
			Labels = GetLabels(Src);
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
			Log = new List<string>();
		}

		public void Run()
		{
			int inputIndex = 0;
			int stepCount = 0;
			while (Running)
			{
				if (CodeIndex >= Src.Count)
				{
					break;
				}
				var code = Src[CodeIndex];
				CodeIndex++;
				if (code.StartsWith(":") || code.StartsWith("//"))
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
				t = Mem[int.Parse(t)];
				callback(t);
			}
			else
			{
				callback(addr);
			}
		}

		private static Dictionary<string, int> GetLabels(List<string> src)
		{
			var codeCount = 0;
			var labels = new Dictionary<string, int>();
			for (int i = 0; i < src.Count; i++)
			{
				var code = src[i];
				if (code.StartsWith(":"))
				{
					labels.Add(code.Substring(1), i);
				}
				else if (!code.StartsWith("//"))
				{
					codeCount++;
				}
			}
			Console.WriteLine($"<Codes : {codeCount}>");
			return labels;
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
			if (args.Length < 3 || !File.Exists(args[0]) || !File.Exists(args[1]) || !File.Exists(args[2]))
			{
				Console.WriteLine("Usage: HumanResourceMachineVM srcFile inputFile memFile [/debug]\nExample: HumanResourceMachineVM 1.hrm 1.txt c.mem /debug");
				return;
			}
			var m = new Machine(args[0], args[1], args[2]);
			m.Run();
			if (args.Length > 3 && args[3].ToLower() == "/debug")
			{
				m.PrintLog();
			}
		}
	}
}
