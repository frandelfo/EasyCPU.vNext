using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyCpu.Assembler.Memoria;
using EasyCpu.Common;
using EasyCpu.Assembler.Processore;

namespace EasyCpu.Assembler.Parsing
{
    public class Compiler
    {
        static List<IndirizzoEtichetta> TabellaEtichette;
        public static List<int> TabellaDebug;

        static bool SeCommento(string s)
        {
            return s[0] == '\'';
        }

        public static List<int> CompilaDati(List<string> data, ref List<CompilerError> errori)
        {
            List<int>memoria = new int[Ram.MASSIMO_INDIRIZZO + 1].ToList();
            for (int indRiga = 0; indRiga < data.Count; indRiga++)
            {
                try
                {
                    int indirizzo;
                    string s = PreparaRiga(data[indRiga]);
                    if (s == "") continue;
                    List<int> rigaDati = Parser.CompilaDati(s, indRiga, out indirizzo);
                    if (rigaDati.Count + indirizzo > Ram.MASSIMO_INDIRIZZO)
                        throw new CodiceException(CodiceErrore.IntervalloIndirizzoDati);

                    for (int i = 0; i < rigaDati.Count; i++)
                    {
                        memoria[indirizzo + i] = rigaDati[i];
                    }

                    //Array.Copy(rigaDati, 0, memoria, indirizzo, rigaDati.Count);
                }
                catch (CodiceException e)
                {
                    if (errori == null)
                        errori = new List<CompilerError>();
                    errori.Add(new CompilerError(Errori.Msg(e.err), indRiga, Parser.IndCar, CompilerError.DATI));
                }
            }
            if (errori == null)
                return memoria;
            else
                return null;
        }

        public static string PreparaRiga(string riga)
        {
            StringBuilder sb = new StringBuilder();
            bool inCostanteChar = false;
            if (riga == null)
                return "";
            for (int i = 0; i < riga.Length; i++)
            {
                if (riga[i] == '/')
                {
                    if (i + 1 < riga.Length && riga[i + 1] == '/')
                        break;
                }
                if (riga[i] == '\'')
                    inCostanteChar = !inCostanteChar;
                char c = (inCostanteChar) ? riga[i] : Char.ToLower(riga[i]);
                sb.Append(c);
            }
            riga = sb.ToString().Trim() + Parser.FINE;
            if (riga.Length == 1)
                return "";
            return riga;
        }

        public static List<Instruction> CompilaCodice(List<string> code, ref List<CompilerError> errori, int rigaTrap)
        {
            if (code == null || code.Count == 0) return null;
            bool messoTrap = false; // consente di mettere un solo trap
            if (rigaTrap == -1) messoTrap = true; // evita l'impostazione del trap
            int indiceEtichetta;
            List<Instruction> istruzioni = new List<Instruction>();
            List<IndirizzoEtichetta> etichette = new List<IndirizzoEtichetta>();
            List<int> debug = new List<int>();

            //codice = null;		
            for (int indRiga = 0; indRiga < code.Count; indRiga++)
            {
                try
                {
                    string s = PreparaRiga(code[indRiga]);
                    string etichetta;
                    if (s == "") continue;
                    Instruction istr = Parser.Compila(s, out etichetta);

                    if (istr != null)
                    {
                        if (indRiga >= rigaTrap && !messoTrap)
                        {
                            istr.Trap = true;
                            messoTrap = true;
                        }
                        istr.indRiga = indRiga;
                        istruzioni.Add(istr);

                        debug.Add(indRiga);
                        indiceEtichetta = istruzioni.Count - 1; // indica l'istruzione corrente
                    }
                    else
                        indiceEtichetta = istruzioni.Count; // indica la prossima istruzione disponibile

                    if (etichetta != null)
                        etichette.Add(new IndirizzoEtichetta(etichetta, indiceEtichetta));
                }
                catch (CodiceException e)
                {
                    if (errori == null)
                        errori = new List<CompilerError>();
                    errori.Add(new CompilerError(Errori.Msg(e.err), indRiga, Parser.IndCar, CompilerError.CODICE));
                }
            }

            TabellaEtichette = etichette;
            // esamina e completa i riferimenti alle etichette
            for (int indRiga = 0; indRiga < istruzioni.Count; indRiga++)
            {
                Instruction istr = (Instruction)istruzioni[indRiga];
                if (istr.Etichetta == null) continue;
                istr.Offset1 = CercaEtichetta(istr.Etichetta);
                if (istr.Offset1 == -1)
                {
                    if (errori == null)
                        errori = new List<CompilerError>();
                    errori.Add(new CompilerError(Errori.Msg(CodiceErrore.EtichettaNonValida), istr.indRiga, -1, CompilerError.CODICE));
                }
                istruzioni[indRiga] = istr;

            }
            if (errori == null && istruzioni.Count > 0)
            {
                TabellaDebug = debug;
                return istruzioni;
            }
            else
                return null;
        }

        static int CercaEtichetta(string s)
        {
            for (int i = 0; i < TabellaEtichette.Count; i++)
                if (s == TabellaEtichette[i].Etichetta)
                    return TabellaEtichette[i].Indirizzo;

            return -1;
        }

    }

}
