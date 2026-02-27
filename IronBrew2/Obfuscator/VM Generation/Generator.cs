using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using IronBrew2.Bytecode_Library.Bytecode;
using IronBrew2.Bytecode_Library.IR;
using IronBrew2.Extensions;
using IronBrew2.Obfuscator.Opcodes;

namespace IronBrew2.Obfuscator.VM_Generation
{
	public class Generator
	{
		private ObfuscationContext _context;
		
		public Generator(ObfuscationContext context) =>
			_context = context;

		public bool IsUsed(Chunk chunk, VOpcode virt)
		{
			bool isUsed = false;
			foreach (Instruction ins in chunk.Instructions)
				if (virt.IsInstruction(ins))
				{
					if (!_context.InstructionMapping.ContainsKey(ins.OpCode))
						_context.InstructionMapping.Add(ins.OpCode, virt);

					ins.CustomData = new CustomInstructionData {Opcode = virt};
					isUsed = true;
				}

			foreach (Chunk sChunk in chunk.Functions)
				isUsed |= IsUsed(sChunk, virt);

			return isUsed;
		}

		public static List<int> Compress(byte[] uncompressed)
		{
			// build the dictionary
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			for (int i = 0; i < 256; i++)
				dictionary.Add(((char)i).ToString(), i);
 
			string    w          = string.Empty;
			List<int> compressed = new List<int>();
 
			foreach (byte b in uncompressed)
			{
				string wc = w + (char)b;
				if (dictionary.ContainsKey(wc))
					w = wc;
				
				else
				{
					// write w to output
					compressed.Add(dictionary[w]);
					// wc is a new sequence; add it to the dictionary
					dictionary.Add(wc, dictionary.Count);
					w = ((char) b).ToString();
				}
			}
 
			// write remaining output if necessary
			if (!string.IsNullOrEmpty(w))
				compressed.Add(dictionary[w]);
 
			return compressed;
		}

		private static string BuildCodecAlphabet(Random random)
		{
			char[] chars =
				"天地玄黄宇宙洪荒日月盈昃辰宿列张寒来暑往秋收冬藏闰余成岁律吕调阳云腾致雨露结为霜金生丽水玉出昆冈剑号巨阙珠称夜光果珍李柰菜重芥姜".ToCharArray();
			chars = chars.OrderBy(_ => random.Next()).ToArray();
			return new string(chars);
		}

		public static string ToCustomBase(ulong value, string alphabet)
		{
			ulong radix = (ulong)alphabet.Length;
			var sb = new StringBuilder(13);
			do
			{
				sb.Insert(0, alphabet[(int)(value % radix)]);
				value /= radix;
			} while (value != 0);
			return sb.ToString();
		}

		public static string CompressedToString(List<int> compressed, string alphabet)
		{
			StringBuilder sb = new StringBuilder();
			foreach (int i in compressed)
			{
				string n = ToCustomBase((ulong)i, alphabet);
				if (n.Length >= alphabet.Length)
					throw new Exception("Encoded token length exceeded codec alphabet range.");

				sb.Append(alphabet[n.Length]);
				sb.Append(n);
			}

			return sb.ToString();
		}
		
		private static string EscapeLuaSingleQuoted(string value) =>
			value.Replace("\\", "\\\\")
				.Replace("'", "\\'")
				.Replace("\r", "\\r")
				.Replace("\n", "\\n");

