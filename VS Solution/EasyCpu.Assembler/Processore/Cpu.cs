using System;
using System.Text;
using System.Collections;
using EasyCpu.Assembler.Parsing;
using EasyCpu.Assembler.Memoria;
using EasyCpu.Common;


namespace EasyCpu.Assembler.Processore
{
    public class Cpu
    {

        static string[] nomiRegs =
        {
        "AX", "BX", "CX", "DX", "SI", "DI", "BP", "SP", "IP",
    };

        public enum StatoCpu
        {
            Pronta,
            Attiva,
            Ferma
        }



        // bit del registro dei flags
        const short ZF = 1;
        const short SF = 2;
        const short OF = 4;

        const short TUTTI = (ZF | SF | OF); // maschera che include tutti i flag

        // registri
        static short ax;        // accumulatore
        static short bx;        // accumulatore secondario
        static short cx;        // contatore
        static short dx;

        static short sp;        // indice inizio dello stack (stack pointer)
        static short bp;        // indice corrente dello stack (base pointer) 
        static short ip;        // program counter

        static short si;        // indice sorgente
        static short di;        // indice destinazione

        static short flags; // flags

        public static bool stop;            // flag di arresto del programma
        public static int inTrap;           // istruzione in fase di trap
        static int loopInfinito;            // per la verifica di un loop infinito	
        public static StatoCpu Stato = StatoCpu.Ferma;

        // public static readonly int MASSIMO_INDIRIZZO = 255;
        // public static readonly int INDIRIZZO_STACK = 240;

        static Ram memoria = new Ram();
        //static int[] memoria = new int[MASSIMO_INDIRIZZO+1];	// memoria 
        static string[] ri = new string[3];     // registro istruzione corrente

        static List<Instruction> Code;
        static Instruction curIstruzione;

        public static bool IPOverRun
        {
            get { return ip > Code.Count || ip < 1; }
        }


        public static short IP
        {
            get { return ip; }
        }

        public static bool FlagSegno
        {
            get { return TestFlag(SF); }
        }

        public static bool FlagZero
        {
            get { return TestFlag(ZF); }
        }

        public static bool FlagOverflow
        {
            get { return TestFlag(OF); }
        }

        public static void Run(int IP)
        {
            ip = (short)IP;
            Run();
        }

        static bool Trap()
        {
            return curIstruzione != null && curIstruzione.Trap == true;
        }

        // ! codice sperimentale
        public static void SetTrap(int riga)
        {
            Code[riga].Trap = true;
        }

        static bool LoopInfinito(int numIstruzioni)
        {
            return loopInfinito > 0 && numIstruzioni == loopInfinito;
        }

        public static void Run()
        {
            Stato = StatoCpu.Attiva;
            int numIstruzioni = 0;
            try
            {
                while (!stop)
                {
                    Fetch();
                    if (Trap() && !(ip == inTrap))  // gestione trap (non catturata dal catch)
                    {
                        inTrap = ip;
                        throw new CpuTrapException();
                    }

                    Execute();

                    if (ip == Code.Count)
                        Stop();

                    if (IPOverRun)
                        throw new CpuException(CodiceErrore.IPNonValido);


                    numIstruzioni++;
                    if (LoopInfinito(numIstruzioni))
                    {
                        throw new CpuLoopException();
                    }
                }
            }
            catch (CpuException)    // non cattura TrapException e LoopException
            {
                Stop();
                throw;
            }
        }


        public static void Debug()
        {
            if (stop) return;
            Stato = StatoCpu.Attiva;
            try
            {
                Fetch();
                Execute();
            }
            catch (Exception)
            {
                Stop();
                throw;
            }

            if (ip == Code.Count)
                Stop();
        }

        public static void Init(List<Instruction> codice, List<int> memoriaDati, bool initRegs, int AloopInfinito)
        {
            stop = false;
            memoria.Imposta(memoriaDati);
            Code = codice;
            ip = 0;
            flags = 0;
            inTrap = -1;
            sp = (short)(Ram.MASSIMO_INDIRIZZO + 1);

            loopInfinito = AloopInfinito;
            Stato = StatoCpu.Pronta;
            if (initRegs)
                InizializzaRegs();
        }

        static void InizializzaRegs()
        {
            ax = bx = cx = dx = si = di = bp = 0;
            flags = 0;
        }


