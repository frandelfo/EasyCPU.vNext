using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using EasyCpu.Common;


namespace EasyCpu.Backend.Local
{

    public static class Storage
    {
        static string PREF_DATA = ".DATA";

        public static void Salva(string nome, string[] codice, string[] dati)
        {
            StreamWriter sw = new StreamWriter(nome);
            if (codice != null)
            {
                for (int i = 0; i < codice.Length; i++)
                    sw.WriteLine(codice[i]);
            }

            if (dati != null)
            {
                sw.WriteLine(PREF_DATA);
                for (int i = 0; i < dati.Length; i++)
                    sw.WriteLine(dati[i]);
            }
            sw.Close();
        }

        public static void Apri(string nome, out List<string> codice, out List<string> dati)
        {
            codice = new List<string>();
            dati = new List<string>();
            StreamReader sr = new StreamReader(nome);

            string riga = sr.ReadLine();
            while (riga != null && riga.Trim().ToUpper() != PREF_DATA)
            {
                codice.Add(riga);
                riga = sr.ReadLine();
            }
            if (riga == PREF_DATA)
            {
                riga = sr.ReadLine();
                while (riga != null)
                {
                    dati.Add(riga);
                    riga = sr.ReadLine();
                }
            }

            sr.Close();
        }

        public static void SalvaOpzioni()
        {
            if (!Directory.Exists(Ambiente.EasyCPUPath))
            {
                Directory.CreateDirectory(Ambiente.EasyCPUPath);
            }
            Ambiente.VersioneAssembly = "";
            StreamWriter sw = new StreamWriter(Ambiente.OpzioniNomeFile);
            sw.WriteLine(string.Format("{0} = {1}", Ambiente.CHIAVE_FORMATODATI, Ambiente.FormatoDati));
            sw.WriteLine(string.Format("{0} = {1}", Ambiente.CHIAVE_FORMATOCARZERO, Ambiente.FormatoCarZero));
            sw.WriteLine(string.Format("{0} = {1}", Ambiente.CHIAVE_MAXERRORI, Ambiente.MaxNumErrori));
            sw.WriteLine(string.Format("{0} = {1}", Ambiente.CHIAVE_COLONNESTACK, Ambiente.ColonneStack));
            sw.WriteLine(string.Format("{0} = {1}", Ambiente.CHIAVE_INIZIALIZZAREGISTRI, Ambiente.InizializzaRegistri));
            sw.WriteLine(string.Format("{0} = {1}", Ambiente.CHIAVE_LOOPINFINITO, Ambiente.LoopInfinito));
            sw.WriteLine(string.Format("{0} = {1}", Ambiente.CHIAVE_MOSTRAMEMORIA, Ambiente.MostraMemoria));
            sw.WriteLine(string.Format("{0} = {1}", Ambiente.CHIAVE_FONTEDITORNOME, Ambiente.FontEditorNome));
            sw.WriteLine(string.Format("{0} = {1}", Ambiente.CHIAVE_FONTEDITORSIZE, Ambiente.FontEditorSize));
            sw.WriteLine(string.Format("{0} = {1}", Ambiente.CHIAVE_FONTEDITORSTYLE, Ambiente.FontEditorStyle));
            sw.WriteLine(string.Format("{0} = {1}", Ambiente.CHIAVE_FONTEDITOR_ZOOM, Ambiente.EditorZoomFactor));
            sw.WriteLine(string.Format("{0} = {1}", Ambiente.CHIAVE_PIENOSCHERMO, Ambiente.PienoSchermo));
            sw.WriteLine(string.Format("{0} = {1}", Ambiente.CHIAVE_VERSIONE, Ambiente.VersioneAssembly));
            sw.WriteLine(string.Format("{0} = {1}", Ambiente.CHIAVE_MARGINESINISTRO, Ambiente.MargineSinistro));
            sw.Close();
        }

