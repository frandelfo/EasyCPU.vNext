using System;
using System.Text;
using System.Collections;
using EasyCpu.Assembler.Memoria;
using EasyCpu.Assembler.Processore;
using EasyCpu.Common;

namespace EasyCpu.Assembler.Parsing
{
	public class Parser
	{
		public static OpCode[] SetCode =
		{

		new OpCode("mov", 2),
		new OpCode("movs", 0),
		new OpCode("add", 2),
		new OpCode("sub", 2),
		new OpCode("cmp", 2),
		new OpCode("and", 2),
		new OpCode("or", 2),
		new OpCode("xor", 2),
		new OpCode("not", 1),
		new OpCode("neg", 1),
		new OpCode("mul", 1),
		new OpCode("div", 1),
		new OpCode("inc", 1),
		new OpCode("dec", 1),
		new OpCode("push", 1),
		new OpCode("pop", 1),
		new OpCode("pushf", 0),
		new OpCode("popf", 0),
		new OpCode("call", 1, TipoOp.Codice),
		new OpCode("jcxz", 1, TipoOp.Codice),
		new OpCode("je", 1, TipoOp.Codice),
		new OpCode("jg", 1, TipoOp.Codice),
		new OpCode("jl", 1, TipoOp.Codice),
		new OpCode("jle", 1, TipoOp.Codice),
		new OpCode("jge", 1, TipoOp.Codice),
		new OpCode("jmp", 1, TipoOp.Codice),
		new OpCode("jne", 1, TipoOp.Codice),
		new OpCode("jo", 1, TipoOp.Codice),
		new OpCode("jno", 1, TipoOp.Codice),
		new OpCode("js", 1, TipoOp.Codice),
		new OpCode("jns", 1, TipoOp.Codice),
		new OpCode("ret", 0),
		new OpCode("nop", 0),
		new OpCode("stop", 0),
		new OpCode("shl", 2),
		new OpCode("shr", 2),

	};

		public const char FINE = '\0';
		static string riga;
		static int indcar;

		public static int IndCar { get { return indcar; } }

		static void SaltaSpazi()
		{
			while (SeSpazio(riga[indcar]))
				indcar++;
		}

		static bool SeInNumero(char c)
		{
			return "01234567890abcdef".IndexOf(c) != -1 || c == 'h';
		}

		public static char TestChar()
		{
			SaltaSpazi();
			return riga[indcar];
		}


		public static string EstraiToken()
		{
			SaltaSpazi();
			if (SeFine())       // fine riga
				return null;

			switch (riga[indcar])
			{
				case '\'': indcar++; return "'";
				case ',': indcar++; return ",";
				case ':': indcar++; return ":";
				case '[': indcar++; return "[";
				case ']': indcar++; return "]";
				case '+':
				case '-': return riga[indcar++].ToString();

				default:
					if (SeInIdentificatore(riga[indcar]))
					{
						int numCar = 1;
						while (SeInIdentificatore(riga[indcar + numCar]))
							numCar++;
						string tmp = riga.Substring(indcar, numCar);
						indcar += numCar;
						return tmp;
					}
					else
						throw new CodiceException(CodiceErrore.CarattereNonValido);
			}
		}

		static int LeggiCostanteChar()
		{
			indcar++;
			char c = riga[indcar++];
			if (c == FINE)
				throw new CodiceException(CodiceErrore.Formato);
			int valore = c;
			c = riga[indcar++];
			if (c != '\'')
				throw new CodiceException(CodiceErrore.Formato);
			return valore;

		}


		public static string LeggiIdentificatore()
		{
			SaltaSpazi();
			if (SeFine())
				return null;
			if (SeInIdentificatore(riga[indcar]))
			{
				int numCar = 1;
				while (SeInIdentificatore(riga[indcar + numCar]))
					numCar++;
				string tmp = riga.Substring(indcar, numCar);
				indcar += numCar;
				return tmp;
			}
			return null;
		}


		public static string TestToken()
		{
			int tmp = indcar;
			string token = EstraiToken();
			indcar = tmp;
			return token;
		}

		static bool SeFine()
		{
			return riga[indcar] == FINE;
		}

		static bool SeSpazio(char c)
		{
			return c == ' ' || c == '\t';
		}

		static bool SeInIdentificatore(char c)
		{
			return Char.IsLetterOrDigit(c) || c == '_';
		}