        public static void Fetch()
        {
            curIstruzione = Code[ip];
        }

        public static void Execute()
        {
            switch (curIstruzione.Code)
            {
                case "shl": Shl(); break;
                case "shr": Shr(); break;
                case "and": And(); break;
                case "or": Or(); break;
                case "xor": Xor(); break;
                case "not": Not(); break;
                case "neg": Neg(); break;
                case "mov": Mov(); break;
                case "movs": Movs(); break;
                case "nop": Nop(); break;
                case "add": Add(); break;
                case "sub": Sub(); break;
                case "mul": IMul(); break;
                case "div": IDiv(); break;
                case "cmp": Cmp(); break;
                case "jcxz": Jcxz(); break;
                case "jg": Jg(); break;
                case "jge": Jge(); break;
                case "jl": Jl(); break;
                case "jle": Jle(); break;
                case "jne": Jne(); break;
                case "je": Je(); break;
                case "jmp": Jmp(); break;
                case "jo": Jo(); break;
                case "jno": Jno(); break;
                case "js": Js(); break;
                case "jns": Jns(); break;
                case "dec": Dec(); break;
                case "inc": Inc(); break;
                case "pop": Pop(); break;
                case "push": Push(); break;
                case "pushf": PushF(); break;
                case "popf": PopF(); break;
                case "call": Call(); break;
                case "ret": Ret(); break;
                case "stop": Stop(); break;
            }
            ip++;
        }

        #region metodi che implementano le istruzioni


        static void And()
        {
            int op = LoadOp(1);
            op &= LoadOp(2);
            StoreOp(op, 1);
            SetFlags(op);
            SetFlag(OF, false);
        }

        static void Or()
        {
            int op = LoadOp(1);
            op |= LoadOp(2);
            StoreOp(op, 1);
            SetFlags(op);
            SetFlag(OF, false);
        }

        static void Xor()
        {
            short op = LoadOp(1);
            op ^= LoadOp(2);
            StoreOp(op, 1);
            SetFlags(op);
            SetFlag(OF, false);
        }

        static void Not()
        {
            int op = LoadOp(1);
            op = (short)~op;
            StoreOp(op, 1);
        }

        static void Neg()
        {
            short op = LoadOp(1);
            op = (short)(0 - op);
            StoreOp(op, 1);
            SetFlags(op);
        }

        static void Mov()
        {
            StoreOp(LoadOp(2), 1);
        }

        static void Movs()
        {
            memoria[di] = memoria[si];
        }

        static void Add()
        {
            int op = LoadOp(1);
            op += LoadOp(2);
            StoreOp(op, 1);
            SetFlags(op);
        }

        static void Sub()
        {
            int op = LoadOp(1);
            op -= LoadOp(2);
            StoreOp(op, 1);
            SetFlags(op);
        }

        static void IMul()
        {
            int tmp = ax * LoadOp(1);
            ax = (short)tmp;
            dx = (short)(tmp >> 16);
            SetFlags(OF + ZF, tmp);
        }

        static void IDiv()
        {
            int dividendo = ax + (dx << 16);
            dx = (short)(dividendo % LoadOp(1));
            ax = (short)(dividendo / LoadOp(1));
        }

        static void Inc()
        {
            int op = LoadOp(1);
            op++;
            StoreOp(op, 1);
            SetFlags(op);
        }

        static void Dec()
        {
            int op = LoadOp(1);
            op--;
            StoreOp(op, 1);
            SetFlags(op);
        }

        static void Cmp()
        {
            int op = LoadOp(1);
            op -= LoadOp(2);
            SetFlags(op);
        }

        static void Jmp()
        {
            ip = NuovoIp();
        }

        static void Je()
        {
            if (TestFlag(ZF))
                ip = NuovoIp();
        }

        static void Jne()
        {
            if (!TestFlag(ZF))
                ip = NuovoIp();
        }

        static void Jg()
        {
            if (TestFlag(SF) == TestFlag(OF) && !TestFlag(ZF))
                ip = NuovoIp();
        }

        static void Jge()
        {
            if (TestFlag(SF) == TestFlag(OF))
                ip = NuovoIp();
        }

        static void Jl()
        {
            if (TestFlag(SF) != TestFlag(OF))
                ip = NuovoIp();
        }

        static void Jle()
        {
            if (TestFlag(SF) != TestFlag(OF) || TestFlag(ZF))
                ip = NuovoIp();
        }