        public static void LeggiOpzioni()
        {
            if (!File.Exists(Ambiente.OpzioniNomeFile))
                return;
            StreamReader sr = new StreamReader(Ambiente.OpzioniNomeFile);
            SortedList sl = new SortedList();

            string riga = sr.ReadLine();
            while (riga != null)
            {
                string[] tmp = riga.Split('=');
                if (tmp.Length == 2)
                    sl.Add(tmp[0].Trim(), tmp[1].Trim());
                riga = sr.ReadLine();
            }
            sr.Close();

            string valore = (string)sl[Ambiente.CHIAVE_FORMATODATI];
            if (valore != null)
                Ambiente.FormatoDati = (FormatoValore)Enum.Parse(typeof(FormatoValore), valore, true);

            valore = (string)sl[Ambiente.CHIAVE_FORMATOCARZERO];
            if (valore != null)
                Ambiente.FormatoCarZero = valore;

            valore = (string)sl[Ambiente.CHIAVE_MAXERRORI];
            if (valore != null)
                StrToInt(valore, ref Ambiente.MaxNumErrori);

            valore = (string)sl[Ambiente.CHIAVE_COLONNESTACK];
            if (valore != null)
                StrToInt(valore, ref Ambiente.ColonneStack);

            valore = (string)sl[Ambiente.CHIAVE_INIZIALIZZAREGISTRI];
            if (valore != null)
                StrToBool(valore, ref Ambiente.InizializzaRegistri);

            valore = (string)sl[Ambiente.CHIAVE_LOOPINFINITO];
            if (valore != null)
                StrToInt(valore, ref Ambiente.LoopInfinito);

            valore = (string)sl[Ambiente.CHIAVE_MARGINESINISTRO];
            if (valore != null)
                StrToInt(valore, ref Ambiente.MargineSinistro);
            if (Ambiente.MargineSinistro < 0 || Ambiente.MargineSinistro > 15)
                Ambiente.MargineSinistro = 0;

            valore = (string)sl[Ambiente.CHIAVE_PIENOSCHERMO];
            if (valore != null)
                StrToBool(valore, ref Ambiente.PienoSchermo);

            valore = (string)sl[Ambiente.CHIAVE_MOSTRAMEMORIA];
            if (valore != null)
                StrToBool(valore, ref Ambiente.MostraMemoria);

            valore = (string)sl[Ambiente.CHIAVE_FONTEDITORNOME];
            if (valore != null)
                Ambiente.FontEditorNome = valore;

            valore = (string)sl[Ambiente.CHIAVE_FONTEDITORSIZE];
            if (valore != null)
                StrToFloat(valore, ref Ambiente.FontEditorSize);

            valore = (string)sl[Ambiente.CHIAVE_FONTEDITOR_ZOOM];
            if (valore != null)
                StrToFloat(valore, ref Ambiente.EditorZoomFactor);

            valore = (string)sl[Ambiente.CHIAVE_FONTEDITORSTYLE];
            if (valore != null)
                StrToInt(valore, ref Ambiente.FontEditorStyle);

            valore = (string)sl[Ambiente.CHIAVE_VERSIONE];
            if (valore != null)
                Ambiente.VersioneAssembly = valore;
        }

        static void StrToInt(string s, ref int valore)
        {
            try { valore = int.Parse(s); } catch { }
        }

        static void StrToFloat(string s, ref float valore)
        {
            try { valore = float.Parse(s); } catch { }
        }

        static void StrToBool(string s, ref bool valore)
        {
            try { valore = bool.Parse(s); } catch { }
        }

        public static void SalvaFileRecenti()
        {
            StreamWriter sw = new StreamWriter(Ambiente.RecentiNomeFile);
            var recents = Ambiente.FileRecenti.Take(Ambiente.MAXFILERECENTI).ToList();
            for (int i = 0; i < recents.Count; i++)
                sw.WriteLine(Ambiente.FileRecenti[i]);
            sw.Close();
        }

        public static void ApriFileRecenti()
        {
            if (!File.Exists(Ambiente.RecentiNomeFile))
                return;
            StreamReader sr = new StreamReader(Ambiente.RecentiNomeFile);
            Ambiente.FileRecenti = new List<string>();
            string path;
            while ((path = sr.ReadLine()) != null)
                Ambiente.FileRecenti.Add(path);
            sr.Close();
        }

        public static bool CreaPathProgetti()
        {
            if (Directory.Exists(Ambiente.ProgettiPath))
                return true;
            try
            {
                Directory.CreateDirectory(Ambiente.ProgettiPath);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static string CartellaIniziale()
        {
            Storage.ApriFileRecenti();
            if (Storage.CreaPathProgetti() && Ambiente.PathCorrente == "")
                return Ambiente.ProgettiPath;
            else
                return Ambiente.PathCorrente;
        }
    }
}
