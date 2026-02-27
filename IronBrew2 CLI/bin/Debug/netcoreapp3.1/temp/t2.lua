
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
end;local __ib2_noise={'constants are sleeping','nothing to see here','noise field enabled','ib2-noise-ae7e01658d','vm whispers in base36','bytecode tastes like static','made by thatsmymute','ironbrew on mac'};local function __ib2_bait(k)local v=__ib2_noise[(k % #__ib2_noise)+1];return Sub(v,1,#v);end;if false then ByteString=__ib2_bait(3);end;local __ib2_alphabet='金冬柰致盈律阙张往雨为丽吕称光闰成巨宇冈菜重腾调云生夜辰月出荒列珍水宙日阳洪宿天果剑藏暑霜收珠玉余昃李寒黄岁号昆姜玄秋露结芥地来';local function __ib2_next_char(s,p)local b=Byte(s,p,p);if not b then return nil,p end;local n=1;if b>=240 then n=4 elseif b>=224 then n=3 elseif b>=192 then n=2 end;return Sub(s,p,p+n-1),p+n end;local function __ib2_count_chars(s)local n=0;local p=1;while p<=#s do local _,np=__ib2_next_char(s,p);if not np then break end;n=n+1;p=np end;return n end;local __ib2_base=__ib2_count_chars(__ib2_alphabet);local __ib2_rev={};do local p=1;local idx=0;while p<=#__ib2_alphabet do local ch,np=__ib2_next_char(__ib2_alphabet,p);if not ch then break end;__ib2_rev[ch]=idx;idx=idx+1;p=np end;end;local function __ib2_to_num(s)local v=0;local p=1;while p<=#s do local ch,np=__ib2_next_char(s,p);if not ch then break end;v=v*__ib2_base+(__ib2_rev[ch] or 0);p=np end;return v;end;local function __ib2_take_chars(s,p,count)local sp=p;for _=1,count do local _,np=__ib2_next_char(s,p);if not np then error('IRONBREW CODEC ERROR') end;p=np end;return Sub(s,sp,p-1),p end;local function decompress(b)local c,d,e="","",{}local f=256;local g={}for h=0,f-1 do g[h]=Char(h)end;local i=1;local function k()local lenSym;lenSym,i=__ib2_next_char(b,i);if not lenSym then error('IRONBREW CODEC ERROR') end;local l=__ib2_rev[lenSym] or -1;if l<=0 then error('IRONBREW CODEC ERROR');end;local tok;tok,i=__ib2_take_chars(b,i,l);local m=__ib2_to_num(tok);return m end;c=Char(k())e[1]=c;while i<=#b do local n=k()if g[n]then d=g[n]else d=c..Sub(c,1,1)end;g[f]=c..Sub(d,1,1)e[#e+1],c,f=d,d,f+1 end;return Concat(e)end;local ByteString=decompress('柰冬荒柰冬生柰盈冬柰冬云柰致珍柰盈冬柰冬生冬宙柰冬冈柰冬成柰冬露冬黄冬结冬藏冬藏冬姜冬地冬结柰冬露柰冬日柰冬玄柰冬露冬藏冬霜冬秋冬秋柰盈吕冬藏柰冬露柰冬岁柰盈往柰冬露冬余冬昆冬来冬号柰盈宇柰冬玄柰盈张柰冬成柰盈雨冬剑冬号柰盈珍冬收柰盈辰柰盈冈柰冬果柰冬暑柰冬剑柰盈出柰盈天柰盈列冬藏冬重冬结冬姜冬芥冬结冬暑柰盈阳冬收冬暑冬霜冬结柰盈李柰盈雨冬霜冬藏柰盈秋柰盈阳柰盈宿柰盈寒冬余冬芥柰盈阳柰盈玉柰冬藏柰冬收柰冬霜柰律冬柰盈雨冬昆冬姜柰盈丽柰盈阳柰冬露冬冈冬号冬昃冬昆冬出冬号柰盈巨柰盈往柰冬成冬阳柰律冬柰冬露冬剑冬姜冬藏冬收冬阙冬藏冬秋冬号冬暑柰盈吕柰盈阳冬柰柰冬霜柰盈昃柰冬玄柰冬收柰冬暑柰冬岁柰冬玄柰冬珍柰冬霜冬盈柰律水冬露柰盈吕柰律宿冬来冬暑冬余冬结冬昆柰律为柰盈冈冬昆冬霜冬岁冬岁柰盈往冬阳柰冬冈柰阙雨柰冬云柰冬月柰盈律冬剑柰律地冬昆冬收柰冬云柰冬出柰盈律冬地柰律冈冬结柰冬云柰冬冈柰盈律冬荒冬结冬收冬为柰盈秋冬玉冬余柰盈生柰冬云柰冬宇柰盈律冬巨冬收冬收冬剑柰阙列冬暑柰阙水柰阙日柰阙夜柰盈冬冬冈冬为冬腾冬调冬出冬结柰律剑柰盈玄柰冬云柰盈金柰盈冬柰盈丽柰盈称柰盈闰冬结柰冬重柰盈律柰冬丽柰盈律柰冬生柰冬云柰张冬柰盈柰柰冬生柰冬辰柰张阙柰张往柰冬生柰冬夜柰盈律柰冬玄柰盈冬柰张雨柰张雨柰阙菜柰张柰柰冬生柰阙菜柰阙称柰张致柰张丽柰阙菜柰张雨柰张闰柰张丽柰张雨柰冬列柰张致柰阙菜柰张云柰张成柰张重柰张丽柰张月柰张巨柰冬生柰阙秋柰张金柰张律柰张雨柰张律柰冬生柰张列柰张张柰张宙柰冬生');

local BitXOR = bit and bit.bxor or function(a,b)
    local p,c=1,0
    while a>0 and b>0 do
        local ra,rb=a%2,b%2
        if ra~=rb then c=c+p end
        a,b,p=(a-ra)/2,(b-rb)/2,p*2
    end
    if a<b then a=b end
    while a>0 do
        local ra=a%2
        if ra>0 then c=c+p end
        a,p=(a-ra)/2,p*2
    end
    return c
end

local function gBit(Bit, Start, End)
	if End then
		local Res = (Bit / 2 ^ (Start - 1)) % 2 ^ ((End - 1) - (Start - 1) + 1);
		return Res - Res % 1;
	else
		local Plc = 2 ^ (Start - 1);
        return (Bit % (Plc + Plc) >= Plc) and 1 or 0;
	end;
end;

local Pos = 1;

local function gBits32()
    local W, X, Y, Z = Byte(ByteString, Pos, Pos + 3);

	W = BitXOR(W, 89)
	X = BitXOR(X, 89)
	Y = BitXOR(Y, 89)
	Z = BitXOR(Z, 89)

    Pos	= Pos + 4;
    return (Z*16777216) + (Y*65536) + (X*256) + W;
end;

local function gBits8()
    local F = BitXOR(Byte(ByteString, Pos, Pos), 89);
    Pos = Pos + 1;
    return F;
end;

local function gBits16()
    local W, X = Byte(ByteString, Pos, Pos + 2);

	W = BitXOR(W, 89)
	X = BitXOR(X, 89)

    Pos	= Pos + 2;
    return (X*256) + W;
end;

local function gFloat()
	local Left = gBits32();
	local Right = gBits32();
	local IsNormal = 1;
	local Mantissa = (gBit(Right, 1, 20) * (2 ^ 32))
					+ Left;
	local Exponent = gBit(Right, 21, 31);
	local Sign = ((-1) ^ gBit(Right, 32));
	if (Exponent == 0) then
		if (Mantissa == 0) then
			return Sign * 0; -- +-0
		else
			Exponent = 1;
			IsNormal = 0;
		end;
	elseif (Exponent == 2047) then
        return (Mantissa == 0) and (Sign * (1 / 0)) or (Sign * (0 / 0));
	end;
	return LDExp(Sign, Exponent - 1023) * (IsNormal + (Mantissa / (2 ^ 52)));
end;

local function WipeTable(T)
	for K in next, T do
		T[K] = nil;
	end;
end;

local gSizet = gBits32;
local function gString(Len)
    local Str;
    if (not Len) then
        Len = gSizet();
        if (Len == 0) then
            return '';
        end;
    end;

    Str	= Sub(ByteString, Pos, Pos + Len - 1);
    Pos = Pos + Len;

	local Bytes = {};
	for Idx = 1, #Str do
		Bytes[Idx] = BitXOR(Byte(Sub(Str, Idx, Idx)), 89);
	end;

	local Parts = {};
	local P = 1;
	while (P <= #Bytes) do
		local Q = P + 31;
		if (Q > #Bytes) then
			Q = #Bytes;
		end;
		Parts[#Parts + 1] = Char(Unpack(Bytes, P, Q));
		P = Q + 1;
	end;

	Str = nil;
	WipeTable(Bytes);
	local Decoded = Concat(Parts);
	WipeTable(Parts);
	return Decoded;
end;

local gInt = gBits32;
local function _R(...) return {...}, Select('#', ...) end

local function ProtectTable(T)
	return Setmetatable(T, {
		__metatable = 'locked',
		__iter = function()
			return function()
				return nil;
			end;
		end
	});
end;

local function ProtectChunk(Chunk)
	ProtectTable(Chunk);
	ProtectTable(Chunk[1]);
	ProtectTable(Chunk[2]);
	ProtectTable(Chunk[4]);

	for _, Child in next, Chunk[2] do
		if Child then
			ProtectChunk(Child);
		end;
	end;

	return Chunk;
end;

local function Deserialize()
    local Instrs = {};
    local Functions = {};
	local Lines = {};
    local Chunk = 
	{
		Instrs,
		Functions,
		nil,
		Lines
	};
	local ConstCount = gBits32()
    local Consts = {}

	for Idx=1, ConstCount do 
		local Type =gBits8();
		local Cons;
	
		if(Type==3) then Cons = (gBits8() ~= 0);
		elseif(Type==0) then Cons = gFloat();
		elseif(Type==1) then Cons = gString();
		end;
		
		Consts[Idx] = Cons;
	end;
for Idx=1,gBits32() do 
									local Descriptor = gBits8();
									if (gBit(Descriptor, 1, 1) == 0) then
										local Type = gBit(Descriptor, 2, 3);
										local Mask = gBit(Descriptor, 4, 6);
										
										local Inst=
										{
											gBits16(),
											gBits16(),
											nil,
											nil
										};
	
										if (Type == 0) then 
											Inst[3] = gBits16(); 
											Inst[4] = gBits16();
										elseif(Type==1) then 
											Inst[3] = gBits32();
										elseif(Type==2) then 
											Inst[3] = gBits32() - (2 ^ 16)
										elseif(Type==3) then 
											Inst[3] = gBits32() - (2 ^ 16)
											Inst[4] = gBits16();
										end;
	
										if (gBit(Mask, 1, 1) == 1) then Inst[2] = Consts[Inst[2]] end
										if (gBit(Mask, 2, 2) == 1) then Inst[3] = Consts[Inst[3]] end
										if (gBit(Mask, 3, 3) == 1) then Inst[4] = Consts[Inst[4]] end
										
										Instrs[Idx] = Inst;
									end
								end;for Idx=1,gBits32() do Functions[Idx-1]=Deserialize();end;Chunk[3] = gBits8();WipeTable(Consts);return ProtectChunk(Chunk);end;
local function Wrap(Chunk, Upvalues, Env)
	local Instr  = Chunk[1];
	local Proto  = Chunk[2];
	local Params = Chunk[3];

	return function(...)
		local Instr  = Instr; 
		local Proto  = Proto; 
		local Params = Params;

		local _R = _R
		local InstrPoint = 1;
		local Top = -1;

		local Vararg = {};
		local Args	= {...};

		local PCount = Select('#', ...) - 1;

		local Lupvals	= {};
		local Stk		= {};

		for Idx = 0, PCount do
			if (Idx >= Params) then
				Vararg[Idx - Params] = Args[Idx + 1];
			else
				Stk[Idx] = Args[Idx + 1];
			end;
		end;

		local Varargsz = PCount - Params + 1

		local Inst;
		local Enum;	

		while true do
			Inst		= Instr[InstrPoint];
			Enum		= Inst[1];if Enum <= 7 then if Enum <= 3 then if Enum <= 1 then if Enum > 0 then Stk[Inst[2]] = Inst[3];else local B;local A;Stk[Inst[2]] = Inst[3];InstrPoint = InstrPoint + 1;Inst = Instr[InstrPoint];Stk[Inst[2]]=Env[Inst[3]];InstrPoint = InstrPoint + 1;Inst = Instr[InstrPoint];Stk[Inst[2]]=Env[Inst[3]];InstrPoint = InstrPoint + 1;Inst = Instr[InstrPoint];A=Inst[2];B=Stk[Inst[3]];Stk[A+1]=B;Stk[A]=B[Inst[4]];InstrPoint = InstrPoint + 1;Inst = Instr[InstrPoint];Stk[Inst[2]] = Inst[3];InstrPoint = InstrPoint + 1;Inst = Instr[InstrPoint];
A= Inst[2]
Stk[A] = Stk[A](Unpack(Stk, A + 1, Inst[3])) 
InstrPoint = InstrPoint + 1;Inst = Instr[InstrPoint];A=Inst[2];B=Stk[Inst[3]];Stk[A+1]=B;Stk[A]=B[Inst[4]];InstrPoint = InstrPoint + 1;Inst = Instr[InstrPoint];Stk[Inst[2]]=Stk[Inst[3]];InstrPoint = InstrPoint + 1;Inst = Instr[InstrPoint];
A= Inst[2]
Stk[A] = Stk[A](Unpack(Stk, A + 1, Inst[3])) 
InstrPoint = InstrPoint + 1;Inst = Instr[InstrPoint];Stk[Inst[2]]=Stk[Inst[3]][Inst[4]];end; elseif Enum == 2 then Stk[Inst[2]]=Stk[Inst[3]];else local A=Inst[2];local B=Stk[Inst[3]];Stk[A+1]=B;Stk[A]=B[Inst[4]];end; elseif Enum <= 5 then if Enum > 4 then Stk[Inst[2]]=Stk[Inst[3]];else Stk[Inst[2]] = Inst[3];end; elseif Enum > 6 then Stk[Inst[2]]=Env[Inst[3]];else do return end;end; elseif Enum <= 11 then if Enum <= 9 then if Enum > 8 then do return end;else local A=Inst[2];local B=Stk[Inst[3]];Stk[A+1]=B;Stk[A]=B[Inst[4]];end; elseif Enum == 10 then 
local A = Inst[2]
Stk[A](Stk[A + 1])
else 
local A = Inst[2]
Stk[A] = Stk[A](Unpack(Stk, A + 1, Inst[3])) 
end; elseif Enum <= 13 then if Enum == 12 then 
local A = Inst[2]
Stk[A](Stk[A + 1])
else Stk[Inst[2]]=Stk[Inst[3]][Inst[4]];end; elseif Enum <= 14 then 
local A = Inst[2]
Stk[A] = Stk[A](Unpack(Stk, A + 1, Inst[3])) 
 elseif Enum > 15 then Stk[Inst[2]]=Env[Inst[3]];else Stk[Inst[2]]=Stk[Inst[3]][Inst[4]];end;
			InstrPoint	= InstrPoint + 1;
		end;
    end;
end;	
return Wrap(Deserialize(), {}, GetFEnv())();