		static bool SeEtichetta(string s)
		{
			return !(s == null) && !(s == "") && Char.IsLetter(s[0]) &&
				s[s.Length - 1] == ':';
		}

		static bool SeIndirizzoDati(string s)
		{
			return s != null && s != "" && s[0] == '@';
		}

		public static int LeggiValore()
		{
			char c = TestChar();
			if (c == '\'')
			{
				return LeggiCostanteChar();
			}
			string tok = EstraiToken();
			string segno = "";
			if (tok == "-" || tok == "+")
			{
				segno = tok;
				tok = EstraiToken();
			}
			return StringToInt(segno + tok);
		}


		static int LeggiIndirizzo()
		{
			string tok = EstraiToken();
			try
			{
				return StringToInt(tok);
			}
			catch
			{
				throw new CodiceException(CodiceErrore.IndirizzoDatiNonValido);
			}
		}

		static List<int> LeggiValori()
		{
			List<int> valori = new List<int>();
			valori.Add(LeggiValore());
			string token = EstraiToken();
			while (token != null)
			{
				if (token != ",")
					throw new CodiceException(CodiceErrore.AttesaVirgola);
				valori.Add(LeggiValore());
				token = EstraiToken();
			}
			return valori;
		}

		// ritorna true se il prossimo token corrisponde al simbolo specificato

		public static bool LeggiSimbolo(string sim)
		{
			return EstraiToken() == sim;
		}

		public static bool TestSimbolo(string sim)
		{
			return TestToken() == sim;
		}

		static bool SeSegno(char c)
		{
			return c == '-' || c == '+';
		}

		static bool SeSegno(string s)
		{
			return s == "-" || s == "+";
		}

		public static int StringToInt(string valore)
		{
			int tmp;
			if (valore == null || valore == "")
				throw new CodiceException(CodiceErrore.Formato);
			int segno = 1;
			if (SeSegno(valore[0]))
			{
				segno = (valore[0] == '+') ? 1 : -1;
				valore = valore.Remove(0, 1);
			}
			if (valore == "")
				throw new CodiceException(CodiceErrore.Formato);
			if (!Char.IsDigit(valore[0]))
				throw new CodiceException(CodiceErrore.Formato);
			if (valore[valore.Length - 1] == 'h')
			{
				if (segno == -1)
					throw new CodiceException(CodiceErrore.Formato);
				else
					tmp = segno * HexToInt(valore.Substring(0, valore.Length - 1));

				if (tmp > ushort.MaxValue || tmp < ushort.MinValue)
					throw new CodiceException(CodiceErrore.CostanteFuoriIntervallo);
			}
			else
			{

				try
				{
					tmp = segno * Convert.ToInt32(valore);
				}
				catch
				{
					throw new CodiceException(CodiceErrore.Formato);
				}
				if (tmp > short.MaxValue || tmp < short.MinValue)
					throw new CodiceException(CodiceErrore.CostanteFuoriIntervallo);
			}
			return tmp;
		}

		public static int HexToInt(string valore)
		{
			int ris = 0;
			int len = valore.Length - 1;
			for (int i = len; i >= 0; i--)
				if (valore[i] >= 'a' && valore[i] <= 'f')
					ris += (valore[i] - 'a' + 10) * (int)Math.Pow(16, len - i);
				else
					if (valore[i] >= '0' && valore[i] <= '9')
					ris += (valore[i] - '0') * (int)Math.Pow(16, len - i);
				else
					throw new CodiceException(CodiceErrore.Formato);
			return ris;

		}

		public static int CercaOpCode(string Nome, out int numOp, out TipoOp tipo)
		{
			for (int i = 0; i < SetCode.Length; i++)
				if (Nome == SetCode[i].Nome)
				{
					numOp = SetCode[i].NumOp;
					tipo = SetCode[i].Tipo;
					return i;
				}
			numOp = -1;
			tipo = TipoOp.Dati;
			return -1;
		}

		static void LeggiOperandoIndiretto(out IdOp op, out int offset)
		{
			offset = 0;
			string token = TestToken();
			switch (token)
			{
				case "si": op = IdOp._si; break;
				case "di": op = IdOp._di; break;
				case "bx": op = IdOp._bx; break;
				case "bp": op = IdOp._bp; break;
				default:                    // è un indirizzo di memoria
					offset = LeggiValore();
					op = IdOp.Memoria;
					if (!IntervalloOk(op, offset))
						throw new CodiceException(CodiceErrore.IntervalloIndirizzoDati);
					token = EstraiToken();
					if (token != "]")
						throw new CodiceException(CodiceErrore.AttesaQuadraChiusura);
					return;

			}
			EstraiToken();      // scarta registro precedentemente testato
			token = EstraiToken();
			switch (token)
			{
				case "+":
				case "-":
					offset = LeggiValore();
					if (token == "-")
						offset *= -1;
					if (EstraiToken() != "]")
						throw new CodiceException(CodiceErrore.AttesaQuadraChiusura);
					break;
				case "]": break;
				default: throw new CodiceException(CodiceErrore.Sintassi);
			}
		}