        static void Jcxz()
        {
            if (cx == 0)
                ip = NuovoIp();
        }

        static void Jo()
        {
            if (TestFlag(OF))
                ip = NuovoIp();
        }

        static void Jno()
        {
            if (!TestFlag(OF))
                ip = NuovoIp();
        }

        static void Js()
        {
            if (TestFlag(SF))
                ip = NuovoIp();
        }

        static void Jns()
        {
            if (!TestFlag(SF))
                ip = NuovoIp();
        }

        static void PushCode(short valore)
        {
            sp--;
            if (sp < Ram.INDIRIZZO_STACK)
                throw new CpuException(CodiceErrore.StackOverflow);
            memoria[sp] = valore;
        }

        static short PopCode()
        {
            if (sp == Ram.MASSIMO_INDIRIZZO + 1)
                throw new CpuException(CodiceErrore.StackUnderflow);
            return memoria[sp++];

        }

        static void Push()
        {
            PushCode(LoadOp(1));
        }

        static void Pop()
        {
            StoreOp(PopCode(), 1);
        }

        static void PushF()
        {
            PushCode(flags);
        }

        static void PopF()
        {
            flags = PopCode();
        }

        static void Call()
        {
            PushCode(ip);
            ip = NuovoIp();
        }

        static void Ret()
        {
            ip = PopCode();
        }

        static void Shl()       // non gestisce flag di overflow!
        {
            int op = LoadOp(1);
            op <<= LoadOp(2);
            StoreOp(op, 1);
            SetFlags(ZF + SF, op);
        }

        static void Shr()       // non gestisce flag di overflow!
        {
            int op = LoadOp(1);
            op >>= LoadOp(2);
            StoreOp(op, 1);
            SetFlags(ZF + SF, op);
        }

        static void Nop()
        {
        }

        public static void Stop()
        {
            stop = true;
            Stato = StatoCpu.Ferma;
        }

        static short NuovoIp()
        {
            return (short)(LoadOp(1) - 1);
        }

        #endregion


        #region metodi che caricano e memorizzano gli operandi e settano i flags

        static short LoadOp(int numOp)
        {
            if (numOp == 1)
                return LoadOp(curIstruzione.Op1, curIstruzione.Offset1);
            else
                return LoadOp(curIstruzione.Op2, curIstruzione.Offset2);
        }


        static void StoreOp(int valore, int numOp)
        {
            if (numOp == 1)
                StoreOp((short)valore, curIstruzione.Op1, curIstruzione.Offset1);
            else
                StoreOp((short)valore, curIstruzione.Op2, curIstruzione.Offset2);
        }

        static short LoadOp(IdOp op, int offset)
        {
            switch (op)
            {
                case IdOp.ax: return ax;
                case IdOp.bx: return bx;
                case IdOp.cx: return cx;
                case IdOp.dx: return dx;
                case IdOp.si: return si;
                case IdOp.di: return di;
                case IdOp.bp: return bp;
                case IdOp.sp: return sp;
                case IdOp._si: return memoria[si + offset];
                case IdOp._di: return memoria[di + offset];
                case IdOp._bx: return memoria[bx + offset];
                case IdOp._bp: return memoria[bp + offset];
                case IdOp.Costante: return (short)offset;
                case IdOp.Memoria: return memoria[offset];
                case IdOp.Etichetta: return (short)offset;
            }
            return -1;
        }

        static void StoreOp(short valore, IdOp op, int offset)
        {
            switch (op)
            {
                case IdOp.ax: ax = valore; break;
                case IdOp.bx: bx = valore; break;
                case IdOp.cx: cx = valore; break;
                case IdOp.dx: dx = valore; break;
                case IdOp.si: si = valore; break;
                case IdOp.di: di = valore; break;
                case IdOp.bp: bp = valore; break;
                case IdOp.sp: sp = valore; break;
                case IdOp._si: memoria[si + offset] = valore; break;
                case IdOp._di: memoria[di + offset] = valore; break;
                case IdOp._bx: memoria[bx + offset] = valore; break;
                case IdOp._bp: memoria[bp + offset] = valore; break;
                case IdOp.Memoria: memoria[offset] = valore; break;
            }
        }