		private static string BuildDecoyNoise(Random random)
		{
			List<string> decoys = new List<string>
			{
				"ironbrew on mac",
				"made by thatsmymute",
				"bytecode tastes like static",
				"nothing to see here",
				"constants are sleeping",
				"vm whispers in base36",
				"not the real payload",
				"decoy lane open",
				"noise field enabled"
			};

			for (int i = 0; i < 4; i++)
				decoys.Add("ib2-noise-" + Guid.NewGuid().ToString("N").Substring(0, 10));

			decoys = decoys.OrderBy(_ => random.Next()).Take(8).ToList();

			StringBuilder noise = new StringBuilder();
			noise.Append("local __ib2_noise={");
			for (int i = 0; i < decoys.Count; i++)
			{
				if (i != 0)
					noise.Append(',');
				noise.Append('\'');
				noise.Append(EscapeLuaSingleQuoted(decoys[i]));
				noise.Append('\'');
			}
			noise.Append("};");
			noise.Append("local function __ib2_bait(k)local v=__ib2_noise[(k % #__ib2_noise)+1];return Sub(v,1,#v);end;");
			noise.Append("if false then ByteString=__ib2_bait(3);end;");
			return noise.ToString();
		}

		public List<OpMutated> GenerateMutations(List<VOpcode> opcodes)
		{
			Random r = new Random();
			List<OpMutated> mutated = new List<OpMutated>();

			foreach (VOpcode opc in opcodes)
			{
				if (opc is OpSuperOperator)
					continue;

				for (int i = 0; i < r.Next(35, 50); i++)
				{
					int[] rand = {0, 1, 2};
					rand.Shuffle();

					OpMutated mut = new OpMutated();

					mut.Registers = rand;
					mut.Mutated = opc;
						
					mutated.Add(mut);
				}
			}

			mutated.Shuffle();
			return mutated;
		}

		public void FoldMutations(List<OpMutated> mutations, HashSet<OpMutated> used, Chunk chunk)
		{
			bool[] skip = new bool[chunk.Instructions.Count + 1];
			
			for (int i = 0; i < chunk.Instructions.Count; i++)
			{
				Instruction opc = chunk.Instructions[i];

				switch (opc.OpCode)
				{
					case Opcode.Closure:
						for (int j = 1; j <= ((Chunk) opc.RefOperands[0]).UpvalueCount; j++)
							skip[i + j] = true;

						break;
				}
			}
			
			for (int i = 0; i < chunk.Instructions.Count; i++)
			{
				if (skip[i])
					continue;
				
				Instruction opc = chunk.Instructions[i];
				CustomInstructionData data = opc.CustomData;
				
				foreach (OpMutated mut in mutations)
					if (data.Opcode == mut.Mutated && data.WrittenOpcode == null)
					{
						if (!used.Contains(mut))
							used.Add(mut);

						data.Opcode = mut;
						break;
					}
			}
			
			foreach (Chunk _c in chunk.Functions)
				FoldMutations(mutations, used, _c);
		}

		public List<OpSuperOperator> GenerateSuperOperators(Chunk chunk, int maxSize, int minSize = 5)
		{
			List<OpSuperOperator> results = new List<OpSuperOperator>();
			Random                r       = new Random();

			bool[] skip = new bool[chunk.Instructions.Count + 1];

			for (int i = 0; i < chunk.Instructions.Count - 1; i++)
			{
				switch (chunk.Instructions[i].OpCode)
				{
					case Opcode.Closure:
					{
						skip[i] = true;
						for (int j = 0; j < ((Chunk) chunk.Instructions[i].RefOperands[0]).UpvalueCount; j++)
							skip[i + j + 1] = true;
							
						break;
					}

					case Opcode.Eq:
					case Opcode.Lt:
					case Opcode.Le:
					case Opcode.Test:
					case Opcode.TestSet:
					case Opcode.TForLoop:
					case Opcode.SetList:
					case Opcode.LoadBool when chunk.Instructions[i].C != 0:
						skip[i + 1] = true;
						break;

					case Opcode.ForLoop:
					case Opcode.ForPrep:
					case Opcode.Jmp:
						chunk.Instructions[i].UpdateRegisters();
						
						skip[i + 1] = true;
						skip[i + chunk.Instructions[i].B + 1] = true;
						break;
				}
				
				if (chunk.Instructions[i].CustomData.WrittenOpcode is OpSuperOperator su && su.SubOpcodes != null)
					for (int j = 0; j < su.SubOpcodes.Length; j++)
						skip[i + j] = true;
			}
			
			int c = 0;
			while (c < chunk.Instructions.Count)
			{
				int targetCount = maxSize;
				OpSuperOperator superOperator = new OpSuperOperator {SubOpcodes = new VOpcode[targetCount]};

				bool d     = true;
				int cutoff = targetCount;

				for (int j = 0; j < targetCount; j++)
					if (c + j > chunk.Instructions.Count - 1 || skip[c + j])
					{
						cutoff = j; 
						d = false;
						break;
					}

				if (!d)
				{
					if (cutoff < minSize)
					{
						c += cutoff + 1;	
						continue;
					}
						
					targetCount = cutoff;	
					superOperator = new OpSuperOperator {SubOpcodes = new VOpcode[targetCount]};
				}
				
				for (int j = 0; j < targetCount; j++)
					superOperator.SubOpcodes[j] =
						chunk.Instructions[c + j].CustomData.Opcode;

				results.Add(superOperator);
				c += targetCount + 1;
			}

			foreach (var _c in chunk.Functions)
				results.AddRange(GenerateSuperOperators(_c, maxSize));
			
			return results;
		}