		static bool IntervalloOk(IdOp op, int offset)
		{
			return !(op == IdOp.Memoria && (offset < 0 || offset > Ram.MASSIMO_INDIRIZZO));
		}

		static void LeggiOperando(out IdOp op, out int offset)
		{
			offset = 0;
			op = IdOp.Null;
			string token = TestToken();
			if (token == "[")
			{
				EstraiToken();      // scarta parentesi quadra
				LeggiOperandoIndiretto(out op, out offset);
				return;
			}
			else
				switch (token)
				{
					case "ax": op = IdOp.ax; break;
					case "bx": op = IdOp.bx; break;
					case "cx": op = IdOp.cx; break;
					case "dx": op = IdOp.dx; break;
					case "si": op = IdOp.si; break;
					case "di": op = IdOp.di; break;
					case "bp": op = IdOp.bp; break;
					case "sp": op = IdOp.sp; break;
					case "'": goto default;     // è una costante carattere
					default:
						op = IdOp.Costante;
						offset = LeggiValore();
						if (!IntervalloOk(op, offset))
							throw new CodiceException(CodiceErrore.IntervalloIndirizzoDati);
						return;
				}
			EstraiToken();  // scarta registro
		}


		public static List<int> CompilaDati(string s, int indice, out int indirizzo)
		{
			indirizzo = 0;
			riga = s;
			indcar = 0;
			indirizzo = LeggiIndirizzo();
			if (EstraiToken() != ":")
				throw new CodiceException(CodiceErrore.AttesoDuePunti);

			if (indirizzo < 0 || indirizzo > Ram.MASSIMO_INDIRIZZO)
				throw new CodiceException(CodiceErrore.IntervalloIndirizzoDati);

			return LeggiValori();

		}



		// elimina commenti, trasforma in minuscolo e aggiunge carattere FINE
		static string AdattaRiga(string riga)
		{
			int indCommento = riga.IndexOf("'");
			if (indCommento == -1) indCommento = riga.Length;

			return riga.ToLower().Substring(0, indCommento) + FINE;
		}

		public static Instruction Compila(string s, out string etichetta)
		{
			etichetta = null;
			int numOp = -1;
			TipoOp tipo;
			int offset1, offset2;
			IdOp op1, op2;
			indcar = 0;
			riga = s;
			string token = LeggiIdentificatore();
			if (token != null && riga[indcar] == ':')
			{
				etichetta = token;
				EstraiToken();          // scarta i duepunti
				token = EstraiToken();
				if (token == null)              // c'è solo l'etichetta
												//return new Istruzione("nop");	// ritorna istruzione NOP
					return null;
			}
			if (token == null || CercaOpCode(token, out numOp, out tipo) == -1)
				throw new CodiceException(CodiceErrore.AttesoCodiceIstruzione);

			string code = token;            // opcode		
			switch (numOp)
			{
				case 0:
					if (TestToken() != null)
						throw new CodiceException(CodiceErrore.NumeroOperandi);
					return new Instruction(code);

				case 1:
					if (tipo == TipoOp.Codice)
					{
						string salto = EstraiToken();
						if (salto == null || TestToken() != null)
							throw new CodiceException(CodiceErrore.NumeroOperandi);
						return new Instruction(code, salto);
					}
					else
						LeggiOperando(out op1, out offset1);
					if (TestToken() != null)
						throw new CodiceException(CodiceErrore.NumeroOperandi);
					return new Instruction(code, op1, offset1);

				case 2:
					LeggiOperando(out op1, out offset1);
					bool siVirgola = LeggiSimbolo(",");
					LeggiOperando(out op2, out offset2);
					if (TestToken() != null)
						throw new CodiceException(CodiceErrore.NumeroOperandi);

					return new Instruction(code, op1, op2, offset1, offset2);
			}
			throw new Exception("l'istruzione non è stata creata");
		}

	}

}