        static void SetFlags(short mask, int ris)
        {
            if ((ZF & mask) != 0)
                flags = (short)(((short)ris == 0) ? flags | ZF : flags & ~ZF);

            if ((SF & mask) != 0)
                flags = (short)(((short)ris < 0) ? flags | SF : flags & ~SF);

            if ((OF & mask) != 0)
                flags = (short)((ris > short.MaxValue || ris < short.MinValue) ? flags | OF : flags & ~OF);
        }

        static void SetFlags(int ris)
        {
            SetFlags(TUTTI, ris);
        }

        static void SetFlag(int flag, bool stato)
        {
            if (stato)
                flags |= (short)flag;
            else
                flags &= (short)~flag;

        }

        public static bool TestFlag(int flag)
        {
            return (flags & flag) != 0;
        }

        #endregion

        #region metodi di utilità

        public static string DumpReg(int indReg)
        {
            string formatoReg;
            if (Ambiente.FormatoDati != FormatoValore.Hex)
                formatoReg = " =  [HEX: {0" + Ambiente.FRHEX + "}] [BIN: {1}] [CAR: {2}]";
            else
                formatoReg = " =  [DEC: {0" + Ambiente.FRDEC + "}] [BIN: {1}] [CAR: {2}]";
            short tmp;
            switch (indReg)
            {
                case 0: tmp = ax; break;
                case 1: tmp = bx; break;
                case 2: tmp = cx; break;
                case 3: tmp = dx; break;
                case 4: tmp = si; break;
                case 5: tmp = di; break;
                case 6: tmp = bp; break;
                case 7: tmp = sp; break;
                case 8: tmp = ip; break;
                default: tmp = 0; break;
            }
            return string.Format(nomiRegs[indReg] + formatoReg, tmp, ShortToStrBin(tmp), IntToChar(tmp));

        }

        public static string ShortToStrBin(short valore)
        {
            StringBuilder s = new StringBuilder();
            int v = (ushort)valore;
            int r = v % 2;
            v = v / 2;
            while (v != 0)
            {
                s.Insert(0, r);
                r = v % 2;
                v = v / 2;
            }
            s.Insert(0, r);
            while (s.Length < 16)
                s.Insert(0, 0);

            return s.ToString();

        }

        public static string[] DumpRegs()
        {
            string formatoReg = " = {0" + Ambiente.FR + "} ";
            string[] reg = new string[9];

            string zfs = (TestFlag(ZF)) ? "Z" : "z";
            string sfs = (TestFlag(SF)) ? "S" : "s";

            reg[0] = string.Format("AX" + formatoReg, ax);
            reg[1] = string.Format("BX" + formatoReg, bx);
            reg[2] = string.Format("CX" + formatoReg, cx);
            reg[3] = string.Format("DX" + formatoReg, dx);
            reg[4] = string.Format("SI" + formatoReg, si);
            reg[5] = string.Format("DI" + formatoReg, di);
            reg[6] = string.Format("BP" + formatoReg, bp);
            reg[7] = string.Format("SP" + formatoReg, sp);
            reg[8] = string.Format("IP" + formatoReg, ip);
            //reg[9] = "";
            //reg[10] = string.Format("{0}  {1}", zfs, sfs);
            return reg;
        }

        static string IntToChar(int x)
        {
            if (x == 0)
                return Ambiente.FormatoCarZero;
            if (x < 0)
                x = 128;
            return Convert.ToChar(x).ToString();
        }

        public static List<string> DumpMemoria(int da, int a, int colonne)
        {
            if (memoria == null) return null;
            string formatoIndirizzo = "{0" + Ambiente.FI + "}: ";
            string formatoDato = "{0" + Ambiente.FD + "} ";

            List<string> dump = new List<string>();
            string s = string.Format(formatoIndirizzo, da);
            for (int i = da; i < a;)
            {
                if (Ambiente.FormatoDati == FormatoValore.Car)
                    s = s + string.Format(formatoDato, IntToChar(memoria[i]));
                else
                    s = s + string.Format(formatoDato, memoria[i]);

                if ((++i - da) % colonne == 0)
                {
                    dump.Add(s);
                    s = string.Format(formatoIndirizzo, i);
                }
            }
            if (s.Length > 6)
                dump.Add(s);
            return dump;
        }
        static void Errore(string msg)
        {
            Console.WriteLine("\a" + msg);
        }


        static void VisualizzaIstruzione()
        {
            Console.WriteLine("\n{0}\n", Code[ip]);
        }

        #endregion
    }




}