		public void FoldAdditionalSuperOperators(Chunk chunk, List<OpSuperOperator> operators, ref int folded)
		{
			bool[] skip = new bool[chunk.Instructions.Count + 1];
			for (int i = 0; i < chunk.Instructions.Count - 1; i++)
			{
				switch (chunk.Instructions[i].OpCode)
				{
					case Opcode.Closure:
					{
						skip[i] = true;
						for (int j = 0; j < ((Chunk) chunk.Instructions[i].RefOperands[0]).UpvalueCount; j++)
							skip[i + j + 1] = true;
							
						break;
					}

					case Opcode.Eq:
					case Opcode.Lt:
					case Opcode.Le:
					case Opcode.Test:
					case Opcode.TestSet:
					case Opcode.TForLoop:
					case Opcode.SetList:
					case Opcode.LoadBool when chunk.Instructions[i].C != 0:
						skip[i + 1] = true;
						break;

					case Opcode.ForLoop:
					case Opcode.ForPrep:
					case Opcode.Jmp:
						chunk.Instructions[i].UpdateRegisters();
						skip[i + 1] = true;
						skip[i + chunk.Instructions[i].B + 1] = true;
						break;
				}
				
				if (chunk.Instructions[i].CustomData.WrittenOpcode is OpSuperOperator su && su.SubOpcodes != null)
					for (int j = 0; j < su.SubOpcodes.Length; j++)
						skip[i + j] = true;
			}
			
			int c = 0;
			while (c < chunk.Instructions.Count)
			{
				if (skip[c])
				{
					c++;
					continue;
				}

				bool used = false;

				foreach (OpSuperOperator op in operators)
				{
					int targetCount = op.SubOpcodes.Length;
					bool cu = true;
					for (int j = 0; j < targetCount; j++)
					{
						if (c + j > chunk.Instructions.Count - 1 || skip[c + j])
						{
							cu = false;
							break;
						}
					}

					if (!cu)
						continue;


					List<Instruction> taken = chunk.Instructions.Skip(c).Take(targetCount).ToList();
					if (op.IsInstruction(taken))
					{
						for (int j = 0; j < targetCount; j++)
						{
							skip[c + j] = true;
							chunk.Instructions[c + j].CustomData.WrittenOpcode = new OpSuperOperator {VIndex = 0};
						}

						chunk.Instructions[c].CustomData.WrittenOpcode = op;

						used = true;
						break;
					}
				}

				if (!used)
					c++;
				else
					folded++;
			}

			foreach (var _c in chunk.Functions)
				FoldAdditionalSuperOperators(_c, operators, ref folded);
		}
		
		public string GenerateVM(ObfuscationSettings settings)
		{
			Random r = new Random();

			List<VOpcode> virtuals = Assembly.GetExecutingAssembly().GetTypes()
			                                 .Where(t => t.IsSubclassOf(typeof(VOpcode)))
			                                 .Select(Activator.CreateInstance)
			                                 .Cast<VOpcode>()
			                                 .Where(t => IsUsed(_context.HeadChunk, t))
			                                 .ToList();

			
			if (settings.Mutate)
			{
				List<OpMutated> muts = GenerateMutations(virtuals).Take(settings.MaxMutations).ToList();
				
				Console.WriteLine("Created " + muts.Count + " mutations.");
				
				HashSet<OpMutated> used = new HashSet<OpMutated>();
				FoldMutations(muts, used, _context.HeadChunk);
				
				Console.WriteLine("Used " + used.Count + " mutations.");
				
				virtuals.AddRange(used);
			}
			
			if (settings.SuperOperators)
			{
				int folded = 0;
				
				var megaOperators = GenerateSuperOperators(_context.HeadChunk, 80, 60).OrderBy(t => r.Next())
					.Take(settings.MaxMegaSuperOperators).ToList();
				
				Console.WriteLine("Created " + megaOperators.Count + " mega super operators.");
				
				virtuals.AddRange(megaOperators);
				
				FoldAdditionalSuperOperators(_context.HeadChunk, megaOperators, ref folded);
				
				var miniOperators = GenerateSuperOperators(_context.HeadChunk, 10).OrderBy(t => r.Next())
					.Take(settings.MaxMiniSuperOperators).ToList();
				
				Console.WriteLine("Created " + miniOperators.Count + " mini super operators.");
				
				virtuals.AddRange(miniOperators);
				
				FoldAdditionalSuperOperators(_context.HeadChunk, miniOperators, ref folded);
				
				Console.WriteLine("Folded " + folded + " instructions into super operators.");
			}
			
			virtuals.Shuffle();
			
			for (int i = 0; i < virtuals.Count; i++)
				virtuals[i].VIndex = i;

			string vm = "";

			byte[] bs = new Serializer(_context, settings).SerializeLChunk(_context.HeadChunk);
			
			vm += @"
local Byte         = string.byte;
local Char         = string.char;
local Sub          = string.sub;
local Insert       = function(T, V) T[#T + 1] = V end;
local LDExp        = math.ldexp;
local GetFEnv      = getfenv or function() return _ENV end;
local Setmetatable = setmetatable;
local Select       = select;

local Unpack = unpack or table.unpack;
local ToNumber = tonumber;
local PCall = pcall;
local RawGet = rawget;
local Dbg = RawGet(GetFEnv(), 'debug');
local DbgInfo = Dbg and Dbg.getinfo;

local function EnsureNative(Name, Fn)
	if DbgInfo then
		local Ok, Info = PCall(DbgInfo, Fn);
		if (not Ok) or (not Info) or (Info.what ~= 'C') then
			error('IRONBREW TAMPER DETECTED (' .. Name .. ')');
		end;
	end;
end;

EnsureNative('string.byte', Byte);
EnsureNative('string.char', Char);
EnsureNative('string.sub', Sub);
EnsureNative('tonumber', ToNumber);
EnsureNative('math.ldexp', LDExp);

local function Concat(T)
	local Len = #T;
	if (Len == 0) then
		return '';
	end;

	while Len > 1 do
		local Next = {};
		local NextIdx = 1;
		local Idx = 1;

		while Idx <= Len do
			if (Idx == Len) then
				Next[NextIdx] = T[Idx];
			else
				Next[NextIdx] = T[Idx] .. T[Idx + 1];
			end;
			NextIdx = NextIdx + 1;
			Idx = Idx + 2;
		end;

		T = Next;
		Len = #T;
	end;

	return T[1];
end;";
			vm += BuildDecoyNoise(r);

				if (settings.BytecodeCompress)
				{
					string codecAlphabet = BuildCodecAlphabet(r);
					string escapedAlphabet = EscapeLuaSingleQuoted(codecAlphabet);
					vm += "local __ib2_alphabet='" + escapedAlphabet + "';";
					vm += "local function __ib2_next_char(s,p)local b=Byte(s,p,p);if not b then return nil,p end;local n=1;if b>=240 then n=4 elseif b>=224 then n=3 elseif b>=192 then n=2 end;return Sub(s,p,p+n-1),p+n end;";
					vm += "local function __ib2_count_chars(s)local n=0;local p=1;while p<=#s do local _,np=__ib2_next_char(s,p);if not np then break end;n=n+1;p=np end;return n end;";
					vm += "local __ib2_base=__ib2_count_chars(__ib2_alphabet);";
					vm += "local __ib2_rev={};do local p=1;local idx=0;while p<=#__ib2_alphabet do local ch,np=__ib2_next_char(__ib2_alphabet,p);if not ch then break end;__ib2_rev[ch]=idx;idx=idx+1;p=np end;end;";
					vm += "local function __ib2_to_num(s)local v=0;local p=1;while p<=#s do local ch,np=__ib2_next_char(s,p);if not ch then break end;v=v*__ib2_base+(__ib2_rev[ch] or 0);p=np end;return v;end;";
					vm += "local function __ib2_take_chars(s,p,count)local sp=p;for _=1,count do local _,np=__ib2_next_char(s,p);if not np then error('IRONBREW CODEC ERROR') end;p=np end;return Sub(s,sp,p-1),p end;";
					vm += "local function decompress(b)local c,d,e=\"\",\"\",{}local f=256;local g={}for h=0,f-1 do g[h]=Char(h)end;local i=1;local function k()local lenSym;lenSym,i=__ib2_next_char(b,i);if not lenSym then error('IRONBREW CODEC ERROR') end;local l=__ib2_rev[lenSym] or -1;if l<=0 then error('IRONBREW CODEC ERROR');end;local tok;tok,i=__ib2_take_chars(b,i,l);local m=__ib2_to_num(tok);return m end;c=Char(k())e[1]=c;while i<=#b do local n=k()if g[n]then d=g[n]else d=c..Sub(c,1,1)end;g[f]=c..Sub(d,1,1)e[#e+1],c,f=d,d,f+1 end;return Concat(e)end;";
					vm += "local ByteString=decompress('" + CompressedToString(Compress(bs), codecAlphabet) + "');\n";
				}
			else
			{
				vm += "ByteString='";

				StringBuilder sb = new StringBuilder();
				foreach (byte b in bs)
				{
					sb.Append('\\');
					sb.Append(b);
				}

				vm += sb + "';\n";
			}

			int maxConstants = 0;

			void ComputeConstants(Chunk c)
			{
				if (c.Constants.Count > maxConstants)
					maxConstants = c.Constants.Count;
				
				foreach (Chunk _c in c.Functions)
					ComputeConstants(_c);
			}
			
			ComputeConstants(_context.HeadChunk);

			vm += VMStrings.VMP1
				.Replace("XOR_KEY", _context.PrimaryXorKey.ToString())
				.Replace("CONST_BOOL", _context.ConstantMapping[1].ToString())
				.Replace("CONST_FLOAT", _context.ConstantMapping[2].ToString())
				.Replace("CONST_STRING", _context.ConstantMapping[3].ToString());
			
			for (int i = 0; i < (int) ChunkStep.StepCount; i++)
			{
				switch (_context.ChunkSteps[i])
				{
					case ChunkStep.ParameterCount:
						vm += "Chunk[3] = gBits8();";
						break;
					case ChunkStep.Instructions:
						vm +=
							$@"for Idx=1,gBits32() do 
									local Descriptor = gBits8();
									if (gBit(Descriptor, 1, 1) == 0) then
										local Type = gBit(Descriptor, 2, 3);
										local Mask = gBit(Descriptor, 4, 6);
										
										local Inst=
										{{
											gBits16(),
											gBits16(),
											nil,
											nil
										}};
	
										if (Type == 0) then 
											Inst[OP_B] = gBits16(); 
											Inst[OP_C] = gBits16();
										elseif(Type==1) then 
											Inst[OP_B] = gBits32();
										elseif(Type==2) then 
											Inst[OP_B] = gBits32() - (2 ^ 16)
										elseif(Type==3) then 
											Inst[OP_B] = gBits32() - (2 ^ 16)
											Inst[OP_C] = gBits16();
										end;
	
										if (gBit(Mask, 1, 1) == 1) then Inst[OP_A] = Consts[Inst[OP_A]] end
										if (gBit(Mask, 2, 2) == 1) then Inst[OP_B] = Consts[Inst[OP_B]] end
										if (gBit(Mask, 3, 3) == 1) then Inst[OP_C] = Consts[Inst[OP_C]] end
										
										Instrs[Idx] = Inst;
									end
								end;";
						break;
					case ChunkStep.Functions:
						vm += "for Idx=1,gBits32() do Functions[Idx-1]=Deserialize();end;";
						break;
					case ChunkStep.LineInfo:
						if (settings.PreserveLineInfo)
							vm += "for Idx=1,gBits32() do Lines[Idx]=gBits32();end;";
						break;
				}
			}

				vm += "WipeTable(Consts);return ProtectChunk(Chunk);end;";
			vm += settings.PreserveLineInfo ? VMStrings.VMP2_LI : VMStrings.VMP2;

			int maxFunc = 0;

			void ComputeFuncs(Chunk c)
			{
				if (c.Functions.Count > maxFunc)
					maxFunc = c.Functions.Count;
				
				foreach (Chunk _c in c.Functions)
					ComputeFuncs(_c);
			}
			
			ComputeFuncs(_context.HeadChunk);

			int maxInstrs = 0;

			void ComputeInstrs(Chunk c)
			{
				if (c.Instructions.Count > maxInstrs)
					maxInstrs = c.Instructions.Count;
				
				foreach (Chunk _c in c.Functions)
					ComputeInstrs(_c);
			}
			
			ComputeInstrs(_context.HeadChunk);
			
			string GetStr(List<int> opcodes)
			{
				string str = "";
				
				if (opcodes.Count == 1)
					str += $"{virtuals[opcodes[0]].GetObfuscated(_context)}";

				else if (opcodes.Count == 2) 
				{
					if (r.Next(2) == 0)
					{
						str +=
							$"if Enum > {virtuals[opcodes[0]].VIndex} then {virtuals[opcodes[1]].GetObfuscated(_context)}";
						str += $"else {virtuals[opcodes[0]].GetObfuscated(_context)}";
						str += "end;";
					}
					else
					{
						str +=
							$"if Enum == {virtuals[opcodes[0]].VIndex} then {virtuals[opcodes[0]].GetObfuscated(_context)}";
						str += $"else {virtuals[opcodes[1]].GetObfuscated(_context)}";
						str += "end;";
					}
				}
				else
				{
					List<int> ordered = opcodes.OrderBy(o => o).ToList();
					var sorted = new[] { ordered.Take(ordered.Count / 2).ToList(), ordered.Skip(ordered.Count / 2).ToList() };
					
					str += "if Enum <= " + sorted[0].Last() + " then ";
					str += GetStr(sorted[0]);
					str += " else";
					str += GetStr(sorted[1]);
				}

				return str;
			}

			vm += GetStr(Enumerable.Range(0, virtuals.Count).ToList());
			vm += settings.PreserveLineInfo ? VMStrings.VMP3_LI : VMStrings.VMP3;

			vm = vm.Replace("OP_ENUM", "1")
				.Replace("OP_A", "2")
				.Replace("OP_B", "3")
				.Replace("OP_C", "4");

			
			return vm;
		}
	}
}
